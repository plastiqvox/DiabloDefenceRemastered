using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Data.Json;

/// <summary>
/// 공용 JSON 설정 (설계서 6.1·6.2, 계약 WP1b). 게임 로더(WP7)·<c>Map/MapDataReader</c>·<c>Assets/ImportTable</c>(도구)·가져오기 출력 읽기가
/// 같은 옵션·변환기를 쓴다 → 병합·변환 규칙이 두 벌이 되어 도구와 게임이 다른 배치를 만드는 일이 없다(1.4).
/// 규칙: 키 snake_case, 주석·끝 쉼표 허용, <b>모르는 속성은 오류</b>(Disallow), 열거형은 snake_case 문자열(숫자 안 받음),
/// 점·크기 <c>[x,y]</c>, 타일 좌표 <c>[tx,ty]</c>, 색 <c>[r,g,b]</c>, 사각형 <c>[x0,y0,x1,y1]</c>.
/// 그 밖의 변환기(도형·시각·DefRef·레벨 표 값)는 WP7 이 <see cref="CreateOptions"/> 로 더한다.
/// </summary>
public static class DefJson
{
    /// <summary>읽기 전용 공용 옵션(처음 쓰면 바뀌지 않는다). 변환기를 더하려면 <see cref="CreateOptions"/>.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>JsonNode 로 읽을 때(로딩 1단계): 주석·끝 쉼표 허용.</summary>
    public static JsonDocumentOptions DocumentOptions { get; } = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>속성 이름은 대소문자를 가린다(<c>Hp</c> 는 <c>hp</c> 가 아니다).</summary>
    public static JsonNodeOptions NodeOptions { get; } = new() { PropertyNameCaseInsensitive = false };

    /// <summary>공용 규칙을 가진 새 옵션. <paramref name="extra"/> 는 공용 변환기보다 앞에 둔다(같은 형이면 이쪽이 이긴다).</summary>
    public static JsonSerializerOptions CreateOptions(params JsonConverter[] extra)
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = null,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = false,
            NumberHandling = JsonNumberHandling.Strict,
        };
        foreach (var c in extra) o.Converters.Add(c);
        AddCommonConverters(o);
        return o;
    }

    /// <summary>공용 변환기를 이미 있는 옵션에 더한다(WP7 이 자기 옵션을 따로 만들 때).</summary>
    public static void AddCommonConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new Vector2JsonConverter());
        options.Converters.Add(new Int2JsonConverter());
        options.Converters.Add(new TileCoordJsonConverter());
        options.Converters.Add(new RgbJsonConverter());
        options.Converters.Add(new RectIJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
    }

    /// <summary>
    /// JsonNode 를 엄격하게 읽는 문서 파싱(주석·끝 쉼표 허용). 중복 키는 여기서 잡히지 않는다 — JsonObject 는 처음 속성을 볼 때
    /// <see cref="ArgumentException"/> 을 던지고, 형으로 바로 읽으면 마지막 값이 조용히 이긴다. 그래서 읽기 전에 <see cref="FindDuplicateKey(string)"/> 를 먼저 부른다.
    /// </summary>
    public static JsonNode? ParseNode(string json) => JsonNode.Parse(json, NodeOptions, DocumentOptions);

    /// <summary>
    /// 같은 객체 안에 두 번 나온 첫 키의 경로(<c>$.maps[0].id</c>, 문서 순서). 없으면 null. 정의·표·가져오기 출력 모두 중복 키는 오류다.
    /// 문법이 깨졌으면 <see cref="JsonException"/>.
    /// </summary>
    public static string? FindDuplicateKey(string json)
    {
        using var doc = JsonDocument.Parse(json, DocumentOptions);
        return FindDuplicateKey(doc.RootElement, "$");
    }

    /// <summary>원소 노드판(로더가 넘긴 원소). 아직 펼치지 않은 JsonObject 는 원문 그대로 쓰므로 중복 키가 남아 있다.</summary>
    public static string? FindDuplicateKey(JsonNode? node)
    {
        if (node is null) return null;
        try { return FindDuplicateKey(node.ToJsonString()); }
        catch (ArgumentException) { return "$"; }
    }

    private static string? FindDuplicateKey(JsonElement e, string path)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in e.EnumerateObject())
                {
                    if (!seen.Add(p.Name)) return $"{path}.{p.Name}";
                    if (FindDuplicateKey(p.Value, $"{path}.{p.Name}") is { } inner) return inner;
                }
                return null;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in e.EnumerateArray())
                {
                    if (FindDuplicateKey(item, $"{path}[{i}]") is { } inner) return inner;
                    i++;
                }
                return null;
            default:
                return null;
        }
    }

    /// <summary>중복 키 경로의 마지막 조각(오류 문구용).</summary>
    public static string LastKey(string path)
    {
        var dot = path.LastIndexOf('.');
        return dot < 0 ? path : path[(dot + 1)..];
    }

    /// <summary>
    /// 원소 경로(<c>$.maps[0]</c>)와 역직렬화 오류 경로(<c>$.layout.gates[1]</c>)를 잇는다 → <c>$.maps[0].layout.gates[1]</c>.
    /// </summary>
    public static string JoinPath(string elementPath, string? innerPath)
    {
        if (string.IsNullOrEmpty(innerPath) || innerPath == "$") return elementPath;
        return innerPath.StartsWith('$') ? elementPath + innerPath[1..] : elementPath + "." + innerPath;
    }

    // ── 배열 읽기 도우미 ─────────────────────────────────────────────

    internal static float ReadFloat(ref Utf8JsonReader reader, string what)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            throw new JsonException($"{what}: 숫자가 와야 한다");
        return reader.GetSingle();
    }

    internal static int ReadInt(ref Utf8JsonReader reader, string what)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var v))
            throw new JsonException($"{what}: 정수가 와야 한다");
        return v;
    }

    internal static void BeginArray(ref Utf8JsonReader reader, string what)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"{what}: 배열 [{what}] 이 와야 한다");
    }

    internal static void EndArray(ref Utf8JsonReader reader, string what)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException($"{what}: 원소 수가 맞지 않다");
    }
}

/// <summary><c>[x,y]</c> ↔ <see cref="Vector2"/>.</summary>
public sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DefJson.BeginArray(ref reader, "x,y");
        var x = DefJson.ReadFloat(ref reader, "x,y");
        var y = DefJson.ReadFloat(ref reader, "x,y");
        DefJson.EndArray(ref reader, "x,y");
        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

/// <summary><c>[x,y]</c> ↔ <see cref="Int2"/>(정수 점·크기).</summary>
public sealed class Int2JsonConverter : JsonConverter<Int2>
{
    public override Int2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DefJson.BeginArray(ref reader, "x,y");
        var x = DefJson.ReadInt(ref reader, "x,y");
        var y = DefJson.ReadInt(ref reader, "x,y");
        DefJson.EndArray(ref reader, "x,y");
        return new Int2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Int2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

/// <summary><c>[tx,ty]</c> ↔ <see cref="TileCoord"/>(하위 타일 좌표, 실수).</summary>
public sealed class TileCoordJsonConverter : JsonConverter<TileCoord>
{
    public override TileCoord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DefJson.BeginArray(ref reader, "tx,ty");
        var x = DefJson.ReadFloat(ref reader, "tx,ty");
        var y = DefJson.ReadFloat(ref reader, "tx,ty");
        DefJson.EndArray(ref reader, "tx,ty");
        return new TileCoord(x, y);
    }

    public override void Write(Utf8JsonWriter writer, TileCoord value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Tx);
        writer.WriteNumberValue(value.Ty);
        writer.WriteEndArray();
    }
}

/// <summary><c>[r,g,b]</c>(0..255) ↔ <see cref="Rgb"/>.</summary>
public sealed class RgbJsonConverter : JsonConverter<Rgb>
{
    public override Rgb Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DefJson.BeginArray(ref reader, "r,g,b");
        var r = Channel(ref reader);
        var g = Channel(ref reader);
        var b = Channel(ref reader);
        DefJson.EndArray(ref reader, "r,g,b");
        return new Rgb(r, g, b);
    }

    private static byte Channel(ref Utf8JsonReader reader)
    {
        var v = DefJson.ReadInt(ref reader, "r,g,b");
        if (v is < 0 or > 255) throw new JsonException($"r,g,b: 색 값은 0..255 여야 한다({v})");
        return (byte)v;
    }

    public override void Write(Utf8JsonWriter writer, Rgb value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.R);
        writer.WriteNumberValue(value.G);
        writer.WriteNumberValue(value.B);
        writer.WriteEndArray();
    }
}

/// <summary><c>[x0,y0,x1,y1]</c>(최소·최대) ↔ <see cref="RectI"/>.</summary>
public sealed class RectIJsonConverter : JsonConverter<RectI>
{
    public override RectI Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        DefJson.BeginArray(ref reader, "x0,y0,x1,y1");
        var x0 = DefJson.ReadInt(ref reader, "x0,y0,x1,y1");
        var y0 = DefJson.ReadInt(ref reader, "x0,y0,x1,y1");
        var x1 = DefJson.ReadInt(ref reader, "x0,y0,x1,y1");
        var y1 = DefJson.ReadInt(ref reader, "x0,y0,x1,y1");
        DefJson.EndArray(ref reader, "x0,y0,x1,y1");
        if (x1 < x0 || y1 < y0) throw new JsonException("x0,y0,x1,y1: 최소가 최대보다 크다");
        return new RectI(x0, y0, x1, y1);
    }

    public override void Write(Utf8JsonWriter writer, RectI value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X0);
        writer.WriteNumberValue(value.Y0);
        writer.WriteNumberValue(value.X1);
        writer.WriteNumberValue(value.Y1);
        writer.WriteEndArray();
    }
}
