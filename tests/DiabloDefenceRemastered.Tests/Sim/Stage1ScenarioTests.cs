using DiabloDefenceRemastered.Core.Sim.Autoplay;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 가짜 이웃으로 도는 액트 1 시나리오 (설계서 11.1 WP4 완료 기준·10.1 <c>Sim</c>): 실제 <c>stage_cathedral</c>(단계 2 — 방 아홉, 정예 방 둘·보스 방) 을
/// <c>RunSession</c> + 자동 조작 + <c>--god</c> 로 해골왕 처치(승리)까지. 예외 없음, 방 틱 상한 통과, <c>DOOR_SKIP</c>·<c>REWARD_STALL</c> 0,
/// 공정성(<c>UnseenCharges</c> 0·<c>kite_switches</c> ≤ kite 적 초당 1), 같은 씨앗 두 번의 해시·<c>RoomRecord</c> 같음.
/// 이웃 묶음(WP2 이동·WP3 전투)은 <see cref="FakeArena"/> 가짜다 — 상인 방은 상인 창 본문(WP3 <c>ShopService</c>)이 없어 <see cref="NoMerchants"/> 정의로 돈다.
/// 실제 모듈 판(상인 포함)은 <see cref="RealActScenarioTests"/>. 단계 1 slice 판(방 1·2·3·4·6·8)은 단계 2 에서 지웠다.
/// </summary>
public sealed class Stage1ScenarioTests(ITestOutputHelper output)
{
    /// <summary>가짜 무기 피해(가짜 이웃은 단일 대상 무기뿐이라 중간 보스·보스를 방 틱 상한 안에 잡게 세게 — 14 → 60, 초당 300).</summary>
    private const float WeaponDamage = 60f;

    private static ModuleContext Modules() => FakeArena.Modules(weaponDamage: WeaponDamage);

    [Fact]
    [Trait("Category", TestCategories.Scenario)]
    public void Act1_runs_to_skeleton_king_victory_with_autoplay_and_god()
    {
        var defs = Wp4Data.Defs;
        var r = ActRunner.Run(defs, "stage_cathedral", 7, modules: Modules, act: NoMerchants(defs.Stages.Get("stage_cathedral")));
        output.WriteLine($"ticks={r.Ticks} ({r.Ticks * World.Dt / 60f:0.0}분) rooms={r.Run.Rooms.Count} peak={r.PeakAlive} spawned={r.Spawned} band={r.BandSpawns} fallback={r.Fallbacks}");
        foreach (var room in r.Run.Rooms)
            output.WriteLine($"ROOM act={room.Act} room={room.Number} map={room.MapId} gate_in={room.GateIn} reward={room.RewardId ?? "-"} door={room.Door ?? "-"} clear_tick={room.ClearTick} cleanup_s={room.CleanupTicks * World.Dt:0.0} collapsed={(room.Collapsed ? 1 : 0)} boss_s={room.BossClockTicks * World.Dt:0.0}");

        Assert.Equal(RunOutcome.Victory, r.Outcome);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], r.Run.Rooms.Select(x => x.Number));
        Assert.Equal("cathedral_arena", r.Run.Rooms[^1].MapId);
        Assert.Empty(r.Policy.DoorSkips);
        Assert.Empty(r.Policy.RewardStalls);
        Assert.Equal(0, r.Run.Fairness.UnseenCharges);
        Assert.True(r.Run.Fairness.KiteSwitches <= r.KiteSeconds + 1f, $"kite_switches {r.Run.Fairness.KiteSwitches} > kite 적 초 {r.KiteSeconds:0}");
        Assert.True(r.PeakAlive <= 360, $"동시 {r.PeakAlive} > 절대 상한 360");
        Assert.True(r.Spawned > 200, $"일반 생성 {r.Spawned} 마리가 너무 적다");
    }

    [Fact]
    [Trait("Category", TestCategories.Scenario)]
    public void Same_seed_same_hashes_and_room_records()
    {
        var defs = Wp4Data.Defs;
        var a = ActRunner.Run(defs, "stage_cathedral", 11, modules: Modules, act: NoMerchants(defs.Stages.Get("stage_cathedral")));
        var b = ActRunner.Run(defs, "stage_cathedral", 11, modules: Modules, act: NoMerchants(defs.Stages.Get("stage_cathedral")));
        Assert.Equal(a.Hashes, b.Hashes);
        Assert.Equal(a.Run.Rooms, b.Run.Rooms);
        var c = ActRunner.Run(defs, "stage_cathedral", 12, modules: Modules, act: NoMerchants(defs.Stages.Get("stage_cathedral")));
        Assert.NotEqual(a.Hashes, c.Hashes);
    }

    [Fact]
    [Trait("Category", TestCategories.Scenario)]
    public void Room_visit_mode_touches_every_inner_room()
    {
        var defs = Wp4Data.Defs;
        var r = ActRunner.Run(defs, "stage_cathedral", 3, visit: AutoplayVisit.Rooms, modules: Modules, act: NoMerchants(defs.Stages.Get("stage_cathedral")));
        foreach (var s in r.Policy.RoomStats)
            output.WriteLine($"VISITS map={s.MapId} room={s.RoomId} visits={s.Visits} skips={s.Skips} inside_s={s.TicksInside * World.Dt:0.0}");
        Assert.Equal(RunOutcome.Victory, r.Outcome);
        Assert.Empty(r.Policy.DoorSkips);
        // 방 맵 풀이 작은 방 다섯(기획 v0.9 V2)이라 판마다 맵이 다르다 — 이 판에서 실제로 돈 맵의 안쪽 방(room_ 기준점)을 모두 한 번 이상 들른다
        var expected = r.Run.Rooms.Select(x => x.MapId).Distinct()
            .SelectMany(m => defs.Maps.Get(m).AnchorsTile.Keys.Where(k => k.StartsWith("room_", StringComparison.Ordinal)).Select(k => (Map: m, Room: k)))
            .ToList();
        Assert.NotEmpty(expected);
        foreach (var (map, id) in expected)
            Assert.True(r.Policy.RoomStats.Where(s => s.RoomId == id && s.MapId.StartsWith(map, StringComparison.Ordinal)).Sum(s => s.Visits) >= 1, $"{map} 의 {id} 를 들르지 않았다");
    }

    /// <summary>
    /// 상인 가중을 0 으로 바꾼 액트(시험 안에서 <c>with</c> — 상인 창 본문 없이 도는 판). <see cref="Wp4Data"/> 의 보상은 대역이라 종류 대신 ID 로도 거른다.
    /// </summary>
    public static StageDef NoMerchants(StageDef act) => act with
    {
        Rewards = act.Rewards with
        {
            Weights = [.. act.Rewards.Weights.Where(w => w.Ref.Def.Kind != RewardKind.Merchant && w.Ref.Id is not ("reward_smith" or "reward_witch" or "reward_healer"))],
        },
    };
}
