using System.Globalization;
using DiabloAssetImporter.Bake;
using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Mpq;
using DiabloAssetImporter.Pipeline;
using DiabloAssetImporter.Upscale;

namespace DiabloAssetImporter.Commands;

/// <summary><c>verify</c>: manifest 와 파일 해시만 대조(1.4). 다르면 목록과 종료 코드 4.</summary>
internal static class VerifyCommand
{
    public static int Run(Options o, string cwd, TextWriter output)
    {
        o.Allow("--out");
        var outDir = ImportCommand.ResolveOut(o, ImportCommand.FindRoot(cwd));
        var path = Path.Combine(outDir, AssetFormat.ManifestPath);
        if (!File.Exists(path)) { output.WriteLine($"manifest 없음: {path} — 가져오기를 돌린다"); return ExitCodes.Missing; }
        var m = AssetManifest.Read(path);
        var bad = 0;
        foreach (var f in m.Files)
        {
            var p = Path.Combine(outDir, f.Path);
            if (!File.Exists(p)) { output.WriteLine($"없음: {f.Path}"); bad++; continue; }
            if (new FileInfo(p).Length != f.Size || AssetManifest.Sha256OfFile(p) != f.Sha256) { output.WriteLine($"다름: {f.Path}"); bad++; }
        }
        output.WriteLine(bad == 0 ? $"manifest 와 같음: 파일 {m.Files.Count}개" : $"다른 파일 {bad}개");
        return bad == 0 ? ExitCodes.Ok : ExitCodes.Check;
    }
}

/// <summary>
/// <c>probe</c>: 원본 사실 보기(성질 표·가져오기 표를 손으로 쓸 때). 출력은 숫자·경로뿐이고 파일을 만들지 않는다.
/// <c>probe tiles --tileset l1 --tiles 1,2,15</c>, <c>probe files &lt;경로…&gt;</c>, <c>probe cl2 &lt;경로&gt; [--groups 8|1]</c>, <c>probe cel &lt;경로&gt; [--width &lt;폭&gt;]</c>.
/// </summary>
internal static class ProbeCommand
{
    public static int Run(Options o, string cwd, TextWriter output)
    {
        o.Allow("--diabdat", "--tileset", "--tiles", "--groups", "--hellfire", "--width", "--sheet");
        var diabdat = o.Get("--diabdat") ?? ImportCommand.CommonDiabdatPaths().FirstOrDefault(File.Exists)
            ?? throw new UsageException("--diabdat 를 준다");
        var what = o.Positional.FirstOrDefault() ?? throw new UsageException("probe tiles | files | cl2 | cel");
        using var mpq = MpqSource.Open(diabdat);
        var extra = new List<MpqSource>();
        if (o.Get("--hellfire") is { } hf)
            foreach (var f in Directory.EnumerateFiles(hf, "*.mpq").Order(StringComparer.OrdinalIgnoreCase))
                if (!string.Equals(Path.GetFileName(f), Path.GetFileName(diabdat), StringComparison.OrdinalIgnoreCase))
                    try { extra.Add(MpqSource.Open(f)); } catch (Exception ex) when (ex is IOException or InvalidDataException) { }
        try
        {
            switch (what)
            {
                case "files":
                    foreach (var p in o.Positional.Skip(1))
                    {
                        var hit = new[] { mpq }.Concat(extra).FirstOrDefault(m => m.Exists(p));
                        output.WriteLine(hit is null ? $"{p}: 없음" : $"{p}: {hit.Name} {hit.Read(p)!.Length} 바이트");
                    }
                    return 0;
                case "cl2":
                {
                    var p = o.Positional.ElementAtOrDefault(1) ?? throw new UsageException("probe cl2 <경로>");
                    var bytes = mpq.Read(p) ?? throw new UsageException($"{p} 없음");
                    var groups = o.GetInt("--groups", Cl2.LooksGrouped(bytes) ? 8 : 1);
                    var gl = groups == 1 ? [Cl2.ReadFlat(bytes)] : Cl2.ReadGroups(bytes, groups);
                    output.WriteLine($"{p}: 그룹 {gl.Count} (그룹 판별 {Cl2.LooksGrouped(bytes)})");
                    for (var g = 0; g < gl.Count; g++)
                    {
                        var widths = gl[g].Select(f => Cl2.EstimateWidth(f.Span)).ToList();
                        var w = widths.FirstOrDefault(x => x is not null) ?? 0;
                        var heights = w > 0 ? gl[g].Select(f => Cl2.Decode(f.Span, w).Height).ToList() : [];
                        output.WriteLine($"  그룹 {g}: 프레임 {gl[g].Count}, 폭 {w}, 높이 최대 {(heights.Count > 0 ? heights.Max() : 0)}");
                    }
                    return 0;
                }
                case "cel":
                {
                    // CEL 프레임마다 머리 유무·추정 폭·픽셀 수·쓰는 색인 범위(1..127 은 층 팔레트마다 다른 자리 — 부록 A.2)
                    var p = o.Positional.ElementAtOrDefault(1) ?? throw new UsageException("probe cel <경로> [--width <폭>]");
                    var bytes = new[] { mpq }.Concat(extra).Select(m => m.Read(p)).FirstOrDefault(b => b is not null) ?? throw new UsageException($"{p} 없음");
                    var frames = Cel.Frames(bytes);
                    int? width = o.Has("--width") ? o.GetInt("--width", 0) : null;
                    output.WriteLine($"{p}: 프레임 {frames.Count}, 머리 있음 {frames.Count(f => Cel.HasHeader(f.Span))}");
                    for (var i = 0; i < frames.Count; i++)
                    {
                        var f = frames[i].Span;
                        var px = Cel.CountPixels(Cel.Data(f));
                        var w = Cel.EstimateWidth(f) ?? width;
                        var low = 0;
                        int min = 255, max = 0;
                        if (w is { } ww)
                        {
                            var img = Cel.Decode(f, ww);
                            for (var k = 0; k < img.Index.Length; k++)
                                if (img.Opaque[k])
                                {
                                    var c = img.Index[k];
                                    if (c is > 0 and < 128) low++;
                                    min = Math.Min(min, c); max = Math.Max(max, c);
                                }
                        }
                        output.WriteLine($"  {i + 1}: {f.Length}B 머리 {(Cel.HasHeader(f) ? "O" : "X")} 폭 {(Cel.EstimateWidth(f)?.ToString(CultureInfo.InvariantCulture) ?? "-")} 픽셀 {px}" +
                                         (w is { } w2 ? $" ({w2}×{(px + w2 - 1) / w2}{(px % w2 == 0 ? "" : " 나머지")}) 색인 {min}..{max} 1~127 {low}" : ""));
                    }
                    if (o.Get("--sheet") is { } sheet) SaveCelSheet(sheet, frames, width, Palette.Read(mpq.Read(@"levels\l1data\l1.pal")!), output);
                    return 0;
                }
                case "tiles":
                {
                    var id = o.Get("--tileset") ?? throw new UsageException("--tileset 을 준다");
                    var root = ImportCommand.FindRoot(cwd) ?? throw new UsageException("저장소 뿌리를 찾을 수 없다");
                    var table = ImportTable.Load(RepoPaths.TableDir(root)).Table ?? throw new UsageException("Table 을 읽을 수 없다");
                    var e = table.Tilesets.Tilesets.FirstOrDefault(t => t.Id == id) ?? throw new UsageException($"타일셋 {id} 가 tilesets.json 에 없다");
                    var dict = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in TilesetSource.SourcePaths(e)) dict[p] = mpq.Read(p) ?? throw new UsageException($"{p} 없음");
                    var ts = TilesetSource.From(e, new SourceBag(dict));
                    output.WriteLine($"타일셋 {id}: 메가타일 {ts.Til.Count}, 하위 타일 {ts.Min.Count}, SOL {ts.Sol.Length}");
                    var tiles = (o.Get("--tiles") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToList();
                    if (o.Get("--sheet") is { } megaSheet)
                    {
                        SaveMegaSheet(megaSheet, ts, tiles.Count > 0 ? tiles : [.. Enumerable.Range(1, ts.Til.Count)], Palette.Read(mpq.Read(e.SourcePath(e.Palettes.Values.First())) ?? throw new UsageException("팔레트 없음")), output);
                        return 0;
                    }
                    foreach (var t in tiles)
                    {
                        var subs = Enumerable.Range(0, 4).Select(k => ts.Til.Subtile(t, k)).ToList();
                        var props = new string([.. subs.Select(s => Sol.PropChar(ts.Sol[s]))]);
                        output.WriteLine($"  메가 {t}: 하위 {string.Join(",", subs.Select(s => s + 1))} SOL {string.Join(",", subs.Select(s => ts.Sol[s]))} 성질 \"{props}\"");
                        foreach (var s in subs)
                            output.WriteLine($"    하위 {s + 1}: 조각 {string.Join(" ", Enumerable.Range(0, ts.Min.PiecesPerSubtile).Select(k => ts.Min.Piece(s, k)).Select(v => v == 0 ? "-" : $"{MinFile.Frame(v)}/{MinFile.Kind(v)}"))}");
                    }
                    return 0;
                }
                default:
                    throw new UsageException("probe tiles | files | cl2 | cel");
            }
        }
        finally
        {
            foreach (var m in extra) m.Dispose();
        }
    }

    /// <summary>
    /// <c>probe cel --sheet &lt;png&gt;</c>: 프레임 전부를 한 줄 10칸 격자로(칸 = 가장 큰 프레임, 어두운 바탕, l1.pal) — 표의 프레임 번호를 눈으로 고를 때.
    /// 줄 r·칸 c 가 프레임 10r + c + 1 이다. 원작 그림이 든 파일이라 저장소 밖에만 쓴다(1.4 출력 경로 검사).
    /// </summary>
    private static void SaveCelSheet(string path, IReadOnlyList<ReadOnlyMemory<byte>> frames, int? width, Palette pal, TextWriter output)
    {
        var full = Path.GetFullPath(path);
        if (RepoPaths.CheckOutput(Path.GetDirectoryName(full)!) is { } bad) throw new UsageException(bad);
        var images = frames.Select(f =>
        {
            var px = Cel.CountPixels(Cel.Data(f.Span));
            var w = Cel.EstimateWidth(f.Span) ?? (width is { } tw && px % tw == 0 ? tw : Math.Max(1, (int)Math.Round(Math.Sqrt(px))));
            return RgbaImage.FromIndexed(Cel.Decode(f.Span, w), pal, null, null);
        }).ToList();
        int cw = images.Max(i => i.Width) + 4, ch = images.Max(i => i.Height) + 4, cols = Math.Min(10, images.Count);
        var sheet = new RgbaImage(cw * cols, ch * ((images.Count + cols - 1) / cols));
        for (var y = 0; y < sheet.Height; y++)
            for (var x = 0; x < sheet.Width; x++)
            {
                var edge = x % cw < 2 || y % ch < 2;
                var v = (byte)(edge ? 90 : 30);
                sheet.Set(x, y, v, v, (byte)(edge ? 40 : 30), 255);
            }
        for (var i = 0; i < images.Count; i++)
            sheet.DrawOver(images[i], i % cols * cw + 2, i / cols * ch + 2 + (ch - 4 - images[i].Height));
        File.WriteAllBytes(full, Png.Encode(sheet));
        output.WriteLine($"격자 {full} ({cols}칸 × {(images.Count + cols - 1) / cols}줄)");
    }

    /// <summary>
    /// <c>probe tiles --tileset l1 --sheet &lt;png&gt; [--tiles …]</c>: 메가타일을 하위 칸 기둥 넷으로 맞춰 그린 격자(한 줄 10칸, 칸 왼쪽 위에 메가 번호) — 맵 조각 번호를 눈으로 고를 때.
    /// 원작 그림이 든 파일이라 저장소 밖에만 쓴다(1.4 출력 경로 검사).
    /// </summary>
    private static void SaveMegaSheet(string path, TilesetSource ts, IReadOnlyList<int> megas, Palette pal, TextWriter output)
    {
        var full = Path.GetFullPath(path);
        if (RepoPaths.CheckOutput(Path.GetDirectoryName(full)!) is { } bad) throw new UsageException(bad);
        var hc = ts.Min.PiecesPerSubtile / 2 * 32;
        int cw = 128 + 8, ch = hc + 32 + 16, cols = Math.Min(10, megas.Count);
        var sheet = new RgbaImage(cw * cols, ch * ((megas.Count + cols - 1) / cols));
        for (var y = 0; y < sheet.Height; y++)
            for (var x = 0; x < sheet.Width; x++)
            {
                var edge = x % cw < 2 || y % ch < 2;
                var v = (byte)(edge ? 90 : 30);
                sheet.Set(x, y, v, v, (byte)(edge ? 40 : 30), 255);
            }
        for (var i = 0; i < megas.Count; i++)
        {
            int ox = i % cols * cw + 4, oy = i / cols * ch + 12;
            // 뒤 → 앞 차례(하위 칸 k = 줄·2 + 열): 칸 가운데 C 의 기둥 왼쪽 위 = (C.x − 32, C.y + 16 − Hc), 메가 위 꼭짓점 칸 C = (64, Hc − 16)
            foreach (var k in (int[])[0, 1, 2, 3])
            {
                int sx = k % 2, sy = k / 2;
                var col = RgbaImage.FromIndexed(ts.Column(ts.Til.Subtile(megas[i], k), upperOnly: false), pal, null, null);
                var cx = 64 + (sx - sy) * 32;
                var cy = hc - 16 + (sx + sy) * 16;
                sheet.DrawOver(col, ox + cx - 32, oy + cy + 16 - col.Height);
            }
            DrawNumber(sheet, megas[i], i % cols * cw + 4, i / cols * ch + 4);
        }
        File.WriteAllBytes(full, Png.Encode(sheet));
        output.WriteLine($"메가 격자 {full} ({cols}칸 × {(megas.Count + cols - 1) / cols}줄, {megas[0]}..{megas[^1]})");
    }

    /// <summary>3×5 숫자(2배) — 견본 격자 번호.</summary>
    private static void DrawNumber(RgbaImage img, int n, int x, int y)
    {
        string[] glyphs = ["111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
                           "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111"];
        foreach (var ch in n.ToString(CultureInfo.InvariantCulture))
        {
            var g = glyphs[ch - '0'];
            for (var p = 0; p < 15; p++)
                if (g[p] == '1')
                    for (var d = 0; d < 4; d++)
                        img.Set(x + p % 3 * 2 + d % 2, y + p / 3 * 2 + d / 2, 255, 230, 120, 255);
            x += 8;
        }
    }
}

/// <summary>
/// <c>upscaler-check</c>(1.4, v0.8.1): 해시 확인 + Vulkan 런타임 확인 뒤, 임시 폴더(<c>d1/.cache/tmp/</c>)에서 도구가 만든 16×16 무늬 그림(원작 바이트 아님) 한 장을 변환해
/// 종료 코드·결과 크기·장치 줄을 보인다. Assets 는 만들지 않는다. 문제가 있으면 이유와 종료 코드 6.
/// </summary>
internal static class UpscalerCheckCommand
{
    public static int Run(Options o, string cwd, TextWriter output) => Run(o, cwd, output, null, null);

    internal static int Run(Options o, string cwd, TextWriter output, Func<bool>? vulkan, UpscaleTable? tableOverride)
    {
        o.Allow("--upscale-bin", "--upscale-gpu", "--upscale", "--out");
        var root = ImportCommand.FindRoot(cwd);
        var outDir = ImportCommand.ResolveOut(o, root);
        if (root is null) throw new UsageException("저장소 뿌리를 찾을 수 없다");
        var table = tableOverride ?? ImportTable.Load(RepoPaths.TableDir(root)).Table?.Upscale ?? throw new UsageException("Table/upscale.json 을 읽을 수 없다");
        var modelName = o.Get("--upscale") ?? "realesrgan-x4plus";
        if (!table.Models.TryGetValue(modelName, out var model)) throw new UsageException($"확대 모델 '{modelName}' 이 표에 없다");
        var bin = o.Get("--upscale-bin") ?? UpscaleSetup.DefaultBin();
        int? gpu = o.Has("--upscale-gpu") ? o.GetInt("--upscale-gpu", 0) : null;
        output.WriteLine($"확대 프로그램 폴더: {bin}");
        if (UpscaleSetup.Check(model, bin, vulkan ?? UpscaleSetup.SystemVulkanPresent) is { } why)
        {
            output.WriteLine($"확인 실패: {why}");
            return ExitCodes.UpscaleFallback;
        }
        output.WriteLine("해시·Vulkan 런타임: 통과");
        var work = Path.Combine(outDir, AssetFormat.Root, ".cache", "tmp");
        Directory.CreateDirectory(work);
        try
        {
            var img = new RgbaImage(16, 16);
            for (var y = 0; y < 16; y++)
                for (var x = 0; x < 16; x++)
                    img.Set(x, y, (byte)((x ^ y) * 16), (byte)(x * 16), (byte)(y * 16), 255);
            var up = new ProcessUpscaler(Path.Combine(bin, UpscaleSetup.ExeFile), modelName, Path.Combine(bin, "models"), gpu, ProcessUpscaler.DefaultNoProgress);
            var r = up.Run([new UpscaleInput("check", Png.Encode(img), 16, 16)], work);
            output.WriteLine($"장치: {(string.IsNullOrEmpty(r.Device) ? "(장치 줄 없음)" : r.Device)}");
            if (r.Failure is { } f) { output.WriteLine($"변환 실패: {f}"); return ExitCodes.UpscaleFallback; }
            if (r.Outputs is null || !r.Outputs.TryGetValue("check", out var png)) { output.WriteLine("변환 실패: 결과 파일 없음"); return ExitCodes.UpscaleFallback; }
            var (w, h) = Png.ReadSize(png);
            output.WriteLine($"결과 크기 {w}×{h} (기대 64×64)");
            return w == 64 && h == 64 ? ExitCodes.Ok : ExitCodes.UpscaleFallback;
        }
        finally
        {
            try { Directory.Delete(work, true); } catch (IOException) { }
        }
    }
}
