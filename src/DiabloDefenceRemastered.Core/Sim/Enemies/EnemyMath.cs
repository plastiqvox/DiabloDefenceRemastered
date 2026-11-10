using DiabloDefenceRemastered.Core.Sim.Combat;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 적 쪽 식 (설계서 5.2 의 적 줄). 식의 정본은 WP3 <c>Combat/DamageMath</c> 이고 여기서는 그대로 부른다 — 합칠 때 바꿈(WP4 구현 기록 "다른 묶음에 넘길 것").
/// 따로 둔 사본은 반올림이 달랐다(<c>MathF.Round</c> 기본 = 짝수 쪽, 정본 = 0 에서 먼 쪽 — x.5 에서 1 차이).
/// </summary>
public static class EnemyMath
{
    /// <summary>최종 피해 = round(기본 × 굳힌 배율 × (1 + 버프% 합) × 광폭화), ≥ 0. 반올림은 마지막 한 번.</summary>
    public static int FinalDamage(float baseDamage, float damageMul, float buffPctSum, float enrage) =>
        DamageMath.EnemyFinalDamage(baseDamage, damageMul, buffPctSum, enrage);

    /// <summary>최종 HP = round(기본 × 시간 × 등급 × (1 + hp_pct 합)), ≥ 1.</summary>
    public static int FinalHp(float baseHp, float timeMul, float rankMul, float hpPctSum) =>
        DamageMath.EnemyFinalHp(baseHp, timeMul, rankMul, hpPctSum);

    /// <summary>적 능력·패턴 쿨: × (1 + 합), 최소 × 0.3.</summary>
    public static float Cooldown(float cooldown, float attackCooldownPctSum) =>
        DamageMath.EnemyCooldown(cooldown, attackCooldownPctSum);

    /// <summary>시간 배율 a + b × t(난이도 분).</summary>
    public static float TimeHpMul(StageScaling s, float minutes) => DamageMath.TimeHpMul(s, minutes);

    public static float TimeDmgMul(StageScaling s, float minutes) => DamageMath.TimeDmgMul(s, minutes);
}
