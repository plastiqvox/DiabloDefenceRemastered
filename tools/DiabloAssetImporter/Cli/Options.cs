namespace DiabloAssetImporter.Cli;

/// <summary>입력 오류(종료 코드 2 — 경로·옵션). 입력을 기다리지 않고 문장 하나로 끝낸다.</summary>
internal sealed class UsageException(string message) : Exception(message);

/// <summary>명령줄(설계서 1.4 "실행"). 값 옵션은 <c>--이름 값</c>.</summary>
internal sealed class Options
{
    public string Command { get; private set; } = "";
    public List<string> Positional { get; } = [];
    private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);

    private static readonly HashSet<string> Flags = ["--force"];

    public static Options Parse(IReadOnlyList<string> args)
    {
        var o = new Options();
        for (var i = 0; i < args.Count; i++)
        {
            var a = args[i];
            if (a.StartsWith("--", StringComparison.Ordinal))
            {
                if (Flags.Contains(a)) { o._values[a] = null; continue; }
                if (i + 1 >= args.Count) throw new UsageException($"{a} 에 값이 없다");
                o._values[a] = args[++i];
            }
            else if (o.Command.Length == 0) o.Command = a;
            else o.Positional.Add(a);
        }
        return o;
    }

    public bool Has(string name) => _values.ContainsKey(name);
    public string? Get(string name) => _values.TryGetValue(name, out var v) ? v : null;

    public int GetInt(string name, int fallback)
    {
        var v = Get(name);
        if (v is null) return fallback;
        return int.TryParse(v, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var n) && n >= 0
            ? n : throw new UsageException($"{name} 값 '{v}' 는 0 이상 정수다");
    }

    /// <summary>알 수 없는 옵션이 있으면 오류(오타가 조용히 무시되지 않게).</summary>
    public void Allow(params string[] names)
    {
        foreach (var k in _values.Keys)
            if (!names.Contains(k)) throw new UsageException($"'{Command}' 에 없는 옵션 {k}");
    }
}
