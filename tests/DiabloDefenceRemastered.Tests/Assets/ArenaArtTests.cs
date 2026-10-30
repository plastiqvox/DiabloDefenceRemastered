using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Tests.Assets;

public class ArenaArtTests
{
    [Fact]
    public void FloorChunks_MatchHandCount()
    {
        // M 10 → N 20, 외곽 상자 1280×768 → 조각 후보 3×2. 손으로 센 결과(합성 타일셋, 기둥 네 개, 네 문):
        //  (0,0)·(1,0) — 위쪽 바닥. (2,0) — 오른쪽 꼭짓점 근처 (17,1) 은 윗줄 뒷벽 "XX.." 의 안쪽 반이라 바닥.
        //  (0,1)·(1,1) — 아래쪽 바닥(예: (6,17) 중심 (288, 512) — 마름모 상자 y 496..528 이 두 줄에 걸침).
        //  (2,1) 없음 — x ≥ 1024 이려면 tx − ty ≥ 12, y ≥ 512 이려면 tx + ty ≥ 23 → tx ≥ 18 인데 그 줄은 앞둘레 "XXXX"(문 틈 (19,8) 은 x 끝이 1024 라 안 겹침).
        var grid = ArenaLayout.Expand(TestDefs.Map(10));
        Assert.Equal('.', grid.Prop(17, 1));
        Assert.Equal('.', grid.Prop(6, 17));
        Assert.Equal(new Vector2(288, 512), grid.Iso.CellCenter(6, 17));
        var chunks = ArenaArt.FloorChunks(grid, grid.Iso);
        Assert.Equal([new Int2(0, 0), new Int2(1, 0), new Int2(2, 0), new Int2(0, 1), new Int2(1, 1)], chunks);
        Assert.Equal("floor_2_0.png", ArenaArt.FloorChunkFile(new Int2(2, 0)));
        Assert.Equal(new Int2(1024, 0), ArenaArt.FloorChunkPos(new Int2(2, 0)));
    }

    [Fact]
    public void FloorChunks_SmallBox_IsOneRow()
    {
        var grid = ArenaLayout.Expand(TestDefs.Map(6));   // 상자 768×512, 맨 아래 칸 중심 y 496 → 상자 끝 512 라 둘째 줄과 안 겹침
        Assert.Equal([new Int2(0, 0), new Int2(1, 0)], ArenaArt.FloorChunks(grid));
    }

    [Fact]
    public void OutputNames_ArtFolder()
    {
        var grid = ArenaLayout.Expand(TestDefs.Map(10));
        Assert.Equal(
        [
            "d1/maps/test_map/arena.json", "d1/maps/test_map/columns.json", "d1/maps/test_map/columns.png",
            "d1/maps/test_map/floor_0_0.png", "d1/maps/test_map/floor_0_1.png", "d1/maps/test_map/floor_1_0.png",
            "d1/maps/test_map/floor_1_1.png", "d1/maps/test_map/floor_2_0.png", "d1/maps/test_map/minimap.png",
        ], ArenaArt.OutputNames(grid));
    }

    private static string ArenaJson(ArenaGrid grid, string cells, string? hash = null)
    {
        var floor = string.Join(", ", ArenaArt.FloorChunks(grid).Select(c =>
            $$"""{ "file": "{{ArenaArt.FloorChunkFile(c)}}", "pos": [{{c.X * 512}}, {{c.Y * 512}}] }"""));
        return $$"""
            { "format": 1, "map": "test_map", "layout_hash": "{{hash ?? grid.LayoutHash}}", "box": [1280, 768],
              "floor": [ {{floor}} ], "columns": "columns", "cells": [ {{cells}} ] }
            """;
    }

    [Fact]
    public void ArenaJson_ValidAgainstGrid_CellsFrameOneBased()
    {
        var grid = ArenaLayout.Expand(TestDefs.Map(10));
        var art = ArenaArt.Parse(ArenaJson(grid, "[0, 0, 0, 1], [1, 0, 0, 2], [6, 6, 0, 3], [7, 6, 1, 4], [7, 6, 2, 1]"));
        Assert.Empty(art.Validate(grid));
        Assert.Equal(new ArenaCell(7, 6, ArenaCellKind.Overlay, 1), art.Cells[4]);

        var again = ArenaArt.Parse(System.Text.Encoding.UTF8.GetString(AssetJson.Serialize(art)));
        Assert.Equal(art.Cells, again.Cells);
    }

    [Fact]
    public void ArenaJson_Errors()
    {
        var grid = ArenaLayout.Expand(TestDefs.Map(10));
        var art = ArenaArt.Parse(ArenaJson(grid, "[0, 0, 0, 0], [5, 0, 0, 1], [4, 0, 0, 1], [30, 0, 0, 1]", hash: "deadbeef"));
        var errors = art.Validate(grid);
        Assert.Contains(errors, e => e.Contains("layout_hash"));
        Assert.Contains(errors, e => e.Contains("cells[0]") && e.Contains("1부터"));
        Assert.Contains(errors, e => e.Contains("cells[2]") && e.Contains("순"));
        Assert.Contains(errors, e => e.Contains("cells[3]") && e.Contains("격자 밖"));

        Assert.Throws<System.Text.Json.JsonException>(() => ArenaArt.Parse(ArenaJson(grid, "[0, 0, 0]")));

        foreach (var bad in new[] { "", "sub/columns", "..", "a\\b" })
            Assert.Contains((art with { Columns = bad }).Validate(grid), e => e.Contains("기둥 아틀라스 이름"));
    }
}
