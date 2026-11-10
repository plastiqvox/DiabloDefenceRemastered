using System.Runtime.CompilerServices;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>A* 경유점 16칸(설계서 4.4 — 넘치면 앞 16개 + 끝에서 재계획).</summary>
[InlineArray(Capacity)]
public struct NavPathPoints
{
    public const int Capacity = 16;
    private Vector2 _e0;
}

/// <summary>
/// 적마다 <c>Navigator</c> 가 쓰는 상태 (설계서 4.3·4.4, 소유 WP2): 길 트임 캐시, 끼임 타이머·비켜 가기, 경로 경유점·번호·재계획 시각.
/// <c>default</c> = 상태 없음.
/// </summary>
public struct NavAgentState
{
    /// <summary>길 트임 캐시(주인공 또는 A* 목표까지, 자기 반경). 6틱에 한 번(<c>Slot % 6</c> 분산).</summary>
    public bool ClearLine;
    public bool ClearLineValid;

    /// <summary>끼임 감지: 쫓는 시간 누적·그 시작 자리.</summary>
    public float StuckTime;
    public Vector2 StuckAnchor;
    /// <summary>비켜 가기 남은 시간(s)·방향(+1 왼쪽, −1 오른쪽).</summary>
    public float SidestepTime;
    public sbyte SidestepSign;

    /// <summary>다듬은 경로(첫 점 = 계획 때 자리). <see cref="PointIndex"/> = 지금 향하는 경유점.</summary>
    public NavPathPoints Points;
    public byte PointCount;
    public byte PointIndex;
    /// <summary>경로가 16개를 넘어 잘렸다(끝에 닿으면 재계획).</summary>
    public bool PathTruncated;
    /// <summary>경로를 계획한 목표·지난 시간(s).</summary>
    public Vector2 PathGoal;
    public float PathAge;
    /// <summary>요청이 줄에 있다.</summary>
    public bool PathRequested;
    /// <summary>시작 3칸 안에 통과 칸이 없어 기울기로 움직이는 남은 시간(s, 4.4 — 0.25s 뒤 다시 요청).</summary>
    public float EscapeTime;

    /// <summary>이번 틱 희망 속도(막힘 판정 — <c>Enemy.MoveBlockedTime</c>).</summary>
    public Vector2 Desired;

    /// <summary>불변식 계측(10.1): <c>stuck</c> 기준 자리·그 뒤 표본 수, <c>in_obstacle</c> 이 이어진 표본 수.</summary>
    public Vector2 CheckAnchor;
    public byte CheckSamples;
    public byte ObstacleRun;
}

public sealed partial class Enemy
{
    /// <summary>내비 상태(WP2 — <c>Navigator</c> 만 쓴다).</summary>
    public NavAgentState Nav;

    partial void ResetNav() => Nav = default;
}
