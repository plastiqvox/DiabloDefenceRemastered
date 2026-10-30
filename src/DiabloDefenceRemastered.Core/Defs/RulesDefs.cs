namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// <c>rules</c>(객체 하나). 여러 파일에서 하위 키 단위로 합친다(같은 하위 키가 두 파일에 있으면 오류, 설계서 6.1).
/// WP3 <c>rules/hero.json</c> = hero·xp_curve·level_up·level_up_push, WP4 <c>rules/enemies.json</c> = ranks·enemies·caps·spawn,
/// WP8 <c>rules/audio.json</c> = audio.
/// </summary>
public sealed record RulesDef
{
    public HeroRulesDef? Hero { get; init; }
    public IReadOnlyList<XpCurveSegment> XpCurve { get; init; } = [];
    public LevelUpRulesDef? LevelUp { get; init; }
    public LevelUpPushDef? LevelUpPush { get; init; }
    public RanksDef? Ranks { get; init; }
    /// <summary>적 정의 검사 기준(예고 필수 판정, 6.5·6.14 의 13.5-13).</summary>
    public EnemyRulesDef? Enemies { get; init; }
    public CapsDef? Caps { get; init; }
    public SpawnRulesDef? Spawn { get; init; }
    public AudioRulesDef? Audio { get; init; }
}

public sealed record EnemyRulesDef
{
    public TelegraphRequiredDef TelegraphRequired { get; init; } = new();
}

/// <summary>
/// 예고가 꼭 필요한 "큰 피해" 기준(기획 12.6, 판정 설계서 6.5). 피해는 기본 피해(정의 값, 배율 전).
/// 범위 즉발·돌진이 기본 피해 ≥ <see cref="MinDamage"/> 면 예고 ≥ <see cref="MinTime"/>.
/// 근접 단일 대상(닿는 거리 ≤ 두 반경 + <see cref="MeleeReachOverContact"/>)은 면제, 단 피해 ≥ <see cref="HeavyMeleeDamage"/> 면
/// 타격 프레임까지 ≥ <see cref="HeavyMeleeWindup"/>.
/// </summary>
public sealed record TelegraphRequiredDef
{
    public float MinDamage { get; init; } = 10f;
    public float MinTime { get; init; } = 0.4f;
    public float MeleeReachOverContact { get; init; } = 50f;
    public float HeavyMeleeDamage { get; init; } = 25f;
    public float HeavyMeleeWindup { get; init; } = 0.35f;
}

public sealed record HeroRulesDef
{
    public required DefRef<SpriteSetDef> Sprites { get; init; }
    public float Radius { get; init; } = 14f;
    public float InvulnAfterHit { get; init; } = 0.25f;
    public IReadOnlyDictionary<StatKey, float> Stats { get; init; } = new Dictionary<StatKey, float>();
    public IReadOnlyList<DefRef<WeaponDef>> StartWeapons { get; init; } = [];
    public CrowdSlowDef? CrowdSlow { get; init; }
}

public sealed record CrowdSlowDef
{
    public int Touching { get; init; } = 3;
    public float Mul { get; init; } = 0.6f;
}

/// <summary>경험치 곡선 한 구간: 필요 경험치 = base + per × (L − origin), L ∈ [from, to](to null = 끝 없음).</summary>
public sealed record XpCurveSegment
{
    public int From { get; init; }
    public int? To { get; init; }
    public float Base { get; init; }
    public float Per { get; init; }
    public int? Origin { get; init; }
}

public sealed record LevelUpRulesDef
{
    public int Cards { get; init; } = 3;
    public LevelUpWeightsDef Weights { get; init; } = new();
    public int WeaponSlots { get; init; } = 4;
    public int PassiveSlots { get; init; } = 4;
    public int Rerolls { get; init; }
    public float SkipHeal { get; init; }
    public float InputLock { get; init; }
    public IReadOnlyList<int> GuaranteeNewWeaponAt { get; init; } = [];
    public IReadOnlyList<FillerCardDef> Fillers { get; init; } = [];
    /// <summary>레벨 표 키·<see cref="StatKey"/> 이름 → 카드 문구 라벨.</summary>
    public IReadOnlyDictionary<string, StatLabelDef> StatLabels { get; init; } = new Dictionary<string, StatLabelDef>();
}

public sealed record LevelUpWeightsDef
{
    public float OwnedWeapon { get; init; }
    public float OwnedPassive { get; init; }
    public float NewWeapon { get; init; }
    public float NewPassive { get; init; }
}

public sealed record FillerCardDef
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public float Heal { get; init; }
    public int Gold { get; init; }
    public IconDef? Icon { get; init; }
}

public enum StatFormat : byte { Abs, Pct }

public sealed record StatLabelDef
{
    public string Label { get; init; } = "";
    public string? Unit { get; init; }
    public StatFormat Format { get; init; } = StatFormat.Abs;
}

/// <summary>선택 창이 닫힌 다음 스테이지 틱에 주인공 반경 radius 안 적을 distance 만큼 밀어낸다(피해 0, 기획 12.6).</summary>
public sealed record LevelUpPushDef
{
    public float Radius { get; init; }
    public float Distance { get; init; }
}

public sealed record RanksDef
{
    public required RankDef Elite { get; init; }
    public required RankDef Unique { get; init; }
    public required PackDef Pack { get; init; }
    /// <summary>생성 때 최종 크기(정의 scale × 등급 배율)의 상한(기획 8.1). 넘으면 자르고 몸 반경도 자른 크기로 센다(6.5).</summary>
    public float MaxScale { get; init; } = 1.6f;
}

/// <summary>정예·우두머리 배율(설계서 6.7·6.10).</summary>
public sealed record RankDef
{
    public float Hp { get; init; } = 1f;
    public float Damage { get; init; } = 1f;
    public float Speed { get; init; } = 1f;
    public float Xp { get; init; } = 1f;
    public float Scale { get; init; } = 1f;
    /// <summary>넉백 저항에 더하는 값(정예).</summary>
    public float? KnockbackResistAdd { get; init; }
    /// <summary>넉백 저항을 이 값으로(우두머리).</summary>
    public float? KnockbackResist { get; init; }
    public int Affixes { get; init; }
    public DefRef<DropTableDef>? DropTable { get; init; }
    public Rgb Glow { get; init; } = Rgb.White;
    public RankPulseDef? Pulse { get; init; }
}

public sealed record RankPulseDef
{
    public float Period { get; init; }
    /// <summary>[최소, 최대] 알파.</summary>
    public float[] Alpha { get; init; } = [];
}

public sealed record PackDef
{
    /// <summary>[최소, 최대]</summary>
    public int[] PackSize { get; init; } = [3, 5];
    public float UniqueChance { get; init; }
    public string Banner { get; init; } = "";
    public float BannerTime { get; init; }
}

public sealed record CapsDef
{
    public int Enemies { get; init; } = 300;
    public int EnemiesAbsolute { get; init; } = 360;
    public int HeroProjectiles { get; init; } = 400;
    public int EnemyProjectiles { get; init; } = 200;
    public int Gems { get; init; } = 300;
}

public sealed record SpawnRulesDef
{
    /// <summary>시야 밖 생성 띠 [최소, 최대] px.</summary>
    public float[] Band { get; init; } = [40f, 200f];
    public int Tries { get; init; } = 12;
    public float EdgeMargin { get; init; }
    /// <summary>[최소, 최대] 무리 크기.</summary>
    public int[] Group { get; init; } = [1, 3];
    public float GroupRadius { get; init; }
    public float AccumulatorHold { get; init; }
}
