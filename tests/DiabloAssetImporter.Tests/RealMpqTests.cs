using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Mpq;
using DiabloAssetImporter.Pipeline;
using DiabloAssetImporter.Tests.TestKit;

namespace DiabloAssetImporter.Tests;

/// <summary>
/// 설계서 10.1 <c>RealMpq</c>(선택): 환경 변수 <c>DDR_DIABDAT</c>(diabdat.mpq 경로)가 있을 때만 돈다 — 없으면 건너뜀 표시.
/// 원본 바이트는 시험 출력에 남기지 않는다(머리 값·개수만 단언, 가져오기 출력은 임시 폴더).
/// v0.6.1: <c>DDR_HELLFIRE_DIR</c>(묶음 MPQ 폴더)가 있으면 묶음마다 대표 항목이 War3Net 으로 열리고 머리가 맞는지 — MVP 표에 묶음 항목이 없어 WP9·WP10 전까지 아무도 읽지 않는 빈틈.
/// </summary>
public class RealMpqTests
{
    private static string Diabdat()
    {
        var p = Environment.GetEnvironmentVariable("DDR_DIABDAT");
        if (string.IsNullOrWhiteSpace(p)) Assert.Skip("RealMpq 는 DDR_DIABDAT 가 있을 때만 돈다");
        Assert.True(File.Exists(p), $"DDR_DIABDAT 파일이 없다: {p}");
        return p;
    }

    /// <summary>표의 원본 항목이 모두 diabdat 에서 열리고 머리(CL2 그룹 수·WAV RIFF·SOL 크기)가 맞다.</summary>
    [Fact]
    public void TableSources_AllOpen_HeadersMatch()
    {
        var path = Diabdat();
        var table = Repo.Table();
        var data = MapDataReader.Read(Repo.DataDir);
        Assert.Empty(data.Errors);
        var arenas = data.Maps.Select(m => ArenaLayout.Expand(m)).ToList();
        var jobs = JobPlanner.Plan(table, arenas, null);
        using var mpq = MpqSource.Open(path);
        var missing = new List<string>();
        var n = 0;
        foreach (var src in jobs.Where(j => j.Bundle is null).SelectMany(j => j.Sources).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var bytes = mpq.Read(src);
            if (bytes is null) { missing.Add(src); continue; }
            n++;
            CheckHeader(src, bytes);
        }
        Assert.Empty(missing);
        Assert.True(n > 0);
    }

    /// <summary>실제 표 전체를 임시 폴더로 가져오기 — 종료 코드 0, 두 번 → manifest 같음(PKWARE implode·암호화 경로).</summary>
    [Fact]
    public void Import_TwiceSameManifest()
    {
        var path = Diabdat();
        using var t = new TempDir();
        ImportRequest Req(string outDir) => new()
        {
            Diabdat = path, OutDir = outDir, TableDir = Repo.TableDir, DataDir = Repo.DataDir, SkipBundles = new HashSet<ContentBundle>(ContentBundles.All), Log = TextWriter.Null,
        };
        var a = Importer.Run(Req(t.Sub("a")));
        var b = Importer.Run(Req(t.Sub("b")));
        Assert.Equal(0, a.ExitCode);
        Assert.Equal(0, b.ExitCode);
        Assert.Equal(File.ReadAllText(Path.Combine(t.Sub("a"), AssetFormat.ManifestPath)), File.ReadAllText(Path.Combine(t.Sub("b"), AssetFormat.ManifestPath)));
    }

    public static TheoryData<string, string> BundleSamples => new()
    {
        { "hfmonk.mpq", "plrgfx\\monk\\mlt\\mltas.cl2" },
        { "hfmonk.mpq", "sfx\\monk\\monk69.wav" },
        { "hfvoice.mpq", "sfx\\monk\\monk69.wav" },
        { "hellfire.mpq", "nlevels\\l5data\\l5.sol" },
        { "hellfire.mpq", "monsters\\nkr\\nkrn.cl2" },
        { "hfmusic.mpq", "music\\dlvle.wav" },
    };

    /// <summary>v0.6.1: 묶음마다 대표 항목이 War3Net 으로 열리고 머리가 맞다.</summary>
    [Theory]
    [MemberData(nameof(BundleSamples))]
    public void BundleSample_Opens_HeaderMatches(string mpqName, string entry)
    {
        var dir = Environment.GetEnvironmentVariable("DDR_HELLFIRE_DIR");
        if (string.IsNullOrWhiteSpace(dir)) Assert.Skip("묶음 대표 항목은 DDR_HELLFIRE_DIR 가 있을 때만 돈다");
        var file = Directory.EnumerateFiles(dir).FirstOrDefault(f => string.Equals(Path.GetFileName(f), mpqName, StringComparison.OrdinalIgnoreCase));
        Assert.True(file is not null, $"{mpqName} 이 {dir} 에 없다");
        using var mpq = MpqSource.Open(file);
        var bytes = mpq.Read(entry);
        Assert.True(bytes is not null, $"{mpqName} 에 {entry} 가 없다");
        CheckHeader(entry, bytes);
    }

    private static void CheckHeader(string src, byte[] bytes)
    {
        var ext = Path.GetExtension(src).ToLowerInvariant();
        switch (ext)
        {
            case ".cl2":
                // 그룹 묶음(몬스터·주인공)은 8그룹, 평면(마법)은 프레임 표 — 어느 쪽이든 프레임이 하나 이상 풀린다
                var frames = Cl2.LooksGrouped(bytes) ? Cl2.ReadGroups(bytes, 8).Sum(g => g.Count) : Cl2.ReadFlat(bytes).Count;
                Assert.True(frames > 0, $"{src}: 프레임 0");
                break;
            case ".wav":
                Assert.True(bytes.Length > 44 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WAVE"u8), $"{src}: RIFF 머리 아님");
                Assert.True(Wav.ToPcm16(bytes).Samples > 0, $"{src}: 표본 0");
                break;
            case ".sol":
                Assert.True(bytes.Length > 0, $"{src}: 빈 SOL");
                break;
            case ".pal":
                Assert.Equal(768, bytes.Length);
                break;
            case ".trn":
                Assert.Equal(256, bytes.Length);
                break;
            default:
                Assert.True(bytes.Length > 0, $"{src}: 빈 항목");
                break;
        }
    }
}
