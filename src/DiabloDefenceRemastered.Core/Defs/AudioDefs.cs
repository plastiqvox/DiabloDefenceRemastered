namespace DiabloDefenceRemastered.Core.Defs;

// 소리 데이터 (설계서 6.15, 계약 WP1b, 데이터·검사기·재생은 WP8). 시뮬레이션(Core Sim/)은 이 형들을 쓰지 않는다 —
// 시뮬레이션은 신호(CueEvent)만 내고, 게임 AudioSystem 이 연결 표로 소리를 고른다(7.9).

/// <summary>
/// 효과음 <c>sounds[]</c>. 파일은 가져오기 출력 이름(<c>d1/sound/…</c>, 원작 경로 소문자·'/').
/// 소리 길이는 정의에 적지 않고 소리 색인(<c>Assets/SoundIndex</c>)에서 읽는다.
/// </summary>
public sealed record SoundDef : IDef
{
    public required string Id { get; init; }
    /// <summary>여럿이면 무작위(게임 쪽 난수 — 시뮬레이션 난수와 따로).</summary>
    public IReadOnlyList<string> Files { get; init; } = [];
    /// <summary><c>rules.audio.categories</c> 의 ID. 우선순위는 그 목록 순서(앞이 높음).</summary>
    public required string Category { get; init; }
    /// <summary>분류 배율에 곱한다.</summary>
    public float Volume { get; init; } = 1f;
    /// <summary>같은 소리 동시 재생 수(없으면 <c>rules.audio.same_sound</c>).</summary>
    public int? MaxInstances { get; init; }
    /// <summary>같은 소리 최소 간격(초, 없으면 <c>rules.audio.min_interval</c>).</summary>
    public float? MinInterval { get; init; }
    /// <summary>높낮이 ± 비율.</summary>
    public float PitchJitter { get; init; }
    /// <summary>분류의 화면 밖 규칙 덮어쓰기.</summary>
    public bool? Offscreen { get; init; }
    public bool Loop { get; init; }
}

/// <summary>음악 <c>music[]</c>.</summary>
public sealed record MusicDef : IDef
{
    public required string Id { get; init; }
    public required string File { get; init; }
    public bool Loop { get; init; } = true;
}

/// <summary>
/// 소리 연결 표 <c>sound_bindings</c>(객체 하나, 데이터 파일 <c>audio/bindings.json</c>). 무엇이 일어나면 어느 소리를 내나.
/// 사전 키 = 그 종류의 정의 ID(무기·투사체·이펙트·그림 묶음·적·획득물) — 키가 있는지는 WP8 검사기가 본다.
/// 값은 소리·음악 참조라 로더 연결 단계(6.2 의 5단계)가 채운다.
/// </summary>
public sealed record SoundBindingsDef
{
    public IReadOnlyDictionary<string, FireSoundsDef> Weapons { get; init; } = new Dictionary<string, FireSoundsDef>();
    public IReadOnlyDictionary<string, FireSoundsDef> Projectiles { get; init; } = new Dictionary<string, FireSoundsDef>();
    /// <summary>이펙트 ID → 소리(<c>FxEvent</c> 가 날 때).</summary>
    public IReadOnlyDictionary<string, DefRef<SoundDef>> Effects { get; init; } = new Dictionary<string, DefRef<SoundDef>>();
    /// <summary>이펙트 ID → 반복 소리(그 이펙트의 장판이 하나라도 살아 있는 동안 하나).</summary>
    public IReadOnlyDictionary<string, DefRef<SoundDef>> HazardLoops { get; init; } = new Dictionary<string, DefRef<SoundDef>>();
    /// <summary>그림 묶음 ID → 몬스터 소리. 같은 묶음을 쓰는 새 적은 소리도 따라온다.</summary>
    public IReadOnlyDictionary<string, UnitSoundsDef> SpriteSets { get; init; } = new Dictionary<string, UnitSoundsDef>();
    /// <summary>적 ID → 덮어쓰기와 보스 대사(<c>voice</c>).</summary>
    public IReadOnlyDictionary<string, EnemySoundsDef> Enemies { get; init; } = new Dictionary<string, EnemySoundsDef>();
    public IReadOnlyDictionary<string, PickupSoundsDef> Pickups { get; init; } = new Dictionary<string, PickupSoundsDef>();
    public HeroSoundsDef Hero { get; init; } = new();
    /// <summary>UI 소리 키(<c>menu_move</c>, <c>menu_select</c>, <c>level_up</c>, <c>chest_open</c>, <c>elite_pack</c>) → 소리.</summary>
    public IReadOnlyDictionary<string, DefRef<SoundDef>> Ui { get; init; } = new Dictionary<string, DefRef<SoundDef>>();
    /// <summary>음악 키(<c>title</c> 또는 스테이지 ID) → 곡.</summary>
    public IReadOnlyDictionary<string, DefRef<MusicDef>> Music { get; init; } = new Dictionary<string, DefRef<MusicDef>>();
}

public sealed record FireSoundsDef
{
    public DefRef<SoundDef>? Fire { get; init; }
}

public sealed record UnitSoundsDef
{
    public DefRef<SoundDef>? Attack { get; init; }
    public DefRef<SoundDef>? Hit { get; init; }
    public DefRef<SoundDef>? Death { get; init; }
    public DefRef<SoundDef>? Special { get; init; }
}

public sealed record EnemySoundsDef
{
    public DefRef<SoundDef>? Attack { get; init; }
    public DefRef<SoundDef>? Hit { get; init; }
    public DefRef<SoundDef>? Death { get; init; }
    public DefRef<SoundDef>? Special { get; init; }
    /// <summary>보스 등장·페이즈 대사(신호 <c>BossArrived</c>·<c>BossPhase</c>).</summary>
    public DefRef<SoundDef>? Voice { get; init; }
}

public sealed record PickupSoundsDef
{
    public DefRef<SoundDef>? Collect { get; init; }
    public DefRef<SoundDef>? Drop { get; init; }
}

public sealed record HeroSoundsDef
{
    public DefRef<SoundDef>? Hurt { get; init; }
    public DefRef<SoundDef>? Death { get; init; }
}

/// <summary>소리 규칙 <c>rules.audio</c>(WP8 파일 <c>rules/audio.json</c>, 기획 12.7).</summary>
public sealed record AudioRulesDef
{
    /// <summary>전체 동시 재생 수. 자리가 없으면 새 소리를 버린다.</summary>
    public int Voices { get; init; } = 24;
    public int SameSound { get; init; } = 3;
    public float MinInterval { get; init; } = 0.05f;
    public float MonsterHitInterval { get; init; } = 0.08f;
    public MonsterDeathAudioDef MonsterDeath { get; init; } = new();
    /// <summary>화면 밖 판정 때 시야를 넓히는 값(px).</summary>
    public float OffscreenMargin { get; init; } = 100f;
    public float Pan { get; init; } = 0.5f;
    public VolumeDefaultsDef Defaults { get; init; } = new();
    public float MusicFade { get; init; } = 0.5f;
    public VoiceDuckDef VoiceDuck { get; init; } = new();
    /// <summary>일시정지 중 음악 배율.</summary>
    public float PauseMusic { get; init; } = 0.5f;
    /// <summary>분류. 순서 = 우선순위(앞이 높음).</summary>
    public IReadOnlyList<SoundCategoryDef> Categories { get; init; } = [];
}

public sealed record MonsterDeathAudioDef
{
    public int Voices { get; init; } = 4;
    public float SameKindInterval { get; init; } = 0.1f;
}

/// <summary>저장 파일에 음량이 없을 때의 기본값(0~1).</summary>
public sealed record VolumeDefaultsDef
{
    public float Master { get; init; } = 0.8f;
    public float Sfx { get; init; } = 0.8f;
    public float Music { get; init; } = 0.6f;
}

public sealed record VoiceDuckDef
{
    public float To { get; init; } = 0.5f;
    public float Time { get; init; } = 2f;
}

public sealed record SoundCategoryDef
{
    public required string Id { get; init; }
    public float Volume { get; init; } = 1f;
    /// <summary>false 면 시야(+ offscreen_margin) 밖에서 난 소리를 버린다.</summary>
    public bool Offscreen { get; init; }
}
