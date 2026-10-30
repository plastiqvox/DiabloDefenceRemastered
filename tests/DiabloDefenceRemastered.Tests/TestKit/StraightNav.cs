namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 장애물 없는 직선 내비 (WP1 TestKit, <see cref="INavWorld"/>). 벽은 맵 경계뿐이다 — 기둥·둘레 벽 칸은 보지 않는다(실제 충돌은 WP2).
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
}
