using DiabloDefenceRemastered.Core.Sim.Autoplay;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 단계 1 시나리오 (설계서 11.1 WP4 완료 기준 단계 1·10.1 <c>Sim</c>): 실제 <c>stage_cathedral</c>(단계 1 판, 일반 방 여섯) 을 <c>RunSession</c> + 자동 조작 + <c>--god</c> 로 끝까지.
/// 예외 없음, 방 틱 상한 통과, <c>DOOR_SKIP</c>·<c>REWARD_STALL</c> 0, 공정성(<c>UnseenCharges</c> 0·<c>kite_switches</c> ≤ kite 적 초당 1), 같은 씨앗 두 번의 해시·<c>RoomRecord</c> 같음.
/// 이웃 묶음(WP2 이동·WP3 전투)은 <see cref="FakeArena"/> 가짜다 — 실제 이동·충돌·피해로 도는 실행은 합친 뒤(WP6 통합).
/// </summary>
public sealed class Stage1ScenarioTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", TestCategories.Scenario)]
    public void Slice_act_runs_to_victory_with_autoplay_and_god()
    {
        var defs = Wp4Data.Defs;
        var r = ActRunner.Run(defs, "stage_cathedral", 7);
        output.WriteLine($"ticks={r.Ticks} ({r.Ticks * World.Dt / 60f:0.0}분) rooms={r.Run.Rooms.Count} peak={r.PeakAlive} spawned={r.Spawned} band={r.BandSpawns} fallback={r.Fallbacks}");
        foreach (var room in r.Run.Rooms)
            output.WriteLine($"ROOM act={room.Act} room={room.Number} map={room.MapId} gate_in={room.GateIn} reward={room.RewardId ?? "-"} door={room.Door ?? "-"} clear_tick={room.ClearTick} cleanup_s={room.CleanupTicks * World.Dt:0.0} collapsed={(room.Collapsed ? 1 : 0)}");

        Assert.Equal(RunOutcome.Victory, r.Outcome);
        Assert.Equal([1, 2, 3, 4, 6, 8], r.Run.Rooms.Select(x => x.Number));
        Assert.Empty(r.Policy.DoorSkips);
        Assert.Empty(r.Policy.RewardStalls);
        Assert.Equal(0, r.Run.Fairness.UnseenCharges);
        Assert.True(r.Run.Fairness.KiteSwitches <= r.KiteSeconds + 1f, $"kite_switches {r.Run.Fairness.KiteSwitches} > kite 적 초 {r.KiteSeconds:0}");
        Assert.True(r.PeakAlive <= 300, $"동시 {r.PeakAlive} > 상한 300");
        Assert.True(r.Spawned > 200, $"일반 생성 {r.Spawned} 마리가 너무 적다");
    }

    [Fact]
    [Trait("Category", TestCategories.Scenario)]
    public void Same_seed_same_hashes_and_room_records()
    {
        var defs = Wp4Data.Defs;
        var a = ActRunner.Run(defs, "stage_cathedral", 11);
        var b = ActRunner.Run(defs, "stage_cathedral", 11);
        Assert.Equal(a.Hashes, b.Hashes);
        Assert.Equal(a.Run.Rooms, b.Run.Rooms);
        var c = ActRunner.Run(defs, "stage_cathedral", 12);
        Assert.NotEqual(a.Hashes, c.Hashes);
    }

    [Fact]
    [Trait("Category", TestCategories.Scenario)]
    public void Room_visit_mode_touches_every_inner_room()
    {
        var r = ActRunner.Run(Wp4Data.Defs, "stage_cathedral", 3, visit: AutoplayVisit.Rooms);
        foreach (var s in r.Policy.RoomStats)
            output.WriteLine($"VISITS map={s.MapId} room={s.RoomId} visits={s.Visits} skips={s.Skips} inside_s={s.TicksInside * World.Dt:0.0}");
        Assert.Equal(RunOutcome.Victory, r.Outcome);
        Assert.Empty(r.Policy.DoorSkips);
        // 방 맵 풀이 작은 방 다섯(기획 v0.9 V2)이라 판마다 맵이 다르다 — 이 판에서 실제로 돈 맵의 안쪽 방(room_ 기준점)을 모두 한 번 이상 들른다
        var expected = r.Run.Rooms.Select(x => x.MapId).Distinct()
            .SelectMany(m => Wp4Data.Defs.Maps.Get(m).AnchorsTile.Keys.Where(k => k.StartsWith("room_", StringComparison.Ordinal)).Select(k => (Map: m, Room: k)))
            .ToList();
        Assert.NotEmpty(expected);                                           // 씨앗 3 은 안쪽 방이 있는 맵을 지난다
        foreach (var (map, id) in expected)
            Assert.True(r.Policy.RoomStats.Where(s => s.RoomId == id && s.MapId.StartsWith(map, StringComparison.Ordinal)).Sum(s => s.Visits) >= 1, $"{map} 의 {id} 를 들르지 않았다");
    }
}
