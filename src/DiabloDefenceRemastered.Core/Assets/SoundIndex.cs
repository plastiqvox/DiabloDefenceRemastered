namespace DiabloDefenceRemastered.Core.Assets;

/// <summary>
/// 소리 색인 <c>d1/sound/index.json</c> (설계서 1.4·6.15·7.9, 계약 WP1b): 파일마다 표본 수·표본율·채널.
/// 소리 길이를 정의에 적지 않고 여기서 읽는다 — 무음 실행도 같은 길이로 동시 재생 자리를 세서 <c>AUDIO</c> 수가 소리 켬과 같다.
/// <code>
/// { "format": 1, "files": [ { "path": "d1/sound/sfx/misc/cbolt.wav", "samples": 12345, "rate": 22050, "channels": 1 }, … ] }
/// </code>
/// <c>files</c> 는 경로 서수 순. 표본 수 = 데이터 바이트 ÷ (채널 × 2)(가져오기가 16비트로 바꾼 뒤, 부록 A.6).
/// </summary>
public sealed class SoundIndex
{
    public const int CurrentFormat = 1;

    private Dictionary<string, SoundInfo>? _byPath;

    public int Format { get; init; }
    public IReadOnlyList<SoundInfo> Files { get; init; } = [];

    public static SoundIndex Read(string path) => AssetJson.Read<SoundIndex>(path);
    public static SoundIndex Parse(string json) => AssetJson.Deserialize<SoundIndex>(json);

    public bool TryGet(string path, out SoundInfo info)
    {
        _byPath ??= BuildIndex();
        if (_byPath.TryGetValue(path, out var i)) { info = i; return true; }
        info = null!;
        return false;
    }

    private Dictionary<string, SoundInfo> BuildIndex()
    {
        var d = new Dictionary<string, SoundInfo>(Files.Count, StringComparer.Ordinal);
        foreach (var f in Files) d[f.Path] = f;
        return d;
    }

    /// <summary>검사: 형식 판, 경로 규칙·서수 순·중복 없음, 표본율·채널 &gt; 0, 표본 수 ≥ 0.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Format != CurrentFormat) errors.Add($"소리 색인 형식 {Format} ≠ {CurrentFormat}");
        for (var i = 0; i < Files.Count; i++)
        {
            var f = Files[i];
            if (!AssetFormat.IsValidRelativePath(f.Path)) errors.Add($"files[{i}] 경로 '{f.Path}' 가 틀렸다");
            if (f.Rate <= 0 || f.Channels <= 0 || f.Samples < 0) errors.Add($"files[{i}] {f.Path}: 표본 수·표본율·채널이 틀렸다");
            if (i > 0 && string.CompareOrdinal(Files[i - 1].Path, f.Path) >= 0) errors.Add($"files[{i}] {f.Path}: 경로 서수 순이 아니거나 중복");
        }
        return errors;
    }
}

public sealed record SoundInfo
{
    /// <summary>Assets 뿌리 기준(<c>d1/sound/…</c>).</summary>
    public required string Path { get; init; }
    /// <summary>채널당 표본 수.</summary>
    public long Samples { get; init; }
    /// <summary>표본율(Hz).</summary>
    public int Rate { get; init; }
    public int Channels { get; init; }

    public double Seconds => Rate > 0 ? (double)Samples / Rate : 0;

    /// <summary>
    /// 재생 길이를 앱 틱으로 올림(7.9 고르기의 "자리가 비는 시각"): ceil(표본 수 / 표본율 ÷ 높낮이 배율 × 틱/초).
    /// </summary>
    public int DurationTicks(float pitch = 1f, int ticksPerSecond = 60)
    {
        if (Rate <= 0 || pitch <= 0f) return 0;
        // 정수 표본·표본율은 double 로 정확하다. 부동소수 잡음으로 정수 경계에서 한 틱 넘지 않게 작은 값을 뺀다
        var ticks = Samples / (double)Rate / pitch * ticksPerSecond;
        return (int)Math.Ceiling(ticks - 1e-9);
    }
}
