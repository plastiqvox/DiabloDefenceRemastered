namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 시험 월드 만들기 (WP1 TestKit, 설계서 10.1). 코드로 만든 작은 맵·정의 + 전수 공간 질의 + 직선 내비.
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
    private Vector2 _mapSize = new(1536, 1024);
    private GameDefs? _defs;
    private VictoryDef? _victory;
    private bool _presentation;
    private readonly List<(SimPhase Phase, Func<World, ISimSystem> Create)> _systems = [];
    private readonly List<Action<World, SimulationBuilder>> _installers = [];
    private readonly List<Action<World>> _setups = [];

    public TestWorldBuilder WithSeed(ulong seed) { _seed = seed; return this; }

    /// <summary>기본 정의(<see cref="TestDefs.Basic"/>)의 맵 크기.</summary>
    public TestWorldBuilder WithMapSize(float width, float height) { _mapSize = new Vector2(width, height); return this; }

    /// <summary>정의 묶음을 통째로 바꾼다. 첫 스테이지가 있으면 그것으로 월드를 만든다.</summary>
    public TestWorldBuilder WithDefs(GameDefs defs) { _defs = defs; return this; }

    public TestWorldBuilder WithVictory(VictoryDef victory) { _victory = victory; return this; }

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
        var defs = _defs ?? TestDefs.Basic(_mapSize, _victory);
        var stage = defs.Stages.Count > 0 ? defs.Stages[0] : null;
        var world = new World(defs, _seed, stage);
        if (stage is null) world.MapSize = _mapSize;
        world.Presentation.Enabled = _presentation;
        world.Nav = new StraightNav(world.MapSize);
        world.Spatial = new BruteForceSpatial(world);
        foreach (var setup in _setups) setup(world);

        var builder = Simulation.CreateBuilder();
        foreach (var install in _installers) install(world, builder);
        foreach (var (phase, create) in _systems) builder.Add(phase, create(world));
        return new TestWorld(world, builder.Build(world));
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
        return e;
    }
}
