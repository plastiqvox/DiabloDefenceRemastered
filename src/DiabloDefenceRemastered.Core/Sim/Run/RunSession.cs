namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// 판 진행 한 곳 (v0.7 → v0.8 WP1f, 설계서 3.6 결정 4 — 그 뒤 WP4). 앱(<c>PlayScene</c>)은 부르기만 하고 시험도 같은 것을 돌린다.
/// <list type="bullet">
/// <item>생성자: 시작 액트의 <see cref="ActPlan"/>(액트 번호 = <c>RunPlanner.ActNumber</c>) → 시작 방(<c>StartRoomIndex</c>)의 <c>World</c> + 시뮬레이션 → <c>ApplyRunSetups(options)</c>.
/// <c>RunOptions.StartAt</c> 은 이 첫 방의 방 시계 시작이다(상한 <see cref="RoomStartLimits"/> — 넘으면 예외).</item>
/// <item><see cref="Advance"/>: 짐 담기 → 방 닫기(<c>CloseRoom</c>) → 고른 문의 보상 → 다음 방(액트의 마지막 방이면 계단 보상 + <c>next_act</c> 의 첫 방) → 새 <c>World</c> + 시뮬레이션 →
/// <c>ApplyRunSetups(무적·계측만)</c> → 짐 풀기. 등록된 <see cref="IHeroCarry"/> 가 없으면 짐은 건너뛴다.</item>
/// <item><see cref="Conclude"/>(v0.7.1): 판이 끝난 방(마지막 방의 승리, 어느 방이든 패배)은 <c>Exit</c> 에 닿지 않으므로 같은 <c>CloseRoom</c> 으로 닫는다.</item>
/// <item>계단 저장(v0.8 → v0.8.1): <see cref="CanSuspend"/>·<see cref="Suspend"/>·이어 하기 생성자 — 끊은 판은 같은 틱에 계단 문으로 나간 판과 같다(3.6 "계단 저장").</item>
/// <item>넓이 배율: 판 시작 때 지금 액트 사슬의 모든 방 맵을 한 번씩 펼쳐 <c>MapArea.WalkableCells</c> / 기준 맵 칸 수를 <c>rules.rooms.area_scale</c> 로 자른다(거울판은 원판 값).</item>
/// </list>
/// 판 수명 상태는 짐·<see cref="RunState"/> 에만 둔다(v0.8.1, 11.2) — 이어 하기는 새 <see cref="ModuleContext"/> 라 모듈 인스턴스·정적 필드의 상태는 사라진다.
/// </summary>
public sealed class RunSession
{
    private readonly GameDefs _defs;
    private readonly ModuleContext _modules;
    private readonly RunOptions _nextOptions;
    private readonly RoomRulesDef _rules;
    private readonly Dictionary<string, float> _areaScale = new(StringComparer.Ordinal);
    private bool _concluded;
    private bool _suspended;
    // 지금 방에 들어올 때의 값(RoomRecord 새 열 — 방 수명, Enter 가 다시 정함)
    private float _hpIn = -1f;
    private int _potionsIn;
    private int _goldSpentIn;
    private int _drunkIn;

    public RunSession(GameDefs defs, ModuleContext modules, RunStart start)
    {
        ArgumentNullException.ThrowIfNull(defs);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(start);
        _defs = defs;
        _modules = modules;
        _rules = defs.Rules.Rooms ?? throw new InvalidOperationException("판(RunSession)에는 rules.rooms 가 있어야 한다(6.10)");
        _nextOptions = NextRoomOptions(start.Options);

        var actNumber = RunPlanner.ActNumber(defs, start.StartAct);
        var plan = RunPlanner.PlanAct(start.StartAct, actNumber, start.Seed, _rules);
        if ((uint)start.StartRoomIndex >= (uint)plan.Rooms.Count)
            throw new ArgumentOutOfRangeException(nameof(start), $"시작 방 자리 {start.StartRoomIndex} 가 액트 '{start.StartAct.Id}' 의 방 {plan.Rooms.Count}개 밖이다");
        var index = start.StartRoomIndex;
        var startClock = start.Options.StartAt is { } at ? MathX.SecondsToTicks(at.Seconds, World.Dt) : 0;
        var room = plan.Rooms[index];
        if (!RoomStartLimits.Allows(room.Def, _rules, startClock))
            throw new ArgumentOutOfRangeException(nameof(start),
                $"--start-at {start.Options.StartAt} — 방 {room.Act}:{room.Number}({room.Kind}) 의 방 시계 상한 {RoomStartLimits.MaxStartClockTicks(room.Def, _rules)}틱 미만이어야 한다(3.6 결정 5)");
        Run = new RunState(start.Seed, start.StartAct, start.HeroClass, plan);
        ComputeAreaScales(start.StartAct);

        // --room 으로 그 방부터면 앞 방 RewardOrder 의 첫 칸(상태 거르기 없이 — 앞 방이 없으므로, v0.8: 정예·보스 방도 덤으로 같음) [해석]
        var reward = index == 0 ? FirstRoomReward(plan) : FirstOrNull(plan.Rooms[index - 1].RewardOrder);
        World = Enter(index, reward, startClock, start.Options, carry: null);
    }

    /// <summary>
    /// 이어 하기(v0.8 → v0.8.1, 3.6 "계단 저장"): 계단 저장의 판 상태로 <see cref="RunState"/> 를 되살리고 다음 액트 1번 방부터 — <see cref="Advance"/> ④⑤ 와 같은 길.
    /// <paramref name="options"/> 는 무적·계측만 쓴다(레벨·무기·패시브·시작 시각은 지움 — 끊지 않은 판의 둘째 방부터와 같음).
    /// 정의가 안 맞으면 <c>Restore</c> 전에 <see cref="RunSnapshotMismatchException"/>.
    /// </summary>
    public RunSession(GameDefs defs, ModuleContext modules, RunSnapshot snapshot, RunOptions options)
    {
        ArgumentNullException.ThrowIfNull(defs);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);
        _defs = defs;
        _modules = modules;
        _rules = defs.Rules.Rooms ?? throw new InvalidOperationException("판(RunSession)에는 rules.rooms 가 있어야 한다(6.10)");
        _nextOptions = NextRoomOptions(options);

        var (startAct, nextAct, heroClass) = CheckSnapshot(defs, modules, snapshot);
        var plan = RunPlanner.PlanAct(nextAct, snapshot.NextActNumber, snapshot.Seed, _rules);
        Run = new RunState(snapshot.Seed, startAct, heroClass, plan, snapshot.Stats, snapshot.Fairness)
        {
            ElapsedTicks = snapshot.ElapsedTicks,
            Depth = snapshot.Depth,
            Carry = snapshot.Carry,
        };
        Run.AddRooms(snapshot.Rooms);
        foreach (var id in snapshot.MerchantsMet) Run.MeetMerchant(id);
        ComputeAreaScales(startAct);

        World = Enter(0, FirstRoomReward(plan), 0, _nextOptions, snapshot.Carry);
    }

    public RunState Run { get; }
    /// <summary>지금 방(<c>World.Simulation</c> 이 들어 있음).</summary>
    public World World { get; private set; }
    /// <summary><see cref="Conclude"/> 를 불렀음.</summary>
    public bool Concluded => _concluded;
    /// <summary><see cref="Suspend"/> 를 불렀음(그 뒤 <see cref="Advance"/>·<see cref="Conclude"/> 는 예외).</summary>
    public bool Suspended => _suspended;

    /// <summary>
    /// 다음 방으로. <c>World.Room.Phase == Exit</c> 이고 <c>World.Outcome == Running</c> 일 때만(아니면 예외 — 죽은 주인공의 짐을 넘기지 않는다).
    /// <see cref="Conclude"/>·<see cref="Suspend"/> 뒤에는 예외.
    /// </summary>
    public void Advance()
    {
        if (_concluded) throw new InvalidOperationException("판이 끝났다(Conclude 뒤 Advance)");
        if (_suspended) throw new InvalidOperationException("판을 끊었다(Suspend 뒤 Advance)");
        var world = World;
        var room = world.Room ?? throw new InvalidOperationException("방 월드가 아니다");
        if (room.Phase != RoomPhase.Exit) throw new InvalidOperationException($"방 {room.Plan.Act}:{room.Plan.Number} 이 나감 단계가 아니다({room.Phase})");
        if (world.Outcome != RunOutcome.Running)
            throw new InvalidOperationException($"방 {room.Plan.Act}:{room.Plan.Number} — 판이 끝나 간다({world.Outcome}, Advance 대신 Conclude)");

        // ①~③ 짐·방 닫기·고른 문의 보상(계단이면 계단 보상)
        var (carry, stairs) = Settle(room);

        // ④ 다음 방
        int next;
        RewardDef? reward;
        if (!stairs)
        {
            next = Run.RoomIndex + 1;
            reward = Run.NextReward;   // v0.8: 정예·보스 방도 덤 보상
        }
        else
        {
            var nextAct = RunPlanner.Next(_defs, Run.Act.Act)
                ?? throw new InvalidOperationException($"액트 '{Run.Act.Act.Id}' 의 마지막 방 뒤 — 다음 액트가 없다(판의 끝은 Conclude)");
            Run.Act = RunPlanner.PlanAct(nextAct, Run.Act.ActNumber + 1, Run.Seed, _rules);
            next = 0;
            reward = FirstRoomReward(Run.Act);
            Run.MerchantRoomsThisAct = 0;   // v0.9: 상인 횟수·보였음은 액트마다
            Run.MerchantShownThisAct = false;
        }

        // ⑤ 새 월드 — 개발 옵션은 무적·계측만 다시(레벨·무기·패시브·시작 시각은 첫 방만) → 짐 풀기 → 상인 물건(v0.9)
        World = Enter(next, reward, 0, _nextOptions, carry);
    }

    /// <summary>
    /// 판 끝 정산(v0.7.1): 판이 끝난 방을 닫는다(<c>RoomRecord.Door = null</c>). <c>Outcome</c> 이 Victory·Defeat 일 때만(아니면 예외).
    /// 두 번째부터는 아무것도 안 하고 같은 <see cref="RunState"/>. 결과 화면·<c>RESULT</c>·<c>RecordResult</c>·시나리오는 이 뒤의 값만 읽는다.
    /// </summary>
    public RunState Conclude()
    {
        if (_concluded) return Run;
        if (_suspended) throw new InvalidOperationException("판을 끊었다(Suspend 뒤 Conclude)");
        if (World.Outcome is not (RunOutcome.Victory or RunOutcome.Defeat))
            throw new InvalidOperationException($"판이 아직 안 끝났다({World.Outcome})");
        CloseRoom(null);
        _concluded = true;
        return Run;
    }

    /// <summary>
    /// 계단 저장을 할 수 있는가(v0.8 → v0.8.1, 3.6 "계단 저장"): 액트 보스 방(다음 액트가 있는 액트의 마지막 방)·<c>Outcome == Running</c>·주인공 살아 있음이고,
    /// ⓐ <c>Phase == Doors</c>(계단 출구가 열림)·대기 0·<c>AwaitingChoice None</c>·끌려오는 획득물 0 — 메뉴 <c>저장하고 끝내기</c>, 또는
    /// ⓑ <c>Phase == Exit</c> 이고 <c>ChosenExit</c> 가 계단 문 — 자동 조작 판·사람이 계단 문 틈을 밟은 틱(<see cref="Advance"/> 를 부를 자리).
    /// </summary>
    public bool CanSuspend
    {
        get
        {
            if (_concluded || _suspended) return false;
            var world = World;
            if (world.Room is not { } room || world.Outcome != RunOutcome.Running || world.Hero.Dead) return false;
            if (Run.RoomIndex != Run.Act.Rooms.Count - 1 || RunPlanner.Next(_defs, Run.Act.Act) is null) return false;
            return room.Phase switch
            {
                RoomPhase.Doors => StairsExitIndex(room) >= 0 && !RoomFlow.ExitBlocked(world),
                RoomPhase.Exit => room.ChosenExitInfo is { Stairs: true },
                _ => false,
            };
        }
    }

    /// <summary>
    /// 판을 끊는다(v0.8 → v0.8.1 — <see cref="CanSuspend"/> 일 때만, 아니면 예외). 계단 문으로 나가는 틱과 <b>같은 길</b>을 탄다:
    /// ⓐ <c>Phase == Doors</c> 면 시뮬레이션을 한 틱 <c>PlayerCommand(0, Exit, 계단 문의 Exits 자리)</c> 로 돌린다(끊지 않은 판이 같은 틱에 계단 문 <c>Exit</c> 명령을 낸 것과 같은 틱),
    /// ⓑ <c>Phase == Exit</c>(계단 문)면 틱을 더 돌리지 않는다. 그다음 계단 문으로 <see cref="Advance"/> 하는 것과 같은 정산(짐 + 계단 보상·방 닫기·<c>NextReward = null</c>)까지만 하고
    /// 새 <c>World</c> 를 만들지 않고 판 상태를 돌려준다. 그 뒤 <see cref="Advance"/>·<see cref="Conclude"/> 는 예외.
    /// </summary>
    public RunSnapshot Suspend()
    {
        if (!CanSuspend) throw new InvalidOperationException("지금은 계단 저장을 할 수 없다(액트 보스 방의 문 단계·계단 문 나감에서만, 3.6)");
        var world = World;
        var room = world.Room!;
        if (room.Phase == RoomPhase.Doors)
        {
            var sim = world.Simulation ?? throw new InvalidOperationException("월드에 시뮬레이션이 없다");
            world.Command = new PlayerCommand(Vector2.Zero, CommandButtons.Exit, (byte)StairsExitIndex(room));
            sim.Step();
            if (room.Phase != RoomPhase.Exit || world.Outcome != RunOutcome.Running || room.ChosenExitInfo is not { Stairs: true })
                throw new InvalidOperationException($"계단 문 Exit 틱 뒤에 나감 단계가 아니다({room.Phase}, {world.Outcome})");
        }

        var (carry, _) = Settle(room);
        // 계단 문은 보상이 없으므로 회복 문이 걸려 있을 수 없다(6.19 — 스냅숏에 넣지 않음)
        if (Run.HealOnEnter) throw new InvalidOperationException("계단 저장인데 회복 문(HealOnEnter)이 걸려 있다 — 계단 문에는 보상이 없다(6.19)");
        var nextAct = RunPlanner.Next(_defs, Run.Act.Act)!;
        _suspended = true;
        return new RunSnapshot(RunSnapshot.CurrentFormat, Run.Seed, Run.StartAct.Id, Run.HeroClass?.Id, nextAct.Id, Run.Act.ActNumber + 1,
                               RunSnapshot.RoomsHash(nextAct), carry, Run.Stats, Run.Fairness, Run.ElapsedTicks, Run.Depth, [.. Run.Rooms],
                               [.. Run.MerchantsMet]);
    }

    /// <summary>방 하나의 넓이 배율(판 시작 때 계산한 값, 없는 맵은 1). 거울판은 원판 값(v0.8).</summary>
    public float AreaScaleOf(MapDef map) => _areaScale.TryGetValue(map.MirrorOf ?? map.Id, out var s) ? s : 1f;

    /// <summary>
    /// 방 하나에 들어간다: 새 <c>World</c> + 시뮬레이션 → 개발 옵션 → 짐 풀기(<paramref name="carry"/> 가 있으면) →
    /// v0.9: 상인 방이면 상인 물건(<see cref="ShopStock.Create"/> — 상인 전용 흐름, 정리 전) + 판의 상인 횟수·만난 상인, 회복 문 표시 넘기기, <c>RoomRecord</c> 새 열의 들어올 때 값.
    /// </summary>
    private World Enter(int index, RewardDef? reward, int startClockTicks, RunOptions options, HeroCarry? carry)
    {
        var act = Run.Act;
        var plan = act.Rooms[index];
        Run.RoomIndex = index;
        var depth = new RunDepth(act.ActNumber, plan.Number);
        if (depth > Run.Depth) Run.Depth = depth;
        var start = new RoomStart(Run, plan, reward, AreaScaleOf(plan.Map), RunPlanner.IsLastInRun(act, index), startClockTicks);
        var world = new World(_defs, start);
        _modules.CreateSimulation(world);
        _modules.ApplyRunSetups(world, options);
        if (carry is not null) _modules.HeroCarry!.Restore(world, carry);

        var room = world.Room!;
        Run.HealOnEnter = false;   // 그 방 RoomState.HealOnEnter 로 넘어감(보상이 heal_next_room 이면 참으로 시작)
        Run.LastRoomWasMerchant = false;
        if (reward is { Kind: RewardKind.Merchant, Merchant: { } merchantRef })
        {
            var merchant = merchantRef.IsBound ? merchantRef.Def : _defs.Merchants.Get(merchantRef.Id);
            var hero = world.Hero;
            room.Shop = ShopStock.Create(merchant, act.ActNumber, plan.Number, Run.Seed, _defs,
                                         new ShopPotions(hero.Potions, hero.PotionSlotsLevel, hero.PotionSpeedLevel));
            Run.MerchantRoomsThisAct++;
            Run.LastRoomWasMerchant = true;
            Run.MeetMerchant(merchant.Id);
        }

        var h = world.Hero;
        // 회복 문이면 들어옴 첫 틱에 채운 뒤 값(RoomFlow 와 같은 식 — v0.9.4 V15: 최대 × heal_amount, 최대에서 자름). v0.9 의 "= 최대" 가 남아 hp_in 이 늘 1 로 찍혔다(단계 2 wp6)
        _hpIn = room.HealOnEnter && !h.Dead && h.Hp < h.MaxHp ? MathF.Min(h.MaxHp, h.Hp + h.MaxHp * Math.Clamp(_rules.Rewards.HealAmount, 0f, 1f)) : h.Hp;
        _potionsIn = h.Potions;
        _goldSpentIn = Run.Stats.GoldSpent;
        _drunkIn = Run.Stats.PotionsDrunk;
        return world;
    }

    /// <summary>
    /// <see cref="Advance"/>·<see cref="Suspend"/> 의 정산 한 곳: ① 짐 담기 ② 방 닫기 ③ 고른 문의 보상을 <c>NextReward</c> 로(계단은 null).
    /// 액트의 마지막 방(계단)이면 짐의 열린 칸을 다음 액트 번호의 <see cref="RunSlots.OpenAt(int, LevelUpRulesDef?, RoomRulesDef)"/> 로(계단 보상).
    /// </summary>
    private (HeroCarry? Carry, bool Stairs) Settle(RoomState room)
    {
        var stairs = Run.RoomIndex + 1 >= Run.Act.Rooms.Count;
        // v0.9.1: 계단 문을 나가는 틱에 마시던 회복을 바로 채운다 — 짐·계단 저장에 회복 중 상태를 넣지 않는다(6.19 "계단 저장", 기획 11.5)
        if (stairs) FinishHeal(World.Hero);

        var carry = _modules.HeroCarry?.Capture(World);
        if (carry is not null) Run.Carry = carry;

        CloseRoom(room.ChosenExit);
        Run.NextReward = room.ChosenExitInfo?.Reward;
        Run.HealOnEnter = Run.NextReward?.Kind == RewardKind.HealNextRoom;   // v0.9 회복 문으로 나감 → 다음 방 첫 틱에 체력 전부
        foreach (var e in room.Exits)
            if (e.Reward?.Kind == RewardKind.Merchant) Run.MerchantShownThisAct = true;   // v0.9 상인 보장 조건(이 액트에 상인 문이 보였음)

        if (stairs && carry is not null)
        {
            var (weapons, passives) = RunSlots.OpenAt(Run.Act.ActNumber + 1, _defs.Rules.LevelUp, _rules);
            carry = carry with { WeaponSlotsOpen = weapons, PassiveSlotsOpen = passives };
            Run.Carry = carry;
        }
        return (carry, stairs);
    }

    /// <summary>방 닫기 한 곳(<see cref="Advance"/>·<see cref="Conclude"/>·<see cref="Suspend"/>): 기록 한 줄·걸린 시간·공정성 합.</summary>
    private void CloseRoom(string? door)
    {
        var world = World;
        var room = world.Room!;
        var plan = room.Plan;
        var level = world.Progression?.Level ?? Run.Carry?.Level ?? 1;
        Run.AddRoom(new RoomRecord(plan.Act, plan.Number, plan.MapId, plan.GateIn, room.Reward?.Id, door,
                                   room.ClearTick, room.CleanupTicks, room.Collapsed, world.Tick, level,
                                   plan.Mirrored, room.BossClockTicks, world.Progression?.PicksLeft ?? -1,
                                   _hpIn, Run.Stats.GoldSpent - _goldSpentIn, room.Shop?.MerchantId, _potionsIn, Run.Stats.PotionsDrunk - _drunkIn));
        Run.ElapsedTicks += world.Tick;
        world.Fairness.AddTo(Run.Fairness);
    }

    /// <summary>마시던 서서히 회복의 남은 양을 바로 채운다(최대 체력에서 자름, 죽었으면 비우기만).</summary>
    private static void FinishHeal(Hero hero)
    {
        if (hero.Heal.Active && !hero.Dead) hero.Hp = MathF.Min(hero.MaxHp, hero.Hp + hero.Heal.Remaining);
        hero.Heal = default;
    }

    /// <summary>계단 출구의 <c>Exits</c> 자리(없으면 −1).</summary>
    private static int StairsExitIndex(RoomState room)
    {
        for (var i = 0; i < room.Exits.Count; i++)
            if (room.Exits[i].Stairs) return i;
        return -1;
    }

    /// <summary>둘째 방부터의 개발 옵션: 무적·계측만(레벨·무기·패시브·시작 시각은 첫 방만).</summary>
    private static RunOptions NextRoomOptions(RunOptions options) =>
        options with { Level = null, Weapons = [], Passives = [], StartAt = null };

    /// <summary>
    /// 이어 하기 대조(v0.8.1 — 모두 <c>Restore</c> 전에): Format, 시작·다음 액트·직업, rooms_hash·액트 번호(시작 액트에서 센 번호 — <see cref="ActNumberAfter"/>), 기록 그릇, 짐 등록 여부·정의 ID·<c>Validate</c>.
    /// </summary>
    private static (StageDef StartAct, StageDef NextAct, HeroClassDef? HeroClass) CheckSnapshot(GameDefs defs, ModuleContext modules, RunSnapshot s)
    {
        static RunSnapshotMismatchException Fail(string why) => new($"계단 저장을 이을 수 없다 — {why}");

        if (s.Format != RunSnapshot.CurrentFormat) throw Fail($"저장 모양 판 {s.Format} ≠ 지금 {RunSnapshot.CurrentFormat}");
        if (!defs.Stages.TryGet(s.StartActId, out var startAct)) throw Fail($"시작 액트 '{s.StartActId}' 가 없다");
        if (!defs.Stages.TryGet(s.NextActId, out var nextAct)) throw Fail($"다음 액트 '{s.NextActId}' 가 없다");
        HeroClassDef? heroClass = null;
        if (s.HeroClassId is { } hid && !defs.HeroClasses.TryGet(hid, out heroClass)) throw Fail($"직업 '{hid}' 가 없다");
        var hash = RunSnapshot.RoomsHash(nextAct);
        if (!string.Equals(hash, s.NextActRoomsHash, StringComparison.Ordinal)) throw Fail($"액트 '{nextAct.Id}' 의 방 표가 바뀌었다(rooms_hash {s.NextActRoomsHash} → {hash})");
        var actNumber = ActNumberAfter(defs, startAct, nextAct)
            ?? throw Fail($"다음 액트 '{nextAct.Id}' 가 시작 액트 '{startAct.Id}' 뒤의 next_act 사슬에 없다");
        if (actNumber != s.NextActNumber) throw Fail($"액트 '{nextAct.Id}' 의 번호가 바뀌었다({s.NextActNumber} → {actNumber})");
        if (s.Stats is null || s.Fairness is null || s.Rooms is null || s.MerchantsMet is null) throw Fail("판 기록(stats·fairness·rooms·merchants_met)이 없다");
        foreach (var m in s.MerchantsMet)
            if (!defs.Merchants.TryGet(m, out _)) throw Fail($"만난 상인 '{m}' 가 없다");
        if (s.Stats.DamageByWeapon is not { Length: WeaponSlots.SlotCount })
            throw Fail($"무기별 피해 칸이 {s.Stats.DamageByWeapon?.Length ?? 0}개다({WeaponSlots.SlotCount}개여야 함)");

        var carrier = modules.HeroCarry;
        if ((carrier is null) != (s.Carry is null))
            throw Fail(carrier is null ? "이 판에는 짐을 받을 IHeroCarry 가 없는데 저장에 짐이 있다" : "저장에 짐(carry)이 없다");
        if (s.Carry is { } carry)
        {
            foreach (var w in carry.Weapons ?? [])
                if (w.Id.Length > 0 && !defs.Weapons.TryGet(w.Id, out _)) throw Fail($"짐의 무기 '{w.Id}' 가 없다");
            foreach (var p in carry.Passives ?? [])
                if (p.Id.Length > 0 && !defs.Passives.TryGet(p.Id, out _)) throw Fail($"짐의 패시브 '{p.Id}' 가 없다");
            foreach (var b in carry.Blessings ?? [])
                if (b.Id.Length > 0 && !defs.Passives.TryGet(b.Id, out _)) throw Fail($"짐의 축복 '{b.Id}' 가 없다");
            foreach (var shrine in carry.Shrines ?? [])
                if (!defs.Rewards.TryGet(shrine, out _)) throw Fail($"짐의 성소 '{shrine}' 가 없다");
            foreach (var e in carry.Equipment ?? [])
            {
                if (e.Item is null || !defs.ItemBases.TryGet(e.Item.BaseId, out _)) throw Fail($"짐의 장비 '{e.Item?.BaseId}'({e.Slot}) 가 없다");
                foreach (var a in (ReadOnlySpan<RolledAffix?>)[e.Item.Prefix, e.Item.Suffix])
                    if (a is { } r && !defs.ItemAffixes.TryGet(r.Id, out _)) throw Fail($"짐의 장비 접사 '{r.Id}' 가 없다");
            }
            if (carrier!.Validate(carry, defs) is { } reason) throw Fail(reason);
        }
        return (startAct, nextAct, heroClass);
    }

    /// <summary>
    /// 판 안의 액트 번호(v0.8.2 검증 반영): 시작 액트의 <c>RunPlanner.ActNumber</c> + 시작 액트에서 <c>next_act</c> 를 따라 <paramref name="act"/> 까지 간 걸음 수(1 이상) —
    /// 생성자·<see cref="Advance"/>·<see cref="Suspend"/> 가 매기는 번호(시작 번호에 계단마다 + 1)와 같은 식이라, 뿌리 사슬 밖 액트(헬파이어 곁가지)끼리 이어진 판도 맞는다.
    /// 시작 액트 뒤 사슬에 없으면 null.
    /// </summary>
    private static int? ActNumberAfter(GameDefs defs, StageDef startAct, StageDef act)
    {
        var n = RunPlanner.ActNumber(defs, startAct) + 1;
        var seen = new HashSet<string>(StringComparer.Ordinal) { startAct.Id };
        for (var s = RunPlanner.Next(defs, startAct); s is not null && seen.Add(s.Id); s = RunPlanner.Next(defs, s), n++)
            if (string.Equals(s.Id, act.Id, StringComparison.Ordinal)) return n;
        return null;
    }

    private static RewardDef? FirstRoomReward(ActPlan act) =>
        act.Rooms.Count > 0 && act.Act.Rewards.FirstRoom is { } first ? first.Def : null;

    private static RewardDef? FirstOrNull(IReadOnlyList<RewardDef> list) => list.Count > 0 ? list[0] : null;

    /// <summary>
    /// 넓이 배율(기획 10.8): 지금 액트 사슬의 모든 방 맵(풀 셋)을 한 번씩 펼쳐 걸을 칸 수 / 기준 맵 칸 수를 [min, max] 로 자른다.
    /// 기준 맵 칸 수가 0 이면 모두 1. 거울판은 펼치지 않는다(걸을 칸 수가 원판과 같다 — <see cref="AreaScaleOf"/> 가 원판 값).
    /// </summary>
    private void ComputeAreaScales(StageDef startAct)
    {
        var area = _rules.AreaScale;
        var reference = area.Reference.IsBound ? area.Reference.Def : _defs.Maps.TryGet(area.Reference.Id, out var m) ? m : null;
        var refCells = reference is null ? 0 : MapArea.WalkableCells(ArenaLayout.Expand(reference));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var act = startAct; act is not null && seen.Add(act.Id); act = RunPlanner.Next(_defs, act))
        {
            foreach (var r in act.RoomMaps.Normal) Add(r.Def);
            foreach (var r in act.RoomMaps.Elite) Add(r.Def);
            if (act.RoomMaps.Boss is { } boss) Add(boss.Def);
        }

        void Add(MapDef map)
        {
            if (_areaScale.ContainsKey(map.Id)) return;
            var scale = 1f;
            if (refCells > 0)
            {
                var cells = string.Equals(map.Id, reference!.Id, StringComparison.Ordinal) ? refCells : MapArea.WalkableCells(ArenaLayout.Expand(map));
                scale = Math.Clamp(cells / (float)refCells, area.Min, area.Max);
            }
            _areaScale[map.Id] = scale;
        }
    }
}
