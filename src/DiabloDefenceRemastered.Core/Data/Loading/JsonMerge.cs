using System.Text.Json.Nodes;

namespace DiabloDefenceRemastered.Core.Data.Loading;

/// <summary>
/// 로딩 3단계 (설계서 6.2·6.5): <c>extends</c> 병합(부모 먼저, 순환 검사), 객체 깊은 병합·배열·값 통째 교체,
/// <c>*_add</c>(배열 뒤에 붙이기)·<c>*_patch</c>(원소 <c>id</c> 로 찾아 깊은 병합 — 없는 id 는 오류), 메타 키 지우기, 페이즈 누적 풀기.
/// 모두 JsonNode 단계라 매개변수 형을 모른다. 깊은 병합 안의 <c>*_add</c>·<c>*_patch</c> 도 같은 규칙(페이즈 <c>patch</c> 의 <c>actions_patch</c>).
/// </summary>
internal static class JsonMerge
{
    public const string Extends = "extends";
    public const string Abstract = "abstract";
    public const string AddSuffix = "_add";
    public const string PatchSuffix = "_patch";

    /// <summary>병합 오류(경로는 원소 안 경로 <c>.behaviors_patch.melee</c>).</summary>
    public sealed class MergeException(string message, string innerPath) : Exception(message)
    {
        public string InnerPath { get; } = innerPath;
    }

    /// <summary>메타 키인가(병합 뒤 남기지 않음).</summary>
    public static bool IsMeta(string key) =>
        key == Extends || key == Abstract || key.EndsWith(AddSuffix, StringComparison.Ordinal) || key.EndsWith(PatchSuffix, StringComparison.Ordinal);

    /// <summary>
    /// 부모(이미 풀린 것, 메타 키 없음) 위에 자식을 얹는다. 자식의 일반 키 → 깊은 병합, 그다음 <c>*_add</c>, 그다음 <c>*_patch</c>(같은 키 안에서는 JSON 순서).
    /// 결과는 새 객체(입력은 바꾸지 않음). <c>abstract</c> 는 물려받지 않는다.
    /// </summary>
    public static JsonObject Apply(JsonObject? parent, JsonObject child)
    {
        var result = parent is null ? [] : (JsonObject)parent.DeepClone();
        result.Remove(Abstract);
        MergeInto(result, child, "");
        return result;
    }

    /// <summary><paramref name="patch"/> 를 <paramref name="target"/> 에 깊은 병합(메타 키 규칙 포함). <paramref name="path"/> 는 오류 경로 앞부분.</summary>
    public static void MergeInto(JsonObject target, JsonObject patch, string path)
    {
        // 일반 키 먼저
        foreach (var (key, value) in patch)
        {
            if (IsMeta(key)) continue;
            if (value is JsonObject po && target[key] is JsonObject to) MergeInto(to, po, $"{path}.{key}");
            else target[key] = value?.DeepClone();
        }
        foreach (var (key, value) in patch)
        {
            if (!key.EndsWith(AddSuffix, StringComparison.Ordinal) || key == AddSuffix) continue;
            var name = key[..^AddSuffix.Length];
            if (value is not JsonArray add) throw new MergeException($"{key} 는 배열이어야 한다", $"{path}.{key}");
            if (target[name] is null) target[name] = new JsonArray();
            if (target[name] is not JsonArray arr) throw new MergeException($"{key}: '{name}' 이 배열이 아니다", $"{path}.{key}");
            foreach (var item in add) arr.Add(item?.DeepClone());
        }
        foreach (var (key, value) in patch)
        {
            if (!key.EndsWith(PatchSuffix, StringComparison.Ordinal) || key == PatchSuffix) continue;
            var name = key[..^PatchSuffix.Length];
            if (value is not JsonObject byId) throw new MergeException($"{key} 는 {{ \"<id>\": {{ … }} }} 객체여야 한다", $"{path}.{key}");
            if (target[name] is not JsonArray arr) throw new MergeException($"{key}: 고칠 '{name}' 배열이 없다", $"{path}.{key}");
            PatchById(arr, byId, $"{path}.{key}");
        }
    }

    /// <summary>배열 원소를 <c>id</c> 로 찾아 깊은 병합. 없는 id 는 오류.</summary>
    public static void PatchById(JsonArray arr, JsonObject byId, string path)
    {
        foreach (var (id, p) in byId)
        {
            if (p is not JsonObject po) throw new MergeException($"'{id}' 의 고칠 값은 객체여야 한다", $"{path}.{id}");
            var target = FindById(arr, id) ?? throw new MergeException($"고칠 id '{id}' 가 없다{IdSuggest.Hint(id, Ids(arr))}", $"{path}.{id}");
            MergeInto(target, po, $"{path}.{id}");
        }
    }

    public static JsonObject? FindById(JsonArray arr, string id)
    {
        foreach (var item in arr)
            if (item is JsonObject o && IdOf(o) == id) return o;
        return null;
    }

    public static string? IdOf(JsonObject o) =>
        o.TryGetPropertyValue("id", out var v) && v is JsonValue jv && jv.TryGetValue<string>(out var s) ? s : null;

    private static IEnumerable<string> Ids(JsonArray arr)
    {
        foreach (var item in arr)
            if (item is JsonObject o && IdOf(o) is { } id) yield return id;
    }

    /// <summary>
    /// 페이즈 누적 풀기(6.5): 정의 <c>patterns</c> 에서 시작해 첫 페이즈부터 k 번째까지 <c>patch → add → remove</c> 를 차례로 적용한 목록을 페이즈마다.
    /// <c>add</c> 의 패턴이 이미 있는 ID 면 그 자리에서 바꾼다(같은 ID 패턴은 원래 자리를 지킴), 없으면 뒤에 붙인다. <c>remove</c> 의 없는 ID 는 오류.
    /// </summary>
    public static List<JsonArray> ResolvePhases(JsonArray? patterns, JsonArray phases, string path)
    {
        var cur = patterns is null ? [] : (JsonArray)patterns.DeepClone();
        var result = new List<JsonArray>(phases.Count);
        for (var k = 0; k < phases.Count; k++)
        {
            var ph = $"{path}[{k}]";
            if (phases[k] is not JsonObject phase) throw new MergeException("페이즈는 객체여야 한다", ph);
            if (phase["patch"] is { } patch)
            {
                if (patch is not JsonObject byId) throw new MergeException("patch 는 { \"<패턴 id>\": { … } } 객체여야 한다", $"{ph}.patch");
                PatchById(cur, byId, $"{ph}.patch");
            }
            if (phase["add"] is { } add)
            {
                if (add is not JsonArray adds) throw new MergeException("add 는 배열이어야 한다", $"{ph}.add");
                foreach (var a in adds)
                {
                    var clone = a?.DeepClone();
                    if (clone is JsonObject o && IdOf(o) is { } id && IndexOfId(cur, id) is var at and >= 0)
                        cur[at] = clone;
                    else
                        cur.Add(clone);
                }
            }
            if (phase["remove"] is { } remove)
            {
                if (remove is not JsonArray removes) throw new MergeException("remove 는 패턴 id 배열이어야 한다", $"{ph}.remove");
                for (var i = 0; i < removes.Count; i++)
                {
                    var id = removes[i] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
                    var at = id is null ? -1 : IndexOfId(cur, id);
                    if (at < 0) throw new MergeException($"지울 패턴 '{id}' 가 없다", $"{ph}.remove[{i}]");
                    cur.RemoveAt(at);
                }
            }
            result.Add((JsonArray)cur.DeepClone());
        }
        return result;
    }

    private static int IndexOfId(JsonArray arr, string id)
    {
        for (var i = 0; i < arr.Count; i++)
            if (arr[i] is JsonObject o && IdOf(o) == id) return i;
        return -1;
    }
}
