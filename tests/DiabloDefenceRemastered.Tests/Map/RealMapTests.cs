using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Map;

/// <summary>
/// 실제 <c>Data/tilesets/l1.json</c>·<c>Data/maps/cathedral_arena.json</c>(WP0a 첫 판, 설계서 11.1 WP0a 완료 기준). 자원 없이 돈다.
/// 13.5-4 배치 쪽(<c>Expand</c>)·13.5-12(타일셋)·13.5-19(<c>ValidateRooms</c> — 하위 칸 ①②·방 영역 포함). WP2 의 80px·반경 14 격자 검사는 그 위에 더한다.
/// </summary>
public class RealMapTests
{
    [Fact]
    public void Reader_RealData_NoErrorsNoWarnings()
    {
        var r = RealData.Maps;
        Assert.True(r.Errors.Count == 0, string.Join("\n", r.Errors));
        Assert.Equal(["l1"], r.Tilesets.Select(t => t.Id));
        Assert.Contains("cathedral_arena", r.Maps.Select(m => m.Id));   // 작은 방 맵은 Nav/RealRoomMapTests
        Assert.Same(RealData.L1, RealData.Cathedral.Tileset.Def);
    }

    [Fact]
    public void Expand_PassesLayoutAndRoomChecks()
    {
        var map = RealData.Cathedral;
        var errors = new List<LayoutError>();
        var grid = ArenaLayout.TryExpand(map, RealData.L1, errors);
        Assert.NotNull(grid);
        Assert.Empty(errors);
        ArenaLayout.ValidateRooms(map, grid, errors);
        Assert.True(errors.Count == 0, string.Join("\n", errors));

        Assert.Equal(40, grid.Size);
        Assert.Equal(new Vector2(2560, 1408), grid.Iso.BoxSize);
        // 같은 입력 → 같은 해시(가져오기 도구와 게임이 같은 배치)
        Assert.Equal(grid.LayoutHash, ArenaLayout.Expand(map).LayoutHash);
    }

    [Fact]
    public void Tileset_PropsKeysAreNamedTiles_AndOriginValues()
    {
        var ts = RealData.L1;
        var errors = new List<LayoutError>();
        ArenaLayout.ValidateTileset(ts, errors);
        Assert.Empty(errors);
        Assert.Equal(ArenaLayout.NamedTiles(ts), ts.Props.Keys.Order());   // 13.5-12 — 덤프가 아님

        // 원본 SOL 을 읽어 적은 값(설계서 6.11 표 — 벽 메가는 반만 막고, 앞 턱은 넷 다)
        Assert.Equal("X.X.", ts.Props[ts.Pieces["wall_v"]]);
        Assert.Equal("XX..", ts.Props[ts.Pieces["wall_h"]]);
        Assert.Equal("XXX.", ts.Props[ts.Pieces["wall_corner"]]);
        Assert.Equal("x...", ts.Props[ts.Pieces["pillar"]]);      // 기둥: 시야 통과(기획 열린 질문 22 기본)
        Assert.Equal("Xw..", ts.Props[ts.Pieces["grate_h"]]);     // 창살: 기둥 칸 보정 + 걷기만
        Assert.Equal("XXX.", ts.Props[ts.Pieces["blood_wall_corner"]]);
        Assert.Equal("XX..", ts.Props[ts.Pieces["door_closed"]]);   // 닫힌 문 26 = 가로 벽(SOL 로 벽 — 문으로 안 씀)
        Assert.Equal("X...", ts.Props[ts.Pieces["wall_post"]]);     // 작은 모서리 기둥 3(┘ 자리)
        Assert.Equal("X.w.", ts.Props[ts.Pieces["grate_v"]]);     // 세로 창살 35 = 36 의 전치(기둥 칸은 아래 design_diffs)
        Assert.Equal("X.X.", ts.Props[ts.Pieces["door_closed_v"]]); // 닫힌 문 세로 25 = 26 의 전치
        foreach (var t in ts.Floor) Assert.Equal("....", ts.Props[t]);
        var b = ts.Borders["arch_hall"];
        foreach (var t in new[] { b.CornerE, b.EdgeSe, b.CornerS, b.EdgeSw, b.CornerW }) Assert.Equal("XXXX", ts.Props[t]);

        // SOL 오류 보정은 창살 기둥 칸 하나(쓰는 글자가 성질 표와 같음 — ValidateTileset)
        var diff = Assert.Single(ts.SolKnownDiffs);
        Assert.Equal((36, 0, "w", "X"), (diff.Tile, diff.Sub, diff.Sol, diff.Use));
        // 기획 이유로 다르게 둔 칸: 세로 창살 기둥 칸(36 의 기둥 칸과 같게 — 거울판 짝, WP0), 석관 오른쪽 위 칸(2026-11-13 — 거울판 자기 짝 www.)
        Assert.Equal([(35, 0, "w", "X"), (131, 1, ".", "w")], ts.DesignDiffs.Select(d => (d.Tile, d.Sub, d.Sol, d.Use)));
        Assert.Equal([15], ts.Marks[TileMark.Pillar]);
        Assert.Equal([36, 35], ts.Marks[TileMark.Grate]);
        Assert.Equal([26, 25], ts.Marks[TileMark.ClosedDoor]);   // 98 은 피 튄 아치 벽(문 아님, 설계서 13.16)
    }

    [Fact]
    public void Layout_GatesEntriesAndCenter()
    {
        var map = RealData.Cathedral;
        var grid = ArenaLayout.Expand(map);
        // 네 문 틈: 변 가운데 메가 9·10 → 하위 18..21 이 비어 있고 양옆은 막힘
        foreach (var k in new[] { 18, 19, 20, 21 })
        {
            Assert.True(grid.IsWalkable(0, k)); Assert.True(grid.IsWalkable(k, 0));
            Assert.True(grid.IsWalkable(39, k)); Assert.True(grid.IsWalkable(k, 39));
        }
        Assert.False(grid.IsWalkable(0, 17)); Assert.False(grid.IsWalkable(0, 22));
        Assert.False(grid.IsWalkable(17, 0)); Assert.False(grid.IsWalkable(22, 0));
        Assert.False(grid.IsWalkable(39, 17)); Assert.False(grid.IsWalkable(39, 22));
        Assert.False(grid.IsWalkable(17, 39)); Assert.False(grid.IsWalkable(22, 39));

        // 입구·시작·기준점은 걸을 수 있는 칸, 바깥 시작점은 격자 밖
        foreach (var e in map.Entries)
        {
            Assert.True(grid.IsWalkable(e.InsideTile.Cell.X, e.InsideTile.Cell.Y), e.Id);
            Assert.False(grid.IsInside(e.OutsideTile.Cell.X, e.OutsideTile.Cell.Y), e.Id);
        }
        Assert.Equal(["gate_ne", "gate_nw", "gate_se", "gate_sw"], map.Entries.Select(e => e.Id).Order(StringComparer.Ordinal));
        Assert.True(grid.IsWalkable(map.HeroStart.Tile.Cell.X, map.HeroStart.Tile.Cell.Y));
        foreach (var (id, at) in map.AnchorsTile) Assert.True(grid.IsWalkable(at.Cell.X, at.Cell.Y), id);

        // open_center [6..12] 안은 바닥과 기둥 둘
        var oc = map.OpenCenter!.Value;
        Assert.Equal(new RectI(6, 6, 13, 13), oc);
        var pillars = 0;
        for (var my = oc.Y0; my < oc.Y1; my++)
            for (var mx = oc.X0; mx < oc.X1; mx++)
                if (grid.MegaTile(mx, my) == RealData.L1.Pieces["pillar"]) pillars++;
                else Assert.Contains(grid.MegaTile(mx, my), ArenaLayout.FloorTiles(RealData.L1));   // 바닥 변형 장식판 포함
        Assert.Equal(2, pillars);
    }

    [Fact]
    public void RoomFloors_TwoOneDoorRooms_TwoTwoDoorAreas()
    {
        // 기획 10.3: 피 방·창살 방 = 문 하나(방 체류 압박·방 보급 대상), ㄱ 안·Γ 안 = 출입구 둘
        var map = RealData.Cathedral;
        var floors = RoomFloors.Compute(map, ArenaLayout.Expand(map));
        Assert.Equal(["room_blood", "room_grate", "room_south", "room_west"], floors.Rooms.Select(r => r.Id));
        Assert.Equal([1, 1, 2, 2], floors.Rooms.Select(r => r.Doors));
        Assert.True(floors.TryGet("room_blood", out var blood));
        Assert.Equal(81, blood.Cells.Count);   // 하위 (1..9, 1..9)
    }

    [Fact]
    public void Placement_IsDesignExampleShape_ArtOnlyDifferences()
    {
        // 6.11 예(합성 이름 ④)와 하위 칸 성질이 칸마다 같다 — 실제 맵에서 바꾼 것은 그림 번호(같은 성질 조각)뿐(13장 처리 표).
        // 단 아래 목록의 칸은 일부러 다르다(설계서 13.16 — 예를 고칠 때 합성 ④ 도 같이 고치면 목록에서 뺀다)
        var known = new Dictionary<(int, int), (char Real, char Example)>
        {
            // 피 방 [5,5] ┘: 예는 가로 벽(위 줄 둘 다 막힘)이라 벽이 오른쪽 벽 선(tx 10) 너머로 한 메가 더 나간다 → 실제는 작은 모서리 기둥(위-왼 칸만)
            [(11, 10)] = ('.', 'X'),
            // 짧은 벽 열린 끝(2026-11-13): 끝 칸을 작은 모서리 기둥으로 바꿔 마감 — L1 에 아래·오른쪽 끝 조각이 없어 벽 단면이 잘려 보였다(사용자 지적)
            [(12, 33)] = ('.', 'X'),   // 서쪽 벽 [6,16] 아래 끝
            [(33, 26)] = ('.', 'X'),   // 남쪽 벽 [16,13] 오른쪽 끝
            [(26, 33)] = ('.', 'X'),   // 남쪽 벽 [13,16] 아래 끝
        };
        var real = ArenaLayout.Expand(RealData.Cathedral);
        var example = TestDefs.RoomMapExample();
        var synth = ArenaLayout.Expand(example, example.Tileset.Def);
        for (var ty = 0; ty < 40; ty++)
            for (var tx = 0; tx < 40; tx++)
            {
                if (known.TryGetValue((tx, ty), out var k))
                {
                    Assert.Equal(k, (real.Prop(tx, ty), synth.Prop(tx, ty)));   // 목록이 낡지 않게 — 정말 다른 칸이어야 한다
                    continue;
                }
                Assert.True(real.Flags(tx, ty) == synth.Flags(tx, ty), $"({tx},{ty}) 실제 {real.Prop(tx, ty)} / 예 {synth.Prop(tx, ty)}");
            }
        Assert.Equal(example.OpenCenter, RealData.Cathedral.OpenCenter);
        Assert.Equal(example.AnchorsTile.OrderBy(a => a.Key), RealData.Cathedral.AnchorsTile.OrderBy(a => a.Key));
        Assert.Equal(example.RoomAreas.OrderBy(a => a.Key), RealData.Cathedral.RoomAreas.OrderBy(a => a.Key));
    }
}
