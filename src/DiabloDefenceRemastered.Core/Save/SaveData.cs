using System.Text.Json;
using System.Text.Json.Serialization;
using DiabloDefenceRemastered.Core.Sim.Run;

namespace DiabloDefenceRemastered.Core.Save;

/// <summary>
/// 저장 파일 <c>save.json</c> 의 모양 (설계서 8.3, WP7). 기록 = 시작 액트 × 직업, 누적 금화, 설정, 마지막 직업. 해금 없음(v0.7).
/// 읽기는 <see cref="SaveBook"/> 이 규칙대로(모르는 키 무시·깨진 기록 한 줄만 버림·높은 판은 읽기 전용·깨진 JSON 은 <c>.bad</c>) 한다.
/// 게임 쪽 계약 형(<c>GameSettings</c>·<c>RunRecord</c>)은 Core 가 모르므로 같은 모양의 Core 형을 두고 게임 <c>SaveStore</c> 가 옮긴다.
/// </summary>
public sealed record SaveData
{
    /// <summary>지금 저장 모양의 판. 파일의 판이 이보다 높으면 읽기 전용.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;
    public int TotalGold { get; init; }
    public string? LastHero { get; init; }
    public SettingsData Settings { get; init; } = new();
    /// <summary><c>records.&lt;시작 액트&gt;.&lt;직업&gt;</c>. 키 순서는 쓸 때 서수 정렬(같은 내용 같은 바이트).</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, RecordData>> Records { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, RecordData>>(StringComparer.Ordinal);

    /// <summary>쓰기 옵션: snake_case, 들여쓰기(손으로 고치기 쉽게), null 은 생략.</summary>
    public static JsonSerializerOptions WriteOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>읽기 옵션: 모르는 키 무시(옛 <c>unlocked</c> 등), 주석·끝 쉼표 허용.</summary>
    public static JsonSerializerOptions ReadOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

/// <summary>설정(8.3 — 게임 <c>GameSettings</c> 와 같은 모양). 음량 null = 저장에 없음 → <c>rules.audio.defaults</c>.</summary>
public sealed record SettingsData
{
    public bool Fullscreen { get; init; }
    public bool ScreenShake { get; init; } = true;
    public bool ShowMinimap { get; init; } = true;
    public bool ShowDamageNumbers { get; init; } = true;
    public float? MasterVolume { get; init; }
    public float? SfxVolume { get; init; }
    public float? MusicVolume { get; init; }
}

/// <summary>
/// 깊이 기록(8.3 "깊이 모양", v0.7.1): 액트 ID·번호·순번·방 종류·그 액트 <c>rooms</c> 표 해시(<see cref="Sim.Run.RunSnapshot.RoomsHash"/>).
/// 읽을 때 액트가 지금 사슬에 없거나 해시가 다르면 깊이만 비운다.
/// </summary>
public sealed record DepthData
{
    public required string Act { get; init; }
    public int ActNumber { get; init; }
    public int Room { get; init; }
    public string Kind { get; init; } = "";
    public string RoomsHash { get; init; } = "";
}

/// <summary>시작 액트 × 직업 하나의 최고 기록. <see cref="BestDepth"/> null = 비운 깊이(어떤 판보다도 얕음).</summary>
public sealed record RecordData
{
    public DepthData? BestDepth { get; init; }
    public bool Victory { get; init; }
    public float BestTime { get; init; }
    public int Level { get; init; }
    public int Kills { get; init; }
}

/// <summary>한 판 결과(게임 <c>RunResult</c> 와 같은 모양 — 8.3).</summary>
public sealed record RunResultData(string StartActId, string HeroClassId, bool Victory, RunDepth Reached, string ReachedActId, float RunTime, int Level, int Kills, int Gold);

/// <summary>최고 기록 보기(게임 <c>RunRecord</c> 와 같은 모양). <see cref="BestDepth"/> null = 비운 깊이.</summary>
public sealed record RecordView(RunDepth? BestDepth, bool Victory, float BestTime, int Level, int Kills);

/// <summary>기록 비교(8.3 v0.7.1): 키 = (깊이, 승리, −걸린 시간)의 사전순 — 시간은 둘 다 승리일 때만 본다. 비운 깊이는 어떤 판보다도 얕다.</summary>
public static class RecordCompare
{
    /// <summary><paramref name="candidate"/> 가 <paramref name="current"/> 보다 나은가(같으면 거짓 — 그대로 둠).</summary>
    public static bool IsBetter(RunDepth candidateDepth, bool candidateVictory, float candidateTime, RunDepth? currentDepth, bool currentVictory, float currentTime)
    {
        if (currentDepth is not { } cur) return true;
        var c = candidateDepth.CompareTo(cur);
        if (c != 0) return c > 0;
        if (candidateVictory != currentVictory) return candidateVictory;
        // 같은 깊이·같은 승패: 둘 다 승리면 짧은 시간, 둘 다 패배면 그대로(8.3 "그 밖은 그대로")
        return candidateVictory && candidateTime < currentTime;
    }
}
