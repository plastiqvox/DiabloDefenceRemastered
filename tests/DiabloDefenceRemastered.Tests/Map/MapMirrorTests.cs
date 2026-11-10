using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Map;

/// <summary>
/// 10.1 <c>Map</c> 행(v0.8 → v0.8.1, WP1f #5·#6) — 거울판 <c>MapMirror.Of</c>·짝 표 검사·자리 짝 검사·입구-문 검사·<c>MapDataReader</c> 의 거울판.
/// </summary>
public class MapMirrorTests
{
    private const int RGrateV = 52, RDoorClosedV = 53;

    /// <summary>안쪽 방 합성 타일셋 + 짝 조각(세로 창살 <c>X.w.</c>·세로 닫힌 문 <c>X.X.</c>) + 짝 표.</summary>
    private static TilesetDef MirrorRoomTileset()
    {
        var ts = TestDefs.RoomTileset();
        return ts with
        {
            Pieces = new Dictionary<string, int>(ts.Pieces) { ["grate_v"] = RGrateV, ["door_closed_v"] = RDoorClosedV },
            Props = new Dictionary<int, string>(ts.Props) { [RGrateV] = "X.w.", [RDoorClosedV] = "X.X." },
            Mirror = new TilesetMirrorDef
            {
                Pairs = [["wall_v", "wall_h"], ["wall_end_v", "wall_end_h"], ["blood_wall_v", "blood_wall_h"], ["grate_h", "grate_v"], ["door_closed", "door_closed_v"]],
                Self = ["pillar", "wall_corner", "blood_floor", "blood_wall_corner", "arch"],
            },
        };
    }

    /// <summary>6.11 예 배치(합성 이름) — 방 맵(문 넷·center)으로.</summary>
    private static MapDef Example() =>
        TestDefs.RoomMapExample() with { Tileset = DefRef<TilesetDef>.To(MirrorRoomTileset()), RoomKinds = [RoomKind.Normal] };

    [Fact]
    public void Of_SubCellPropsAreTransposeOfOriginal_EveryCell()
    {
        var map = Example();
        var mirror = MapMirror.Of(map, map.Tileset.Def);
        var a = ArenaLayout.Expand(map);
        var b = ArenaLayout.Expand(mirror);
        Assert.Equal(a.Size, b.Size);
        const CellFlags bits = CellFlags.BlocksWalk | CellFlags.BlocksSight | CellFlags.BlocksProjectiles | CellFlags.Boundary;
        for (var ty = 0; ty < a.Size; ty++)
            for (var tx = 0; tx < a.Size; tx++)
                Assert.True((a.Flags(tx, ty) & bits) == (b.Flags(ty, tx) & bits), $"({tx},{ty}) {a.Prop(tx, ty)} ≠ 거울판 ({ty},{tx}) {b.Prop(ty, tx)}");
        Assert.NotEqual(a.LayoutHash, b.LayoutHash);                     // 배치가 다름(바닥 섞기도 거울판 ID)
    }

    [Fact]
    public void Of_Ids_Art_Positions_Rects_Facing()
    {
        var map = Example() with { HeroStart = new HeroStartDef { Tile = new TileCoord(10f, 25f), Facing = Compass.SouthWest } };
        var m = MapMirror.Of(map, map.Tileset.Def);
        Assert.Equal("test_room_example@m", m.Id);
        Assert.Equal("test_room_example", m.MirrorOf);
        Assert.True(m.IsMirror);
        Assert.False(map.IsMirror);
        Assert.Equal(map.Art + "@m", m.Art);
        Assert.Equal(map.Size, m.Size);
        Assert.Equal(map.RoomKinds, m.RoomKinds);
        Assert.Equal(map.Layout.Border, m.Layout.Border);

        // 문: ID·변 짝, at 그대로
        Assert.Equal(map.Layout.Gates.Select(g => (GateZones.MirrorGate(g.Id), GateZones.MirrorSide(g.Side), g.At)), m.Layout.Gates.Select(g => (g.Id, g.Side, g.At)));
        Assert.Equal(["gate_ne", "gate_nw", "gate_sw", "gate_se"], m.Layout.Gates.Select(g => g.Id));
        // 입구: ID 도 짝(v0.8.1), 좌표 뒤집기
        foreach (var e in map.Entries)
        {
            var f = Assert.Single(m.Entries, x => x.Id == GateZones.MirrorGate(e.Id));
            Assert.Equal(new TileCoord(e.InsideTile.Ty, e.InsideTile.Tx), f.InsideTile);
            Assert.Equal(new TileCoord(e.OutsideTile.Ty, e.OutsideTile.Tx), f.OutsideTile);
        }
        // 기준점 ID 는 그대로, 좌표만
        Assert.Equal(map.AnchorsTile.Keys.Order(), m.AnchorsTile.Keys.Order());
        foreach (var (id, at) in map.AnchorsTile) Assert.Equal(new TileCoord(at.Ty, at.Tx), m.AnchorsTile[id]);
        Assert.Equal(new RectI(6, 6, 13, 13), m.OpenCenter);             // [x0,y0,x1,y1] → [y0,x0,y1,x1]
        Assert.Equal(new RectI(0, 13, 6, 19), m.RoomAreas["room_grate"]); // 원판 [13,0,19,6]
        Assert.Equal(new TileCoord(25f, 10f), m.HeroStart.Tile);
        Assert.Equal(Compass.SouthEast, m.HeroStart.Facing);
        Assert.Equal(Compass.North, MapMirror.MirrorFacing(Compass.North));
        Assert.Equal(Compass.East, MapMirror.MirrorFacing(Compass.West));
        Assert.Equal(Compass.NorthWest, MapMirror.MirrorFacing(Compass.NorthEast));
    }

    [Fact]
    public void OfOf_LayoutEqualsOriginal()
    {
        var map = Example();
        var ts = map.Tileset.Def;
        var twice = MapMirror.Of(MapMirror.Of(map, ts), ts);
        Assert.Equal(Normalize(map.Layout), Normalize(twice.Layout));
        Assert.Equal(map.Entries.Select(e => (e.Id, e.InsideTile, e.OutsideTile)), twice.Entries.Select(e => (e.Id, e.InsideTile, e.OutsideTile)));
        Assert.Equal(map.OpenCenter, twice.OpenCenter);
        Assert.Equal(map.RoomAreas.OrderBy(p => p.Key), twice.RoomAreas.OrderBy(p => p.Key));
        Assert.Equal(map.HeroStart, twice.HeroStart);
        // 하위 칸 성질도 그대로
        var a = ArenaLayout.Expand(map);
        var b = ArenaLayout.Expand(twice);
        for (var ty = 0; ty < a.Size; ty++)
            for (var tx = 0; tx < a.Size; tx++) Assert.Equal(a.Prop(tx, ty), b.Prop(tx, ty));
    }

    private static List<string> Normalize(ArenaLayoutDef layout)
    {
        var lines = layout.Gates.Select(g => $"gate {g.Id} {g.Side} {g.At}").ToList();
        foreach (var p in layout.Place)
        {
            var rows = p.Grid?.Select(r =>
            {
                var t = r.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                while (t.Count > 1 && t[^1] == ".") t.RemoveAt(t.Count - 1);
                return string.Join(' ', t);
            });
            lines.Add($"{p.Id}|{p.Piece}|{string.Join(";", p.At)}|{string.Join(";", p.Line ?? [])}|{p.Origin}|{string.Join("/", rows ?? [])}|{p.OverBorder}|{p.DecorWhy}");
        }
        return lines;
    }

    [Fact]
    public void GateZones_FloorsAndArea_OfMirrorAreTransposed_PassesRoomChecks()
    {
        var map = Example();
        var mirror = MapMirror.Of(map, map.Tileset.Def);
        var a = ArenaLayout.Expand(map);
        var b = ArenaLayout.Expand(mirror);
        var za = GateZones.Compute(map, a);
        var zb = GateZones.Compute(mirror, b);
        foreach (var (id, zone) in za)
            Assert.Equal(zone.Cells.Select(c => new Int2(c.Y, c.X)).OrderBy(c => (c.Y, c.X)), zb[GateZones.MirrorGate(id)].Cells.OrderBy(c => (c.Y, c.X)));

        var fa = RoomFloors.Compute(map, a);
        var fb = RoomFloors.Compute(mirror, b);
        Assert.Equal(fa.Rooms.Select(r => (r.Id, r.Cells.Count, r.Doors)), fb.Rooms.Select(r => (r.Id, r.Cells.Count, r.Doors)));
        Assert.Equal(MapArea.WalkableCells(a), MapArea.WalkableCells(b));

        var errors = new List<LayoutError>();
        ArenaLayout.ValidateRooms(map, a, errors);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        ArenaLayout.ValidateRooms(mirror, b, errors);                     // 13.5-19·23 ③ 을 거울판에도
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void EntryInFrontOfOtherGate_IsError_V081()
    {
        // 입구 ID 를 그대로 둔 고장 MapMirror 흉내(v0.8 의 "ID 그대로"): 입구 gate_nw 가 ne 변 — 곧 문 gate_ne 앞
        var map = Example();
        var mirror = MapMirror.Of(map, map.Tileset.Def);
        var broken = mirror with { Entries = [.. mirror.Entries.Select(e => e with { Id = GateZones.MirrorGate(e.Id) })] };
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateRooms(broken, ArenaLayout.Expand(broken), errors);
        Assert.Contains(errors, e => e.Path.StartsWith("$.entries", StringComparison.Ordinal) && e.Message.Contains("입구 gate_nw 가 문 gate_ne 앞에 있음"));

        // 원판·거울판은 가장 가까운 문 틈 칸이 같은 ID 문
        foreach (var m in new[] { map, mirror })
        {
            var grid = ArenaLayout.Expand(m);
            var zones = GateZones.Compute(m, grid);
            foreach (var e in m.Entries)
            {
                var at = grid.Iso.ToWorld(e.InsideTile);
                var nearest = zones.Values.SelectMany(z => z.Cells.Select(c => (z.GateId, D: Vector2.Distance(grid.Iso.CellCenter(c.X, c.Y), at)))).MinBy(x => x.D);
                Assert.Equal(e.Id, nearest.GateId);
            }
        }
    }

    [Fact]
    public void MirrorGate_Pairs_OthersUnchanged()
    {
        Assert.Equal("gate_ne", GateZones.MirrorGate("gate_nw"));
        Assert.Equal("gate_nw", GateZones.MirrorGate("gate_ne"));
        Assert.Equal("gate_se", GateZones.MirrorGate("gate_sw"));
        Assert.Equal("gate_sw", GateZones.MirrorGate("gate_se"));
        Assert.Equal("north", GateZones.MirrorGate("north"));
        Assert.Equal(GateSide.Ne, GateZones.MirrorSide(GateSide.Nw));
        Assert.Equal(GateSide.Se, GateZones.MirrorSide(GateSide.Sw));
        Assert.Equal("X.w.", MapMirror.TransposeProps("Xw.."));
    }

    // ── 짝 표 검사(13.5-12) ─────────────────────────────────────────────

    private static List<LayoutError> TilesetErrors(TilesetDef ts)
    {
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateTileset(ts, errors);
        return errors;
    }

    [Fact]
    public void PairTable_ValidSyntheticPasses_FourErrorKinds()
    {
        Assert.Empty(TilesetErrors(MirrorRoomTileset()));
        Assert.Empty(TilesetErrors(RoomActs.MirrorTileset()));
        var ts = MirrorRoomTileset();
        var mirror = ts.Mirror!;

        var notTransposed = TilesetErrors(ts with { Mirror = mirror with { Pairs = [["wall_v", "grate_h"]] } });   // 성질이 전치와 다름
        Assert.Contains(notTransposed, e => e.Path == "$.mirror.pairs[0]" && e.Message.Contains("전치"));
        var unknown = TilesetErrors(ts with { Mirror = mirror with { Self = ["no_such_piece"] } });                // 없는 이름
        Assert.Contains(unknown, e => e.Message.Contains("no_such_piece") && e.Message.Contains("pieces 에 없다"));
        var twice = TilesetErrors(ts with { Mirror = mirror with { Self = [.. mirror.Self, "wall_v"] } });        // 이름 두 번
        Assert.Contains(twice, e => e.Message.Contains("두 번"));
        var notSelf = TilesetErrors(ts with { Mirror = mirror with { Self = ["wall_v"], Pairs = [] } });          // 자기 전치가 아님
        Assert.Contains(notSelf, e => e.Path == "$.mirror.self[0]");

        // mirror: true 인데 짝 없는 조각 — 짝 표에서 창살을 빼면 거울판을 못 만든다
        var noGrate = ts with { Mirror = mirror with { Pairs = [.. mirror.Pairs.Where(p => p[0] != "grate_h")] } };
        var map = Example() with { Tileset = DefRef<TilesetDef>.To(noGrate) };
        var errors = new List<LayoutError>();
        Assert.Null(MapMirror.TryOf(map, noGrate, errors));
        Assert.Contains(errors, e => e.Message.Contains("조각 grate_h 의 짝이 타일셋"));
        Assert.Throws<InvalidDataException>(() => MapMirror.Of(map, noGrate));
    }

    [Fact]
    public void PositionPairs_BorderAndGateEnds_V081()
    {
        var ts = RoomActs.MirrorTileset();
        // 윗줄 edge_ne 의 성질을 edge_nw 의 전치와 다르게
        var badEdge = ts with { Props = new Dictionary<int, string>(ts.Props) { [TestDefs.EdgeNe] = "XXX." } };
        Assert.Contains(TilesetErrors(badEdge), e => e.Message.Contains("edge_nw ↔ edge_ne"));
        // 문 end_b 도 같음: ne 변의 끝 조각만 성질이 다른 번호
        var gate = ts.Gates[TestDefs.Border];
        var oddEnd = 70;
        var badEnd = ts with
        {
            Pieces = new Dictionary<string, int>(ts.Pieces) { ["odd_end"] = oddEnd },
            Props = new Dictionary<int, string>(ts.Props) { [oddEnd] = "XX.." },
            Gates = new Dictionary<string, GateSetDef> { [TestDefs.Border] = gate with { Ne = gate.Ne! with { EndB = oddEnd } } },
        };
        Assert.Contains(TilesetErrors(badEnd), e => e.Message.Contains("nw ↔ ne end_b"));
        // 짝 표가 없는 타일셋은 자리 짝을 보지 않는다
        Assert.DoesNotContain(TilesetErrors(badEdge with { Mirror = null }), e => e.Message.Contains("자리 짝"));
    }

    [Fact]
    public void PairCompare_FinalPropsOnly_NoInventedCorrectionRequired()
    {
        // 창살 기둥 칸 보정(sol_known_diffs)은 grate_h 에만 있고 짝 grate_v 에는 없어도 된다 — 최종 성질(props)만 견준다(v0.8.1)
        var ts = MirrorRoomTileset() with
        {
            SolKnownDiffs = [new SolDiffDef { Tile = TestDefs.RGrateH, Sub = 0, Sol = "w", Use = "X", Why = "창살 기둥 칸 보정(합성)" }],
        };
        Assert.Empty(TilesetErrors(ts));
    }

    [Fact]
    public void RealCathedral_MirrorTrue_ReportsMissingPairs_GrateAndClosedDoor()
    {
        // 실제 l1.json 에는 아직 짝 표가 없다 — 다른 조각 오류도 함께 나오고 순서는 보지 않는다(포함)
        var tilesets = RealData.Read("tilesets/l1.json");
        var maps = RealData.Read("maps/cathedral_arena.json");
        var map = (JsonObject)((JsonArray)maps["maps"]!)[0]!.DeepClone();
        map["mirror"] = true;
        var r = MapDataReader.Read(
        [
            new MapDataElement("tilesets/l1.json", "$.tilesets[0]", MapDataReader.TilesetsKind, ((JsonArray)tilesets["tilesets"]!)[0]!.DeepClone()),
            new MapDataElement("maps/cathedral_arena.json", "$.maps[0]", MapDataReader.MapsKind, map),
        ]);
        Assert.Contains(r.Errors, e => e.File == "maps/cathedral_arena.json" && e.Message.Contains("조각 grate_h 의 짝"));
        Assert.Contains(r.Errors, e => e.File == "maps/cathedral_arena.json" && e.Message.Contains("조각 door_closed 의 짝"));
        Assert.Null(r.Maps.Single().Mirrored);
        Assert.False(RealData.Cathedral.Mirror);                         // 실제 데이터는 바꾸지 않았다
    }

    [Fact]
    public void Reader_MirrorTrue_MakesMirroredMap_OriginalOnlyInList()
    {
        using var d = new TempDir();
        d.Write("tilesets/t.json", """
            { "tilesets": [ { "id": "t", "column_height": 160, "floor": [1],
                "borders": { "wall": { "corner_n": 11, "edge_ne": 12, "corner_e": 13, "edge_se": 14, "corner_s": 15, "edge_sw": 16, "corner_w": 17, "edge_nw": 18 } },
                "gates": { "wall": { "nw": { "open": 3 }, "ne": { "open": 3 }, "se": { "open": 3 }, "sw": { "open": 3 } } },
                "pieces": { "pillar": 30, "wall_v": 40, "wall_h": 41 },
                "props": { "1": "....", "3": "....", "11": "XXX.", "12": "XX..", "18": "X.X.", "13": "XXXX", "14": "XXXX", "15": "XXXX", "16": "XXXX", "17": "XXXX",
                           "30": "x...", "40": "X.X.", "41": "XX.." },
                "mirror": { "pairs": [["wall_v", "wall_h"]], "self": ["pillar"] } } ] }
            """);
        d.Write("maps/m.json", """
            { "maps": [ { "id": "m", "tileset": "t", "palette": "p", "size": 12, "art": "d1/maps/m", "mirror": true,
                "layout": { "border": "wall",
                            "gates": [ { "id": "gate_nw", "side": "nw" }, { "id": "gate_ne", "side": "ne" }, { "id": "gate_se", "side": "se" }, { "id": "gate_sw", "side": "sw" } ],
                            "place": [ { "piece": "wall_h", "line": [[4, 8], [6, 8]] }, { "piece": "pillar", "at": [[3, 3]] } ] },
                "entries": [ { "id": "gate_nw", "outside": [-4, 11.5], "inside": [4, 11.5] }, { "id": "gate_ne", "outside": [11.5, -4], "inside": [11.5, 4] },
                             { "id": "gate_se", "outside": [27, 11.5], "inside": [18, 11.5] }, { "id": "gate_sw", "outside": [11.5, 27], "inside": [11.5, 18] } ],
                "hero_start": { "tile": [11.5, 11.5], "facing": "south" },
                "anchors": { "center": [11.5, 11.5] } } ] }
            """);
        var r = MapDataReader.Read(d.Path);
        Assert.True(r.Errors.Count == 0, string.Join("\n", r.Errors));
        var map = Assert.Single(r.Maps);
        Assert.True(map.Mirror);
        Assert.Equal("m@m", map.Mirrored!.Id);
        Assert.Equal("m", map.Mirrored.MirrorOf);
        Assert.Equal("d1/maps/m@m", map.Mirrored.Art);
        Assert.Same(r.Tilesets.Single(), map.Mirrored.Tileset.Def);
        Assert.Equal(["wall_v"], map.Mirrored.Layout.Place.Where(p => p.Line is not null).Select(p => p.Piece));
        Assert.Equal([new Int2(8, 4), new Int2(8, 6)], map.Mirrored.Layout.Place[0].Line!);
    }
}
