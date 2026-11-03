using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Map;

/// <summary>10.1 <c>Map</c> 행 v0.7 — <c>GateZones</c>(나가는 자리·맞은편 문), [구현-2] <c>MapArea</c>·13.5-23 ③ 맵 쪽(<c>ValidateRooms</c>).</summary>
public class GateZonesTests
{
    [Fact]
    public void Compute_GateOpenMegasWalkableCells_RowMajor_Deterministic()
    {
        var map = TestDefs.Map(12);
        var grid = ArenaLayout.Expand(map);
        var zones = GateZones.Compute(map, grid);
        Assert.Equal(["gate_ne", "gate_nw", "gate_se", "gate_sw"], zones.Keys.Order(StringComparer.Ordinal));
        // nw 문: 메가 (0,5)·(0,6) — 하위 x 0..1, y 10..13 의 걷기 칸(합성 문 열린 칸은 "....")
        var nw = zones["gate_nw"];
        Assert.Equal(GateSide.Nw, nw.Side);
        Assert.Equal(8, nw.Cells.Count);
        Assert.All(nw.Cells, c => Assert.InRange(c.X, 0, 1));
        Assert.All(nw.Cells, c => Assert.InRange(c.Y, 10, 13));
        Assert.Equal(nw.Cells.OrderBy(c => c.Y).ThenBy(c => c.X), nw.Cells);
        Assert.All(nw.Cells, c => Assert.True(grid.IsWalkable(c.X, c.Y)));
        var se = zones["gate_se"];
        Assert.All(se.Cells, c => Assert.InRange(c.X, 22, 23));
        var again = GateZones.Compute(map, grid);
        foreach (var (id, z) in zones) Assert.Equal(z.Cells, again[id].Cells);
    }

    [Fact]
    public void Opposite_IsAcrossSides()
    {
        var map = TestDefs.Map(12);
        Assert.Equal("gate_se", GateZones.Opposite(map, "gate_nw"));
        Assert.Equal("gate_nw", GateZones.Opposite(map, "gate_se"));
        Assert.Equal("gate_sw", GateZones.Opposite(map, "gate_ne"));
        Assert.Equal("gate_ne", GateZones.Opposite(map, "gate_sw"));
        Assert.Null(GateZones.Opposite(map, "gate_x"));
        var threeGates = map with { Layout = map.Layout with { Gates = [.. map.Layout.Gates.Where(g => g.Id != "gate_se")] } };
        Assert.Null(GateZones.Opposite(threeGates, "gate_nw"));
    }

    [Fact]
    public void RealCathedral_FourZonesNotEmpty()
    {
        var map = RealData.Cathedral;
        var zones = GateZones.Compute(map, ArenaLayout.Expand(map));
        Assert.Equal(4, zones.Count);
        Assert.All(zones.Values, z => Assert.InRange(z.Cells.Count, 1, 8));
    }

    // ── [구현-2] 걸을 넓이·13.5-23 맵 쪽 ─────────────────────────────

    [Fact]
    public void MapArea_FloodFromHeroStart()
    {
        var empty = TestDefs.Map(12, place: []);
        var grid = ArenaLayout.Expand(empty);
        var all = 0;
        for (var y = 0; y < grid.Size; y++)
            for (var x = 0; x < grid.Size; x++)
                if (grid.IsWalkable(x, y)) all++;
        Assert.Equal(all, MapArea.WalkableCells(grid));                 // 빈 방은 모든 걷기 칸이 이어짐
        var withPillars = TestDefs.Map(12);
        Assert.Equal(all - 4, MapArea.WalkableCells(ArenaLayout.Expand(withPillars)));   // 기둥 넷(위-왼 한 칸씩)
        var blocked = empty with { HeroStart = new HeroStartDef { Tile = new TileCoord(0, 0) } };
        Assert.Equal(0, MapArea.WalkableCells(ArenaLayout.Expand(blocked)));
    }

    private static List<LayoutError> Validate(MapDef map)
    {
        var errors = new List<LayoutError>();
        var grid = ArenaLayout.TryExpand(map, map.Tileset.Def, errors);
        Assert.NotNull(grid);
        ArenaLayout.ValidateRooms(map, grid, errors);
        return errors;
    }

    [Fact]
    public void RoomMap_SyntheticSmallRoom_Passes_AndWithoutRoomKindsNotChecked()
    {
        var map = TestDefs.Map(16, "small", roomKinds: [RoomKind.Normal, RoomKind.Elite]);
        Assert.True(ArenaLayout.Expand(map).Size <= RoomMapRules.SmallRoomMaxN);
        var errors = Validate(map);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void RoomMap_GatesEntriesCenter_Required()
    {
        var map = TestDefs.Map(16, "small", roomKinds: [RoomKind.Normal]);
        var three = map with { Layout = map.Layout with { Gates = [.. map.Layout.Gates.Where(g => g.Id != "gate_sw")] }, Entries = [.. map.Entries.Where(e => e.Id != "gate_sw")] };
        var e1 = Validate(three);
        Assert.Contains(e1, e => e.Path == "$.layout.gates");
        Assert.Contains(e1, e => e.Path == "$.entries");
        var renamed = map with { Layout = map.Layout with { Gates = [.. map.Layout.Gates.Select(g => g.Id == "gate_ne" ? g with { Id = "gate_n" } : g)] } };
        Assert.Contains(Validate(renamed), e => e.Path == "$.layout.gates");
        // 이름은 그대로, 변만 서로 바꿈(gate_nw 가 ne 변·gate_ne 가 nw 변)
        var swapped = map with { Layout = map.Layout with { Gates = [.. map.Layout.Gates.Select(g =>
            g.Id == "gate_nw" ? g with { Side = GateSide.Ne } : g.Id == "gate_ne" ? g with { Side = GateSide.Nw } : g)] } };
        Assert.Contains(Validate(swapped), e => e.Path == "$.layout.gates");
        var noCenter = map with { AnchorsTile = new Dictionary<string, TileCoord>() };
        Assert.Contains(Validate(noCenter), e => e.Path == "$.anchors");
        Assert.DoesNotContain(Validate(noCenter with { RoomKinds = [] }), e => e.Path == "$.anchors");   // 방 맵이 아니면 안 봄
    }

    [Fact]
    public void SmallRoom_BlockedNearEntry_Errors_BigRoomNotChecked()
    {
        // sw 진입점 (15.5, 26) 근처 메가 (7,12) 에 기둥(위-왼 칸 (14,24) — 진입점에서 약 60px)
        var place = new PiecePlacementDef[] { new() { Piece = "pillar", At = [new(7, 12)] } };
        var small = TestDefs.Map(16, "small", place: place, roomKinds: [RoomKind.Normal]);
        var errors = Validate(small);
        var e = Assert.Single(errors);
        Assert.Equal("$.entries[3]", e.Path);
        Assert.Contains("gate_sw", e.Message);
        // 큰 방(N 40 > 36)은 작은 방 검사를 하지 않는다
        var bigPlace = new PiecePlacementDef[] { new() { Piece = "pillar", At = [new(9, 16)] } };
        Assert.Empty(Validate(TestDefs.Map(20, "big", place: bigPlace, roomKinds: [RoomKind.Boss])));
    }

    [Fact]
    public void SmallRoom_OneDoorInnerRoom_Errors()
    {
        var one = TestDefs.RoomMapOneDoor();
        var small = TestDefs.RoomMap(one.Layout.Place, "small_one_door", megaTiles: 16,
                                     anchors: new Dictionary<string, TileCoord> { ["room_a"] = new(11, 11) },
                                     roomAreas: new Dictionary<string, RectI> { ["room_a"] = new(3, 3, 8, 8) }) with { RoomKinds = [RoomKind.Normal] };
        var errors = Validate(small);
        Assert.Contains(errors, e => e.Path == "$.room_areas.room_a" && e.Message.Contains("문 하나인 방 바닥"));
        Assert.DoesNotContain(Validate(small with { RoomKinds = [] }), e => e.Message.Contains("문 하나인 방 바닥"));
    }

    [Fact]
    public void RealCathedral_AsRoomMapCopy_Passes()
    {
        var copy = RealData.Cathedral with { RoomKinds = [RoomKind.Normal, RoomKind.Boss] };
        var errors = Validate(copy);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
