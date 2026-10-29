using DiabloDefenceRemastered.Core.Sim.Core;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 스테이지 틱 하나 = <see cref="SimPhase"/> 순서대로 <see cref="ISimSystem.Tick"/> (설계서 3.5). 빈 단계는 건너뛴다.
/// 만들기는 <see cref="CreateBuilder"/>(BeginTick·EndTick 포함) → 모듈 설치 → <see cref="SimulationBuilder.Build"/>.
/// </summary>
public sealed class Simulation
{
    private readonly ISimSystem?[] _systems;

    internal Simulation(World world, ISimSystem?[] systems)
    {
        World = world;
        _systems = systems;
        world.Simulation = this;
    }

    public World World { get; }

    /// <summary>단계별 시간 재기(--log-ticks). 없으면 재지 않는다.</summary>
    public ISimProfiler? Profiler { get; set; }

    /// <summary>BeginTick·EndTick 이 들어간 빌더.</summary>
    public static SimulationBuilder CreateBuilder() =>
        new SimulationBuilder()
            .Add(SimPhase.BeginTick, new BeginTickSystem())
            .Add(SimPhase.EndTick, new EndTickSystem());

    public ISimSystem? SystemAt(SimPhase phase) => _systems[(int)phase];

    /// <summary>스테이지 틱 하나를 돈다. 보통은 <see cref="StageStepper.AppTick"/> 를 거친다(멈춤·느려짐 규칙).</summary>
    public void Step()
    {
        var profiler = Profiler;
        for (var i = 0; i < _systems.Length; i++)
        {
            var system = _systems[i];
            if (system is null) continue;
            if (profiler is null)
            {
                system.Tick(World);
                continue;
            }
            var phase = (SimPhase)i;
            profiler.Begin(phase);
            system.Tick(World);
            profiler.End(phase);
        }
    }

    public void Run(int ticks)
    {
        for (var i = 0; i < ticks; i++) Step();
    }
}
