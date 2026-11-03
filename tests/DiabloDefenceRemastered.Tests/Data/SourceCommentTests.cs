namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>
/// 13.5-16 원본 유래 값의 출처 주석 (설계서 6.14·1.4, WP0a). JSON 주석은 로더가 버리므로 원문 문자열로 본다.
/// <c>Data/tilesets/*.json</c> 의 <c>props</c>·<c>sol_known_diffs</c>, <c>Data/sprites/*.json</c> 의 <c>frame_time</c>·<c>event_frame</c>,
/// 가져오기 표 <c>Table/tilesets.json</c> 의 <c>overlay</c> 가 있는 줄마다(값이 여러 줄이면 그 줄들마다) 그 줄 끝, 또는 그 줄을 담은 객체·배열을 여는 줄 끝에
/// <c>// 출처: 원본 확인</c> 또는 <c>// 출처: DevilutionX 참고</c> 가 있어야 한다(줄 단위 + 괄호 깊이 추적). 저장소 원문을 읽는다(자원 없음).
/// </summary>
public class SourceCommentTests
{
    private static readonly string[] Sources = ["출처: 원본 확인", "출처: DevilutionX 참고"];

    [Fact]
    public void RealData_EveryOriginValueLine_HasSourceComment()
    {
        var root = RealData.RepoRoot();
        var data = Path.Combine(root, "src", "DiabloDefenceRemastered", "Data");
        var tilesets = Files(Path.Combine(data, "tilesets"));
        var sprites = Files(Path.Combine(data, "sprites"));
        Assert.NotEmpty(tilesets);   // WP0a 가 넣은 실제 데이터 — 비어 있으면 검사가 헛돈다
        Assert.NotEmpty(sprites);

        var problems = new List<string>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        void Check(string file, params string[] keys)
        {
            foreach (var (key, line) in FindKeys(File.ReadAllText(file), keys, problems, Path.GetRelativePath(root, file)))
                seen[key] = seen.GetValueOrDefault(key) + 1;
        }
        foreach (var f in tilesets) Check(f, "props", "sol_known_diffs");
        foreach (var f in sprites) Check(f, "frame_time", "event_frame");
        var table = Path.Combine(root, "tools", "DiabloAssetImporter", "Table", "tilesets.json");
        if (File.Exists(table)) Check(table, "overlay");   // 가져오기 표는 WP0 이 만든다 — 생기면 같이 본다

        Assert.True(problems.Count == 0, "출처 주석 없음:\n" + string.Join("\n", problems));
        // 실제 파일에 검사 대상이 있다(키 이름이 바뀌어 아무것도 안 보는 일 막기)
        Assert.True(seen.GetValueOrDefault("props") > 0, "tilesets 에 props 가 없다");
        Assert.True(seen.GetValueOrDefault("frame_time") > 0, "sprites 에 frame_time 이 없다");
        Assert.True(seen.GetValueOrDefault("event_frame") > 0, "sprites 에 event_frame 이 없다");
    }

    [Fact]
    public void Checker_FindsMissing_AcceptsContainerLine_IgnoresCommentInString()
    {
        const string ok = """
            { "tilesets": [ { "id": "t",
                "props": {                        // 출처: 원본 확인
                  "13": "....",
                  "15": "x..." },
                "sol_known_diffs": [              // 출처: DevilutionX 참고
                  { "tile": 15, "sub": 0, "sol": "x", "use": "X", "why": "보정" } ] } ] }
            """;
        Assert.Empty(Problems(ok, "props", "sol_known_diffs"));

        // 키 줄에도 여는 객체 줄에도 없음 → 값이 걸친 줄마다
        const string missing = """
            { "a": {
                "props": {
                  "13": "...." } } }
            """;
        Assert.Equal(2, Problems(missing, "props").Count);   // 키 줄 + 안의 줄(닫는 괄호 포함)

        // 한 줄짜리 값: 줄 끝 주석 또는 그 줄을 담은 객체를 여는 줄
        const string clips = """
            { "clips": {                                         // 출처: DevilutionX 참고
                "attack": { "sprite": "a", "event_frame": 8 },
                "idle": { "sprite": "i" } },
              "x": { "y": {
                "frame_time": 0.05,                               // 출처: 원본 확인
                "event_frame": 3 } } }
            """;
        var p = Problems(clips, "frame_time", "event_frame");
        Assert.Single(p);
        Assert.Contains("6줄", p[0]);

        // 문자열 안의 "// 출처" 는 주석이 아니다
        const string inString = """
            { "o": {
                "frame_time": 0.1, "why": "// 출처: 원본 확인" } }
            """;
        Assert.Single(Problems(inString, "frame_time"));
    }

    private static List<string> Problems(string text, params string[] keys)
    {
        var problems = new List<string>();
        _ = FindKeys(text, keys, problems, "시험").ToList();
        return problems;
    }

    /// <summary>
    /// 원문에서 <paramref name="keys"/> 를 키로 쓰는 자리를 찾고, 그 값이 걸친 줄마다 출처 주석을 본다(없으면 <paramref name="problems"/> 에).
    /// 덮는 주석 = 그 줄 끝 주석, 또는 그 줄이 시작될 때 열려 있던 가장 안쪽 객체·배열을 연 줄의 끝 주석.
    /// </summary>
    private static IEnumerable<(string Key, int Line)> FindKeys(string text, string[] keys, List<string> problems, string file)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var code = new string[lines.Length];
        var hasSource = new bool[lines.Length];
        var containerAtStart = new int[lines.Length];
        var found = new List<(string Key, int Line, int End)>();
        var stack = new Stack<int>();
        // 열린 키: 값이 객체·배열이면 그 괄호 깊이가 닫힐 때 끝
        var pending = new List<(string Key, int Line, int Depth)>();
        (string Key, int Line)? afterKey = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var (c, comment) = SplitComment(lines[i]);
            code[i] = c;
            hasSource[i] = comment is not null && Sources.Any(s => comment.Contains(s, StringComparison.Ordinal));
            containerAtStart[i] = stack.Count > 0 ? stack.Peek() : -1;

            var j = 0;
            while (j < c.Length)
            {
                var ch = c[j];
                if (ch == '"')
                {
                    var end = j + 1;
                    while (end < c.Length && c[end] != '"') end += c[end] == '\\' ? 2 : 1;
                    var str = c[(j + 1)..Math.Min(end, c.Length)];
                    var k = end + 1;
                    while (k < c.Length && char.IsWhiteSpace(c[k])) k++;
                    if (k < c.Length && c[k] == ':' && keys.Contains(str))
                    {
                        afterKey = (str, i);
                        j = k + 1;
                        continue;
                    }
                    if (afterKey is { } ak) { found.Add((ak.Key, ak.Line, i)); afterKey = null; }
                    j = end + 1;
                    continue;
                }
                if (ch is '{' or '[')
                {
                    if (afterKey is { } ak) { pending.Add((ak.Key, ak.Line, stack.Count)); afterKey = null; }
                    stack.Push(i);
                }
                else if (ch is '}' or ']')
                {
                    if (stack.Count > 0) stack.Pop();
                    for (var p = pending.Count - 1; p >= 0; p--)
                        if (pending[p].Depth == stack.Count) { found.Add((pending[p].Key, pending[p].Line, i)); pending.RemoveAt(p); }
                }
                else if (!char.IsWhiteSpace(ch) && ch != ',' && ch != ':' && afterKey is { } ak)
                {
                    found.Add((ak.Key, ak.Line, i));   // 숫자·참거짓 값
                    afterKey = null;
                }
                j++;
            }
        }

        foreach (var (key, start, end) in found)
        {
            for (var l = start; l <= end; l++)
            {
                if (string.IsNullOrWhiteSpace(code[l])) continue;
                var covered = hasSource[l] || (containerAtStart[l] >= 0 && hasSource[containerAtStart[l]]);
                if (!covered) problems.Add($"{file} {l + 1}줄 ({key}): {lines[l].Trim()}");
            }
            yield return (key, start);
        }
    }

    /// <summary>한 줄을 코드와 <c>//</c> 주석으로 나눈다(문자열 안의 <c>//</c> 는 주석이 아니다).</summary>
    private static (string Code, string? Comment) SplitComment(string line)
    {
        var inString = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inString)
            {
                if (ch == '\\') i++;
                else if (ch == '"') inString = false;
            }
            else if (ch == '"') inString = true;
            else if (ch == '/' && i + 1 < line.Length && line[i + 1] == '/') return (line[..i], line[(i + 2)..]);
        }
        return (line, null);
    }

    private static string[] Files(string dir) =>
        Directory.Exists(dir) ? [.. Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal)] : [];
}
