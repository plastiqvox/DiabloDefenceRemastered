namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 장애물 없는 직선 내비 (WP1 TestKit, <see cref="INavWorld"/>). 벽은 맵 경계뿐이다.
/// Clearance = 맵 경계까지 거리(밖이면 음수). 직사각형 안에서 Clearance 는 오목하므로 선분의 최솟값은 양 끝에 있다.
/// </summary>
public sealed class StraightNav(Vector2 mapSize) : INavWorld
{
    public Vector2 MapSize { get; } = mapSize;

    public float Clearance(Vector2 p) =>
        MathF.Min(MathF.Min(p.X, MapSize.X - p.X), MathF.Min(p.Y, MapSize.Y - p.Y));

    public bool HasClearLine(Vector2 a, Vector2 b, float radius) => Clearance(a) >= radius && Clearance(b) >= radius;

    public bool ProjectileLineClear(Vector2 a, Vector2 b) => true;

    public bool IsInsideMap(Vector2 p) => p.X >= 0 && p.Y >= 0 && p.X < MapSize.X && p.Y < MapSize.Y;
}
