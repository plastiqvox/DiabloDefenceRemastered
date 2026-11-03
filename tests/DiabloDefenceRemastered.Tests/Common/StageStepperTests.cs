namespace DiabloDefenceRemastered.Tests.Common;

/// <summary>레벨업·상자·보상(v0.7) 대기만 흉내 내는 성장. <see cref="Unavailable"/> 에 넣은 보상 ID 는 <see cref="RewardAvailable"/> 가 거짓.</summary>
internal sealed class FakeProgression(World world) : IProgression
{
    public int Level { get; set; } = 1;
    public int Xp => 0;
    public int XpToNext => 5;
    public int PendingLevelUps { get; set; }
    public int PendingChests { get; set; }
    public int PendingRewards { get; set; }
    public HashSet<string> Unavailable { get; } = new(StringComparer.Ordinal);

    public LevelUpOffer Offer() => new(Level + 1, [], 3);

    public void Choose(int index)
    {
        PendingLevelUps--;
        Level++;
        if (PendingLevelUps == 0 && PendingChests == 0 && PendingRewards == 0) world.AwaitingChoice = ChoiceKind.None;
    }

    public void Reroll() { }
    public void Skip() => Choose(-1);
    public ChestReward OpenChest() => new([]);

    public LevelUpOffer RewardOffer() => new(Level, [], 3, OfferSource.Tome);

    public void ChooseReward(int index)
    {
        PendingRewards--;
        if (PendingLevelUps == 0 && PendingChests == 0 && PendingRewards == 0) world.AwaitingChoice = ChoiceKind.None;
    }

    public void RerollReward() { }
    public void SkipReward() => ChooseReward(-1);
    public bool RewardAvailable(RewardDef reward) => !Unavailable.Contains(reward.Id);
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
    public void RoomExit_DoesNotStep()
    {
        var t = new TestWorldBuilder().WithRoom(RoomPhase.Exit).Build();
        Assert.Equal(StageStep.RoomExit, t.AppTick());
        Assert.Equal(StageStep.RoomExit, t.AppTick());
        Assert.Equal(0, t.World.Tick);
    }

    [Fact]
    public void StepOrder_FinishedThenChoiceThenRoomExit()
    {
        // v0.7.1: Exit + 선택 대기 → AwaitingChoice, 끝난 판 → Finished
        var t = new TestWorldBuilder().WithRoom(RoomPhase.Exit).Build();
        t.World.AwaitingChoice = ChoiceKind.LevelUp;
        Assert.Equal(StageStep.AwaitingChoice, t.AppTick());
        t.World.Outcome = RunOutcome.Defeat;
        Assert.Equal(StageStep.Finished, t.AppTick());
        t.World.Outcome = RunOutcome.Running;
        t.World.AwaitingChoice = ChoiceKind.None;
        t.World.Freeze(5);
        Assert.Equal(StageStep.RoomExit, t.AppTick());   // 히트 스톱보다 앞
        Assert.Equal(5, t.World.FreezeTicks);
    }

    [Fact]
    public void PlayerCommandDefaults_HaveNoExitFlag()
    {
        Assert.Equal(CommandButtons.None, PlayerCommand.None.Buttons & CommandButtons.Exit);
        Assert.Equal(CommandButtons.None, default(PlayerCommand).Buttons & CommandButtons.Exit);
        Assert.Equal(CommandButtons.None, new PlayerCommand().Buttons & CommandButtons.Exit);
        Assert.Equal(0, PlayerCommand.None.ExitIndex);
        // 기존 생성 호출(이동·버튼)은 그대로 컴파일되고 깃발이 없다
        Assert.Equal(CommandButtons.None, new PlayerCommand(Vector2.UnitX, CommandButtons.Confirm).Buttons & CommandButtons.Exit);
    }

    [Fact]
    public void WorldWithoutSimulation_Throws()
    {
        var world = new World(TestDefs.Basic(), 1);
        Assert.Throws<InvalidOperationException>(() => StageStepper.AppTick(world, default));
    }
}

public class EndTickTests
{
    // v0.7: survive 승리는 없앴다(판의 끝 = 마지막 방 — RoomFlow·DeathHooks 가 BeginVictory). 승리 지연은 BeginVictory 를 직접 불러 본다.
    [Fact]
    public void BeginVictory_CollapsesEnemies_ThenVictoryAfter3s()
    {
        var t = new TestWorldBuilder().Build();
        var e = t.AddEnemy(new Vector2(100, 100));
        var proj = t.World.Projectiles.Add();
        proj.Faction = Faction.Enemy;
        var heroProj = t.World.Projectiles.Add();
        heroProj.Faction = Faction.Hero;

        t.Step(59);
        Assert.Equal(RunOutcome.Running, t.World.Outcome);
        t.World.BeginVictory();                           // 60번째 틱 앞(앞 단계가 부른 것처럼)
        Assert.Equal(RunOutcome.VictoryPending, t.World.Outcome);
        Assert.True(e.Dead);
        Assert.Equal(KillCause.Collapse, e.KillCause);
        t.Step();
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

        // 같은 틱에 앞 단계가 승리를 시작했으면 주인공 HP 가 0 이어도 승리(EndTick 은 Running 일 때만 패배로)
        var v = new TestWorldBuilder().WithSystem(SimPhase.DeathHooks, new BeginVictoryAt(0)).Build();
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

    [Fact]
    public void ChoiceOrder_LevelUpThenChestThenReward()
    {
        var t = new TestWorldBuilder().Build();
        var prog = new FakeProgression(t.World) { PendingLevelUps = 1, PendingChests = 1, PendingRewards = 1 };
        t.World.Progression = prog;
        t.Step();
        Assert.Equal(ChoiceKind.LevelUp, t.World.AwaitingChoice);
        prog.PendingLevelUps = 0;
        t.World.AwaitingChoice = ChoiceKind.None;
        t.Step();
        Assert.Equal(ChoiceKind.Chest, t.World.AwaitingChoice);
        prog.PendingChests = 0;
        t.World.AwaitingChoice = ChoiceKind.None;
        t.Step();
        Assert.Equal(ChoiceKind.Reward, t.World.AwaitingChoice);
    }

    [Fact]
    public void RewardPhase_ClearsEnemyAttacks_EachTick()
    {
        // 형의 RoomFlowSystem 이 단계를 바꾸지 않던 자리 — 구현 뒤에도 보상 단계는 대기 물체가 있으면 그대로다(FakeSpawner 가 보상 물체를 놓지 않게 보상 없음)
        var t = new TestWorldBuilder().WithRoom(RoomPhase.Reward).Build();
        var chest = t.World.Pickups.Add();
        chest.Def = t.World.Defs.Pickups.Get(TestDefs.EliteChestId);
        chest.GatesExit = true;
        var proj = t.World.Projectiles.Add();
        proj.Faction = Faction.Enemy;
        var hz = t.World.Hazards.Add();
        hz.Faction = Faction.Enemy;
        var heroProj = t.World.Projectiles.Add();
        heroProj.Faction = Faction.Hero;

        t.Step();
        Assert.Equal(RoomPhase.Reward, t.World.Room!.Phase);
        Assert.Equal(1, t.World.Projectiles.Count);
        Assert.Same(heroProj, t.World.Projectiles[0]);
        Assert.Equal(0, t.World.Hazards.Count);
    }

    private sealed class BeginVictoryAt(int tick) : ISimSystem
    {
        public void Tick(World world)
        {
            if (world.Tick == tick) world.BeginVictory();
        }
    }
}
