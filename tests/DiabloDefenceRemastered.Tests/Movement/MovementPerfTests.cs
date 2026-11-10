using System.Diagnostics;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Movement;

/// <summary>
/// 그래픽 없는 측정 (설계서 4.9·10.3, 11.1 WP2 완료 기준). <c>Perf</c> 분류 — <c>DDR_RUN=Perf</c> 일 때만 돈다. 합격 숫자는 Release 기준이다
/// (Debug 로 돌리면 숫자만 적고 한계를 4배로 본다).
/// ① 실제 <c>cathedral_arena</c>(안쪽 방 배치)에 적 500 을 흩어 플로우로 주인공에게 모으는 3,600틱(워밍업 600틱 뒤): Navigation+Locomotion+Collision 평균 ≤ 1.0ms, 할당 0, 불변식 기준.
/// ② NavModule 설치(충돌 도형·색인·clearance·첫 필드): 큰 방(N 40)·작은 방(N 32 합성) 각 ≤ 150ms 중 내비 몫.
/// </summary>
public class MovementPerfTests
{
#if DEBUG
    private const float Slack = 4f;
#else
    private const float Slack = 1f;
#endif

    private readonly ITestOutputHelper _out;

    public MovementPerfTests(ITestOutputHelper output) => _out = output;

    /// <summary>실제 맵 + NavModule 시험 월드(합성 적·직업, 실제 rules.sight, 빛 420).</summary>
    internal static TestWorld CathedralNavWorld()
    {
        var basic = TestDefs.Basic();
        var map = RealData.Cathedral;
        var stage = TestDefs.Stage(map, lighting: new LightingDef { HeroLight = new HeroLightDef { Radius = 420f } });
        var defs = new GameDefs
        {
            Rules = basic.Rules with { Sight = RealData.Sight() },
            Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Weapons = basic.Weapons, Passives = basic.Passives,
            HeroClasses = basic.HeroClasses, Enemies = basic.Enemies,
            Tilesets = new DefTable<TilesetDef>([RealData.L1]), Maps = new DefTable<MapDef>([map]), Stages = new DefTable<StageDef>([stage]),
        };
        var ctx = new ModuleContext();
        new NavModule().Register(ctx);
        return new TestWorldBuilder().WithDefs(defs).WithModules(ctx).Build();
    }

    [Fact, Trait("Category", TestCategories.Perf)]
    public void Flow500_3600Ticks_NavLocoCollision()
    {
        TestCategories.RequireEnabled(TestCategories.Perf);
        var t = CathedralNavWorld();
        var nav = (NavWorld)t.World.Nav;
        var rng = new Random(1);
        var box = t.World.MapSize;
        while (t.World.Enemies.Count < 500)
        {
            var p = new Vector2((float)rng.NextDouble() * box.X, (float)rng.NextDouble() * box.Y);
            if (nav.ExactClearance(p) < 16f || Vector2.Distance(p, t.Hero.Pos) < 200f) continue;
            t.AddEnemy(p).Intent = MoveIntent.SeekHero();
        }
        var checker = new Core.Sim.Movement.Invariants.InvariantChecker(t.World, nav) { Enabled = true };
        var profiler = new PhaseClock();
        // 주인공은 가운데 둘레를 천천히 돈다(필드 갱신이 있는 틱을 넣고, 무리가 한 점에 굳지 않게). 의도는 WP3 대신 시험이 쓴다
        var hero = t.Hero;
        var center = hero.Pos;
        hero.MoveSpeed = 60f;
        void Steer(int tick)
        {
            var target = center + MathX.FromAngle(tick * World.Dt * 0.25f) * new Vector2(160f, 80f);
            hero.Intent = MoveIntent.SeekPoint(target);
        }
        for (var i = 0; i < 600; i++)
        {
            Steer(i);
            t.Step();
        }
        t.Simulation.Profiler = profiler;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 3600; i++)
        {
            Steer(600 + i);
            t.Step();
            if (i % 60 == 0) checker.Sample();
        }
        var alloc = GC.GetAllocatedBytesForCurrentThread() - before;
        t.Simulation.Profiler = null;
        var nlc = profiler.MeanMs(SimPhase.Navigation) + profiler.MeanMs(SimPhase.Locomotion) + profiler.MeanMs(SimPhase.Collision);
        _out.WriteLine($"Navigation {profiler.MeanMs(SimPhase.Navigation):0.000}ms Locomotion {profiler.MeanMs(SimPhase.Locomotion):0.000}ms " +
                       $"Collision {profiler.MeanMs(SimPhase.Collision):0.000}ms SpatialRebuild {profiler.MeanMs(SimPhase.SpatialRebuild):0.000}ms 합 {nlc:0.000}ms 할당 {alloc}B");
        _out.WriteLine(t.World.Invariants.Format(t.World.Fairness));
        Assert.True(nlc <= 1.0f * Slack, $"Navigation+Locomotion+Collision 평균 {nlc:0.000}ms > {1.0f * Slack}");
        Assert.Equal(0, alloc);
        // 500 은 절대 상한(360)을 일부러 넘는 측정 규모라 over_cap 은 보지 않는다
        var r = t.World.Invariants;
        Assert.True(r.Nan == 0 && r.OutOfMap == 0 && r.InObstacleLongest <= 1 && r.StuckMax <= r.StuckMaxAlive * 0.02f, r.Format(t.World.Fairness));
    }

    [Fact, Trait("Category", TestCategories.Perf)]
    public void Install_BigAndSmallRoom_Under150ms()
    {
        TestCategories.RequireEnabled(TestCategories.Perf);
        // 처음 한 번은 JIT — 버리고 잰다
        var big = CathedralNavWorld().World;
        _ = NavWorld.Build(big);
        var small = new TestWorldBuilder().WithMegaTiles(16).Build().World;   // 작은 방 N 32 합성
        foreach (var (name, world) in new[] { ("cathedral_arena N40", big), ("합성 N32", small) })
        {
            var sw = Stopwatch.StartNew();
            const int runs = 5;
            for (var i = 0; i < runs; i++) _ = NavWorld.Build(world);
            var ms = sw.Elapsed.TotalMilliseconds / runs;
            _out.WriteLine($"{name}: NavWorld.Build {ms:0.0}ms");
            Assert.True(ms <= 150.0 * Slack, $"{name} 설치 {ms:0.0}ms");
        }
    }

    private sealed class PhaseClock : ISimProfiler
    {
        private readonly long[] _start = new long[SimulationBuilder.PhaseCount];
        private readonly long[] _total = new long[SimulationBuilder.PhaseCount];
        private readonly int[] _count = new int[SimulationBuilder.PhaseCount];

        public void Begin(SimPhase phase) => _start[(int)phase] = Stopwatch.GetTimestamp();

        public void End(SimPhase phase)
        {
            _total[(int)phase] += Stopwatch.GetTimestamp() - _start[(int)phase];
            _count[(int)phase]++;
        }

        public float MeanMs(SimPhase phase) =>
            _count[(int)phase] == 0 ? 0f : (float)(_total[(int)phase] * 1000.0 / Stopwatch.Frequency / _count[(int)phase]);
    }
}
