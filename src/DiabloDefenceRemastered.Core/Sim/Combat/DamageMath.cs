namespace DiabloDefenceRemastered.Core.Sim.Combat;

/// <summary>
/// 피해 식 한 곳 (설계서 5.2, 기획 4.2, 소유 WP3). <c>damage_pct</c> 는 <see cref="HeroToEnemy"/> 에서만 곱한다.
/// 반올림은 모두 <see cref="Round"/>(0.5 는 0 에서 먼 쪽) 하나로 한다 — 기획의 "반올림" 을 은행가 반올림으로 읽지 않게.
/// </summary>
public static class DamageMath
{
    /// <summary>주인공 무기 쿨다운 감소 상한(기획 4.1).</summary>
    public const float CooldownCap = 0.6f;
    /// <summary>마력 폭주 중 상한(기획 11.1).</summary>
    public const float SurgeCooldownCap = 0.8f;
    /// <summary>적 능력 쿨다운 배율 하한(설계서 6.7).</summary>
    public const float EnemyCooldownFloor = 0.3f;

    /// <summary>round(d×(1+pct))×(crit?2:1) − armor, ≥1.</summary>
    public static int HeroToEnemy(float weaponDamage, float damagePct, bool crit, int enemyArmor)
    {
        var d = Round(weaponDamage * (1f + damagePct)) * (crit ? 2 : 1) - enemyArmor;
        return Math.Max(1, d);
    }

    /// <summary>반올림 마지막 한 번: round(기본 × 배율 × (1 + 버프 합) × 광폭화).</summary>
    public static int EnemyFinalDamage(float baseDamage, float damageMul, float buffPctSum, float enrage)
        => Math.Max(0, Round(baseDamage * damageMul * (1f + buffPctSum) * enrage));

    /// <summary>round(기본 HP × 시간 배율 × 등급 배율 × (1 + 접두사 HP%)), ≥1.</summary>
    public static int EnemyFinalHp(float baseHp, float timeMul, float rankMul, float hpPctSum)
        => Math.Max(1, Round(baseHp * timeMul * rankMul * (1f + hpPctSum)));

    /// <summary>받는 피해 = 적 피해 − 방어, ≥1.</summary>
    public static int ToHero(int enemyDamage, int heroArmor) => Math.Max(1, enemyDamage - heroArmor);

    /// <summary>주인공 무기: cooldown × (1 − min(상한, cooldown_pct + 폭주%)). 상한 0.6 / 마력 폭주 중 0.8.</summary>
    public static float FinalCooldown(float cooldown, float cooldownPct, float surgePct)
    {
        var cap = surgePct > 0f ? SurgeCooldownCap : CooldownCap;
        return cooldown * (1f - MathF.Min(cap, cooldownPct + surgePct));
    }

    /// <summary>적 능력·패턴: × (1 + 합), 최소 × 0.3.</summary>
    public static float EnemyCooldown(float cooldown, float attackCooldownPctSum)
        => cooldown * MathF.Max(EnemyCooldownFloor, 1f + attackCooldownPctSum);

    /// <summary>시간 체력 배율 a + b × t(난이도 분).</summary>
    public static float TimeHpMul(StageScaling s, float minutes) => Linear(s.Hp, minutes);

    /// <summary>시간 피해 배율 a + b × t(난이도 분).</summary>
    public static float TimeDmgMul(StageScaling s, float minutes) => Linear(s.Damage, minutes);

    /// <summary>반올림(0.5 는 0 에서 먼 쪽).</summary>
    public static int Round(float x) => (int)MathF.Round(x, MidpointRounding.AwayFromZero);

    private static float Linear(float[] ab, float minutes) => ab.Length switch
    {
        0 => 1f,
        1 => ab[0],
        _ => ab[0] + ab[1] * minutes,
    };
}
