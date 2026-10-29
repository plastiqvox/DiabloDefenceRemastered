namespace DiabloDefenceRemastered.Core.Sim.Combat;

/// <summary>
/// 피해 식 한 곳 (설계서 5.2, 기획 4.2, 소유 WP3). <c>damage_pct</c> 는 <see cref="HeroToEnemy"/> 에서만 곱한다.
/// WP1 이 시그니처만 만든 빈 연결 파일 — WP4(EnemyFactory·패턴 쿨다운)가 먼저 부르고 컴파일한다. 본문은 WP3 이 채운다.
/// </summary>
public static class DamageMath
{
    /// <summary>round(d×(1+pct))×(crit?2:1) − armor, ≥1.</summary>
    public static int HeroToEnemy(float weaponDamage, float damagePct, bool crit, int enemyArmor)
        => throw new NotImplementedException("WP3 DamageMath.HeroToEnemy");

    /// <summary>반올림 마지막 한 번.</summary>
    public static int EnemyFinalDamage(float baseDamage, float damageMul, float buffPctSum, float enrage)
        => throw new NotImplementedException("WP3 DamageMath.EnemyFinalDamage");

    public static int EnemyFinalHp(float baseHp, float timeMul, float rankMul, float hpPctSum)
        => throw new NotImplementedException("WP3 DamageMath.EnemyFinalHp");

    /// <summary>≥1.</summary>
    public static int ToHero(int enemyDamage, int heroArmor)
        => throw new NotImplementedException("WP3 DamageMath.ToHero");

    /// <summary>주인공 무기: 상한 0.6 / 마력 폭주 중 0.8.</summary>
    public static float FinalCooldown(float cooldown, float cooldownPct, float surgePct)
        => throw new NotImplementedException("WP3 DamageMath.FinalCooldown");

    /// <summary>적 능력·패턴: × (1 + 합), 최소 × 0.3.</summary>
    public static float EnemyCooldown(float cooldown, float attackCooldownPctSum)
        => throw new NotImplementedException("WP3 DamageMath.EnemyCooldown");

    public static float TimeHpMul(StageScaling s, float minutes)
        => throw new NotImplementedException("WP3 DamageMath.TimeHpMul");

    public static float TimeDmgMul(StageScaling s, float minutes)
        => throw new NotImplementedException("WP3 DamageMath.TimeDmgMul");
}
