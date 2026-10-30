using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Defs;

public enum MovementKind : byte { Flow, Astar, Direct }

/// <summary>정의 자체의 등급. 정예·우두머리는 정의가 아니라 생성 때 붙는 등급(<c>EnemyRank</c>).</summary>
public enum DefRank : byte { Normal, Miniboss, Boss }

/// <summary>죽음 연출. anim(죽기 클립 재생)만 남았다 — v0.2 의 knight_fade(원작 exe 검사 그림)는 없앴다.</summary>
public enum DeathStyle : byte { Anim }

/// <summary>
/// 몬스터 <c>enemies[]</c> (설계서 6.5). <c>extends</c>·<c>abstract</c>·<c>*_add</c>·<c>*_patch</c> 는 로딩 3단계에서 풀리고 지워진다.
/// 적 하나 = 이동 방식 1개 + 부품 여러 개(<see cref="Behaviors"/>·<see cref="Patterns"/>) + 인스턴스 상태.
/// </summary>
public sealed record EnemyDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public required DefRef<SpriteSetDef> Sprites { get; init; }
    /// <summary>그림 묶음의 원작 TRN 변종 이름(<c>""</c> = 원색). 묶음 <c>variants</c> 안에 있어야 한다. 변종이 다르면 다른 페이지 파일이다(7.5).</summary>
    public string Variant { get; init; } = "";
    /// <summary>덧입힘(색조·발광·알파·크기)은 정예·우두머리·접두사 표시와 원작에 없는 변형에 쓴다. 일반 몬스터는 될 수 있으면 TRN 만.</summary>
    public Rgb Tint { get; init; } = Rgb.White;
    public GlowDef? Glow { get; init; }
    public float Scale { get; init; } = 1f;
    public float Alpha { get; init; } = 1f;
    /// <summary>몸 반경. 없으면 round(그림 묶음 base_radius × scale).</summary>
    public float? Radius { get; init; }
    public float Hp { get; init; }
    public float Speed { get; init; }
    public float Damage { get; init; }
    public float Armor { get; init; }
    public float KnockbackResist { get; init; }
    public int Xp { get; init; }
    public MovementKind Movement { get; init; } = MovementKind.Flow;

    [PartKind(PartKind.EnemyBehavior)]
    public IReadOnlyList<PartDef> Behaviors { get; init; } = [];
    public IReadOnlyList<PatternDef> Patterns { get; init; } = [];
    public IReadOnlyList<PhaseDef> Phases { get; init; } = [];

    public DeathStyle Death { get; init; } = DeathStyle.Anim;
    public DefRef<DropTableDef>? DropTable { get; init; }
    public DefRank Rank { get; init; } = DefRank.Normal;
    /// <summary>확률 정예 무리의 종류 뽑기에 넣을지. miniboss·boss 는 늘 빠진다.</summary>
    public bool EliteEligible { get; init; } = true;
    /// <summary>[최소, 최대] 일반 생성 무리 크기(없으면 rules.spawn.group).</summary>
    public int[]? SpawnGroup { get; init; }
    public float AnimSpeed { get; init; } = 1f;
    public EnrageDef? Enrage { get; init; }
    public ArrivalDef? Arrival { get; init; }
    /// <summary><c>knockback</c>, <c>slow</c> … 버프 <c>debuff</c> 이름과 같은 말.</summary>
    public IReadOnlyList<string> Immunities { get; init; } = [];
    public bool ShowBossBar { get; init; }
    /// <summary>시간 배율 적용 여부(중간 보스·보스 false).</summary>
    public bool TimeScaling { get; init; } = true;
}

/// <summary>
/// 패턴(데코레이터, 6.6): 조건 → 클립 → 예고 → 이벤트 프레임에서 <see cref="Actions"/> 를 순서대로.
/// 여러 틱 동작(dash)은 목록 마지막에만(검증).
/// </summary>
public sealed record PatternDef
{
    public string? Id { get; init; }
    /// <summary>[최소, 최대] 거리.</summary>
    public float[]? RangeBand { get; init; }
    public float? Cooldown { get; init; }
    /// <summary>HP 비율 문턱 1회.</summary>
    public float? HpOnce { get; init; }
    public ClipKind? Clip { get; init; }
    /// <summary>1부터.</summary>
    public int? EventFrame { get; init; }
    public TelegraphDef? Telegraph { get; init; }

    [PartKind(PartKind.EnemyAction)]
    public IReadOnlyList<PartDef> Actions { get; init; } = [];
}

/// <summary>
/// 페이즈. 원본 <see cref="Patch"/>(패턴 ID 별 깊은 병합, 패턴 안 동작은 <c>actions_patch</c>)·<see cref="Add"/>·<see cref="Remove"/> 를
/// <b>로더(WP7)가 푼다</b>: 3단계에서 <c>*_patch</c> 와 같은 깊은 병합으로 페이즈마다 완성된 패턴 JSON 목록을 만들고,
/// 4단계(부품 매개변수 역직렬화·부품 생성)·5단계(DefRef 연결)를 거쳐 <see cref="Patterns"/> 에 채운다.
/// WP4 는 <see cref="Patterns"/> 만 읽어 슬롯 번호 표를 만든다(JSON 을 다시 풀지 않는다).
/// </summary>
public sealed record PhaseDef
{
    public string? Id { get; init; }
    public float HpBelow { get; init; }
    public float Invulnerable { get; init; }
    public ShakeDef? Shake { get; init; }
    public GlowDef? Glow { get; init; }
    public float? Speed { get; init; }
    /// <summary>이 페이즈에 들어설 때 대사를 한 번 더 낸다(신호 <c>BossPhase</c>, 도살자 2페이즈, 7.9).</summary>
    public bool Voice { get; init; }
    public JsonObject? Patch { get; init; }
    public IReadOnlyList<PatternDef> Add { get; init; } = [];
    public IReadOnlyList<string> Remove { get; init; } = [];

    /// <summary>
    /// 이 페이즈에서 쓰는 완성된 패턴 목록(로더가 채움, JSON 에 적지 않는다). 정의 <c>patterns</c> 에서 시작해
    /// 첫 페이즈부터 이 페이즈까지의 patch → add → remove 를 차례로 적용한 결과다. 같은 ID 패턴은 원래 자리를 지킨다.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<PatternDef> Patterns { get; init; } = [];
}

/// <summary>광폭화. after 는 등장 시각부터 센다.</summary>
public sealed record EnrageDef
{
    public ClockTime After { get; init; }
    public float DamageMul { get; init; } = 1f;
    public float SpeedMul { get; init; } = 1f;
}

public sealed record ArrivalDef
{
    public string Banner { get; init; } = "";
    public float Time { get; init; }
    public ShakeDef? Shake { get; init; }
}

/// <summary>
/// 접두사 <c>affixes[]</c> (설계서 6.7). JSON 의 <c>behaviors_add</c> 는 부모가 없으므로 로딩 때 <see cref="Behaviors"/> 가 된다.
/// 정의 부품 뒤 슬롯에 붙는다.
/// </summary>
public sealed record AffixDef : IDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public IReadOnlyDictionary<EnemyStatKey, float> Stats { get; init; } = new Dictionary<EnemyStatKey, float>();
    public IReadOnlyList<string> Immunities { get; init; } = [];
    public Rgb? Glow { get; init; }
    public IReadOnlyList<string> ExcludeTags { get; init; } = [];
    public IReadOnlyList<string> ExcludeParts { get; init; } = [];

    [PartKind(PartKind.EnemyBehavior)]
    public IReadOnlyList<PartDef> Behaviors { get; init; } = [];
}

/// <summary>버프 <c>buffs[]</c>. 같은 버프는 큰 값 하나만, 다른 버프는 더한다. debuff 이름이 정의 immunities 에 있으면 안 붙는다.</summary>
public sealed record BuffDef : IDef
{
    public required string Id { get; init; }
    public IReadOnlyDictionary<EnemyStatKey, float> Stats { get; init; } = new Dictionary<EnemyStatKey, float>();
    public Rgb? FootRing { get; init; }
    public string? Debuff { get; init; }
}
