namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 적 쪽 식 (설계서 5.2 의 적 줄을 그대로). 식의 정본은 WP3 <c>Combat/DamageMath</c> 인데 그 본문이 아직 없어(NotImplemented) WP4 가 같은 식을 여기 둔다.
/// 합칠 때 본문을 <c>DamageMath</c> 호출로 바꾼다(WP4 구현 기록 — 다른 묶음에 넘길 것). 부르는 곳은 이 클래스 하나라 바꿀 자리도 하나다.
/// </summary>
public static class EnemyMath
{
    /// <summary>최종 피해 = round(기본 × 굳힌 배율 × (1 + 버프% 합) × 광폭화), ≥ 0. 반올림은 마지막 한 번.</summary>
    public static int FinalDamage(float baseDamage, float damageMul, float buffPctSum, float enrage) =>
        Math.Max(0, (int)MathF.Round(baseDamage * damageMul * (1f + buffPctSum) * enrage));

    /// <summary>최종 HP = round(기본 × 시간 × 등급 × (1 + hp_pct 합)), ≥ 1.</summary>
    public static int FinalHp(float baseHp, float timeMul, float rankMul, float hpPctSum) =>
        Math.Max(1, (int)MathF.Round(baseHp * timeMul * rankMul * (1f + hpPctSum)));

    /// <summary>적 능력·패턴 쿨: × (1 + 합), 최소 × 0.3.</summary>
    public static float Cooldown(float cooldown, float attackCooldownPctSum) =>
        cooldown * MathF.Max(0.3f, 1f + attackCooldownPctSum);

    /// <summary>시간 배율 a + b × t(난이도 분).</summary>
    public static float TimeHpMul(StageScaling s, float minutes) => Lin(s.Hp, minutes);

    public static float TimeDmgMul(StageScaling s, float minutes) => Lin(s.Damage, minutes);

    private static float Lin(float[] ab, float t) => ab is { Length: >= 2 } ? ab[0] + ab[1] * t : ab is { Length: 1 } ? ab[0] : 1f;
}
