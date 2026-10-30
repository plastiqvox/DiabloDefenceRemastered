namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>묶음 모듈. <see cref="Modules.GameModules"/> 가 목록을 가지고 순서대로 <see cref="Register"/> 를 부른다.</summary>
public interface IGameModule
{
    void Register(ModuleContext ctx);
}

/// <summary>
/// 모듈이 등록하는 곳 (설계서 2.1 레지스트리). 부품·검사기·월드 설치·개발 옵션 적용을 모은다.
/// 로더(WP7)·게임(GameApp)·시험이 같은 목록을 쓴다.
/// </summary>
public sealed class ModuleContext
{
    private readonly List<IDataValidator> _validators = [];
    private readonly List<Action<World, SimulationBuilder>> _installers = [];
    private readonly List<IRunSetup> _runSetups = [];

    public PartRegistry Parts { get; } = new();

    public IReadOnlyList<IDataValidator> Validators => _validators;
    public IReadOnlyList<IRunSetup> RunSetups => _runSetups;

    public void AddValidator(IDataValidator validator) => _validators.Add(validator);

    /// <summary>
    /// 월드를 만들 때 부른다: 모듈이 월드 서비스(Nav·Spatial·Hostiles·Spawner·Progression …)를 넣고 자기 단계 시스템을 등록한다.
    /// 등록 순서 = 모듈 순서.
    /// </summary>
    public void AddWorldInstaller(Action<World, SimulationBuilder> installer) => _installers.Add(installer);

    public void AddRunSetup(IRunSetup setup) => _runSetups.Add(setup);

    /// <summary>등록된 월드 설치를 순서대로 부른다(시험이 자기 시스템을 더 넣을 때).</summary>
    public void Install(World world, SimulationBuilder builder)
    {
        foreach (var install in _installers) install(world, builder);
    }

    /// <summary>BeginTick·EndTick 과 모듈 시스템을 넣어 시뮬레이션을 만든다.</summary>
    public Simulation CreateSimulation(World world)
    {
        var builder = Simulation.CreateBuilder();
        Install(world, builder);
        return builder.Build(world);
    }

    /// <summary>개발 옵션을 <c>World.Options</c> 에 넣고 등록 순서대로 적용한다(첫 틱 전).</summary>
    public void ApplyRunSetups(World world, RunOptions options)
    {
        world.Options = options;
        foreach (var setup in _runSetups) setup.Apply(world, options);
    }
}
