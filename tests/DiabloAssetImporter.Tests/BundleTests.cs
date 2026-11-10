using DiabloAssetImporter.Tests.TestKit;

namespace DiabloAssetImporter.Tests;

/// <summary>설계서 10.1 <c>Pipeline</c> 묶음(v0.6·v0.6.1) ①~⑧·⑩: 시험 표에 묶음 항목을 넣고 묶음 MPQ 를 있게·없게·깨지게.</summary>
public class BundleTests
{
    private const string FelCl2 = "monsters\\fel\\fel{a}.cl2";
    private const string FelTrn = "monsters\\fel\\red.trn";
    private const string MonkSound = "sfx\\monk\\monk69.wav";

    /// <summary>실제 표 + 묶음 항목 둘(hellfire 몬스터 — TRN 은 diabdat, hfmonk 소리 — prefer hfvoice).</summary>
    private static string BundleTable(string dir)
    {
        Repo.CopyTable(dir);
        var mon = Path.Combine(dir, "monsters.json");
        File.WriteAllText(mon, File.ReadAllText(mon).Replace("  \"monsters\": [", """
              "monsters": [
                { "out": "d1/monsters/fel", "cl2": "monsters\\fel\\fel{a}.cl2", "width": 96, "bundle": "hellfire",
                  "actions": { "walk": "w" }, "variants": { "": null, "red": "monsters\\fel\\red.trn" } },
            """));
        File.WriteAllText(Path.Combine(dir, "sounds.json"), """
            { "sounds": [ { "path": "sfx\\monk\\monk69.wav", "bundle": "hfmonk", "prefer": ["hfvoice"] } ] }
            """);
        return dir;
    }

    private sealed class BundleFixture : IDisposable
    {
        private readonly TempDir _t = new();
        public ImportFixture F { get; }
        public BundleFixture(bool hellfire = true, bool felInHellfire = true, bool hfvoice = true, bool hfmonk = true, string hellfireName = "HELLFIRE.MPQ")
        {
            var table = BundleTable(_t.Sub("table"));
            // diabdat 에서는 묶음 항목 원본(fel CL2·monk 소리)을 뺀다 — TRN 은 diabdat 에 둔다(⑩)
            F = new ImportFixture(files =>
            {
                files.Remove("monsters\\fel\\felw.cl2");
                files.Remove(MonkSound);
                return files;
            }, table);
            if (hellfire)
                SynthMpq.Write(Path.Combine(F.SourceDir, hellfireName), felInHellfire
                    ? new Dictionary<string, byte[]> { ["monsters\\fel\\felw.cl2"] = SynthSources.UnitCl2(96, 3, 8) }
                    : new Dictionary<string, byte[]> { ["other.bin"] = [1] });
            if (hfmonk) SynthMpq.Write(Path.Combine(F.SourceDir, "hfmonk.mpq"), new Dictionary<string, byte[]> { [MonkSound] = Encoders.Wav8([1, 2, 3]) });
            if (hfvoice) SynthMpq.Write(Path.Combine(F.SourceDir, "hfvoice.mpq"), new Dictionary<string, byte[]> { [MonkSound] = Encoders.Wav8([9, 9, 9, 9, 9]) });
        }

        public void Dispose() { F.Dispose(); _t.Dispose(); }
    }

    [Fact]
    public void AllPresent_Made_SourcesSorted_PreferWins_DefaultFolderUppercase()
    {
        using var b = new BundleFixture();
        var r = b.F.Run();
        Assert.True(r.ExitCode == 0, string.Join("\n", r.Report));
        var m = r.Manifest!;
        // ⑦ --hellfire 기본 = diabdat 폴더, 대문자 이름도 찾음 → hellfire 만듦. hfmusic 은 없음
        Assert.Equal([ContentBundle.Hfmusic], m.SkippedBundles.Select(s => s.Bundle));
        Assert.Contains(r.Report, l => l.Contains("묶음 hellfire: 만듦", StringComparison.Ordinal));
        Assert.Contains(m.Sources, s => s.Name == "hellfire.mpq" && s.Bundle == ContentBundle.Hellfire && s.Sha256.Length == 64);
        // ⑩ diabdat TRN + hellfire CL2 → 두 이름 정렬
        Assert.True(m.TryGetFile("d1/monsters/fel@red.png", out var fel));
        Assert.Equal(["diabdat.mpq", "hellfire.mpq"], fel.Sources);
        Assert.Equal(ContentBundle.Hellfire, fel.Bundle);
        // ⑤ prefer: hfvoice 가 있으면 그 바이트(표본 5개)
        Assert.True(m.TryGetFile("d1/sound/sfx/monk/monk69.wav", out var monk));
        Assert.Equal(["hfvoice.mpq"], monk.Sources);
        Assert.Equal(44 + 10, monk.Size);
    }

    [Fact]
    public void PreferMissing_FallsToBundle()
    {
        using var b = new BundleFixture(hfvoice: false);
        var r = b.F.Run();
        Assert.Equal(0, r.ExitCode);
        Assert.True(r.Manifest!.TryGetFile("d1/sound/sfx/monk/monk69.wav", out var monk));
        Assert.Equal(["hfmonk.mpq"], monk.Sources);
        Assert.Equal(44 + 6, monk.Size);
    }

    [Fact]
    public void Missing_And_SkipBundles_SameBaseOutput()
    {
        using var on = new BundleFixture();
        var rOn = on.F.Run();
        using var off = new BundleFixture(hellfire: false);
        var rOff = off.F.Run();
        Assert.Equal(0, rOff.ExitCode);
        Assert.False(rOff.Manifest!.TryGetFile("d1/monsters/fel.png", out _));
        Assert.Contains(rOff.Manifest.SkippedBundles, s => s.Bundle == ContentBundle.Hellfire && s.Reason == "없음");
        var rSkip = on.F.Run(q => q with { SkipBundles = new HashSet<ContentBundle> { ContentBundle.Hellfire } }, Path.Combine(on.F.Tmp, "skip"));
        Assert.Equal(0, rSkip.ExitCode);
        Assert.Contains(rSkip.Manifest!.SkippedBundles, s => s.Bundle == ContentBundle.Hellfire && s.Reason == "--skip-bundles");
        Assert.False(rSkip.Manifest.TryGetFile("d1/monsters/fel.png", out _));
        // ⑥ 기본 항목 출력 바이트 같음
        foreach (var f in rOn.Manifest!.Files.Where(f => f.Bundle is null && !f.Path.StartsWith("d1/sound/", StringComparison.Ordinal)))
        {
            Assert.True(rSkip.Manifest.TryGetFile(f.Path, out var g), f.Path);
            Assert.Equal(f.Sha256, g.Sha256);
        }
    }

    [Fact]
    public void VersionDiffers_SkipsWholeBundle_Warning_ExplicitIs3()
    {
        using var b = new BundleFixture(felInHellfire: false);
        var r = b.F.Run();
        Assert.Equal(0, r.ExitCode);
        var s = Assert.Single(r.Manifest!.SkippedBundles, x => x.Bundle == ContentBundle.Hellfire);
        Assert.Equal("판 다름: monsters\\fel\\felw.cl2", s.Reason);
        Assert.Contains(r.Report, l => l.StartsWith("경고: 헬파이어 묶음 hellfire 을 건너뜀", StringComparison.Ordinal));
        Assert.True(r.Manifest.TryGetFile("d1/monsters/zombie.png", out _));
        var r3 = b.F.Run(q => q with { HellfireDir = b.F.SourceDir, HellfireExplicit = true });
        Assert.Equal(3, r3.ExitCode);
        Assert.Contains(r3.Report, l => l.Contains("--skip-bundles hellfire", StringComparison.Ordinal));
    }

    [Fact]
    public void BrokenMpq_Unreadable_ExplicitIs2()
    {
        using var b = new BundleFixture(hellfire: false);
        File.WriteAllBytes(Path.Combine(b.F.SourceDir, "hellfire.mpq"), [0x4D, 0x50, 0x51, 0x1A, 1, 2, 3]);
        var r = b.F.Run();
        Assert.Equal(0, r.ExitCode);
        Assert.Contains(r.Manifest!.SkippedBundles, x => x.Bundle == ContentBundle.Hellfire && x.Reason == "열 수 없음");
        Assert.Contains(r.Report, l => l.Contains("hellfire.mpq 를 열 수 없음", StringComparison.Ordinal));
        var r2 = b.F.Run(q => q with { HellfireDir = b.F.SourceDir, HellfireExplicit = true });
        Assert.Equal(2, r2.ExitCode);
    }

    [Fact]
    public void SameNameInDiabdatAndBundle_DiabdatWins()
    {
        using var t = new TempDir();
        var table = BundleTable(t.Sub("table"));
        using var f = new ImportFixture(files => files, table);   // diabdat 에 fel CL2 가 남아 있음
        SynthMpq.Write(Path.Combine(f.SourceDir, "hellfire.mpq"), new Dictionary<string, byte[]> { ["monsters\\fel\\felw.cl2"] = SynthSources.UnitCl2(96, 1, 8) });
        var r = f.Run();
        Assert.Equal(0, r.ExitCode);
        Assert.True(r.Manifest!.TryGetFile("d1/monsters/fel.png", out var fel));
        Assert.Equal(["diabdat.mpq"], fel.Sources);
    }
}
