namespace DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

// 탄막 동작 barrage (설계서 6.20 ①, 기획 9.5 "보스전 = 탄막 액션"·38.1, 13.3). 고리·나선·노린 연사·빈틈 벽·속도 섞기·바닥액이 모두 이 동작 하나의 쓰임이다.

/// <summary>탄막 모양: <c>ring</c> 사방 등간격 · <c>fan</c> <c>arc</c> 안 등간격(1발이면 가운데) · <c>wall</c> 주인공 앞에서 밀려오는 줄.</summary>
public enum BarrageShape : byte { Ring, Fan, Wall }

/// <summary>기준 방향: <c>hero</c> 주인공 쪽 · <c>facing</c> 바라보는 쪽 · <c>fixed</c> 화면 남(0°)에서 <c>angle</c>.</summary>
public enum BarrageAim : byte { Hero, Facing, Fixed }

/// <summary>빈틈 벽 <c>{ "distance", "width", "spacing", "gap", "gap_range", "gap_shift" }</c>.</summary>
public sealed record BarrageWallParams
{
    /// <summary>주인공에서 보스 쪽으로 줄까지(px).</summary>
    public float Distance { get; init; } = 260f;
    public float Width { get; init; } = 560f;
    public float Spacing { get; init; } = 24f;
    /// <summary>빈 탄 수(빈틈 가운데에 가장 가까운 탄부터).</summary>
    public int Gap { get; init; } = 3;
    /// <summary>첫 빈틈 가운데의 가로 범위(±px).</summary>
    public float GapRange { get; init; } = 160f;
    /// <summary>다음 파도 빈틈 이동 한도(±px).</summary>
    public float GapShift { get; init; } = 100f;
}

public sealed record BarrageParams
{
    public BarrageShape Shape { get; init; }
    public required DefRef<ProjectileDef> Projectile { get; init; }
    /// <summary>파도 하나의 발 수(<c>ring</c>·<c>fan</c>). <c>wall</c> 은 폭/간격이 정한다.</summary>
    public int Count { get; init; } = 1;
    /// <summary><c>fan</c> 의 전체 퍼짐각(도).</summary>
    public float Arc { get; init; }
    /// <summary>0 이면 정의 속도.</summary>
    public float Speed { get; init; }
    /// <summary>기본 피해(null = 그 적의 damage).</summary>
    public float? Damage { get; init; }
    public BarrageAim Aim { get; init; } = BarrageAim.Hero;
    /// <summary>기준에 더하는 각(도).</summary>
    public float Angle { get; init; }
    /// <summary>기준에 더하는 각(도) — "빈틈 조준"(고리16 = 11.25) 용 이름.</summary>
    public float Offset { get; init; }
    public int Waves { get; init; } = 1;
    /// <summary>파도 간격(초).</summary>
    public float Interval { get; init; }
    /// <summary>파도 k 에 더하는 각 = k × turn(도, 나선).</summary>
    public float Turn { get; init; }
    /// <summary>참이면 파도마다 그 틱의 주인공 쪽(노린 연사).</summary>
    public bool Reaim { get; init; }
    /// <summary><c>[a, b]</c> — <c>fan</c> 의 i번째 탄 사거리 = 주인공까지 × (a + (b − a) × i/(n − 1)), [40, 정의 사거리]로 자름(바닥액).</summary>
    public float[]? RangeToHero { get; init; }
    public BarrageWallParams? Wall { get; init; }
}

/// <summary>
/// 탄막(6.20 ①). 파도 k(0부터)는 <c>ActionState.BeginTick + round(k × interval / Dt)</c> 틱에 나간다 — 상태를 들고 다니지 않으므로 한 패턴의 여러 <c>barrage</c> 가
/// 같은 <see cref="ActionState"/> 로 함께 돈다. <see cref="Begin"/> 이 그 틱까지 차례가 된 파도(파도 0 포함), <see cref="Tick"/> 이 그 틱이 차례인 파도를 낸다(한 틱에 둘 이상이면 차례로).
/// 각 = 기준(<c>reaim</c> 이면 그 파도 틱의 주인공 쪽, 아니면 <c>ActionState.Dir</c>) + <c>angle</c> + <c>offset</c> + k × <c>turn</c>.
/// 고리·부채는 보스 발 + 방향 × 몸 반경에서, 벽은 주인공(첫 파도 틱, <c>ActionState.From</c>) − 기준 × distance 를 가운데로 한 수직 줄에서 나온다.
/// 벽 빈틈: 파도 0 = <c>World.Rng.Ai</c> 로 [−gap_range, gap_range], 다음은 앞 빈틈(<c>ActionState.Point.X</c>) ± gap_shift 를 그 범위로 자름. 맵 밖·길 막힌 벽 탄은 빠진다.
/// 어느 모양이든 주인공 발에서 <c>rules.enemies.barrage.spawn_clear</c> 안의 탄은 빠진다(<see cref="EnemyShots.FireClear"/>).
/// </summary>
public sealed class Barrage(BarrageParams p) : IEnemyAction, IDamageInfo
{
    /// <summary>바닥액 사거리 하한(px, 기획 9.4).</summary>
    public const float MinRange = 40f;

    public BarrageParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;
    public bool MultiTick => p.Waves > 1;

    /// <summary>파도 k 의 틱 오프셋(BeginTick 부터).</summary>
    public int WaveOffset(int k) => (int)MathF.Round(k * p.Interval / World.Dt, MidpointRounding.AwayFromZero);

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        if (p.Shape == BarrageShape.Wall) action.From = ctx.HeroPos;
        for (var k = 0; k < Math.Max(1, p.Waves); k++)
            if (WaveOffset(k) <= 0 || k == 0) Wave(ctx, ref action, k);
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref ActionState action)
    {
        var since = ctx.Tick - action.BeginTick;
        var waves = Math.Max(1, p.Waves);
        for (var k = 1; k < waves; k++)
        {
            var at = WaveOffset(k);
            if (at > 0 && at == since) Wave(ctx, ref action, k);
        }
        return since >= WaveOffset(waves - 1) ? ActionStatus.Done : ActionStatus.Running;
    }

    private void Wave(in EnemyCtx ctx, ref ActionState action, int k)
    {
        // 시야 조건 패턴(블랙애시 연사·로트피스트 바닥액 — clear_sight)은 파도마다 다시 본다: 파도 0 은 이벤트 프레임이 이미 봤고, 뒤 파도는 안 보이면 그 파도만 버린다(6.6 "쏘는 순간" 규칙)
        if (k > 0 && action.SightGated && !ctx.SeenClearly) return;
        var self = ctx.Self;
        var def = p.Projectile.Def;
        var damage = ctx.FinalDamage(p.Damage);
        var baseDir = p.Aim switch
        {
            BarrageAim.Facing => self.Facing,
            BarrageAim.Fixed => new Vector2(0f, 1f),
            _ => p.Reaim ? ctx.DirToHero : MathX.NormalizeOr(action.Dir, ctx.DirToHero),
        };
        var angle = MathF.Atan2(baseDir.Y, baseDir.X) + (p.Angle + p.Offset + k * p.Turn) * MathX.DegToRad;
        var fired = 0;
        switch (p.Shape)
        {
            case BarrageShape.Ring:
            {
                var n = Math.Max(1, p.Count);
                for (var i = 0; i < n; i++)
                {
                    var dir = MathX.FromAngle(angle + MathF.Tau * i / n);
                    fired += EnemyShots.FireClear(ctx, def, self.Pos + dir * self.Radius, dir, damage, action.SightGated, p.Speed) ? 1 : 0;
                }
                break;
            }
            case BarrageShape.Fan:
            {
                var n = Math.Max(1, p.Count);
                var arc = p.Arc * MathX.DegToRad;
                var dist = ctx.DistToHero;
                for (var i = 0; i < n; i++)
                {
                    var a = n == 1 ? angle : angle - arc * 0.5f + arc * i / (n - 1);
                    var dir = MathX.FromAngle(a);
                    var range = 0f;
                    if (p.RangeToHero is { Length: 2 } rh)
                    {
                        var mul = n == 1 ? (rh[0] + rh[1]) * 0.5f : rh[0] + (rh[1] - rh[0]) * i / (n - 1);
                        range = Math.Clamp(dist * mul, MinRange, def.Range ?? float.MaxValue);
                    }
                    fired += EnemyShots.FireClear(ctx, def, self.Pos + dir * self.Radius, dir, damage, action.SightGated, p.Speed, range) ? 1 : 0;
                }
                break;
            }
            case BarrageShape.Wall when p.Wall is { } wall:
                fired = WallWave(ctx, ref action, k, wall, MathX.FromAngle(angle), def, damage);
                break;
        }
        if (fired > 0) EnemyShots.Cue(ctx, def);
    }

    /// <summary>벽 한 파도: 가운데 C = 주인공(첫 파도 틱) − 기준 × distance, 수직 줄에 spacing 간격, 빈틈 가운데에 가까운 gap 발을 뺌.</summary>
    private int WallWave(in EnemyCtx ctx, ref ActionState action, int k, BarrageWallParams wall, Vector2 dir, ProjectileDef def, int damage)
    {
        var rng = ctx.Rng;
        var gap = k == 0
            ? rng.Range(-wall.GapRange, wall.GapRange)
            : Math.Clamp(action.Point.X + rng.Range(-wall.GapShift, wall.GapShift), -wall.GapRange, wall.GapRange);
        action.Point = new Vector2(gap, action.Point.Y);
        var center = action.From - dir * wall.Distance;
        var perp = new Vector2(-dir.Y, dir.X);
        var spacing = MathF.Max(1f, wall.Spacing);
        var count = (int)MathF.Floor(wall.Width / spacing) + 1;
        var half = (count - 1) * spacing * 0.5f;
        // 빈틈 가운데에 가장 가까운 gap 발: 가로 자리 j × spacing − half 와 gap 의 거리 순(같으면 작은 j)
        var skipFrom = (int)MathF.Round((gap + half) / spacing - (wall.Gap - 1) * 0.5f, MidpointRounding.AwayFromZero);
        skipFrom = Math.Clamp(skipFrom, 0, Math.Max(0, count - wall.Gap));
        var nav = ctx.World.Nav;
        var fired = 0;
        for (var j = 0; j < count; j++)
        {
            if (j >= skipFrom && j < skipFrom + wall.Gap) continue;
            var pos = center + perp * (j * spacing - half);
            if (nav is not null && (!nav.IsInsideMap(pos) || !nav.ProjectileLineClear(center, pos))) continue;
            fired += EnemyShots.FireClear(ctx, def, pos, dir, damage, action.SightGated, p.Speed, 0f, cue: false) ? 1 : 0;
        }
        return fired;
    }
}
