using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Tests.Map;

public class MapDataReaderTests
{
    // 설계서 6.11 예와 같은 모양(번호는 가짜). 주석·끝 쉼표를 쓴다
    private const string TilesetJson = """
        // 시험용 타일셋
        {
          "tilesets": [
            { "id": "t1", "column_height": 160, "floor": [13, 162],
              "borders": { "hall": { "corner_n": 4, "edge_ne": 2, "corner_e": 23, "edge_se": 18,
                                     "corner_s": 20, "edge_sw": 19, "corner_w": 24, "edge_nw": 1 } },
              "gates":   { "hall": { "nw": { "open": 13, "end_a": 25, "end_b": 25 }, "se": { "open": 13 } } },
              "pieces":  { "pillar": 15 },
              "props": {
                "13": "....", "162": "....",
                "15": "X...",                       // 기둥
                "1": "X.X.", "2": "XX..",            // 뒷벽: 칸의 반만
                "4": "XXX.", "23": "XXXX", "18": "XXXX", "19": "XXXX", "20": "XXXX", "24": "XXXX", "25": "XXXX",
              },
              "sol_known_diffs": [ { "tile": 4, "sub": 3, "sol": "M", "use": ".", "why": "시험용 보정" } ] },
          ],
        }
        """;

    private const string MapJson = """
        {
          "maps": [
            { "id": "arena", "tileset": "t1", "palette": "p1", "size": 10,
              "layout": {
                "border": "hall",
                "gates": [ { "id": "gate_nw", "side": "nw" }, { "id": "gate_se", "side": "se" } ],
                "place": [ { "piece": "pillar", "at": [[3, 3], [6, 6]] } ] },
              "entries": [ { "id": "gate_nw", "outside": [-4, 9.5], "inside": [4, 9.5] },
                           { "id": "gate_se", "outside": [23, 9.5], "inside": [14, 9.5] } ],
              "hero_start": { "tile": [9.5, 9.5], "facing": "south_west" },
              "anchors": { "center": [9.5, 9.5] },
              "art": "d1/maps/arena" }
          ]
        }
        """;

    // 다른 종류의 데이터 오류는 MapDataReader 와 무관하다
    private const string OtherKindsJson = """
        { "enemies": [ { "id": "x", "bogus": 1 } ], "projectiles": "배열도 아님", "unknown_kind": 3 }
        """;

    private static TempDir DataDir(string? mapJson = null, string? tilesetJson = null, params (string Rel, string Text)[] extra)
    {
        var d = new TempDir();
        d.Write("tilesets/t1.json", tilesetJson ?? TilesetJson);
        d.Write("maps/arena.json", mapJson ?? MapJson);
        d.Write("enemies/x.json", OtherKindsJson);
        foreach (var (rel, text) in extra) d.Write(rel, text);
        return d;
    }

    [Fact]
    public void Reads_OnlyTwoKinds_LinksTileset_AndExpands()
    {
        using var d = DataDir();
        var r = MapDataReader.Read(d.Path);
        Assert.Empty(r.Errors);
        Assert.False(r.HasErrors);
        var map = Assert.Single(r.Maps);
        var ts = Assert.Single(r.Tilesets);
        Assert.Same(ts, map.Tileset.Def);
        Assert.Equal(10, map.Size);
        Assert.Equal(Compass.SouthWest, map.HeroStart.Facing);
        Assert.Equal(new TileCoord(23, 9.5f), map.Entries[1].OutsideTile);
        Assert.Equal(new TileCoord(9.5f, 9.5f), map.AnchorsTile["center"]);
        Assert.Equal(".", ts.SolKnownDiffs[0].Use);

        var grid = ArenaLayout.Expand(map);
        Assert.Equal(15, grid.MegaTile(3, 3));
        Assert.Equal(25, grid.MegaTile(0, 3));   // 북서 문 끝(가운데 (10−2)/2 = 4 → 끝 3·6)
        Assert.Equal(13, grid.MegaTile(9, 4));   // 남동 문 틈
        Assert.Equal(18, grid.MegaTile(9, 3));   // 남동 문은 끝 조각이 없어 테두리 그대로
    }

    [Fact]
    public void SameFolder_ReadTwice_SameLayoutHash_AsWorldArena()
    {
        using var d = DataDir();
        var a = MapDataReader.Read(d.Path);
        var b = MapDataReader.Read(d.Path);
        var hash = ArenaLayout.Expand(a.Maps[0]).LayoutHash;
        Assert.Equal(hash, ArenaLayout.Expand(b.Maps[0]).LayoutHash);

        // 읽은 정의로 월드를 만들 수 있다(로더 대신 코드로 묶음) — 월드의 배치도 같은 해시
        var stage = TestDefs.Stage(a.Maps[0]);
        var world = new World(new GameDefs { Tilesets = new(a.Tilesets), Maps = new(a.Maps), Stages = new([stage]) }, 1, stage);
        Assert.Equal(hash, world.Arena!.LayoutHash);
        Assert.Equal(new Vector2(1280, 768), world.MapSize);
        Assert.Equal(world.Arena.Iso.ToWorld(new TileCoord(9.5f, 9.5f)), world.Hero.Pos);
    }

    [Fact]
    public void DuplicateKeys_AreDataErrors_NotExceptions()
    {
        // 원소 안
        using (var d = DataDir(tilesetJson: TilesetJson.Replace("\"id\": \"t1\",", "\"id\": \"t1\", \"id\": \"t2\",")))
        {
            var r = MapDataReader.Read(d.Path);
            var e = Assert.Single(r.Errors, x => x.File == "tilesets/t1.json");
            Assert.Equal("$.tilesets[0].id", e.JsonPath);
            Assert.Contains("두 번", e.Message);
        }
        // 파일 최상위
        using (var d = DataDir(tilesetJson: TilesetJson.Replace("\"tilesets\": [", "\"tilesets\": [], \"tilesets\": [")))
        {
            var r = MapDataReader.Read(d.Path);
            Assert.Contains(r.Errors, e => e.File == "tilesets/t1.json" && e.JsonPath == "$.tilesets");
        }
        // 사전(props) 안 — 형으로 읽으면 마지막 값이 조용히 이기는 자리
        using (var d = DataDir(tilesetJson: TilesetJson.Replace("\"1\": \"X.X.\",", "\"1\": \"X.X.\", \"1\": \"XXXX\",")))
        {
            var r = MapDataReader.Read(d.Path);
            Assert.Contains(r.Errors, e => e.File == "tilesets/t1.json" && e.JsonPath == "$.tilesets[0].props.1");
            Assert.Empty(r.Tilesets);
        }
        // 로더가 넘긴 원소
        var ts = DefJson.ParseNode(TilesetJson.Replace("\"1\": \"X.X.\",", "\"1\": \"X.X.\", \"1\": \"XXXX\","))!["tilesets"]![0];
        var r2 = MapDataReader.Read([new MapDataElement("tilesets/l1.json", "$.tilesets[3]", MapDataReader.TilesetsKind, ts)]);
        Assert.Contains(r2.Errors, e => e.JsonPath == "$.tilesets[3].props.1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("d1\\\\maps\\\\arena")]
    [InlineData("d1/../maps")]
    [InlineData("/d1/maps/arena")]
    public void BadArtPath_IsError(string art)
    {
        using var d = DataDir(MapJson.Replace("\"art\": \"d1/maps/arena\"", $"\"art\": \"{art}\""));
        var r = MapDataReader.Read(d.Path);
        Assert.Contains(r.Errors, e => e.File == "maps/arena.json" && e.JsonPath == "$.maps[0].art");
    }

    [Fact]
    public void ExtendsAndPatchKeys_AreErrors()
    {
        var json = MapJson.Replace("\"id\": \"arena\",", "\"id\": \"arena\", \"extends\": \"base\",");
        using var d = DataDir(json, TilesetJson.Replace("\"column_height\": 160,", "\"column_height\": 160, \"props_add\": {},"));
        var r = MapDataReader.Read(d.Path);
        Assert.Contains(r.Errors, e => e.File == "maps/arena.json" && e.JsonPath == "$.maps[0].extends");
        Assert.Contains(r.Errors, e => e.File == "tilesets/t1.json" && e.JsonPath == "$.tilesets[0].props_add");
        Assert.Empty(r.Maps);
        Assert.Empty(r.Tilesets);
    }

    [Fact]
    public void UnknownProperty_IsError_WithJsonPath()
    {
        using var d = DataDir(MapJson.Replace("\"border\": \"hall\",", "\"border\": \"hall\", \"bordr\": 1,"));
        var r = MapDataReader.Read(d.Path);
        var e = Assert.Single(r.Errors);
        Assert.Equal("maps/arena.json", e.File);
        Assert.StartsWith("$.maps[0].layout", e.JsonPath);
        Assert.Empty(r.Maps);
    }

    [Fact]
    public void MissingTileset_DuplicateIds_HeroStartOutside()
    {
        var map2 = MapJson.Replace("\"tileset\": \"t1\"", "\"tileset\": \"t9\"");
        var map3 = MapJson.Replace("[9.5, 9.5], \"facing\"", "[30, 9.5], \"facing\"");
        using var d = DataDir(extra: [("maps/b.json", map2), ("maps/c.json", map3.Replace("\"id\": \"arena\"", "\"id\": \"arena_c\""))]);
        var r = MapDataReader.Read(d.Path);
        Assert.Contains(r.Errors, e => e.File == "maps/b.json" && e.JsonPath == "$.maps[0].id");   // arena 가 두 번(서수 순으로 뒤)
        Assert.Contains(r.Errors, e => e.File == "maps/c.json" && e.JsonPath == "$.maps[0].hero_start.tile");

        using var d2 = DataDir(map2);
        var r2 = MapDataReader.Read(d2.Path);
        Assert.Contains(r2.Errors, e => e.JsonPath == "$.maps[0].tileset" && e.Message.Contains("t9"));
    }

    [Fact]
    public void LayoutAndTilesetErrors_CarryElementPaths()
    {
        var badMap = MapJson.Replace("[[3, 3], [6, 6]]", "[[0, 3]]");
        var badTs = TilesetJson.Replace("\"25\": \"XXXX\",", "\"25\": \"XXXX\", \"777\": \"....\",");
        using var d = DataDir(badMap, badTs);
        var r = MapDataReader.Read(d.Path);
        Assert.Contains(r.Errors, e => e.File == "maps/arena.json" && e.JsonPath == "$.maps[0].layout.place[0].at[0]");
        Assert.Contains(r.Errors, e => e.File == "tilesets/t1.json" && e.JsonPath == "$.tilesets[0].props.777");
    }

    [Fact]
    public void BrokenJsonFile_IsReported()
    {
        using var d = DataDir(extra: ("weapons/broken.json", "{ \"weapons\": [ "));
        var r = MapDataReader.Read(d.Path);
        var e = Assert.Single(r.Errors);
        Assert.Equal("weapons/broken.json", e.File);
        Assert.Single(r.Maps);   // 나머지는 그대로 읽힌다
    }

    [Fact]
    public void Elements_KeepLoaderFileAndPath()
    {
        var ts = DefJson.ParseNode(TilesetJson)!["tilesets"]![0];
        var map = DefJson.ParseNode(MapJson.Replace("\"size\": 10", "\"size\": 2"))!["maps"]![0];
        var r = MapDataReader.Read(
        [
            new MapDataElement("tilesets/l1.json", "$.tilesets[3]", MapDataReader.TilesetsKind, ts),
            new MapDataElement("maps/x.json", "$.maps[7]", MapDataReader.MapsKind, map),
            new MapDataElement("maps/x.json", "$.maps[8]", MapDataReader.MapsKind, JsonValue.Create(3)),
        ]);
        Assert.Contains(r.Errors, e => e.File == "maps/x.json" && e.JsonPath == "$.maps[7].size");
        Assert.Contains(r.Errors, e => e.File == "maps/x.json" && e.JsonPath == "$.maps[8]");
        Assert.Single(r.Tilesets);
    }

    [Fact]
    public void MissingDataDir_IsError()
    {
        var r = MapDataReader.Read(Path.Combine(Path.GetTempPath(), "ddr-no-such-" + Guid.NewGuid().ToString("N")));
        Assert.True(r.HasErrors);
    }
}
