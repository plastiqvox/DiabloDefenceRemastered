using System.Security.Cryptography;

namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>
/// <c>d1/manifest.json</c> (설계서 1.4 출력, 계약 WP1b). 가져오기가 표의 모든 항목을 만든 뒤 <b>마지막에</b> 쓴다 → 없으면 "자원 없음".
/// <code>
/// { "format": 1, "tool": "&lt;도구·Core 어셈블리 SHA-256&gt;", "runtime": "9.0.x",
///   "sources": [ { "name": "diabdat.mpq", "size": …, "sha256": "…" } ],
///   "table_hash": "&lt;Table/*.json 을 이름 순으로 이은 바이트의 SHA-256&gt;",
///   "files": [ { "path": "d1/monsters/zombie.png", "size": …, "sha256": "…" } ] }
/// </code>
/// 해시는 소문자 16진. <c>files</c> 는 경로 서수 순, 경로는 Assets 뿌리 기준(<see cref="AssetFormat"/>).
/// manifest 자신·<c>import.log</c>·<c>.cache/</c> 는 목록에 없다.
/// </summary>
public sealed record AssetManifest
{
    /// <summary>형식 판(<see cref="AssetFormat.Version"/>).</summary>
    public int Format { get; init; }
    /// <summary>도구 판 = 도구·Core 어셈블리 파일 바이트의 SHA-256(사람이 올리는 번호가 아니다).</summary>
    public string Tool { get; init; } = "";
    /// <summary>.NET 런타임 판(압축 결과는 같은 런타임 판 안에서만 같다).</summary>
    public string Runtime { get; init; } = "";
    public IReadOnlyList<ManifestSource> Sources { get; init; } = [];
    /// <summary>가져오기 표 해시(<see cref="ImportTable.ComputeHash"/>).</summary>
    public string TableHash { get; init; } = "";
    public IReadOnlyList<ManifestFile> Files { get; init; } = [];

    public static AssetManifest Read(string path) => AssetJson.Read<AssetManifest>(path);

    public bool TryGetFile(string path, out ManifestFile file)
    {
        foreach (var f in Files)
            if (string.Equals(f.Path, path, StringComparison.Ordinal)) { file = f; return true; }
        file = null!;
        return false;
    }

    /// <summary>
    /// 낡음 판정(시작 검사 ①·②, 1.4 끝 — 시험의 "낡음" 건너뜀도 같은 함수). 문제가 없으면 빈 목록.
    /// </summary>
    /// <param name="currentTableHash">지금 저장소(게임은 출력 <c>ImportTable\</c>)의 표 해시.</param>
    public IReadOnlyList<string> Staleness(string currentTableHash)
    {
        var problems = new List<string>();
        if (Format != AssetFormat.Version)
            problems.Add($"자원 형식이 낡았다(판 {Format} → {AssetFormat.Version}). 가져오기를 다시 돌린다");
        if (!string.Equals(TableHash, currentTableHash, StringComparison.OrdinalIgnoreCase))
            problems.Add("가져오기 표가 바뀌었다 — 가져오기를 다시 돌린다");
        return problems;
    }

    /// <summary>파일 바이트의 SHA-256(소문자 16진). 도구·검사가 같은 함수를 쓴다.</summary>
    public static string Sha256Of(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    public static string Sha256OfFile(string path)
    {
        using var s = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(s));
    }
}

public sealed record ManifestSource
{
    public required string Name { get; init; }
    public long Size { get; init; }
    public required string Sha256 { get; init; }
}

public sealed record ManifestFile
{
    /// <summary>Assets 뿌리 기준(<c>d1/…</c>).</summary>
    public required string Path { get; init; }
    public long Size { get; init; }
    public required string Sha256 { get; init; }
}
