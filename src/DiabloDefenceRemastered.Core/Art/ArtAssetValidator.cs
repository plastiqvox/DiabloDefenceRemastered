using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Core.Art;

/// <summary>
/// 실제 자원 대조 13.5-7 (설계서 6.2 7단계·6.14, 소유 WP5) — <b>자원이 있을 때만</b>. 자원 뿌리에 <c>d1/manifest.json</c> 이 없거나
/// 형식 판이 다르면(낡음 — 시작 검사 ①, WP7 이 따로 막는다) 아무것도 하지 않는다. 판이 같을 때만 어긋남을 오류로 낸다(1.3 "코드 회귀와 다시 가져오기를 섞지 않는다").
/// <list type="bullet">
/// <item>atlas: 메타(<c>&lt;이름&gt;.json</c>)가 있고 <see cref="SpriteMeta.Validate"/> 통과, 그림 배율이 s 에 맞음(<see cref="AssetScale.FitsArtScale"/>),
/// 정의의 <c>action</c> 이 있고 칸 ÷ 메타 <c>scale</c> = 정의 <c>frame</c>·방향·프레임 = 정의, 페이지마다 그 그림을 쓰는 묶음의 모든 변종 PNG IHDR = 메타 페이지.</item>
/// <item>grid: PNG IHDR = <c>sheet</c> × manifest 의 그 파일 <c>scale</c>.</item>
/// <item>맵: <c>arena.json</c> 이 <see cref="ArenaArt.Validate"/>(<c>layout_hash</c> = 저장소 배치) 통과, 배율이 s 에 맞음, 기둥 아틀라스 메타 통과. 거울판(<c>mirror: true</c>)이면 <c>&lt;art&gt;@m</c> 도.</item>
/// </list>
/// 오류의 파일 = 자원 뿌리 기준 경로, JSON 경로 = <c>$</c>(자원 파일 안 위치가 아니라 그 파일 자체).
/// </summary>
public sealed class ArtAssetValidator(string assetRoot) : IDataValidator
{
    public string Name => "art_assets";

    /// <summary>자원 뿌리(<c>Assets</c> — 그 아래 <c>d1/manifest.json</c>).</summary>
    public string AssetRoot { get; } = assetRoot;

    /// <summary>대조를 했는가(자원이 있고 판이 맞았다). 시험이 "건너뜀" 과 "통과" 를 가른다.</summary>
    public bool Ran { get; private set; }

    public void Validate(GameDefs defs, ValidationContext ctx)
    {
        Ran = false;
        var manifestPath = Path.Combine(AssetRoot, AssetFormat.ManifestPath);
        if (!File.Exists(manifestPath)) return;
        AssetManifest manifest;
        try { manifest = AssetManifest.Read(manifestPath); }
        catch (Exception e) when (e is IOException or System.Text.Json.JsonException)
        {
            ctx.Add(AssetFormat.ManifestPath, "$", $"manifest 를 읽지 못했다: {e.Message}");
            return;
        }
        if (manifest.Format != AssetFormat.Version) return;   // 낡음 — 시작 검사(WP7)·시험 건너뜀이 맡는다
        Ran = true;

        var s = manifest.Upscale.ArtScale;
        var variants = VariantsBySprite(defs);
        var metas = new Dictionary<string, SpriteMeta?>(StringComparer.Ordinal);
        var checkedPages = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sprite in defs.Sprites.All)
        {
            if (sprite.Source == SpriteSource.Atlas) CheckAtlas(sprite, s, variants, metas, checkedPages, ctx);
            else CheckGrid(sprite, manifest, ctx);
        }

        foreach (var map in defs.Maps.All)
        {
            CheckMap(map, s, ctx);
            if (map.Mirrored is { } mirror) CheckMap(mirror, s, ctx);
        }
    }

    /// <summary>그림 ID → 그 그림을 클립으로 쓰는 묶음들의 변종 합(원색 <c>""</c> 은 늘). 묶음에 안 든 그림(마법·아이콘)은 원색만.</summary>
    internal static IReadOnlyDictionary<string, SortedSet<string>> VariantsBySprite(GameDefs defs)
    {
        var map = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var set in defs.SpriteSets.All)
            foreach (var kind in Enum.GetValues<ClipKind>())
            {
                if (set.Clips.Get(kind) is not { } clip) continue;
                if (!map.TryGetValue(clip.Sprite.Id, out var vs)) map[clip.Sprite.Id] = vs = new SortedSet<string>(StringComparer.Ordinal) { "" };
                foreach (var v in set.Variants) vs.Add(v);
            }
        return map;
    }

    private void CheckAtlas(SpriteDef sprite, int s, IReadOnlyDictionary<string, SortedSet<string>> variants,
                            Dictionary<string, SpriteMeta?> metas, HashSet<string> checkedPages, ValidationContext ctx)
    {
        var metaRel = SpriteMeta.MetaFile(sprite.File);
        if (!metas.TryGetValue(metaRel, out var meta))
        {
            meta = ReadMeta(metaRel, s, ctx);
            metas[metaRel] = meta;
        }
        if (meta is null) return;

        if (!meta.TryGetAction(sprite.Action, out var action))
        {
            ctx.Add(metaRel, "$", $"그림 '{sprite.Id}': 메타에 동작 '{sprite.Action ?? "(이름 없음 — 동작이 하나여야)"}' 이 없다");
            return;
        }
        var m = meta.Scale;
        if (action.Cell.X % m != 0 || action.Cell.Y % m != 0 || new Int2(action.Cell.X / m, action.Cell.Y / m) != sprite.Frame)
            ctx.Add(metaRel, "$", $"그림 '{sprite.Id}': 칸 {action.Cell} ÷ scale {m} ≠ 정의 frame {sprite.Frame}");
        if (action.Directions != sprite.Directions)
            ctx.Add(metaRel, "$", $"그림 '{sprite.Id}': 방향 {action.Directions} ≠ 정의 {sprite.Directions}");
        if (action.Frames != sprite.Frames)
            ctx.Add(metaRel, "$", $"그림 '{sprite.Id}': 프레임 {action.Frames} ≠ 정의 {sprite.Frames}");

        IEnumerable<string> vs = variants.TryGetValue(sprite.Id, out var set) ? set : [""];
        foreach (var v in vs)
            for (var p = 0; p < meta.Pages.Count; p++)
            {
                var page = SpriteMeta.PageFile(sprite.File, p, v);
                if (!checkedPages.Add(page)) continue;
                CheckPng(page, meta.Pages[p], ctx);
            }
    }

    private SpriteMeta? ReadMeta(string metaRel, int s, ValidationContext ctx)
    {
        var path = Path.Combine(AssetRoot, metaRel);
        if (!File.Exists(path)) { ctx.Add(metaRel, "$", "아틀라스 메타가 없다"); return null; }
        SpriteMeta meta;
        try { meta = SpriteMeta.Read(path); }
        catch (Exception e) when (e is IOException or System.Text.Json.JsonException)
        {
            ctx.Add(metaRel, "$", $"아틀라스 메타를 읽지 못했다: {e.Message}");
            return null;
        }
        foreach (var error in meta.Validate()) ctx.Add(metaRel, "$", error);
        if (!AssetScale.FitsArtScale(meta.Scale, s))
            ctx.Add(metaRel, "$", $"그림 배율 scale {meta.Scale} 는 그림 배율 s = {s} 로 그릴 수 없다({{1, s}} 안이고 s 를 나눠야 — 7.1)");
        return meta;
    }

    private void CheckGrid(SpriteDef sprite, AssetManifest manifest, ValidationContext ctx)
    {
        if (sprite.Sheet.X <= 0 || sprite.Sheet.Y <= 0) return;   // sheet 없는 grid 는 정의끼리 검사(WP7)가 잡는다
        var scale = manifest.TryGetFile(sprite.File, out var file) ? file.Scale : 1;
        CheckPng(sprite.File, new Int2(sprite.Sheet.X * scale, sprite.Sheet.Y * scale), ctx);
    }

    private void CheckMap(MapDef map, int s, ValidationContext ctx)
    {
        if (string.IsNullOrEmpty(map.Art)) return;
        var art = map.Art.TrimEnd('/');
        var arenaRel = $"{art}/{ArenaArt.FileName}";
        var path = Path.Combine(AssetRoot, arenaRel);
        if (!File.Exists(path)) { ctx.Add(arenaRel, "$", $"맵 '{map.Id}' 의 그림 목록이 없다"); return; }

        ArenaArt arena;
        ArenaGrid grid;
        try
        {
            arena = ArenaArt.Read(path);
            grid = ArenaLayout.Expand(map);
        }
        catch (Exception e) when (e is IOException or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
        {
            ctx.Add(arenaRel, "$", $"맵 '{map.Id}' 의 그림 목록을 대조하지 못했다: {e.Message}");
            return;
        }
        foreach (var error in arena.Validate(grid)) ctx.Add(arenaRel, "$", error);
        if (!AssetScale.FitsArtScale(arena.Scale, s))
            ctx.Add(arenaRel, "$", $"그림 배율 scale {arena.Scale} 는 그림 배율 s = {s} 로 그릴 수 없다(7.1)");

        var columnsRel = $"{art}/{arena.Columns}.json";
        var columns = ReadMeta(columnsRel, s, ctx);
        if (columns is not null)
        {
            if (columns.Scale != arena.Scale) ctx.Add(columnsRel, "$", $"기둥 아틀라스 scale {columns.Scale} ≠ arena.json scale {arena.Scale}");
            var frames = columns.Actions.Count == 1 ? columns.Actions.Values.First().Frames : 0;
            foreach (var cell in arena.Cells)
                if (cell.Frame > frames) { ctx.Add(arenaRel, "$", $"칸 ({cell.Tx},{cell.Ty}) frame {cell.Frame} 가 기둥 아틀라스 프레임 {frames}개 밖이다"); break; }
            for (var p = 0; p < columns.Pages.Count; p++)
                CheckPng(SpriteMeta.PageFile($"{art}/{arena.Columns}.png", p), columns.Pages[p], ctx);
        }
        var chunkPx = ArenaArt.ChunkSize * arena.Scale;
        foreach (var chunk in arena.Floor)
            CheckPng($"{art}/{chunk.File}", new Int2(chunkPx, chunkPx), ctx);
    }

    private void CheckPng(string rel, Int2 expected, ValidationContext ctx)
    {
        var path = Path.Combine(AssetRoot, rel);
        if (!File.Exists(path)) { ctx.Add(rel, "$", "그림 파일이 없다"); return; }
        Int2 size;
        try { size = PngHeader.Read(path); }
        catch (Exception e) when (e is IOException or InvalidDataException)
        {
            ctx.Add(rel, "$", e.Message);
            return;
        }
        if (size != expected) ctx.Add(rel, "$", $"PNG 크기 {size} ≠ 기대 {expected}");
    }
}
