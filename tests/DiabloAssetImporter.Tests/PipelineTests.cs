using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Pipeline;
using DiabloAssetImporter.Tests.TestKit;

namespace DiabloAssetImporter.Tests;

/// <summary>설계서 10.1 <c>Pipeline</c>: 시험이 만든 작은 MPQ + 실제 표·맵 정의로 전체 실행.</summary>
public class PipelineTests
{
    [Fact]
    public void FullRealTable_ExitZero_NamesEqualTableNames_TwiceSameManifest_CacheHits()
    {
        using var f = new ImportFixture();
        var r1 = f.Run();
        Assert.True(r1.ExitCode == 0, string.Join("\n", r1.Report));
        var m1 = f.ManifestText();

        // 만든 이름 = 13.5-10 이름 목록(표 이름 + 맵 그림 이름, 둘째 페이지 제외)
        var table = ImportTable.Load(f.TableDir).Table!;
        var arenas = MapDataReader.Read(Repo.DataDir).Maps.Select(m => ArenaLayout.Expand(m)).ToList();
        var made = r1.Manifest!.Files.Select(x => x.Path).Where(p => !p.Contains("_p", StringComparison.Ordinal)).ToHashSet();
        var expected = table.OutputNames().Concat(arenas.SelectMany(a => ArenaArt.OutputNames(a))).ToHashSet();
        Assert.Equal(expected.Order(StringComparer.Ordinal), made.Order(StringComparer.Ordinal));
        Assert.Equal(AssetFormat.Version, r1.Manifest.Format);
        Assert.Equal(ImportTable.ComputeHash(f.TableDir), r1.Manifest.TableHash);
        Assert.Equal(UpscaleStatus.Off, r1.Manifest.Upscale.Status);
        Assert.All(r1.Manifest.Files, x => Assert.Equal(["diabdat.mpq"], x.Sources));

        // 메타가 정의의 칸·방향·프레임과 대조된다(합성 프레임 수 4)
        var meta = SpriteMeta.Read(Path.Combine(f.Out, "d1/monsters/zombie.json"));
        Assert.Empty(meta.Validate());
        Assert.Equal(8, meta.Actions["walk"].Directions);

        var r2 = f.Run();
        Assert.Equal(0, r2.ExitCode);
        Assert.Equal(m1, f.ManifestText());
        var jobs = r2.Report.First(l => l.StartsWith("가져오기 끝", StringComparison.Ordinal));
        Assert.Matches(@"작업 (\d+)개\(캐시 적중 \1\)", jobs);

        // --force: 캐시를 무시해도 같은 바이트
        var r3 = f.Run(q => q with { Force = true });
        Assert.Matches(@"캐시 적중 0\)", r3.Report.First(l => l.StartsWith("가져오기 끝", StringComparison.Ordinal)));
        Assert.Equal(m1, f.ManifestText());
    }

    [Fact]
    public void Jobs1_EqualsJobs8()
    {
        using var f = new ImportFixture();
        var a = Path.Combine(f.Tmp, "a");
        var b = Path.Combine(f.Tmp, "b");
        Assert.Equal(0, f.Run(q => q with { Jobs = 1 }, a).ExitCode);
        Assert.Equal(0, f.Run(q => q with { Jobs = 8 }, b).ExitCode);
        Assert.Equal(f.ManifestText(a), f.ManifestText(b));
    }

    [Fact]
    public void MissingSources_Exit3_NoManifest_ListsAll()
    {
        using var f = new ImportFixture(files =>
        {
            files.Remove("monsters\\zombie\\zombiew.cl2");
            files.Remove("missiles\\fireba3.cl2");
            return files;
        });
        // 앞 실행이 남긴 manifest 도 지운다
        using (var good = new ImportFixture()) Assert.Equal(0, good.Run(outDir: f.Out).ExitCode);
        Assert.True(File.Exists(Path.Combine(f.Out, AssetFormat.ManifestPath)));
        var r = f.Run();
        Assert.Equal(3, r.ExitCode);
        Assert.False(File.Exists(Path.Combine(f.Out, AssetFormat.ManifestPath)));
        Assert.Contains(r.Report, l => l.Contains("monsters\\zombie\\zombiew.cl2", StringComparison.Ordinal));
        Assert.Contains(r.Report, l => l.Contains("missiles\\fireba3.cl2", StringComparison.Ordinal));
    }

    [Fact]
    public void ExtraSourceFiles_Ok_OutsideD1Untouched_StaleD1Removed_SourceFileUnchanged()
    {
        using var f = new ImportFixture(files => { files["junk\\a.bin"] = [1, 2, 3]; return files; });
        File.WriteAllText(Path.Combine(f.Out, "TITLE.png"), "exe");
        Directory.CreateDirectory(Path.Combine(f.Out, "d1", "old"));
        File.WriteAllText(Path.Combine(f.Out, "d1", "old", "stale.png"), "x");
        var before = (File.GetLastWriteTimeUtc(f.Diabdat), File.ReadAllBytes(f.Diabdat));
        Assert.Equal(0, f.Run().ExitCode);
        Assert.Equal("exe", File.ReadAllText(Path.Combine(f.Out, "TITLE.png")));
        Assert.False(File.Exists(Path.Combine(f.Out, "d1", "old", "stale.png")));
        Assert.Equal(before.Item1, File.GetLastWriteTimeUtc(f.Diabdat));
        Assert.Equal(before.Item2, File.ReadAllBytes(f.Diabdat));
        Assert.Equal([Path.GetFileName(f.Diabdat)], Directory.GetFiles(f.SourceDir).Select(Path.GetFileName));   // 원본 폴더에 캐시·로그 없음
    }

    [Fact]
    public void PropMismatch_Exit4()
    {
        var entryDir = Repo.Table().Tilesets.Tilesets[0];
        using var f = new ImportFixture(files =>
        {
            var sol = files[entryDir.SourcePath(entryDir.Sol)];
            sol[(15 - 1) * 4] ^= 2;   // 합성 타일셋: 메가 15 하위 0 = 하위 타일 56 — 시야 비트만 바꿈
            return files;
        });
        var r = f.Run();
        Assert.Equal(4, r.ExitCode);
        Assert.Contains(r.Report, l => l.Contains("메가타일 15 하위 칸 0", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(f.Out, AssetFormat.ManifestPath)));
    }

    [Fact]
    public void WidthMismatch_Exit4()
    {
        using var f = new ImportFixture(files => { files["plrgfx\\sorceror\\slt\\sltfm.cl2"] = SynthSources.UnitCl2(96, 4, 8); return files; });
        var r = f.Run();
        Assert.Equal(4, r.ExitCode);
        Assert.Contains(r.Report, l => l.Contains("hero/slt cast_fire: 파일 폭 96, 표 128", StringComparison.Ordinal));
    }

    [Fact]
    public void OutputPathCheck_RejectsTrackedFolder_AllowsAssets_RootFromOtherCwd()
    {
        Assert.NotNull(RepoPaths.CheckOutput(Path.Combine(Repo.Root, "src", "DiabloDefenceRemastered", "Data")));
        Assert.Null(RepoPaths.CheckOutput(RepoPaths.AssetsDir(Repo.Root)));
        Assert.Null(RepoPaths.CheckOutput(Path.Combine(Path.GetTempPath(), "ddr-out-" + Guid.NewGuid().ToString("N"))));
        // 다른 현재 폴더(저장소 밖)에서도 도구 자리에서 뿌리를 찾아 저장소 기준 --out
        var root = Commands.ImportCommand.FindRoot(Path.GetTempPath());
        Assert.Equal(Repo.Root, root);
        Assert.Equal(RepoPaths.AssetsDir(Repo.Root), Commands.ImportCommand.ResolveOut(Options.Parse(["import"]), root));
        // 무시되지 않는 --out 은 종료 코드 2(아무것도 안 만듦)
        var w = new StringWriter();
        Assert.Throws<UsageException>(() => Program.Run(["import", "--diabdat", "x.mpq", "--out", Path.Combine(Repo.Root, "docs")], Repo.Root, w));
    }

    [Fact]
    public void DiabdatMissing_Exit2()
    {
        using var f = new ImportFixture();
        var r = f.Run(q => q with { Diabdat = Path.Combine(f.Tmp, "none.mpq") });
        Assert.Equal(2, r.ExitCode);
        Assert.Contains(r.Report, l => l.Contains("--diabdat", StringComparison.Ordinal));
    }
}
