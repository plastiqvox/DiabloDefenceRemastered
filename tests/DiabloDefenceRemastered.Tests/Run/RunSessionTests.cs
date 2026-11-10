namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 10.1 <c>Run</c> 행 — <c>RunSession</c>(합성 액트 <see cref="RoomActs"/> + <see cref="FakeHeroCarry"/>) 와 방 월드 생성자 <c>World(defs, RoomStart)</c>.
/// WP3·WP4 없이 돌리려고 시험 모듈이 직선 내비·<see cref="FakeSpawner"/> 를 넣고, 정예·보스 방은 <see cref="AutoKill"/> 이 처치 틱 이벤트를 낸다.
/// </summary>
public class RunSessionTests
{
    /// <summary>정예 방 중간 보스·보스 방 보스를 생존 단계 1초째에 "처치"(DeathHooks 자리 — EnemyKilled·BossDefeated 만).</summary>
    private sealed class AutoKill : ISimSystem
    {
        public void Tick(World world)
        {
            if (world.Room is not { Phase: RoomPhase.Survive } room || room.ClockTicks != 4 * 60) return;
            if (room.Plan.Kind == RoomKind.Elite)
                world.Events.Current.EnemyKilled.Add(new EnemyKilled(default, world.Defs.Enemies.IndexOf(RoomActs.MinibossId), Vector2.Zero, EnemyRank.Unique, KillCause.Damage, default));
            else if (room.Plan.Kind == RoomKind.Boss)
                world.Events.Current.BossDefeated.Add(new BossDefeated(default, world.Defs.Enemies.IndexOf(RoomActs.BossId), Vector2.Zero));
        }
    }

    private static ModuleContext Modules(FakeHeroCarry? carry = null)
    {
        var m = new ModuleContext();
        m.AddWorldInstaller((w, b) =>
        {
            w.Presentation.Enabled = false;
            w.Nav = new StraightNav(w.Arena!.Iso);
            w.Spatial = new BruteForceSpatial(w);
            w.Spawner = new FakeSpawner(w);
            b.Add(SimPhase.DeathHooks, new AutoKill());
        });
        if (carry is not null) m.SetHeroCarry(carry);
        return m;
    }

    private static RunSession Session(GameDefs defs, ulong seed = 11, int startIndex = 0, FakeHeroCarry? carry = null, RunOptions? options = null) =>
        new(defs, Modules(carry), new RunStart(seed, defs.Stages.Get(RoomActs.Act1Id), null, options ?? RunOptions.Default, startIndex));

    /// <summary>지금 방을 나감 단계까지(보상 물체는 집은 것으로 지움, 문은 Exit 명령 0번). 판이 끝나면 멈춘다.</summary>
    private static void PlayToExit(RunSession s, Action<World>? each = null)
    {
        var w = s.World;
        for (var i = 0; i < 60 * 600 && w.Room!.Phase != RoomPhase.Exit && w.Outcome == RunOutcome.Running; i++)
        {
            each?.Invoke(w);
            if (w.Room.Phase == RoomPhase.Reward)
                for (var k = 0; k < w.Pickups.Count; k++)
                    if (w.Pickups[k].GatesExit) w.Pickups.Remove(w.Pickups[k].Id);
            var cmd = w.Room.Phase == RoomPhase.Doors ? new PlayerCommand(Vector2.Zero, CommandButtons.Exit) : PlayerCommand.None;
            StageStepper.AppTick(w, cmd);
        }
    }

    private static void PlayToEnd(RunSession s)
    {
        for (var room = 0; room < 20; room++)
        {
            PlayToExit(s);
            if (s.World.Outcome != RunOutcome.Running) break;
            s.Advance();
        }
        while (s.World.Outcome is RunOutcome.VictoryPending or RunOutcome.DefeatPending) StageStepper.AppTick(s.World, PlayerCommand.None);
    }

    [Fact]
    public void RoomWorld_HeroAtEntryFacingCenter_EnterPhase_RunStatsShared()
    {
        var t = new TestWorldBuilder().WithRoom(gateIn: "gate_se").Build();
        var w = t.World;
        Assert.Equal(w.Resolved!.Entry("gate_se").Inside, w.Hero.Pos);
        Assert.Equal(w.Hero.Pos, w.Hero.PrevPos);
        var toCenter = Vector2.Normalize(w.Resolved.Anchor("center") - w.Hero.Pos);
        Assert.True(Vector2.Dot(Vector2.Normalize(w.Hero.Facing), toCenter) > 0.7f);   // 8방위로 맞춘 center 쪽
        Assert.Equal(RoomPhase.Enter, w.Room!.Phase);
        Assert.NotNull(w.Run);
        Assert.Same(w.Run!.Stats, w.RunStats);
        Assert.Equal(w.Room.Plan.RoomSeed, w.Seed);
        Assert.Same(w.Run.Act.Act, w.Stage);
        Assert.Equal(4, w.Gates.Count);
        Assert.All(w.Gates.Values, g => Assert.NotEmpty(g.Cells));
    }

    [Fact]
    public void RoomWorld_SeedActClassComeFromRoomStart()
    {
        var defs = RoomActs.Defs();
        var other = TestDefs.HeroClass(defs.SpriteSets[0], defs.Weapons[0], id: "other_hero");
        var s = new RunSession(defs, Modules(), new RunStart(5, defs.Stages.Get(RoomActs.Act1Id), other, RunOptions.Default));
        Assert.Same(other, s.World.HeroClass);
        Assert.Equal(RunSeeds.Room(5, 1, 1), s.World.Seed);
        Assert.Same(defs.Stages.Get(RoomActs.Act1Id), s.World.Stage);
        Assert.Same(s.World.Room!.Plan.Map, s.World.Map);
    }

    [Fact]
    public void RoomlessWorld_StillWorks_DifficultyIsStageMinutes_GatesWithoutMapEmpty()
    {
        var t = new TestWorldBuilder().Build();
        t.Step(120);
        Assert.Equal(t.World.StageMinutes, t.World.DifficultyMinutes);
        Assert.Null(t.World.Room);
        Assert.Equal(4, t.World.Gates.Count);
        Assert.Empty(new World(GameDefs.Empty, 1).Gates);
    }

    [Fact]
    public void ReplayLighting_AppliedBeforeFirstTick()
    {
        var defs = RoomActs.Defs();
        var s = Session(defs, startIndex: 2);                    // 방 3(t 2:15) — 0:30 어둠 0.3, 1:30 빛 300 이 지났다
        Assert.Equal(0.3f, s.World.Lighting.Darkness);
        Assert.Equal(300f, s.World.Lighting.HeroLightRadius);
        Assert.Equal(300f, s.World.Visibility.Radius);            // 첫 틱 시야가 바뀐 반경으로
        var first = Session(defs);
        Assert.Equal(0.1f, first.World.Lighting.Darkness);        // 방 1 시작은 액트 조명 그대로
    }

    [Fact]
    public void Advance_NewWorldEachRoom_CarryRoundTrip_StatsAccumulate()
    {
        var defs = RoomActs.Defs();
        var carry = new FakeHeroCarry { Level = 4 };
        var s = Session(defs, carry: carry);
        var w1 = s.World;
        w1.Hero.Hp = 37f;
        w1.Hero.MaxHp = 50f;
        w1.Hero.Weapons[2] = new WeaponSlot { Def = defs.Weapons[0], Level = 3, MaxedAtTick = -1 };
        w1.RunStats.Kills += 3;
        w1.Fairness.UnseenShots += 1;
        PlayToExit(s);
        var ticks1 = w1.Tick;
        s.Advance();
        var w2 = s.World;
        Assert.NotSame(w1, w2);
        Assert.NotSame(w1.Pickups, w2.Pickups);
        Assert.NotSame(w1.Visibility, w2.Visibility);
        Assert.Equal(0.3f, w2.Lighting.Darkness);                  // Advance 길도 지난 light_change(0:30)를 다시 적용
        Assert.Equal(420f, w2.Lighting.HeroLightRadius);           // 1:30 은 아직
        Assert.Equal(0, w2.Tick);
        Assert.Equal(37f, w2.Hero.Hp);
        Assert.Equal(defs.Weapons[0], w2.Hero.Weapons[2].Def);
        Assert.Equal(3, w2.Hero.Weapons[2].Level);
        Assert.True(w2.Hero.Weapons[0].IsEmpty);
        Assert.Equal(1, carry.Captures);
        Assert.Equal(1, carry.Restores);
        Assert.Equal(4, s.Run.Carry!.Level);

        w2.RunStats.Kills += 2;
        w2.Fairness.UnseenShots += 2;
        PlayToExit(s);
        var ticks2 = w2.Tick;
        s.Advance();
        Assert.Equal(5, s.Run.Stats.Kills);                        // 두 방의 합(지우는 곳 없음, v0.7.1)
        Assert.Same(s.Run.Stats, s.World.RunStats);
        Assert.Equal(ticks1 + ticks2, s.Run.ElapsedTicks);
        Assert.Equal(3, s.Run.Fairness.UnseenShots);
        Assert.Equal(2, s.Run.Rooms.Count);
        Assert.Equal([1, 2], s.Run.Rooms.Select(r => r.Number));
        Assert.All(s.Run.Rooms, r => Assert.NotNull(r.Door));
        Assert.Equal(TestDefs.RewardTomeId, s.Run.Rooms[0].RewardId);   // 첫 방 = first_room
        Assert.Equal(new RunDepth(1, 3), s.Run.Depth);
    }

    [Fact]
    public void FairnessAddTo_AddsEveryField()
    {
        var room = new FairnessCounters { UnseenShots = 1, UnseenCharges = 2, HiddenHits = 3, KiteSwitches = 4 };
        var total = new FairnessCounters { UnseenShots = 10, UnseenCharges = 20, HiddenHits = 30, KiteSwitches = 40 };
        room.AddTo(total);
        Assert.Equal((11, 22, 33, 44), (total.UnseenShots, total.UnseenCharges, total.HiddenHits, total.KiteSwitches));
        // 필드를 더하면 이 시험도 고친다 — 속성 수로 붙잡아 둠
        Assert.Equal(4, typeof(FairnessCounters).GetProperties().Length);
    }

    [Fact]
    public void Advance_WhenOutcomeNotRunning_Throws()
    {
        var s = Session(RoomActs.Defs());
        PlayToExit(s);
        Assert.Equal(RoomPhase.Exit, s.World.Room!.Phase);
        s.World.Outcome = RunOutcome.DefeatPending;                // 죽은 틱에 Exit 로 간 옛 길 흉내
        Assert.Throws<InvalidOperationException>(s.Advance);
        Assert.Equal(0, s.Run.RoomIndex);
    }

    [Fact]
    public void Advance_NextNormalRoomReward_IsChosenDoorReward()
    {
        var s = Session(RoomActs.Defs());
        PlayToExit(s);
        var chosen = s.World.Room!.ChosenExitInfo!.Value.Reward;
        s.Advance();
        Assert.Same(chosen, s.World.Room!.Reward);
        Assert.Same(chosen, s.Run.NextReward);
    }

    [Fact]
    public void WorksWithoutHeroCarry()
    {
        var s = Session(RoomActs.Defs());
        PlayToExit(s);
        s.Advance();
        Assert.Null(s.Run.Carry);
        Assert.Equal(1, s.Run.RoomIndex);
    }

    [Fact]
    public void StartAt_OnlyFirstRoomClock()
    {
        var s = Session(RoomActs.Defs(), options: RunOptions.Default with { StartAt = new ClockTime(20f) });
        Assert.Equal(1200, s.World.Room!.ClockTicks);
        PlayToExit(s);
        s.Advance();
        Assert.Equal(0, s.World.Room!.ClockTicks);
    }

    [Fact]
    public void StartRoom_SameAsReachingIt_MapGateSeedAndRoomColumns()
    {
        var defs = RoomActs.Defs();
        var full = Session(defs, seed: 21, carry: new FakeHeroCarry());
        PlayToExit(full);
        full.Advance();
        PlayToExit(full);
        full.Advance();                                             // 방 3 에 들어온 순간
        var a = full.World;
        var direct = Session(defs, seed: 21, startIndex: 2, carry: new FakeHeroCarry());
        new FakeHeroCarry().Restore(direct.World, full.Run.Carry!);   // 같은 짐
        var b = direct.World;
        Assert.Equal(a.Room!.Plan.Map.Id, b.Room!.Plan.Map.Id);
        Assert.Equal(a.Room.Plan.GateIn, b.Room.Plan.GateIn);
        Assert.Equal(a.Room.Plan.RoomSeed, b.Room.Plan.RoomSeed);
        Assert.Equal(a.Seed, b.Seed);
        // 방 해시 대리(StateHasher 는 WP7): 주인공·방 시계·단계·조명·난수 상태 — 판 누적·고른 문 빼고
        for (var i = 0; i < 600; i++)
        {
            StageStepper.AppTick(a, PlayerCommand.None);
            StageStepper.AppTick(b, PlayerCommand.None);
            if (i % 60 == 59) Assert.Equal(RoomColumn(a), RoomColumn(b));
        }
    }

    private static string RoomColumn(World w) =>
        $"{w.Tick}|{w.Hero.Pos}|{w.Hero.Hp}|{w.Room!.Phase}|{w.Room.ClockTicks}|{w.DifficultyMinutes}|{w.Lighting.Darkness}|{w.Lighting.HeroLightRadius}|" +
        $"{w.Rng.Spawn.GetState()}|{w.Rng.Drop.GetState()}|{w.Enemies.Count}|{w.Pickups.Count}";

    [Fact]
    public void Advance_NotAtExit_Throws_AfterLastRoomThrows()
    {
        var defs = RoomActs.Defs();
        var s = Session(defs);
        Assert.Throws<InvalidOperationException>(s.Advance);
        // 판의 마지막 방(액트 2 보스)을 억지로 나감 단계로 → 다음 방이 없다
        var last = new RunSession(defs, Modules(), new RunStart(3, defs.Stages.Get(RoomActs.Act2Id), null, RunOptions.Default, 2));
        Assert.True(last.World.Room!.IsLastInRun);
        last.World.Room.EnterPhase(RoomPhase.Exit, 0);
        Assert.Throws<InvalidOperationException>(last.Advance);
    }

    [Fact]
    public void Conclude_Defeat_ClosesLastRoom()
    {
        var s = Session(RoomActs.Defs());
        Assert.Throws<InvalidOperationException>(() => s.Conclude());   // 판이 안 끝남
        PlayToExit(s);
        var t1 = s.World.Tick;
        s.Advance();
        s.World.Fairness.HiddenHits = 2;
        for (var i = 0; i < 300; i++) StageStepper.AppTick(s.World, PlayerCommand.None);
        s.World.Hero.Dead = true;                                  // 생존 중 HP 0
        while (s.World.Outcome != RunOutcome.Defeat) StageStepper.AppTick(s.World, PlayerCommand.None);
        var t2 = s.World.Tick;
        var run = s.Conclude();
        Assert.Equal(t1 + t2, run.ElapsedTicks);
        Assert.Equal(2, run.Rooms.Count);
        Assert.Null(run.Rooms[^1].Door);
        Assert.Equal(-1, run.Rooms[^1].ClearTick);
        Assert.Equal(2, run.Fairness.HiddenHits);
        Assert.Same(run, s.Conclude());                            // 두 번째는 같은 값
        Assert.Equal(t1 + t2, s.Conclude().ElapsedTicks);
        Assert.Equal(2, s.Run.Rooms.Count);
        Assert.Throws<InvalidOperationException>(s.Advance);
    }

    // ── [구현-2] 계단·넓이 배율·판 끝 승리 ───────────────────────────────

    [Fact]
    public void Stairs_GoToNextActFirstRoom_ThenVictoryConclude()
    {
        var defs = RoomActs.Defs();
        var s = Session(defs, seed: 4);
        var sum = 0;
        for (var room = 0; room < 6; room++)
        {
            PlayToExit(s);
            sum += s.World.Tick;
            if (room == 5)
            {
                Assert.True(s.World.Room!.Exits.Single().Stairs);
                Assert.Equal("합성 액트 2", s.World.Room.Exits[0].NextActName);
            }
            s.Advance();
        }
        Assert.Equal(2, s.Run.Act.ActNumber);
        Assert.Same(defs.Stages.Get(RoomActs.Act2Id), s.Run.Act.Act);
        Assert.Equal(0, s.Run.RoomIndex);
        Assert.Equal(new RunDepth(2, 1), s.Run.Depth);
        Assert.Equal(TestDefs.RewardTomeId, s.World.Room!.Reward!.Id);   // 액트 첫 방 = first_room
        Assert.Equal(2, s.World.Room.Plan.Act);
        Assert.Equal(RunSeeds.Room(4, 2, 1), s.World.Seed);
        Assert.Equal(0f, s.World.Lighting.Darkness, 4);                // 액트 2 는 자기 조명(액트 1 의 light_change 는 넘어오지 않음)

        PlayToEnd(s);
        Assert.Equal(RunOutcome.Victory, s.World.Outcome);
        var run = s.Conclude();
        Assert.Equal(9, run.Rooms.Count);
        Assert.Null(run.Rooms[^1].Door);
        Assert.Equal(new RunDepth(2, 3), run.Depth);
        Assert.Equal(run.Rooms.Sum(r => r.Ticks), run.ElapsedTicks);
    }

    [Fact]
    public void EliteRoomStart_BonusIsPreviousOrderFirst_ThenChosenDoorReward()
    {
        var defs = RoomActs.Defs();
        var s = Session(defs, startIndex: 3);                       // 정예 4
        Assert.Same(s.Run.Act.Rooms[2].RewardOrder[0], s.World.Room!.Reward);   // v0.8: --room 시작 정예 방도 앞 방 RewardOrder[0](덤 — v0.7 은 null)
        Assert.Equal(RoomKind.Elite, s.World.Room.Plan.Kind);
        PlayToExit(s);
        Assert.Equal(RoomPhase.Exit, s.World.Room!.Phase);
        s.Advance();
        Assert.Equal(5, s.World.Room!.Plan.Number);
        Assert.NotNull(s.World.Room.Reward);                       // 정예 방 뒤 일반 방은 고른 문의 보상
    }

    [Fact]
    public void AreaScale_SmallRoomsScaledDown_BossRoomOne()
    {
        var defs = RoomActs.Defs();
        var s = Session(defs);
        var small = MapArea.WalkableCells(ArenaLayout.Expand(defs.Maps.Get(RoomActs.RoomA)));
        var big = MapArea.WalkableCells(ArenaLayout.Expand(defs.Maps.Get(RoomActs.BossRoom)));
        Assert.True(small < big);
        Assert.Equal(Math.Clamp(small / (float)big, 0.6f, 1f), s.World.Room!.AreaScale, 5);
        Assert.Equal(1f, s.AreaScaleOf(defs.Maps.Get(RoomActs.BossRoom)));
        var one = Session(RoomActs.Defs(twoNormalMaps: false));
        Assert.InRange(one.World.Room!.AreaScale, 0.6f, 1f);
    }
}
