using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DiabloDefenceRemastered.Core.Dev;

/// <summary>
/// 로그 첫 줄 = 재현 정보 (설계서 3.4·9절, WP7):
/// <c>seed=… config=Release|Debug commit=&lt;있으면&gt; data=&lt;Data 내용 해시&gt; hero=&lt;직업&gt; start=&lt;액트&gt; room=&lt;a:n&gt;(있을 때) args="&lt;전체 명령줄&gt;"</c>.
/// 그래픽과 무관해 Core 에서 시험한다.
/// </summary>
public static class RunHeader
{
    /// <summary>
    /// Data 내용 해시: 파일을 상대 경로 서수 순으로, 경로·길이·바이트를 SHA-256 에 넣은 앞 8바이트(16진 16자).
    /// 줄 끝은 그대로(같은 git 작업 트리의 같은 파일이면 같다).
    /// </summary>
    public static string DataHash(IEnumerable<(string RelPath, byte[] Bytes)> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (rel, bytes) in files.OrderBy(f => f.RelPath.Replace('\\', '/'), StringComparer.Ordinal))
        {
            sha.AppendData(Encoding.UTF8.GetBytes(rel.Replace('\\', '/')));
            sha.AppendData(BitConverter.GetBytes((long)bytes.Length));
            sha.AppendData(bytes);
        }
        return Convert.ToHexString(sha.GetHashAndReset(), 0, 8).ToLowerInvariant();
    }

    /// <summary>폴더의 <c>**/*.json</c> 해시(<see cref="DataHash(IEnumerable{ValueTuple{string, byte[]}})"/>). 폴더가 없으면 <c>none</c>.</summary>
    public static string DataHash(string dataDir)
    {
        if (!Directory.Exists(dataDir)) return "none";
        return DataHash(Directory.EnumerateFiles(dataDir, "*.json", SearchOption.AllDirectories)
            .Select(f => (Path.GetRelativePath(dataDir, f), File.ReadAllBytes(f))));
    }

    /// <summary>
    /// 첫 줄을 만든다. <paramref name="commit"/>·<paramref name="room"/> 이 null 이면 그 칸을 뺀다.
    /// 명령줄은 인자마다 공백이 있으면 따옴표로 감싸고 줄 안의 <c>"</c> 는 <c>\"</c> 로.
    /// </summary>
    public static string Format(ulong seed, string config, string? commit, string dataHash, string hero, string start, RoomRef? room, IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"seed={seed} config={config}");
        if (!string.IsNullOrEmpty(commit)) sb.Append(" commit=").Append(commit);
        sb.Append(" data=").Append(dataHash);
        sb.Append(" hero=").Append(hero);
        sb.Append(" start=").Append(start);
        if (room is { } r) sb.Append(CultureInfo.InvariantCulture, $" room={r.Act}:{r.Room}");
        sb.Append(" args=\"").Append(JoinArgs(args).Replace("\"", "\\\"", StringComparison.Ordinal)).Append('"');
        return sb.ToString();
    }

    public static string JoinArgs(IReadOnlyList<string> args) =>
        string.Join(' ', args.Select(a => a.Length == 0 || a.Contains(' ', StringComparison.Ordinal) ? "\"" + a + "\"" : a));

    /// <summary>
    /// <c>AssemblyInformationalVersion</c>(<c>1.0.0+&lt;sha&gt;</c>)의 <c>+</c> 뒤 커밋(앞 12자). 없으면 null.
    /// </summary>
    public static string? CommitFrom(string? informationalVersion)
    {
        if (informationalVersion is null) return null;
        var i = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (i < 0 || i == informationalVersion.Length - 1) return null;
        var sha = informationalVersion[(i + 1)..];
        return sha.Length > 12 ? sha[..12] : sha;
    }

    /// <summary><c>--seed</c> 가 없을 때 시각에서 만든 씨앗(0 이 아님).</summary>
    public static ulong SeedFromClock(DateTime utcNow)
    {
        var x = (ulong)utcNow.Ticks;
        // splitmix64 한 번 — 가까운 시각끼리도 흩어지게
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= x >> 31;
        return x == 0 ? 1 : x;
    }
}
