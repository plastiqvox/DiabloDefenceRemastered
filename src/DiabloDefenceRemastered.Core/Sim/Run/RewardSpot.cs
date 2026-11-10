namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// 보상 자리 (v0.8, 설계서 3.6 "보상 자리" — 계약·구현 WP1f, 기획 v0.8.1 11.4). 정리가 끝난 순간(보스 방은 보스가 무너진 순간) 주인공 앞에 보상을 놓는다.
/// <list type="number">
/// <item>방향 후보 = 바라보는 쪽, +45°, −45°, +90°, −90°, +135°, −135°, 180°(이 순서, + 쪽 먼저).</item>
/// <item>후보마다 주인공 발에서 그 방향으로 <see cref="Step"/> px 씩 나아가며 <c>INavWorld.IsInsideMap</c> 이고 <c>Clearance ≥ 문턱</c> 인 동안만 걷는다(직선이 끊기지 않으므로 그 자리는 주인공과 길로 이어진다).
/// <c>distance</c> 까지 간 첫 방향의 끝, 없으면 가장 멀리 간 방향의 끝(같으면 앞 후보).</item>
/// <item>어느 방향도 <see cref="Step"/> 을 못 가면(v0.8.1 — 문 틈·기둥 사이) 같은 차례를 문턱 = 주인공 반경으로 한 번 더, 그래도 못 가면 주인공 발.</item>
/// </list>
/// 난수를 쓰지 않는다. <c>TryPlace</c>(WP2)를 기다리지 않으려고 <c>INavWorld</c> 질의 둘만 쓴다(자원 없는 시험은 TestKit 직선 내비). 내비가 없는 월드(모듈 없는 시험)는 벽이 없다고 본다.
/// </summary>
public static class RewardSpot
{
    /// <summary>걷기 한 걸음(px).</summary>
    public const float Step = 8f;

    /// <summary>방향 후보의 회전(도, 바라보는 쪽 기준 — 이 순서).</summary>
    public static IReadOnlyList<float> TurnDegrees { get; } = [0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f];

    /// <summary>월드의 주인공 발·바라보는 방향·반경으로 <see cref="Find(INavWorld?, Vector2, Vector2, float, RewardSpotDef)"/>.</summary>
    public static Vector2 Find(World world, RewardSpotDef def) => Find(world, def, out _);

    /// <summary><see cref="Find(World, RewardSpotDef)"/> + 자리를 찾은 문턱(<see cref="Row"/> 에 넘김 — v0.8.2 검증 반영).</summary>
    public static Vector2 Find(World world, RewardSpotDef def, out float threshold) =>
        Find(world.Nav, world.Hero.Pos, world.Hero.Facing, world.Hero.Radius, def, out threshold);

    /// <summary>보상 자리(월드 px). <paramref name="facing"/> 이 0 이면 남(+y)을 바라본다고 본다.</summary>
    public static Vector2 Find(INavWorld? nav, Vector2 feet, Vector2 facing, float heroRadius, RewardSpotDef def) =>
        Find(nav, feet, facing, heroRadius, def, out _);

    /// <summary>
    /// 보상 자리 + 그 자리를 찾은 clearance 문턱(<paramref name="threshold"/> — 첫 차례면 <c>def.Clearance</c>, 두 번째 차례·주인공 발이면 <paramref name="heroRadius"/>).
    /// </summary>
    public static Vector2 Find(INavWorld? nav, Vector2 feet, Vector2 facing, float heroRadius, RewardSpotDef def, out float threshold)
    {
        ArgumentNullException.ThrowIfNull(def);
        var forward = Forward(facing);
        foreach (var t in new[] { def.Clearance, heroRadius })
        {
            threshold = t;
            var bestDir = Vector2.Zero;
            var bestReach = 0f;
            foreach (var turn in TurnDegrees)
            {
                var dir = Rotate(forward, turn);
                var reach = Walk(nav, feet, dir, def.Distance, t);
                if (reach >= def.Distance) return feet + dir * def.Distance;
                if (reach > bestReach) { bestReach = reach; bestDir = dir; }
            }
            if (bestReach > 0f) return feet + bestDir * bestReach;
        }
        threshold = heroRadius;
        return feet;
    }

    /// <summary>
    /// 여럿 놓을 자리: <paramref name="spot"/> 에서 바라보는 방향의 수직으로 <c>spacing</c> 간격 한 줄(가운데 맞춤, 순서 = 넘긴 순서 — 보상 → 상자).
    /// 줄의 자리가 맵 안·<c>Clearance ≥ 문턱</c> 을 못 지키면 <paramref name="spot"/> 쪽으로 <see cref="Step"/> px 씩 당긴다(끝까지 못 지키면 <paramref name="spot"/>).
    /// 문턱 = <paramref name="threshold"/>(<see cref="Find(World, RewardSpotDef, out float)"/> 가 자리를 찾은 문턱 — v0.8.2: 좁은 통로에서 줄이 한 점에 겹치지 않게), 없으면 <c>def.Clearance</c>.
    /// </summary>
    public static Vector2[] Row(INavWorld? nav, Vector2 spot, Vector2 facing, int count, RewardSpotDef def, float? threshold = null)
    {
        ArgumentNullException.ThrowIfNull(def);
        if (count <= 0) return [];
        var forward = Forward(facing);
        var side = new Vector2(-forward.Y, forward.X);
        var clearance = threshold ?? def.Clearance;
        var result = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var wish = spot + side * ((i - (count - 1) * 0.5f) * def.Spacing);
            result[i] = Pull(nav, spot, wish, clearance);
        }
        return result;
    }

    /// <summary>바라보는 방향(정규화). 0 이면 남(+y).</summary>
    private static Vector2 Forward(Vector2 facing) =>
        facing.LengthSquared() > 1e-8f ? Vector2.Normalize(facing) : new Vector2(0f, 1f);

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        if (degrees == 0f) return v;
        if (degrees == 180f) return -v;
        var r = degrees * (MathF.PI / 180f);
        var (s, c) = MathF.SinCos(r);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    /// <summary>발에서 dir 로 걸을 수 있는 거리(<see cref="Step"/> 씩, 마지막 걸음은 distance 에 맞춤). 첫 걸음도 못 가면 0.</summary>
    private static float Walk(INavWorld? nav, Vector2 feet, Vector2 dir, float distance, float threshold)
    {
        if (nav is null) return distance;
        var reached = 0f;
        for (var d = Step; ; d += Step)
        {
            var at = MathF.Min(d, distance);
            var p = feet + dir * at;
            if (!nav.IsInsideMap(p) || nav.Clearance(p) < threshold) return reached;
            reached = at;
            if (at >= distance) return reached;
        }
    }

    private static Vector2 Pull(INavWorld? nav, Vector2 spot, Vector2 wish, float clearance)
    {
        if (nav is null) return wish;
        var toSpot = spot - wish;
        var dist = toSpot.Length();
        if (dist < 1e-4f) return spot;
        var dir = toSpot / dist;
        for (var pulled = 0f; pulled < dist; pulled += Step)
        {
            var p = wish + dir * pulled;
            if (nav.IsInsideMap(p) && nav.Clearance(p) >= clearance) return p;
        }
        return spot;
    }
}
