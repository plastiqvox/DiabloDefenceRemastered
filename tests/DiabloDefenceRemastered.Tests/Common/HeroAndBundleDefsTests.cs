using System.Text.Json;
using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Tests.Common;

/// <summary>
/// WP1d-형 계약 읽기 (설계서 11.1 WP1d 완료 기준): 중간 보스 패턴 조건(v0.5.3)·직업(6.16)·자원 묶음(6.17)·안쪽 방 맵 모양(6.11).
/// </summary>
public class HeroAndBundleDefsTests
{
    // ── 중간 보스(v0.5.3) ─────────────────────────────────────────

    [Fact]
    public void PatternDef_ClearSight_DefaultsFalse()
    {
        Assert.True(JsonSerializer.Deserialize<PatternDef>("""{ "clear_sight": true }""", DefJson.Options)!.ClearSight);
        Assert.False(JsonSerializer.Deserialize<PatternDef>("""{ "cooldown": 4 }""", DefJson.Options)!.ClearSight);
    }

    [Fact]
    public void EnemyProjectileSpawn_SightGated_DefaultsFalse()
    {
        var plain = new EnemyProjectileSpawn(null!, Vector2.Zero, Vector2.UnitX, 5, default);
        Assert.False(plain.SightGated);
        Assert.True((plain with { SightGated = true }).SightGated);
    }

    // ── 직업(v0.6, 6.16) ─────────────────────────────────────────

    private const string SorcererJson = """
        { "id": "sorcerer", "name": "마법사", "description": "마법 무기의 쿨다운이 조금 짧다",
          "sprites": "set_sorcerer", "radius": 14,
          "stats": {},                                         // rules.hero.stats 를 키별로 덮어씀
          "start_weapons": ["magic_missile"],
          "traits": ["trait_arcane_affinity"] }
        """;

    private const string MonkJson = """
        { "id": "monk", "name": "수도사", "description": "…", "sprites": "set_monk", "radius": 14,
          "stats": { "max_hp": 115, "move_speed": 100 }, "start_weapons": ["staff_sweep"],
          "traits": ["trait_discipline"],
          "requires": ["hfmonk"] }
        """;

    [Fact]
    public void HeroClassDef_DocExamples_Read()
    {
        var sorc = TestJson.Read<HeroClassDef>(SorcererJson);
        Assert.Equal("set_sorcerer", sorc.Sprites.Id);
        Assert.Equal(14f, sorc.Radius);
        Assert.Empty(sorc.Stats);
        Assert.Equal(["magic_missile"], sorc.StartWeapons.Select(w => w.Id));
        Assert.Equal(["trait_arcane_affinity"], sorc.Traits.Select(t => t.Id));
        Assert.Empty(sorc.Requires);

        var monk = TestJson.Read<HeroClassDef>(MonkJson);
        Assert.Equal(115f, monk.Stats[StatKey.MaxHp]);
        Assert.Equal(100f, monk.Stats[StatKey.MoveSpeed]);
        Assert.Equal([ContentBundle.Hfmonk], monk.Requires);
    }

    [Fact]
    public void HeroClassDef_UnknownBundle_And_MissingStartWeapons_AreErrors()
    {
        Assert.Throws<JsonException>(() => TestJson.Read<HeroClassDef>(MonkJson.Replace("\"hfmonk\"", "\"hfmnok\"")));
        Assert.Throws<JsonException>(() => TestJson.Read<HeroClassDef>("""{ "id": "x", "sprites": "set_x" }"""));
        // v0.6 의 특성 객체(HeroTraitDef)는 없다 — traits 는 패시브 ID 목록
        Assert.Throws<JsonException>(() => TestJson.Read<HeroClassDef>(SorcererJson.Replace("[\"trait_arcane_affinity\"]", "[{ \"id\": \"t\" }]")));
    }

    [Fact]
    public void ContentBundle_JsonNames_And_MpqNames()
    {
        foreach (var b in ContentBundles.All)
        {
            Assert.Equal(b, JsonSerializer.Deserialize<ContentBundle>($"\"{ContentBundles.Name(b)}\"", DefJson.Options));
            Assert.Equal(ContentBundles.Name(b) + ".mpq", ContentBundles.MpqName(b));
        }
        Assert.Equal("hellfire.mpq", ContentBundles.MpqName(ContentBundle.Hellfire));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ContentBundle>("\"diabdat\"", DefJson.Options));   // 기본 내용은 묶음이 아니다
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ContentBundle>("0", DefJson.Options));
    }

    [Fact]
    public void PassiveDef_TraitAndClasses()
    {
        var p = TestJson.Read<PassiveDef>("""
            { "id": "trait_arcane_affinity", "name": "마력 친화",
              "trait": true, "max_level": 1, "per_level": { "cooldown_pct": 0.05 }, "classes": ["sorcerer"] }
            """);
        Assert.True(p.Trait);
        Assert.Equal(["sorcerer"], p.Classes.Select(c => c.Id));
        var plain = TestJson.Read<PassiveDef>("""{ "id": "armor", "max_level": 5 }""");
        Assert.False(plain.Trait);
        Assert.Empty(plain.Classes);
        Assert.Empty(TestJson.Read<EvolutionDef>("""{ "weapon": "a", "passive": "b", "result": "c" }""").Classes);
    }

    [Fact]
    public void RulesHero_ClassRequired_OldKeysAreErrors()
    {
        var hero = TestJson.Read<HeroRulesDef>("""{ "class": "sorcerer", "invuln_after_hit": 0.25, "stats": { "max_hp": 100 } }""");
        Assert.Equal("sorcerer", hero.Class.Id);
        Assert.Equal(100f, hero.Stats[StatKey.MaxHp]);
        Assert.Throws<JsonException>(() => TestJson.Read<HeroRulesDef>("""{ "stats": {} }"""));
        // v0.5 의 sprites·radius·start_weapons 는 직업 정의로 옮겼다
        Assert.Throws<JsonException>(() => TestJson.Read<HeroRulesDef>("""{ "class": "sorcerer", "start_weapons": ["magic_missile"] }"""));
        Assert.Throws<JsonException>(() => TestJson.Read<HeroRulesDef>("""{ "class": "sorcerer", "radius": 14 }"""));
        Assert.Equal(1.5f, TestJson.Read<SpawnRulesDef>("{}").PathDetour);

        // v0.6.3 방 체류 압박: 없으면 끔, 있으면 세 값 모두 필수(기본값을 코드에 두지 않음)
        Assert.Null(TestJson.Read<SpawnRulesDef>("{}").RoomPressure);
        var rp = TestJson.Read<SpawnRulesDef>("""{ "room_pressure": { "after": 8, "every_nth": 3, "min_distance": 150 } }""").RoomPressure!;
        Assert.Equal((8f, 3, 150f), (rp.After, rp.EveryNth, rp.MinDistance));
        Assert.Throws<JsonException>(() => TestJson.Read<SpawnRulesDef>("""{ "room_pressure": { "after": 8, "every_nth": 3 } }"""));
    }

    [Fact]
    public void MusicDef_RequiresAndFallback()
    {
        var m = TestJson.Read<MusicDef>("""{ "id": "mus_crypt", "file": "d1/sound/music/dlvle.wav", "requires": ["hfmusic"], "fallback": "mus_dlvla" }""");
        Assert.Equal([ContentBundle.Hfmusic], m.Requires);
        Assert.Equal("mus_dlvla", m.Fallback!.Id);
        Assert.Null(TestJson.Read<MusicDef>("""{ "id": "mus_title", "file": "d1/sound/music/dintro.wav" }""").Fallback);
    }

    [Fact]
    public void WeaponDef_ClassesAndRequires()
    {
        var w = TestJson.Read<WeaponDef>("""{ "id": "staff_sweep", "cast_clip": "attack", "classes": ["monk"], "requires": ["hfmonk"] }""");
        Assert.Equal(ClipKind.Attack, w.CastClip);
        Assert.Equal(["monk"], w.Classes.Select(c => c.Id));
        Assert.Equal([ContentBundle.Hfmonk], w.Requires);
        Assert.Empty(TestJson.Read<WeaponDef>("""{ "id": "mm" }""").Classes);
    }

    [Fact]
    public void EnemyAndStage_Requires()
    {
        var e = TestJson.Read<EnemyDef>("""{ "id": "hf_stinger", "sprites": "set_stinger", "requires": ["hellfire"] }""");
        Assert.Equal([ContentBundle.Hellfire], e.Requires);
        var s = TestJson.Read<StageDef>("""{ "id": "stage_crypt", "map": "crypt_arena", "requires": ["hellfire"] }""");
        Assert.Equal([ContentBundle.Hellfire], s.Requires);
        Assert.Empty(TestJson.Read<StageDef>("""{ "id": "stage_cathedral", "map": "cathedral_arena" }""").Requires);
    }

    [Fact]
    public void SoundBindings_Hero_IsKeyedByClassId()
    {
        var b = TestJson.Read<SoundBindingsDef>("""
            { "hero": { "sorcerer": { "hurt": "snd_sorc_hurt", "death": "snd_sorc_death" },
                        "monk":     { "hurt": "snd_monk_hurt" } } }
            """);
        Assert.Equal("snd_sorc_hurt", b.Hero["sorcerer"].Hurt!.Id);
        Assert.Null(b.Hero["monk"].Death);
        // v0.5 의 객체 하나(직업 키 없음)는 이제 모르는 속성이다
        Assert.Throws<JsonException>(() => TestJson.Read<SoundBindingsDef>("""{ "hero": { "hurt": "snd_a" } }"""));
    }

    [Fact]
    public void World_HeroClass_FromArgument_OrRules_OrNone()
    {
        var defs = TestDefs.Basic(8);
        var stage = defs.Stages[0];

        var byRules = new World(defs, 1, stage);
        Assert.Equal(TestDefs.HeroClassId, byRules.HeroClass!.Id);
        Assert.Equal(14f, byRules.Hero.Radius);

        var small = TestDefs.HeroClass(defs.SpriteSets[0], defs.Weapons[0], id: "small", radius: 10f);
        var byArg = new World(defs, 1, stage, small);
        Assert.Same(small, byArg.HeroClass);
        Assert.Equal(10f, byArg.Hero.Radius);

        var none = new World(GameDefs.Empty, 1);
        Assert.Null(none.HeroClass);
        Assert.Equal(14f, none.Hero.Radius);

        var built = new TestWorldBuilder().WithMegaTiles(8).WithHeroClass(small).Build();
        Assert.Same(small, built.World.HeroClass);
    }

    // ── 맵 모양(v0.6, 6.11) ───────────────────────────────────────

    private static MapDataResult ReadMaps(string tilesetJson, string mapJson) => MapDataReader.Read(
    [
        new MapDataElement("tilesets/t.json", "$.tilesets[0]", MapDataReader.TilesetsKind, DefJson.ParseNode(tilesetJson)),
        new MapDataElement("maps/m.json", "$.maps[0]", MapDataReader.MapsKind, DefJson.ParseNode(mapJson)),
    ]);

    private const string TilesetJson = """
        { "id": "t", "column_height": 160, "floor": [13],
          "borders": { "hall": { "corner_n": 4, "edge_ne": 2, "corner_e": 23, "edge_se": 18, "corner_s": 20, "edge_sw": 19, "corner_w": 24, "edge_nw": 1 } },
          "gates": { "hall": { "nw": { "open": 13 } } },
          "pieces": { "pillar": 15, "wall_h": 2, "door_closed": 98 },
          "marks": { "pillar": [15], "closed_door": [98] },
          "props": { "13": "....", "15": "x...", "1": "X.X.", "2": "XX..", "4": "XXX.", "98": "XX..",
                     "18": "XXXX", "19": "XXXX", "20": "XXXX", "23": "XXXX", "24": "XXXX" } }
        """;

    private const string MapJson = """
        { "id": "m", "tileset": "t", "palette": "p", "size": 10,
          "layout": { "border": "hall", "gates": [ { "id": "gate_nw", "side": "nw" } ],
                      "place": [ { "id": "room", "origin": [2, 2], "grid": [ "wall_h   wall_h", ".  .  pillar" ] } ] },
          "open_center": [3, 3, 7, 7],
          "hero_start": { "tile": [9.5, 9.5] },
          "anchors": { "center": [9.5, 9.5], "room_a": [5, 7] },
          "room_areas": { "room_a": [2, 3, 4, 5] },
          "art": "d1/maps/m" }
        """;

    [Fact]
    public void MapAndTileset_V06Shapes_Read()
    {
        var r = ReadMaps(TilesetJson, MapJson);
        Assert.Empty(r.Errors);
        var ts = r.Tilesets[0];
        Assert.Equal([98], ts.Marks[TileMark.ClosedDoor]);   // 두 낱말 키 closed_door
        Assert.Equal([15], ts.Marks[TileMark.Pillar]);
        var map = r.Maps[0];
        Assert.Equal(new RectI(3, 3, 7, 7), map.OpenCenter);
        Assert.Equal(new RectI(2, 3, 4, 5), map.RoomAreas["room_a"]);   // v0.6.3
        var place = map.Layout.Place[0];
        Assert.Null(place.Piece);
        Assert.Equal(new Int2(2, 2), place.Origin);
        Assert.Equal("room", place.Id);
        Assert.False(place.OverBorder);

        var grid = ArenaLayout.Expand(map);
        Assert.Equal(2, grid.MegaTile(2, 2));
        Assert.Equal(2, grid.MegaTile(3, 2));
        Assert.Equal(13, grid.MegaTile(3, 3));    // '.' = 건드리지 않음
        Assert.Equal(15, grid.MegaTile(4, 3));
    }

    [Fact]
    public void TileMark_UnknownKey_And_MapRequires_AreErrors()
    {
        var badMark = ReadMaps(TilesetJson.Replace("\"closed_door\": [98]", "\"door\": [98]"), MapJson);
        Assert.Contains(badMark.Errors, e => e.File == "tilesets/t.json" && e.Severity == DataSeverity.Error);
        Assert.Empty(badMark.Tilesets);

        // 맵에는 requires 를 두지 않는다(1.4 — 맵 그림 묶음은 타일셋 표 항목의 bundle)
        var withRequires = ReadMaps(TilesetJson, MapJson.Replace("\"art\":", "\"requires\": [\"hellfire\"], \"art\":"));
        Assert.Contains(withRequires.Errors, e => e.File == "maps/m.json" && e.Message.Contains("requires"));
        Assert.Empty(withRequires.Maps);
    }

    [Fact]
    public void TileMark_JsonNames_RoundTrip()
    {
        foreach (var mark in Enum.GetValues<TileMark>())
        {
            var json = JsonSerializer.Serialize(mark, DefJson.Options);
            Assert.Equal($"\"{ArenaLayout.MarkName(mark)}\"", json);
            Assert.Equal(mark, JsonSerializer.Deserialize<TileMark>(json, DefJson.Options));
        }
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TileMark>("\"door\"", DefJson.Options));
    }
}
