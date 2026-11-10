namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 충돌 도형 하나 = 타일 공간 직사각형(막힌 하위 칸 묶음)을 등각 변환한 평행사변형(볼록 다각형, 설계서 4.2).
/// </summary>
/// <param name="Corners">월드 px 꼭짓점 4개(시계 방향).</param>
/// <param name="Tiles">타일 공간 직사각형 [tx0,ty0,tx1,ty1)(끝 미포함).</param>
/// <param name="IsBoundary">테두리·문 끝 조각에서 온 도형. 장애물 통과 유닛(<c>phase_through</c>)도 막힌다(4.7-3).</param>
public sealed record CollisionShape(IReadOnlyList<Vector2> Corners, RectI Tiles, bool BlocksWalk, bool BlocksProjectiles, bool IsBoundary);

/// <summary>
/// 막힌 하위 칸 → 충돌 도형 (설계서 4.2, 소유 WP2). 막힘 종류마다(걷기: W·X, 투사체: M·X) 타일 공간에서 탐욕 직사각형으로 묶고(행 우선, 결정적)
/// 등각 변환으로 평행사변형을 만든다. 같은 목록에서 충돌 해소·clearance·투사체 막음·<c>--debug-nav shapes</c> 를 만든다.
/// 칸 성질·둘레 표시·좌표 변환은 <see cref="ArenaGrid"/>(<c>ArenaGrid.Iso</c>, <c>ArenaGrid.Tileset</c>)가 함께 가진다.
/// 묶는 단위는 칸 종류(걷기 막힘·투사체 막힘·둘레 세 비트가 같은 칸)다 — 그래서 <c>X</c> 칸 도형은 둘 다 막고, <c>w</c>·<c>W</c> 는 걷기만, <c>m</c>·<c>M</c> 은 투사체만 막으며,
/// 둘레 칸과 안쪽 칸이 한 도형에 섞이지 않는다(<c>IsBoundary</c> 가 칸 그대로). 시야 비트는 보지 않는다(4.2 끝 — 시야는 <c>VisibilityField</c> 가 칸으로 읽음).
/// </summary>
public static class ArenaCollision
{
    public static IReadOnlyList<CollisionShape> Build(ArenaGrid arena)
    {
        var n = arena.Size;
        var iso = arena.Iso;
        var kind = new byte[n * n];
        for (var ty = 0; ty < n; ty++)
            for (var tx = 0; tx < n; tx++)
                kind[ty * n + tx] = KindOf(arena.Flags(tx, ty));

        var shapes = new List<CollisionShape>();
        var used = new bool[n * n];
        // 탐욕 직사각형(행 우선): 첫 빈 칸에서 가로로 같은 종류를 늘리고, 그 폭 전체가 같은 종류인 줄만큼 아래로 늘린다
        for (var ty = 0; ty < n; ty++)
            for (var tx = 0; tx < n; tx++)
            {
                var i = ty * n + tx;
                var k = kind[i];
                if (k == 0 || used[i]) continue;
                var x1 = tx + 1;
                while (x1 < n && kind[ty * n + x1] == k && !used[ty * n + x1]) x1++;
                var y1 = ty + 1;
                while (y1 < n && RowMatches(kind, used, n, y1, tx, x1, k)) y1++;
                for (var y = ty; y < y1; y++)
                    for (var x = tx; x < x1; x++) used[y * n + x] = true;
                var rect = new RectI(tx, ty, x1, y1);
                shapes.Add(new CollisionShape(Corners(iso, rect), rect, (k & 1) != 0, (k & 2) != 0, (k & 4) != 0));
            }
        return shapes;
    }

    /// <summary>타일 공간 직사각형 [tx0,ty0,tx1,ty1) → 화면 평행사변형(위·오른·아래·왼 — 화면에서 시계 방향).</summary>
    public static Vector2[] Corners(IsoMath iso, RectI tiles) =>
    [
        iso.ToWorld(new TileCoord(tiles.X0 - 0.5f, tiles.Y0 - 0.5f)),
        iso.ToWorld(new TileCoord(tiles.X1 - 0.5f, tiles.Y0 - 0.5f)),
        iso.ToWorld(new TileCoord(tiles.X1 - 0.5f, tiles.Y1 - 0.5f)),
        iso.ToWorld(new TileCoord(tiles.X0 - 0.5f, tiles.Y1 - 0.5f)),
    ];

    // 1 = 걷기, 2 = 투사체, 4 = 둘레(막힌 칸만). 0 = 도형 없음
    private static byte KindOf(CellFlags f)
    {
        var k = 0;
        if ((f & CellFlags.BlocksWalk) != 0) k |= 1;
        if ((f & CellFlags.BlocksProjectiles) != 0) k |= 2;
        if (k != 0 && (f & CellFlags.Boundary) != 0) k |= 4;
        return (byte)k;
    }

    private static bool RowMatches(byte[] kind, bool[] used, int n, int y, int x0, int x1, byte k)
    {
        for (var x = x0; x < x1; x++)
            if (kind[y * n + x] != k || used[y * n + x]) return false;
        return true;
    }
}

/// <summary>
/// <c>--debug-nav</c> 가 읽는 내비 내부(소유 WP2). WP2 의 <c>INavWorld</c> 구현이 함께 구현하고,
/// WP5 <c>DebugNavOverlay</c> 는 <c>world.Nav as INavDebugView</c> 로 얻는다(시험용 StraightNav 는 구현하지 않아도 된다).
/// </summary>
public interface INavDebugView
{
    /// <summary>충돌 도형(<see cref="ArenaCollision.Build"/> 결과).</summary>
    IReadOnlyList<CollisionShape> Collision { get; }

    /// <summary>하위 칸 성질(<c>W</c>/<c>M</c>/<c>X</c> 겹침, <c>--debug-nav tiles</c>). 맵 없는 시험 월드(상자 내비)에서는 null(v0.9 WP1g — WP2 요청).</summary>
    ArenaGrid? Arena { get; }

    /// <summary>내비 격자 칸 크기(px, 16).</summary>
    int CellSize { get; }
    /// <summary>격자 칸 수(가로, 세로) — 외곽 상자 전체.</summary>
    Int2 GridSize { get; }

    /// <summary>칸 중심의 부호 있는 여유 거리(px).</summary>
    float CellClearance(int cx, int cy);

    /// <summary>몸 크기 등급(0 = S, 1 = M)에서 막힌 칸인가.</summary>
    bool IsBlocked(int sizeClass, int cx, int cy);

    /// <summary>플로우 필드 방향(단위 벡터, 갈 곳이 없으면 0).</summary>
    Vector2 FlowDirection(int sizeClass, int cx, int cy);

    /// <summary>그 적의 A* 원 경로와 다듬은 경로를 채운다(목록은 비우고 채움). 경로가 없으면 false.</summary>
    bool TryGetPath(EntityId enemy, List<Vector2> raw, List<Vector2> smoothed);
}
