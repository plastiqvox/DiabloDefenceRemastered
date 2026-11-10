using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Pipeline;

namespace DiabloAssetImporter.Commands;

/// <summary>
/// <c>contact</c>: 사람이 눈으로 볼 견본 PNG(1.4, WP0 완료 기준) — 저장소 밖 <c>--out</c> 에 가져오기를 한 번 돌리고(<c>&lt;out&gt;/assets</c>)
/// 그 출력으로 견본을 만든다: 좀비 원색·<c>bluered</c> 걷기, 파이어볼 16방향, L1 아레나 바닥 + 벽(전체), 마법사 시전. 확대 옵션을 그대로 넘길 수 있다.
/// </summary>
internal static class ContactCommand
{
    public static int Run(Options o, string cwd, TextWriter output)
    {
        o.Allow("--diabdat", "--out", "--upscale", "--upscale-bin", "--upscale-gpu", "--jobs", "--skip-bundles");
        var root = ImportCommand.FindRoot(cwd) ?? throw new UsageException("저장소 뿌리를 찾을 수 없다");
        var outDir = Path.GetFullPath(o.Get("--out") ?? throw new UsageException("contact 는 저장소 밖 --out 을 준다"));
        if (RepoPaths.CheckOutput(outDir) is { } bad) throw new UsageException(bad);
        var assets = Path.Combine(outDir, "assets");
        var diabdat = o.Get("--diabdat") ?? ImportCommand.CommonDiabdatPaths().FirstOrDefault(File.Exists) ?? throw new UsageException("--diabdat 를 준다");
        var r = Importer.Run(new ImportRequest
        {
            Diabdat = diabdat, OutDir = assets, TableDir = RepoPaths.TableDir(root), DataDir = RepoPaths.DataDir(root), Jobs = o.GetInt("--jobs", 0),
            SkipBundles = ImportCommand.ParseBundles(o.Get("--skip-bundles")),
            Upscale = o.Get("--upscale"), UpscaleBin = o.Get("--upscale-bin"), UpscaleGpu = o.Has("--upscale-gpu") ? o.GetInt("--upscale-gpu", 0) : null, Log = output,
        });
        if (r.ExitCode is not (ExitCodes.Ok or ExitCodes.UpscaleFallback)) return r.ExitCode;

        void Save(string name, RgbaImage img) { File.WriteAllBytes(Path.Combine(outDir, name), Png.Encode(img)); output.WriteLine($"견본 {Path.Combine(outDir, name)}"); }
        foreach (var variant in new[] { "", "bluered" })
            if (Strip(assets, "d1/monsters/zombie", "walk", 0, variant, null) is { } z) Save($"zombie_walk{(variant.Length > 0 ? "@" + variant : "")}.png", z);
        if (Strip(assets, "d1/monsters/zombie", "idle", null, "", 1) is { } zi) Save("zombie_idle_8dirs.png", zi);
        if (Strip(assets, "d1/missiles/fireba", null, null, "", 1) is { } fb) Save("fireball_16dirs.png", fb);
        if (Strip(assets, "d1/hero/slt", "cast_fire", 0, "", null) is { } sc) Save("sorcerer_cast_fire.png", sc);
        foreach (var icon in Directory.Exists(Path.Combine(assets, "d1", "icons")) ? Directory.GetFiles(Path.Combine(assets, "d1", "icons"), "*.png") : [])
            File.Copy(icon, Path.Combine(outDir, "icon_" + Path.GetFileName(icon)), true);
        foreach (var dir in Directory.Exists(Path.Combine(assets, "d1", "maps")) ? Directory.GetDirectories(Path.Combine(assets, "d1", "maps")) : [])
            Save($"arena_{Path.GetFileName(dir)}.png", Arena(dir));
        return r.ExitCode;
    }

    /// <summary>한 동작의 프레임 줄: <paramref name="dir"/> 가 있으면 그 방향의 모든 프레임, 없으면 모든 방향의 <paramref name="frame"/> 번째.</summary>
    internal static RgbaImage? Strip(string assets, string outName, string? action, int? dir, string variant, int? frame)
    {
        var metaPath = Path.Combine(assets, SpriteMeta.MetaFile(outName + ".png"));
        if (!File.Exists(metaPath)) return null;
        var meta = SpriteMeta.Read(metaPath);
        if (!meta.TryGetAction(action, out var a)) return null;
        var pages = Enumerable.Range(0, meta.Pages.Count).Select(p => Png.Decode(File.ReadAllBytes(Path.Combine(assets, SpriteMeta.PageFile(outName + ".png", p, variant))))).ToList();
        var cells = dir is { } d ? Enumerable.Range(1, a.Frames).Select(f => a.Rect(d, f)).ToList() : Enumerable.Range(0, a.Directions).Select(d2 => a.Rect(d2, frame ?? 1)).ToList();
        var img = new RgbaImage(a.Cell.X * cells.Count, a.Cell.Y);
        for (var i = 0; i < cells.Count; i++)
        {
            var r = cells[i];
            if (r.IsEmpty) continue;
            img.Blit(pages[r.Page], r.X, r.Y, r.W, r.H, i * a.Cell.X + r.Ox, r.Oy);
        }
        return OnDark(img);
    }

    /// <summary>맵 그림 전체: 바닥 조각 → 칸 그림(행 x+y, x 순 — 원작 그리는 순서, 부록 A.5). 월드 px 를 그림 배율로 그린다.</summary>
    internal static RgbaImage Arena(string dir)
    {
        var art = ArenaArt.Read(Path.Combine(dir, ArenaArt.FileName));
        var s = art.Scale;
        var img = new RgbaImage(art.Box.X * s, art.Box.Y * s);
        foreach (var f in art.Floor)
            img.DrawOver(Png.Decode(File.ReadAllBytes(Path.Combine(dir, f.File))), f.Pos.X * s, f.Pos.Y * s);
        var meta = SpriteMeta.Read(Path.Combine(dir, art.Columns + ".json"));
        var a = meta.Actions.Values.First();
        var pages = Enumerable.Range(0, meta.Pages.Count).Select(p => Png.Decode(File.ReadAllBytes(Path.Combine(dir, SpriteMeta.PageFile(art.Columns + ".png", p))))).ToList();
        var n = art.Box.X / 64;   // 상자 폭 = 64N
        var hc = a.Cell.Y / s;
        var h = hc - 32;
        foreach (var c in art.Cells.OrderBy(c => c.Tx + c.Ty).ThenBy(c => c.Tx).ThenBy(c => c.Kind))
        {
            var r = a.Rect(0, c.Frame);
            if (r.IsEmpty) continue;
            int cx = 32 * n + 32 * (c.Tx - c.Ty), cy = h + 16 * (c.Tx + c.Ty) + 16;
            var tile = pages[r.Page].Crop(r.X, r.Y, r.W, r.H);
            img.DrawOver(tile, (cx - 32) * s + r.Ox, (cy + 16 - hc) * s + r.Oy);
        }
        return OnDark(img);
    }

    private static RgbaImage OnDark(RgbaImage src)
    {
        var r = new RgbaImage(src.Width, src.Height);
        for (var i = 0; i < src.Width * src.Height; i++)
        {
            int a = src.Pixels[i * 4 + 3];
            for (var c = 0; c < 3; c++) r.Pixels[i * 4 + c] = (byte)((src.Pixels[i * 4 + c] * a + 40 * (255 - a)) / 255);
            r.Pixels[i * 4 + 3] = 255;
        }
        return r;
    }
}
