namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 10.1 <c>Run</c> 행(v0.8 WP1f #8·#9) — <c>RewardSpot.Find</c>/<c>Row</c>(3.6 "보상 자리"), <c>RoomFlow</c> 의 덤 보상·상자 줄·<c>DoorsOpened</c> 자리·<c>NextFloor</c>·
/// <c>BossClockTicks</c>·<c>TimedEffectsRun</c>·<c>cleanup_limit</c>.
/// </summary>
public class RewardSpotTests
{
    private static readonly RewardSpotDef Spot = new() { Distance = 100f, Spacing = 40f, Clearance = 30f };
    private static readonly Vector2 Feet = new(500f, 500f);
    private static readonly Vector2 East = new(1f, 0f);

    /// <summary>시험 내비: clearance 를 함수로(맵 안 = 늘 참).</summary>
    private sealed class FuncNav(Func<Vector2, float> clearance) : INavWorld
    {
        public float Clearance(Vector2 p) => clearance(p);
        public bool HasClearLine(Vector2 a, Vector2 b, float radius) => true;
        public bool ProjectileLineClear(Vector2 a, Vector2 b) => true;
        public bool IsInsideMap(Vector2 p) => true;
        public float PathLength(Vector2 from, Vector2 to, float radius, float maxLength) => Vector2.Distance(from, to);
        public float PathLengthToHero(Vector2 from, float radius, float maxLength) => 0f;
        public bool TryPathStep(Vector2 from, Vector2 to, float radius, out Vector2 next) { next = to; return true; }
    }

    private static void Near(Vector2 expected, Vector2 actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }

    [Fact]
    public void Open_FacingSide100px()
    {
        Near(Feet + East * 100f, RewardSpot.Find(new FuncNav(_ => 1000f), Feet, East, 14f, Spot));
        Near(Feet + East * 100f, RewardSpot.Find(null, Feet, East, 14f, Spot));   // 내비 없음 = 벽 없음
    }

    [Fact]
    public void FrontWall_NextCandidateInOrder_PlusSideFirst()
    {
        // 바라보는 쪽 40px 앞이 벽(반평면): 0° 는 40, ±45° 는 56 까지, +90° 가 처음으로 100 을 감
        var nav = new FuncNav(p => Vector2.Dot(p - Feet, East) > 40f ? 0f : 1000f);
        Near(Feet + new Vector2(0f, 100f), RewardSpot.Find(nav, Feet, East, 14f, Spot));
    }

    [Fact]
    public void NoDirectionReachesDistance_FarthestFirstCandidate()
    {
        // 발에서 60px 안만 트임 → 모든 방향이 56 까지(8px 걸음) → 같으면 앞 후보(바라보는 쪽)
        var nav = new FuncNav(p => Vector2.Distance(p, Feet) <= 60f ? 1000f : 0f);
        Near(Feet + East * 56f, RewardSpot.Find(nav, Feet, East, 14f, Spot));
    }

    [Fact]
    public void NarrowCorridor_SecondPassWithHeroRadius_V081()
    {
        // clearance 20 인 합성 복도(x 축을 따라): 문턱 30 으로는 첫 걸음도 못 감 → 두 번째 차례(문턱 = 주인공 반경 14)로 발 밖 자리
        var nav = new FuncNav(p => MathF.Abs(p.Y - Feet.Y) < 10f ? 20f : -1f);
        var spot = RewardSpot.Find(nav, Feet, East, 14f, Spot);
        Near(Feet + East * 100f, spot);
        Assert.NotEqual(Feet, spot);
        // 문턱이 반경보다 높은 막힌 곳(clearance 5)은 주인공 발
        Assert.Equal(Feet, RewardSpot.Find(new FuncNav(_ => 5f), Feet, East, 14f, Spot));
    }

    [Fact]
    public void SpotIsReachedByStraightLine()
    {
        // 자리까지 8px 걸음마다 clearance ≥ 30 — 벽(앞 70px 지점의 기둥) 너머로 건너뛰지 않는다
        var pillar = Feet + East * 70f;
        var nav = new FuncNav(p => Vector2.Distance(p, pillar) < 20f ? 0f : 1000f);
        var spot = RewardSpot.Find(nav, Feet, East, 14f, Spot);
        for (var d = 8f; d <= Vector2.Distance(Feet, spot); d += 8f)
            Assert.True(nav.Clearance(Feet + Vector2.Normalize(spot - Feet) * d) >= 30f);
    }

    [Fact]
    public void Row_PerpendicularLine_Spacing40_Centered_PulledTowardSpot()
    {
        var spot = Feet + East * 100f;
        var row = RewardSpot.Row(new FuncNav(_ => 1000f), spot, East, 3, Spot);
        Assert.Equal([spot + new Vector2(0f, -40f), spot, spot + new Vector2(0f, 40f)], row);
        Assert.Equal([spot + new Vector2(0f, -20f), spot + new Vector2(0f, 20f)], RewardSpot.Row(null, spot, East, 2, Spot));
        // 아래쪽(+y)이 30px 넘게 막히면 그 자리는 spot 쪽으로 8px 씩 당김
        var pulled = RewardSpot.Row(new FuncNav(p => p.Y > spot.Y + 30f ? 0f : 1000f), spot, East, 3, Spot);
        Assert.Equal(spot + new Vector2(0f, 40f - 16f), pulled[2]);
        Assert.Empty(RewardSpot.Row(null, spot, East, 0, Spot));
    }

    // ── RoomFlow 연결(v0.8) ───────────────────────────────────────────

    [Fact]
    public void EliteRoom_BonusRewardAndChest_InOneRow40px_AllGateExit_CuesAtSpot()
    {
        var defs = TestDefs.Basic();
        var elite = TestDefs.Room(RoomKind.Elite, 60f, 120f, 2);
        var t = new TestWorldBuilder().WithRoom(RoomPhase.Cleanup, 3600, elite, TestDefs.Room(RoomKind.Normal, 120f, 180f, 3),
                                                reward: defs.Rewards.Get(TestDefs.RewardPotionId)).WithPresentation().Build();
        var chest = t.World.Pickups.Add();                           // 중간 보스가 떨군 정예 상자
        chest.Def = defs.Pickups.Get(TestDefs.EliteChestId);
        chest.Pos = chest.PrevPos = new Vector2(100f, 700f);
        t.Step();
        var room = t.World.Room!;
        Assert.Equal(RoomPhase.Reward, room.Phase);
        var bonus = Assert.Single(t.World.Pickups.Items.ToArray(), p => !p.Removed && p.Reward is not null);
        Assert.Equal(TestDefs.RewardPotionId, bonus.Reward!.Id);    // 덤 보상(v0.8 — v0.7 은 정예 방 Reward = null)
        Assert.True(bonus.GatesExit);
        Assert.True(chest.GatesExit);
        Assert.Equal(40f, Vector2.Distance(bonus.Pos, chest.Pos), 3);   // 보상 → 상자 순서로 한 줄
        Near(room.RewardSpot, (bonus.Pos + chest.Pos) * 0.5f);         // 가운데 맞춤
        var cleared = Assert.Single(Cues(t.World), c => c.Kind == CueKind.RoomCleared);
        Assert.Equal(room.RewardSpot, cleared.Pos);

        FakeSpawner.Collect(t.World, bonus);
        t.World.Pickups.Remove(chest.Id);
        t.Step();
        Assert.Equal(RoomPhase.Doors, room.Phase);
        var opened = Assert.Single(Cues(t.World), c => c.Kind == CueKind.DoorsOpened);
        Assert.Equal(room.RewardSpot, opened.Pos);                    // v0.8.1: rules.reward_anchor 대신 RoomState.RewardSpot
    }

    [Fact]
    public void NarrowCorridor_EliteRoom_RowUsesFindThreshold_NoOverlap_V082()
    {
        // 어디나 clearance 20(문턱 30 미만): Find 는 두 번째 차례(주인공 반경)로 자리를 찾고, 줄도 그 문턱으로 당긴다 — 덤 보상과 상자가 한 점에 겹치지 않음
        var narrow = new FuncNav(_ => 20f);
        var spot = RewardSpot.Find(narrow, Feet, East, 14f, Spot, out var threshold);
        Assert.Equal(14f, threshold);
        Near(Feet + East * 100f, spot);
        var row = RewardSpot.Row(narrow, spot, East, 2, Spot, threshold);
        Assert.Equal(40f, Vector2.Distance(row[0], row[1]), 3);
        Assert.All(RewardSpot.Row(narrow, spot, East, 2, Spot), p => Assert.Equal(spot, p));   // 문턱 30(기본)이면 모두 spot 으로 되돌아옴(v0.8.1 의 겹침)

        var defs = TestDefs.Basic();
        var elite = TestDefs.Room(RoomKind.Elite, 60f, 120f, 2);
        var t = new TestWorldBuilder().WithRoom(RoomPhase.Cleanup, 3600, elite, TestDefs.Room(RoomKind.Normal, 120f, 180f, 3),
                                                reward: defs.Rewards.Get(TestDefs.RewardPotionId)).Build();
        t.World.Nav = narrow;
        var chest = t.World.Pickups.Add();
        chest.Def = defs.Pickups.Get(TestDefs.EliteChestId);
        chest.Pos = chest.PrevPos = new Vector2(100f, 700f);
        t.Step();
        Assert.Equal(RoomPhase.Reward, t.World.Room!.Phase);
        var bonus = Assert.Single(t.World.Pickups.Items.ToArray(), p => !p.Removed && p.Reward is not null);
        Assert.NotEqual(t.Hero.Pos, t.World.Room.RewardSpot);
        Assert.Equal(40f, Vector2.Distance(bonus.Pos, chest.Pos), 3);
    }

    [Fact]
    public void RewardSpot_BeforeRewardPhase_IsHeroStart()
    {
        var t = new TestWorldBuilder().WithRoom(RoomPhase.Survive).Build();
        Assert.Equal(t.Hero.Pos, t.World.Room!.RewardSpot);
        Assert.Equal(t.World.Resolved!.Entry("gate_se").Inside, t.Hero.Pos);
    }

    [Fact]
    public void EliteRoom_BossClockTicks_OnMinibossKill_NormalRoomStaysMinusOne()
    {
        var defs = TestDefs.Basic(extraEnemies: [TestDefs.Enemy(TestDefs.Basic().SpriteSets[0], "mb", hp: 100f)]);
        var index = defs.Enemies.IndexOf("mb");
        var kill = false;
        var t = new TestWorldBuilder().WithDefs(defs)
            .WithRoom(RoomPhase.Survive, 30 * 60, TestDefs.Room(RoomKind.Elite, 300f, 360f, 5, miniboss: defs.Enemies.Get("mb")), TestDefs.Room(RoomKind.Normal, 360f, 420f, 6))
            .WithSystem(SimPhase.DeathHooks, new Hook(w => { if (kill) w.Events.Current.EnemyKilled.Add(new EnemyKilled(default, index, Vector2.Zero, EnemyRank.Normal, KillCause.Damage, default)); }))
            .Build();
        Assert.Equal(-1, t.World.Room!.BossClockTicks);
        t.Step(9);
        kill = true;
        t.Step();
        Assert.Equal(RoomPhase.Cleanup, t.World.Room.Phase);
        Assert.Equal(30 * 60 + 10, t.World.Room.BossClockTicks);

        var normal = new TestWorldBuilder().WithRoom(RoomPhase.Cleanup, 3600).Build();
        normal.Step();
        Assert.Equal(-1, normal.World.Room!.BossClockTicks);
    }

    [Fact]
    public void TimedEffectsRun_OnlyInSurviveAndCleanup()
    {
        foreach (var (phase, runs) in new[]
                 {
                     (RoomPhase.Enter, false), (RoomPhase.Survive, true), (RoomPhase.Cleanup, true),
                     (RoomPhase.Reward, false), (RoomPhase.Doors, false), (RoomPhase.Exit, false),
                 })
        {
            var w = new TestWorldBuilder().WithRoom(phase).Build().World;
            Assert.Equal(runs, w.Room!.TimedEffectsRun);
            Assert.Equal(runs, w.TimedEffectsRun);
        }
        Assert.True(new TestWorldBuilder().Build().World.TimedEffectsRun);   // 방 없는 월드는 늘 흐름
    }

    [Fact]
    public void CleanupLimit_ReadFromRules()
    {
        var basic = TestDefs.Basic();
        var defs = WithRooms(basic, basic.Rules.Rooms! with { CleanupLimit = 10f });
        var t = new TestWorldBuilder().WithDefs(defs).WithRoom(RoomPhase.Cleanup, 3600).Build();
        Assert.Equal(600, t.World.Room!.CleanupLimitTicks);
        t.AddEnemy(new Vector2(300, 300));
        t.Step(599);
        Assert.Equal(RoomPhase.Cleanup, t.World.Room.Phase);
        t.Step();
        Assert.True(t.World.Room.Collapsed);
    }

    [Fact]
    public void NextFloor_OnFloorChange_IncludingStairs()
    {
        var defs = RoomActs.Defs(floors: true);
        var act1 = defs.Stages.Get(RoomActs.Act1Id);
        var plan = RunPlanner.PlanAct(act1, 1, 7, defs.Rules.Rooms!);
        Assert.All(DoorsOf(defs, plan, 0), e => Assert.Equal(0, e.NextFloor));   // 1층 → 1층
        Assert.All(DoorsOf(defs, plan, 2), e => Assert.Equal(2, e.NextFloor));   // 방 3(1층) → 정예 4(2층)
        var stairs = Assert.Single(DoorsOf(defs, plan, 5));
        Assert.True(stairs.Stairs);
        Assert.Equal(3, stairs.NextFloor);                                       // 계단 문도 — 액트 2 첫 방 3층
        Assert.Equal("합성 액트 2", stairs.NextActName);

        var noFloors = RoomActs.Defs();
        var plain = RunPlanner.PlanAct(noFloors.Stages.Get(RoomActs.Act1Id), 1, 7, noFloors.Rules.Rooms!);
        Assert.All(DoorsOf(noFloors, plain, 2), e => Assert.Equal(0, e.NextFloor));   // 층을 안 적은 액트
    }

    private static IReadOnlyList<RoomExit> DoorsOf(GameDefs defs, ActPlan plan, int index)
    {
        var run = new RunState(7, plan.Act, null, plan);
        var world = new World(defs, new RoomStart(run, plan.Rooms[index], null, 1f, false));
        world.Room!.EnterPhase(RoomPhase.Reward, 0);
        world.Nav = new StraightNav(world.Arena!.Iso);
        world.Spatial = new BruteForceSpatial(world);
        var sim = Simulation.CreateBuilder().Build(world);
        sim.Step();
        Assert.Equal(RoomPhase.Doors, world.Room.Phase);
        return world.Room.Exits;
    }

    private static List<CueEvent> Cues(World w)
    {
        var list = new List<CueEvent>();
        for (var i = 0; i < w.Presentation.Cues.Count; i++) list.Add(w.Presentation.Cues[i]);
        return list;
    }

    private sealed class Hook(Action<World> act) : ISimSystem
    {
        public void Tick(World world) => act(world);
    }

    private static GameDefs WithRooms(GameDefs d, RoomRulesDef rooms) => new()
    {
        Rules = d.Rules with { Rooms = rooms },
        Sprites = d.Sprites, SpriteSets = d.SpriteSets, Weapons = d.Weapons, Passives = d.Passives, HeroClasses = d.HeroClasses,
        Tilesets = d.Tilesets, Maps = d.Maps, Stages = d.Stages, Enemies = d.Enemies, Rewards = d.Rewards, Pickups = d.Pickups,
    };
}
