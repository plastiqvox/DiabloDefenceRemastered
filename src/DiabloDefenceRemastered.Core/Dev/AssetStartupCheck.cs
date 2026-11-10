using System.Globalization;
using System.Text.Json;
using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Core.Dev;

/// <summary>시작 검사 결과(1.4 끝). <see cref="Problems"/> 가 비지 않으면 안내 화면(개발 옵션 실행은 로그 + 종료 코드 5).</summary>
public sealed record AssetCheckResult
{
    /// <summary>오류(자원 없음·낡음·크기 다름·배치 바뀜·교차 확인). 사람이 읽는 문장 하나씩.</summary>
    public IReadOnlyList<string> Problems { get; init; } = [];
    /// <summary>"꺼짐" 으로 센 파일(건너뛴 묶음이 만들 파일 — 오류 아님).</summary>
    public IReadOnlyList<string> DisabledFiles { get; init; } = [];
    /// <summary>manifest(없으면 null).</summary>
    public AssetManifest? Manifest { get; init; }
    public ContentAvailability Availability { get; init; } = ContentAvailability.All;
    /// <summary>꺼진 스테이지·직업.</summary>
    public DisabledContent Disabled { get; init; } = new([], [], []);
    /// <summary>그림 배율 s(7.1 — <c>upscale.status</c> 가 <c>done</c> 이면 <c>output_scale</c>).</summary>
    public int ArtScale { get; init; } = 1;
    /// <summary>안내 화면에 보일 명령 한 줄(기획 19.3).</summary>
    public string Command { get; init; } = AssetStartupCheck.ImportCommand;

    public bool Ok => Problems.Count == 0;

    /// <summary>
    /// 로그 <c>ASSETS</c> 줄의 자원 쪽(9절): <c>manifest=… format=… bundles=… skipped=… disabled=… upscale=… art_scale=…</c>.
    /// <c>stage=</c>·<c>textures=</c>·<c>vram_mb=</c> 는 텍스처를 읽는 쪽(WP5·WP6)이 덧붙인다.
    /// </summary>
    public string LogFields()
    {
        var m = Manifest;
        var made = m is null ? "-" : Join(m.Sources.Where(s => s.Bundle is not null).Select(s => ContentBundles.Name(s.Bundle!.Value)));
        var skipped = m is null ? "-" : Join(m.SkippedBundles.Select(s => ContentBundles.Name(s.Bundle)));
        var disabled = Join(Disabled.Stages.Select(s => s.Id).Concat(Disabled.Heroes.Select(h => h.Id)));
        var upscale = m is null ? "off" : m.Upscale.Status.ToString().ToLowerInvariant();
        return string.Create(CultureInfo.InvariantCulture,
            $"manifest={(m is null ? "missing" : "ok")} format={(m is null ? "-" : m.Format.ToString(CultureInfo.InvariantCulture))} bundles={made} skipped={skipped} disabled={disabled} upscale={upscale} art_scale={ArtScale}");
    }

    private static string Join(IEnumerable<string> items)
    {
        var s = string.Join(",", items);
        return s.Length == 0 ? "-" : s;
    }
}

/// <summary>
/// 실행 시작 자원 검사 (설계서 1.4 끝, WP7) — 게임 <c>App/AssetCheck</c> 가 부르는 순수 로직(그래픽 없음, 시험 가능).
/// <list type="number">
/// <item>manifest 가 없으면 "자원이 없다".</item>
/// <item>① 형식 판·② 표 해시(<see cref="AssetManifest.Staleness"/> — 표 해시는 출력 <c>ImportTable\</c>).</item>
/// <item>③ 정의가 가리키는 파일(<see cref="DefAssetNames.For"/>)이 manifest 에 없으면 오류 — 단 그 이름을 만드는 표 항목의 묶음(<see cref="ImportTable.OutputBundles"/>)이
/// <c>skipped_bundles</c> 에 있으면 "꺼짐". 꺼짐 파일에 닿는 주인 D 가 하나라도 켜져 있으면 오류(교차 확인). <c>d1/</c> 밖(원작 exe PNG)은 디스크에 있는지.</item>
/// <item>④ manifest 파일마다 디스크 크기, (v0.8) 파일 줄 <c>scale</c> 이 그림 배율 s 에 맞는지(<see cref="AssetScale.FitsArtScale"/>).</item>
/// <item>⑤ 맵 <c>arena.json</c> 의 <c>layout_hash</c> 가 저장소 배치로 계산한 값과 같은지(<c>scale</c> 도 ④ 와 같은 규칙).</item>
/// </list>
/// 확대 결과(<c>fallback</c> 포함)는 오류도 꺼짐도 아니다.
/// </summary>
public static class AssetStartupCheck
{
    public const string ImportCommand = @"tools\import-all.ps1 -Diabdat <diabdat.mpq> -Exe <DiabloDefence.exe>";

    /// <param name="assetsDir">게임 출력 <c>Assets\</c>.</param>
    /// <param name="importTableDir">게임 출력 <c>ImportTable\</c>.</param>
    /// <param name="arenas">정의 맵(과 거울판)의 펼친 격자.</param>
    public static AssetCheckResult Run(string assetsDir, string importTableDir, GameDefs defs, IReadOnlyList<ArenaGrid> arenas)
    {
        ArgumentNullException.ThrowIfNull(defs);
        var problems = new List<string>();
        var manifestPath = Path.Combine(assetsDir, AssetFormat.ManifestPath);
        if (!File.Exists(manifestPath))
            return new AssetCheckResult { Problems = [$"자원이 없다({AssetFormat.ManifestPath} 없음). {ImportCommand} 로 채운다"] };

        AssetManifest manifest;
        try
        {
            manifest = AssetManifest.Read(manifestPath);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return new AssetCheckResult { Problems = [$"manifest 를 읽지 못했다({ex.Message}). 가져오기를 다시 돌린다"] };
        }

        // ①②
        problems.AddRange(manifest.Staleness(ImportTable.ComputeHash(importTableDir)));
        var availability = ContentAvailability.From(manifest);
        var disabledContent = availability.Disabled(defs);
        var artScale = manifest.Upscale.ArtScale;

        // ③
        var table = ImportTable.Load(importTableDir);
        var bundles = table.Table?.OutputBundles(arenas) ?? new Dictionary<string, ContentBundle?>();
        if (table.Table is null) problems.Add($"가져오기 표를 읽지 못했다({string.Join("; ", table.Errors.Take(3))})");
        var have = new HashSet<string>(manifest.Files.Select(f => f.Path), StringComparer.Ordinal);
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var disabledFiles = new SortedSet<string>(StringComparer.Ordinal);
        var enabled = EnabledOwners(defs, availability);
        foreach (var group in DefAssetNames.For(defs, arenas).GroupBy(a => a.File))
        {
            var file = group.Key;
            if (!file.StartsWith(AssetFormat.Root + "/", StringComparison.Ordinal))
            {
                if (!File.Exists(Path.Combine(assetsDir, file.Replace('/', Path.DirectorySeparatorChar)))) missing.Add(file);
                continue;
            }
            if (have.Contains(file)) continue;
            var bundle = bundles.TryGetValue(file, out var b) ? b : null;
            if (bundle is { } bd && !availability.Has(bd))
            {
                var user = group.Select(a => a.Owner).FirstOrDefault(o => o is null || enabled.Contains(o));
                if (group.Any(a => a.Owner is null || enabled.Contains(a.Owner)))
                {
                    var who = user is null ? "기본 내용" : $"{Kind(user)} {user.Id}";
                    problems.Add($"{who} 이 꺼진 묶음 {ContentBundles.Name(bd)} 의 파일 {file} 을 씀");
                }
                else disabledFiles.Add(file);
                continue;
            }
            missing.Add(file);
        }
        if (missing.Count > 0)
            problems.Add($"없는 파일 {missing.Count}개: {string.Join(", ", missing.Take(20))}{(missing.Count > 20 ? " …" : "")}. 가져오기를 다시 돌린다");

        // ④
        var wrongSize = new List<string>();
        var wrongScale = new List<string>();
        foreach (var f in manifest.Files)
        {
            var full = Path.Combine(assetsDir, f.Path.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(full);
            if (!info.Exists || info.Length != f.Size) wrongSize.Add(f.Path);
            if (!AssetScale.FitsArtScale(f.Scale, artScale)) wrongScale.Add($"{f.Path}(scale {f.Scale})");
        }
        if (wrongSize.Count > 0)
            problems.Add($"manifest 와 크기가 다르거나 없는 파일 {wrongSize.Count}개: {string.Join(", ", wrongSize.Take(20))}{(wrongSize.Count > 20 ? " …" : "")} — 섞인 출력 폴더. 가져오기를 다시 돌린다");
        if (wrongScale.Count > 0)
            problems.Add($"그림 배율 {artScale} 로 그릴 수 없는 파일 {wrongScale.Count}개: {string.Join(", ", wrongScale.Take(10))}");

        // ⑤
        foreach (var grid in arenas)
        {
            var rel = $"{grid.Map.Art.TrimEnd('/')}/{ArenaArt.FileName}";
            if (!have.Contains(rel)) continue;   // ③ 이 없음·꺼짐으로 셌다
            var full = Path.Combine(assetsDir, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) continue;    // ④ 가 셌다
            try
            {
                var art = ArenaArt.Read(full);
                if (!string.Equals(art.LayoutHash, grid.LayoutHash, StringComparison.Ordinal))
                    problems.Add($"맵 배치가 바뀌었다({grid.Map.Id}) — 가져오기를 다시 돌린다");
                if (!AssetScale.FitsArtScale(art.Scale, artScale))
                    problems.Add($"맵 그림 {rel} 의 scale {art.Scale} 을 그림 배율 {artScale} 로 그릴 수 없다");
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                problems.Add($"맵 그림 {rel} 을 읽지 못했다({ex.Message})");
            }
        }

        return new AssetCheckResult
        {
            Problems = problems,
            DisabledFiles = [.. disabledFiles],
            Manifest = manifest,
            Availability = availability,
            Disabled = disabledContent,
            ArtScale = artScale,
        };
    }

    /// <summary>
    /// 켜진 주인 D: 켜진 스테이지·직업, 켜진 스테이지에서 나오는 적(<see cref="StageReach"/>), 켜진 직업·스테이지가 쓸 수 있는 무기(묶음이 모두 있음),
    /// 켜진 스테이지의 음악 중 <c>fallback</c> 이 없는 것(있으면 대체되므로 뺀다).
    /// </summary>
    private static HashSet<IDef> EnabledOwners(GameDefs defs, ContentAvailability availability)
    {
        var set = new HashSet<IDef>(ReferenceEqualityComparer.Instance);
        foreach (var s in defs.Stages.All)
        {
            if (availability.MissingFor(s).Count > 0) continue;
            set.Add(s);
            foreach (var e in StageReach.Enemies(s, defs)) set.Add(e);
            if (defs.SoundBindings.Music.TryGetValue(s.Id, out var m) && m.IsBound && m.Def.Fallback is null) set.Add(m.Def);
        }
        foreach (var h in defs.HeroClasses.All)
            if (availability.MissingFor(h).Count == 0) set.Add(h);
        foreach (var w in defs.Weapons.All)
            if (availability.Missing(w.Requires).Count == 0) set.Add(w);
        foreach (var m in defs.Music.All)
            if (m.Requires.Count == 0) set.Add(m);
        return set;
    }

    private static string Kind(IDef d) => d switch
    {
        StageDef => "스테이지",
        HeroClassDef => "직업",
        EnemyDef => "적",
        WeaponDef => "무기",
        MusicDef => "음악",
        _ => d.GetType().Name,
    };
}
