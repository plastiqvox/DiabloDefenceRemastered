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
/// 방 사이 빌드 짐(v0.7 → v0.8 WP1f, 3.6 결정 1 표 — WP3 <see cref="IHeroCarry"/> 가 채움). 칸 목록은 <b>칸 번호 순서 그대로</b>이고 <c>Restore</c> 는 같은 번호 칸에 놓는다
/// (<c>RunStats.DamageByWeapon[칸]</c> 이 칸 번호로 세므로, v0.7.1). 대기(레벨업·상자·보상)는 넘기지 않는다 — 나갈 때 늘 0 이다(<c>Capture</c> 는 0 이 아니면 예외).
/// v0.8: init 속성 record(필드를 더할 때 기존 생성 호출이 깨지지 않게). 계단 저장(<c>RunSnapshot</c>)이 이 모양 그대로 STJ 왕복한다 —
/// 모양을 바꾸면 <c>RunSnapshot.CurrentFormat</c> 을 올린다(모양 지문 시험). v0.8.1: 짐에 필드가 필요하면 WP1g 로 모은다(sealed 계약 record — WP3 가 조각으로 더할 길이 없다).
/// 판 수명 상태를 모듈 인스턴스·정적 필드에 숨기지 않는다(11.2).
/// </summary>
public sealed record HeroCarry
{
    public required int Level { get; init; }
    public required int Xp { get; init; }
    public required int RerollsLeft { get; init; }
    public required float Hp { get; init; }
    public IReadOnlyList<CarriedSlot> Weapons { get; init; } = [];
    public IReadOnlyList<CarriedSlot> Passives { get; init; } = [];
    public IReadOnlyList<CarriedSlot> Blessings { get; init; } = [];
    /// <summary>열린 무기 칸 수(v0.8 — <c>RunSlots.OpenAt</c> 로 시작, 계단마다 <c>rules.rooms.stairs</c> 만큼 +, 상한 6).</summary>
    public required int WeaponSlotsOpen { get; init; }
    /// <summary>열린 패시브 칸 수(v0.8 — 위와 같은 규칙).</summary>
    public required int PassiveSlotsOpen { get; init; }
    /// <summary>허리띠 물약 수(v0.8, 0..<c>rules.hero.belt.slots</c>).</summary>
    public int Belt { get; init; }
    /// <summary>판 끝까지 가는 능력치 더하기(v0.8 — <c>단련</c> 쌓임·성소 대가. 같은 키는 하나로 합침, <see cref="StatKey"/> 순).</summary>
    public IReadOnlyList<CarriedStat> Mods { get; init; } = [];
    /// <summary>이 판에 받은 성소 보상 ID(v0.8 — 서수 순, 한 판에 한 번).</summary>
    public IReadOnlyList<string> Shrines { get; init; } = [];
    /// <summary>시간 효과의 남은 틱(v0.8 — 마력 폭주 등, <c>Id</c> 서수 순. 이름은 WP3 가 정함, 예 buff ID). 보상·문 단계·넘김·들어옴 동안은 줄지 않는다(3.6 결정 1).</summary>
    public IReadOnlyList<CarriedEffect> Effects { get; init; } = [];
}

/// <summary>짐의 칸 하나(정의 ID·레벨·진화). 빈 칸의 모양은 WP3 이 정한다.</summary>
public readonly record struct CarriedSlot(string Id, int Level, bool Evolved);

/// <summary>판 끝까지 가는 능력치 더하기 하나(v0.8 — 짐 <c>Mods</c>).</summary>
public readonly record struct CarriedStat(StatKey Stat, float Add);

/// <summary>시간 효과 하나의 남은 틱과 값(v0.8 — 짐 <c>Effects</c>, 뜻은 WP3).</summary>
public readonly record struct CarriedEffect(string Id, int TicksLeft, float Value);

/// <summary>
/// 빌드 넘기기(v0.7 → v0.8 — WP3 이 <c>ModuleContext.SetHeroCarry</c> 로 등록). v0.8: 넘기는 것 = 레벨·경험치·재추첨·체력·칸(무기·패시브·축복)·열린 칸 수·허리띠·
/// <c>Mods</c>·<c>Shrines</c>·시간 효과의 남은 틱. 무기 쿨다운·무기 내부 시간 상태·피격 무적은 넘기지 않는다(새 방에 들어오면 모두 준비).
/// </summary>
public interface IHeroCarry
{
    HeroCarry Capture(World world);
    void Restore(World world, HeroCarry carry);
    /// <summary>
    /// 이어 하기 전 WP3 만 아는 뜻의 확인(v0.8.1 — 시간 효과 ID·진화 짝·칸 수 범위 등). null = 맞음, 아니면 이유 —
    /// <c>RunSession</c> 이어 하기 생성자가 <c>Restore</c> 전에 부르고 이유가 있으면 <c>RunSnapshotMismatchException</c>.
    /// </summary>
    string? Validate(HeroCarry carry, GameDefs defs);
}

/// <summary>
/// 끝난 방 하나(v0.7 — HUD 방 줄·결과·로그 <c>ROOM</c>, 9절). <paramref name="Door"/> = 나간 문(판이 끝난 방은 null),
/// <paramref name="ClearTick"/> = 보상 단계에 들어온 방 틱(정리 못 함 −1), <paramref name="Ticks"/> = 그 방 <c>World.Tick</c>, <paramref name="Level"/> = 방을 나갈 때 레벨.
/// <paramref name="MapId"/> = 원판 맵 ID(v0.8 — 거울판은 <paramref name="Mirrored"/>).
/// </summary>
/// <param name="Mirrored">v0.8: 거울판 방(로그 <c>mirror=1</c>).</param>
/// <param name="BossClockTicks">v0.8: 정예·보스 방에서 중간 보스·보스를 잡은 방 시계(로그 <c>boss_s</c>), 아니면 −1.</param>
/// <param name="PicksLeft">v0.8: 방을 나갈 때 <c>IProgression.PicksLeft</c>(로그 <c>picks_left</c>), 성장 모듈이 없으면 −1.</param>
public readonly record struct RoomRecord(int Act, int Number, string MapId, string GateIn, string? RewardId, string? Door,
                                         int ClearTick, int CleanupTicks, bool Collapsed, int Ticks, int Level,
                                         bool Mirrored = false, int BossClockTicks = -1, int PicksLeft = -1);

/// <summary>
/// 판 상태 (v0.7, 설계서 3.6 — 판 수명). 방 월드마다 <c>World.Run</c> 이 이것을 가리킨다. 쓰기는 <see cref="RunSession"/> 뿐(방 사이에서만).
/// 결과 화면·저장·<c>RESULT</c> 로그의 출처다 — 판이 끝나면 <see cref="RunSession.Conclude"/> 뒤의 값을 읽는다.
/// </summary>
public sealed class RunState
{
    private readonly List<RoomRecord> _rooms = [];

    internal RunState(ulong seed, StageDef startAct, HeroClassDef? heroClass, ActPlan act, RunStats? stats = null, FairnessCounters? fairness = null)
    {
        Seed = seed;
        StartAct = startAct;
        HeroClass = heroClass;
        Act = act;
        Stats = stats ?? new RunStats();
        Fairness = fairness ?? new FairnessCounters();
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
    /// <summary>판 누적 기록(지우는 곳 없음, v0.7.1). 이어 하기(v0.8)는 계단 저장의 값을 그대로 쓴다.</summary>
    public RunStats Stats { get; }
    /// <summary>방마다 공정성 수의 합(방이 끝날 때 더함).</summary>
    public FairnessCounters Fairness { get; }
    /// <summary>끝난 방들의 <c>World.Tick</c> 합(판 걸린 시간).</summary>
    public int ElapsedTicks { get; internal set; }
    /// <summary>끝난 방(HUD 방 줄·결과·<c>ROOM</c> 로그). 판이 끝난 방은 <see cref="RunSession.Conclude"/> 가 더한다.</summary>
    public IReadOnlyList<RoomRecord> Rooms => _rooms;
    /// <summary>마지막으로 넘긴 짐(등록된 <see cref="IHeroCarry"/> 가 없으면 null).</summary>
    public HeroCarry? Carry { get; internal set; }
    /// <summary>앞 방에서 고른 문의 보상(다음 일반 방의 <c>RoomState.Reward</c>).</summary>
    public RewardDef? NextReward { get; internal set; }

    internal void AddRoom(in RoomRecord record) => _rooms.Add(record);

    internal void AddRooms(IEnumerable<RoomRecord> records) => _rooms.AddRange(records);
}
