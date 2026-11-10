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
    /// <summary>
    /// 무기 칸별 만렙 순번(v0.9 WP1g — WP3 요청: 방을 넘으면 "먼저 만렙이 된 무기" 순서가 칸 순서로 바뀌던 것). <see cref="Weapons"/> 와 같은 칸 순서·같은 길이(비면 모름),
    /// 값 = 만렙이 된 차례(작을수록 먼저, 같으면 칸 번호), −1 = 아직. 판 안 틱이 아니라 순번이다(<c>World.Tick</c> 은 방마다 0 부터).
    /// </summary>
    public IReadOnlyList<int> MaxedAtTick { get; init; } = [];
    /// <summary>장비(v0.9 V4, 6.19 — 칸 순서(<see cref="EquipSlot"/>), 빈 칸은 담지 않는다). 정의 ID 는 이어 하기 대조가 본다.</summary>
    public IReadOnlyList<CarriedEquip> Equipment { get; init; } = [];
    /// <summary>판 안 지갑(v0.9 V3 — 상인에게 쓸 금화, <c>Hero.Gold</c>). 얻은 금화 누적은 <c>RunStats.Gold</c>.</summary>
    public int Gold { get; init; }
    /// <summary>물약 칸의 체력 물약 수(v0.9.1 V9 — v0.8 의 <c>Belt</c> 를 바꿈, <c>Hero.Potions</c>). 마시는 중인 회복은 짐에 넣지 않는다 —
    /// 방 사이에는 WP3 이 시간 효과(<see cref="Effects"/>)로 넘기고, 계단 문을 나가는 틱에는 <c>RunSession</c> 이 바로 채운다(6.19 "계단 저장").</summary>
    public int Potions { get; init; }
    /// <summary>물약 주머니 단계(v0.9.1 V11 — <c>Hero.PotionSlotsLevel</c>).</summary>
    public int PotionSlotsLevel { get; init; }
    /// <summary>빠른 물약 단계(v0.9.1 V11 — <c>Hero.PotionSpeedLevel</c>).</summary>
    public int PotionSpeedLevel { get; init; }
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
/// 빌드 넘기기(v0.7 → v0.8 — WP3 이 <c>ModuleContext.SetHeroCarry</c> 로 등록). v0.8: 넘기는 것 = 레벨·경험치·재추첨·체력·칸(무기·패시브·축복)·열린 칸 수·
/// <c>Mods</c>·<c>Shrines</c>·시간 효과의 남은 틱. v0.9: + 만렙 순번·장비·지갑·물약 칸(수·주머니·빠른 물약 단계) — v0.8 의 허리띠는 없앰.
/// 무기 쿨다운·무기 내부 시간 상태·피격 무적은 넘기지 않는다(새 방에 들어오면 모두 준비).
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
/// <param name="HpIn">v0.9: 들어올 때 체력(로그 <c>hp_in</c> — 회복 문이면 채운 뒤 값, 짐을 넘긴 뒤 <c>Hero.Hp</c>). 모르면 −1.</param>
/// <param name="GoldSpent">v0.9: 이 방에서 쓴 금화(로그 <c>gold_spent</c> — <c>RunStats.GoldSpent</c> 의 방 차이).</param>
/// <param name="Shop">v0.9: 상인 방이면 상인 ID(로그 <c>shop</c>), 아니면 null.</param>
/// <param name="Potions">v0.9.1: 들어올 때 물약 칸의 물약 수(로그 <c>potions</c>).</param>
/// <param name="Drunk">v0.9.1: 이 방에서 마신 물약 수(로그 <c>drunk</c> — <c>RunStats.PotionsDrunk</c> 의 방 차이).</param>
public readonly record struct RoomRecord(int Act, int Number, string MapId, string GateIn, string? RewardId, string? Door,
                                         int ClearTick, int CleanupTicks, bool Collapsed, int Ticks, int Level,
                                         bool Mirrored = false, int BossClockTicks = -1, int PicksLeft = -1,
                                         float HpIn = -1f, int GoldSpent = 0, string? Shop = null, int Potions = 0, int Drunk = 0);

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

    // ── v0.9 문 보상 뽑기·상인(6.19 — 쓰기 RunSession, 읽기 RoomFlow 문 열기 RewardDraw) ──

    /// <summary>
    /// 회복 문(<c>heal_next_room</c>)으로 나갔다(v0.9 V1): 문을 나가는 틱(<c>Advance</c> 정산)에 참, 다음 방을 만들면 그 방 <c>RoomState.HealOnEnter</c> 로 넘기고 거짓.
    /// 계단 문은 보상이 없으므로 <c>Suspend</c> 때 늘 거짓이다(<c>Suspend</c> 가 단언 — 스냅숏에 넣지 않음).
    /// </summary>
    public bool HealOnEnter { get; internal set; }
    /// <summary>이 판에 들어간 상인 방의 상인 ID(v0.9 — 서수 순, 중복 없음). "만난 뒤 다른 상인 × 1.5" 가중. 계단 저장(<c>RunSnapshot.MerchantsMet</c>)에 이어진다.</summary>
    public IReadOnlyList<string> MerchantsMet => _merchantsMet;
    /// <summary>지금 액트에 들어간 상인 방 수(v0.9 — 액트 상한 <c>merchant_per_act</c>, 액트마다 0).</summary>
    public int MerchantRoomsThisAct { get; internal set; }
    /// <summary>지금 액트에 상인 문이 한 번이라도 보였다(v0.9 — 상인 보장 조건, 액트마다 거짓).</summary>
    public bool MerchantShownThisAct { get; internal set; }
    /// <summary>지금 방이 상인 방이다(v0.9 — "바로 앞 방이 상인 방" 이면 이 방 문에 상인 없음).</summary>
    public bool LastRoomWasMerchant { get; internal set; }

    private readonly List<string> _merchantsMet = [];

    internal void MeetMerchant(string id)
    {
        var i = _merchantsMet.BinarySearch(id, StringComparer.Ordinal);
        if (i < 0) _merchantsMet.Insert(~i, id);
    }

    internal void AddRoom(in RoomRecord record) => _rooms.Add(record);

    internal void AddRooms(IEnumerable<RoomRecord> records) => _rooms.AddRange(records);
}
