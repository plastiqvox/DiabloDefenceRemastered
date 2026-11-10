using System.Collections.Concurrent;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Pipeline;
using DiabloAssetImporter.Tests.TestKit;
using DiabloAssetImporter.Upscale;

namespace DiabloAssetImporter.Tests;

/// <summary>
/// 시험용 <see cref="IUpscaler"/>: 픽셀을 4배로 늘리기만 한다(1.4 "IUpscaler 가짜"). <see cref="FailFromBatch"/> 번째 묶음부터 실패.
/// </summary>
internal sealed class FakeUpscalerInProc : IUpscaler
{
    public int Batches;
    public int Planes;
    public int? FailFromBatch { get; init; }
    public string FailReason { get; init; } = "오류 종료(코드 1)";

    public UpscaleBatchResult Run(IReadOnlyList<UpscaleInput> batch, string workDir)
    {
        var n = Interlocked.Increment(ref Batches);
        if (FailFromBatch is { } f && n >= f) return new UpscaleBatchResult(null, FailReason);
        Interlocked.Add(ref Planes, batch.Count);
        Assert.EndsWith(Path.Combine("d1", ".cache", "tmp"), workDir, StringComparison.Ordinal);   // 원본 바이트가 든 판을 저장소 밖 %TEMP% 에 흘리지 않는다
        return new UpscaleBatchResult(batch.ToDictionary(b => b.Name, b => Png.Encode(UpscaleMath.Nearest(Png.Decode(b.Png), 4))), null, "[0 가짜 장치]");
    }
}

/// <summary>설계서 10.1 <c>Pipeline</c> AI 확대(v0.8·v0.8.1): <see cref="IUpscaler"/> 가짜로 처리 ①~⑦·세 단계·되돌아감.</summary>
public class UpscaleTests
{
    /// <summary>가짜 확대 폴더(네 파일)와 그 해시를 담은 확대 표 — 실제 Table/upscale.json 은 바꾸지 않는다.</summary>
    internal static (string Bin, UpscaleTable Table) FakeBin(string dir, IReadOnlyList<string>? include = null, bool maps = true)
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rel in new[] { "realesrgan-ncnn-vulkan.exe", "vcomp140.dll", "models/realesrgan-x4plus.param", "models/realesrgan-x4plus.bin" })
        {
            var p = Path.Combine(dir, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllText(p, "가짜 " + rel);
            files[rel] = AssetManifest.Sha256OfFile(p);
        }
        var table = new UpscaleTable
        {
            Models = new Dictionary<string, UpscaleModelDef> { ["realesrgan-x4plus"] = new() { Scale = 4, Files = files } },
            OutputScale = 2, Apply = new UpscaleApplyDef { Maps = maps, MonstersMinWidth = 160 },
            Include = include ?? ["d1/monsters/zombie"],
        };
        return (dir, table);
    }

    private static ImportRequest Up(ImportRequest q, string bin, UpscaleTable t, IUpscaler up) => q with
    {
        Upscale = "realesrgan-x4plus", UpscaleBin = bin, UpscaleTableOverride = t, VulkanPresent = () => true, UpscalerFactory = _ => up,
    };

    [Fact]
    public void Done_ScaleTwo_MetaAndArena_VariantsShareRects_SecondRunCached_GpuNotInKey()
    {
        using var f = new ImportFixture();
        var (bin, t) = FakeBin(Path.Combine(f.Tmp, "bin"));
        var up = new FakeUpscalerInProc();
        var r = f.Run(q => Up(q, bin, t, up));
        Assert.True(r.ExitCode == 0, string.Join("\n", r.Report));
        var m = r.Manifest!;
        Assert.Equal(UpscaleStatus.Done, m.Upscale.Status);
        Assert.Equal(2, m.Upscale.ArtScale);
        Assert.Equal("default", m.Upscale.Gpu);
        Assert.Equal(t.Models["realesrgan-x4plus"].Files, m.Upscale.Files);
        Assert.True(m.TryGetFile("d1/monsters/zombie@bluered.png", out var z) && z.Scale == 2);
        Assert.True(m.TryGetFile("d1/monsters/skelaxe.png", out var s) && s.Scale == 1);   // 범위 밖
        Assert.True(m.TryGetFile("d1/hero/slt.png", out var h) && h.Scale == 1);
        Assert.True(m.TryGetFile("d1/maps/cathedral_arena/minimap.png", out var mm) && mm.Scale == 1);
        var meta = SpriteMeta.Read(Path.Combine(f.Out, "d1/monsters/zombie.json"));
        Assert.Equal(2, meta.Scale);
        Assert.Empty(meta.Validate());
        var plain = SpriteMeta.Read(Path.Combine(f.Out, "d1/monsters/skelaxe.json"));
        Assert.Equal(1, plain.Scale);
        var art = ArenaArt.Read(Path.Combine(f.Out, "d1/maps/cathedral_arena/arena.json"));
        Assert.Equal(2, art.Scale);
        var chunk = art.Floor[0];
        Assert.Equal((1024, 1024), Png.ReadSize(File.ReadAllBytes(Path.Combine(f.Out, "d1/maps/cathedral_arena", chunk.File))));
        Assert.Equal(2, SpriteMeta.Read(Path.Combine(f.Out, "d1/maps/cathedral_arena/columns.json")).Scale);
        Assert.Contains(r.Report, l => l.StartsWith("  확대 진척 ", StringComparison.Ordinal));

        // 맵 알파 = 원본 2배 점 확대: 바닥 조각 알파가 원본 실행의 알파를 2배로 늘린 것과 같다
        using var off = new ImportFixture();
        Assert.Equal(0, off.Run().ExitCode);
        var a2 = Png.Decode(File.ReadAllBytes(Path.Combine(f.Out, "d1/maps/cathedral_arena", chunk.File)));
        var a1 = Png.Decode(File.ReadAllBytes(Path.Combine(off.Out, "d1/maps/cathedral_arena", chunk.File)));
        for (var y = 0; y < 1024; y += 7)
            for (var x = 0; x < 1024; x += 5) Assert.Equal(a1.A(x / 2, y / 2), a2.A(x, y));

        // 몬스터 알파: 0.5 에서 잘라 몸은 0 또는 255, 그림자는 그 사이(⑥ 시트에 구움)
        var page = Png.Decode(File.ReadAllBytes(Path.Combine(f.Out, "d1/monsters/zombie.png")));
        var alphas = Enumerable.Range(0, page.Width * page.Height).Select(i => page.Pixels[i * 4 + 3]).Distinct().ToHashSet();
        Assert.Contains((byte)255, alphas);
        Assert.Contains(alphas, a => a is > 0 and < 255);

        // 두 번째 실행: 출력 캐시 적중 — 외부 프로그램 안 띄움, GPU 를 바꿔도 적중
        var before = f.ManifestText();
        var up2 = new FakeUpscalerInProc();
        var r2 = f.Run(q => Up(q, bin, t, up2) with { UpscaleGpu = 1 });
        Assert.Equal(0, r2.ExitCode);
        Assert.Equal(0, up2.Batches);
        Assert.Equal(before, f.ManifestText());
    }

    [Fact]
    public void Jobs1_EqualsJobs8_WithUpscale()
    {
        using var f = new ImportFixture();
        var (bin, t) = FakeBin(Path.Combine(f.Tmp, "bin"));
        Assert.Equal(0, f.Run(q => Up(q, bin, t, new FakeUpscalerInProc()) with { Jobs = 1 }, Path.Combine(f.Tmp, "a")).ExitCode);
        Assert.Equal(0, f.Run(q => Up(q, bin, t, new FakeUpscalerInProc()) with { Jobs = 8 }, Path.Combine(f.Tmp, "b")).ExitCode);
        Assert.Equal(f.ManifestText(Path.Combine(f.Tmp, "a")), f.ManifestText(Path.Combine(f.Tmp, "b")));
    }

    public static TheoryData<string> FallbackCases => ["no-bin", "hash", "vulkan", "fail", "drop"];

    [Theory]
    [MemberData(nameof(FallbackCases))]
    public void Fallback_AllOriginal_Exit6_SameHashesAsOff(string why)
    {
        using var f = new ImportFixture();
        var offOut = Path.Combine(f.Tmp, "off");
        Assert.Equal(0, f.Run(outDir: offOut).ExitCode);
        var offManifest = AssetManifest.Read(Path.Combine(offOut, AssetFormat.ManifestPath));

        var (bin, t) = FakeBin(Path.Combine(f.Tmp, "bin"));
        var calls = 0;
        IUpscaler fake = why switch
        {
            "fail" => new FakeUpscalerInProc { FailFromBatch = 1 },
            "drop" => new DroppingUpscaler(),
            _ => new FakeUpscalerInProc(),
        };
        var r = f.Run(q =>
        {
            var u = Up(q, bin, t, fake) with { UpscalerFactory = _ => { calls++; return fake; } };
            return why switch
            {
                "no-bin" => u with { UpscaleBin = Path.Combine(f.Tmp, "empty") },
                "hash" => u with { UpscaleTableOverride = t with { Models = new Dictionary<string, UpscaleModelDef> { ["realesrgan-x4plus"] = new() { Scale = 4, Files = new Dictionary<string, string> { ["vcomp140.dll"] = new string('0', 64) } } } } },
                "vulkan" => u with { VulkanPresent = () => false },
                _ => u,
            };
        });
        Assert.True(r.ExitCode == 6, string.Join("\n", r.Report));
        var m = r.Manifest!;
        Assert.Equal(UpscaleStatus.Fallback, m.Upscale.Status);
        Assert.Equal(1, m.Upscale.ArtScale);
        Assert.Contains(r.Report, l => l.StartsWith("경고: AI 확대를 하지 못해 원본 픽셀로 만듦 — ", StringComparison.Ordinal));
        if (why is "no-bin" or "hash" or "vulkan") Assert.Equal(0, calls);   // 모르는 실행 파일·런타임 없음 → 띄우지 않는다
        if (why == "vulkan") Assert.Equal("Vulkan 런타임 없음", m.Upscale.Reason);
        if (why == "drop") Assert.Equal("결과 파일 없음", m.Upscale.Reason);
        Assert.Equal(offManifest.Files.Select(x => (x.Path, x.Sha256, x.Scale)), m.Files.Select(x => (x.Path, x.Sha256, x.Scale)));
    }

    private sealed class DroppingUpscaler : IUpscaler
    {
        public UpscaleBatchResult Run(IReadOnlyList<UpscaleInput> batch, string workDir) =>
            new(batch.Skip(1).ToDictionary(b => b.Name, b => Png.Encode(UpscaleMath.Nearest(Png.Decode(b.Png), 4))), null);
    }

    [Fact]
    public void PartialFailure_AllOriginal_CachedPlanesKept_NextRunOnlyRemaining()
    {
        using var f = new ImportFixture();
        var (bin, t) = FakeBin(Path.Combine(f.Tmp, "bin"), maps: false);   // 좀비만(판 수가 적게)
        var failing = new FakeUpscalerInProc { FailFromBatch = 3 };
        var r1 = f.Run(q => Up(q, bin, t, failing) with { BatchPixels = 200_000 });
        Assert.Equal(6, r1.ExitCode);
        Assert.True(r1.Manifest!.TryGetFile("d1/monsters/zombie.png", out var z1) && z1.Scale == 1);   // 일부만 확대된 채로 두지 않음
        var done1 = failing.Planes;
        Assert.True(done1 > 0);
        Assert.Equal(done1, Directory.GetFiles(Path.Combine(f.Out, "d1/.cache/upscaled"), "*.png").Length);

        var ok = new FakeUpscalerInProc();
        var r2 = f.Run(q => Up(q, bin, t, ok) with { BatchPixels = 200_000 });
        Assert.Equal(0, r2.ExitCode);
        Assert.True(r2.Manifest!.TryGetFile("d1/monsters/zombie.png", out var z2) && z2.Scale == 2);
        var total = r2.Report.First(l => l.StartsWith("  확대 진척", StringComparison.Ordinal));
        Assert.Contains($"중간 캐시 적중 {done1}", total);
        Assert.Equal(int.Parse(System.Text.RegularExpressions.Regex.Match(total, @"/(\d+)").Groups[1].Value) - done1, ok.Planes);
    }

    [Fact]
    public void UnknownModel_Exit2_NoUpscaleReadsNothing()
    {
        using var f = new ImportFixture();
        var r = f.Run(q => q with { Upscale = "nope" });
        Assert.Equal(2, r.ExitCode);
        Assert.False(File.Exists(Path.Combine(f.Out, AssetFormat.ManifestPath)));
        // --upscale 이 없으면 확대 프로그램 폴더를 보지도 않는다
        var r2 = f.Run(q => q with { UpscaleBin = Path.Combine(f.Tmp, "nowhere"), UpscalerFactory = _ => throw new InvalidOperationException("띄우면 안 됨"), VulkanPresent = () => throw new InvalidOperationException() });
        Assert.Equal(0, r2.ExitCode);
        Assert.Equal(UpscaleStatus.Off, r2.Manifest!.Upscale.Status);
    }

    [Fact]
    public void Math_FillRadius_Pad_Box2_ShadowSmooth()
    {
        var img = new RgbaImage(4, 4);
        img.Set(1, 1, 100, 50, 25, 255);
        var plane = UpscaleMath.ColorPlane(img);
        Assert.Equal((4 + 32, 4 + 32), (plane.Width, plane.Height));
        // 여백 바깥 끝까지 번진 색(검정 테 없음 — 16px 이상)
        Assert.Equal(new byte[] { 100, 50, 25, 255 }, plane.Pixels[0..4]);
        var up = UpscaleMath.Nearest(plane, 4);
        var back = UpscaleMath.CropBox2(up, 4, 4, UpscaleMath.Pad);
        Assert.Equal((8, 8), (back.Width, back.Height));
        var mask = new byte[16];
        mask[5] = 255;
        var sh = UpscaleMath.SmoothShadow2x(mask, 4, 4);
        Assert.Equal(64, sh.Length);
        Assert.True(sh[3 * 8 + 3] > 0);
        Assert.Equal(sh, UpscaleMath.SmoothShadow2x(mask, 4, 4));
    }

    [Fact]
    public void Batches_SplitByPixels_InJobPlaneOrder()
    {
        RgbaImage I(int w) => new(w, w);
        var planes = new List<UpscalePlane>
        {
            new("b", "1", I(10), "h1"), new("a", "2", I(10), "h2"), new("a", "1", I(10), "h3"),
        };
        var batches = UpscaleBatches.Split(planes, 250);
        Assert.Equal([["a/1", "a/2"], ["b/1"]], batches.Select(b => b.Select(p => p.Job + "/" + p.Name).ToList()).ToList());
    }
}

/// <summary>가짜 실행 파일(tests/FakeUpscaler)로 <see cref="ProcessUpscaler"/> 의 <c>Process</c> 경로 전체(1.4). 환경 변수를 쓰므로 한 클래스 안에서 차례로.</summary>
public class FakeExeTests
{
    private static string FakeExe()
    {
        var cfg = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ? "Release" : "Debug";
        var p = Path.Combine(Repo.Root, "tests", "FakeUpscaler", "bin", cfg, "net9.0", OperatingSystem.IsWindows() ? "FakeUpscaler.exe" : "FakeUpscaler");
        Assert.True(File.Exists(p), $"가짜 확대 실행 파일이 없다: {p}");
        return p;
    }

    private static UpscaleBatchResult RunFake(string mode, out IReadOnlyList<string> args, out int launches, int? gpu = 2, TimeSpan? noProgress = null, string? log = null)
    {
        using var t = new TempDir();
        Environment.SetEnvironmentVariable("DDR_FAKE_MODE", mode);
        Environment.SetEnvironmentVariable("DDR_FAKE_LOG", log);
        try
        {
            var up = new ProcessUpscaler(FakeExe(), "realesrgan-x4plus", Path.Combine(t.Path, "bin", "models"), gpu, noProgress ?? TimeSpan.FromSeconds(30));
            var img = new RgbaImage(8, 6);
            img.Set(1, 1, 9, 9, 9, 255);
            var outcome = UpscaleBatches.Run(up, [new UpscalePlane("j", "p1", img, "h1"), new UpscalePlane("j", "p2", img, "h2")], t.Sub("work"), 1 << 20, (_, _) => { });
            args = up.LastArguments;
            launches = up.Launches;
            return new UpscaleBatchResult(null, outcome.Failure, outcome.Device);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DDR_FAKE_MODE", null);
            Environment.SetEnvironmentVariable("DDR_FAKE_LOG", null);
        }
    }

    [Fact]
    public void ProcessPath_Arguments_Retry_NoProgress_Results_DllNotFound()
    {
        var ok = RunFake("", out var args, out var launches);
        Assert.Null(ok.Failure);
        Assert.Equal(1, launches);
        Assert.Equal("[0 Fake Vulkan GPU]  queueC=0[1]", ok.Device);
        Assert.Equal("-i", args[0]);
        Assert.Equal("-o", args[2]);
        Assert.Equal(["-n", "realesrgan-x4plus", "-m"], args.Skip(4).Take(3));
        Assert.True(Path.IsPathRooted(args[7]));   // -m 은 늘 절대 경로(해시를 확인한 모델만)
        Assert.Equal(["-s", "4", "-f", "png", "-g", "2"], args.Skip(8));

        // 오류 종료 → -t 128 로 한 번 다시
        var retry = RunFake("fail-unless-t128", out var args2, out var launches2);
        Assert.Null(retry.Failure);
        Assert.Equal(2, launches2);
        Assert.Equal(["-t", "128"], args2.TakeLast(2));
        var fail = RunFake("fail", out _, out var launches3);
        Assert.Equal("오류 종료(코드 1)", fail.Failure);
        Assert.Equal(2, launches3);

        Assert.Equal("Vulkan 런타임 없음(0xC0000135)", RunFake("dllnotfound", out _, out _).Failure);
        Assert.Equal("결과 파일 없음", RunFake("drop", out _, out _).Failure);
        Assert.Equal("결과 크기 틀림", RunFake("size", out _, out _).Failure);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Assert.StartsWith("진행 없음", RunFake("sleep", out _, out _, noProgress: TimeSpan.FromSeconds(2)).Failure);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// 가짜 실행 파일을 확대 프로그램 폴더 모양(실행 파일 이름·DLL·모델 두 파일)으로 깐다. 실행 파일 옆에 가짜의 dll·runtimeconfig 를 같이 둔다(앱 호스트가 찾음).
    /// 표는 이 폴더의 해시로 만든 것을 주입한다(실제 Table/upscale.json 은 그대로).
    /// </summary>
    private static UpscaleTable FakeBinWithExe(string bin)
    {
        var src = Path.GetDirectoryName(FakeExe())!;
        Directory.CreateDirectory(Path.Combine(bin, "models"));
        foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(bin, Path.GetFileName(f)), true);
        File.Copy(FakeExe(), Path.Combine(bin, UpscaleSetup.ExeFile), true);
        File.WriteAllText(Path.Combine(bin, "vcomp140.dll"), "가짜");
        File.WriteAllText(Path.Combine(bin, "models", "realesrgan-x4plus.param"), "가짜");
        File.WriteAllText(Path.Combine(bin, "models", "realesrgan-x4plus.bin"), "가짜");
        var files = new[] { UpscaleSetup.ExeFile, "vcomp140.dll", "models/realesrgan-x4plus.param", "models/realesrgan-x4plus.bin" }
            .ToDictionary(r => r, r => AssetManifest.Sha256OfFile(Path.Combine(bin, r.Replace('/', Path.DirectorySeparatorChar))), StringComparer.Ordinal);
        return UpscaleTests.FakeBin(Path.Combine(bin, "unused")).Table with
        {
            Models = new Dictionary<string, UpscaleModelDef> { ["realesrgan-x4plus"] = new() { Scale = 4, Files = files } },
        };
    }

    private static (int Code, string Text) Check(string bin, string outDir, UpscaleTable table, bool vulkan = true, string mode = "")
    {
        Environment.SetEnvironmentVariable("DDR_FAKE_MODE", mode);
        try
        {
            var w = new StringWriter();
            var code = Commands.UpscalerCheckCommand.Run(Cli.Options.Parse(["upscaler-check", "--upscale-bin", bin, "--out", outDir, "--upscale-gpu", "0"]), Repo.Root, w, () => vulkan, table);
            return (code, w.ToString());
        }
        finally { Environment.SetEnvironmentVariable("DDR_FAKE_MODE", null); }
    }

    /// <summary><c>upscaler-check</c>(1.4 v0.8.1): 해시·Vulkan 확인 → 16×16 무늬 한 장 변환 → 장치 줄·결과 크기. Assets 를 만들지 않고 임시 폴더를 지운다.</summary>
    [Fact]
    public void UpscalerCheck_DeviceLine_Size_NoAssets_FailuresAre6()
    {
        using var t = new TempDir();
        var bin = t.Sub("bin");
        var table = FakeBinWithExe(bin);
        var outDir = Path.Combine(t.Path, "assets");

        var (code, text) = Check(bin, outDir, table);
        Assert.True(code == 0, text);
        Assert.Contains("장치: [0 Fake Vulkan GPU]", text);
        Assert.Contains("결과 크기 64×64", text);
        Assert.False(File.Exists(Path.Combine(outDir, AssetFormat.ManifestPath)));
        Assert.False(Directory.Exists(Path.Combine(outDir, "d1", ".cache", "tmp")));

        Assert.Equal(6, Check(bin, outDir, table, vulkan: false).Code);   // 런타임 없음 → 띄우지 않음
        var (c2, t2) = Check(bin, outDir, table, mode: "fail");
        Assert.Equal(6, c2);
        Assert.Contains("변환 실패: 오류 종료", t2);
        File.AppendAllText(Path.Combine(bin, "vcomp140.dll"), "바뀜");
        var (c3, t3) = Check(bin, outDir, table);
        Assert.Equal(6, c3);
        Assert.Contains("확인 실패", t3);
    }
}
