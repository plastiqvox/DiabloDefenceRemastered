namespace DiabloDefenceRemastered.Core.Sim.Weapons;

/// <summary>조준 결과. <see cref="Target"/> = 적을 골랐다, <see cref="Fallback"/> = 대상이 없어 대체 자리(바라보는 방향·주인공 둘레), <see cref="None"/> = 조준 없음.</summary>
public enum AimResult : byte { Target, Fallback, None }

/// <summary>
/// 조준 (설계서 5.3, 기획 5.1 표 — WP3 소유 형). <c>nearest</c>·<c>random_in_radius</c>·<c>facing</c>·<c>densest</c>·<c>none</c>.
/// 시야를 보지 않는다(기획 5.1 — 시야 밖·벽 너머 적도 고른다). 후보는 <c>ISpatialQuery</c>(조준 가능한 적 — 입장·등장 중 제외)뿐이다.
/// </summary>
public interface ITargeting
{
    string Id { get; }
    float Radius { get; }

    /// <summary>
    /// <paramref name="shotIndex"/> 번째 발의 겨눌 자리. 대상이 없으면 <see cref="AimResult.Fallback"/>(규칙 표의 "대상이 없을 때") 또는 <see cref="AimResult.None"/>.
    /// 유도형(<c>homing</c>)은 Fallback 이면 쏘지 않는다 — 판단은 패턴이 한다.
    /// </summary>
    AimResult Aim(World world, Vector2 origin, int shotIndex, out Vector2 point, out EntityId target);
}

/// <summary>조준 부품 매개변수(<c>{ "type": "nearest", "radius": 450 }</c>). 반경이 없으면 450(기획 5.1 기본).</summary>
public sealed record TargetingParams
{
    public float Radius { get; init; } = 450f;
}

/// <summary>조준 다섯 종 등록·공용 도우미.</summary>
public static class Targetings
{
    public static readonly string[] Ids = ["nearest", "random_in_radius", "facing", "densest", "none"];

    public static ITargeting Create(string id, float radius) => id switch
    {
        "nearest" => new NearestTargeting(radius),
        "random_in_radius" => new RandomTargeting(radius),
        "facing" => new FacingTargeting(radius),
        "densest" => new DensestTargeting(radius),
        "none" => new NoTargeting(),
        _ => throw new ArgumentException($"모르는 조준 '{id}'", nameof(id)),
    };

    /// <summary>조준 부품 원소(시험·코드로 정의를 만들 때).</summary>
    public static PartDef Part(string id, float radius = 450f) =>
        new(PartKind.Targeting, id, null, null, new TargetingParams { Radius = radius }, Create(id, radius));

    /// <summary>바라보는 방향 반경 끝(대상이 없을 때 쏘는 자리).</summary>
    internal static Vector2 FacingPoint(World world, Vector2 origin, float radius)
    {
        var f = world.Hero.Facing;
        if (f.LengthSquared() < 1e-6f) f = new Vector2(0, 1);
        return origin + Vector2.Normalize(f) * MathF.Max(radius, 1f);
    }

    /// <summary>반경 안 조준 가능한 적 밀집 인덱스(결정적 순서). 버퍼보다 많으면 버퍼만큼.</summary>
    internal static int Candidates(World world, Vector2 origin, float radius, out int[] buffer)
    {
        buffer = world.Combat.Query;
        var n = world.Spatial.Query(origin, radius, buffer);
        return Math.Min(n, buffer.Length);
    }
}

internal sealed class NearestTargeting(float radius) : ITargeting
{
    public string Id => "nearest";
    public float Radius => radius;

    /// <summary>가까운 순으로 서로 다른 적(적이 발 수보다 적으면 다시 처음부터, 기획 5.1). 같은 거리는 밀집 인덱스 순.</summary>
    public AimResult Aim(World world, Vector2 origin, int shotIndex, out Vector2 point, out EntityId target)
    {
        var n = Targetings.Candidates(world, origin, radius, out var buf);
        if (n == 0)
        {
            point = Targetings.FacingPoint(world, origin, radius);
            target = EntityId.None;
            return AimResult.Fallback;
        }
        var enemies = world.Enemies;
        var want = shotIndex % n;
        // want+1 번째로 가까운 적: 부분 선택 정렬(n 은 작다)
        Span<int> order = n <= 256 ? stackalloc int[n] : new int[n];
        for (var i = 0; i < n; i++) order[i] = buf[i];
        for (var i = 0; i <= want; i++)
        {
            var best = i;
            var bestD = Vector2.DistanceSquared(enemies[order[i]].Pos, origin);
            for (var j = i + 1; j < n; j++)
            {
                var d = Vector2.DistanceSquared(enemies[order[j]].Pos, origin);
                if (d < bestD || (d == bestD && order[j] < order[best])) { best = j; bestD = d; }
            }
            (order[i], order[best]) = (order[best], order[i]);
        }
        var e = enemies[order[want]];
        point = e.Pos;
        target = e.Id;
        return AimResult.Target;
    }
}

internal sealed class RandomTargeting(float radius) : ITargeting
{
    public string Id => "random_in_radius";
    public float Radius => radius;

    public AimResult Aim(World world, Vector2 origin, int shotIndex, out Vector2 point, out EntityId target)
    {
        var n = Targetings.Candidates(world, origin, radius, out var buf);
        if (n == 0)
        {
            point = Targetings.FacingPoint(world, origin, radius);
            target = EntityId.None;
            return AimResult.Fallback;
        }
        var e = world.Enemies[buf[world.Rng.Weapon.Index(n)]];
        point = e.Pos;
        target = e.Id;
        return AimResult.Target;
    }
}

internal sealed class FacingTargeting(float radius) : ITargeting
{
    public string Id => "facing";
    public float Radius => radius;

    public AimResult Aim(World world, Vector2 origin, int shotIndex, out Vector2 point, out EntityId target)
    {
        point = Targetings.FacingPoint(world, origin, radius);
        target = EntityId.None;
        return AimResult.Fallback;
    }
}

/// <summary>
/// 반경 안 64px 칸 중 적이 가장 많은 칸 가운데(설계서 5.3: 32px 칸 개수를 2×2 로 합친다 — 해시를 다시 훑지 않음). 같은 수면 훑는 순서(위 → 아래, 왼 → 오른)의 첫 칸.
/// 적이 없으면 주인공 둘레 무작위(무기 난수).
/// </summary>
internal sealed class DensestTargeting(float radius) : ITargeting
{
    private const float Cell = 32f;

    public string Id => "densest";
    public float Radius => radius;

    public AimResult Aim(World world, Vector2 origin, int shotIndex, out Vector2 point, out EntityId target)
    {
        target = EntityId.None;
        var spatial = world.Spatial;
        var x0 = (int)MathF.Floor((origin.X - radius) / (2 * Cell));
        var x1 = (int)MathF.Floor((origin.X + radius) / (2 * Cell));
        var y0 = (int)MathF.Floor((origin.Y - radius) / (2 * Cell));
        var y1 = (int)MathF.Floor((origin.Y + radius) / (2 * Cell));
        var best = 0;
        var bestPoint = origin;
        var r2 = radius * radius;
        for (var cy = y0; cy <= y1; cy++)
        for (var cx = x0; cx <= x1; cx++)
        {
            var center = new Vector2((cx * 2 + 1) * Cell, (cy * 2 + 1) * Cell);
            if (Vector2.DistanceSquared(center, origin) > r2) continue;
            var c = spatial.CellCount(cx * 2, cy * 2) + spatial.CellCount(cx * 2 + 1, cy * 2)
                  + spatial.CellCount(cx * 2, cy * 2 + 1) + spatial.CellCount(cx * 2 + 1, cy * 2 + 1);
            if (c > best) { best = c; bestPoint = center; }
        }
        if (best > 0)
        {
            point = bestPoint;
            return AimResult.Target;
        }
        var rng = world.Rng.Weapon;
        var a = rng.Angle();
        var d = rng.Range(0f, radius);
        point = origin + new Vector2(MathF.Cos(a), MathF.Sin(a)) * d;
        return AimResult.Fallback;
    }
}

internal sealed class NoTargeting : ITargeting
{
    public string Id => "none";
    public float Radius => 0f;

    public AimResult Aim(World world, Vector2 origin, int shotIndex, out Vector2 point, out EntityId target)
    {
        point = origin;
        target = EntityId.None;
        return AimResult.None;
    }
}
