using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Data.Loading;

/// <summary>
/// 정의 객체 그래프를 반사로 돈다(로딩 5·6단계). 경로는 JSON 경로와 같게 만든다(속성 = snake_case 이름 또는 <c>[JsonPropertyName]</c>, 목록 = <c>[i]</c>,
/// 사전·가중치 = <c>.키</c>). 다른 정의로는 <see cref="DefRef{T}"/> 에서 멈춘다(그 너머로 들어가지 않음 — 고리 없음).
/// <see cref="PartDef"/> 는 <see cref="PartDef.Params"/> 만 들어간다(<c>Instance</c>·<c>ReferencedEnemies</c> 는 같은 참조를 다시 담음).
/// </summary>
internal static class DefWalker
{
    public interface IVisitor
    {
        void DefRef(object defRef, Type target, string path);
        void Part(PartDef part, string path);
        void PartTypeName(PartKind kind, string type, string path);
    }

    private static readonly ConcurrentDictionary<Type, PropInfo[]> Props = new();

    private sealed record PropInfo(PropertyInfo Property, string JsonName, PartKind? PartType);

    public static void Walk(object? value, string path, IVisitor visitor)
    {
        if (value is null) return;
        var type = value.GetType();
        if (IsLeaf(type)) return;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefRef<>))
        {
            visitor.DefRef(value, type.GetGenericArguments()[0], path);
            return;
        }
        if (value is PartDef part)
        {
            visitor.Part(part, path);
            Walk(part.Params, path, visitor);
            return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(WeightEntry<>))
        {
            var r = type.GetProperty(nameof(WeightEntry<IDef>.Ref))!.GetValue(value)!;
            var id = (string)r.GetType().GetProperty(nameof(DefRef<IDef>.Id))!.GetValue(r)!;
            visitor.DefRef(r, type.GetGenericArguments()[0], $"{path}.{id}");
            return;
        }
        if (value is IDictionary dict)
        {
            foreach (DictionaryEntry e in dict) Walk(e.Value, $"{path}.{KeyName(e.Key)}", visitor);
            return;
        }
        if (IsReadOnlyDictionary(type))
        {
            foreach (var kv in (IEnumerable)value)
            {
                var kvType = kv.GetType();
                var key = kvType.GetProperty("Key")!.GetValue(kv);
                Walk(kvType.GetProperty("Value")!.GetValue(kv), $"{path}.{KeyName(key)}", visitor);
            }
            return;
        }
        if (value is IEnumerable list)
        {
            var i = 0;
            foreach (var item in list)
            {
                // 가중치 목록은 JSON 에서 객체라 경로가 .<id>(WeightEntry 가 붙인다)
                var isWeight = item?.GetType() is { IsGenericType: true } it && it.GetGenericTypeDefinition() == typeof(WeightEntry<>);
                Walk(item, isWeight ? path : $"{path}[{i}]", visitor);
                i++;
            }
            return;
        }

        foreach (var p in PropsOf(type))
        {
            var v = p.Property.GetValue(value);
            if (p.PartType is { } kind && v is string typeName) visitor.PartTypeName(kind, typeName, $"{path}.{p.JsonName}");
            Walk(v, $"{path}.{p.JsonName}", visitor);
        }
    }

    /// <summary>객체 안의 모든 <c>DefRef&lt;T&gt;</c>(연결 전이어도 됨 — <see cref="PartDef.ReferencedEnemies"/> 모으기).</summary>
    public static IReadOnlyList<DefRef<T>> CollectRefs<T>(object value) where T : class, IDef
    {
        var c = new RefCollector<T>();
        Walk(value, "$", c);
        return c.Found;
    }

    private sealed class RefCollector<T> : IVisitor where T : class, IDef
    {
        public List<DefRef<T>> Found { get; } = [];
        public void DefRef(object defRef, Type target, string path)
        {
            if (defRef is DefRef<T> r) Found.Add(r);
        }
        public void Part(PartDef part, string path) { }
        public void PartTypeName(PartKind kind, string type, string path) { }
    }

    private static string KeyName(object? key) => key switch
    {
        null => "",
        Enum e => JsonNamingPolicy.SnakeCaseLower.ConvertName(e.ToString()),
        _ => key.ToString() ?? "",
    };

    private static bool IsReadOnlyDictionary(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

    private static bool IsLeaf(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
        || type == typeof(Vector2) || type == typeof(Int2) || type == typeof(Rgb) || type == typeof(RectI) || type == typeof(TileCoord)
        || type == typeof(ClockTime) || type == typeof(TileOrAnchor) || typeof(JsonNode).IsAssignableFrom(type) || type == typeof(JsonElement)
        || typeof(Delegate).IsAssignableFrom(type) || type == typeof(Type)
        || type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true && !typeof(IEnumerable).IsAssignableFrom(type);

    private static PropInfo[] PropsOf(Type type) => Props.GetOrAdd(type, static t =>
    [
        .. t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.GetCustomAttribute<JsonExtensionDataAttribute>() is null
                        && !(t == typeof(EvolutionDef) && p.Name == nameof(EvolutionDef.Id)))
            .Select(p => new PropInfo(p, p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.SnakeCaseLower.ConvertName(p.Name),
                                      p.GetCustomAttribute<PartTypeAttribute>()?.Kind)),
    ]);
}
