namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 내비 문턱·격자 상수 한 곳 (설계서 4.2·4.3·6.14, 소유 WP2). 플로우 필드·길 검사(13.5-5)·생성 위치 검사가 같이 읽는다.
/// </summary>
public static class NavConst
{
    /// <summary>내비 격자 칸(px, 4.2). 외곽 상자 전체를 덮는다.</summary>
    public const int CellSize = 16;
    /// <summary>장애물 색인 칸(px, 4.5 <c>ObstacleIndex</c>).</summary>
    public const int ObstacleCellSize = 64;
    /// <summary>공간 해시 칸(px, 4.8).</summary>
    public const int SpatialCellSize = 32;

    /// <summary>플로우 필드 <c>S</c> 의 통과 문턱(clearance ≥ 16) — 몸 반경 ≤ 16.</summary>
    public const float FieldS = 16f;
    /// <summary>플로우 필드 <c>M</c> 의 통과 문턱(clearance ≥ 26) — 몸 반경 16 초과.</summary>
    public const float FieldM = 26f;
    /// <summary>필드 등급 수(0 = S, 1 = M — <c>INavDebugView.IsBlocked</c> 의 sizeClass).</summary>
    public const int FieldCount = 2;

    /// <summary>13.5-5 주인공 반경(직업 radius ≤ 14 는 13.5-21 이 지킨다).</summary>
    public const float HeroCheckRadius = 14f;
    /// <summary>13.5-5 가장 큰 적 반경(입구 진입점 → center·room_*).</summary>
    public const float LargeCheckRadius = 34f;
    /// <summary>13.5-6 입구 진입점·시작 위치의 clearance 하한.</summary>
    public const float EntryClearance = 38f;
    /// <summary>13.5-6 입구 진입점에서 둘레가 아닌 걷기 막힘 도형까지(초과해야 함).</summary>
    public const float EntryInnerDistance = 60f;
    /// <summary>13.5-4 도형 묶음끼리·경계와의 거리 하한(붙어 있으면 됨).</summary>
    public const float ShapeGap = 80f;

    /// <summary>플로우 필드 비용(직교 10, 대각선 14, 4.3). 65535 = 못 감.</summary>
    public const int StraightCost = 10, DiagonalCost = 14;
    public const ushort Unreachable = ushort.MaxValue;
    /// <summary>필드 시작점 반경 = 주인공 반경 + 이 값(4.3).</summary>
    public const float FieldGoalMargin = 16f;
    /// <summary>필드 갱신 주기(틱): S 는 <c>Tick % 6 == 0</c>, M 은 <c>== 3</c>.</summary>
    public const int FieldPeriod = 6;

    /// <summary>"가까우면 직진" 거리(px, 4.3).</summary>
    public const float DirectRange = 200f;
    /// <summary>A* 요청 처리 수(틱당, 4.4).</summary>
    public const int PathRequestsPerTick = 2;
    /// <summary>A* 펼침 수 상한(한 번 탐색).</summary>
    public const int PathExpandLimit = 20000;
    /// <summary>경유점 도착 거리(px).</summary>
    public const float WaypointReach = 8f;
    /// <summary>A* 재계획: 시간(s)·목표 움직임(px).</summary>
    public const float ReplanSeconds = 0.5f, ReplanGoalMove = 32f;
    /// <summary>A* 시작 칸이 막혔을 때 찾는 반경(칸).</summary>
    public const int StartSearchCells = 3;

    /// <summary>길 트임 걸음(px, 4.5).</summary>
    public const float LineStep = 4f;
}
