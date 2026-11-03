using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 정의 모양 시험용 읽기 (WP1d). 공용 STJ 옵션(<see cref="DefJson"/> — 모르는 속성 오류·snake_case 열거형)에
/// 시험용 참조 변환기 하나만 더한다: ID 문자열 → 연결 안 된 <see cref="DefRef{T}"/>. 실제 연결은 로더(WP7) 5단계다.
/// </summary>
public static class TestJson
{
    public static JsonSerializerOptions Options { get; } = DefJson.CreateOptions(new UnboundRefConverterFactory());

    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException($"{typeof(T).Name}: null");

    private sealed class UnboundRefConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(DefRef<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(RefConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }

    private sealed class RefConverter<T> : JsonConverter<DefRef<T>> where T : class, IDef
    {
        public override DefRef<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException($"{typeof(T).Name} ID 문자열이 와야 한다");
            return new DefRef<T>(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DefRef<T> value, JsonSerializerOptions options) => writer.WriteStringValue(value.Id);
    }
}
