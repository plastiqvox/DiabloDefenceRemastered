using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Tests.Assets;

public class ImportTableTests
{
    /// <summary>설계서 1.4 의 표 8개 예 그대로(주석 포함).</summary>
    internal static Dictionary<string, string> DocTables() => new()
    {
        ["monsters.json"] = """
            // Table/monsters.json — 표 머리 값은 항목 기본값, 항목·동작이 덮어쓴다
            { "palette": "levels\\l1data\\l1.pal", "shadow_alpha": 0.5,
              "monsters": [
                { "out": "d1/monsters/zombie", "cl2": "monsters\\zombie\\zombie{a}.cl2", "width": 128,
                  "actions": { "idle": "n", "walk": "w", "attack": "a", "special": "s", "die": "d" },
                  "variants": { "": null, "bluered": "monsters\\zombie\\bluered.trn", "grey": "monsters\\zombie\\grey.trn",
                                "yellow": "monsters\\zombie\\yellow.trn", "eth": "monsters\\monsters\\eth.trn" } } ] }
            """,
        ["hero.json"] = """
            { "palette": "levels\\l1data\\l1.pal", "shadow_alpha": 0.5,
              "hero": [
                { "out": "d1/hero/slt", "cl2": "plrgfx\\sorceror\\slt\\slt{a}.cl2", "width": 96,
                  "actions": { "idle": "as", "walk": "aw", "attack": { "file": "at", "width": 128 },
                               "cast_fire": { "file": "fm", "width": 128 }, "cast_lightning": { "file": "lm", "width": 128 },
                               "cast_magic": { "file": "qm", "width": 128 },
                               "die": { "cl2": "plrgfx\\sorceror\\sln\\slndt.cl2", "width": 128 } },
                  "variants": { "": null } } ] }
            """,
        ["missiles.json"] = """
            // shadow_alpha 를 적은 항목만 색인 0 을 그림자로 바꾼다
            { "palette": "levels\\l1data\\l1.pal",
              "missiles": [
                { "out": "d1/missiles/fireba",   "cl2": "missiles\\fireba{d}.cl2", "dirs": 16, "width": 96 },
                { "out": "d1/missiles/arrows",   "cl2": "missiles\\arrows.cl2", "frames_are_dirs": 16, "width": 96 },
                { "out": "d1/missiles/firewal2", "cl2": "missiles\\firewal2.cl2", "width": 128, "frames": [1, 11] },
                { "out": "d1/missiles/firerun",  "cl2": "missiles\\firerun{d}.cl2", "dirs": 8, "width": 128, "shadow_alpha": 0.5 } ] }
            """,
        ["icons.json"] = """
            { "size": 48, "background": [16, 12, 10],
              "icons": [
                { "out": "d1/icons/fireball", "cl2": "missiles\\fireba{d}.cl2", "dirs": 16, "width": 96,
                  "layers": [ { "dir": 4, "frame": 7 } ] },
                { "out": "d1/icons/multishot", "cl2": "missiles\\arrows.cl2", "frames_are_dirs": 16, "width": 96,
                  "layers": [ { "dir": 3, "frame": 1, "offset": [-6, 4] }, { "dir": 4, "frame": 1 }, { "dir": 5, "frame": 1, "offset": [6, 4] } ] } ] }
            """,
        ["sounds.json"] = """
            { "sounds": [
                { "path": "sfx\\misc\\cbolt.wav" },
                { "path": "monsters\\zombie\\zombie{k}{i}.wav", "vars": { "k": ["a", "h", "d", "s"], "i": ["1", "2"] } } ] }
            """,
        ["music.json"] = """
            { "music": [ { "path": "music\\dintro.wav" }, { "path": "music\\dlvla.wav" }, { "path": "music\\dlvlb.wav" } ] }
            """,
        ["exe.json"] = """
            { "files": [ "TITLE.png", "LOGO.png", "Gold.png", "DIInop.cur" ] }
            """,
        ["tilesets.json"] = """
            { "tilesets": [
                { "id": "l1", "dir": "levels\\l1data", "cel": "l1.cel", "min": "l1.min", "til": "l1.til", "sol": "l1.sol",
                  "pieces_per_subtile": 10, "palettes": { "l1_1": "l1_1.pal", "l1_2": "l1_2.pal" },
                  "overlay": { "cel": "l1s.cel",                       // 출처: DevilutionX 참고(아치 덧그림 번호표, 부록 A.5)
                               "frames": { "1": [11, 70, 320, 210, 340, 417], "2": [10, 248, 324, 343, 330, 420],
                                           "3": [252], "4": [254], "5": [258], "6": [266] } } } ] }
            """,
    };

    private static ImportTable ReadOk(Dictionary<string, string> files)
    {
        var r = ImportTable.Read(files);
        Assert.Empty(r.Errors);
        return r.Table!;
    }

    [Fact]
    public void DocExamples_ReadStrictly_WithoutErrors()
    {
        var t = ReadOk(DocTables());
        Assert.Equal(0.5f, t.Monsters.ShadowAlpha);
        Assert.Equal(5, t.Monsters.Monsters[0].Actions.Count);
        Assert.Null(t.Monsters.Monsters[0].Variants[""]);
        Assert.Equal(16, t.Missiles.Missiles[0].Dirs);
        Assert.Equal(16, t.Missiles.Missiles[1].Directions);
        Assert.Equal([1, 11], t.Missiles.Missiles[2].Frames!);
        Assert.Equal(new Rgb(16, 12, 10), t.Icons.Background);
        Assert.Equal(new Int2(-6, 4), t.Icons.Icons[1].Layers[0].Offset);
        Assert.Equal([11, 70, 320, 210, 340, 417], t.Tilesets.Tilesets[0].Overlay!.Frames[1]);
        Assert.Equal("levels\\l1data\\l1.cel", t.Tilesets.Tilesets[0].SourcePath("l1.cel"));
        Assert.Equal(["TITLE.png", "LOGO.png", "Gold.png", "DIInop.cur"], t.ExeNames);
    }

    [Fact]
    public void OutputNames_SetsTimesVariantPages_MissilesPacked_Icons_SoundsExpanded()
    {
        var names = ReadOk(DocTables()).OutputNames();
        string[] expected =
        [
            "d1/hero/slt.json", "d1/hero/slt.png",
            "d1/icons/fireball.png", "d1/icons/multishot.png",
            "d1/missiles/arrows.json", "d1/missiles/arrows.png",
            "d1/missiles/fireba.json", "d1/missiles/fireba.png",
            "d1/missiles/firerun.json", "d1/missiles/firerun.png",
            "d1/missiles/firewal2.json", "d1/missiles/firewal2.png",
            "d1/monsters/zombie.json", "d1/monsters/zombie.png",
            "d1/monsters/zombie@bluered.png", "d1/monsters/zombie@eth.png", "d1/monsters/zombie@grey.png", "d1/monsters/zombie@yellow.png",
            "d1/sound/index.json",
            "d1/sound/monsters/zombie/zombiea1.wav", "d1/sound/monsters/zombie/zombiea2.wav",
            "d1/sound/monsters/zombie/zombied1.wav", "d1/sound/monsters/zombie/zombied2.wav",
            "d1/sound/monsters/zombie/zombieh1.wav", "d1/sound/monsters/zombie/zombieh2.wav",
            "d1/sound/monsters/zombie/zombies1.wav", "d1/sound/monsters/zombie/zombies2.wav",
            "d1/sound/music/dintro.wav", "d1/sound/music/dlvla.wav", "d1/sound/music/dlvlb.wav",
            "d1/sound/sfx/misc/cbolt.wav",
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), names);
    }

    [Fact]
    public void MissileDirFiles_AreSixteenSources_ForOnePage()
    {
        var t = ReadOk(DocTables());
        var fireba = t.Missiles.Missiles[0].SourcePaths();
        Assert.Equal(16, fireba.Count);
        Assert.Equal("missiles\\fireba1.cl2", fireba[0]);
        Assert.Equal("missiles\\fireba16.cl2", fireba[15]);
        Assert.Equal(["missiles\\arrows.cl2"], t.Missiles.Missiles[1].SourcePaths());
        Assert.Equal(8, t.Missiles.Missiles[3].SourcePaths().Count);
    }

    [Fact]
    public void HeroActions_WidthAndPathOverrides()
    {
        var t = ReadOk(DocTables());
        var slt = t.Hero.Hero[0];
        var idle = slt.Resolve("idle", t.Hero.ShadowAlpha);
        Assert.Equal("plrgfx\\sorceror\\slt\\sltas.cl2", idle.SourcePath);
        Assert.Equal(96, idle.Width);
        Assert.Equal(0.5f, idle.ShadowAlpha);
        Assert.Equal(128, slt.Resolve("cast_fire", t.Hero.ShadowAlpha).Width);
        Assert.Equal("plrgfx\\sorceror\\slt\\sltfm.cl2", slt.Resolve("cast_fire", null).SourcePath);
        Assert.Equal("plrgfx\\sorceror\\sln\\slndt.cl2", slt.Resolve("die", null).SourcePath);   // 동작 cl2 가 틀을 대신
        Assert.Equal(["attack", "cast_fire", "cast_lightning", "cast_magic", "die", "idle", "walk"],
            slt.ResolveAll(null).Select(a => a.Name));   // 묶기 순서 = 동작 이름 서수
    }

    [Fact]
    public void ShadowAlpha_OverrideOrder_HeadThenEntryThenAction()
    {
        var files = DocTables();
        files["monsters.json"] = """
            { "palette": "levels\\l1data\\l1.pal", "shadow_alpha": 0.5,
              "monsters": [
                { "out": "d1/monsters/a", "cl2": "m\\a{a}.cl2", "width": 96, "actions": { "walk": "w" } },
                { "out": "d1/monsters/b", "cl2": "m\\b{a}.cl2", "width": 96, "shadow_alpha": 0.3,
                  "actions": { "walk": "w", "die": { "file": "d", "shadow_alpha": 0.1, "frames": [2, 9] } } } ] }
            """;
        var t = ReadOk(files);
        var head = t.Monsters.ShadowAlpha;
        Assert.Equal(0.5f, t.Monsters.Monsters[0].Resolve("walk", head).ShadowAlpha);   // 표 머리
        Assert.Equal(0.3f, t.Monsters.Monsters[1].Resolve("walk", head).ShadowAlpha);   // 항목
        var die = t.Monsters.Monsters[1].Resolve("die", head);
        Assert.Equal(0.1f, die.ShadowAlpha);                                              // 동작
        Assert.Equal([2, 9], die.Frames!);
        Assert.Null(t.Missiles.Missiles[0].ShadowAlpha);                                  // 마법은 적은 항목만
        Assert.Equal(0.5f, t.Missiles.Missiles[3].ShadowAlpha);
    }

    [Fact]
    public void Sounds_VarsExpand_InKeyOrder()
    {
        var t = ReadOk(DocTables());
        var paths = t.Sounds.Sounds[1].SourcePaths();
        Assert.Equal(8, paths.Count);
        Assert.Equal("monsters\\zombie\\zombiea1.wav", paths[0]);   // 키 서수 순(i 바깥, k 안)
        Assert.Equal("monsters\\zombie\\zombieh1.wav", paths[1]);
        Assert.Equal("d1/sound/monsters/zombie/zombiea1.wav", AssetFormat.SoundOutput(paths[0]));
    }

    [Fact]
    public void UnknownProperty_And_MissingFile_AreErrors()
    {
        var files = DocTables();
        files["music.json"] = """{ "music": [ { "path": "music\\dintro.wav", "lopo": true } ] }""";
        files.Remove("exe.json");
        var r = ImportTable.Read(files);
        Assert.Null(r.Table);
        Assert.Contains(r.Errors, e => e.File == "music.json" && e.JsonPath.Contains("music"));
        Assert.Contains(r.Errors, e => e.File == "exe.json" && e.Message.Contains("없다"));
    }

    [Fact]
    public void ActionObject_IsStrict()
    {
        var files = DocTables();
        files["hero.json"] = files["hero.json"].Replace("{ \"file\": \"at\", \"width\": 128 }", "{ \"file\": \"at\", \"widht\": 128 }");
        var r = ImportTable.Read(files);
        Assert.Null(r.Table);
        Assert.Contains(r.Errors, e => e.File == "hero.json");
    }

    [Fact]
    public void Validate_ShapeErrors()
    {
        var files = DocTables();
        files["monsters.json"] = """
            { "palette": "p.pal",
              "monsters": [
                { "out": "monsters/zombie", "cl2": "m\\z{a}.cl2", "width": 0, "actions": { "walk": "w" },
                  "variants": { "": "x.trn", "Blue": "b.trn" } },
                { "out": "d1/monsters/k", "cl2": "m\\k.cl2", "width": 96, "actions": { "walk": "w", "die": { "file": "d", "cl2": "m\\kd.cl2" } } } ] }
            """;
        files["missiles.json"] = """
            { "palette": "p.pal",
              "missiles": [ { "out": "d1/missiles/a", "cl2": "missiles\\a.cl2", "dirs": 16, "width": 96 },
                            { "out": "d1/missiles/a", "cl2": "missiles\\b{d}.cl2", "width": 96, "frames": [3, 1] } ] }
            """;
        files["sounds.json"] = """{ "sounds": [ { "path": "sfx\\x{k}.wav", "vars": { "j": ["1"] } } ] }""";
        files["icons.json"] = """
            { "icons": [ { "out": "d1/icons/x", "cl2": "missiles\\arrows.cl2", "frames_are_dirs": 16, "width": 96,
                           "layers": [ { "dir": 16, "frame": 2 } ] } ] }
            """;
        var r = ImportTable.Read(files);
        Assert.NotNull(r.Table);
        bool Has(string file, string path) => r.Errors.Any(e => e.File == file && e.JsonPath == path);
        Assert.True(Has("monsters.json", "$.monsters[0].out"));
        Assert.True(Has("monsters.json", "$.monsters[0].width"));
        Assert.True(Has("monsters.json", "$.monsters[0].variants[\"\"]"));
        Assert.True(Has("monsters.json", "$.monsters[0].variants.Blue"));
        Assert.True(Has("monsters.json", "$.monsters[1].cl2"));               // 글자를 쓰는데 틀에 {a} 없음
        Assert.True(Has("monsters.json", "$.monsters[1].actions.die"));       // file 과 cl2 둘 다
        Assert.True(Has("missiles.json", "$.missiles[0].cl2"));               // dirs 인데 {d} 없음
        Assert.True(Has("missiles.json", "$.missiles[1].cl2"));               // dirs 없는데 {d}
        Assert.True(Has("missiles.json", "$.missiles[1].frames"));
        Assert.True(Has("missiles.json", "$.missiles[1]"));                   // 출력 이름이 겹침
        Assert.True(Has("sounds.json", "$.sounds[0].path"));
        Assert.True(Has("icons.json", "$.icons[0].layers[0].dir"));
        Assert.True(Has("icons.json", "$.icons[0].layers[0].frame"));
    }

    [Fact]
    public void Load_FromFolder_AndHash_IsConcatenatedBytesInNameOrder()
    {
        using var d = new TempDir();
        foreach (var (name, text) in DocTables()) d.Write(name, text);
        var r = ImportTable.Load(d.Path);
        Assert.Empty(r.Errors);

        var bytes = ImportTable.FileNames.Order(StringComparer.Ordinal)
            .SelectMany(n => File.ReadAllBytes(Path.Combine(d.Path, n))).ToArray();
        var expected = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
        var hash = ImportTable.ComputeHash(d.Path);
        Assert.Equal(expected, hash);

        // 표 8개가 아닌 옛 파일(PreserveNewest 출력에 남은 것)은 해시에 들어가지 않는다
        d.Write("old_renamed.json", "{}");
        Assert.Equal(hash, ImportTable.ComputeHash(d.Path));

        d.Write("exe.json", DocTables()["exe.json"].Replace("Gold.png", "Gold2.png"));
        Assert.NotEqual(hash, ImportTable.ComputeHash(d.Path));
    }

    [Fact]
    public void DuplicateKeys_AreErrors_NotLastValueWins()
    {
        var files = DocTables();
        files["icons.json"] = files["icons.json"].Replace("\"size\": 48,", "\"size\": 48, \"size\": 32,");
        var r = ImportTable.Read(files);
        Assert.Null(r.Table);
        Assert.Contains(r.Errors, e => e.File == "icons.json" && e.JsonPath == "$.size" && e.Message.Contains("두 번"));
    }

    [Fact]
    public void AllNames_AddsExeAndMapArt()
    {
        var t = ReadOk(DocTables());
        var grid = ArenaLayout.Expand(TestDefs.Map(10));
        var all = t.AllNames([grid]);
        Assert.Contains("TITLE.png", all);
        Assert.Contains("d1/maps/test_map/arena.json", all);
        Assert.Contains("d1/maps/test_map/columns.png", all);
        Assert.Contains("d1/maps/test_map/columns.json", all);
        Assert.Contains("d1/maps/test_map/minimap.png", all);
        Assert.Contains("d1/maps/test_map/floor_0_0.png", all);
        Assert.Contains("d1/monsters/zombie@eth.png", all);

        var map = TestDefs.Map(10) with { Palette = "l1_1" };
        var tsMismatch = map with { Tileset = DefRef<TilesetDef>.To(TestDefs.Tileset("l1")) };
        Assert.Empty(t.CheckMaps([tsMismatch]));
        Assert.Single(t.CheckMaps([map]));   // test_tiles 가 tilesets.json 에 없음
    }
}
