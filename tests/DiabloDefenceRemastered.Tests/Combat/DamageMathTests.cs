using DiabloDefenceRemastered.Core.Sim.Combat;

namespace DiabloDefenceRemastered.Tests.Combat;

/// <summary>10.1 <c>Combat</c> — <c>DamageMath</c>(기획 4.2 예).</summary>
public class DamageMathTests
{
    [Fact]
    public void HeroToEnemy_RoundsThenCritThenArmor_Min1()
    {
        Assert.Equal(11, DamageMath.HeroToEnemy(10f, 0.1f, false, 0));        // round(11)
        Assert.Equal(22, DamageMath.HeroToEnemy(10f, 0.1f, true, 0));         // × 2
        Assert.Equal(19, DamageMath.HeroToEnemy(10f, 0.1f, true, 3));         // − 방어
        Assert.Equal(1, DamageMath.HeroToEnemy(10f, 0f, false, 50));          // 최소 1
        Assert.Equal(13, DamageMath.HeroToEnemy(10f, 0.25f, false, 0));       // 12.5 → 13(0.5 는 0 에서 먼 쪽)
    }

    [Fact]
    public void EnemyFinalDamage_AuraPlusWarCry_Is155_RoundedOnce()
    {
        // 오라 +25% + 함성 +30% = ×1.55(더함), 반올림은 마지막 한 번
        Assert.Equal(16, DamageMath.EnemyFinalDamage(10f, 1f, 0.25f + 0.30f, 1f));     // 15.5 → 16
        Assert.Equal(13, DamageMath.EnemyFinalDamage(10f, 1f, 0.25f, 1f));             // 12.5 → 13
    }

    [Fact]
    public void EnemyFinalDamage_RankAndEnrage()
    {
        Assert.Equal(22, DamageMath.EnemyFinalDamage(10f, 1.4f, 0.55f, 1f));            // 21.7 → 22
        Assert.Equal(70, DamageMath.EnemyFinalDamage(35f, 1f, 0f, 2f));                 // 보스 광폭화 ×2
    }

    [Fact]
    public void EnemyFinalHp_TimeRankAffix()
    {
        Assert.Equal(126, DamageMath.EnemyFinalHp(20f, 1.8f, 3.5f, 0f));                // 20 × 1.8 × 3.5 = 126
        Assert.Equal(1, DamageMath.EnemyFinalHp(0.1f, 1f, 1f, 0f));
    }

    [Fact]
    public void ToHero_ArmorMin1()
    {
        Assert.Equal(8, DamageMath.ToHero(10, 2));
        Assert.Equal(1, DamageMath.ToHero(3, 7));
    }

    [Fact]
    public void FinalCooldown_Cap06_Surge08()
    {
        Assert.Equal(1.2f * 0.95f, DamageMath.FinalCooldown(1.2f, 0.05f, 0f), 5);
        Assert.Equal(1f * 0.4f, DamageMath.FinalCooldown(1f, 0.9f, 0f), 5);              // 상한 0.6
        Assert.Equal(1f * 0.2f, DamageMath.FinalCooldown(1f, 0.5f, 0.5f), 5);            // 마력 폭주 중 상한 0.8
        Assert.Equal(1f * 0.45f, DamageMath.FinalCooldown(1f, 0.05f, 0.5f), 5);
    }

    [Fact]
    public void EnemyCooldown_Floor03()
    {
        Assert.Equal(2f * 0.8f, DamageMath.EnemyCooldown(2f, -0.2f), 5);
        Assert.Equal(2f * 0.3f, DamageMath.EnemyCooldown(2f, -0.9f), 5);
    }

    [Fact]
    public void TimeMultipliers_Linear()
    {
        var s = new StageScaling { Hp = [1.8f, 0.10f], Damage = [1.3f, 0.04f] };
        Assert.Equal(1.8f + 0.10f * 5f, DamageMath.TimeHpMul(s, 5f), 5);
        Assert.Equal(1.3f + 0.04f * 5f, DamageMath.TimeDmgMul(s, 5f), 5);
    }
}
