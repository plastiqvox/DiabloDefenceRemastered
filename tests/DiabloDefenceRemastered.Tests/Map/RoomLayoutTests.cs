using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Tests.Map;

/// <summary>
/// 안쪽 방 배치(설계서 6.11 v0.6·v0.6.1, 13.5-19, WP1d-구현): <c>grid</c>·<c>over_border</c>(막힘 줄어듦)·<c>marks</c>·<c>ValidateRooms</c>(open_center·문 표시·방 기준점·하위 칸 ①②).
/// 합성 방 맵은 <see cref="TestDefs"/> 의 ①~⑥.
/// </summary>
public class RoomLayoutTests
{
    private static ArenaGrid ExpandOk(MapDef map)
    {
        var errors = new List<LayoutError>();
        var grid = ArenaLayout.TryExpand(map, map.Tileset.Def, errors);
        Assert.Empty(errors);
        return grid!;
    }

    private static List<LayoutError> ExpandErrors(MapDef map)
    {
        var errors = new List<LayoutError>();
        Assert.Null(ArenaLayout.TryExpand(map, map.Tileset.Def, errors));
        Assert.NotEmpty(errors);
        return errors;
    }

    private static List<LayoutError> Rooms(MapDef map)
    {
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateRooms(map, ExpandOk(map), errors);
        return errors;
    }

    private static MapDef Room(params PiecePlacementDef[] place) => TestDefs.RoomMap(place);

    // ── grid 놓기 ─────────────────────────────────────────────────

    [Fact]
    public void Grid_PlacesRowsFromOrigin_DotSkips_RowsMayDifferInLength()
    {
        var g = ExpandOk(Room(new PiecePlacementDef
        {
            Origin = new(3, 4),
            Grid = ["wall_corner   wall_h  wall_h", "wall_v", "  .   pillar   ", "wall_v . . wall_v"],
        }));
        Assert.Equal(TestDefs.RWallCorner, g.MegaTile(3, 4));
        Assert.Equal(TestDefs.RWallH, g.MegaTile(5, 4));      // 칸 사이 공백 여러 개
        Assert.Equal(TestDefs.RWallV, g.MegaTile(3, 5));
        Assert.Equal(TestDefs.RFloor, g.MegaTile(4, 5));      // 짧은 줄의 나머지는 건드리지 않음
        Assert.Equal(TestDefs.RFloor, g.MegaTile(3, 6));      // '.'
        Assert.Equal(TestDefs.RPillar, g.MegaTile(4, 6));     // 앞뒤 공백
        Assert.Equal(TestDefs.RWallV, g.MegaTile(6, 7));
        Assert.Equal(TestDefs.RFloor, g.MegaTile(4, 7));
    }

    [Fact]
    public void Grid_Errors_UnknownName_PieceTogether_NoOrigin_TwoKinds_Overlap()
    {
        var errors = ExpandErrors(Room(
            new PiecePlacementDef { Origin = new(3, 3), Grid = ["wall_h statue"] },
            new PiecePlacementDef { Piece = "wall_h", Origin = new(3, 6), Grid = ["wall_h"] },
            new PiecePlacementDef { Grid = ["wall_h"] },
            new PiecePlacementDef { Piece = "pillar", At = [new(8, 8)], Line = [new(8, 8), new(8, 9)] },
            new PiecePlacementDef { Piece = "pillar", At = [new(10, 10)], Origin = new(1, 1) },
            new PiecePlacementDef { Origin = new(10, 9), Grid = ["wall_h", "wall_h"] },
            new PiecePlacementDef { Piece = "pillar", At = [new(10, 10)] },
            new PiecePlacementDef { Origin = new(2, 2), Grid = [] },
            new PiecePlacementDef { Origin = new(2, 2), Grid = [" . .", "."] }));
        Assert.Contains(errors, e => e.Path == "$.layout.place[0].grid[0]" && e.Message.Contains("statue"));
        Assert.Contains(errors, e => e.Path == "$.layout.place[7].grid" && e.Message.Contains("하나도 없다"));   // 빈 grid
        Assert.Contains(errors, e => e.Path == "$.layout.place[8].grid" && e.Message.Contains("하나도 없다"));   // '.' 만
        Assert.Contains(errors, e => e.Path == "$.layout.place[1].piece");
        Assert.Contains(errors, e => e.Path == "$.layout.place[2].origin");
        Assert.Contains(errors, e => e.Path == "$.layout.place[3]" && e.Message.Contains("하나만"));
        Assert.Contains(errors, e => e.Path == "$.layout.place[4].origin");
        Assert.Contains(errors, e => e.Path == "$.layout.place[6].at[0]" && e.Message.Contains("이미"));   // grid 가 놓은 (10,10)
    }

    // ── over_border ──────────────────────────────────────────────

    [Fact]
    public void OverBorder_CoversBorder_KeepsBoundaryMarks()
    {
        var g = ExpandOk(TestDefs.RoomMapOverBorder());
        Assert.Equal(TestDefs.RWallV, g.MegaTile(0, 3));
        // 덮인 칸의 하위 칸도 둘레 표시가 남는다(4.2)
        Assert.True(g.IsBoundary(0, 6));
        Assert.True(g.IsBoundary(1, 6));
        Assert.False(g.IsBoundary(2, 6));
    }

    [Fact]
    public void Border_WithoutOverBorder_IsStillError_GateCellAlwaysError()
    {
        var plain = ExpandErrors(Room(new PiecePlacementDef { Piece = "wall_v", At = [new(0, 3)] }));
        Assert.Contains(plain, e => e.Path == "$.layout.place[0].at[0]" && e.Message.Contains("테두리"));

        // M 20 → 북서 문 열린 칸 (0,9)·(0,10)
        var gate = ExpandErrors(Room(new PiecePlacementDef { Piece = "wall_v", At = [new(0, 9)], OverBorder = true }));
        Assert.Contains(gate, e => e.Path == "$.layout.place[0].at[0]" && e.Message.Contains("문 칸"));
    }

    [Fact]
    public void OverBorder_ThatUnblocksBorder_IsError_CornerPasses()
    {
        // ⑥: 둘레 왼쪽 열(X.X.)을 가로 조각(XX..)으로 덮으면 아래-왼 하위 칸이 뚫린다
        var errors = ExpandErrors(TestDefs.RoomMapBorderNotch());
        var e = Assert.Single(errors);
        Assert.Equal("$.layout.place[0].line", e.Path);
        Assert.Contains("테두리 (0,13) 하위 칸 아래-왼", e.Message);
        Assert.Contains("걷기·시야·투사체", e.Message);
        Assert.Contains("wall_h", e.Message);

        // 모서리(XXX.)는 둘레 왼쪽 열을 다 덮는다 → 통과
        ExpandOk(Room(new PiecePlacementDef { Piece = "wall_corner", At = [new(0, 13)], OverBorder = true }));
        // 앞둘레(XXXX)를 반만 막는 조각으로 덮어도 오류
        Assert.Contains(ExpandErrors(Room(new PiecePlacementDef { Piece = "wall_v", At = [new(19, 4)], OverBorder = true })),
            x => x.Message.Contains("위-오른"));
    }

    // ── marks ───────────────────────────────────────────────────

    [Fact]
    public void Marks_MustBeNamedTiles()
    {
        var ts = TestDefs.RoomTileset() with
        {
            Marks = new Dictionary<TileMark, IReadOnlyList<int>> { [TileMark.ClosedDoor] = [TestDefs.RDoorClosed, 999], [TileMark.Arch] = [] },
        };
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateTileset(ts, errors);
        Assert.Contains(errors, e => e.Path == "$.marks.closed_door[1]" && e.Message.Contains("999"));
        Assert.Contains(errors, e => e.Path == "$.marks.arch");
        Assert.DoesNotContain(errors, e => e.Path == "$.marks.closed_door[0]");

        var ok = new List<LayoutError>();
        ArenaLayout.ValidateTileset(TestDefs.RoomTileset(), ok);
        Assert.Empty(ok);
    }

    [Fact]
    public void Marks_DoNotChangeLayoutHash()
    {
        var map = TestDefs.RoomMapExample();
        var a = ExpandOk(map).LayoutHash;
        var noMarks = map.Tileset.Def with { Marks = new Dictionary<TileMark, IReadOnlyList<int>>() };
        var b = ArenaLayout.Expand(map, noMarks).LayoutHash;
        Assert.Equal(a, b);
    }

    // ── 13.5-19: open_center·문 표시·방 기준점 ──────────────────────

    [Fact]
    public void OpenCenter_TwoPillarsPass_ThreeFail_WallFails_OutsideFails()
    {
        var oc = new RectI(6, 6, 13, 13);
        PiecePlacementDef Pillars(params Int2[] at) => new() { Piece = "pillar", At = at };

        Assert.Empty(TestDefs.RoomMap([Pillars(new(7, 9), new(12, 9))], openCenter: oc).Let(Rooms));

        var three = TestDefs.RoomMap([Pillars(new(7, 9), new(12, 9), new(9, 12))], openCenter: oc).Let(Rooms);
        Assert.Contains(three, e => e.Path == "$.open_center" && e.Message.Contains("3개"));

        var wall = TestDefs.RoomMap([new() { Piece = "wall_h", At = [new(8, 8)] }], openCenter: oc).Let(Rooms);
        Assert.Contains(wall, e => e.Path == "$.open_center" && e.Message.Contains("(8,8)"));

        var outside = TestDefs.RoomMap([], openCenter: new RectI(0, 6, 13, 13)).Let(Rooms);
        Assert.Contains(outside, e => e.Path == "$.open_center" && e.Message.Contains("테두리 안"));
    }

    [Fact]
    public void ClosedDoorOrArch_WithoutDecorWhy_IsWarning()
    {
        var door = new PiecePlacementDef { Piece = "door_closed", At = [new(8, 4)] };
        var warn = Assert.Single(Room(door).Let(Rooms));
        Assert.True(warn.Warning);
        Assert.Equal("$.layout.place[0]", warn.Path);
        Assert.Contains("door_closed", warn.Message);

        Assert.Empty(Room(door with { DecorWhy = "벽 장식" }).Let(Rooms));
    }

    [Fact]
    public void RoomAnchor_OnBlockedCell_IsError()
    {
        var map = TestDefs.RoomMap([new() { Piece = "wall_h", At = [new(4, 4)] }],
            anchors: new Dictionary<string, TileCoord> { ["room_x"] = new(8, 8), ["room_ok"] = new(11, 11), ["not_room"] = new(8, 8) },
            roomAreas: new Dictionary<string, RectI> { ["room_x"] = new(4, 4, 5, 5), ["room_ok"] = new(5, 5, 6, 6) });
        var errors = Rooms(map);
        var e = Assert.Single(errors);
        Assert.Equal("$.anchors.room_x", e.Path);
        Assert.Contains("걷기 막힘", e.Message);
        Assert.Contains(TestDefs.RoomMap([], anchors: new Dictionary<string, TileCoord> { ["room_far"] = new(50, 3) }).Let(Rooms),
            x => x.Path == "$.anchors.room_far" && x.Message.Contains("격자 밖"));
    }

    // ── 13.5-19 하위 칸 검사(v0.6.1) ──────────────────────────────

    [Fact]
    public void Strip_NextToBorder_WithClosedEnds_IsTrappedFloor()
    {
        // ①: 둘레 왼쪽 열 옆 메가에 세로 벽 줄 → 하위 x = 1 띠, 두 끝은 둘레에 겹친 모서리로 막힘
        var errors = Rooms(TestDefs.RoomMapStrip());
        Assert.Contains(errors, e => e.Message.StartsWith("갇힌 바닥 — 메가 (0,3)", StringComparison.Ordinal) && e.Message.Contains("(1,6)"));
        Assert.Contains(errors, e => e.Message.StartsWith("하위 1칸 틈", StringComparison.Ordinal));
        // 메가로 묶어 한 줄씩: 띠 (1,5..13) = 메가 (0,2)..(0,6) → 갇힌 바닥 다섯 줄
        Assert.Equal(5, errors.Count(e => e.Message.StartsWith("갇힌 바닥", StringComparison.Ordinal)));
        Assert.All(errors, e => Assert.False(e.Warning));
    }

    [Fact]
    public void SameWall_OverBorder_Passes()
    {
        // ②: 같은 벽을 둘레 자리에 겹치면 띠가 없다
        Assert.Empty(Rooms(TestDefs.RoomMapOverBorder()));
    }

    [Fact]
    public void OneDoorRoom_Passes_AndCornersReplacedByStraightPieces_IsOneCellGap()
    {
        // ③: 문 하나 방 + room_a
        var map = TestDefs.RoomMapOneDoor();
        Assert.Empty(Rooms(map));
        Assert.True(map.AnchorsTile.ContainsKey("room_a"));

        // ⑤: 꺾이는 자리에 가로·세로 조각만 → 하위 1칸 구멍
        var errors = Rooms(TestDefs.RoomMapNoCorners());
        Assert.Contains(errors, e => e.Message.StartsWith("하위 1칸 틈 — 메가 (3,3)", StringComparison.Ordinal) && e.Message.Contains("(6,7)"));
        Assert.DoesNotContain(errors, e => e.Message.StartsWith("갇힌 바닥", StringComparison.Ordinal));
    }

    [Fact]
    public void ArchAsDoor_IsOneCellGap_AndWarning()
    {
        // 가로 벽 줄의 한 칸을 아치(위 줄 틈이 하위 1칸)로 → 1칸 틈 오류 + closed_door·arch 경고
        var errors = Room(
            new PiecePlacementDef { Piece = "wall_h", Line = [new(3, 15), new(8, 15)] },
            new PiecePlacementDef { Piece = "wall_h", Line = [new(10, 15), new(12, 15)] },
            new PiecePlacementDef { Piece = "arch", At = [new(9, 15)] }).Let(Rooms);
        Assert.Contains(errors, e => !e.Warning && e.Message.StartsWith("하위 1칸 틈 — 메가 (9,15)", StringComparison.Ordinal) && e.Message.Contains("(19,30)"));
        Assert.Contains(errors, e => e.Warning && e.Path == "$.layout.place[2]");
    }

    [Fact]
    public void DesignExample_Section611_PassesAllRoomChecks()
    {
        // ④: 6.11 예 배치 좌표 그대로 — 배치 풀기(over_border 막힘 포함)·13.5-19 전체가 오류·경고 0
        var map = TestDefs.RoomMapExample();
        var grid = ExpandOk(map);
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateRooms(map, grid, errors);
        Assert.Empty(errors);

        // 피 방 바깥 두 벽은 둘레 자리에 겹쳤다 — 둘레 표시는 남는다
        Assert.Equal(TestDefs.RBloodWallCorner, grid.MegaTile(5, 0));
        Assert.True(grid.IsBoundary(10, 0));
        // 창살 칸: 기둥 칸 X, 나머지 한 칸은 걷기만(w)
        Assert.Equal('X', grid.Prop(30, 12));
        Assert.Equal('w', grid.Prop(31, 12));
        // 방 기준점 넷 + center
        Assert.Equal(4, map.AnchorsTile.Keys.Count(k => k.StartsWith("room_", StringComparison.Ordinal)));
    }

    // ── 방 영역·방 바닥(v0.6.3) ─────────────────────────────────────

    [Fact]
    public void RoomFloors_DesignExample_DoorsAndCells()
    {
        // 6.11 예의 room_areas: 문 하나 = 피 방·창살 방, 출입구 둘 = ㄱ 안·Γ 안
        var map = TestDefs.RoomMapExample();
        var grid = ExpandOk(map);
        var floors = RoomFloors.Compute(map, grid);
        Assert.Equal(["room_blood", "room_grate", "room_south", "room_west"], floors.Rooms.Select(r => r.Id));   // 기준점 ID 서수 순
        Assert.Equal([1, 1, 2, 2], floors.Rooms.Select(r => r.Doors));
        Assert.Equal([true, true, false, false], floors.Rooms.Select(r => r.OneDoor));

        // 피 방: 하위 (1..9, 1..9) 81칸 — 오른쪽 벽(메가 5 왼쪽 열) 바깥 반 칸·문 틈 (10, 6..9)은 바닥이 아니다
        Assert.True(floors.TryGet("room_blood", out var blood));
        Assert.Equal(81, blood.Cells.Count);
        Assert.Equal(new Int2(1, 1), blood.Cells[0]);           // 행 우선
        Assert.Equal(new Int2(9, 9), blood.Cells[^1]);
        Assert.Equal(0, floors.IndexAt(5, 5));
        Assert.Equal(-1, floors.IndexAt(10, 7));                 // 문 틈
        Assert.Equal(-1, floors.IndexAt(11, 3));                 // 오른쪽 벽 바깥 반 칸
        Assert.Equal(-1, floors.IndexAt(20, 20));                // 가운데
        Assert.Equal(-1, floors.IndexAt(-1, 3));
        // 월드 px 로도 같은 방
        Assert.Equal(1, floors.IndexAt(grid.Iso.CellCenter(32, 7)));
        Assert.Equal(-1, floors.IndexAt(grid.Iso.CellCenter(20, 20)));

        // 같은 입력이면 같은 결과
        var again = RoomFloors.Compute(map, grid);
        Assert.Equal(floors.Rooms.Select(r => (r.Id, r.Doors, r.Cells.Count)), again.Rooms.Select(r => (r.Id, r.Doors, r.Cells.Count)));
    }

    [Fact]
    public void RoomFloors_EmptyWithoutAreas()
    {
        var map = TestDefs.RoomMapOverBorder();
        Assert.Same(RoomFloors.Empty, RoomFloors.Compute(map, ExpandOk(map)));
        Assert.Equal(-1, RoomFloors.Empty.IndexAt(Vector2.Zero));
    }

    [Fact]
    public void RoomAreas_Errors()
    {
        var map = TestDefs.RoomMap([],
            anchors: new Dictionary<string, TileCoord>
            {
                ["room_a"] = new(11, 11), ["room_b"] = new(13, 13), ["room_c"] = new(3, 3), ["room_d"] = new(30, 30), ["room_e"] = new(36, 3),
            },
            roomAreas: new Dictionary<string, RectI>
            {
                ["room_a"] = new(5, 5, 7, 7), ["room_b"] = new(6, 6, 8, 8),   // 겹침
                ["room_c"] = new(10, 10, 11, 11),                               // 기준점이 영역 밖
                ["room_e"] = new(18, 1, 21, 3),                                 // 맵 밖(M 20)
                ["hall"] = new(1, 1, 2, 2),                                     // room_* 기준점이 아님
            });
        var errors = Rooms(map);
        Assert.Contains(errors, e => e.Path == "$.room_areas.room_a" && e.Message.Contains("room_b") && e.Message.Contains("겹친다"));
        Assert.Contains(errors, e => e.Path == "$.room_areas.room_c" && e.Message.Contains("영역") && e.Message.Contains("밖이다"));
        Assert.Contains(errors, e => e.Path == "$.room_areas" && e.Message.Contains("room_d"));
        Assert.Contains(errors, e => e.Path == "$.room_areas.room_e" && e.Message.Contains("맵 안"));
        Assert.Contains(errors, e => e.Path == "$.room_areas.hall");
        Assert.All(errors, e => Assert.False(e.Warning));
    }

    [Fact]
    public void ClosedRoom_HasNoDoor_IsError()
    {
        // ③ 의 문 칸 둘을 벽으로 막은 방: 문 0 오류(+ 갇힌 바닥)
        var map = TestDefs.RoomMap(
        [
            new()
            {
                Id = "room_a", Origin = new(3, 3),
                Grid =
                [
                    "wall_corner wall_h wall_h wall_h wall_h wall_corner",
                    "wall_v .      .      .      .      wall_v",
                    "wall_v .      .      .      .      wall_v",
                    "wall_v .      .      .      .      wall_v",
                    "wall_v .      .      .      .      wall_v",
                    "wall_corner wall_h wall_h wall_h wall_h wall_corner",
                ],
            },
        ], anchors: new Dictionary<string, TileCoord> { ["room_a"] = new(11, 11) },
        roomAreas: new Dictionary<string, RectI> { ["room_a"] = new(3, 3, 8, 8) });
        var errors = Rooms(map);
        Assert.Contains(errors, e => e.Path == "$.room_areas.room_a" && e.Message.Contains("문이 없는 방"));
        Assert.Contains(errors, e => e.Message.StartsWith("갇힌 바닥", StringComparison.Ordinal));
        Assert.Equal(0, RoomFloors.Compute(map, ExpandOk(map)).Rooms[0].Doors);

        // ③ 그대로면 문 하나
        var one = TestDefs.RoomMapOneDoor();
        var floor = RoomFloors.Compute(one, ExpandOk(one)).Rooms[0];
        Assert.Equal((1, 81), (floor.Doors, floor.Cells.Count));   // 바닥 하위 (7..15, 7..15)
    }

    // ── MapDataReader 연결 ─────────────────────────────────────────

    [Fact]
    public void Reader_RunsValidateRooms_WarningsBecomeWarningSeverity()
    {
        const string tilesets = """
            { "tilesets": [
              { "id": "t", "column_height": 160, "floor": [13],
                "borders": { "hall": { "corner_n": 4, "edge_ne": 2, "corner_e": 23, "edge_se": 18, "corner_s": 20, "edge_sw": 19, "corner_w": 24, "edge_nw": 1 } },
                "gates": { "hall": { "nw": { "open": 13 } } },
                "pieces": { "wall_h": 2, "door_closed": 98 },
                "marks": { "closed_door": [98] },
                "props": { "13": "....", "1": "X.X.", "2": "XX..", "4": "XXX.", "98": "XX..",
                           "18": "XXXX", "19": "XXXX", "20": "XXXX", "23": "XXXX", "24": "XXXX" } } ] }
            """;
        const string maps = """
            { "maps": [
              { "id": "m", "tileset": "t", "palette": "p", "size": 10,
                "layout": { "border": "hall", "gates": [ { "id": "gate_nw", "side": "nw" } ],
                            "place": [ { "piece": "door_closed", "at": [[4, 4]] } ] },
                "hero_start": { "tile": [9.5, 9.5] },
                "anchors": { "room_bad": [8, 8] },
                "room_areas": { "room_bad": [4, 4, 5, 5] },
                "art": "d1/maps/m" } ] }
            """;
        var r = MapDataReader.Read(
        [
            new MapDataElement("tilesets/t.json", "$.tilesets[0]", MapDataReader.TilesetsKind, DefJson.ParseNode(tilesets)!["tilesets"]![0]!.DeepClone()),
            new MapDataElement("maps/m.json", "$.maps[0]", MapDataReader.MapsKind, DefJson.ParseNode(maps)!["maps"]![0]!.DeepClone()),
        ]);
        var warn = Assert.Single(r.Errors, e => e.Severity == DataSeverity.Warning);
        Assert.Equal("maps/m.json", warn.File);
        Assert.Equal("$.maps[0].layout.place[0]", warn.JsonPath);
        var err = Assert.Single(r.Errors, e => e.Severity == DataSeverity.Error);
        Assert.Equal("$.maps[0].anchors.room_bad", err.JsonPath);   // (8,8) = 메가 (4,4) 위-왼 = 닫힌 문 X
        Assert.True(r.HasErrors);
    }
}

internal static class RoomLayoutTestExtensions
{
    /// <summary>식 하나로 맵을 만들고 검사에 넘기는 도우미.</summary>
    public static TResult Let<T, TResult>(this T value, Func<T, TResult> f) => f(value);
}
