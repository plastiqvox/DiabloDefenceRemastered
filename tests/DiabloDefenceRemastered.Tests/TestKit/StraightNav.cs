namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 장애물 없는 직선 내비 (WP1 TestKit, WP1d 갱신, <see cref="INavWorld"/>). 벽은 맵 경계뿐이다 — 기둥·둘레 벽 칸은 보지 않는다(실제 충돌은 WP2).
/// 길 길이(<see cref="PathLength"/>·<see cref="PathLengthToHero"/>)는 트인 선이면 직선 거리, 아니면 +∞.
/// 경계는 맵이 있으면 바깥 마름모(4.1), 맵 없는 시험 월드면 직사각형 [0, MapSize).
/// Clearance = 경계까지 부호 있는 거리(밖이면 음수). 두 경계 모두 볼록이라 Clearance 는 오목하고, 선분의 최솟값은 양 끝에 있다.
/// </summary>
public sealed class StraightNav : INavWorld
{
    private readonly IsoMath? _iso;

    public StraightNav(Vector2 mapSize) => MapSize = mapSize;

    /// <summary>등각 맵: 경계 = 바깥 마름모.</summary>
    public StraightNav(IsoMath iso)
    {
        _iso = iso;
        MapSize = iso.BoxSize;
    }

    public Vector2 MapSize { get; }

    public float Clearance(Vector2 p) => _iso is { } iso
        ? iso.SignedDistanceToBoundary(p)
        : MathF.Min(MathF.Min(p.X, MapSize.X - p.X), MathF.Min(p.Y, MapSize.Y - p.Y));

    public bool HasClearLine(Vector2 a, Vector2 b, float radius) => Clearance(a) >= radius && Clearance(b) >= radius;

    public bool ProjectileLineClear(Vector2 a, Vector2 b) => true;

    public bool IsInsideMap(Vector2 p) => _iso is { } iso
        ? iso.IsInsideMap(p)
        : p.X >= 0 && p.Y >= 0 && p.X < MapSize.X && p.Y < MapSize.Y;

    /// <summary>주인공 위치(<see cref="PathLengthToHero"/> 용, v0.6.1). <c>TestWorldBuilder</c> 가 월드의 주인공으로 잇는다.</summary>
    public Func<Vector2> HeroPos { get; set; } = () => Vector2.Zero;

    /// <summary>트인 선이면 직선 거리, 아니면 +∞(v0.6 — 실제 A* 길이는 WP2). maxLength 를 넘어도 +∞.</summary>
    public float PathLength(Vector2 from, Vector2 to, float radius, float maxLength)
    {
        if (!HasClearLine(from, to, radius)) return float.PositiveInfinity;
        var d = Vector2.Distance(from, to);
        return d <= maxLength ? d : float.PositiveInfinity;
    }

    public float PathLengthToHero(Vector2 from, float radius, float maxLength) => PathLength(from, HeroPos(), radius, maxLength);

    /// <summary>트인 선이면 목표가 다음 꺾임점, 아니면 길 없음.</summary>
    public bool TryPathStep(Vector2 from, Vector2 to, float radius, out Vector2 next)
    {
        var clear = HasClearLine(from, to, radius);
        next = clear ? to : from;
        return clear;
    }
}
