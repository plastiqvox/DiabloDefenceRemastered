using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Tests.TestKit;

namespace DiabloAssetImporter.Tests;

/// <summary>설계서 1.4 "실행" 의 작은 명령: <c>verify</c>(manifest ↔ 파일 해시), <c>contact</c>(견본 PNG — 저장소 밖 <c>--out</c>만).</summary>
public class CommandTests
{
    private static (int Code, string Text) Run(params string[] args)
    {
        var w = new StringWriter();
        var code = Program.Run(args, Repo.Root, w);
        return (code, w.ToString());
    }

    [Fact]
    public void Verify_Same0_Tampered4_Missing3()
    {
        using var f = new ImportFixture();
        Assert.Equal(0, f.Run().ExitCode);
        var (ok, text) = Run("verify", "--out", f.Out);
        Assert.True(ok == 0, text);
        Assert.Contains("manifest 와 같음", text);

        var page = Path.Combine(f.Out, "d1", "monsters", "zombie.png");
        File.AppendAllBytes(page, [0]);
        var (bad, t2) = Run("verify", "--out", f.Out);
        Assert.Equal(4, bad);
        Assert.Contains("다름: d1/monsters/zombie.png", t2);

        File.Delete(Path.Combine(f.Out, AssetFormat.ManifestPath));
        Assert.Equal(3, Run("verify", "--out", f.Out).Code);
    }

    [Fact]
    public void Contact_WritesSamplesOutsideRepo_RejectsTrackedFolder()
    {
        using var f = new ImportFixture();
        var outDir = Path.Combine(f.Tmp, "contact");
        var (code, text) = Run("contact", "--diabdat", f.Diabdat, "--out", outDir);
        Assert.True(code == 0, text);
        foreach (var name in new[] { "zombie_walk.png", "zombie_walk@bluered.png", "fireball_16dirs.png", "sorcerer_cast_fire.png", "arena_cathedral_arena.png" })
            Assert.True(File.Exists(Path.Combine(outDir, name)), name);
        Assert.Throws<UsageException>(() => Run("contact", "--diabdat", f.Diabdat, "--out", Path.Combine(Repo.Root, "docs", "contact-wp0-test")));
        Assert.False(Directory.Exists(Path.Combine(Repo.Root, "docs", "contact-wp0-test")));
    }
}
