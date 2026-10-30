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
/// WP1b 가 시그니처만 만든 빈 연결 파일 — WP5(디버그 그리기)가 먼저 부르고 컴파일한다. 본문은 WP2 가 채운다.
/// </summary>
public static class ArenaCollision
{
    public static IReadOnlyList<CollisionShape> Build(ArenaGrid arena)
        => throw new NotImplementedException("WP2 ArenaCollision.Build");
}

/// <summary>
/// <c>--debug-nav</c> 가 읽는 내비 내부(소유 WP2). WP2 의 <c>INavWorld</c> 구현이 함께 구현하고,
/// WP5 <c>DebugNavOverlay</c> 는 <c>world.Nav as INavDebugView</c> 로 얻는다(시험용 StraightNav 는 구현하지 않아도 된다).
/// </summary>
public interface INavDebugView
{
    /// <summary>충돌 도형(<see cref="ArenaCollision.Build"/> 결과).</summary>
    IReadOnlyList<CollisionShape> Collision { get; }

    /// <summary>하위 칸 성질(<c>W</c>/<c>M</c>/<c>X</c> 겹침, <c>--debug-nav tiles</c>).</summary>
    ArenaGrid Arena { get; }

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
