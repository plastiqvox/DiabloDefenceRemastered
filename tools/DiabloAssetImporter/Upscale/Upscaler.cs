using System.Diagnostics;
using System.Runtime.InteropServices;
using DiabloAssetImporter.Imaging;

namespace DiabloAssetImporter.Upscale;

/// <summary>확대 묶음 하나의 입력 판(이름 = 파일 이름 줄기, PNG 바이트).</summary>
internal sealed record UpscaleInput(string Name, byte[] Png, int Width, int Height);

/// <summary>묶음 결과: 성공이면 이름 → 4배 PNG, 실패면 이유. <see cref="Device"/> = 표준 오류의 장치 줄(있으면).</summary>
internal sealed record UpscaleBatchResult(IReadOnlyDictionary<string, byte[]>? Outputs, string? Failure, string? Device = null);

/// <summary>외부 확대 프로그램 한 번(묶음 하나). 시험은 가짜를 넣는다(1.4 "시험").</summary>
internal interface IUpscaler
{
    UpscaleBatchResult Run(IReadOnlyList<UpscaleInput> batch, string workDir);
}

/// <summary>
/// 확대 준비(1.4 "해시 확인 단계"·"Vulkan 런타임 확인"): 표의 그 모델 <c>files</c> 마다 <c>--upscale-bin</c> 기준 SHA-256 대조, <c>vulkan-1.dll</c> 확인.
/// 하나라도 틀리면 실행 파일을 띄우지 않는다(이유를 돌려 되돌아감).
/// </summary>
internal static class UpscaleSetup
{
    public const string ToolName = "realesrgan-ncnn-vulkan v0.2.5.0";
    public const string ExeFile = "realesrgan-ncnn-vulkan.exe";

    public static string DefaultBin() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiabloDefenceRemastered", "realesrgan-ncnn-vulkan");

    /// <summary>문제 이유(null = 통과). <paramref name="vulkanPresent"/> 는 시험이 꾸민다.</summary>
    public static string? Check(UpscaleModelDef model, string binDir, Func<bool> vulkanPresent)
    {
        if (!Directory.Exists(binDir)) return $"확대 프로그램 폴더 없음({binDir})";
        foreach (var (rel, sha) in model.Files.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var path = Path.Combine(binDir, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return $"확대 프로그램 파일 없음({rel})";
            var actual = AssetManifest.Sha256OfFile(path);
            if (!string.Equals(actual, sha, StringComparison.OrdinalIgnoreCase)) return $"확대 프로그램 해시 다름({rel})";
        }
        if (!vulkanPresent()) return "Vulkan 런타임 없음";
        return null;
    }

    public static bool SystemVulkanPresent() =>
        File.Exists(Path.Combine(Environment.SystemDirectory, "vulkan-1.dll"));
}

/// <summary>
/// 실제 확대 프로그램(1.4 "띄우는 법"): 셸 없이 <c>Process</c> 로 직접, <c>ArgumentList</c>, 창 없음, 표준 입력 닫음, 작업 폴더 = 임시 폴더.
/// 인자 <c>-i -o -n -m -s 4 -f png [-g n]</c>, 진행 기준 시간 제한(결과 파일 수가 <see cref="NoProgress"/> 동안 늘지 않으면 죽임), 오류 종료면 <c>-t 128</c> 로 한 번 다시.
/// </summary>
internal sealed class ProcessUpscaler(string exePath, string modelName, string modelsDir, int? gpu, TimeSpan noProgress) : IUpscaler
{
    /// <summary>진행 없음 첫 값(2분, 1.4 — WP0 실측 뒤 고침).</summary>
    public static readonly TimeSpan DefaultNoProgress = TimeSpan.FromMinutes(2);
    public const int DllNotFound = unchecked((int)0xC0000135);

    public TimeSpan NoProgress { get; } = noProgress;
    /// <summary>마지막으로 띄운 인자(시험이 본다).</summary>
    public IReadOnlyList<string> LastArguments { get; private set; } = [];
    public int Launches { get; private set; }

    public UpscaleBatchResult Run(IReadOnlyList<UpscaleInput> batch, string workDir)
    {
        var first = RunOnce(batch, workDir, tile128: false);
        if (first.Failure is { } f && f.StartsWith("오류 종료", StringComparison.Ordinal))
            return RunOnce(batch, workDir, tile128: true);   // 메모리 부족 같은 오류 종료는 작은 타일로 한 번 다시
        return first;
    }

    private UpscaleBatchResult RunOnce(IReadOnlyList<UpscaleInput> batch, string workDir, bool tile128)
    {
        var inDir = Path.Combine(workDir, "in");
        var outDir = Path.Combine(workDir, "out");
        if (Directory.Exists(inDir)) Directory.Delete(inDir, true);
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(inDir);
        Directory.CreateDirectory(outDir);
        foreach (var b in batch) File.WriteAllBytes(Path.Combine(inDir, b.Name + ".png"), b.Png);

        var psi = new ProcessStartInfo(exePath)
        {
            UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = workDir,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        string[] args = ["-i", inDir, "-o", outDir, "-n", modelName, "-m", modelsDir, "-s", UpscaleMath.ModelScale.ToString(System.Globalization.CultureInfo.InvariantCulture), "-f", "png"];
        foreach (var a in args) psi.ArgumentList.Add(a);
        if (gpu is { } g) { psi.ArgumentList.Add("-g"); psi.ArgumentList.Add(g.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
        if (tile128) { psi.ArgumentList.Add("-t"); psi.ArgumentList.Add("128"); }
        LastArguments = [.. psi.ArgumentList];

        ErrorMode.SuppressDialogs();
        Process p;
        try
        {
            p = Process.Start(psi) ?? throw new InvalidOperationException("띄울 수 없음");
            Launches++;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new UpscaleBatchResult(null, $"띄울 수 없음({ex.Message})");
        }
        using (p)
        {
            p.StandardInput.Close();
            var err = new System.Text.StringBuilder();
            p.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (err) err.AppendLine(e.Data); };
            p.OutputDataReceived += (_, _) => { };
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            var last = Stopwatch.StartNew();
            var seen = 0;
            while (!p.WaitForExit(200))
            {
                var n = Directory.GetFiles(outDir).Length;
                if (n > seen) { seen = n; last.Restart(); }
                if (last.Elapsed > NoProgress)
                {
                    try { p.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                    p.WaitForExit();
                    return new UpscaleBatchResult(null, $"진행 없음 {NoProgress.TotalSeconds:0}초");
                }
            }
            p.WaitForExit();
            string device;
            lock (err) device = DeviceLine(err.ToString());
            if (p.ExitCode == DllNotFound) return new UpscaleBatchResult(null, "Vulkan 런타임 없음(0xC0000135)", device);
            if (p.ExitCode != 0) return new UpscaleBatchResult(null, $"오류 종료(코드 {p.ExitCode})", device);
            var outputs = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var b in batch)
            {
                var path = Path.Combine(outDir, b.Name + ".png");
                if (File.Exists(path)) outputs[b.Name] = File.ReadAllBytes(path);
            }
            return new UpscaleBatchResult(outputs, null, device);
        }
    }

    /// <summary>표준 오류에서 장치 줄(<c>[0 NVIDIA …]</c> 꼴)을 고른다.</summary>
    public static string DeviceLine(string stderr)
    {
        foreach (var line in stderr.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith('[') && t.Contains(']')) return t;
        }
        return "";
    }
}

/// <summary>오류 창 막기(1.4 — 자식이 물려받는 오류 모드). Windows 가 아니면 아무것도 안 한다.</summary>
internal static class ErrorMode
{
    private const uint SemFailCriticalErrors = 0x0001;
    private const uint SemNoOpenFileErrorBox = 0x8000;

    public static void SuppressDialogs()
    {
        if (!OperatingSystem.IsWindows()) return;
        SetErrorMode(SemFailCriticalErrors | SemNoOpenFileErrorBox);
    }

    [DllImport("kernel32.dll")]
    private static extern uint SetErrorMode(uint mode);
}

/// <summary>
/// 확대 실행(1.4 "세 단계" 의 (2)): 판을 (작업 이름 서수, 판 이름) 순으로 놓고 입력 픽셀 합이 <see cref="BatchPixels"/> 를 넘지 않게 끊어 묶음마다 한 번.
/// 결과 판정(묶음마다): 실패 이유, 판마다 결과 PNG 없음, 결과 크기 ≠ 입력 × 4. 성공한 묶음은 <paramref name="onBatchDone"/> 로 바로 넘긴다(중간 캐시).
/// </summary>
internal static class UpscaleBatches
{
    /// <summary>묶음 입력 픽셀 첫 값 16MP(1.4 — WP0 실측 뒤 고침).</summary>
    public const long DefaultBatchPixels = 16L * 1024 * 1024;

    public sealed record Outcome(string? Failure, int Batches, IReadOnlyList<TimeSpan> Times, string Device);

    public static List<List<UpscalePlane>> Split(IReadOnlyList<UpscalePlane> planes, long batchPixels)
    {
        var ordered = planes.OrderBy(p => p.Job, StringComparer.Ordinal).ThenBy(p => p.Name, StringComparer.Ordinal).ToList();
        var result = new List<List<UpscalePlane>>();
        var cur = new List<UpscalePlane>();
        long px = 0;
        foreach (var p in ordered)
        {
            long n = (long)p.Image.Width * p.Image.Height;
            if (cur.Count > 0 && px + n > batchPixels) { result.Add(cur); cur = []; px = 0; }
            cur.Add(p);
            px += n;
        }
        if (cur.Count > 0) result.Add(cur);
        return result;
    }

    public static Outcome Run(IUpscaler up, IReadOnlyList<UpscalePlane> planes, string workDir, long batchPixels,
                              Action<UpscalePlane, RgbaImage> onPlaneDone)
    {
        var times = new List<TimeSpan>();
        var device = "";
        var batches = Split(planes, batchPixels);
        foreach (var batch in batches)
        {
            var sw = Stopwatch.StartNew();
            // 이름 = 순번(묶음 안 고유, 파일 이름에 안전)
            var inputs = batch.Select((p, i) => new UpscaleInput(i.ToString("D5", System.Globalization.CultureInfo.InvariantCulture), Png.Encode(p.Image), p.Image.Width, p.Image.Height)).ToList();
            var r = up.Run(inputs, workDir);
            sw.Stop();
            times.Add(sw.Elapsed);
            if (!string.IsNullOrEmpty(r.Device)) device = r.Device;
            if (r.Failure is { } f) return new Outcome(f, batches.Count, times, device);
            var decoded = new List<(UpscalePlane, RgbaImage)>();
            for (var i = 0; i < batch.Count; i++)
            {
                if (r.Outputs is null || !r.Outputs.TryGetValue(inputs[i].Name, out var png)) return new Outcome("결과 파일 없음", batches.Count, times, device);
                RgbaImage img;
                try { img = Png.Decode(png); }
                catch (InvalidDataException) { return new Outcome("결과 파일을 읽을 수 없음", batches.Count, times, device); }
                if (img.Width != inputs[i].Width * UpscaleMath.ModelScale || img.Height != inputs[i].Height * UpscaleMath.ModelScale)
                    return new Outcome("결과 크기 틀림", batches.Count, times, device);
                decoded.Add((batch[i], img));
            }
            foreach (var (p, img) in decoded) onPlaneDone(p, img);
        }
        return new Outcome(null, batches.Count, times, device);
    }
}
