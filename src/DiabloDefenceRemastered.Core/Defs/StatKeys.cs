using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Defs;

/// <summary>
/// 주인공 능력치 키 (기획 4.1 표 그대로, JSON 이름 = 키). 패시브 <c>per_level</c>·<c>rules.hero.stats</c> 가 쓰고,
/// 목록에 없는 키는 로딩 오류다(13.5-9). <c>_pct</c> 키는 같은 키끼리 더한 뒤 한 번 곱한다.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<StatKey>))]
public enum StatKey : byte
{
    [JsonStringEnumMemberName("max_hp")] MaxHp,
    [JsonStringEnumMemberName("move_speed")] MoveSpeed,
    [JsonStringEnumMemberName("armor")] Armor,
    [JsonStringEnumMemberName("regen")] Regen,
    [JsonStringEnumMemberName("damage_pct")] DamagePct,
    [JsonStringEnumMemberName("cooldown_pct")] CooldownPct,
    [JsonStringEnumMemberName("area_pct")] AreaPct,
    [JsonStringEnumMemberName("projectile_count")] ProjectileCount,
    [JsonStringEnumMemberName("duration_pct")] DurationPct,
    [JsonStringEnumMemberName("crit_chance")] CritChance,
    [JsonStringEnumMemberName("pickup_radius")] PickupRadius,
    [JsonStringEnumMemberName("move_pct")] MovePct,
    [JsonStringEnumMemberName("pickup_pct")] PickupPct,
    [JsonStringEnumMemberName("knockback_taken_pct")] KnockbackTakenPct,
    [JsonStringEnumMemberName("gold_pct")] GoldPct,
    /// <summary>최대 체력 비율(v0.8, 6.10 — 매혹의 성소 대가 −10%). 최대 체력 = (기본 + 더하기) × (1 + 합), 지금 체력도 같은 비율로(WP3).</summary>
    [JsonStringEnumMemberName("max_hp_pct")] MaxHpPct,
}

/// <summary>
/// 적 능력치 키 (설계서 6.7). 접두사 <c>stats</c> 와 버프 <c>stats</c> 가 같은 키를 쓰고, 목록에 없는 키는 로딩 오류다.
/// hp_pct·armor·knockback_resist 는 생성 때 한 번(접두사만), speed_pct·damage_pct 는 매 틱(버프 포함),
/// attack_cooldown_pct 는 모든 능력 부품·패턴의 cooldown 에 × (1 + 합)(최소 ×0.3).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EnemyStatKey>))]
public enum EnemyStatKey : byte
{
    [JsonStringEnumMemberName("hp_pct")] HpPct,
    [JsonStringEnumMemberName("armor")] Armor,
    [JsonStringEnumMemberName("speed_pct")] SpeedPct,
    [JsonStringEnumMemberName("damage_pct")] DamagePct,
    [JsonStringEnumMemberName("attack_cooldown_pct")] AttackCooldownPct,
    [JsonStringEnumMemberName("knockback_resist")] KnockbackResist,
}

/// <summary>
/// 주인공 능력치의 출처(v0.9, 6.19 — 4.1 "능력치 키 적용 규칙" 의 합산 차례). 절댓값 키는 출처마다 더하고 <c>_pct</c> 는 같은 키끼리 더해 한 번 곱한다.
/// <see cref="Equipment"/>(v0.9)는 끼운 장비의 기본 능력치 + 굴린 접사 + 방어구 무게 이동(<c>EquipStats.Sum</c>). 방어 상한·이동 하한은 모든 출처를 합친 <b>뒤</b> 자른다(<c>EquipStats.Clamp</c>).
/// </summary>
public enum StatSources : byte
{
    /// <summary><c>rules.hero.stats</c> 를 직업 <c>stats</c> 가 덮어쓴 값.</summary>
    Base,
    /// <summary>직업 특성(<c>trait</c> 패시브).</summary>
    Traits,
    /// <summary>패시브 칸·축복 칸.</summary>
    Passives,
    /// <summary>판 끝까지 가는 더하기(짐 <c>Mods</c> — <c>단련</c>·성소 대가).</summary>
    Mods,
    /// <summary>장비(v0.9 — 6.19).</summary>
    Equipment,
}

public static class StatKeys
{
    public const int Count = 16;
    public const int EnemyCount = 6;

    private static readonly Dictionary<string, StatKey> HeroByName = BuildNames<StatKey>();
    private static readonly Dictionary<string, EnemyStatKey> EnemyByName = BuildNames<EnemyStatKey>();

    /// <summary>JSON 이름("max_hp") → 키. 레벨 표 열 이름·카드 문구 라벨에서 쓴다.</summary>
    public static bool TryParse(string name, out StatKey key) => HeroByName.TryGetValue(name, out key);

    public static bool TryParse(string name, out EnemyStatKey key) => EnemyByName.TryGetValue(name, out key);

    public static string JsonName(StatKey key) => NameOf(key);
    public static string JsonName(EnemyStatKey key) => NameOf(key);

    private static string NameOf<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetField(value.ToString())!;
        var attr = (JsonStringEnumMemberNameAttribute?)Attribute.GetCustomAttribute(member, typeof(JsonStringEnumMemberNameAttribute));
        return attr?.Name ?? value.ToString();
    }

    private static Dictionary<string, TEnum> BuildNames<TEnum>() where TEnum : struct, Enum
    {
        var map = new Dictionary<string, TEnum>(StringComparer.Ordinal);
        foreach (var v in Enum.GetValues<TEnum>()) map[NameOf(v)] = v;
        return map;
    }
}
