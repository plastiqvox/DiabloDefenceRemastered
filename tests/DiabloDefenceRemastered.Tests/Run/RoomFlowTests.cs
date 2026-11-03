using DiabloDefenceRemastered.Tests.Common;

namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>10.1 <c>Run</c> 행 — <c>RoomFlow</c>(표 3.6 의 줄마다, 합성 방 월드 <c>WithRoom</c> + <see cref="FakeSpawner"/>).</summary>
public class RoomFlowTests
{
    private const int Grace = 180;
    private static readonly RoomDef NextNormal = TestDefs.Room(RoomKind.Normal, 60f, 120f, 2);

    /// <summary>시험 단계에서 월드를 건드린다(앞 단계가 낸 틱 이벤트 흉내 — DeathHooks 자리).</summary>
    private sealed class Inject(Action<World> act) : ISimSystem
    {
        public void Tick(World world) => act(world);
    }

    private static RewardDef Reward(string id) => TestDefs.Basic().Rewards.Get(id);

    private static TestWorld Room(RoomPhase phase, int clock = 0, RoomDef? room = null, RoomDef? next = null, RewardDef? reward = null,
                                  bool last = false, string gateIn = "gate_se", Action<World>? inject = null)
    {
        var b = new TestWorldBuilder().WithRoom(phase, clock, room, next ?? (last ? null : NextNormal), gateIn, reward: reward, isLastInRun: last);
        if (inject is not null) b.WithSystem(SimPhase.DeathHooks, new Inject(inject));
        return b.Build();
    }

    private static Vector2 ZoneCell(TestWorld t, string gate)
    {
        var c = t.World.Gates[gate].Cells[0];
        return t.CellCenter(c.X, c.Y);
    }

    private static TestWorld AtDoors(RoomDef? next = null, FakeProgression? progression = null, Action<World>? inject = null)
    {
        var t = Room(RoomPhase.Reward, next: next, inject: inject);
        if (progression is not null) t.World.Progression = progression;
        t.Step();
        Assert.Equal(RoomPhase.Doors, t.World.Room!.Phase);
        return t;
    }

    [Fact]
    public void Enter_GraceThenSurvive_NoSpawningDuringGrace()
    {
        var t = Room(RoomPhase.Enter);
        var room = t.World.Room!;
        for (var i = 0; i < Grace - 1; i++)
        {
            Assert.False(room.SpawningOpen);
            t.Step();
        }
        Assert.Equal(RoomPhase.Enter, room.Phase);
        t.Step();
        Assert.Equal(RoomPhase.Survive, room.Phase);
        Assert.Equal(Grace, room.ClockTicks);
        Assert.True(room.SpawningOpen);
    }

    [Fact]
    public void NormalSurviveEnds_AtSurviveLength_ThenCleanup()
    {
        var t = Room(RoomPhase.Survive, clock: 3600 - 60);
        var room = t.World.Room!;
        Assert.Equal(3600, room.SurviveTicks);
        t.Step(59);
        Assert.Equal(RoomPhase.Survive, room.Phase);
        t.Step();
        Assert.Equal(RoomPhase.Cleanup, room.Phase);
        Assert.Equal(1, t.World.Events.Current.RoomSurviveEnded.Count);
        Assert.False(room.SpawningOpen);
    }

    [Fact]
    public void Cleanup_WaitsForEveryEnemy_IncludingEnteringAndSplits_ThenReward()
    {
        var t = Room(RoomPhase.Cleanup, clock: 3600, reward: Reward(TestDefs.RewardTomeId));
        var room = t.World.Room!;
        var entering = t.AddEnemy(new Vector2(300, 300), life: LifeState.Entering);
        t.Step(10);
        Assert.Equal(RoomPhase.Cleanup, room.Phase);
        entering.Dead = true;
        var split = t.AddEnemy(new Vector2(320, 300));   // 정리 중 분열체도 정리 대상
        t.Step(10);
        Assert.Equal(RoomPhase.Cleanup, room.Phase);
        split.Dead = true;
        t.Step();
        Assert.Equal(RoomPhase.Reward, room.Phase);
        Assert.False(room.Collapsed);
        Assert.Equal(1, t.World.Events.Current.RoomCleared.Count);
        Assert.Equal(new RoomCleared(RoomKind.Normal, false), t.World.Events.Current.RoomCleared[0]);
        // 보상 물체 하나가 center 에, 문을 막음
        var obj = Assert.Single(t.World.Pickups.Items.ToArray(), p => !p.Removed);
        Assert.Equal(TestDefs.RewardTomeId, obj.Reward!.Id);
        Assert.True(obj.GatesExit);
        Assert.Equal(t.World.Resolved!.Anchor("center"), obj.Pos);
    }

    [Fact]
    public void Cleanup_45s_CollapsesAndSetsCollapsed()
    {
        var t = Room(RoomPhase.Cleanup, clock: 3600);
        var room = t.World.Room!;
        var e = t.AddEnemy(new Vector2(300, 300));
        t.Step(45 * 60 - 1);
        Assert.Equal(RoomPhase.Cleanup, room.Phase);
        t.Step();
        Assert.Equal(RoomPhase.Reward, room.Phase);
        Assert.True(room.Collapsed);
        Assert.Equal(45 * 60, room.CleanupTicks);
        Assert.True(e.Dead);
        Assert.Equal(KillCause.Collapse, e.KillCause);
        Assert.True(t.World.Events.Current.RoomCleared[0].Collapsed);
    }

    [Fact]
    public void LastNormalRoom_CleanupEnd_IsVictory()
    {
        var t = Room(RoomPhase.Cleanup, clock: 3600, last: true);
        t.Step();
        Assert.Equal(RunOutcome.VictoryPending, t.World.Outcome);
        Assert.Equal(RoomPhase.Cleanup, t.World.Room!.Phase);     // 보상·문으로 가지 않음
        Assert.Equal(0, t.World.Events.Current.RoomCleared.Count);
    }

    [Fact]
    public void T_FreezesInCleanup_NormalRoom()
    {
        var t = Room(RoomPhase.Cleanup, clock: 3600, room: TestDefs.Room(RoomKind.Normal, 60f, 120f, 2));
        t.AddEnemy(new Vector2(300, 300));
        var end = (120f - World.Dt) / 60f;                          // [from, to) — t 끝 − 1틱(기획 v0.8.1 2.5)
        Assert.Equal(end, t.World.DifficultyMinutes, 5);
        Assert.True(t.World.DifficultyMinutes < 2f);                // 다음 방 구간(t 2:00~)을 가리키지 않음
        t.Step(600);
        Assert.Equal(end, t.World.DifficultyMinutes, 5);           // 정리 중 t 멈춤
        Assert.Equal(4200, t.World.Room!.ClockTicks);             // 방 시계는 감
    }

    [Fact]
    public void RewardObject_MustBeCollected_BeforeDoorsOpen()
    {
        foreach (var id in new[] { TestDefs.RewardTomeId, TestDefs.RewardPotionId })
        {
            var t = Room(RoomPhase.Cleanup, clock: 3600, reward: Reward(id));
            t.Hero.Hp = t.Hero.MaxHp = 100f;                       // 체력 가득이어도 물약은 문을 막는다(집히는 것은 WP3)
            t.Step();
            t.Step(120);
            Assert.Equal(RoomPhase.Reward, t.World.Room!.Phase);
            FakeSpawner.Collect(t.World, t.World.Pickups[0]);
            t.Step();
            Assert.Equal(RoomPhase.Doors, t.World.Room.Phase);
        }
    }

    [Fact]
    public void PendingChoices_OrMagnetizedPickups_KeepDoorsShut()
    {
        var t = Room(RoomPhase.Reward);
        var prog = new FakeProgression(t.World) { PendingRewards = 1 };
        t.World.Progression = prog;
        t.Step(5);
        Assert.Equal(RoomPhase.Reward, t.World.Room!.Phase);
        prog.PendingRewards = 0;
        prog.PendingChests = 1;
        t.Step();
        Assert.Equal(RoomPhase.Reward, t.World.Room.Phase);
        prog.PendingChests = 0;
        prog.PendingLevelUps = 1;
        t.Step();
        Assert.Equal(RoomPhase.Reward, t.World.Room.Phase);
        prog.PendingLevelUps = 0;
        t.World.AwaitingChoice = ChoiceKind.None;

        var gem = t.World.Pickups.Add();
        gem.Def = new PickupDef { Id = "test_gem", Kind = PickupKind.Xp, Draw = new PickupDrawDef { Procedural = "gem", Size = new Int2(8, 8) } };
        gem.Magnetized = true;                                     // 끌려오는 보석이 남아 있으면 Doors 아님
        t.Step(3);
        Assert.Equal(RoomPhase.Reward, t.World.Room.Phase);
        t.World.Pickups.Remove(gem.Id);
        t.Step();
        Assert.Equal(RoomPhase.Doors, t.World.Room.Phase);
    }

    [Fact]
    public void OpenDoors_FiltersByRewardAvailable_FillsWithFallback()
    {
        var all = new TestWorldBuilder().WithRoom(RoomPhase.Reward, next: NextNormal).Build();
        all.Step();
        Assert.Equal([TestDefs.RewardTomeId, TestDefs.RewardChestId], all.World.Room!.Exits.Select(e => e.Reward!.Id));   // 보상 표 순서(합성)
        Assert.Equal(["gate_nw", "gate_ne"], all.World.Room.Exits.Select(e => e.GateId));

        var t = Room(RoomPhase.Reward);
        var prog = new FakeProgression(t.World);
        prog.Unavailable.Add(TestDefs.RewardTomeId);
        t.World.Progression = prog;
        t.Step();
        Assert.Equal([TestDefs.RewardChestId, TestDefs.RewardPotionId], t.World.Room!.Exits.Select(e => e.Reward!.Id));

        var none = Room(RoomPhase.Reward);
        var p2 = new FakeProgression(none.World);
        p2.Unavailable.UnionWith([TestDefs.RewardTomeId, TestDefs.RewardChestId, TestDefs.RewardPotionId]);
        none.World.Progression = p2;
        none.Step();
        Assert.All(none.World.Room!.Exits, e => Assert.Equal(TestDefs.RewardChestId, e.Reward!.Id));   // fallback_reward(같은 종류 둘)
    }

    [Fact]
    public void OpenDoors_NextEliteOrBoss_ShowsKindAndName_LastOfActIsStairs()
    {
        var set = TestDefs.Basic().SpriteSets[0];
        var mb = TestDefs.Enemy(set, "mb");
        var elite = Room(RoomPhase.Reward, next: TestDefs.Room(RoomKind.Elite, 60f, 120f, 2, miniboss: mb));
        elite.Step();
        var e = elite.World.Room!.Exits[0];
        Assert.Equal(RoomKind.Elite, e.NextKind);
        Assert.Null(e.Reward);
        Assert.Equal("mb", e.NextBoss!.Id);

        var stairs = new TestWorldBuilder().WithRoom(RoomPhase.Reward, exits: ["gate_ne"]).Build();
        stairs.Step();
        Assert.True(Assert.Single(stairs.World.Room!.Exits).Stairs);
    }

    [Fact]
    public void Doors_ExitByWalkingIn_NotFromClosedOrEntryGate()
    {
        var t = AtDoors(NextNormal);
        var room = t.World.Room!;
        Assert.True(room.ExitArmed);                                // 주인공이 문 틈 밖(진입점)
        t.PlaceHero(ZoneCell(t, "gate_se"));                        // 들어온 문 틈
        t.Step(5);
        Assert.Equal(RoomPhase.Doors, room.Phase);
        t.PlaceHero(ZoneCell(t, "gate_sw"));                        // 닫힌 문 틈
        t.Step(5);
        Assert.Equal(RoomPhase.Doors, room.Phase);
        t.PlaceHero(t.World.Resolved!.Anchor("center"));           // 나가는 자리 밖
        t.Step(5);
        Assert.Equal(RoomPhase.Doors, room.Phase);
        t.PlaceHero(ZoneCell(t, "gate_ne"));
        t.Step();
        Assert.Equal(RoomPhase.Exit, room.Phase);
        Assert.Equal("gate_ne", room.ChosenExit);
        Assert.Equal(TestDefs.RewardChestId, room.ChosenExitInfo!.Value.Reward!.Id);
        Assert.Equal(StageStep.RoomExit, t.AppTick());
    }

    [Fact]
    public void Doors_StandingInGapWhenOpened_MustStepOutFirst()
    {
        var t = Room(RoomPhase.Reward);
        t.PlaceHero(ZoneCell(t, "gate_nw"));
        t.Step();
        var room = t.World.Room!;
        Assert.Equal(RoomPhase.Doors, room.Phase);
        Assert.False(room.ExitArmed);
        t.Step(30);
        Assert.Equal(RoomPhase.Doors, room.Phase);                 // 서 있기만 해서는 안 나감
        t.PlaceHero(t.World.Resolved!.Anchor("center"));
        t.Step();
        Assert.True(room.ExitArmed);
        t.PlaceHero(ZoneCell(t, "gate_nw"));
        t.Step();
        Assert.Equal(RoomPhase.Exit, room.Phase);
    }

    [Fact]
    public void Doors_PendingLevelUpOnSameTick_BlocksExit()
    {
        FakeProgression? prog = null;
        var raise = false;
        var t = AtDoors(NextNormal, inject: w => { if (raise && prog is not null) prog.PendingLevelUps = 1; });
        prog = new FakeProgression(t.World);
        t.World.Progression = prog;
        t.PlaceHero(ZoneCell(t, "gate_nw"));
        raise = true;
        t.Step();
        Assert.Equal(RoomPhase.Doors, t.World.Room!.Phase);       // 그 틱은 Exit 아님
        Assert.Equal(ChoiceKind.LevelUp, t.World.AwaitingChoice);
    }

    [Fact]
    public void Doors_ExitCommand_UsesIndex_DefaultCommandNeverExits()
    {
        var t = AtDoors(NextNormal);
        t.PlaceHero(t.World.Resolved!.Anchor("center"));
        for (var i = 0; i < 600; i++) Assert.Equal(StageStep.Stepped, t.AppTick(PlayerCommand.None));
        Assert.Equal(RoomPhase.Doors, t.World.Room!.Phase);
        t.AppTick(new PlayerCommand(Vector2.Zero, CommandButtons.Exit, ExitIndex: 1));
        Assert.Equal(RoomPhase.Exit, t.World.Room.Phase);
        Assert.Equal("gate_ne", t.World.Room.ChosenExit);
        Assert.Equal(StageStep.RoomExit, t.AppTick());
    }

    [Fact]
    public void HeroDeath_IsDefeatInAnyPhase_RoomFlowStops()
    {
        foreach (var phase in new[] { RoomPhase.Enter, RoomPhase.Survive, RoomPhase.Cleanup, RoomPhase.Reward, RoomPhase.Doors })
        {
            var t = Room(phase, clock: phase >= RoomPhase.Cleanup ? 3600 : 0);
            t.Hero.Dead = true;
            t.Step();
            Assert.Equal(RunOutcome.DefeatPending, t.World.Outcome);
            var before = (t.World.Room!.Phase, t.World.Room.ClockTicks);
            t.Step(200);
            Assert.Equal(RunOutcome.Defeat, t.World.Outcome);
            Assert.Equal(before, (t.World.Room.Phase, t.World.Room.ClockTicks));
        }
    }

    [Fact]
    public void HeroDiesOnTickOfEnteringExitGap_IsDefeatNotExit()
    {
        foreach (var byCommand in new[] { false, true })
        {
            var kill = false;
            var t = AtDoors(NextNormal, inject: w => { if (kill) w.Hero.Dead = true; });
            Assert.True(t.World.Room!.ExitArmed);
            if (!byCommand) t.PlaceHero(ZoneCell(t, "gate_nw"));      // 나가는 자리에 들어서는 틱
            kill = true;
            var first = t.AppTick(byCommand ? new PlayerCommand(Vector2.Zero, CommandButtons.Exit) : PlayerCommand.None);
            Assert.Equal(StageStep.Stepped, first);
            Assert.Equal(RoomPhase.Doors, t.World.Room.Phase);         // 그 틱에 Exit 로 가지 않음
            Assert.Equal(RunOutcome.DefeatPending, t.World.Outcome);
            for (var i = 0; i < 300 && t.World.Outcome != RunOutcome.Defeat; i++) Assert.NotEqual(StageStep.RoomExit, t.AppTick());
            Assert.Equal(RunOutcome.Defeat, t.World.Outcome);
            Assert.Equal(StageStep.Finished, t.AppTick());
        }
    }

    [Fact]
    public void LastNormalRoom_CleanupEndOnHeroDeathTick_IsVictory()
    {
        var t = Room(RoomPhase.Cleanup, clock: 3600, last: true);
        t.Hero.Dead = true;                                            // 정리 끝과 주인공 죽음이 같은 틱
        t.Step();
        Assert.Equal(RunOutcome.VictoryPending, t.World.Outcome);     // 3.5 "같은 틱이면 승리가 먼저"
        t.Step(200);
        Assert.Equal(RunOutcome.Victory, t.World.Outcome);
    }

    [Fact]
    public void LastBossRoom_BossDefeatedOnHeroDeathTick_IsVictory()
    {
        var kill = false;
        var t = Room(RoomPhase.Survive, clock: 600, room: TestDefs.Room(RoomKind.Boss, 360f, null, 6), last: true, inject: w =>
        {
            if (!kill) return;
            w.Events.Current.BossDefeated.Add(new BossDefeated(default, 0, Vector2.Zero));
            w.Hero.Dead = true;                                        // 보스 마지막 일격과 주인공 죽음이 같은 틱
            kill = false;
        });
        t.Step(10);
        kill = true;
        t.Step();
        Assert.Equal(RunOutcome.VictoryPending, t.World.Outcome);
        t.Step(200);
        Assert.Equal(RunOutcome.Victory, t.World.Outcome);
    }

    [Fact]
    public void ExitPhaseWithPendingOutcome_StepsToEnd_NotRoomExit()
    {
        var t = AtDoors(NextNormal);
        t.World.Room!.EnterPhase(RoomPhase.Exit, t.World.Tick);
        t.World.Outcome = RunOutcome.DefeatPending;                    // 막힌 길(StageStepper 쪽 방어)
        Assert.Equal(StageStep.Stepped, t.AppTick());
    }

    [Fact]
    public void RoomlessWorld_RoomFlowIsNoop()
    {
        var t = new TestWorldBuilder().Build();
        Assert.Null(t.World.Room);
        Assert.Null(t.World.Run);
        t.Step(10);
        Assert.Equal(t.World.StageMinutes, t.World.DifficultyMinutes);
    }

    // ── [구현-2] 정예·보스 방 ──────────────────────────────────────────

    [Fact]
    public void Elite_SurviveEndsOnMinibossDamageKill_NotCollapse_SpawningStopsAt230()
    {
        var defs = TestDefs.Basic(extraEnemies: [TestDefs.Enemy(TestDefs.Basic().SpriteSets[0], "mb", hp: 100f)]);
        var mb = defs.Enemies.Get("mb");
        var index = defs.Enemies.IndexOf("mb");
        KillCause? cause = null;
        var t = new TestWorldBuilder().WithDefs(defs)
            .WithRoom(RoomPhase.Survive, 149 * 60, TestDefs.Room(RoomKind.Elite, 300f, 360f, 5, miniboss: mb), NextNormal)
            .WithSystem(SimPhase.DeathHooks, new Inject(w =>
            {
                if (cause is { } c) w.Events.Current.EnemyKilled.Add(new EnemyKilled(default, index, Vector2.Zero, EnemyRank.Normal, c, default));
                cause = null;
            }))
            .Build();
        var room = t.World.Room!;
        Assert.True(room.SpawningOpen);
        t.Step(59);
        Assert.True(room.SpawningOpen);
        t.Step();
        Assert.False(room.SpawningOpen);                           // 방 시계 2:30
        Assert.Equal(RoomPhase.Survive, room.Phase);
        cause = KillCause.Collapse;
        t.Step();
        Assert.Equal(RoomPhase.Survive, room.Phase);               // Collapse 로는 안 넘어감
        cause = KillCause.Damage;
        t.Step();
        Assert.Equal(RoomPhase.Cleanup, room.Phase);
    }

    [Fact]
    public void Elite_T_StopsOneTickBeforeTo()
    {
        var t = Room(RoomPhase.Survive, clock: 120 * 60, room: TestDefs.Room(RoomKind.Elite, 300f, 360f, 5));
        Assert.True(t.World.DifficultyMinutes < 6f);
        Assert.Equal((360f - World.Dt) / 60f, t.World.DifficultyMinutes, 5);
    }

    [Fact]
    public void Boss_NotLast_CollapseThenReward_ChestDroppedSameTickGatesExit()
    {
        var chestDef = TestDefs.Basic().Pickups.Get(TestDefs.EliteChestId);
        var drop = false;
        var t = Room(RoomPhase.Survive, clock: 600, room: TestDefs.Room(RoomKind.Boss, 360f, null, 6), next: NextNormal, inject: w =>
        {
            if (!drop) return;
            w.Events.Current.BossDefeated.Add(new BossDefeated(default, 0, Vector2.Zero));
            var c = w.Pickups.Add();                                 // DeathRewards 가 같은 틱에 떨군 상자
            c.Def = chestDef;
            c.SpawnTick = w.Tick;
            c.Pos = c.PrevPos = new Vector2(100, 700);
            drop = false;
        });
        var minion = t.AddEnemy(new Vector2(300, 300));
        Assert.Equal((360f + 10f) / 60f, t.World.DifficultyMinutes, 4);
        t.Step(60);
        Assert.Equal((360f + 11f) / 60f, t.World.DifficultyMinutes, 4);   // 보스 방 t 안 멈춤
        drop = true;
        t.Step();
        var room = t.World.Room!;
        Assert.Equal(RoomPhase.Reward, room.Phase);                // 정리 단계 없음
        Assert.True(minion.Dead);
        Assert.Equal(KillCause.Collapse, minion.KillCause);
        Assert.Equal(new RoomCleared(RoomKind.Boss, false), t.World.Events.Current.RoomCleared[0]);
        var chest = t.World.Pickups[0];
        Assert.True(chest.GatesExit);
        Assert.InRange(Vector2.Distance(chest.Pos, t.World.Resolved!.Anchor("center")), 0f, 48.5f);
        t.Step(30);
        Assert.Equal(RoomPhase.Reward, room.Phase);                // 열기 전에는 Doors 아님
        t.World.Pickups.Remove(chest.Id);
        t.Step();
        Assert.Equal(RoomPhase.Doors, room.Phase);
    }

    [Fact]
    public void Boss_Last_IsVictory()
    {
        var kill = false;
        var t = Room(RoomPhase.Survive, clock: 600, room: TestDefs.Room(RoomKind.Boss, 360f, null, 6), last: true,
                     inject: w => { if (kill) w.Events.Current.BossDefeated.Add(new BossDefeated(default, 0, Vector2.Zero)); });
        kill = true;
        t.Step();
        Assert.Equal(RunOutcome.VictoryPending, t.World.Outcome);
    }
}
