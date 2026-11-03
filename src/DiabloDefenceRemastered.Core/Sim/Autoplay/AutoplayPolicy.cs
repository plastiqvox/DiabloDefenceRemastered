namespace DiabloDefenceRemastered.Core.Sim.Autoplay;

/// <summary>자동 조작 이동 모드(v0.6.1 — 9절 <c>--autoplay-visit</c>, 게임 <c>AppOptions.AutoplayVisit</c>). Core 에 두어 <c>Scenario</c> 시험도 쓴다.</summary>
public enum AutoplayVisit : byte
{
    /// <summary>보통 자동 조작.</summary>
    None,
    /// <summary>이동 규칙에 더해 맵의 방 기준점(<c>room_*</c>)을 이름 순으로 차례로 들른다(기준점 쪽 가중 0.8, 반경 60 안이면 다음 방, 40초 넘으면 넘김).</summary>
    Rooms,
}

/// <summary>방 들르기 기록 하나(닿음 또는 넘김). WP6 이 새 기록마다 로그 <c>VISIT</c>·<c>VISIT_SKIP</c> 줄을 쓴다. <paramref name="MapId"/>(v0.7) = 그 방 맵(로그 <c>map=</c>).</summary>
public readonly record struct RoomVisit(string MapId, string RoomId, int StageTick, bool Skipped);

/// <summary>안쪽 방 하나의 누적(판 끝 <c>VISITS</c> 줄): 들른 수·넘긴 수·방 안에 있던 틱·그동안 받은 피해. v0.7: 키 = (맵 ID, 기준점 ID) — 판 전체에서 그 판이 쓴 맵마다.</summary>
public readonly record struct RoomVisitStats(string MapId, string RoomId, int Visits, int Skips, int TicksInside, float DamageInside);

/// <summary>자동 조작이 문 단계를 <c>rules.rooms.autoplay_door_skip</c> 안에 못 끝내 <c>CommandButtons.Exit</c> 로 나간 기록(v0.7 — 로그 <c>DOOR_SKIP</c>, 시험 기준 0).</summary>
public readonly record struct DoorSkip(int Act, int Room, int StageTick);

/// <summary>보상·문 단계가 <c>autoplay_door_skip</c> 안에 안 끝난 기록(v0.7.1 — 로그 <c>REWARD_STALL</c>, 상태는 바꾸지 않음, 시험 기준 0).</summary>
public readonly record struct RewardStall(int Act, int Room, RoomPhase Phase, int StageTick);

/// <summary>
/// 자동 조작 정책 (설계서 3.4·8.1·9절, 소유 WP4). 시험과 게임(--autoplay)이 같은 정책을 써서 같은 씨앗·같은 옵션이면 같은 판이 된다.
/// v0.6.1: 정적 클래스 → 판마다 인스턴스 하나(WP6 이 <c>new AutoplayPolicy(visit)</c>). 방 번호·다시 구할 시각 같은 상태는 인스턴스에 둔다 —
/// 시뮬레이션 상태가 아니다(해시에 들지 않음, 같은 입력이면 같은 명령). 안개를 무시한다(<c>World.Visibility</c> 를 읽지 않는다).
/// 메뉴 넘김은 WP6 AutoplayDriver. WP1d 가 시그니처만 만든 빈 연결 파일 — 본문은 WP4 가 채운다.
/// </summary>
public sealed class AutoplayPolicy(AutoplayVisit visit)
{
    public AutoplayVisit Visit { get; } = visit;

    /// <summary>방 들르기 기록(닿음·넘김, 일어난 순서). <see cref="AutoplayVisit.None"/> 이면 늘 빈 목록.</summary>
    public IReadOnlyList<RoomVisit> Events => throw new NotImplementedException("WP4 AutoplayPolicy.Events");

    /// <summary>안쪽 방마다 누적((맵 ID, 방 ID) 서수 순).</summary>
    public IReadOnlyList<RoomVisitStats> RoomStats => throw new NotImplementedException("WP4 AutoplayPolicy.RoomStats");

    /// <summary>문 단계 넘김 기록(v0.7, 일어난 순서). WP6 이 새 기록마다 로그 <c>DOOR_SKIP</c>.</summary>
    public IReadOnlyList<DoorSkip> DoorSkips => throw new NotImplementedException("WP4 AutoplayPolicy.DoorSkips");

    /// <summary>보상 단계 멈춤 기록(v0.7.1, 일어난 순서 — 방마다 한 번). WP6 이 새 기록마다 로그 <c>REWARD_STALL</c>.</summary>
    public IReadOnlyList<RewardStall> RewardStalls => throw new NotImplementedException("WP4 AutoplayPolicy.RewardStalls");

    /// <summary>
    /// 이번 스테이지 틱의 명령(v0.7 — v0.6 의 <c>Move</c> 를 대신함): 이동(길이 ≤ 1) + 방 규칙(9절 — 정리 추적·보상 집기·문 우선순위),
    /// 문 단계가 <c>autoplay_door_skip</c> 안에 안 끝나면 <see cref="CommandButtons.Exit"/> + <c>ExitIndex</c>. 정책 인스턴스는 판마다 하나(월드가 바뀌어도 그대로).
    /// </summary>
    public PlayerCommand Next(World world)
        => throw new NotImplementedException("WP4 AutoplayPolicy.Next");

    /// <summary>고를 카드 번호(0부터, <see cref="IProgression.Choose"/>·<c>ChooseReward</c> 에 넘김). <see cref="LevelUpOffer.Source"/> 로 규칙을 고른다(마법서 = 무기 순위, 성소 = 가진 축복 +1 먼저).</summary>
    public int PickCard(LevelUpOffer offer)
        => throw new NotImplementedException("WP4 AutoplayPolicy.PickCard");
}
