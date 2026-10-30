using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Map;

/// <summary>읽을 정의 원소 하나: 파일(Data 기준 상대 경로, '/')·JSON 경로(<c>$.maps[0]</c>)·종류(<c>tilesets</c>/<c>maps</c>)·원소.</summary>
public readonly record struct MapDataElement(string File, string JsonPath, string Kind, JsonNode? Node);

/// <summary>읽은 결과. 맵의 <c>Tileset</c> 은 연결돼 있다(오류가 없을 때). 형으로 못 바꾼 원소는 빠진다.</summary>
public sealed record MapDataResult(IReadOnlyList<MapDef> Maps, IReadOnlyList<TilesetDef> Tilesets, IReadOnlyList<DataError> Errors)
{
    public bool HasErrors
    {
        get
        {
            foreach (var e in Errors)
                if (e.Severity == DataSeverity.Error) return true;
            return false;
        }
    }
}

/// <summary>
/// <c>tilesets</c>·<c>maps</c> 두 종류만 읽는 공용 리더 (설계서 1.4·6.2, 계약 WP1b). 가져오기 도구(WP0)와 게임 로더(WP7)가 같은 함수로 같은 record 를 만든다
/// → 같은 저장소면 <c>LayoutHash</c> 가 같다.
/// <list type="bullet">
/// <item>공용 STJ 옵션·변환기(<see cref="DefJson"/>) — 모르는 속성 오류.</item>
/// <item><c>extends</c>·<c>abstract</c>·<c>*_add</c>·<c>*_patch</c> 를 쓰면 오류(병합 규칙이 두 벌이 될 일이 없게).</item>
/// <item><c>MapDef.Tileset</c> 을 ID 로 연결, 배치 풀기(13.5-4 배치 쪽)와 타일셋 검사(13.5-12)까지.</item>
/// <item>다른 종류의 데이터 오류·부품 등록과 무관하다. 단 JSON 문법이 깨진 파일은 두 종류가 있는지 알 수 없으므로 오류로 낸다.</item>
/// </list>
/// </summary>
public static class MapDataReader
{
    public const string TilesetsKind = "tilesets";
    public const string MapsKind = "maps";

    private static readonly JsonSerializerOptions Options = DefJson.CreateOptions(new TilesetRefConverter());

    /// <summary><paramref name="dataDir"/> 아래 <c>**/*.json</c> 을 경로 서수 순으로 읽어 두 종류만 모은다(도구·시험).</summary>
    public static MapDataResult Read(string dataDir)
    {
        var errors = new List<DataError>();
        var elements = new List<MapDataElement>();
        if (!Directory.Exists(dataDir))
        {
            errors.Add(new DataError("", "$", $"Data 폴더가 없다: {dataDir}"));
            return new MapDataResult([], [], errors);
        }

        var files = Directory.EnumerateFiles(dataDir, "*.json", SearchOption.AllDirectories)
            .Select(f => (Full: f, Rel: Path.GetRelativePath(dataDir, f).Replace('\\', '/')))
            .OrderBy(f => f.Rel, StringComparer.Ordinal)
            .ToList();
        foreach (var (full, rel) in files)
        {
            // 중복 키를 먼저 본다: JsonObject 는 첫 속성 접근에서 ArgumentException 을 던지고, 형으로 읽는 사전은 마지막 값으로 덮는다
            try
            {
                var text = File.ReadAllText(full);
                if (DefJson.FindDuplicateKey(text) is { } dup)
                {
                    errors.Add(new DataError(rel, dup, $"키가 두 번 있다: {DefJson.LastKey(dup)}"));
                    continue;
                }
                if (DefJson.ParseNode(text) is not JsonObject obj)
                {
                    errors.Add(new DataError(rel, "$", "파일은 종류 이름을 키로 하는 객체여야 한다"));
                    continue;
                }
                foreach (var kind in new[] { TilesetsKind, MapsKind })
                {
                    if (!obj.TryGetPropertyValue(kind, out var list)) continue;
                    if (list is not JsonArray arr)
                    {
                        errors.Add(new DataError(rel, $"$.{kind}", $"{kind} 는 배열이어야 한다"));
                        continue;
                    }
                    for (var i = 0; i < arr.Count; i++)
                        elements.Add(new MapDataElement(rel, $"$.{kind}[{i}]", kind, arr[i]));
                }
            }
            catch (JsonException ex)
            {
                errors.Add(new DataError(rel, "$", $"JSON 문법 오류: {ex.Message}"));
            }
            catch (ArgumentException ex)
            {
                errors.Add(new DataError(rel, "$", $"키가 두 번 있다: {ex.Message}"));
            }
        }

        var result = Read(elements);
        if (errors.Count == 0) return result;
        errors.AddRange(result.Errors);
        return result with { Errors = errors };
    }

    /// <summary>로더(WP7)가 1~2단계에서 모은 두 종류의 원소를 넘긴다(파일·경로 그대로 → 오류 위치가 같다).</summary>
    public static MapDataResult Read(IEnumerable<MapDataElement> elements)
    {
        var errors = new List<DataError>();
        var tilesets = new List<(TilesetDef Def, MapDataElement Src)>();
        var maps = new List<(MapDef Def, MapDataElement Src)>();
        var tilesetIds = new Dictionary<string, TilesetDef>(StringComparer.Ordinal);
        var mapIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var el in elements)
        {
            if (el.Node is not JsonObject obj)
            {
                errors.Add(new DataError(el.File, el.JsonPath, "정의 원소는 객체여야 한다"));
                continue;
            }
            try
            {
                // 로더가 넘긴 원소도 중복 키를 본다(원소 안 사전 — props 등 — 은 형으로 읽으면 마지막 값이 조용히 이긴다)
                if (DefJson.FindDuplicateKey(obj) is { } dup)
                {
                    errors.Add(new DataError(el.File, DefJson.JoinPath(el.JsonPath, dup), $"키가 두 번 있다: {DefJson.LastKey(dup)}"));
                    continue;
                }
                if (FindMergeKey(obj) is { } mergeKey)
                {
                    errors.Add(new DataError(el.File, $"{el.JsonPath}.{mergeKey}", "tilesets·maps 에는 extends·abstract·*_add·*_patch 를 쓸 수 없다"));
                    continue;
                }
                if (el.Kind == TilesetsKind)
                {
                    var def = obj.Deserialize<TilesetDef>(Options) ?? throw new JsonException("null");
                    if (!tilesetIds.TryAdd(def.Id, def)) { errors.Add(new DataError(el.File, $"{el.JsonPath}.id", $"타일셋 ID '{def.Id}' 가 두 번 있다")); continue; }
                    tilesets.Add((def, el));
                }
                else if (el.Kind == MapsKind)
                {
                    var def = obj.Deserialize<MapDef>(Options) ?? throw new JsonException("null");
                    if (!mapIds.Add(def.Id)) { errors.Add(new DataError(el.File, $"{el.JsonPath}.id", $"맵 ID '{def.Id}' 가 두 번 있다")); continue; }
                    maps.Add((def, el));
                }
                else errors.Add(new DataError(el.File, el.JsonPath, $"MapDataReader 는 tilesets·maps 만 읽는다('{el.Kind}')"));
            }
            catch (JsonException ex)
            {
                errors.Add(new DataError(el.File, DefJson.JoinPath(el.JsonPath, ex.Path), ex.Message));
            }
            catch (ArgumentException ex)
            {
                errors.Add(new DataError(el.File, el.JsonPath, $"키가 두 번 있다: {ex.Message}"));
            }
        }

        var layoutErrors = new List<LayoutError>();
        foreach (var (def, src) in tilesets)
        {
            layoutErrors.Clear();
            ArenaLayout.ValidateTileset(def, layoutErrors);
            foreach (var e in layoutErrors) errors.Add(new DataError(src.File, DefJson.JoinPath(src.JsonPath, e.Path), e.Message));
        }

        foreach (var (def, src) in maps)
        {
            if (!tilesetIds.TryGetValue(def.Tileset.Id, out var ts))
            {
                errors.Add(new DataError(src.File, $"{src.JsonPath}.tileset", $"타일셋 '{def.Tileset.Id}' 가 없다"));
                continue;
            }
            if (!def.Tileset.IsBound) def.Tileset.Bind(ts);

            // art 는 Assets 뿌리 기준 '/' 상대 경로 — 아니면 ArenaArt.OutputNames 가 manifest·13.5-10 과 조용히 어긋난 이름을 만든다
            if (!AssetFormat.IsValidRelativePath(def.Art))
                errors.Add(new DataError(src.File, $"{src.JsonPath}.art", $"그림 폴더 '{def.Art}' 는 Assets 기준 '/' 상대 경로다(비지 않음, 역슬래시·'..'·앞뒤 '/' 없음)"));

            var entryIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < def.Entries.Count; i++)
                if (!entryIds.Add(def.Entries[i].Id))
                    errors.Add(new DataError(src.File, $"{src.JsonPath}.entries[{i}].id", $"입구 ID '{def.Entries[i].Id}' 가 두 번 있다"));

            layoutErrors.Clear();
            var grid = ArenaLayout.TryExpand(def, ts, layoutErrors);
            foreach (var e in layoutErrors) errors.Add(new DataError(src.File, DefJson.JoinPath(src.JsonPath, e.Path), e.Message));
            if (grid is null) continue;

            var start = def.HeroStart.Tile;
            if (!grid.IsInside(IsoMath.RoundToCell(start.Tx), IsoMath.RoundToCell(start.Ty)))
                errors.Add(new DataError(src.File, $"{src.JsonPath}.hero_start.tile", $"주인공 시작 {start} 가 {grid.Size}×{grid.Size} 격자 밖이다"));
        }

        return new MapDataResult([.. maps.Select(m => m.Def)], [.. tilesets.Select(t => t.Def)], errors);
    }

    /// <summary>원소 최상위의 병합 키(<c>extends</c>·<c>abstract</c>·<c>*_add</c>·<c>*_patch</c>). 없으면 null.</summary>
    private static string? FindMergeKey(JsonObject obj)
    {
        foreach (var (key, _) in obj)
            if (key is "extends" or "abstract" || key.EndsWith("_add", StringComparison.Ordinal) || key.EndsWith("_patch", StringComparison.Ordinal))
                return key;
        return null;
    }

    /// <summary><c>"tileset": "l1"</c> → 아직 연결 안 된 참조. 연결은 위에서 한 번.</summary>
    private sealed class TilesetRefConverter : JsonConverter<DefRef<TilesetDef>>
    {
        public override DefRef<TilesetDef> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("타일셋 ID 문자열이 와야 한다");
            return new DefRef<TilesetDef>(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DefRef<TilesetDef> value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.Id);
    }
}
