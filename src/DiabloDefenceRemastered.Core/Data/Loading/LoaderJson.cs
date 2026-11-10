using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Data.Loading;

/// <summary>
/// 로더 전용 STJ 옵션 (설계서 6.2 "변환기" — WP7). 공용 규칙(<see cref="DefJson"/>)에 더하는 것:
/// ID 문자열 → 연결 전 <see cref="DefRef{T}"/>, 시각 <c>"mm:ss"</c> → <see cref="ClockTime"/>, 가중치 객체 → 적힌 순서의 <see cref="WeightEntry{T}"/> 목록,
/// 이펙트 참조 문자열 줄임 → <see cref="EffectRef"/>, <c>[PartKind]</c> 속성의 원소 → <see cref="PartDef"/>(레지스트리에서 매개변수 형을 고름).
/// 도형(<see cref="ShapeDef"/>)·레벨 표 값(<c>float?</c>)은 공용 규칙 그대로 읽힌다(모양 검사는 의미 검사 단계).
/// 옵션은 로딩 한 번마다 새로 만든다 — 부품 변환기가 그 로딩의 레지스트리를 안다.
/// </summary>
internal static class LoaderJson
{
    public static JsonSerializerOptions Create(PartRegistry registry, DataLoadOptions options)
    {
        var o = DefJson.CreateOptions(new DefRefConverterFactory(), new ClockTimeConverter(), new WeightsConverterFactory(), new EffectRefConverter());
        var parts = new PartReader(registry, options);
        o.TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { info => AttachPartConverters(info, parts) } };
        parts.Options = o;
        return o;
    }

    /// <summary><c>[PartKind]</c> 가 붙은 속성(<c>IReadOnlyList&lt;PartDef&gt;</c> 또는 <c>PartDef?</c>)에 그 종류의 부품 변환기를 단다.</summary>
    private static void AttachPartConverters(JsonTypeInfo info, PartReader parts)
    {
        if (info.Kind != JsonTypeInfoKind.Object) return;
        foreach (var prop in info.Properties)
        {
            if (prop.AttributeProvider is not MemberInfo member) continue;
            if (member.GetCustomAttribute<PartKindAttribute>() is not { } attr) continue;
            if (prop.PropertyType == typeof(PartDef)) prop.CustomConverter = new PartConverter(attr.Kind, parts);
            else if (prop.PropertyType == typeof(IReadOnlyList<PartDef>)) prop.CustomConverter = new PartListConverter(attr.Kind, parts);
        }
    }

    /// <summary>예외 문구에서 STJ 가 붙인 꼬리(<c> Path: … | LineNumber: …</c>)를 뗀다 — 경로는 DataError 에 따로 있다.</summary>
    public static string CleanMessage(JsonException ex)
    {
        var m = ex.Message;
        var cut = m.IndexOf(" Path: ", StringComparison.Ordinal);
        if (cut >= 0) m = m[..cut];
        cut = m.IndexOf(" LineNumber: ", StringComparison.Ordinal);
        if (cut >= 0) m = m[..cut];
        return m.TrimEnd(' ', '|', '.');
    }

    /// <summary>부품 매개변수 안쪽 경로(바깥 STJ 경로 뒤에 붙음)를 예외에 싣는 키.</summary>
    public const string InnerPathKey = "ddr.inner_path";

    // ── 변환기 ─────────────────────────────────────────────

    private sealed class DefRefConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(DefRef<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(DefRefConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }

    private sealed class DefRefConverter<T> : JsonConverter<DefRef<T>> where T : class, IDef
    {
        public override DefRef<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException($"{DefKinds.KindOf(typeof(T))} ID 문자열이 와야 한다");
            var id = reader.GetString()!;
            if (id.Length == 0) throw new JsonException($"{DefKinds.KindOf(typeof(T))} ID 가 비었다");
            return new DefRef<T>(id);
        }

        public override void Write(Utf8JsonWriter writer, DefRef<T> value, JsonSerializerOptions options) => writer.WriteStringValue(value.Id);
    }

    private sealed class ClockTimeConverter : JsonConverter<ClockTime>
    {
        public override ClockTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String || !ClockTime.TryParse(reader.GetString()!, out var t))
                throw new JsonException("시각은 \"mm:ss\" 문자열이어야 한다");
            return t;
        }

        public override void Write(Utf8JsonWriter writer, ClockTime value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }

    private sealed class WeightsConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
            && typeToConvert.GetGenericArguments()[0] is { IsGenericType: true } e && e.GetGenericTypeDefinition() == typeof(WeightEntry<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(WeightsConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0].GetGenericArguments()[0]))!;
    }

    /// <summary>가중치 객체 <c>{ "a": 30, "b": 70 }</c> → JSON 에 적힌 순서의 배열(3.4). 음수 가중치는 오류.</summary>
    private sealed class WeightsConverter<T> : JsonConverter<IReadOnlyList<WeightEntry<T>>> where T : class, IDef
    {
        public override IReadOnlyList<WeightEntry<T>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("가중치는 { \"ID\": 가중, … } 객체여야 한다");
            var list = new List<WeightEntry<T>>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var id = reader.GetString()!;
                if (!reader.Read() || reader.TokenType != JsonTokenType.Number) throw new JsonException($"가중치 '{id}' 는 숫자여야 한다");
                var w = reader.GetSingle();
                if (w < 0f || !float.IsFinite(w)) throw new JsonException($"가중치 '{id}' 가 음수다({w})");
                list.Add(new WeightEntry<T>(new DefRef<T>(id), w));
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<WeightEntry<T>> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var w in value) writer.WriteNumber(w.Ref.Id, w.Weight);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// 이펙트 참조: ID 문자열 줄임 또는 <c>{ effect, tint?, scale? }</c>(참조 쪽 덮어쓰기는 색조·크기만 — 다른 키는 모르는 속성 오류).
    /// </summary>
    private sealed class EffectRefConverter : JsonConverter<EffectRef>
    {
        public override EffectRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var id = reader.GetString()!;
                if (id.Length == 0) throw new JsonException("이펙트 ID 가 비었다");
                return new EffectRef { Effect = new DefRef<EffectDef>(id) };
            }
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("이펙트 참조는 ID 문자열 또는 { effect, tint, scale } 객체여야 한다");
            DefRef<EffectDef>? effect = null;
            Rgb? tint = null;
            float? scale = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var name = reader.GetString()!;
                reader.Read();
                switch (name)
                {
                    case "effect":
                        effect = JsonSerializer.Deserialize<DefRef<EffectDef>>(ref reader, options);
                        break;
                    case "tint":
                        tint = JsonSerializer.Deserialize<Rgb>(ref reader, options);
                        break;
                    case "scale":
                        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("scale 은 숫자여야 한다");
                        scale = reader.GetSingle();
                        break;
                    default:
                        throw new JsonException($"이펙트 참조에서 덮어쓸 수 있는 것은 tint·scale 뿐이다('{name}')");
                }
            }
            return new EffectRef { Effect = effect ?? throw new JsonException("이펙트 참조 객체에 effect 가 없다"), Tint = tint, Scale = scale };
        }

        public override void Write(Utf8JsonWriter writer, EffectRef value, JsonSerializerOptions options)
        {
            if (value.Tint is null && value.Scale is null)
            {
                writer.WriteStringValue(value.Effect.Id);
                return;
            }
            writer.WriteStartObject();
            writer.WriteString("effect", value.Effect.Id);
            if (value.Tint is { } t)
            {
                writer.WritePropertyName("tint");
                JsonSerializer.Serialize(writer, t, options);
            }
            if (value.Scale is { } s) writer.WriteNumber("scale", s);
            writer.WriteEndObject();
        }
    }

    private sealed class PartConverter(PartKind kind, PartReader parts) : JsonConverter<PartDef>
    {
        public override PartDef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return parts.Read(JsonNode.Parse(ref reader, DefJson.NodeOptions), kind, null);
        }

        public override void Write(Utf8JsonWriter writer, PartDef value, JsonSerializerOptions options) =>
            throw new NotSupportedException("부품은 쓰지 않는다");
    }

    private sealed class PartListConverter(PartKind kind, PartReader parts) : JsonConverter<IReadOnlyList<PartDef>>
    {
        public override IReadOnlyList<PartDef> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("부품 목록은 배열이어야 한다");
            if (JsonNode.Parse(ref reader, DefJson.NodeOptions) is not JsonArray arr) throw new JsonException("부품 목록은 배열이어야 한다");
            var list = new List<PartDef>(arr.Count);
            for (var i = 0; i < arr.Count; i++) list.Add(parts.Read(arr[i], kind, i));
            return list;
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<PartDef> value, JsonSerializerOptions options) =>
            throw new NotSupportedException("부품은 쓰지 않는다");
    }
}

/// <summary>
/// 부품 원소 하나 읽기(6.2 4단계): <c>type</c>·<c>id</c>·<c>trigger</c>(스테이지 이벤트는 <c>at</c> 도)를 빼고 나머지를 등록된 매개변수 형으로 역직렬화, 생성.
/// <c>behaviors</c> 원소에 <c>trigger</c> 가 있으면 <see cref="PartKind.EnemyAction"/> 으로 찾는다(훅 어댑터, 6.6).
/// </summary>
internal sealed class PartReader(PartRegistry registry, DataLoadOptions options)
{
    public JsonSerializerOptions Options { get; set; } = DefJson.Options;

    public PartDef Read(JsonNode? node, PartKind kind, int? index)
    {
        var at = index is { } i ? $"[{i}]" : "";
        if (node is not JsonObject src) throw Fail("부품 원소는 객체여야 한다", at);
        var obj = (JsonObject)src.DeepClone();

        var type = TakeString(obj, "type", at) ?? throw Fail("부품에 type 이 없다", at);
        var id = TakeString(obj, "id", at);
        var trigger = TakeString(obj, "trigger", at);
        ClockTime? when = null;
        if (kind == PartKind.StageEvent)
        {
            var text = TakeString(obj, "at", at) ?? throw Fail($"이벤트 '{type}' 에 at(\"mm:ss\") 이 없다", at);
            if (!ClockTime.TryParse(text, out var t)) throw Fail($"이벤트 at 은 \"mm:ss\" 꼴이어야 한다: '{text}'", at + ".at");
            when = t;
        }

        var lookKind = kind == PartKind.EnemyBehavior && trigger is not null ? PartKind.EnemyAction : kind;
        if (!registry.TryGet(lookKind, type, out var factory))
        {
            if (!options.AllowUnknownParts)
            {
                var hint = IdSuggest.Hint(type, registry.TypesOf(lookKind));
                throw Fail($"부품 ({PartKindName(lookKind)}, '{type}') 가 등록되지 않았다{hint}", at + ".type");
            }
            // 로더 단위 시험 전용: 매개변수는 JSON 그대로
            return new PartDef(lookKind, type, id, trigger, obj, obj) { At = when };
        }

        object parameters;
        try
        {
            parameters = obj.Deserialize(factory.ParamsType, Options) ?? throw Fail($"부품 '{type}' 매개변수가 null 이다", at);
        }
        catch (JsonException ex) when (!ex.Data.Contains(LoaderJson.InnerPathKey))
        {
            throw Fail(LoaderJson.CleanMessage(ex), DefJson.JoinPath(at, ex.Path).TrimStart('$'));
        }

        object instance;
        try
        {
            instance = factory.Create(parameters);
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            throw Fail($"부품 '{type}' 을 만들지 못했다: {ex.Message}", at);
        }

        return new PartDef(lookKind, type, id, trigger, parameters, instance)
        {
            ReferencedEnemies = DefWalker.CollectRefs<EnemyDef>(parameters),
            At = when,
        };
    }

    private static string? TakeString(JsonObject obj, string key, string at)
    {
        if (!obj.TryGetPropertyValue(key, out var v)) return null;
        obj.Remove(key);
        if (v is JsonValue jv && jv.TryGetValue<string>(out var s)) return s;
        throw Fail($"{key} 는 문자열이어야 한다", $"{at}.{key}");
    }

    /// <summary>바깥 STJ 가 이 변환기 자리까지의 경로를 붙이고, 그 뒤 경로는 예외 데이터로 싣는다.</summary>
    private static JsonException Fail(string message, string innerPath)
    {
        var ex = new JsonException(message);
        ex.Data[LoaderJson.InnerPathKey] = innerPath;
        return ex;
    }

    public static string PartKindName(PartKind kind) => kind switch
    {
        PartKind.EnemyBehavior => "적 부품",
        PartKind.EnemyAction => "적 동작",
        PartKind.FirePattern => "발사 패턴",
        PartKind.Targeting => "조준",
        PartKind.StageEvent => "스테이지 이벤트",
        PartKind.WeaponEffect => "무기 효과",
        _ => kind.ToString(),
    };
}
