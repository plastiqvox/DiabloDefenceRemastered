using DiabloAssetImporter.Commands;
using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Tests.TestKit;

namespace DiabloAssetImporter.Tests;

/// <summary>설계서 10.1 <c>Pipeline</c> "<c>audit</c> DUN 대조"(13.5-22): 시험 인코더로 만든 합성 DUN 과 실제 <c>cathedral_arena</c> 배치.</summary>
public class AuditTests
{
    private static (ArenaGrid Grid, HashSet<int> Floor) Cathedral()
    {
        var data = MapDataReader.Read(Repo.DataDir);
        Assert.Empty(data.Errors);
        var map = data.Maps.Single(m => m.Id == "cathedral_arena");
        return (ArenaLayout.Expand(map), [.. map.Tileset.Def.Floor]);
    }

    /// <summary>격자의 4×4 창을 DUN 으로(바닥 번호 → 0, 나머지 번호 그대로). <paramref name="keep"/> 가 거짓인 칸은 0.</summary>
    private static DunFile Window(ArenaGrid g, HashSet<int> floor, int mx, int my, Func<int, int, bool>? keep = null)
    {
        var cells = new List<int> { 4, 4 };
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                var v = g.MegaTile(mx + x, my + y);
                cells.Add(floor.Contains(v) || keep?.Invoke(x, y) == false ? 0 : v);
            }
        return DunFile.Read(Encoders.U16s([.. cells]));
    }

    private static int NonFloor(ArenaGrid g, HashSet<int> floor, int mx, int my)
    {
        var n = 0;
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                if (!floor.Contains(g.MegaTile(mx + x, my + y))) n++;
        return n;
    }

    [Fact]
    public void OurOwnWindow_Is4_FloorOnly_And_BelowThreshold_AreClean()
    {
        var (g, floor) = Cathedral();
        var m = g.MegaSize;
        var windows = (from my in Enumerable.Range(0, m - 3) from mx in Enumerable.Range(0, m - 3) select (mx, my)).ToList();

        // 우리 배열을 4×4 그대로 담은 DUN → 걸림
        var (bx, by) = windows.First(w => NonFloor(g, floor, w.mx, w.my) >= AuditCommand.MinNonFloor);
        var hits = AuditCommand.CompareDun(g, floor, Window(g, floor, bx, by), "same.dun").ToList();
        Assert.Contains($"maps/cathedral_arena.json ({bx},{by}): same.dun (0,0) 와 4×4 배열이 같다", hits);

        // 바닥만 → 안 걸림
        Assert.Empty(AuditCommand.CompareDun(g, floor, DunFile.Read(Encoders.U16s([4, 4, .. new int[16]])), "floor.dun"));

        // 바닥 아닌 칸이 문턱보다 적은 창(벽 모서리 하나 같은 흔한 모양) → 안 걸림
        var small = windows.Where(w => NonFloor(g, floor, w.mx, w.my) is > 0 and < AuditCommand.MinNonFloor).ToList();
        Assert.NotEmpty(small);
        foreach (var (sx, sy) in small.Take(20))
            Assert.Empty(AuditCommand.CompareDun(g, floor, Window(g, floor, sx, sy), "corner.dun"));

        // 한 칸만 달라도 안 걸림
        var oneOff = Window(g, floor, bx, by, (x, y) => !(x == 0 && y == 0 && !floor.Contains(g.MegaTile(bx, by))));
        if (!floor.Contains(g.MegaTile(bx, by)))
            Assert.DoesNotContain(AuditCommand.CompareDun(g, floor, oneOff, "off.dun"), h => h.Contains($"({bx},{by}): off.dun (0,0)", StringComparison.Ordinal));
    }
}
