using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Tests.TestKit;

/// <summary>
/// 정의 모양 시험용 읽기 (WP1d, v0.7 WP1e 보탬). 공용 STJ 옵션(<see cref="DefJson"/> — 모르는 속성 오류·snake_case 열거형)에
/// 시험용 변환기만 더한다: ID 문자열 → 연결 안 된 <see cref="DefRef{T}"/>, 시각 <c>"mm:ss"</c> → <see cref="ClockTime"/>,
/// 가중치 객체 <c>{ "a": 30, "b": 70 }</c> → 적힌 순서의 <see cref="WeightEntry{T}"/> 목록(3.4). 실제 변환기·연결은 로더(WP7)다.
/// </summary>
public static class TestJson
{
    public static JsonSerializerOptions Options { get; } =
        DefJson.CreateOptions(new UnboundRefConverterFactory(), new ClockTimeConverter(), new WeightsConverterFactory());

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

    private sealed class WeightsConverter<T> : JsonConverter<IReadOnlyList<WeightEntry<T>>> where T : class, IDef
    {
        public override IReadOnlyList<WeightEntry<T>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("가중치는 객체여야 한다");
            var list = new List<WeightEntry<T>>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var id = reader.GetString()!;
                reader.Read();
                list.Add(new WeightEntry<T>(new DefRef<T>(id), reader.GetSingle()));
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
}
