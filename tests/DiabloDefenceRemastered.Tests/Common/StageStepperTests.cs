namespace DiabloDefenceRemastered.Tests.Common;

/// <summary>레벨업 대기만 흉내 내는 성장.</summary>
internal sealed class FakeProgression(World world) : IProgression
{
    public int Level { get; set; } = 1;
    public int Xp => 0;
    public int XpToNext => 5;
    public int PendingLevelUps { get; set; }
    public int PendingChests { get; set; }

    public LevelUpOffer Offer() => new(Level + 1, [], 3);

    public void Choose(int index)
    {
        PendingLevelUps--;
        Level++;
        if (PendingLevelUps == 0 && PendingChests == 0) world.AwaitingChoice = ChoiceKind.None;
    }

    public void Reroll() { }
    public void Skip() => Choose(-1);
    public ChestReward OpenChest() => new([]);
}

public class StageStepperTests
{
    [Fact]
    public void Steps_OnePerAppTick_AndPassesCommand()
    {
        var t = new TestWorldBuilder().Build();
        var cmd = new PlayerCommand(new Vector2(1, 0));
        Assert.Equal(StageStep.Stepped, t.AppTick(cmd));
        Assert.Equal(1, t.World.Tick);
        Assert.Equal(cmd, t.World.Command);
    }

    [Fact]
    public void HitStop_HoldsStageClock()
    {
        var t = new TestWorldBuilder().Build();
        t.AppTick();
        t.World.Freeze(5);
        for (var i = 0; i < 5; i++) Assert.Equal(StageStep.Frozen, t.AppTick());
        Assert.Equal(1, t.World.Tick);
        Assert.Equal(StageStep.Stepped, t.AppTick());
        Assert.Equal(2, t.World.Tick);
    }

    [Fact]
    public void SlowMotion_StretchesTicks()
    {
        var t = new TestWorldBuilder().Build();
        t.World.SlowMotion(0.3f, 30);
        var stepped = 0;
        var alphas = new List<float>();
        for (var i = 0; i < 30; i++)
        {
            if (t.AppTick() == StageStep.Stepped) stepped++;
            alphas.Add(StageStepper.Alpha(t.World, 0.5f));
        }
        Assert.Equal(9, stepped);                       // 30 × 0.3
        Assert.All(alphas.Take(29), a => Assert.InRange(a, 0f, 1f));
        Assert.Equal(9, t.World.Tick);

        // 끝나면 다시 앱 틱마다 한 번, alpha 는 누적기 값
        Assert.Equal(StageStep.Stepped, t.AppTick());
        Assert.Equal(0.5f, StageStepper.Alpha(t.World, 0.5f));
    }

    [Fact]
    public void AwaitingChoice_RaisedAtEndTick_AndBlocksSteps()
    {
        var t = new TestWorldBuilder().Build();
        var prog = new FakeProgression(t.World);
        t.World.Progression = prog;

        prog.PendingLevelUps = 2;
        Assert.Equal(StageStep.Stepped, t.AppTick());
        Assert.Equal(ChoiceKind.LevelUp, t.World.AwaitingChoice);   // 같은 스테이지 틱 끝에서 켜진다
        var tick = t.World.Tick;

        Assert.Equal(StageStep.AwaitingChoice, t.AppTick());
        Assert.Equal(tick, t.World.Tick);

        prog.Choose(0);
        Assert.Equal(ChoiceKind.LevelUp, t.World.AwaitingChoice);   // 하나 남음
        prog.Choose(1);
        Assert.Equal(ChoiceKind.None, t.World.AwaitingChoice);
        Assert.Equal(StageStep.Stepped, t.AppTick());
        Assert.Equal(tick + 1, t.World.Tick);
    }

    [Fact]
    public void ChestPending_RaisesChestChoice()
    {
        var t = new TestWorldBuilder().Build();
        var prog = new FakeProgression(t.World) { PendingChests = 1 };
        t.World.Progression = prog;
        t.AppTick();
        Assert.Equal(ChoiceKind.Chest, t.World.AwaitingChoice);
    }

    [Fact]
    public void FinishedRun_DoesNotStep()
    {
        var t = new TestWorldBuilder().Build();
        t.World.Outcome = RunOutcome.Victory;
        Assert.Equal(StageStep.Finished, t.AppTick());
        Assert.Equal(0, t.World.Tick);
    }

    [Fact]
    public void WorldWithoutSimulation_Throws()
    {
        var world = new World(TestDefs.Basic(new Vector2(960, 540)), 1);
        Assert.Throws<InvalidOperationException>(() => StageStepper.AppTick(world, default));
    }
}

public class EndTickTests
{
    [Fact]
    public void SurviveVictory_AtTime_CollapsesEnemies_ThenVictoryAfter3s()
    {
        var t = new TestWorldBuilder()
            .WithVictory(new VictoryDef { Type = VictoryKind.Survive, At = new ClockTime(1f) })
            .Build();
        var e = t.AddEnemy(new Vector2(100, 100));
        var proj = t.World.Projectiles.Add();
        proj.Faction = Faction.Enemy;
        var heroProj = t.World.Projectiles.Add();
        heroProj.Faction = Faction.Hero;

        t.Step(59);
        Assert.Equal(RunOutcome.Running, t.World.Outcome);
        t.Step();                                         // 60틱 = 1초째 틱 끝
        Assert.Equal(RunOutcome.VictoryPending, t.World.Outcome);
        Assert.True(e.Dead);
        Assert.Equal(KillCause.Collapse, e.KillCause);
        Assert.Equal(1, t.World.Projectiles.Count);       // 적 투사체만 지워짐
        Assert.Same(heroProj, t.World.Projectiles[0]);

        t.Step(178);                                      // 대기 틱 179개
        Assert.Equal(RunOutcome.VictoryPending, t.World.Outcome);
        t.Step();                                         // 180틱 = 3초
        Assert.Equal(RunOutcome.Victory, t.World.Outcome);
    }

    [Fact]
    public void HeroDeath_DefeatAfter2s_VictoryWinsSameTick()
    {
        var t = new TestWorldBuilder().Build();
        t.Step();
        t.Hero.Dead = true;
        t.Step();
        Assert.Equal(RunOutcome.DefeatPending, t.World.Outcome);
        t.Step(119);
        Assert.Equal(RunOutcome.Defeat, t.World.Outcome);

        var v = new TestWorldBuilder()
            .WithVictory(new VictoryDef { Type = VictoryKind.Survive, At = new ClockTime(1f / 60f) })
            .Build();
        v.Hero.Dead = true;
        v.Step();
        Assert.Equal(RunOutcome.VictoryPending, v.World.Outcome);
    }

    [Fact]
    public void BeginVictory_OnlyFromRunning()
    {
        var t = new TestWorldBuilder().Build();
        t.World.Outcome = RunOutcome.DefeatPending;
        t.World.BeginVictory();
        Assert.Equal(RunOutcome.DefeatPending, t.World.Outcome);
    }
}
