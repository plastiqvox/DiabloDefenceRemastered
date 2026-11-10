using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Data.Loading;

/// <summary>로더 선택 (설계서 6.2).</summary>
public sealed record DataLoadOptions
{
    public static DataLoadOptions Strict { get; } = new();

    /// <summary>
    /// 등록 안 된 부품 <c>(종류, type)</c> 을 오류로 내지 않고 JSON 그대로 담는다. <b>로더 단위 시험 전용</b> — 게임·실제 데이터 시험은 늘 엄격하다(6.2).
    /// </summary>
    public bool AllowUnknownParts { get; init; }
}

/// <summary>데이터 파일 하나(Data 기준 상대 경로 <c>'/'</c>, 원문). 시험은 이것으로 파일 목록을 직접 준다.</summary>
public readonly record struct DataFile(string RelPath, string Text);

/// <summary>로딩 결과: 정의 + 오류·경고 전부(오류가 있어도 끝까지 돈 결과).</summary>
public sealed record DataLoadResult(GameDefs Defs, IReadOnlyList<DataError> Errors)
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
/// 데이터 로더 (설계서 6.2, WP7). <c>Data/**/*.json</c> 을 경로 서수 순으로 읽어 7단계를 돌고 <see cref="GameDefs"/> 와 모든 오류를 돌려준다.
/// <list type="number">
/// <item>파일 목록·파싱(중복 키·문법 오류 → 그 파일만 빠짐)</item>
/// <item>종류별로 모으기·ID 중복·<c>rules</c>·<c>sound_bindings</c> 하위 키 합치기(같은 하위 키 두 번은 오류)</item>
/// <item><c>extends</c>·<c>*_add</c>·<c>*_patch</c>·페이즈 풀기(<see cref="JsonMerge"/>) — <c>tilesets</c>·<c>maps</c> 는 <see cref="MapDataReader"/> 가 읽음</item>
/// <item>항목마다 형 변환(부품은 레지스트리로, 스테이지 이벤트의 <c>at</c> → <see cref="PartDef.At"/>)</item>
/// <item>연결(<see cref="DefRef{T}"/>, 비슷한 ID 제안), 부품 매개변수 검사, <c>[PartType]</c> 등록 검사, <c>RoomDef.Number</c> 채우기</item>
/// <item>의미 검사(<see cref="DataRules"/>)</item>
/// <item>등록된 <see cref="IDataValidator"/> 들(등록 순서)</item>
/// </list>
/// </summary>
public static class DataLoader
{
    private static readonly HashSet<string> ObjectKinds = new(StringComparer.Ordinal) { "rules", "sound_bindings" };

    /// <summary>폴더에서 읽는다(<c>**/*.json</c>). 폴더가 없으면 오류 하나.</summary>
    public static DataLoadResult Load(string dataDir, PartRegistry registry, IReadOnlyList<IDataValidator> validators, DataLoadOptions? options = null)
    {
        if (!Directory.Exists(dataDir))
            return new DataLoadResult(GameDefs.Empty, [new DataError("", "$", $"Data 폴더가 없다: {dataDir}")]);
        var files = Directory.EnumerateFiles(dataDir, "*.json", SearchOption.AllDirectories)
            .Select(f => new DataFile(Path.GetRelativePath(dataDir, f).Replace('\\', '/'), File.ReadAllText(f)))
            .ToList();
        return Load(files, registry, validators, options);
    }

    /// <summary>파일 목록에서 읽는다. 주는 순서와 상관없이 경로 서수 순으로 정렬해 읽는다(3.4).</summary>
    public static DataLoadResult Load(IEnumerable<DataFile> files, PartRegistry registry, IReadOnlyList<IDataValidator> validators, DataLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(validators);
        options ??= DataLoadOptions.Strict;
        var errors = new List<DataError>();

        // 1·2단계
        var collected = Collect(files, errors);

        // 3단계 + 4단계
        var json = LoaderJson.Create(registry, options);
        var maps = MapDataReader.Read(collected.MapElements);
        errors.AddRange(maps.Errors);

        var rules = ReadObjectKind<RulesDef>(collected.Rules, json, errors);
        var bindings = ReadObjectKind<SoundBindingsDef>(collected.SoundBindings, json, errors);

        var tables = new Dictionary<string, List<IDef>>(StringComparer.Ordinal);
        foreach (var (kind, type) in DefKinds.Types)
        {
            if (kind is MapDataReader.TilesetsKind or MapDataReader.MapsKind) continue;
            tables[kind] = ReadKind(kind, type, collected.Elements(kind), json, errors);
        }

        var defs = new GameDefs
        {
            Rules = rules ?? new RulesDef(),
            Sprites = Table<SpriteDef>(tables["sprites"]),
            SpriteSets = Table<SpriteSetDef>(tables["sprite_sets"]),
            Effects = Table<EffectDef>(tables["effects"]),
            Projectiles = Table<ProjectileDef>(tables["projectiles"]),
            Tilesets = new DefTable<TilesetDef>(maps.Tilesets),
            Maps = new DefTable<MapDef>(maps.Maps),
            Enemies = Table<EnemyDef>(tables["enemies"]),
            Affixes = Table<AffixDef>(tables["affixes"]),
            Buffs = Table<BuffDef>(tables["buffs"]),
            Weapons = Table<WeaponDef>(tables["weapons"]),
            Passives = Table<PassiveDef>(tables["passives"]),
            Evolutions = Table<EvolutionDef>(tables["evolutions"]),
            Pickups = Table<PickupDef>(tables["pickups"]),
            DropTables = Table<DropTableDef>(tables["drop_tables"]),
            Stages = Table<StageDef>(tables["stages"]),
            Sounds = Table<SoundDef>(tables["sounds"]),
            Music = Table<MusicDef>(tables["music"]),
            SoundBindings = bindings ?? new SoundBindingsDef(),
            HeroClasses = Table<HeroClassDef>(tables["hero_classes"]),
            Rewards = Table<RewardDef>(tables["rewards"]),
            ItemBases = Table<ItemBaseDef>(tables["items"]),
            ItemAffixes = Table<ItemAffixDef>(tables["item_affixes"]),
            Merchants = Table<MerchantDef>(tables["merchants"]),
        };

        // 5단계: 연결·부품 검사
        DefLinker.Link(defs, collected, registry, errors);

        // 6단계: 의미 검사
        var ctx = new ValidationContext { Defs = defs };
        DataRules.Check(defs, collected, ctx);
        errors.AddRange(ctx.Errors);

        // 7단계: 등록된 검사기(예외는 그 검사기의 오류 하나로)
        foreach (var v in validators)
        {
            var vctx = new ValidationContext { Defs = defs };
            try
            {
                v.Validate(defs, vctx);
            }
            catch (Exception ex)
            {
                vctx.Add("", "$", $"검사기 {v.Name} 가 예외로 멈췄다: {ex.GetType().Name}: {ex.Message}");
            }
            errors.AddRange(vctx.Errors);
        }

        return new DataLoadResult(defs, errors);
    }

    private static DefTable<T> Table<T>(List<IDef> items) where T : class, IDef => new(items.Cast<T>());

    // ── 1·2단계 ─────────────────────────────────────────────

    private static Collected Collect(IEnumerable<DataFile> files, List<DataError> errors)
    {
        var c = new Collected();
        foreach (var file in files.OrderBy(f => f.RelPath, StringComparer.Ordinal))
        {
            JsonObject root;
            try
            {
                if (DefJson.FindDuplicateKey(file.Text) is { } dup)
                {
                    errors.Add(new DataError(file.RelPath, dup, $"키가 두 번 있다: {DefJson.LastKey(dup)}"));
                    continue;
                }
                if (DefJson.ParseNode(file.Text) is not JsonObject obj)
                {
                    errors.Add(new DataError(file.RelPath, "$", "파일은 종류 이름을 키로 하는 객체여야 한다"));
                    continue;
                }
                root = obj;
            }
            catch (JsonException ex)
            {
                errors.Add(new DataError(file.RelPath, "$", $"JSON 문법 오류: {ex.Message}"));
                continue;
            }

            foreach (var (kind, node) in root)
            {
                var path = $"$.{kind}";
                if (ObjectKinds.Contains(kind))
                {
                    if (node is not JsonObject sub)
                    {
                        errors.Add(new DataError(file.RelPath, path, $"{kind} 는 객체여야 한다"));
                        continue;
                    }
                    var target = kind == "rules" ? c.Rules : c.SoundBindings;
                    foreach (var (key, value) in sub)
                    {
                        if (target.TryGetValue(key, out var first))
                        {
                            errors.Add(new DataError(file.RelPath, $"{path}.{key}", $"{kind} 하위 키 '{key}' 가 두 파일에 있다(처음: {first.File})"));
                            continue;
                        }
                        target[key] = new RawElement(file.RelPath, $"{path}.{key}", value);
                    }
                    continue;
                }
                if (!DefKinds.Types.ContainsKey(kind))
                {
                    errors.Add(new DataError(file.RelPath, path, $"모르는 종류 '{kind}'{IdSuggest.Hint(kind, GameDefs.Kinds)}"));
                    continue;
                }
                if (node is not JsonArray arr)
                {
                    errors.Add(new DataError(file.RelPath, path, $"{kind} 는 배열이어야 한다"));
                    continue;
                }
                for (var i = 0; i < arr.Count; i++)
                    c.Add(kind, new RawElement(file.RelPath, $"{path}[{i}]", arr[i]));
            }
        }
        return c;
    }

    // ── 3·4단계 ─────────────────────────────────────────────

    private static T? ReadObjectKind<T>(Dictionary<string, RawElement> parts, JsonSerializerOptions json, List<DataError> errors) where T : class, new()
    {
        if (parts.Count == 0) return null;
        object result = new T();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => JsonNamingPolicy.SnakeCaseLower.ConvertName(p.Name), StringComparer.Ordinal);
        foreach (var (key, el) in parts)
        {
            if (!props.TryGetValue(key, out var prop))
            {
                errors.Add(new DataError(el.File, el.Path, $"모르는 하위 키 '{key}'{IdSuggest.Hint(key, props.Keys)}"));
                continue;
            }
            try
            {
                var value = el.Node.Deserialize(prop.PropertyType, json);
                prop.SetValue(result, value);
            }
            catch (JsonException ex)
            {
                errors.Add(new DataError(el.File, JoinPath(el.Path, ex), LoaderJson.CleanMessage(ex)));
            }
        }
        return (T)result;
    }

    private static List<IDef> ReadKind(string kind, Type type, IReadOnlyList<RawElement> elements, JsonSerializerOptions json, List<DataError> errors)
    {
        // 원소 객체·ID 중복(abstract 포함)
        var byId = new Dictionary<string, (RawElement El, JsonObject Obj)>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var el in elements)
        {
            if (el.Node is not JsonObject obj)
            {
                errors.Add(new DataError(el.File, el.Path, "정의 원소는 객체여야 한다"));
                continue;
            }
            var idKey = kind == "evolutions" ? "result" : "id";
            if (!obj.TryGetPropertyValue(idKey, out var idNode) || idNode is not JsonValue v || !v.TryGetValue<string>(out var id) || id.Length == 0)
            {
                errors.Add(new DataError(el.File, $"{el.Path}.{idKey}", $"{idKey} 가 없다(문자열)"));
                continue;
            }
            if (byId.TryGetValue(id, out var first))
            {
                errors.Add(new DataError(el.File, $"{el.Path}.{idKey}", $"{kind} ID '{id}' 가 두 번 있다(처음: {first.El.File} {first.El.Path})"));
                continue;
            }
            byId[id] = (el, obj);
            order.Add(id);
        }

        // extends 풀기(부모 먼저, 순환 검사)
        var resolved = new Dictionary<string, JsonObject?>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        JsonObject? Resolve(string id)
        {
            if (resolved.TryGetValue(id, out var done)) return done;
            var (el, obj) = byId[id];
            if (!visiting.Add(id))
            {
                errors.Add(new DataError(el.File, $"{el.Path}.extends", $"extends 가 고리를 이룬다('{id}')"));
                resolved[id] = null;
                return null;
            }
            JsonObject? parent = null;
            var ok = true;
            if (obj.TryGetPropertyValue(JsonMerge.Extends, out var ext))
            {
                if (ext is not JsonValue ev || !ev.TryGetValue<string>(out var parentId))
                {
                    errors.Add(new DataError(el.File, $"{el.Path}.extends", "extends 는 부모 ID 문자열이어야 한다"));
                    ok = false;
                }
                else if (!byId.ContainsKey(parentId))
                {
                    errors.Add(new DataError(el.File, $"{el.Path}.extends", $"부모 '{parentId}' 가 없다{IdSuggest.Hint(parentId, byId.Keys)}"));
                    ok = false;
                }
                else
                {
                    parent = Resolve(parentId);
                    if (parent is null) ok = false;
                }
            }
            JsonObject? result = null;
            if (ok)
            {
                try
                {
                    result = JsonMerge.Apply(parent, obj);
                    if (kind == "enemies") ResolvePhases(result);
                }
                catch (JsonMerge.MergeException ex)
                {
                    errors.Add(new DataError(el.File, el.Path + ex.InnerPath, ex.Message));
                }
            }
            visiting.Remove(id);
            resolved[id] = result;
            return result;
        }

        var list = new List<IDef>();
        foreach (var id in order)
        {
            if (Resolve(id) is not { } shared) continue;
            var merged = (JsonObject)shared.DeepClone();   // 풀린 부모는 자식 풀기에 다시 쓰이므로 고치지 않는다
            var (el, obj) = byId[id];
            if (IsAbstract(obj)) continue;   // 추상 정의는 표에 넣지 않는다(참조하면 연결 단계 오류)
            if (kind == "sprites" && !SpriteKeys(merged, el, errors)) continue;
            var phases = merged[PhasePatternsKey] as JsonArray;
            merged.Remove(PhasePatternsKey);
            try
            {
                var def = (IDef?)merged.Deserialize(type, json) ?? throw new JsonException("정의가 null 이다");
                if (def is EnemyDef enemy && phases is not null) def = FillPhases(enemy, phases, el, json, errors);
                if (def is StageDef stage) def = NumberRooms(stage);
                list.Add(def);
            }
            catch (JsonException ex)
            {
                errors.Add(new DataError(el.File, JoinPath(el.Path, ex), LoaderJson.CleanMessage(ex)));
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new DataError(el.File, el.Path, ex.Message));
            }
        }
        return list;
    }

    private const string PhasePatternsKey = "__phase_patterns";

    /// <summary>
    /// 그림의 JsonNode 단계 규칙(6.3): atlas 에 <c>sheet</c>·<c>color_key</c>·<c>slack</c> 속성이 있으면 오류(값이 null 이어도),
    /// grid 에 <c>color_key</c> 속성이 없으면 검정 <c>[0,0,0]</c> 을 채운다(<c>"color_key": null</c> 은 PNG 알파 그대로).
    /// </summary>
    private static bool SpriteKeys(JsonObject sprite, RawElement el, List<DataError> errors)
    {
        var source = sprite["source"] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
        if (source == "atlas")
        {
            var ok = true;
            foreach (var key in new[] { "sheet", "color_key", "slack" })
                if (sprite.ContainsKey(key))
                {
                    errors.Add(new DataError(el.File, $"{el.Path}.{key}", $"atlas 그림에는 {key} 를 적지 않는다(시트 크기는 메타 페이지가 정한다, 6.3)"));
                    ok = false;
                }
            return ok;
        }
        if (source == "grid" && !sprite.ContainsKey("color_key")) sprite["color_key"] = new JsonArray(0, 0, 0);
        return true;
    }

    private static bool IsAbstract(JsonObject obj) =>
        obj.TryGetPropertyValue(JsonMerge.Abstract, out var a) && a is JsonValue v && v.TryGetValue<bool>(out var b) && b;

    /// <summary>페이즈 누적 풀기 결과를 숨은 키에 둔다(형 변환 뒤 <see cref="PhaseDef.Patterns"/> 로).</summary>
    private static void ResolvePhases(JsonObject enemy)
    {
        enemy.Remove(PhasePatternsKey);
        if (enemy["phases"] is not JsonArray phases || phases.Count == 0) return;
        var resolved = JsonMerge.ResolvePhases(enemy["patterns"] as JsonArray, phases, ".phases");
        enemy[PhasePatternsKey] = new JsonArray([.. resolved]);
    }

    private static EnemyDef FillPhases(EnemyDef enemy, JsonArray resolved, RawElement el, JsonSerializerOptions json, List<DataError> errors)
    {
        var phases = new List<PhaseDef>(enemy.Phases.Count);
        for (var k = 0; k < enemy.Phases.Count; k++)
        {
            IReadOnlyList<PatternDef> patterns = [];
            try
            {
                patterns = resolved[k]?.Deserialize<List<PatternDef>>(json) ?? [];
            }
            catch (JsonException ex)
            {
                errors.Add(new DataError(el.File, JoinPath($"{el.Path}.phases[{k}](풀린 패턴)", ex), LoaderJson.CleanMessage(ex)));
            }
            phases.Add(enemy.Phases[k] with { Patterns = patterns });
        }
        return enemy with { Phases = phases };
    }

    /// <summary><c>RoomDef.Number</c> 를 적지 않은 방(0)은 배열 자리 + 1 로(v0.7.1). 연결 전에 해야 <c>next_act</c> 참조가 이 객체를 가리킨다.</summary>
    private static StageDef NumberRooms(StageDef stage)
    {
        var any = false;
        foreach (var r in stage.Rooms) any |= r.Number == 0;
        if (!any) return stage;
        var rooms = new List<RoomDef>(stage.Rooms.Count);
        for (var i = 0; i < stage.Rooms.Count; i++)
            rooms.Add(stage.Rooms[i].Number == 0 ? stage.Rooms[i] with { Number = i + 1 } : stage.Rooms[i]);
        return stage with { Rooms = rooms };
    }

    /// <summary>원소 경로 + STJ 경로 + 부품 안쪽 경로.</summary>
    internal static string JoinPath(string elementPath, JsonException ex)
    {
        var p = DefJson.JoinPath(elementPath, ex.Path);
        if (ex.Data[LoaderJson.InnerPathKey] is string inner && inner.Length > 0)
            p += inner[0] is '[' or '.' ? inner : "." + inner;
        return p;
    }
}

/// <summary>1·2단계에서 모은 원소 하나(파일·JSON 경로·노드).</summary>
internal readonly record struct RawElement(string File, string Path, JsonNode? Node);

/// <summary>1·2단계 결과: 종류별 원소, <c>rules</c>·<c>sound_bindings</c> 하위 키, 정의 → 원소 위치(오류 경로용).</summary>
internal sealed class Collected
{
    private readonly Dictionary<string, List<RawElement>> _kinds = new(StringComparer.Ordinal);

    public Dictionary<string, RawElement> Rules { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, RawElement> SoundBindings { get; } = new(StringComparer.Ordinal);

    public void Add(string kind, RawElement el)
    {
        if (!_kinds.TryGetValue(kind, out var list)) _kinds[kind] = list = [];
        list.Add(el);
    }

    public IReadOnlyList<RawElement> Elements(string kind) => _kinds.TryGetValue(kind, out var l) ? l : [];

    public IEnumerable<MapDataElement> MapElements =>
        Elements(MapDataReader.TilesetsKind).Select(e => new MapDataElement(e.File, e.Path, MapDataReader.TilesetsKind, e.Node))
            .Concat(Elements(MapDataReader.MapsKind).Select(e => new MapDataElement(e.File, e.Path, MapDataReader.MapsKind, e.Node)));

    /// <summary>정의 ID → 원소 위치(종류마다). 같은 ID 가 두 번이면 처음 것. 오류 위치를 정의에 잇는다.</summary>
    public (string File, string Path) Where(string kind, string id)
    {
        var idKey = kind == "evolutions" ? "result" : "id";
        foreach (var el in Elements(kind))
            if (el.Node is JsonObject o && o[idKey] is JsonValue v && v.TryGetValue<string>(out var s) && s == id) return (el.File, el.Path);
        return ("", $"$.{kind}");
    }
}
