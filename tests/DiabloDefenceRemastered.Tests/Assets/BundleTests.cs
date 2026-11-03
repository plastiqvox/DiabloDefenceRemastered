using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Tests.Assets;

/// <summary>
/// 헬파이어 묶음(설계서 1.4·6.17, 사용자 결정 H, WP1d): 가져오기 표 <c>bundle</c>·<c>prefer</c>, <c>OutputBundles(arenas)</c>,
/// manifest <c>skipped_bundles</c>·파일 <c>bundle</c>·<c>sources</c>, <see cref="ContentAvailability"/>.
/// </summary>
public class BundleTests
{
    /// <summary>문서 예 표에 묶음 항목을 더한 것: 몬스터 hellfire, 수도사 hfmonk, 목소리 prefer hfvoice, 음악 hfmusic, 타일셋 l5 hellfire.</summary>
    private static Dictionary<string, string> BundleTables()
    {
        var files = ImportTableTests.DocTables();
        files["monsters.json"] = files["monsters.json"].Replace("""
              "monsters": [
            """, """
              "monsters": [
                { "out": "d1/monsters/stinger", "cl2": "monsters\\stinger\\sting{a}.cl2", "width": 128, "bundle": "hellfire",
                  "actions": { "walk": "w" } },
            """);
        files["hero.json"] = files["hero.json"].Replace("""
              "hero": [
            """, """
              "hero": [
                { "out": "d1/hero/mlt", "cl2": "plrgfx\\monk\\mlt\\mlt{a}.cl2", "width": 96, "bundle": "hfmonk",
                  "actions": { "idle": "as" } },
            """);
        files["sounds.json"] = """
            { "sounds": [
                { "path": "sfx\\misc\\cbolt.wav" },
                { "path": "sfx\\monk\\monk69.wav", "bundle": "hfmonk", "prefer": ["hfvoice"] } ] }
            """;
        files["music.json"] = """
            { "music": [ { "path": "music\\dintro.wav" }, { "path": "music\\dlvle.wav", "bundle": "hfmusic" } ] }
            """;
        // 문서 예는 '{ "tilesets": [' 한 줄 — 그 뒤에 l5 항목을 끼운다
        files["tilesets.json"] = files["tilesets.json"].Replace("\"tilesets\": [", """
            "tilesets": [
                { "id": "l5", "dir": "nlevels\\l5data", "cel": "l5.cel", "min": "l5.min", "til": "l5.til", "sol": "l5.sol",
                  "pieces_per_subtile": 10, "palettes": { "l5base": "l5base.pal" }, "bundle": "hellfire", "audit_dun": ["naktest.dun"] },
            """);
        return files;
    }

    private static ImportTable ReadOk(Dictionary<string, string> files)
    {
        var r = ImportTable.Read(files);
        Assert.Empty(r.Errors);
        return r.Table!;
    }

    [Fact]
    public void Table_BundleAndPrefer_ReadStrictly()
    {
        var t = ReadOk(BundleTables());
        Assert.Equal(ContentBundle.Hellfire, t.Monsters.Monsters[0].Bundle);
        Assert.Null(t.Monsters.Monsters[1].Bundle);
        Assert.Equal(ContentBundle.Hfmonk, t.Hero.Hero[0].Bundle);
        Assert.Equal([ContentBundle.Hfvoice], t.Sounds.Sounds[1].Prefer);
        Assert.Equal(ContentBundle.Hfmusic, t.Music.Music[1].Bundle);
        Assert.Equal(ContentBundle.Hellfire, t.Tilesets.Tilesets[0].Bundle);
        Assert.Equal(["naktest.dun"], t.Tilesets.Tilesets[0].AuditDun);
        Assert.Null(t.Tilesets.Tilesets[1].Bundle);
    }

    [Fact]
    public void Table_OldOptional_And_PreferOutsideSounds_AreUnknownProperties()
    {
        var files = ImportTableTests.DocTables();
        files["monsters.json"] = files["monsters.json"].Replace("\"width\": 128,", "\"width\": 128, \"optional\": true,");
        var r = ImportTable.Read(files);
        Assert.Null(r.Table);
        Assert.Contains(r.Errors, e => e.File == "monsters.json" && e.Message.Contains("optional"));

        files = ImportTableTests.DocTables();
        files["monsters.json"] = files["monsters.json"].Replace("\"width\": 128,", "\"width\": 128, \"prefer\": [\"hellfire\"],");
        Assert.Contains(ImportTable.Read(files).Errors, e => e.File == "monsters.json" && e.Message.Contains("prefer"));

        files = ImportTableTests.DocTables();
        files["monsters.json"] = files["monsters.json"].Replace("\"width\": 128,", "\"width\": 128, \"bundle\": \"hellfir\",");
        Assert.Contains(ImportTable.Read(files).Errors, e => e.File == "monsters.json");
    }

    [Fact]
    public void Table_HfvoiceAsBundle_AndBadPrefer_AreErrors()
    {
        var files = BundleTables();
        files["sounds.json"] = """
            { "sounds": [
                { "path": "sfx\\monk\\monk69.wav", "bundle": "hfvoice" },
                { "path": "sfx\\monk\\monk70.wav", "bundle": "hfmonk", "prefer": ["hfvoice", "hfvoice"] },
                { "path": "sfx\\monk\\monk71.wav", "bundle": "hfmonk", "prefer": ["hfmonk"] } ] }
            """;
        files["monsters.json"] = files["monsters.json"].Replace("\"bundle\": \"hellfire\"", "\"bundle\": \"hfvoice\"");
        var r = ImportTable.Read(files);
        Assert.NotNull(r.Table);
        bool Has(string file, string path) => r.Errors.Any(e => e.File == file && e.JsonPath == path);
        Assert.True(Has("sounds.json", "$.sounds[0].bundle"));
        Assert.True(Has("sounds.json", "$.sounds[1].prefer[1]"));   // 두 번
        Assert.True(Has("sounds.json", "$.sounds[2].prefer[0]"));   // 자기 묶음
        Assert.True(Has("monsters.json", "$.monsters[0].bundle"));
    }

    [Fact]
    public void OutputBundles_EntryBundles_SoundIndexDefault_MapArtFromTilesetEntry()
    {
        var t = ReadOk(BundleTables());
        var l5Map = TestDefs.Map(10, id: "test_l5", tileset: TestDefs.Tileset("l5"));
        var baseMap = TestDefs.Map(10);   // test_tiles 는 표에 없음 → 기본
        var grids = new[] { ArenaLayout.Expand(l5Map), ArenaLayout.Expand(baseMap) };
        var b = t.OutputBundles(grids);

        Assert.Equal(ContentBundle.Hellfire, b["d1/monsters/stinger.png"]);
        Assert.Equal(ContentBundle.Hellfire, b[SpriteMeta.MetaFile("d1/monsters/stinger.png")]);
        Assert.Null(b["d1/monsters/zombie.png"]);
        Assert.Equal(ContentBundle.Hfmonk, b["d1/hero/mlt.png"]);
        Assert.Null(b["d1/hero/slt.png"]);
        Assert.Equal(ContentBundle.Hfmonk, b[AssetFormat.SoundOutput("sfx\\monk\\monk69.wav")]);
        Assert.Equal(ContentBundle.Hfmusic, b[AssetFormat.SoundOutput("music\\dlvle.wav")]);
        Assert.Null(b[AssetFormat.SoundIndexPath]);

        // v0.6.1: 맵 그림 이름은 그 맵 타일셋의 표 항목 bundle 로
        foreach (var name in ArenaArt.OutputNames(grids[0]))
            Assert.Equal(ContentBundle.Hellfire, b[name]);
        Assert.Equal(ContentBundle.Hellfire, b["d1/maps/test_l5/arena.json"]);
        Assert.Equal(ContentBundle.Hellfire, b["d1/maps/test_l5/minimap.png"]);
        Assert.Equal(ContentBundle.Hellfire, b["d1/maps/test_l5/columns.png"]);
        Assert.Contains(b, p => p.Key.StartsWith("d1/maps/test_l5/floor_", StringComparison.Ordinal) && p.Value == ContentBundle.Hellfire);
        Assert.Null(b["d1/maps/test_map/arena.json"]);

        // 이름 집합은 OutputNames + 맵 그림과 같다
        Assert.Equal(t.AllNames(grids).Except(t.ExeNames).Order(StringComparer.Ordinal), b.Keys.Order(StringComparer.Ordinal));
    }

    // ── manifest(형식 판 2) ───────────────────────────────────────

    private const string ManifestJson = """
        { "format": 2, "tool": "abc", "runtime": "9.0.10",
          "sources": [ { "name": "diabdat.mpq", "size": 517000000, "sha256": "00" },
                       { "name": "hellfire.mpq", "size": 1000, "sha256": "01", "bundle": "hellfire" } ],
          "skipped_bundles": [ { "bundle": "hfmonk", "mpq": "hfmonk.mpq", "reason": "없음" },
                               { "bundle": "hfmusic", "mpq": "hfmusic.mpq", "reason": "--skip-bundles" },
                               { "bundle": "hfvoice", "mpq": "hfvoice.mpq", "reason": "열 수 없음" } ],
          "table_hash": "ab",
          "files": [ { "path": "d1/monsters/stinger.png", "size": 1, "sha256": "11", "bundle": "hellfire", "sources": ["diabdat.mpq", "hellfire.mpq"] },
                     { "path": "d1/monsters/zombie.png", "size": 2, "sha256": "22", "sources": ["diabdat.mpq"] } ] }
        """;

    [Fact]
    public void Manifest_SkippedBundles_FileBundleAndSources_Read()
    {
        var m = AssetJson.Deserialize<AssetManifest>(ManifestJson);
        Assert.Null(m.Sources[0].Bundle);
        Assert.Equal(ContentBundle.Hellfire, m.Sources[1].Bundle);
        Assert.Equal(3, m.SkippedBundles.Count);
        Assert.Equal(new ManifestSkippedBundle(ContentBundle.Hfmonk, "hfmonk.mpq", ManifestSkippedBundle.ReasonMissing), m.SkippedBundles[0]);
        Assert.False(m.SkippedBundles[0].Unreadable);
        Assert.False(m.SkippedBundles[1].Unreadable);
        Assert.True(m.SkippedBundles[2].Unreadable);
        Assert.True(new ManifestSkippedBundle(ContentBundle.Hellfire, "hellfire.mpq", "판 다름: nlevels\\l5data\\l5.sol").Unreadable);
        Assert.True(m.TryGetFile("d1/monsters/stinger.png", out var f));
        Assert.Equal(ContentBundle.Hellfire, f.Bundle);
        Assert.Equal(["diabdat.mpq", "hellfire.mpq"], f.Sources);

        // 쓰기: 계산 속성(unreadable)·없는 bundle 은 쓰지 않는다, 왕복 같은 바이트
        var bytes = AssetJson.Serialize(m);
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain("unreadable", text);
        Assert.DoesNotContain("\"bundle\": null", text);
        Assert.Equal(bytes, AssetJson.Serialize(AssetJson.Deserialize<AssetManifest>(text)));
        // 모르는 이유 키 등 모르는 속성은 오류
        Assert.Throws<System.Text.Json.JsonException>(() => AssetJson.Deserialize<AssetManifest>(ManifestJson.Replace("\"reason\": \"없음\"", "\"why\": \"없음\"")));
    }

    // ── ContentAvailability(6.17) ────────────────────────────────

    private static GameDefs BundleDefs()
    {
        var defs = TestDefs.Basic(8);
        var map = defs.Maps[0];
        var baseStage = defs.Stages[0];
        var crypt = TestDefs.Stage(map, id: "stage_crypt") with { Requires = [ContentBundle.Hellfire] };
        var monk = TestDefs.HeroClass(defs.SpriteSets[0], defs.Weapons[0], id: "monk", requires: [ContentBundle.Hfmonk]);
        return new GameDefs
        {
            Rules = defs.Rules,
            Sprites = defs.Sprites, SpriteSets = defs.SpriteSets, Tilesets = defs.Tilesets, Maps = defs.Maps,
            Enemies = defs.Enemies, Weapons = defs.Weapons, Passives = defs.Passives,
            Stages = new DefTable<StageDef>([baseStage, crypt]),
            HeroClasses = new DefTable<HeroClassDef>([defs.HeroClasses[0], monk]),
        };
    }

    private static AssetManifest Skipping(params ManifestSkippedBundle[] skipped) => new() { Format = AssetFormat.Version, SkippedBundles = skipped };

    [Fact]
    public void Availability_EmptySkipped_HasAll()
    {
        var a = ContentAvailability.From(Skipping());
        foreach (var b in ContentBundles.All) Assert.True(a.Has(b));
        Assert.False(a.Disabled(BundleDefs()).Any);
        Assert.Null(a.Skipped(ContentBundle.Hellfire));
        Assert.All(ContentBundles.All, b => Assert.True(ContentAvailability.All.Has(b)));
        Assert.Null(ContentAvailability.All.Skipped(ContentBundle.Hfmonk));
    }

    [Fact]
    public void Availability_HellfireSkipped_OnlyHellfireStageOff()
    {
        var defs = BundleDefs();
        var a = ContentAvailability.From(Skipping(new ManifestSkippedBundle(ContentBundle.Hellfire, "hellfire.mpq", ManifestSkippedBundle.ReasonMissing)));
        Assert.False(a.Has(ContentBundle.Hellfire));
        Assert.True(a.Has(ContentBundle.Hfmonk));
        Assert.Empty(a.MissingFor(defs.Stages[0]));
        Assert.Equal([ContentBundle.Hellfire], a.MissingFor(defs.Stages[1]));
        Assert.Empty(a.MissingFor(defs.HeroClasses[1]));
        Assert.Equal([ContentBundle.Hellfire], a.Missing([ContentBundle.Hfmusic, ContentBundle.Hellfire, ContentBundle.Hellfire]));

        var off = a.Disabled(defs);
        Assert.True(off.Any);
        Assert.Equal(["stage_crypt"], off.Stages.Select(s => s.Id));
        Assert.Empty(off.Heroes);
        Assert.Equal([ContentBundle.Hellfire], off.Bundles);
        Assert.Equal(ManifestSkippedBundle.ReasonMissing, a.Skipped(ContentBundle.Hellfire)!.Reason);
        Assert.Null(a.Skipped(ContentBundle.Hfmonk));
    }

    [Fact]
    public void Availability_HfmonkUnreadable_DisablesMonk_HfmusicOnly_IsNotAny()
    {
        var defs = BundleDefs();
        var monk = ContentAvailability.From(Skipping(new ManifestSkippedBundle(ContentBundle.Hfmonk, "hfmonk.mpq", ManifestSkippedBundle.ReasonUnreadable)));
        var off = monk.Disabled(defs);
        Assert.Equal(["monk"], off.Heroes.Select(h => h.Id));
        Assert.Empty(off.Stages);
        Assert.True(monk.Skipped(ContentBundle.Hfmonk)!.Unreadable);

        // 음악은 꺼지지 않고 fallback 으로 바뀐다 → hfmusic 만 없으면 꺼진 것 없음(타이틀 한 줄 없음)
        var music = ContentAvailability.From(Skipping(new ManifestSkippedBundle(ContentBundle.Hfmusic, "hfmusic.mpq", ManifestSkippedBundle.ReasonSkipped)));
        Assert.False(music.Has(ContentBundle.Hfmusic));
        Assert.False(music.Disabled(defs).Any);
        Assert.Empty(music.Disabled(defs).Bundles);

        // 같은 묶음이 두 번이면 앞의 이유
        var twice = ContentAvailability.From(Skipping(
            new ManifestSkippedBundle(ContentBundle.Hellfire, "hellfire.mpq", ManifestSkippedBundle.ReasonSkipped),
            new ManifestSkippedBundle(ContentBundle.Hellfire, "hellfire.mpq", ManifestSkippedBundle.ReasonMissing)));
        Assert.Equal(ManifestSkippedBundle.ReasonSkipped, twice.Skipped(ContentBundle.Hellfire)!.Reason);
    }

    [Fact]
    public void SimSources_DoNotReferenceBundles()
    {
        // 6.17: 시뮬레이션은 묶음을 모른다(같은 씨앗·스테이지·직업이면 MPQ 유무와 무관) — Sim/ 소스 문자열 검사
        var root = FindRepoRoot();
        var sim = Path.Combine(root, "src", "DiabloDefenceRemastered.Core", "Sim");
        foreach (var file in Directory.EnumerateFiles(sim, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.False(text.Contains("ContentAvailability", StringComparison.Ordinal), $"{file} 가 ContentAvailability 를 쓴다");
            Assert.False(text.Contains("AssetManifest", StringComparison.Ordinal), $"{file} 가 AssetManifest 를 쓴다");
        }
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "DiabloDefenceRemastered.sln"))) return dir.FullName;
        throw new InvalidOperationException("저장소 뿌리(DiabloDefenceRemastered.sln)를 찾지 못했다");
    }
}
