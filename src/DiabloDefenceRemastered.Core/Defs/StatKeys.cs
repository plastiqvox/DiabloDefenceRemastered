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

public static class StatKeys
{
    public const int Count = 15;
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
