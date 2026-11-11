using DiabloDefenceRemastered.Tests.Sim;

namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 방 맵 덱 순서 (기획 10.8 v0.9 "고르기"·설계 13.23 33-6 — WP4 <c>RunPlanner</c> ①): 액트 시작 때 섞은 순서를 차례로 쓰고, 풀을 다 쓰기 전에는 같은 맵을 다시 쓰지 않으며,
/// 다시 섞은 첫 장은 바로 앞 방 맵이 아니다. 같은 씨앗이면 같은 순서. 실제 <c>stage_cathedral</c>(작은 방 다섯 × 거울판)과, 같은 풀로 방을 늘린 액트로 본다.
/// </summary>
public sealed class DeckOrderTests
{
    private static readonly GameDefs Defs = Wp4Data.Defs;
    private static StageDef Act => Defs.Stages.Get("stage_cathedral");
    private static RoomRulesDef Rules => Defs.Rules.Rooms!;

    /// <summary>같은 풀로 일반 방 <paramref name="count"/> 개(덱을 여러 번 다시 섞게).</summary>
    private static StageDef LongAct(int count) =>
        Act with { Rooms = [.. Enumerable.Range(1, count).Select(n => TestDefs.Room(RoomKind.Normal, (n - 1) * 60f, n * 60f, n))] };

    private static string Base(RoomPlan p) => p.MapId;

    [Fact]
    public void Same_seed_same_map_order()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var a = RunPlanner.PlanAct(Act, 1, seed, Rules).Rooms.Select(r => r.Map.Id).ToList();
            var b = RunPlanner.PlanAct(Act, 1, seed, Rules).Rooms.Select(r => r.Map.Id).ToList();
            Assert.Equal(a, b);
        }
        // 씨앗이 다르면 순서가 달라진다(적어도 한 쌍)
        var orders = Enumerable.Range(1, 20).Select(s => string.Join(",", RunPlanner.PlanAct(Act, 1, (ulong)s, Rules).Rooms.Select(r => r.Map.Id))).Distinct().Count();
        Assert.True(orders > 10, $"씨앗 20 개의 순서가 {orders} 가지뿐");
    }

    [Fact]
    public void No_repeat_before_pool_is_used_up_and_boss_room_keeps_arena()
    {
        var poolSize = Act.RoomMaps.Normal.Select(r => r.Id).Union(Act.RoomMaps.Elite.Select(r => r.Id)).Count();
        Assert.Equal(5, poolSize);
        for (ulong seed = 1; seed <= 50; seed++)
        {
            var rooms = RunPlanner.PlanAct(Act, 1, seed, Rules).Rooms;
            var small = rooms.Where(r => r.Kind != RoomKind.Boss).Select(Base).ToList();
            Assert.Equal(8, small.Count);
            // 앞 다섯 장은 서로 다르고(풀을 다 씀), 같은 맵은 액트에서 많아야 두 번(기획 10.8 "풀 5장이면 같은 맵은 많아야 두 번")
            Assert.Equal(poolSize, small.Take(poolSize).Distinct().Count());
            Assert.All(small.GroupBy(m => m), g => Assert.True(g.Count() <= 2, $"씨앗 {seed}: {g.Key} 가 {g.Count()} 번"));
            Assert.Equal("cathedral_arena", rooms[^1].MapId);
        }
    }

    [Fact]
    public void Reshuffled_first_card_is_not_previous_room_map_and_both_orientations_appear()
    {
        var act = LongAct(31);   // 다섯 장 덱을 여섯 번 넘게 섞는다
        var mirrored = 0;
        var total = 0;
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var rooms = RunPlanner.PlanAct(act, 1, seed, Rules).Rooms;
            for (var start = 0; start < rooms.Count; start += 5)
            {
                var chunk = rooms.Skip(start).Take(5).Select(Base).ToList();
                if (chunk.Count == 5) Assert.Equal(5, chunk.Distinct().Count());                       // 덱 한 바퀴 = 다섯 맵 모두
                if (start > 0) Assert.NotEqual(Base(rooms[start - 1]), chunk[0]);                      // 다시 섞은 첫 장 ≠ 바로 앞 방 맵
            }
            for (var i = 1; i < rooms.Count; i++) Assert.NotEqual(Base(rooms[i - 1]), Base(rooms[i]));  // 연달아 같은 맵 없음
            mirrored += rooms.Count(r => r.Mirrored);
            total += rooms.Count;
        }
        // 방향은 섞을 때 장마다 — 원판·거울판이 대략 반씩
        Assert.InRange(mirrored / (float)total, 0.35f, 0.65f);
    }

    [Fact]
    public void Single_map_pool_alternates_orientation()
    {
        var one = Act with
        {
            RoomMaps = Act.RoomMaps with { Normal = [Act.RoomMaps.Normal[0]], Elite = [] },
            Rooms = [.. Enumerable.Range(1, 6).Select(n => TestDefs.Room(RoomKind.Normal, (n - 1) * 60f, n * 60f, n))],
        };
        var rooms = RunPlanner.PlanAct(one, 1, 9, Rules).Rooms;
        for (var i = 1; i < rooms.Count; i++) Assert.NotEqual(rooms[i - 1].Mirrored, rooms[i].Mirrored);
    }
}
