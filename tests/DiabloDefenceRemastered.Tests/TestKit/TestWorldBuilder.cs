namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 시험 월드 만들기 (WP1 TestKit, WP1b·WP1c·WP1d 갱신, 설계서 10.1). 시야는 기본 꺼짐 — <see cref="WithSight"/> 로 켠다. 코드로 만든 작은 합성 타일셋·맵(<see cref="ArenaGrid"/> 로 푼 등각 아레나)·정의
/// + 전수 공간 질의 + 직선 내비(바깥 마름모 경계). 주인공은 맵의 <c>hero_start</c>(<c>World.Resolved</c>)에서 시작한다.
/// 각 묶음은 다른 묶음 없이 자기 시스템만 넣어 시험한다(R9).
/// <code>
/// var t = new TestWorldBuilder().WithSystem(SimPhase.EnemyThink, new MySystem()).Build();
/// var e = t.AddEnemy(new Vector2(100, 100));
/// t.Step(60);
/// </code>
/// </summary>
public sealed class TestWorldBuilder
{
    private ulong _seed = 1;
    private int _megaTiles = TestDefs.DefaultMegaTiles;
    private GameDefs? _defs;
    private RoomSpec? _room;
    private float _sightRadius;
    private SightRulesDef? _sightRules;
    private IReadOnlyList<PiecePlacementDef>? _place;
    private bool _presentation;
    private HeroClassDef? _heroClass;
    private readonly List<(SimPhase Phase, Func<World, ISimSystem> Create)> _systems = [];
    private readonly List<Action<World, SimulationBuilder>> _installers = [];
    private readonly List<Action<World>> _setups = [];

    public TestWorldBuilder WithSeed(ulong seed) { _seed = seed; return this; }

    /// <summary>기본 정의(<see cref="TestDefs.Basic"/>)의 맵 메가타일 수(하위 타일 N = 2M, 외곽 상자 64N × (32N + 128)).</summary>
    public TestWorldBuilder WithMegaTiles(int megaTiles) { _megaTiles = megaTiles; return this; }

    /// <summary>정의 묶음을 통째로 바꾼다. 첫 스테이지가 있으면 그것으로 월드를 만든다.</summary>
    public TestWorldBuilder WithDefs(GameDefs defs) { _defs = defs; return this; }

    /// <summary>
    /// 방 월드(v0.7.1 #23-형 — <c>World(defs, RoomStart)</c>): 정의의 첫 액트로 합성 <c>RunState</c>·<c>ActPlan</c>(이 방 + 있으면 다음 방)을 만들고 단계·방 시계를 손으로 정한다.
    /// 기본 방 = 첫 액트의 첫 방(일반 0:00~1:00), 들어온 문 <c>gate_se</c>, 출구 = <c>rules.rooms.gates.exits</c>(판의 마지막 방이면 없음), 보상 후보 순서 = 정의의 보상 표 순서.
    /// <paramref name="fakeSpawner"/> 면 <see cref="FakeSpawner"/>(WP3 없이 보상 물체 놓기)를 넣는다. 형 단계의 <c>RoomFlowSystem</c> 은 단계를 바꾸지 않았다 — 구현 뒤에는 돈다.
    /// </summary>
    public TestWorldBuilder WithRoom(RoomPhase phase = RoomPhase.Enter, int clockTicks = 0, RoomDef? room = null, RoomDef? next = null,
                                     string gateIn = "gate_se", IReadOnlyList<string>? exits = null, RewardDef? reward = null,
                                     bool isLastInRun = false, bool fakeSpawner = true)
    {
        _room = new RoomSpec(phase, clockTicks, room, next, gateIn, exits, reward, isLastInRun, fakeSpawner);
        return this;
    }

    /// <summary>
    /// 시야 켬(v0.5): 기본 정의에 <c>rules.sight</c>(기본 <see cref="TestDefs.Sight"/>)와 주인공 빛 반경 <paramref name="radius"/> 를 넣는다.
    /// 시야를 끈 대조군은 이것을 부르지 않은 월드(<c>World.Visibility.Enabled</c> 거짓)다. 실행 중 반경 바꾸기는 <see cref="TestWorld.SetHeroLightRadius"/>.
    /// </summary>
    public TestWorldBuilder WithSight(float radius = TestDefs.SightRadius, SightRulesDef? rules = null)
    {
        _sightRadius = radius;
        _sightRules = rules ?? TestDefs.Sight;
        return this;
    }

    /// <summary>기본 정의 맵의 안쪽 조각(기본 기둥 넷 대신). 빈 목록이면 빈 방.</summary>
    public TestWorldBuilder WithPlace(params PiecePlacementDef[] place) { _place = place; return this; }

    /// <summary>
    /// 고른 직업(v0.6 — <c>World(…, heroClass)</c>). 없으면 정의의 <c>rules.hero.class</c>(기본 정의는 <see cref="TestDefs.HeroClassId"/>).
    /// 넘긴 직업은 정의 표에 없어도 된다 — 월드는 그 정의만 읽는다.
    /// </summary>
    public TestWorldBuilder WithHeroClass(HeroClassDef heroClass) { _heroClass = heroClass; return this; }

    /// <summary>표시 이벤트를 쌓을지(기본 끔 — 그래픽 없는 실행).</summary>
    public TestWorldBuilder WithPresentation(bool enabled = true) { _presentation = enabled; return this; }

    public TestWorldBuilder WithSystem(SimPhase phase, ISimSystem system) => WithSystem(phase, _ => system);

    public TestWorldBuilder WithSystem(SimPhase phase, Func<World, ISimSystem> create)
    {
        _systems.Add((phase, create));
        return this;
    }

    /// <summary>모듈의 월드 설치(<see cref="ModuleContext.AddWorldInstaller"/>)를 그대로 쓴다.</summary>
    public TestWorldBuilder WithModules(ModuleContext modules)
    {
        _installers.Add(modules.Install);
        return this;
    }

    /// <summary>시스템을 넣기 전에 월드를 고친다(서비스 바꾸기 등).</summary>
    public TestWorldBuilder Setup(Action<World> setup) { _setups.Add(setup); return this; }

    public TestWorld Build()
    {
        if (_defs is not null && (_sightRules is not null || _place is not null))
            throw new InvalidOperationException("WithSight·WithPlace 는 기본 정의에만 쓴다 — WithDefs 에는 TestDefs.Basic(sight:, heroLight:, place:) 로 넣는다");
        var defs = _defs ?? TestDefs.Basic(_megaTiles, sight: _sightRules, heroLight: _sightRadius, place: _place);
        var stage = defs.Stages.Count > 0 ? defs.Stages[0] : null;
        var world = _room is { } spec ? BuildRoomWorld(defs, stage ?? throw new InvalidOperationException("WithRoom 은 스테이지(액트)가 있는 정의에만"), spec) : new World(defs, _seed, stage, _heroClass);
        world.Presentation.Enabled = _presentation;
        var nav = world.Arena is { } arena ? new StraightNav(arena.Iso) : new StraightNav(world.MapSize);
        nav.HeroPos = () => world.Hero.Pos;
        world.Nav = nav;
        world.Spatial = new BruteForceSpatial(world);
        if (_room is { FakeSpawner: true }) world.Spawner = new FakeSpawner(world);
        foreach (var setup in _setups) setup(world);

        var builder = Simulation.CreateBuilder();
        foreach (var install in _installers) install(world, builder);
        foreach (var (phase, create) in _systems) builder.Add(phase, create(world));
        return new TestWorld(world, builder.Build(world));
    }

    private sealed record RoomSpec(RoomPhase Phase, int ClockTicks, RoomDef? Room, RoomDef? Next, string GateIn, IReadOnlyList<string>? Exits,
                                   RewardDef? Reward, bool IsLastInRun, bool FakeSpawner);

    private World BuildRoomWorld(GameDefs defs, StageDef stage, RoomSpec spec)
    {
        var rules = defs.Rules.Rooms ?? throw new InvalidOperationException("WithRoom 은 rules.rooms 가 있는 정의에만");
        var map = stage.RoomMaps.Boss?.Def ?? stage.RoomMaps.Normal[0].Def;
        var def = spec.Room ?? stage.Rooms[0];
        var order = defs.Rewards.All;
        var exits = spec.Exits ?? (spec.IsLastInRun ? [] : rules.Gates.Exits);
        var plans = new List<RoomPlan> { new(1, 0, def, map, spec.GateIn, exits, order, _seed) };
        if (spec.Next is { } next) plans.Add(new RoomPlan(1, 1, next, map, rules.Gates.Entry[0], [], order, _seed + 1));
        var run = new RunState(_seed, stage, _heroClass ?? defs.Rules.Hero?.Class.Def, new ActPlan(stage, 1, plans));
        var world = new World(defs, new RoomStart(run, plans[0], spec.Reward, 1f, spec.IsLastInRun, spec.ClockTicks));
        world.Room!.EnterPhase(spec.Phase, 0);
        return world;
    }
}

/// <summary>만든 시험 월드와 도우미.</summary>
public sealed class TestWorld(World world, Simulation simulation)
{
    public World World { get; } = world;
    public Simulation Simulation { get; } = simulation;
    public Hero Hero => World.Hero;

    public void Step(int ticks = 1) => Simulation.Run(ticks);

    public StageStep AppTick(PlayerCommand command = default) => StageStepper.AppTick(World, command);

    /// <summary>하위 칸 (tx, ty) 중심의 월드 px(맵 있는 월드).</summary>
    public Vector2 CellCenter(int tx, int ty) => World.Arena!.Iso.CellCenter(tx, ty);

    /// <summary>주인공을 옮긴다(이전 위치도 같이 — 보간·이동 없음).</summary>
    public void PlaceHero(Vector2 pos) => Hero.Pos = Hero.PrevPos = pos;

    /// <summary><c>light_change</c> 처럼 주인공 빛 반경을 바꾼다. 시야는 다음 <c>BeginTick</c> 부터 새 반경이다(4.11).</summary>
    public void SetHeroLightRadius(float radius) => World.Lighting.HeroLightRadius = radius;

    /// <summary>적 하나를 바로 행동하는 상태로 넣는다(기본 정의 <see cref="TestDefs.EnemyId"/>).</summary>
    public Enemy AddEnemy(Vector2 pos, string defId = TestDefs.EnemyId, LifeState life = LifeState.Active)
        => AddEnemy(World.Defs.Enemies.Get(defId), pos, life);

    public Enemy AddEnemy(EnemyDef def, Vector2 pos, LifeState life = LifeState.Active)
    {
        var e = World.Enemies.Add();
        e.Def = def;
        e.DefIndex = World.Defs.Enemies.IndexOf(def.Id);
        e.Faction = Faction.Enemy;
        e.SpawnTick = World.Tick - 1;
        e.Life = life;
        e.Pos = e.PrevPos = pos;
        e.Radius = def.Radius ?? MathF.Round(def.Sprites.Def.BaseRadius * def.Scale);
        e.Hp = e.MaxHp = def.Hp;
        e.Armor = def.Armor;
        e.KnockbackResist = def.KnockbackResist;
        e.Immunities = Immunities.FromNames(def.Immunities);
        e.MoveSpeed = def.Speed;
        e.Intent = MoveIntent.SeekHero();
        // 생성 때 WP4 가 쓰는 값을 흉내(4.11·6.6): 몸 높이 × 크기, 닿은 거리 여분은 touch_margin(근접 부품 없음)
        e.BodyTop = def.Sprites.Def.BodyHeight * def.Scale;
        e.TouchReach = World.Defs.Rules.Sight?.TouchMargin ?? 0f;
        return e;
    }
}
