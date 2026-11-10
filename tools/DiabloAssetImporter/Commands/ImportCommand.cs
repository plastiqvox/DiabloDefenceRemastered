using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Pipeline;

namespace DiabloAssetImporter.Commands;

/// <summary><c>import</c> (1.4 "실행"): 옵션 → <see cref="ImportRequest"/>. 출력 폴더·원본 경로 검사는 여기서(종료 코드 2).</summary>
internal static class ImportCommand
{
    /// <summary><c>--diabdat</c> 가 없을 때 찾아볼 흔한 설치 위치.</summary>
    public static IReadOnlyList<string> CommonDiabdatPaths()
    {
        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return
        [
            Path.Combine(pf86, "Diablo", "DIABDAT.MPQ"),
            Path.Combine(pf, "Diablo", "DIABDAT.MPQ"),
            Path.Combine(pf86, "GOG Galaxy", "Games", "Diablo", "DIABDAT.MPQ"),
            @"C:\GOG Games\Diablo\DIABDAT.MPQ",
        ];
    }

    public static int Run(Options o, string cwd, TextWriter output)
    {
        o.Allow("--diabdat", "--out", "--force", "--jobs", "--hellfire", "--skip-bundles", "--upscale", "--upscale-bin", "--upscale-gpu");
        var root = FindRoot(cwd);
        var outDir = ResolveOut(o, root);
        if (root is null) throw new UsageException("저장소 뿌리(DiabloDefenceRemastered.sln)를 찾을 수 없다 — 가져오기 표·맵 정의가 저장소에 있다. 저장소 안에서 돌린다");
        if (RepoPaths.CheckOutput(outDir) is { } bad) throw new UsageException(bad);

        var diabdat = o.Get("--diabdat") ?? CommonDiabdatPaths().FirstOrDefault(File.Exists)
            ?? throw new UsageException("diabdat.mpq 를 흔한 설치 위치에서 찾지 못했다 — 경로를 --diabdat 로 준다");
        var skip = ParseBundles(o.Get("--skip-bundles"));
        int? gpu = o.Has("--upscale-gpu") ? o.GetInt("--upscale-gpu", 0) : null;
        var result = Importer.Run(new ImportRequest
        {
            Diabdat = diabdat, OutDir = Path.GetFullPath(outDir), TableDir = RepoPaths.TableDir(root), DataDir = RepoPaths.DataDir(root),
            Force = o.Has("--force"), Jobs = o.GetInt("--jobs", 0),
            HellfireDir = o.Get("--hellfire"), HellfireExplicit = o.Has("--hellfire"), SkipBundles = skip,
            Upscale = o.Get("--upscale"), UpscaleBin = o.Get("--upscale-bin"), UpscaleGpu = gpu, Log = output,
        });
        return result.ExitCode;
    }

    /// <summary>현재 폴더에서 위로, 없으면 도구 실행 파일 자리에서 위로(<c>dotnet run --project</c> 를 다른 폴더에서 돌린 경우).</summary>
    public static string? FindRoot(string cwd) => RepoPaths.FindRoot(cwd) ?? RepoPaths.FindRoot(AppContext.BaseDirectory);

    public static string ResolveOut(Options o, string? root) =>
        o.Get("--out") is { } given ? Path.GetFullPath(given)
        : root is not null ? RepoPaths.AssetsDir(root)
        : throw new UsageException("저장소 뿌리를 찾을 수 없어 출력 폴더를 정할 수 없다 — --out 을 준다");

    public static HashSet<ContentBundle> ParseBundles(string? text)
    {
        var set = new HashSet<ContentBundle>();
        if (string.IsNullOrWhiteSpace(text)) return set;
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part == "all") { foreach (var b in ContentBundles.All) set.Add(b); continue; }
            var match = ContentBundles.All.FirstOrDefault(b => ContentBundles.Name(b) == part);
            if (ContentBundles.Name(match) != part) throw new UsageException($"--skip-bundles 의 '{part}' — 묶음은 {string.Join(", ", ContentBundles.All.Select(ContentBundles.Name))} 또는 all");
            set.Add(match);
        }
        return set;
    }
}
