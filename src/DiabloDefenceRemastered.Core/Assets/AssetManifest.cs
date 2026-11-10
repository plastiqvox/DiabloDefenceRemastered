using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>
/// <c>d1/manifest.json</c> (설계서 1.4 출력, 계약 WP1b). 가져오기가 표의 모든 항목을 만든 뒤 <b>마지막에</b> 쓴다 → 없으면 "자원 없음".
/// <code>
/// { "format": 2, "tool": "&lt;도구·Core 어셈블리 SHA-256&gt;", "runtime": "9.0.x",
///   "sources": [ { "name": "diabdat.mpq", "size": …, "sha256": "…" }, { "name": "hellfire.mpq", "size": …, "sha256": "…", "bundle": "hellfire" } ],
///   "skipped_bundles": [ { "bundle": "hfmonk", "mpq": "hfmonk.mpq", "reason": "없음" } ],
///   "table_hash": "&lt;Table/*.json 을 이름 순으로 이은 바이트의 SHA-256&gt;",
///   "files": [ { "path": "d1/monsters/zombie.png", "size": …, "sha256": "…", "sources": ["diabdat.mpq"] } ] }
/// </code>
/// v0.6(형식 판 2): 원본 줄의 <c>bundle</c>, 건너뛴 묶음 <c>skipped_bundles</c>, 파일 줄의 <c>bundle</c>·<c>sources</c>(1.4 "헬파이어 묶음").
/// v0.8(형식 판 3, 사용자 결정 U — 1.4 "AI 확대"): <c>upscale</c>(<see cref="ManifestUpscale"/>), 확대한 파일 줄의 <c>scale</c>(그림 배율, 1 이면 적지 않음).
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
    /// <summary>건너뛴 묶음(v0.6): 묶음 MPQ 가 없거나 <c>--skip-bundles</c>·열 수 없음·판 다름. 여기 있는 묶음의 파일은 시작 검사가 "꺼짐" 으로 센다.</summary>
    public IReadOnlyList<ManifestSkippedBundle> SkippedBundles { get; init; } = [];
    /// <summary>가져오기 표 해시(<see cref="ImportTable.ComputeHash"/>).</summary>
    public string TableHash { get; init; } = "";
    /// <summary>AI 확대 요청·결과(v0.8). 확대를 주지 않은 실행은 <c>status: off</c>.</summary>
    public ManifestUpscale Upscale { get; init; } = new();
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
    /// <summary>묶음 MPQ 면 그 묶음(v0.6). diabdat 은 없음(쓰지 않음).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentBundle? Bundle { get; init; }
}

/// <summary>
/// 건너뛴 묶음 하나(v0.6, 1.4). <see cref="Reason"/> 은 넷 중 하나: <see cref="ReasonMissing"/>, <see cref="ReasonSkipped"/>,
/// <see cref="ReasonUnreadable"/>(v0.6.1), <see cref="ReasonVersionPrefix"/> + 빠진 원본 경로 목록(v0.6.1).
/// </summary>
public sealed record ManifestSkippedBundle(ContentBundle Bundle, string Mpq, string Reason)
{
    public const string ReasonMissing = "없음";
    public const string ReasonSkipped = "--skip-bundles";
    public const string ReasonUnreadable = "열 수 없음";
    /// <summary>판이 달라 그 묶음 표 항목의 원본이 빠짐 — 뒤에 빠진 경로 목록(<c>판 다름: a, b</c>).</summary>
    public const string ReasonVersionPrefix = "판 다름";

    /// <summary>MPQ 가 있는데 쓸 수 없었다(열 수 없음·판 다름) — 타이틀 한 줄 앞말 "읽지 못함(&lt;MPQ&gt;)". 거짓이면 "없음"(없음·--skip-bundles).</summary>
    [JsonIgnore]
    public bool Unreadable => Reason == ReasonUnreadable || Reason.StartsWith(ReasonVersionPrefix, StringComparison.Ordinal);
}

public sealed record ManifestFile
{
    /// <summary>Assets 뿌리 기준(<c>d1/…</c>).</summary>
    public required string Path { get; init; }
    public long Size { get; init; }
    public required string Sha256 { get; init; }
    /// <summary>그 파일을 만든 표 항목의 묶음(v0.6, 맵 그림은 타일셋 항목의 묶음). 기본은 없음(쓰지 않음).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentBundle? Bundle { get; init; }
    /// <summary>
    /// 바이트를 읽은 MPQ 이름 목록(v0.6.1, 서수 정렬 — 고유 몬스터 페이지처럼 CL2 는 hellfire, TRN 은 diabdat 인 출력이 있다).
    /// 원작 exe PNG·맵 그림 밖의 계산 출력(소리 색인 등)은 읽은 MPQ 들.
    /// </summary>
    public IReadOnlyList<string> Sources { get; init; } = [];
    /// <summary>
    /// 그림 배율(v0.8 — 그림 픽셀 / 월드 px, AI 확대한 파일은 <c>output_scale</c>). 기본 1 이고 1 이면 JSON 에 적지 않는다(<c>scale</c>).
    /// 확대한 파일은 같은 바이트를 요구하지 않는다(1.4 "결정론" 예외).
    /// </summary>
    [JsonIgnore]
    public int Scale { get; init; } = 1;

    /// <summary>JSON <c>scale</c>(1 이면 없음) — <see cref="Scale"/> 를 쓴다.</summary>
    [JsonPropertyName("scale"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public int? ScaleJson
    {
        get => Scale == 1 ? null : Scale;
        init => Scale = value ?? 1;
    }
}

/// <summary>AI 확대 결과(v0.8, 1.4): <c>off</c> = 요청 없음, <c>done</c> = 확대함, <c>fallback</c> = 요청했으나 못 해 <b>모든 그림을 원본 픽셀로</b>(일부만 확대하지 않음).</summary>
public enum UpscaleStatus : byte { Off, Done, Fallback }

/// <summary>
/// manifest <c>upscale</c>(v0.8, 1.4 "AI 확대"):
/// <c>{ "status": "off|done|fallback", "model": "realesrgan-x4plus", "tool": "realesrgan-ncnn-vulkan v0.2.5.0", "files": { "&lt;상대 경로&gt;": "&lt;sha256&gt;" }, "output_scale": 2, "gpu": "default|&lt;n&gt;|mixed", "reason": "…" }</c>.
/// 그림 배율 s = <see cref="Status"/> 가 <c>done</c> 이면 <see cref="OutputScale"/>, 아니면 1(7.1 — 시작 검사 WP7). <see cref="Gpu"/> 는 만든 때의 값(v0.8.1 — 판마다 다르면 <c>mixed</c>).
/// </summary>
public sealed record ManifestUpscale
{
    public UpscaleStatus Status { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tool { get; init; }
    /// <summary>쓴 확대 프로그램·모델 파일(<c>--upscale-bin</c> 기준 상대 경로 → SHA-256).</summary>
    public IReadOnlyDictionary<string, string> Files { get; init; } = new Dictionary<string, string>();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? OutputScale { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Gpu { get; init; }
    /// <summary>되돌아간 이유(<c>fallback</c> 일 때 — 끝 보고 <c>경고:</c> 줄과 같음).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; init; }

    /// <summary>그림 배율 s(7.1): <c>done</c> 이면 <see cref="OutputScale"/>(없으면 1), 아니면 1.</summary>
    [JsonIgnore]
    public int ArtScale => Status == UpscaleStatus.Done ? OutputScale ?? 1 : 1;
}
