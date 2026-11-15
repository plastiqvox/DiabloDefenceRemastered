using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>
/// 바닥 핏웅덩이 자리(2026-11-15 사용자 결정 "기존의 피타일은 제거하고 핏웅덩이 몇군데"·"방마다 미리 몇군데"):
/// 실제 맵 전부(원판·거울판)에서 Min~Max 곳, 둘레까지 걸을 수 있는 칸, 서로 MinGap·문에서 GateMargin 떨어짐, 같은 key 면 같은 자리.
/// </summary>
public class BloodPoolsTests
{
    private static readonly BloodPoolsDef Def = new() { Min = 2, Max = 4, RadiusMin = 28f, RadiusMax = 48f, MinGap = 180f, GateMargin = 110f };

    [Fact]
    public void RealMaps_CountInRange_OnOpenFloor_SpacedAndAwayFromGates_Stable()
    {
        var maps = RealData.Maps.Maps.SelectMany(m => m.Mirrored is { } mm ? new[] { m, mm } : [m]).ToList();
        Assert.NotEmpty(maps);
        foreach (var map in maps)
        {
            var grid = ArenaLayout.Expand(map);
            var gates = GateZones.Compute(map, grid).Values.SelectMany(z => z.Cells).Select(c => grid.Iso.CellCenter(c.X, c.Y)).ToList();
            for (var seed = 1ul; seed <= 8ul; seed++)
            {
                var key = BloodPools.Key(seed, map.Id);
                var pools = BloodPools.Place(grid, Def, key, gates);
                Assert.InRange(pools.Count, Def.Min, Def.Max);
                foreach (var p in pools)
                {
                    var c = grid.Iso.CellOf(p.Pos);
                    for (var dy = -1; dy <= 1; dy++)
                        for (var dx = -1; dx <= 1; dx++)
                            Assert.True(grid.IsWalkable(c.X + dx, c.Y + dy), $"{map.Id} {c} 둘레가 막힘");
                    Assert.All(gates, g => Assert.True(Vector2.Distance(g, p.Pos) >= Def.GateMargin, $"{map.Id} 문 가까이"));
                    Assert.InRange(p.Radius, Def.RadiusMin, Def.RadiusMax);
                    Assert.InRange(p.Seed, 0f, 1f);
                }
                for (var i = 0; i < pools.Count; i++)
                    for (var j = i + 1; j < pools.Count; j++)
                        Assert.True(Vector2.Distance(pools[i].Pos, pools[j].Pos) >= Def.MinGap, $"{map.Id} 너무 붙음");
                Assert.Equal(pools, BloodPools.Place(grid, Def, key, gates));   // 같은 방은 늘 같은 자리
            }
        }
    }

    [Fact]
    public void DifferentRoomSeeds_UsuallyDifferentSpots()
    {
        var map = RealData.Cathedral;
        var grid = ArenaLayout.Expand(map);
        var a = BloodPools.Place(grid, Def, BloodPools.Key(1, map.Id), []);
        var b = BloodPools.Place(grid, Def, BloodPools.Key(2, map.Id), []);
        Assert.NotEqual(a, b);
    }

    /// <summary>밟기 물결(2026-11-15 "물웅덩이 밟는 효과"): 움직이며 웅덩이 안을 밟을 때만, 웅덩이마다 0.22초 간격, 최근 4개.</summary>
    [Fact]
    public void Ripples_OnlyMovingInsidePool_Throttled_KeepsLatestFour()
    {
        var pools = new[] { new BloodPool(new Vector2(100, 100), 40f, 0.3f), new BloodPool(new Vector2(400, 100), 40f, 0.6f) };
        var r = new BloodRipples();
        static int Live(BloodRipples r, int i) { var n = 0; foreach (var x in r.Of(i)) if (float.IsFinite(x.Start)) n++; return n; }

        r.Observe(pools, 0f, new Vector2(100, 100), new Vector2(100, 100));      // 서 있음 — 없음
        Assert.Equal(0, Live(r, 0));
        r.Observe(pools, 0f, new Vector2(200, 100), new Vector2(199, 100));      // 웅덩이 밖 — 없음
        Assert.Equal(0, Live(r, 0));
        r.Observe(pools, 0f, new Vector2(110, 104), new Vector2(108, 104));      // 밟음
        Assert.Equal(1, Live(r, 0));
        Assert.Equal(0, Live(r, 1));                                              // 다른 웅덩이는 그대로
        var first = r.Of(0)[0];
        Assert.Equal(new Vector2(0.25f, 0.2f), first.Local);                      // 세로는 반경 절반 기준
        r.Observe(pools, 0.1f, new Vector2(112, 104), new Vector2(110, 104));    // 간격 안 — 더 안 생김
        Assert.Equal(1, Live(r, 0));
        for (var t = 1; t <= 6; t++) r.Observe(pools, t * 0.3f, new Vector2(100, 100), new Vector2(98, 100));
        Assert.Equal(BloodRipples.PerPool, Live(r, 0));
        Assert.DoesNotContain(first, r.Of(0).ToArray());                          // 가장 오래된 것부터 덮음
    }

    [Fact]
    public void RealMaps_NoBloodTiles()
    {
        // 피 타일(피 벽·피 바닥)은 지웠다 — 피는 셰이더 웅덩이로
        var l1 = RealData.L1;
        var blood = new[] { "blood_floor", "blood_wall_v", "blood_wall_h", "blood_wall_corner" }.Select(n => l1.Pieces[n]).ToHashSet();
        foreach (var map in RealData.Maps.Maps)
        {
            var grid = ArenaLayout.Expand(map);
            for (var my = 0; my < grid.MegaSize; my++)
                for (var mx = 0; mx < grid.MegaSize; mx++)
                    Assert.DoesNotContain(grid.MegaTile(mx, my), blood);
        }
    }
}
