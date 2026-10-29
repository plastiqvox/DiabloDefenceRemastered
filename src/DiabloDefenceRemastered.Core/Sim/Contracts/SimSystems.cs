namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 스테이지 틱 하나의 단계 순서 (설계서 3.5 표가 정본). 한 단계 = 시스템 하나. 단계를 더하려면 WP1 에 요청한다(11.3).
/// </summary>
public enum SimPhase : byte
{
    BeginTick,      // 1  WP1
    Director,       // 2  WP4
    HeroControl,    // 3  WP3
    Navigation,     // 4  WP2
    EnemyThink,     // 5  WP4
    Locomotion,     // 6  WP2
    Collision,      // 7  WP2
    SpatialRebuild, // 8  WP2
    Weapons,        // 9  WP3
    Projectiles,    // 10 WP3
    Hazards,        // 11 WP3
    DamageResolve,  // 12 WP3
    DeathHooks,     // 13 WP4
    DeathRewards,   // 14 WP3
    Pickups,        // 15 WP3
    EndTick,        // 16 WP1
}

/// <summary>시스템 하나. 상태가 있으면 월드마다 따로 만든다(<see cref="ModuleContext.AddWorldInstaller"/>).</summary>
public interface ISimSystem
{
    void Tick(World world);
}

/// <summary>단계별 시간 재기(TickProfiler, WP7). 없으면 재지 않는다.</summary>
public interface ISimProfiler
{
    void Begin(SimPhase phase);
    void End(SimPhase phase);
}

/// <summary>단계 → 시스템 짝을 모은다. 같은 단계에 두 번째 시스템을 넣으면 예외.</summary>
public sealed class SimulationBuilder
{
    public const int PhaseCount = 16;

    private readonly ISimSystem?[] _systems = new ISimSystem?[PhaseCount];

    public SimulationBuilder Add(SimPhase phase, ISimSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        var i = (int)phase;
        if ((uint)i >= PhaseCount) throw new ArgumentOutOfRangeException(nameof(phase));
        if (_systems[i] is { } existing)
            throw new InvalidOperationException(
                $"단계 {phase} 에 시스템이 이미 있다({existing.GetType().Name}). 한 단계 = 시스템 하나({system.GetType().Name} 등록 거부)");
        _systems[i] = system;
        return this;
    }

    public bool Has(SimPhase phase) => _systems[(int)phase] is not null;

    public ISimSystem? Get(SimPhase phase) => _systems[(int)phase];

    /// <summary>빈 단계는 건너뛴다(묶음이 아직 없는 시험 월드).</summary>
    public Simulation Build(World world) => new(world, (ISimSystem?[])_systems.Clone());
}
