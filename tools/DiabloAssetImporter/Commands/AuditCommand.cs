using System.Diagnostics;
using DiabloAssetImporter.Cli;
using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Mpq;
using DiabloAssetImporter.Pipeline;

namespace DiabloAssetImporter.Commands;

/// <summary>
/// <c>audit</c> (1.4, R24): git 이 추적하는 파일에 원작 자원이 섞였는지 대조 — manifest 출력 해시·표가 가리키는 원본 항목 해시·확장자 목록·<c>Assets/d1/**</c>,
/// 원본이 있으면 방 배열 대조(13.5-22 — 표 <c>audit_dun</c> 의 DUN 을 메모리에서만 읽어 맵 메가 격자와 4×4 창마다 비교). 하나라도 걸리면 종료 코드 4.
/// </summary>
internal static class AuditCommand
{
    public static readonly string[] Extensions = [".mpq", ".cl2", ".cel", ".pal", ".trn", ".min", ".til", ".sol", ".dun", ".amp", ".wav"];
    /// <summary>창 안 바닥 아닌 칸 문턱(1.4 — "벽 모서리 하나 = 7칸" 같은 흔한 모양을 거름).</summary>
    public const int MinNonFloor = 8;

    public static int Run(Options o, string cwd, TextWriter output)
    {
        o.Allow("--out", "--diabdat");
        var root = ImportCommand.FindRoot(cwd) ?? throw new UsageException("저장소 뿌리를 찾을 수 없다");
        var outDir = ImportCommand.ResolveOut(o, root);
        var problems = new List<string>();
        var tracked = GitLsFiles(root);

        var outputHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        var manifestPath = Path.Combine(outDir, AssetFormat.ManifestPath);
        if (File.Exists(manifestPath))
            foreach (var f in AssetManifest.Read(manifestPath).Files) outputHashes.TryAdd(f.Sha256, f.Path);

        var table = ImportTable.Load(RepoPaths.TableDir(root)).Table;
        var sourceHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        var diabdat = o.Get("--diabdat") ?? ImportCommand.CommonDiabdatPaths().FirstOrDefault(File.Exists);
        MpqSource? mpq = null;
        if (diabdat is not null && File.Exists(diabdat)) mpq = MpqSource.Open(diabdat);
        try
        {
            if (mpq is not null && table is not null)
                foreach (var p in SourcePaths(table))
                    if (mpq.Read(p) is { } b) sourceHashes.TryAdd(AssetManifest.Sha256Of(b), p);

            foreach (var rel in tracked)
            {
                var ext = Path.GetExtension(rel).ToLowerInvariant();
                if (Extensions.Contains(ext)) problems.Add($"원작 형식 확장자: {rel}");
                if (rel.Replace('\\', '/').Contains("Assets/d1/", StringComparison.OrdinalIgnoreCase)) problems.Add($"가져오기 출력 폴더: {rel}");
                var full = Path.Combine(root, rel);
                if (!File.Exists(full) || (outputHashes.Count == 0 && sourceHashes.Count == 0)) continue;
                var sha = AssetManifest.Sha256OfFile(full);
                if (outputHashes.TryGetValue(sha, out var outName)) problems.Add($"가져오기 출력과 같은 파일: {rel} = {outName}");
                if (sourceHashes.TryGetValue(sha, out var srcName)) problems.Add($"원본 항목과 같은 파일: {rel} = {srcName}");
            }

            if (mpq is not null && table is not null)
            {
                var data = MapDataReader.Read(RepoPaths.DataDir(root));
                foreach (var e in table.Tilesets.Tilesets)
                {
                    var tilesetDef = data.Tilesets.FirstOrDefault(t => t.Id == e.Id);
                    if (tilesetDef is null) continue;
                    var floor = ArenaLayout.FloorTiles(tilesetDef);
                    foreach (var dunName in e.AuditDun)
                    {
                        if (mpq.Read(e.SourcePath(dunName)) is not { } bytes) { problems.Add($"audit_dun {e.SourcePath(dunName)} 가 원본에 없다"); continue; }
                        var dun = DunFile.Read(bytes);
                        foreach (var map in data.Maps.Where(m => m.Tileset.Id == e.Id))
                            foreach (var variant in map.Mirrored is { } mm ? new[] { map, mm } : [map])
                            {
                                var grid = ArenaLayout.Expand(variant, variant.Tileset.Def);
                                problems.AddRange(CompareDun(grid, floor, dun, dunName));
                            }
                    }
                }
            }
        }
        finally { mpq?.Dispose(); }

        foreach (var p in problems) output.WriteLine(p);
        output.WriteLine(problems.Count == 0
            ? $"audit 통과: 추적 파일 {tracked.Count}개{(mpq is null ? " (원본 없음 — 출력 해시·확장자만)" : "")}"
            : $"audit 실패 {problems.Count}건");
        return problems.Count == 0 ? ExitCodes.Ok : ExitCodes.Check;
    }

    /// <summary>
    /// 방 배열 대조(13.5-22): 바닥 번호와 DUN 의 0 을 "바닥" 하나로 본 뒤 4×4 창 16칸이 모두 같고 바닥 아닌 칸이 <see cref="MinNonFloor"/> 이상이면 문제.
    /// </summary>
    public static IEnumerable<string> CompareDun(ArenaGrid grid, IReadOnlySet<int> floor, DunFile dun, string dunName)
    {
        // 변형 장식판(타일셋 accents)은 원래 번호로 — 변형 전 배치로 비교한다(장식판이 대조를 피하는 길이 되지 않게)
        int Norm(int v) => v == 0 || floor.Contains(v) ? 0 : ArenaLayout.AccentBase(grid.Tileset, v);
        var m = grid.MegaSize;
        for (var my = 0; my + 4 <= m; my++)
            for (var mx = 0; mx + 4 <= m; mx++)
                for (var dy = 0; dy + 4 <= dun.Height; dy++)
                    for (var dx = 0; dx + 4 <= dun.Width; dx++)
                    {
                        var same = true;
                        var nonFloor = 0;
                        for (var y = 0; y < 4 && same; y++)
                            for (var x = 0; x < 4; x++)
                            {
                                var a = Norm(grid.MegaTile(mx + x, my + y));
                                if (a != Norm(dun.At(dx + x, dy + y))) { same = false; break; }
                                if (a != 0) nonFloor++;
                            }
                        if (same && nonFloor >= MinNonFloor)
                            yield return $"maps/{grid.Map.Id}.json ({mx},{my}): {dunName} ({dx},{dy}) 와 4×4 배열이 같다";
                    }
    }

    private static IEnumerable<string> SourcePaths(ImportTable t)
    {
        foreach (var e in t.Monsters.Monsters.Concat(t.Hero.Hero))
        {
            foreach (var a in e.ResolveAll(null)) yield return a.SourcePath;
            foreach (var v in e.Variants.Values) if (v is not null) yield return v;
        }
        foreach (var e in t.Missiles.Missiles) foreach (var p in e.SourcePaths()) yield return p;
        foreach (var e in t.Icons.Icons) foreach (var p in e.SourcePaths()) yield return p;
        foreach (var e in t.Sounds.Sounds) foreach (var p in e.SourcePaths()) yield return p;
        foreach (var e in t.Music.Music) yield return e.Path;
        foreach (var e in t.Tilesets.Tilesets)
        {
            foreach (var p in Bake.TilesetSource.SourcePaths(e)) yield return p;
            foreach (var p in e.Palettes.Values) yield return e.SourcePath(p);
        }
        yield return t.Monsters.Palette;
    }

    private static List<string> GitLsFiles(string root)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        psi.ArgumentList.Add("ls-files");
        psi.ArgumentList.Add("-z");
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("git 을 띄울 수 없다");
        var text = p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new InvalidOperationException("git ls-files 실패");
        return [.. text.Split('\0', StringSplitOptions.RemoveEmptyEntries)];
    }
}
