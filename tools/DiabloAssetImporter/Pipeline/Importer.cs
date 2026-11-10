using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DiabloAssetImporter.Bake;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Mpq;
using DiabloAssetImporter.Upscale;

namespace DiabloAssetImporter.Pipeline;

/// <summary>종료 코드 (1.4 "실패와 보고").</summary>
internal static class ExitCodes
{
    public const int Ok = 0, Unhandled = 1, Input = 2, Missing = 3, Check = 4, UpscaleFallback = 6;
}

/// <summary><c>import</c> 요청. 시험은 확대 표·Vulkan 판정·확대 프로그램·묶음 크기를 주입한다(1.4 "시험").</summary>
internal sealed record ImportRequest
{
    public required string Diabdat { get; init; }
    public required string OutDir { get; init; }
    public required string TableDir { get; init; }
    public required string DataDir { get; init; }
    public bool Force { get; init; }
    public int Jobs { get; init; }
    public string? HellfireDir { get; init; }
    public bool HellfireExplicit { get; init; }
    public IReadOnlySet<ContentBundle> SkipBundles { get; init; } = new HashSet<ContentBundle>();
    public string? Upscale { get; init; }
    public string? UpscaleBin { get; init; }
    public int? UpscaleGpu { get; init; }
    public UpscaleTable? UpscaleTableOverride { get; init; }
    public Func<bool>? VulkanPresent { get; init; }
    public Func<string, IUpscaler>? UpscalerFactory { get; init; }
    public long BatchPixels { get; init; } = UpscaleBatches.DefaultBatchPixels;
    public TimeSpan? NoProgress { get; init; }
    public TextWriter? Log { get; init; }
}

internal sealed record ImportResult(int ExitCode, AssetManifest? Manifest, IReadOnlyList<string> Report);

/// <summary>
/// 가져오기 본체 (1.4 전부): 표·맵 정의 읽기 → MPQ 열기·묶음 판정 → manifest 지우기 → 원본 읽기(주 스레드) → 캐시 → 굽기(<c>--jobs</c>, 확대 세 단계)
/// → 임시 이름 바꿔치기 → 새 manifest 에 없는 <c>d1/</c> 파일 지우기 → 소리 색인 → manifest(마지막) → 끝 보고.
/// </summary>
internal static class Importer
{
    /// <summary>확대 처리 판(캐시 키 — 처리 ①~⑦ 코드가 바뀌면 올린다. 도구 판 해시도 키에 있으므로 보조).</summary>
    public const int UpscaleProcessVersion = 1;

    private sealed record CacheFile(string Path, string Sha256, long Size, int Scale);
    private sealed record CacheEntry(string Key, IReadOnlyList<CacheFile> Files, IReadOnlyList<string> Sources, IReadOnlyList<SoundInfo> Sounds, string? Gpu);
    private sealed record CacheIndex(IReadOnlyDictionary<string, CacheEntry> Jobs);
    private sealed record PlaneCacheIndex(IReadOnlyDictionary<string, string> Gpu);

    public static ImportResult Run(ImportRequest req)
    {
        var report = new List<string>();
        var warnings = new List<string>();
        var log = req.Log ?? Console.Out;
        void Say(string s) { report.Add(s); log.WriteLine(s); }
        var total = Stopwatch.StartNew();

        // ── 표·정의 ───────────────────────────────────────────────
        var tableResult = ImportTable.Load(req.TableDir);
        if (tableResult.HasErrors || tableResult.Table is null)
        {
            foreach (var e in tableResult.Errors) Say($"표 오류 {e}");
            return new ImportResult(ExitCodes.Check, null, report);
        }
        var table = tableResult.Table with { Upscale = req.UpscaleTableOverride ?? tableResult.Table.Upscale };
        var data = MapDataReader.Read(req.DataDir);
        if (data.HasErrors)
        {
            foreach (var e in data.Errors) Say($"맵 정의 오류 {e}");
            return new ImportResult(ExitCodes.Check, null, report);
        }
        var mapProblems = table.CheckMaps(data.Maps);
        if (mapProblems.Count > 0)
        {
            foreach (var p in mapProblems) Say(p);
            return new ImportResult(ExitCodes.Check, null, report);
        }
        UpscaleModelDef? model = null;
        if (req.Upscale is { } modelName && !table.Upscale.Models.TryGetValue(modelName, out model))
        {
            Say($"확대 모델 '{modelName}' 이 Table/upscale.json 에 없다(있는 것: {string.Join(", ", table.Upscale.Models.Keys.Order(StringComparer.Ordinal))})");
            return new ImportResult(ExitCodes.Input, null, report);
        }

        // ── MPQ ───────────────────────────────────────────────────
        if (!File.Exists(req.Diabdat))
        {
            Say($"diabdat.mpq 를 찾을 수 없다: {req.Diabdat} — 경로를 --diabdat 로 준다");
            return new ImportResult(ExitCodes.Input, null, report);
        }
        MpqSource diabdat;
        try { diabdat = MpqSource.Open(req.Diabdat); }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Say($"diabdat.mpq 를 열 수 없다: {req.Diabdat} ({ex.Message})");
            return new ImportResult(ExitCodes.Input, null, report);
        }
        var opened = new List<MpqSource> { diabdat };
        try
        {
            var bundles = new Dictionary<ContentBundle, MpqSource>();
            var skipped = new Dictionary<ContentBundle, ManifestSkippedBundle>();
            var hfDir = req.HellfireDir ?? Path.GetDirectoryName(Path.GetFullPath(req.Diabdat)) ?? ".";
            foreach (var b in ContentBundles.All)
            {
                var mpqName = ContentBundles.MpqName(b);
                if (req.SkipBundles.Contains(b)) { skipped[b] = new(b, mpqName, ManifestSkippedBundle.ReasonSkipped); continue; }
                var path = Directory.Exists(hfDir)
                    ? Directory.EnumerateFiles(hfDir).FirstOrDefault(f => string.Equals(Path.GetFileName(f), mpqName, StringComparison.OrdinalIgnoreCase))
                    : null;
                if (path is null) { skipped[b] = new(b, mpqName, ManifestSkippedBundle.ReasonMissing); continue; }
                try
                {
                    var m = MpqSource.Open(path);
                    opened.Add(m);
                    bundles[b] = m;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)   // War3Net 은 머리가 깨지면 MpqParserException 등을 던진다
                {
                    if (req.HellfireExplicit)
                    {
                        Say($"{mpqName} 를 열 수 없다: {path} ({ex.Message})");
                        Say(Bypass(b));
                        return new ImportResult(ExitCodes.Input, null, report);
                    }
                    skipped[b] = new(b, mpqName, ManifestSkippedBundle.ReasonUnreadable);
                }
            }

            // ── 맵: 묶음을 건너뛴 타일셋의 맵은 굽지 않는다 ───────────
            var arenas = new List<ArenaGrid>();
            foreach (var map in data.Maps.OrderBy(m => m.Id, StringComparer.Ordinal))
            {
                arenas.Add(ArenaLayout.Expand(map));
                if (map.Mirrored is { } mm) arenas.Add(ArenaLayout.Expand(mm, mm.Tileset.Def));
            }

            var jobsAll = JobPlanner.Plan(table, arenas, req.Upscale is null ? null : table.Upscale);

            // ── 원본 찾기(이름만) — 빠진 기본 항목은 3, 빠진 묶음 항목은 그 묶음 전체 건너뜀 ──
            MpqSource? Locate(ImportJob j, string path, out bool usable)
            {
                usable = true;
                foreach (var p in j.Prefer)
                    if (bundles.TryGetValue(p, out var pm) && !skipped.ContainsKey(p) && pm.Exists(path)) return pm;
                if (diabdat.Exists(path)) return diabdat;
                if (j.Bundle is { } b && bundles.TryGetValue(b, out var bm) && bm.Exists(path)) return bm;
                return null;
            }
            var missingBase = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var missingByBundle = new Dictionary<ContentBundle, SortedSet<string>>();
            foreach (var j in jobsAll)
            {
                if (j.Bundle is { } b && skipped.ContainsKey(b)) continue;
                foreach (var s in j.Sources)
                    if (Locate(j, s, out _) is null)
                    {
                        if (j.Bundle is { } bb) (missingByBundle.TryGetValue(bb, out var set) ? set : missingByBundle[bb] = new(StringComparer.OrdinalIgnoreCase)).Add(s);
                        else missingBase.Add(s);
                    }
            }
            foreach (var (b, list) in missingByBundle.OrderBy(p => p.Key))
            {
                if (req.HellfireExplicit)
                {
                    foreach (var s in list) missingBase.Add(s);
                    continue;
                }
                skipped[b] = new(b, ContentBundles.MpqName(b), $"{ManifestSkippedBundle.ReasonVersionPrefix}: {string.Join(", ", list)}");
            }
            if (missingBase.Count > 0)
            {
                Say($"표에 있는데 원본에 없는 파일 {missingBase.Count}개:");
                foreach (var s in missingBase) Say($"  {s}");
                if (missingByBundle.Count > 0 && req.HellfireExplicit)
                    foreach (var b in missingByBundle.Keys) Say(Bypass(b));
                DeleteManifest(req.OutDir);
                return new ImportResult(ExitCodes.Missing, null, report);
            }
            var jobs = jobsAll.Where(j => j.Bundle is not { } b || !skipped.ContainsKey(b)).ToList();

            DeleteManifest(req.OutDir);
            var d1 = Path.Combine(req.OutDir, AssetFormat.Root);
            Directory.CreateDirectory(d1);

            // ── 원본 읽기(주 스레드, 차례로) ───────────────────────────
            var readSw = Stopwatch.StartNew();
            var bags = new SourceBag[jobs.Count];
            var jobSources = new List<string>[jobs.Count];
            var inputHash = new string[jobs.Count];
            var byteCache = new Dictionary<(string Mpq, string Path), (byte[] Bytes, string Sha)>();
            for (var i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                var dict = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                var names = new SortedSet<string>(StringComparer.Ordinal);
                var sb = new StringBuilder();
                foreach (var s in j.Sources)
                {
                    var m = Locate(j, s, out _)!;
                    var key = (m.Name, s.ToLowerInvariant());
                    if (!byteCache.TryGetValue(key, out var hit))
                    {
                        var bytes = m.Read(s) ?? throw new InvalidOperationException($"{s} 를 {m.Name} 에서 읽지 못했다");
                        hit = (bytes, AssetManifest.Sha256Of(bytes));
                        byteCache[key] = hit;
                    }
                    dict[s] = hit.Bytes;
                    names.Add(m.Name);
                    sb.Append(s.ToLowerInvariant()).Append('=').Append(hit.Sha).Append('\n');
                }
                bags[i] = new SourceBag(dict);
                jobSources[i] = [.. names];
                inputHash[i] = sb.ToString();
            }
            readSw.Stop();

            // ── 캐시 키 ────────────────────────────────────────────────
            var toolHash = ToolHash();
            string? upscaleReason = null;
            IUpscaler? upscaler = null;
            var binDir = req.UpscaleBin ?? UpscaleSetup.DefaultBin();
            if (model is not null)
            {
                upscaleReason = UpscaleSetup.Check(model, binDir, req.VulkanPresent ?? UpscaleSetup.SystemVulkanPresent);
                if (upscaleReason is null)
                    upscaler = req.UpscalerFactory?.Invoke(binDir)
                        ?? new ProcessUpscaler(Path.Combine(binDir, UpscaleSetup.ExeFile), req.Upscale!, Path.Combine(binDir, "models"), req.UpscaleGpu,
                                               req.NoProgress ?? ProcessUpscaler.DefaultNoProgress);
            }
            var upscaleMode = model is null ? "원본" :
                $"upscale\n{req.Upscale}\n{string.Join("\n", model.Files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value))}\n{table.Upscale.OutputScale}\n{UpscaleProcessVersion}";
            string KeyOf(int i, bool upscaled) => Sha($"{toolHash}\n{jobs[i].Spec}\n{inputHash[i]}\n{(upscaled ? upscaleMode : "원본")}");

            var cachePath = Path.Combine(d1, ".cache", "index.json");
            var cache = LoadCache(cachePath, req.Force);
            var newCache = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
            var results = new JobOutput?[jobs.Count];
            var gpuOf = new string?[jobs.Count];
            var hits = 0;

            bool TryHit(int i, string key)
            {
                if (!cache.TryGetValue(jobs[i].Name, out var c) || c.Key != key) return false;
                foreach (var f in c.Files)
                {
                    var p = Path.Combine(req.OutDir, f.Path);
                    if (!File.Exists(p) || new FileInfo(p).Length != f.Size || AssetManifest.Sha256OfFile(p) != f.Sha256) return false;
                }
                newCache[jobs[i].Name] = c;
                gpuOf[i] = c.Gpu;
                return true;
            }

            var parallel = new ParallelOptions { MaxDegreeOfParallelism = req.Jobs > 0 ? req.Jobs : Environment.ProcessorCount };
            var bakeSw = Stopwatch.StartNew();
            var targets = Enumerable.Range(0, jobs.Count).Where(i => jobs[i].UpscaleTarget && model is not null).ToList();
            var plain = Enumerable.Range(0, jobs.Count).Except(targets).ToList();
            var useUpscale = upscaler is not null;
            var upscaleBatches = 0;
            var upscaleTimes = new List<TimeSpan>();
            var planesNeeded = 0;
            var planesHit = 0;
            var device = "";

            // 확대 대상 (1) 병렬: 출력 캐시 조회 → 미적중은 판 모으기
            var planeDir = Path.Combine(d1, ".cache", "upscaled");
            var planeIndexPath = Path.Combine(planeDir, "index.json");
            var planeGpu = LoadPlaneIndex(planeIndexPath, req.Force);
            var usedPlaneKeys = new HashSet<string>(StringComparer.Ordinal);
            var resolved = new Dictionary<string, RgbaImage>(StringComparer.Ordinal);
            string PlaneKey(string planeHash) => Sha($"{planeHash}\n{upscaleMode}");
            var gpuLabel = req.UpscaleGpu?.ToString(CultureInfo.InvariantCulture) ?? "default";
            if (useUpscale)
            {
                var pending = targets.Where(i => !TryHit(i, KeyOf(i, true))).ToList();
                hits += targets.Count - pending.Count;
                var collected = new List<UpscalePlane>[jobs.Count];
                Parallel.ForEach(pending, parallel, i =>
                {
                    var c = new CollectingPlanes(jobs[i].Name);
                    jobs[i].Bake(bags[i], c);
                    collected[i] = c.Planes;
                });
                var all = pending.SelectMany(i => collected[i]).ToList();
                var unique = new Dictionary<string, UpscalePlane>(StringComparer.Ordinal);
                foreach (var p in all.OrderBy(p => p.Job, StringComparer.Ordinal).ThenBy(p => p.Name, StringComparer.Ordinal))
                    unique.TryAdd(p.Hash, p);
                planesNeeded = unique.Count;
                var todo = new List<UpscalePlane>();
                foreach (var (hash, p) in unique)
                {
                    var key = PlaneKey(hash);
                    usedPlaneKeys.Add(key);
                    var file = Path.Combine(planeDir, key + ".png");
                    if (File.Exists(file))
                    {
                        try
                        {
                            var img = Png.Decode(File.ReadAllBytes(file));
                            if (img.Width == p.Image.Width * UpscaleMath.ModelScale && img.Height == p.Image.Height * UpscaleMath.ModelScale)
                            {
                                resolved[hash] = img;
                                planesHit++;
                                continue;
                            }
                        }
                        catch (InvalidDataException) { }
                    }
                    todo.Add(p);
                }
                // (2) 주 스레드: 외부 프로그램(동시에 하나), 성공한 묶음은 바로 중간 캐시에
                if (todo.Count > 0)
                {
                    Directory.CreateDirectory(planeDir);
                    var work = Path.Combine(d1, ".cache", "tmp");
                    Directory.CreateDirectory(work);
                    try
                    {
                        var outcome = UpscaleBatches.Run(upscaler!, todo, work, req.BatchPixels, (p, img) =>
                        {
                            var key = PlaneKey(p.Hash);
                            WriteAtomic(Path.Combine(planeDir, key + ".png"), Png.Encode(img));
                            resolved[p.Hash] = img;
                            planeGpu[key] = gpuLabel;
                        });
                        upscaleBatches = outcome.Batches;
                        upscaleTimes.AddRange(outcome.Times);
                        device = outcome.Device;
                        if (outcome.Failure is { } f) upscaleReason = f;
                    }
                    finally
                    {
                        try { Directory.Delete(work, true); } catch (IOException) { }
                        WriteAtomic(planeIndexPath, AssetJson.Serialize(new PlaneCacheIndex(planeGpu)));
                    }
                }
                if (upscaleReason is null)
                {
                    // (3) 병렬: 줄이기·알파·그림자·묶기·PNG
                    var r = new ResolvedPlanes(resolved);
                    Parallel.ForEach(pending, parallel, i => results[i] = jobs[i].Bake(bags[i], r));
                    foreach (var i in pending)
                    {
                        var gpus = collected[i].Select(p => planeGpu.TryGetValue(PlaneKey(p.Hash), out var g) ? g : gpuLabel).Distinct().ToList();
                        gpuOf[i] = gpus.Count == 1 ? gpus[0] : "mixed";
                        newCache[jobs[i].Name] = Entry(KeyOf(i, true), results[i]!, jobSources[i], gpuOf[i]);
                    }
                }
                else
                {
                    // 되돌아감: 확대 대상 전부를 원본으로(이미 적중한 확대 출력도) — 일부만 확대된 채로 두지 않는다
                    foreach (var i in targets) { results[i] = null; newCache.Remove(jobs[i].Name); gpuOf[i] = null; }
                    plain.AddRange(targets);
                    useUpscale = false;
                }
            }
            else if (model is not null) plain = [.. Enumerable.Range(0, jobs.Count)];

            // 확대하지 않는 작업: 한 스레드에서 끝까지
            var pendingPlain = plain.Where(i => !TryHit(i, KeyOf(i, false))).ToList();
            hits += plain.Count - pendingPlain.Count;
            var errors = new string?[jobs.Count];
            Parallel.ForEach(pendingPlain, parallel, i =>
            {
                try { results[i] = jobs[i].Bake(bags[i], null); }
                catch (ImportCheckException ex) { errors[i] = ex.Message; }
                catch (InvalidDataException ex) { errors[i] = $"{jobs[i].Name}: 형식 오류 — {ex.Message}"; }
            });
            if (errors.Any(e => e is not null))
            {
                foreach (var e in errors.Where(e => e is not null)) Say($"대조 실패: {e}");
                return new ImportResult(ExitCodes.Check, null, report);
            }
            foreach (var i in pendingPlain) newCache[jobs[i].Name] = Entry(KeyOf(i, false), results[i]!, jobSources[i], null);
            bakeSw.Stop();

            // ── 성질 대조(타일셋마다, 원본 픽셀·SOL) ───────────────────
            var propProblems = new List<string>();
            foreach (var def in data.Tilesets.OrderBy(t => t.Id, StringComparer.Ordinal))
            {
                var entry = table.Tilesets.Tilesets.FirstOrDefault(t => t.Id == def.Id);
                if (entry is null || (entry.Bundle is { } b && skipped.ContainsKey(b))) continue;
                var srcBundle = entry.Bundle;
                var paths = TilesetSource.SourcePaths(entry);
                var dict = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in paths)
                {
                    var m = (diabdat.Exists(p) ? diabdat : srcBundle is { } sb && bundles.TryGetValue(sb, out var bm) ? bm : null)
                            ?? throw new InvalidOperationException($"{p} 없음");
                    dict[p] = m.Read(p)!;
                }
                propProblems.AddRange(ArenaBaker.CheckProps(def, TilesetSource.From(entry, new SourceBag(dict))));
            }
            if (propProblems.Count > 0)
            {
                foreach (var p in propProblems) Say($"대조 실패: {p}");
                return new ImportResult(ExitCodes.Check, null, report);
            }

            // ── 쓰기(임시 이름 → 바꿔치기) ─────────────────────────────
            var manifestFiles = new List<ManifestFile>();
            var soundInfos = new List<SoundInfo>();
            var soundSources = new SortedSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                var entry = newCache[j.Name];
                if (results[i] is { } res)
                    foreach (var f in res.Files) WriteAtomic(Path.Combine(req.OutDir, f.Path), f.Bytes);
                foreach (var f in entry.Files)
                    manifestFiles.Add(new ManifestFile { Path = f.Path, Size = f.Size, Sha256 = f.Sha256, Bundle = j.Bundle, Sources = entry.Sources, Scale = f.Scale });
                if (entry.Sounds.Count > 0)
                {
                    soundInfos.AddRange(entry.Sounds);
                    foreach (var s in entry.Sources) soundSources.Add(s);
                }
            }
            if (soundInfos.Count > 0)
            {
                var index = new SoundIndex { Format = SoundIndex.CurrentFormat, Files = [.. soundInfos.OrderBy(s => s.Path, StringComparer.Ordinal)] };
                var bytes = AssetJson.Serialize(index);
                WriteAtomic(Path.Combine(req.OutDir, AssetFormat.SoundIndexPath), bytes);
                manifestFiles.Add(new ManifestFile { Path = AssetFormat.SoundIndexPath, Size = bytes.Length, Sha256 = AssetManifest.Sha256Of(bytes), Sources = [.. soundSources] });
            }
            manifestFiles.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

            // 새 manifest 에 없는 d1/ 아래 파일은 지운다(manifest·import.log·.cache 빼고)
            var keep = manifestFiles.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Directory.EnumerateFiles(d1, "*", SearchOption.AllDirectories).ToList())
            {
                var rel = Path.GetRelativePath(req.OutDir, f).Replace('\\', '/');
                if (rel.StartsWith("d1/.cache/", StringComparison.Ordinal) || rel is AssetFormat.ManifestPath or "d1/import.log") continue;
                if (!keep.Contains(rel)) File.Delete(f);
            }
            foreach (var dir in Directory.EnumerateDirectories(d1, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length).ToList())
                if (!dir.Contains(Path.Combine(d1, ".cache"), StringComparison.OrdinalIgnoreCase) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);

            WriteAtomic(cachePath, AssetJson.Serialize(new CacheIndex(newCache)));
            if (useUpscale)
            {
                // 쓰인 키 목록 밖의 중간 캐시는 지운다
                foreach (var f in Directory.Exists(planeDir) ? Directory.EnumerateFiles(planeDir, "*.png").ToList() : [])
                    if (!usedPlaneKeys.Contains(Path.GetFileNameWithoutExtension(f))) File.Delete(f);
            }

            // ── manifest(마지막) ───────────────────────────────────────
            var sources = new List<ManifestSource>();
            foreach (var m in opened)
            {
                var bundle = bundles.FirstOrDefault(p => p.Value == m).Key;
                var isBundle = m != diabdat;
                sources.Add(new ManifestSource { Name = m.Name, Size = new FileInfo(m.Path).Length, Sha256 = AssetManifest.Sha256OfFile(m.Path), Bundle = isBundle ? bundle : null });
            }
            ManifestUpscale up;
            if (model is null) up = new ManifestUpscale { Status = UpscaleStatus.Off };
            else
            {
                var files = model.Files.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
                if (upscaleReason is null)
                {
                    var gpus = targets.Select(i => gpuOf[i] ?? gpuLabel).Distinct().ToList();
                    up = new ManifestUpscale
                    {
                        Status = UpscaleStatus.Done, Model = req.Upscale, Tool = UpscaleSetup.ToolName, Files = files,
                        OutputScale = table.Upscale.OutputScale, Gpu = gpus.Count == 0 ? gpuLabel : gpus.Count == 1 ? gpus[0] : "mixed",
                    };
                }
                else up = new ManifestUpscale { Status = UpscaleStatus.Fallback, Model = req.Upscale, Tool = UpscaleSetup.ToolName, Files = files, Reason = upscaleReason };
            }
            var manifest = new AssetManifest
            {
                Format = AssetFormat.Version, Tool = toolHash, Runtime = Environment.Version.ToString(), Sources = sources,
                SkippedBundles = [.. skipped.Values.OrderBy(s => s.Bundle)], TableHash = ImportTable.ComputeHash(req.TableDir),
                Upscale = up, Files = manifestFiles,
            };
            total.Stop();

            // ── 끝 보고 ────────────────────────────────────────────────
            Say($"가져오기 끝: 파일 {manifestFiles.Count}개 {manifestFiles.Sum(f => f.Size) / 1024.0 / 1024.0:0.0}MB, 작업 {jobs.Count}개(캐시 적중 {hits}), 걸린 시간 {total.Elapsed.TotalSeconds:0.0}초(원본 읽기 {readSw.Elapsed.TotalSeconds:0.0}초, 굽기 {bakeSw.Elapsed.TotalSeconds:0.0}초)");
            foreach (var g in jobs.GroupBy(j => j.Kind).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                var names = g.Select(j => j.Name).ToHashSet();
                var files = jobs.Select((j, i) => (j, i)).Where(x => names.Contains(x.j.Name)).SelectMany(x => newCache[x.j.Name].Files).ToList();
                Say($"  {g.Key}: 작업 {g.Count()}개, 파일 {files.Count}개 {files.Sum(f => f.Size) / 1024.0:0}KB");
            }
            foreach (var j in jobs)
            {
                var pages = newCache[j.Name].Files.Count(f => f.Path.Contains("_p", StringComparison.Ordinal) && f.Path.EndsWith(".png", StringComparison.Ordinal));
                if (pages > 0) Say($"  2페이지 이상: {j.Name} (+{pages})");
            }
            foreach (var s in sources) Say($"  원본 {s.Name} {s.Size} 바이트 SHA-256 {s.Sha256}");
            foreach (var b in ContentBundles.All)
                Say(skipped.TryGetValue(b, out var sk) ? $"  묶음 {ContentBundles.Name(b)}: 건너뜀({sk.Reason})" : $"  묶음 {ContentBundles.Name(b)}: 만듦");
            if (model is not null)
            {
                var applied = targets.Select(i => jobs[i].Name).ToList();
                Say($"  확대 대상 {applied.Count}개: {string.Join(", ", applied)}");
                Say($"  확대 진척 {planesHit + (upscaleReason is null ? planesNeeded - planesHit : 0)}/{planesNeeded} (중간 캐시 적중 {planesHit}, 묶음 {upscaleBatches}개{(upscaleTimes.Count > 0 ? ": " + string.Join(", ", upscaleTimes.Select(t => $"{t.TotalSeconds:0.0}초")) : "")})");
                if (device.Length > 0) Say($"  확대 장치 {device}");
            }
            foreach (var (b, sk) in skipped.OrderBy(p => p.Key))
                if (sk.Unreadable) warnings.Add($"경고: 헬파이어 묶음 {ContentBundles.Name(b)} 을 건너뜀 — {sk.Mpq} {(sk.Reason == ManifestSkippedBundle.ReasonUnreadable ? "를 열 수 없음" : "의 " + sk.Reason)}");
            if (model is not null && upscaleReason is not null) warnings.Add($"경고: AI 확대를 하지 못해 원본 픽셀로 만듦 — {upscaleReason}");
            foreach (var w in warnings) Say(w);

            var manifestBytes = AssetJson.Serialize(manifest);
            WriteAtomic(Path.Combine(req.OutDir, AssetFormat.ManifestPath), manifestBytes);
            File.WriteAllText(Path.Combine(d1, "import.log"), string.Join("\n", report) + "\n");
            var code = model is not null && upscaleReason is not null ? ExitCodes.UpscaleFallback : ExitCodes.Ok;
            return new ImportResult(code, manifest, report);
        }
        finally
        {
            foreach (var m in opened) m.Dispose();
        }
    }

    public static string Bypass(ContentBundle b) =>
        $"헬파이어 없이 가져오려면: … --skip-bundles {ContentBundles.Name(b)} (import-all.ps1 이면 -SkipBundles {ContentBundles.Name(b)})";

    private static CacheEntry Entry(string key, JobOutput r, IReadOnlyList<string> sources, string? gpu) => new(
        key, [.. r.Files.Select(f => new CacheFile(f.Path, AssetManifest.Sha256Of(f.Bytes), f.Bytes.Length, f.Scale))], sources, r.Sounds, gpu);

    private static Dictionary<string, CacheEntry> LoadCache(string path, bool force)
    {
        if (force || !File.Exists(path)) return new(StringComparer.Ordinal);
        try { return new(AssetJson.Read<CacheIndex>(path).Jobs, StringComparer.Ordinal); }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException) { return new(StringComparer.Ordinal); }
    }

    private static Dictionary<string, string> LoadPlaneIndex(string path, bool force)
    {
        if (force || !File.Exists(path)) return new(StringComparer.Ordinal);
        try { return new(AssetJson.Read<PlaneCacheIndex>(path).Gpu, StringComparer.Ordinal); }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException) { return new(StringComparer.Ordinal); }
    }

    private static void DeleteManifest(string outDir)
    {
        var m = Path.Combine(outDir, AssetFormat.ManifestPath);
        if (File.Exists(m)) File.Delete(m);
    }

    /// <summary>임시 이름으로 쓰고 바꿔치기.</summary>
    public static void WriteAtomic(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, bytes);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>도구 판 = 도구·Core 어셈블리 파일 바이트의 SHA-256(1.4 "캐시").</summary>
    public static string ToolHash()
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var asm in new[] { typeof(Importer).Assembly, typeof(ImportTable).Assembly })
            sha.AppendData(File.ReadAllBytes(asm.Location));
        return Convert.ToHexStringLower(sha.GetHashAndReset());
    }

    private static string Sha(string s) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
