namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 풀린 장애물 하나(설계서 4.2 "도형 하나가 정본"). 기준점(<c>circle_at</c>)은 좌표로 풀려 있다.
/// </summary>
/// <param name="Id">맵 collision 또는 스테이지 장애물 ID.</param>
/// <param name="Kind">스테이지 장애물의 종류. 맵 collision(배경 그림에 이미 있음)은 null → 굽지 않는다.</param>
/// <param name="Shapes">월드 좌표 모양(<c>CircleAt</c> 없음). 보통 하나, 아치는 기둥 여럿.</param>
/// <param name="Height">종류 높이 또는 배치의 덮어쓰기. 맵 collision 은 0.</param>
public sealed record ResolvedObstacle(string Id, ObstacleKindDef? Kind, IReadOnlyList<ShapeDef> Shapes, float Height,
                                      LintelDef? Lintel, bool BlocksProjectiles);

/// <summary>
/// 맵 + 스테이지 장애물을 푼 목록 (소유 WP2). 충돌 도형·내비 격자(WP2)·장애물 굽기(WP5)·<c>--debug-nav</c> 가 같은 목록을 읽는다.
/// WP1 이 시그니처만 만든 빈 연결 파일 — WP5 가 먼저 부르고 컴파일한다. 본문은 WP2 가 채운다.
/// </summary>
/// <param name="Map">맵 <c>collision</c>(그리지 않음).</param>
/// <param name="Stage">스테이지 <c>obstacles</c>(정의 순서). 굽기 대상은 <c>Kind.Draw</c> 인 것.</param>
public sealed record ResolvedObstacles(IReadOnlyList<ResolvedObstacle> Map, IReadOnlyList<ResolvedObstacle> Stage)
{
    public static ResolvedObstacles Resolve(MapDef map, StageDef? stage)
        => throw new NotImplementedException("WP2 ResolvedObstacles.Resolve");
}

/// <summary>
/// <c>--debug-nav</c> 가 읽는 내비 내부(소유 WP2). WP2 의 <c>INavWorld</c> 구현이 함께 구현하고,
/// WP5 <c>DebugNavOverlay</c> 는 <c>world.Nav as INavDebugView</c> 로 얻는다(시험용 StraightNav 는 구현하지 않아도 된다).
/// </summary>
public interface INavDebugView
{
    ResolvedObstacles Obstacles { get; }

    /// <summary>내비 격자 칸 크기(px, 16).</summary>
    int CellSize { get; }
    /// <summary>격자 칸 수(가로, 세로).</summary>
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
