namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary><c>--room 2:6</c>(v0.7, 9절). 둘 다 1부터, <see cref="Room"/> = 보이는 방 번호(<c>RoomDef.Number</c>).</summary>
public readonly record struct RoomRef(int Act, int Room)
{
    public override string ToString() => $"{Act}:{Room}";
}

/// <summary>
/// 가장 깊이 간 곳(v0.7, 8.3): 액트 → 방 번호 사전순. 기록 비교 키는 (깊이, 승리, −시간)(v0.7.1, 8.3 — 저장 구현 WP7).
/// </summary>
public readonly record struct RunDepth(int Act, int Room) : IComparable<RunDepth>
{
    public int CompareTo(RunDepth other) => Act != other.Act ? Act.CompareTo(other.Act) : Room.CompareTo(other.Room);

    public static bool operator <(RunDepth a, RunDepth b) => a.CompareTo(b) < 0;
    public static bool operator >(RunDepth a, RunDepth b) => a.CompareTo(b) > 0;
    public static bool operator <=(RunDepth a, RunDepth b) => a.CompareTo(b) <= 0;
    public static bool operator >=(RunDepth a, RunDepth b) => a.CompareTo(b) >= 0;

    /// <summary>로그 <c>depth=1:8</c> 모양.</summary>
    public override string ToString() => $"{Act}:{Room}";
}

/// <summary>
/// 판 시작 입력(v0.7, 3.6). <c>--room a:n</c> → 앱(WP7)이 <c>RunPlanner.ActAt(defs, a)</c> 로 액트를 찾고 <paramref name="StartRoomIndex"/> = <c>Number == n</c> 인 rooms 원소의 배열 자리.
/// 없는 번호는 옵션 오류(64).
/// </summary>
public sealed record RunStart(ulong Seed, StageDef StartAct, HeroClassDef? HeroClass, RunOptions Options, int StartRoomIndex = 0);

/// <summary>
/// 방 사이 빌드 짐(v0.7, 3.6 결정 1 표 — WP3 <see cref="IHeroCarry"/> 가 채움). 칸 목록은 <b>칸 번호 순서 그대로</b>이고 <c>Restore</c> 는 같은 번호 칸에 놓는다
/// (<c>RunStats.DamageByWeapon[칸]</c> 이 칸 번호로 세므로, v0.7.1). 대기(레벨업·상자·보상)는 넘기지 않는다 — 나갈 때 늘 0 이다(<c>Capture</c> 는 0 이 아니면 예외).
/// 판 중간 저장을 넣을 때 이것만 직렬화하면 된다(기획 열린 질문 33).
/// </summary>
public sealed record HeroCarry(int Level, int Xp, int RerollsLeft, float Hp,
                               IReadOnlyList<CarriedSlot> Weapons, IReadOnlyList<CarriedSlot> Passives, IReadOnlyList<CarriedSlot> Blessings);

/// <summary>짐의 칸 하나(정의 ID·레벨·진화). 빈 칸의 모양은 WP3 이 정한다.</summary>
public readonly record struct CarriedSlot(string Id, int Level, bool Evolved);

/// <summary>빌드 넘기기(v0.7 — WP3 이 <c>ModuleContext.SetHeroCarry</c> 로 등록). 무기 내부 시간 상태·버프·마력 폭주·피격 무적은 넘기지 않는다.</summary>
public interface IHeroCarry
{
    HeroCarry Capture(World world);
    void Restore(World world, HeroCarry carry);
}

/// <summary>
/// 끝난 방 하나(v0.7 — HUD 방 줄·결과·로그 <c>ROOM</c>, 9절). <paramref name="Door"/> = 나간 문(판이 끝난 방은 null),
/// <paramref name="ClearTick"/> = 보상 단계에 들어온 방 틱(정리 못 함 −1), <paramref name="Ticks"/> = 그 방 <c>World.Tick</c>, <paramref name="Level"/> = 방을 나갈 때 레벨.
/// </summary>
public readonly record struct RoomRecord(int Act, int Number, string MapId, string GateIn, string? RewardId, string? Door,
                                         int ClearTick, int CleanupTicks, bool Collapsed, int Ticks, int Level);

/// <summary>
/// 판 상태 (v0.7, 설계서 3.6 — 판 수명). 방 월드마다 <c>World.Run</c> 이 이것을 가리킨다. 쓰기는 <see cref="RunSession"/> 뿐(방 사이에서만).
/// 결과 화면·저장·<c>RESULT</c> 로그의 출처다 — 판이 끝나면 <see cref="RunSession.Conclude"/> 뒤의 값을 읽는다.
/// </summary>
public sealed class RunState
{
    private readonly List<RoomRecord> _rooms = [];

    internal RunState(ulong seed, StageDef startAct, HeroClassDef? heroClass, ActPlan act)
    {
        Seed = seed;
        StartAct = startAct;
        HeroClass = heroClass;
        Act = act;
    }

    public ulong Seed { get; }
    public StageDef StartAct { get; }
    public HeroClassDef? HeroClass { get; }
    /// <summary>지금 액트의 계획.</summary>
    public ActPlan Act { get; internal set; }
    /// <summary>지금 방의 배열 자리(<c>Act.Rooms</c>).</summary>
    public int RoomIndex { get; internal set; }
    /// <summary>가장 깊이 간 곳(들어간 방 — 액트 번호·방 번호).</summary>
    public RunDepth Depth { get; internal set; }
    /// <summary>판 누적 기록(지우는 곳 없음, v0.7.1).</summary>
    public RunStats Stats { get; } = new();
    /// <summary>방마다 공정성 수의 합(방이 끝날 때 더함).</summary>
    public FairnessCounters Fairness { get; } = new();
    /// <summary>끝난 방들의 <c>World.Tick</c> 합(판 걸린 시간).</summary>
    public int ElapsedTicks { get; internal set; }
    /// <summary>끝난 방(HUD 방 줄·결과·<c>ROOM</c> 로그). 판이 끝난 방은 <see cref="RunSession.Conclude"/> 가 더한다.</summary>
    public IReadOnlyList<RoomRecord> Rooms => _rooms;
    /// <summary>마지막으로 넘긴 짐(등록된 <see cref="IHeroCarry"/> 가 없으면 null).</summary>
    public HeroCarry? Carry { get; internal set; }
    /// <summary>앞 방에서 고른 문의 보상(다음 일반 방의 <c>RoomState.Reward</c>).</summary>
    public RewardDef? NextReward { get; internal set; }

    internal void AddRoom(in RoomRecord record) => _rooms.Add(record);
}
