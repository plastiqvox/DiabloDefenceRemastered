namespace DiabloDefenceRemastered.Tests.Common;

/// <summary>단계마다 (단계, 그때 World.Tick) 를 적는 시스템.</summary>
internal sealed class PhaseRecorder(SimPhase phase, List<(SimPhase Phase, int Tick)> log) : ISimSystem
{
    public void Tick(World world) => log.Add((phase, world.Tick));
}

public class SimulationTests
{
    [Fact]
    public void EmptyWorld_1000Ticks_RunPhasesInOrder()
    {
        var log = new List<(SimPhase Phase, int Tick)>();
        var builder = new TestWorldBuilder();
        // v0.7.1: RoomFlow 는 CreateBuilder 가 이미 넣으므로(두 번째 등록 예외) 시험 시스템은 Director..Pickups 에만
        for (var p = SimPhase.Director; p < SimPhase.RoomFlow; p++)
            builder.WithSystem(p, new PhaseRecorder(p, log));
        var t = builder.Build();

        t.Step(1000);

        Assert.Equal(1000, t.World.Tick);
        Assert.Equal(14 * 1000, log.Count);
        var i = 0;
        for (var tick = 0; tick < 1000; tick++)
            for (var p = SimPhase.Director; p < SimPhase.RoomFlow; p++)
            {
                Assert.Equal((p, tick), log[i]);   // 가운데 단계는 BeginTick 뒤·EndTick 앞(시계는 EndTick 끝에서만 늘어남)
                i++;
            }
        Assert.Equal(RunOutcome.Running, t.World.Outcome);
        Assert.Equal(0, t.World.Enemies.Count);
    }

    [Fact]
    public void AllSeventeenPhases_InEnumOrder()
    {
        var log = new List<(SimPhase Phase, int Tick)>();
        var builder = new SimulationBuilder();
        foreach (var p in Enum.GetValues<SimPhase>()) builder.Add(p, new PhaseRecorder(p, log));
        var world = new TestWorldBuilder().Build().World;
        var sim = builder.Build(world);

        sim.Run(3);

        Assert.Equal(17, Enum.GetValues<SimPhase>().Length);
        Assert.Equal(17, SimulationBuilder.PhaseCount);
        Assert.Equal(SimulationBuilder.PhaseCount, Enum.GetValues<SimPhase>().Length);
        var expected = Enumerable.Range(0, 3).SelectMany(_ => Enum.GetValues<SimPhase>()).ToList();
        Assert.Equal(expected, log.Select(x => x.Phase).ToList());
        Assert.Equal(SimPhase.BeginTick, (SimPhase)0);
        Assert.Equal(SimPhase.RoomFlow, (SimPhase)15);   // v0.7: 16번째 단계(0부터 15) — EndTick 바로 앞
        Assert.Equal(SimPhase.EndTick, (SimPhase)16);
        Assert.Equal(SimPhase.EndTick, SimPhase.RoomFlow + 1);
    }

    [Fact]
    public void TwoSystemsInOnePhase_Throws()
    {
        var builder = Simulation.CreateBuilder();
        var log = new List<(SimPhase, int)>();
        builder.Add(SimPhase.Weapons, new PhaseRecorder(SimPhase.Weapons, log));
        Assert.Throws<InvalidOperationException>(() => builder.Add(SimPhase.Weapons, new PhaseRecorder(SimPhase.Weapons, log)));
        // BeginTick·RoomFlow(v0.7)·EndTick 은 이미 계약 시스템이 차지한다
        Assert.Throws<InvalidOperationException>(() => builder.Add(SimPhase.BeginTick, new PhaseRecorder(SimPhase.BeginTick, log)));
        Assert.Throws<InvalidOperationException>(() => builder.Add(SimPhase.RoomFlow, new PhaseRecorder(SimPhase.RoomFlow, log)));
        Assert.Throws<InvalidOperationException>(() =>
            new TestWorldBuilder()
                .WithSystem(SimPhase.Director, new PhaseRecorder(SimPhase.Director, log))
                .WithSystem(SimPhase.Director, new PhaseRecorder(SimPhase.Director, log))
                .Build());
    }

    [Fact]
    public void BeginTick_SwapsEvents_AndSavesPrevPos()
    {
        var t = new TestWorldBuilder().Build();
        var e = t.AddEnemy(new Vector2(100, 100));
        t.World.Events.Current.XpGained.Add(new XpGained(3, 3));
        e.Pos = new Vector2(110, 100);

        t.Step();

        Assert.Equal(0, t.World.Events.Current.XpGained.Count);
        Assert.Equal(1, t.World.Events.Previous.XpGained.Count);   // 다음 틱 전체에서 읽힌다
        Assert.Equal(new Vector2(110, 100), e.PrevPos);

        t.Step();
        Assert.Equal(0, t.World.Events.Previous.XpGained.Count);   // 두 틱 뒤에는 사라진다
    }

    [Fact]
    public void EndTick_CompactsRemovedEntities()
    {
        var t = new TestWorldBuilder().Build();
        var a = t.AddEnemy(new Vector2(100, 100));
        var b = t.AddEnemy(new Vector2(200, 100));
        t.World.Enemies.Remove(a.Id);
        Assert.Equal(2, t.World.Enemies.Count);
        t.Step();
        Assert.Equal(1, t.World.Enemies.Count);
        Assert.Same(b, t.World.Enemies[0]);
    }

    [Fact]
    public void Profiler_SeesEveryPhaseThatRuns()
    {
        var t = new TestWorldBuilder().Build();
        var seen = new List<SimPhase>();
        t.Simulation.Profiler = new RecordingProfiler(seen);
        t.Step();
        Assert.Equal([SimPhase.BeginTick, SimPhase.RoomFlow, SimPhase.EndTick], seen);   // CreateBuilder 기본 등록 셋(v0.7.1)
    }

    private sealed class RecordingProfiler(List<SimPhase> seen) : ISimProfiler
    {
        public void Begin(SimPhase phase) => seen.Add(phase);
        public void End(SimPhase phase) { }
    }
}
