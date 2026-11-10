using System.Text.Json;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Tests.Assets;

namespace DiabloDefenceRemastered.Tests.Common;

/// <summary>
/// WP1f 형 완료 기준(설계서 11.1 WP1f — v0.8 → v0.8.1) 읽기 시험: <c>rules.hero.belt</c>·<c>level_up.temper</c>·필러 <c>stats</c>·<c>max_hp_pct</c>, <c>RoomDef.floor</c>,
/// 타일셋·맵 <c>mirror</c>, <c>Table/upscale.json</c>(표 9개·<c>output_scale</c>·<c>Applies</c> 둘), manifest <c>upscale</c>·파일 <c>scale</c>, 메타 형식 3·<c>arena.json</c> 형식 2, 새 열거 값.
/// </summary>
public class ContractV08Tests
{
    // ── 6.10 rules.hero / level_up ─────────────────────────────────────

    [Fact]
    public void HeroPotions_Temper_FillerStats_MaxHpPct()
    {
        var r = TestJson.Read<RulesDef>("""
            { "hero": { "class": "sorcerer", "stats": { "max_hp": 100, "max_hp_pct": 0 },
                        "potions": { "slots": 2, "slots_max": 5, "heal_pct": 0.4, "durations": [3.0, 2.0, 1.2], "bot_threshold": 0.35 } },
              "level_up": { "weapon_slots": 4, "passive_slots": 4,
                            "fillers": [ { "id": "filler_potion", "name": "체력 물약", "heal": 30 },
                                         { "id": "filler_temper", "name": "단련", "heal": 5, "stats": { "max_hp": 5 } } ],
                            "temper": "filler_temper" } }
            """);
        Assert.Equal((2, 5, 0.4f, 3), (r.Hero!.Potions!.Slots, r.Hero.Potions.SlotsMax, r.Hero.Potions.HealPct, r.Hero.Potions.Durations.Count));   // v0.9.1: 허리띠 → 물약 칸(WP1g)
        Assert.Equal("filler_temper", r.LevelUp!.Temper);
        Assert.Equal(5f, r.LevelUp.Fillers[1].Stats[StatKey.MaxHp]);
        Assert.Empty(r.LevelUp.Fillers[0].Stats);
        Assert.True(r.Hero.Stats.ContainsKey(StatKey.MaxHpPct));
        Assert.Equal("max_hp_pct", StatKeys.JsonName(StatKey.MaxHpPct));
        Assert.True(StatKeys.TryParse("max_hp_pct", out StatKey k) && k == StatKey.MaxHpPct);
        Assert.Null(TestJson.Read<HeroRulesDef>("""{ "class": "x" }""").Potions);   // 없으면 물약 칸 없음

        Assert.Throws<JsonException>(() => TestJson.Read<HeroRulesDef>("""{ "class": "x", "belt": { "slots": 2, "threshold": 0.3, "cooldown": 1 } }"""));   // v0.9.1: 옛 belt → 모르는 속성
        Assert.Throws<JsonException>(() => TestJson.Read<FillerCardDef>("""{ "id": "filler_gold", "gold": 25 }"""));  // v0.8: gold 없앰
    }

    // ── 6.12 floor, 6.11 mirror ────────────────────────────────────────

    [Fact]
    public void RoomFloor_TilesetMirror_MapMirror_Read()
    {
        Assert.Equal(2, TestJson.Read<RoomDef>("""{ "kind": "normal", "from": "00:00", "to": "01:00", "floor": 2 }""").Floor);
        Assert.Equal(0, TestJson.Read<RoomDef>("""{ "kind": "normal", "from": "00:00", "to": "01:00" }""").Floor);

        var ts = TestJson.Read<TilesetDef>("""
            { "id": "l1", "mirror": { "pairs": [ ["wall_v", "wall_h"], ["grate_h", "grate_v"] ], "self": [ "pillar", "wall_corner" ] } }
            """);
        Assert.Equal([["wall_v", "wall_h"], ["grate_h", "grate_v"]], ts.Mirror!.Pairs.Select(p => p.ToArray()));
        Assert.Equal(["pillar", "wall_corner"], ts.Mirror.Self);
        Assert.Null(TestJson.Read<TilesetDef>("""{ "id": "l1" }""").Mirror);

        var map = TestJson.Read<MapDef>("""{ "id": "m", "tileset": "l1", "layout": { "border": "wall" }, "hero_start": { "tile": [1, 1] }, "mirror": true }""");
        Assert.True(map.Mirror);
        Assert.Null(map.Mirrored);                                       // 읽기(MapDataReader)가 채움
        Assert.False(map.IsMirror);
        Assert.False(TestJson.Read<MapDef>("""{ "id": "m", "tileset": "l1", "layout": { "border": "wall" }, "hero_start": { "tile": [1, 1] } }""").Mirror);
        // 거울판 속성 셋은 JSON 이 아니다(JsonIgnore — 파일에 적어도 채워지지 않는다. 엄격 읽기는 무시된 멤버 이름을 아는 이름으로 본다)
        Assert.Null(TestJson.Read<MapDef>("""{ "id": "m", "tileset": "l1", "layout": { "border": "wall" }, "hero_start": { "tile": [1, 1] }, "mirror_of": "x" }""").MirrorOf);
    }

    // ── 1.4 Table/upscale.json ─────────────────────────────────────────

    private static Dictionary<string, string> Tables(Func<string, string>? upscale = null)
    {
        var t = ImportTableTests.DocTables();
        if (upscale is not null) t["upscale.json"] = upscale(t["upscale.json"]);
        return t;
    }

    [Fact]
    public void Upscale_ReadsStrictly_NineTables()
    {
        Assert.Equal(13, ImportTable.FileNames.Count);   // v0.9 WP1g: + ctrlpan·items·inv·towners
        Assert.Contains("upscale.json", ImportTable.FileNames);
        var r = ImportTable.Read(Tables());
        Assert.Empty(r.Errors);
        var u = r.Table!.Upscale;
        Assert.Equal(4, u.Models["realesrgan-x4plus"].Scale);
        Assert.Equal(4, u.Models["realesrgan-x4plus"].Files.Count);
        Assert.Equal(2, u.OutputScale);
        Assert.True(u.Apply.Maps);
        Assert.Equal(160, u.Apply.MonstersMinWidth);

        var missing = Tables();
        missing.Remove("upscale.json");
        Assert.Contains(ImportTable.Read(missing).Errors, e => e.File == "upscale.json" && e.Message.Contains("없다"));
        Assert.Contains(ImportTable.Read(Tables(s => s.Replace("\"include\": []", "\"include\": [], \"bogus\": 1"))).Errors, e => e.File == "upscale.json");
    }

    [Fact]
    public void Upscale_OutputScale4_IsTableError_V081()
    {
        var r = ImportTable.Read(Tables(s => s.Replace("\"output_scale\": 2", "\"output_scale\": 4")));
        Assert.Contains(r.Errors, e => e.File == "upscale.json" && e.JsonPath == "$.output_scale");
        var badHash = ImportTable.Read(Tables(s => s.Replace("0000000000000000000000000000000000000000000000000000000000000001", "ABC")));
        Assert.Contains(badHash.Errors, e => e.File == "upscale.json" && e.Message.Contains("SHA-256"));
        var badKey = ImportTable.Read(Tables(s => s.Replace("\"exclude\": []", "\"exclude\": [\"monsters/diablo\"]")));
        Assert.Contains(badKey.Errors, e => e.JsonPath == "$.exclude[0]");
    }

    private static UpscaleTable Upscale(IReadOnlyList<string>? include = null, IReadOnlyList<string>? exclude = null, bool maps = true) =>
        ImportTable.Read(Tables()).Table!.Upscale with { Include = include ?? [], Exclude = exclude ?? [], Apply = new UpscaleApplyDef { Maps = maps, MonstersMinWidth = 160 } };

    private static ImportUnitEntry Unit(string outName, int width, params (string Name, int? Width)[] actions) => new()
    {
        Out = outName,
        Cl2 = "monsters\\x\\x{a}.cl2",
        Width = width,
        Actions = actions.ToDictionary(a => a.Name, a => new ImportAction { File = a.Name[..1], Width = a.Width }),
    };

    [Fact]
    public void Upscale_AppliesUnit_ByWidestAction_IncludeExclude()
    {
        var u = Upscale();
        Assert.False(u.Applies(Unit("d1/monsters/zombie", 128, ("walk", null), ("attack", null))));
        Assert.True(u.Applies(Unit("d1/monsters/diablo", 160, ("walk", null))));
        // 동작마다 폭이 다르면 가장 넓은 칸 폭(항목 단위 — 한 묶음 안에서 배율이 섞이지 않게)
        Assert.True(u.Applies(Unit("d1/monsters/mixed", 128, ("walk", null), ("attack", 160))));
        Assert.False(u.Applies(Unit("d1/monsters/mixed2", 128, ("walk", 96), ("attack", 144))));
        Assert.True(Upscale(include: ["d1/monsters/zombie"]).Applies(Unit("d1/monsters/zombie", 128, ("walk", null))));
        Assert.False(Upscale(exclude: ["d1/monsters/diablo"]).Applies(Unit("d1/monsters/diablo", 160, ("walk", null))));
        Assert.False(Upscale(include: ["d1/monsters/zombie"], exclude: ["d1/monsters/zombie"]).Applies(Unit("d1/monsters/zombie", 128, ("walk", null))));   // exclude 가 이김
        Assert.False((u with { Apply = new UpscaleApplyDef { Maps = true } }).Applies(Unit("d1/monsters/diablo", 160, ("walk", null))));   // 문턱 없음 = 몬스터 없음
    }

    [Fact]
    public void Upscale_AppliesTileset_MapsFlag_TilesetKeys()
    {
        Assert.True(Upscale().Applies("l1"));
        Assert.False(Upscale(exclude: ["tileset:l1"]).Applies("l1"));
        Assert.True(Upscale(exclude: ["tileset:l1"]).Applies("l2"));
        Assert.False(Upscale(maps: false).Applies("l5"));
        Assert.True(Upscale(include: ["tileset:l5"], maps: false).Applies("l5"));
    }

    // ── 1.4 manifest upscale·파일 scale, 메타 형식 3, arena.json 형식 2 ───────

    [Fact]
    public void Manifest_Upscale_FileScale_ReadWrite()
    {
        var m = AssetJson.Deserialize<AssetManifest>("""
            { "format": 3, "tool": "t", "runtime": "9.0", "table_hash": "h",
              "upscale": { "status": "done", "model": "realesrgan-x4plus", "tool": "realesrgan-ncnn-vulkan v0.2.5.0",
                           "files": { "realesrgan-ncnn-vulkan.exe": "aa" }, "output_scale": 2, "gpu": "mixed" },
              "files": [ { "path": "d1/monsters/diablo.png", "size": 1, "sha256": "x", "scale": 2 },
                         { "path": "d1/hero/slt.png", "size": 1, "sha256": "y" } ] }
            """);
        Assert.Equal(UpscaleStatus.Done, m.Upscale.Status);
        Assert.Equal(2, m.Upscale.ArtScale);
        Assert.Equal("mixed", m.Upscale.Gpu);
        Assert.Equal(2, m.Files[0].Scale);
        Assert.Equal(1, m.Files[1].Scale);                               // 없으면 1
        var json = System.Text.Encoding.UTF8.GetString(AssetJson.Serialize(m));
        Assert.Contains("\"scale\": 2", json);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(json, "\"scale\""));   // 1 이면 쓰지 않음
        Assert.Contains("\"status\": \"done\"", json);

        var off = AssetJson.Deserialize<AssetManifest>("""{ "format": 3, "upscale": { "status": "fallback", "reason": "해시 다름" } }""");
        Assert.Equal(1, off.Upscale.ArtScale);
        Assert.Equal("해시 다름", off.Upscale.Reason);
        Assert.Equal(UpscaleStatus.Off, AssetJson.Deserialize<AssetManifest>("""{ "format": 3 }""").Upscale.Status);
        Assert.Throws<JsonException>(() => AssetJson.Deserialize<AssetManifest>("""{ "format": 3, "upscale": { "status": "partial" } }"""));
    }

    [Fact]
    public void SpriteMetaFormat3_Scale_ArenaArtFormat2_Scale()
    {
        const string meta = """
            { "format": 3, "scale": 2, "pages": [[192, 192]],
              "actions": { "walk": { "cell": [192, 160], "directions": 1, "frames": 1, "rects": [[0, 0, 0, 100, 150, 40, 10]] } } }
            """;
        var ok = SpriteMeta.Parse(meta);
        Assert.Empty(ok.Validate());
        Assert.Equal(2, ok.Scale);
        Assert.Equal(1, SpriteMeta.Parse(meta.Replace("\"scale\": 2, ", "")).Scale);   // 기본 1
        Assert.Contains(SpriteMeta.Parse(meta.Replace("\"scale\": 2", "\"scale\": 4")).Validate(), e => e.Contains("scale 4"));
        Assert.Contains(SpriteMeta.Parse(meta.Replace("\"format\": 3", "\"format\": 2")).Validate(), e => e.Contains("형식 2"));
        Assert.Equal(3, SpriteMeta.CurrentFormat);
        Assert.Equal(2, ArenaArt.CurrentFormat);
        Assert.Equal(3, AssetFormat.Version);

        var grid = ArenaLayout.Expand(TestDefs.Map(10));
        var floor = string.Join(", ", ArenaArt.FloorChunks(grid).Select(c => $$"""{ "file": "{{ArenaArt.FloorChunkFile(c)}}", "pos": [{{c.X * 512}}, {{c.Y * 512}}] }"""));
        var art = $$"""{ "format": 2, "scale": 2, "map": "test_map", "layout_hash": "{{grid.LayoutHash}}", "box": [1280, 768], "floor": [ {{floor}} ], "cells": [] }""";
        Assert.Empty(ArenaArt.Parse(art).Validate(grid));
        Assert.Contains(ArenaArt.Parse(art.Replace("\"scale\": 2", "\"scale\": 3")).Validate(grid), e => e.Contains("scale 3"));
        Assert.Contains(ArenaArt.Parse(art.Replace("\"format\": 2", "\"format\": 1")).Validate(grid), e => e.Contains("형식 1"));
    }

    [Fact]
    public void AssetScale_SupportedAndFitsArtScale()
    {
        Assert.True(AssetScale.IsSupported(1));
        Assert.True(AssetScale.IsSupported(2));
        Assert.False(AssetScale.IsSupported(4));
        Assert.True(AssetScale.FitsArtScale(1, 2));
        Assert.True(AssetScale.FitsArtScale(2, 2));
        Assert.False(AssetScale.FitsArtScale(2, 1));                     // 확대 그림인데 s = 1(되돌아간 manifest)
        Assert.True(AssetScale.FitsArtScale(1, 1));
    }

    // ── 새 열거 값·계약 멤버 ───────────────────────────────────────────

    [Fact]
    public void NewEnumValues_AndMembers()
    {
        Assert.False(Enum.GetNames<CueKind>().Contains("BeltPotion"));   // v0.9.1 WP1g 에 없앰(PotionDrink)
        Assert.Equal(["Evolution", "WeaponUpgrade", "PassiveUpgrade", "Temper"], Enum.GetNames<ChestItemKind>());
        Assert.Equal(["WeaponCards", "WeaponBundle", "HealNextRoom", "Shrine", "Merchant"], Enum.GetNames<RewardKind>());   // v0.9 WP1g
        Assert.NotNull(typeof(IProgression).GetProperty(nameof(IProgression.PicksLeft)));
        Assert.NotNull(typeof(IHeroCarry).GetMethod(nameof(IHeroCarry.Validate)));
        var hero = new Hero { WeaponSlotsOpen = 5, PassiveSlotsOpen = 5, Potions = 2 };
        hero.Reset();
        Assert.Equal((0, 0, 0), (hero.WeaponSlotsOpen, hero.PassiveSlotsOpen, hero.Potions));   // Reset 은 규칙을 받지 않는다(주인공 만들기 WP3 가 정함)
        Assert.Equal(-1, new RoomRecord(1, 1, "m", "gate_sw", null, null, -1, 0, false, 0, 1).BossClockTicks);
        Assert.Equal(-1, new RoomRecord(1, 1, "m", "gate_sw", null, null, -1, 0, false, 0, 1).PicksLeft);
        Assert.Equal(0, new RoomExit("gate_ne", null, RoomKind.Normal, null, false, null).NextFloor);
    }
}
