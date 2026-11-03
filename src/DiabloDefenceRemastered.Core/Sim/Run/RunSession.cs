namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// 판 진행 한 곳 (v0.7, 설계서 3.6 결정 4 — 계약·구현 WP1e, 그 뒤 WP4). 앱(<c>PlayScene</c>)은 부르기만 하고 시험도 같은 것을 돌린다.
/// <list type="bullet">
/// <item>생성자: 시작 액트의 <see cref="ActPlan"/>(액트 번호 = <c>RunPlanner.ActNumber</c>) → 시작 방(<c>StartRoomIndex</c>)의 <c>World</c> + 시뮬레이션 → <c>ApplyRunSetups(options)</c>.
/// <c>RunOptions.StartAt</c> 은 이 첫 방의 방 시계 시작이다.</item>
/// <item><see cref="Advance"/>: 짐 담기 → 방 닫기(<c>CloseRoom</c>) → 고른 문의 보상 → 다음 방(액트의 마지막 방이면 <c>next_act</c> 의 첫 방) → 새 <c>World</c> + 시뮬레이션 →
/// <c>ApplyRunSetups(무적·계측만)</c> → 짐 풀기. 등록된 <see cref="IHeroCarry"/> 가 없으면 짐은 건너뛴다.</item>
/// <item><see cref="Conclude"/>(v0.7.1): 판이 끝난 방(마지막 방의 승리, 어느 방이든 패배)은 <c>Exit</c> 에 닿지 않으므로 같은 <c>CloseRoom</c> 으로 닫는다.</item>
/// <item>넓이 배율: 판 시작 때 지금 액트 사슬의 모든 방 맵을 한 번씩 펼쳐 <c>MapArea.WalkableCells</c> / 기준 맵 칸 수를 <c>rules.rooms.area_scale</c> 로 자른다.</item>
/// </list>
/// </summary>
public sealed class RunSession
{
    private readonly GameDefs _defs;
    private readonly ModuleContext _modules;
    private readonly RunStart _start;
    private readonly RoomRulesDef _rules;
    private readonly Dictionary<string, float> _areaScale = new(StringComparer.Ordinal);
    private bool _concluded;

    public RunSession(GameDefs defs, ModuleContext modules, RunStart start)
    {
        ArgumentNullException.ThrowIfNull(defs);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(start);
        _defs = defs;
        _modules = modules;
        _start = start;
        _rules = defs.Rules.Rooms ?? throw new InvalidOperationException("판(RunSession)에는 rules.rooms 가 있어야 한다(6.10)");

        var actNumber = RunPlanner.ActNumber(defs, start.StartAct);
        var plan = RunPlanner.PlanAct(start.StartAct, actNumber, start.Seed, _rules);
        if ((uint)start.StartRoomIndex >= (uint)plan.Rooms.Count)
            throw new ArgumentOutOfRangeException(nameof(start), $"시작 방 자리 {start.StartRoomIndex} 가 액트 '{start.StartAct.Id}' 의 방 {plan.Rooms.Count}개 밖이다");
        Run = new RunState(start.Seed, start.StartAct, start.HeroClass, plan);
        ComputeAreaScales(start.StartAct);

        var index = start.StartRoomIndex;
        // --room 으로 그 방부터면 앞 방 RewardOrder 의 첫 칸(상태 거르기 없이 — 앞 방이 없으므로) [해석]
        var reward = index == 0 ? FirstRoomReward(plan) : plan.Rooms[index].Kind == RoomKind.Normal ? FirstOrNull(plan.Rooms[index - 1].RewardOrder) : null;
        var startClock = start.Options.StartAt is { } at ? MathX.SecondsToTicks(at.Seconds, World.Dt) : 0;
        World = Enter(index, reward, startClock, start.Options);
    }

    public RunState Run { get; }
    /// <summary>지금 방(<c>World.Simulation</c> 이 들어 있음).</summary>
    public World World { get; private set; }
    /// <summary><see cref="Conclude"/> 를 불렀음.</summary>
    public bool Concluded => _concluded;

    /// <summary>
    /// 다음 방으로. <c>World.Room.Phase == Exit</c> 이고 <c>World.Outcome == Running</c> 일 때만(아니면 예외 — 죽은 주인공의 짐을 넘기지 않는다).
    /// <see cref="Conclude"/> 뒤에는 예외.
    /// </summary>
    public void Advance()
    {
        if (_concluded) throw new InvalidOperationException("판이 끝났다(Conclude 뒤 Advance)");
        var world = World;
        var room = world.Room ?? throw new InvalidOperationException("방 월드가 아니다");
        if (room.Phase != RoomPhase.Exit) throw new InvalidOperationException($"방 {room.Plan.Act}:{room.Plan.Number} 이 나감 단계가 아니다({room.Phase})");
        if (world.Outcome != RunOutcome.Running)
            throw new InvalidOperationException($"방 {room.Plan.Act}:{room.Plan.Number} — 판이 끝나 간다({world.Outcome}, Advance 대신 Conclude)");

        // ① 짐
        var carrier = _modules.HeroCarry;
        var carry = carrier?.Capture(world);
        if (carry is not null) Run.Carry = carry;

        // ② 방 닫기 ③ 고른 문의 보상
        CloseRoom(room.ChosenExit);
        var exit = room.ChosenExitInfo;
        Run.NextReward = exit?.Reward;

        // ④ 다음 방
        var next = Run.RoomIndex + 1;
        RewardDef? reward;
        if (next < Run.Act.Rooms.Count)
            reward = Run.Act.Rooms[next].Kind == RoomKind.Normal ? Run.NextReward : null;
        else
        {
            var nextAct = RunPlanner.Next(_defs, Run.Act.Act)
                ?? throw new InvalidOperationException($"액트 '{Run.Act.Act.Id}' 의 마지막 방 뒤 — 다음 액트가 없다(판의 끝은 Conclude)");
            Run.Act = RunPlanner.PlanAct(nextAct, Run.Act.ActNumber + 1, Run.Seed, _rules);
            next = 0;
            reward = FirstRoomReward(Run.Act);
        }

        // ⑤ 새 월드 — 개발 옵션은 무적·계측만 다시(레벨·무기·패시브·시작 시각은 첫 방만)
        World = Enter(next, reward, 0, _start.Options with { Level = null, Weapons = [], Passives = [], StartAt = null });
        if (carry is not null) carrier!.Restore(World, carry);
    }

    /// <summary>
    /// 판 끝 정산(v0.7.1): 판이 끝난 방을 닫는다(<c>RoomRecord.Door = null</c>). <c>Outcome</c> 이 Victory·Defeat 일 때만(아니면 예외).
    /// 두 번째부터는 아무것도 안 하고 같은 <see cref="RunState"/>. 결과 화면·<c>RESULT</c>·<c>RecordResult</c>·시나리오는 이 뒤의 값만 읽는다.
    /// </summary>
    public RunState Conclude()
    {
        if (_concluded) return Run;
        if (World.Outcome is not (RunOutcome.Victory or RunOutcome.Defeat))
            throw new InvalidOperationException($"판이 아직 안 끝났다({World.Outcome})");
        CloseRoom(null);
        _concluded = true;
        return Run;
    }

    /// <summary>방 하나의 넓이 배율(판 시작 때 계산한 값, 없는 맵은 1).</summary>
    public float AreaScaleOf(MapDef map) => _areaScale.TryGetValue(map.Id, out var s) ? s : 1f;

    private World Enter(int index, RewardDef? reward, int startClockTicks, RunOptions options)
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
        return world;
    }

    /// <summary>방 닫기 한 곳(<see cref="Advance"/>·<see cref="Conclude"/>): 기록 한 줄·걸린 시간·공정성 합.</summary>
    private void CloseRoom(string? door)
    {
        var world = World;
        var room = world.Room!;
        var plan = room.Plan;
        var level = world.Progression?.Level ?? Run.Carry?.Level ?? 1;
        Run.AddRoom(new RoomRecord(plan.Act, plan.Number, plan.Map.Id, plan.GateIn, room.Reward?.Id, door,
                                   room.ClearTick, room.CleanupTicks, room.Collapsed, world.Tick, level));
        Run.ElapsedTicks += world.Tick;
        world.Fairness.AddTo(Run.Fairness);
    }

    private static RewardDef? FirstRoomReward(ActPlan act) =>
        act.Rooms.Count > 0 && act.Rooms[0].Kind == RoomKind.Normal && act.Act.Rewards.FirstRoom is { } first ? first.Def : null;

    private static RewardDef? FirstOrNull(IReadOnlyList<RewardDef> list) => list.Count > 0 ? list[0] : null;

    /// <summary>
    /// 넓이 배율(기획 10.8): 지금 액트 사슬의 모든 방 맵(풀 셋)을 한 번씩 펼쳐 걸을 칸 수 / 기준 맵 칸 수를 [min, max] 로 자른다.
    /// 기준 맵 칸 수가 0 이면 모두 1.
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
