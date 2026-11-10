using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Tests.Assets;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Dev;

/// <summary>
/// 시작 자원 검사(1.4 끝 ①~⑤, 꺼짐·교차 확인 — WP7). 합성 자원 폴더: 정의가 가리키는 파일을 1바이트로 만들고 manifest 에 적는다(원작 바이트 없음).
/// 가져오기 표는 설계서 예 표(<see cref="ImportTableTests.DocTables"/>) — 표 해시만 쓰고, 묶음 시험은 그 표의 zombie 항목에 <c>bundle</c> 을 단다.
/// </summary>
public sealed class AssetStartupCheckTests
{
    private sealed class Setup : IDisposable
    {
        public TempDir Dir { get; } = new();
        public string Assets => Path.Combine(Dir.Path, "Assets");
        public string Table => Path.Combine(Dir.Path, "ImportTable");
        public GameDefs Defs { get; private set; } = GameDefs.Empty;
        public List<ArenaGrid> Grids { get; private set; } = [];

        public Setup(LoaderFixture f, bool zombieHellfire = false, IReadOnlyList<ManifestSkippedBundle>? skipped = null, Func<string, bool>? omit = null,
                     int format = AssetFormat.Version, string? tableHash = null)
        {
            var r = f.Load();
            Assert.DoesNotContain(r.Errors, e => e.Severity == DataSeverity.Error && !e.Message.Contains("13.5-20", StringComparison.Ordinal));
            Defs = r.Defs;
            Grids = [.. Defs.Maps.All.Select(m => ArenaLayout.Expand(m))];
            var tables = ImportTableTests.DocTables();
            if (zombieHellfire) tables["monsters.json"] = tables["monsters.json"].Replace("\"width\": 128,", "\"width\": 128, \"bundle\": \"hellfire\",", StringComparison.Ordinal);
            foreach (var (name, text) in tables) Dir.Write($"ImportTable/{name}", text);
            var files = new List<ManifestFile>();
            foreach (var file in DefAssetNames.For(Defs, Grids).Select(a => a.File).Distinct())
            {
                if (!file.StartsWith("d1/", StringComparison.Ordinal) || omit?.Invoke(file) == true) continue;
                byte[] bytes = file.EndsWith("/arena.json", StringComparison.Ordinal)
                    ? AssetJson.Serialize(new ArenaArt { Format = ArenaArt.CurrentFormat, Map = "m", LayoutHash = Grids.Single(g => file.StartsWith(g.Map.Art + "/", StringComparison.Ordinal)).LayoutHash })
                    : [1];
                Dir.Write($"Assets/{file}", "");
                File.WriteAllBytes(Path.Combine(Assets, file.Replace('/', Path.DirectorySeparatorChar)), bytes);
                files.Add(new ManifestFile { Path = file, Size = bytes.Length, Sha256 = AssetManifest.Sha256Of(bytes) });
            }
            var manifest = new AssetManifest
            {
                Format = format,
                TableHash = tableHash ?? ImportTable.ComputeHash(Table),
                SkippedBundles = skipped ?? [],
                Files = [.. files.OrderBy(x => x.Path, StringComparer.Ordinal)],
            };
            Dir.Write("Assets/" + AssetFormat.ManifestPath, "");
            File.WriteAllBytes(Path.Combine(Assets, AssetFormat.ManifestPath), AssetJson.Serialize(manifest));
        }

        public AssetCheckResult Run() => AssetStartupCheck.Run(Assets, Table, Defs, Grids);

        public void Dispose() => Dir.Dispose();
    }

    [Fact]
    public void AllPresent_IsOk_AndLogFields()
    {
        using var s = new Setup(new LoaderFixture());
        var r = s.Run();
        Assert.True(r.Ok, string.Join("\n", r.Problems));
        Assert.Equal("manifest=ok format=3 bundles=- skipped=- disabled=- upscale=off art_scale=1", r.LogFields());
    }

    [Fact]
    public void NoManifest_IsProblem()
    {
        using var dir = new TempDir();
        var r = AssetStartupCheck.Run(Path.Combine(dir.Path, "Assets"), dir.Path, GameDefs.Empty, []);
        Assert.False(r.Ok);
        Assert.Contains("자원이 없다", r.Problems[0], StringComparison.Ordinal);
        Assert.Equal("manifest=missing format=- bundles=- skipped=- disabled=- upscale=off art_scale=1", r.LogFields());
    }

    [Fact]
    public void OldFormat_And_TableChanged()
    {
        using var s = new Setup(new LoaderFixture(), format: 2, tableHash: "00");
        var r = s.Run();
        Assert.Contains(r.Problems, p => p.Contains("판 2 → 3", StringComparison.Ordinal));
        Assert.Contains(r.Problems, p => p.Contains("가져오기 표가 바뀌었다", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingFile_SizeMismatch_LayoutChanged()
    {
        using var a = new Setup(new LoaderFixture(), omit: f => f == "d1/hero/slt.png");
        Assert.Contains(a.Run().Problems, p => p.Contains("없는 파일 1개: d1/hero/slt.png", StringComparison.Ordinal));

        using var b = new Setup(new LoaderFixture());
        File.WriteAllBytes(Path.Combine(b.Assets, "d1", "hero", "slt.png"), [1, 2]);
        Assert.Contains(b.Run().Problems, p => p.Contains("크기가 다르거나", StringComparison.Ordinal));

        using var c = new Setup(new LoaderFixture());
        var art = c.Defs.Maps.Get("cathedral_arena").Art;
        var path = Path.Combine(c.Assets, (art + "/arena.json").Replace('/', Path.DirectorySeparatorChar));
        // 크기를 맞춰 ⑤ 만 걸리게: 같은 길이 해시 문자열
        var good = File.ReadAllBytes(path);
        var hash = System.Text.Encoding.UTF8.GetString(good).Split("\"layout_hash\": \"")[1].Split('"')[0];
        File.WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(good).Replace(hash, new string('0', hash.Length), StringComparison.Ordinal)));
        var problems = c.Run().Problems;
        Assert.Contains(problems, p => p.Contains("맵 배치가 바뀌었다", StringComparison.Ordinal));
        Assert.DoesNotContain(problems, p => p.Contains("크기가 다르거나", StringComparison.Ordinal));
    }

    /// <summary>합성 manifest 에 skipped_bundles: [hellfire] + requires: [hellfire] 합성 스테이지 → 꺼짐·오류 0. 기본 스테이지가 그 적을 내면 오류(교차 확인).</summary>
    [Fact]
    public void SkippedBundle_DisablesStage_ButCrossCheckCatchesBaseUse()
    {
        static LoaderFixture WithHellfireAct(bool baseUsesZed)
        {
            var f = new LoaderFixture();
            var enemies = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
            enemies.Add(LoaderFixture.Obj("""{ "id": "zed", "sprites": "set_zombie", "variant": "bluered", "requires": ["hellfire"] }"""));
            var stages = (JsonArray)f.Files["stages/stage_test.json"]["stages"]!;
            var hf = (JsonObject)f.Stage.DeepClone();
            hf["id"] = "stage_hf";
            hf["requires"] = new JsonArray("hellfire");
            hf["spawn_intervals"]![0]!["weights"] = LoaderFixture.Obj("""{ "zed": 100 }""");
            stages.Add(hf);
            if (baseUsesZed) LoaderTests.AddToStage(f, "zed");
            return f;
        }

        var skipped = new[] { new ManifestSkippedBundle(ContentBundle.Hellfire, "hellfire.mpq", ManifestSkippedBundle.ReasonMissing) };
        using (var s = new Setup(WithHellfireAct(false), zombieHellfire: true, skipped: skipped, omit: f => f.Contains("zombie", StringComparison.Ordinal)))
        {
            var r = s.Run();
            Assert.True(r.Ok, string.Join("\n", r.Problems));
            Assert.Contains("d1/monsters/zombie@bluered.png", r.DisabledFiles);
            Assert.Equal("stage_hf", Assert.Single(r.Disabled.Stages).Id);
            Assert.Contains("skipped=hellfire disabled=stage_hf", r.LogFields(), StringComparison.Ordinal);
        }
        using (var s = new Setup(WithHellfireAct(true), zombieHellfire: true, skipped: skipped, omit: f => f.Contains("zombie", StringComparison.Ordinal)))
        {
            var r = s.Run();
            Assert.Contains(r.Problems, p => p.Contains("꺼진 묶음 hellfire", StringComparison.Ordinal));
        }
        // 같은 파일이 기본 내용(표 항목에 bundle 없음)이면 오류
        using (var s = new Setup(WithHellfireAct(false), zombieHellfire: false, skipped: skipped, omit: f => f.Contains("zombie", StringComparison.Ordinal)))
        {
            var r = s.Run();
            Assert.Contains(r.Problems, p => p.Contains("없는 파일", StringComparison.Ordinal) && p.Contains("zombie", StringComparison.Ordinal));
        }
    }
}
