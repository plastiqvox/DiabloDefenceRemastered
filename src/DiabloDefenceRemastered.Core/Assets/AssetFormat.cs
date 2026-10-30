using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>
/// 가져오기 출력의 형식 판·경로 규칙 (설계서 1.4, 계약 WP1b). 가져오기 도구(WP0)가 쓰고 게임 시작 검사(WP7)·렌더(WP5)·시험이 읽는다.
/// <para>
/// <see cref="Version"/> 은 게임과의 <b>호환 판정</b>에만 쓴다: 출력 모양·뜻이 바뀌면(메타·<c>arena.json</c> 모양, 바닥/벽 나눔, 그림자·잘라 내기 규칙)
/// WP0 이 올린다. 캐시 키는 이 번호가 아니라 도구·Core 어셈블리 해시다.
/// </para>
/// 경로는 모두 <b>Assets 뿌리 기준 상대 경로, 구분자 '/'</b>(<c>d1/monsters/zombie.png</c>) — 정의의 <c>file</c> 과 같은 꼴이라 그대로 대조한다.
/// </summary>
public static class AssetFormat
{
    /// <summary>manifest 의 <c>format</c>. 시작 검사가 다르면 "자원 형식이 낡았다(판 a → b)".</summary>
    public const int Version = 1;

    /// <summary>가져오기 도구가 쓰는 폴더(Assets 뿌리 기준). 원작 exe PNG 는 뿌리, 도구는 이 아래만 쓴다.</summary>
    public const string Root = "d1";
    /// <summary>manifest(가져오기가 마지막에 쓴다 — 없으면 "자원 없음").</summary>
    public const string ManifestPath = "d1/manifest.json";
    /// <summary>소리 색인.</summary>
    public const string SoundIndexPath = "d1/sound/index.json";
    /// <summary>원작 소리·음악 출력 앞머리. 출력 = 이것 + 원작 경로 소문자·'/'.</summary>
    public const string SoundPrefix = "d1/sound/";
    /// <summary>빌드 검사(DDR0001)가 함께 보는 원작 exe PNG.</summary>
    public const string ExeCheckFile = "TITLE.png";

    /// <summary>원작 경로(<c>sfx\misc\cbolt.wav</c>) → 출력 이름(<c>d1/sound/sfx/misc/cbolt.wav</c>).</summary>
    public static string SoundOutput(string sourcePath) => SoundPrefix + sourcePath.Replace('\\', '/').ToLowerInvariant();

    /// <summary>
    /// Assets 상대 경로 규칙: '/' 구분, 앞뒤 '/' 없음, 빈 조각·<c>.</c>·<c>..</c> 없음, 역슬래시·드라이브 없음.
    /// </summary>
    public static bool IsValidRelativePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Contains('\\') || path.Contains(':') || path.StartsWith('/') || path.EndsWith('/')) return false;
        foreach (var part in path.Split('/'))
            if (part.Length == 0 || part == "." || part == "..") return false;
        return true;
    }
}

/// <summary>
/// 가져오기 출력 JSON 읽기·쓰기(manifest·메타·<c>arena.json</c>·소리 색인). 읽기는 공용 엄격 옵션(모르는 속성 오류),
/// 쓰기는 결정적(정해진 키 순서 = record 선언 순서, <b>사전은 키 서수 순</b> — 넣은 순서와 무관, 불변 문화, 줄바꿈 <c>\n</c>, 들여쓰기 2칸, 끝에 줄바꿈 하나).
/// </summary>
public static class AssetJson
{
    public static JsonSerializerOptions ReadOptions => DefJson.Options;

    public static JsonSerializerOptions WriteOptions { get; } = CreateWriteOptions();

    private static JsonSerializerOptions CreateWriteOptions()
    {
        // 병렬 굽기 결과를 순서 없이 사전에 넣어도 같은 바이트가 나오게 사전 키를 정렬해 쓴다(1.4 결정론)
        var o = DefJson.CreateOptions(new SortedStringDictionaryConverterFactory());
        o.WriteIndented = true;
        o.IndentSize = 2;
        o.NewLine = "\n";
        o.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        return o;
    }

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, ReadOptions) ?? throw new JsonException($"{typeof(T).Name}: null");

    public static T Read<T>(string path) => Deserialize<T>(File.ReadAllText(path));

    /// <summary>UTF-8(BOM 없음) 바이트. 같은 값이면 같은 바이트.</summary>
    public static byte[] Serialize<T>(T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, WriteOptions);
        var result = new byte[bytes.Length + 1];
        bytes.CopyTo(result, 0);
        result[^1] = (byte)'\n';
        return result;
    }

    /// <summary>문자열 키 사전(<c>Dictionary</c>·<c>IDictionary</c>·<c>IReadOnlyDictionary</c>)을 키 서수 순으로 쓴다. 읽기는 공용 옵션 그대로.</summary>
    private sealed class SortedStringDictionaryConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType) return false;
            var def = typeToConvert.GetGenericTypeDefinition();
            return (def == typeof(Dictionary<,>) || def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>))
                && typeToConvert.GetGenericArguments()[0] == typeof(string);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(SortedConverter<,>).MakeGenericType(typeToConvert, typeToConvert.GetGenericArguments()[1]))!;

        private sealed class SortedConverter<TDict, TValue> : JsonConverter<TDict> where TDict : IEnumerable<KeyValuePair<string, TValue>>
        {
            public override TDict Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                (TDict)(object)JsonSerializer.Deserialize<Dictionary<string, TValue>>(ref reader, DefJson.Options)!;

            public override void Write(Utf8JsonWriter writer, TDict value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                foreach (var (key, item) in value.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(key);
                    JsonSerializer.Serialize(writer, item, options);
                }
                writer.WriteEndObject();
            }
        }
    }
}
