namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>주인공 — WP3 조각(설계서 11.2): 최대 체력 비율 기억. v0.9.1: 허리띠 다음 허용 틱은 저절로 마심과 함께 없앰(WP1g).</summary>
public sealed partial class Hero
{
    /// <summary>마지막으로 계산한 (1 + max_hp_pct). 비율이 바뀌면 지금 체력도 같은 비율로(기획 4.1·11.4).</summary>
    public float MaxHpFactor = 1f;

    partial void ResetCombat()
    {
        MaxHpFactor = 1f;
    }
}
