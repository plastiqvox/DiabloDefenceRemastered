using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Json;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>
/// 대성당 작은 방 A "기둥 홀"(기획 10.8 첫 안, 설계서 11.1 WP5 맵 데이터). 자원 없이 돈다.
/// 맵은 <c>Data/maps/cathedral_room_a.json</c>(처음엔 시험 고정 파일이었다 — <c>Map/RealMapTests</c> 의 맵 목록 줄이 풀려 옮김).
/// 실제 <c>l1</c> 타일셋과 함께 <see cref="MapDataReader"/> 로 읽어 13.5-4 배치 쪽·13.5-19·13.5-23 ③(작은 방 진입점 150px) 를 본다.
/// 길 검사 13.5-4(80px)·5·6 과 10.1 <c>Nav</c> 전수는 모든 작은 방을 함께 보는 <c>Nav/RealRoomMapTests</c>.
/// </summary>
public class RoomMapTests
{
    private const string File = "maps/cathedral_room_a.json";

    private static string FixturePath() =>
        Path.Combine(RealData.Dir, "maps", "cathedral_room_a.json");

    private static MapDataResult Read()
    {
        var tilesets = RealData.Read("tilesets/l1.json");
        var text = System.IO.File.ReadAllText(FixturePath());
        Assert.Null(DefJson.FindDuplicateKey(text));
        var maps = (JsonObject)DefJson.ParseNode(text)!;
        return MapDataReader.Read(
        [
            new MapDataElement("tilesets/l1.json", "$.tilesets[0]", MapDataReader.TilesetsKind, ((JsonArray)tilesets["tilesets"]!)[0]!.DeepClone()),
            new MapDataElement(File, "$.maps[0]", MapDataReader.MapsKind, ((JsonArray)maps["maps"]!)[0]!.DeepClone()),
        ]);
    }

    [Fact]
    public void Reader_NoErrorsNoWarnings_SmallRoomOfActOne()
    {
        var r = Read();
        Assert.True(r.Errors.Count == 0, string.Join("\n", r.Errors));
        var map = Assert.Single(r.Maps);
        Assert.Equal("cathedral_room_a", map.Id);
        Assert.Equal(16, map.Size);
        Assert.Equal([RoomKind.Normal, RoomKind.Elite], map.RoomKinds);
        Assert.True(map.Mirror);                                             // L1 짝 표(WP0)로 거울판을 켠다
        Assert.Equal("d1/maps/cathedral_room_a", map.Art);
    }

    [Fact]
    public void Expand_PassesLayoutAndRoomChecks_BoxAndGates()
    {
        var map = Read().Maps.Single();
        var errors = new List<LayoutError>();
        var grid = ArenaLayout.TryExpand(map, map.Tileset.Def, errors);
        Assert.NotNull(grid);
        Assert.Empty(errors);
        ArenaLayout.ValidateRooms(map, grid, errors);
        Assert.True(errors.Count == 0, string.Join("\n", errors));

        var n = grid.Size;
        Assert.Equal(32, n);
        Assert.True(n <= RoomMapRules.SmallRoomMaxN);                       // 작은 방 검사 대상(13.5-23)
        Assert.Equal(new Vector2(2048, 1152), grid.Iso.BoxSize);

        // 네 문 틈: 변 가운데 메가 7·8 → 하위 14..17 이 비어 있고 양옆은 막힘(큰 방과 같은 arch_hall 짜임)
        foreach (var k in new[] { 14, 15, 16, 17 })
        {
            Assert.True(grid.IsWalkable(0, k)); Assert.True(grid.IsWalkable(k, 0));
            Assert.True(grid.IsWalkable(n - 1, k)); Assert.True(grid.IsWalkable(k, n - 1));
        }
        Assert.False(grid.IsWalkable(0, 13)); Assert.False(grid.IsWalkable(0, 18));
        Assert.False(grid.IsWalkable(13, 0)); Assert.False(grid.IsWalkable(18, 0));
    }

    [Fact]
    public void Entries_BackFour_FrontNMinusSix_ClearAndCentered()
    {
        var map = Read().Maps.Single();
        var grid = ArenaLayout.Expand(map);
        var n = grid.Size;
        var mid = n / 2f - 0.5f;                                             // 문 틈 가운데(칸 중심 = 정수)
        var e = map.Entries.ToDictionary(x => x.Id, x => x.InsideTile);
        Assert.Equal(new TileCoord(4, mid), e["gate_nw"]);                   // 뒤 문 안쪽 4
        Assert.Equal(new TileCoord(mid, 4), e["gate_ne"]);
        Assert.Equal(new TileCoord(n - 6, mid), e["gate_se"]);               // 앞 문 안쪽 N − 6(설계서 11.1 WP5)
        Assert.Equal(new TileCoord(mid, n - 6), e["gate_sw"]);
        foreach (var t in e.Values) Assert.True(grid.IsWalkable(t.Cell.X, t.Cell.Y), $"진입점 {t} 막힘");

        Assert.Equal(new TileCoord(mid, mid), map.HeroStart.Tile);
        Assert.Equal(new TileCoord(mid, mid), map.AnchorsTile[RoomMapRules.CenterAnchor]);
        Assert.DoesNotContain(map.AnchorsTile.Keys, k => k.StartsWith("room_", StringComparison.Ordinal));   // 안쪽 방(room_areas) 없는 방
    }

    [Fact]
    public void Header_NotModeledOnOriginalRoom()
    {
        // 13.5-22: 첫머리 주석 "// 본뜬 원작 방:" — 없음이면 audit_dun 대조 줄을 요청하지 않는다
        var first = System.IO.File.ReadLines(FixturePath()).First();
        Assert.StartsWith("// 본뜬 원작 방: 없음", first);
    }
}
