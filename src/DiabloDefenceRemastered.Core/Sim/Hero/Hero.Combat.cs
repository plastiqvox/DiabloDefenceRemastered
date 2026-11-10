namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>주인공 — WP3 조각(설계서 11.2): 재생 누적·허리띠 다음 허용 틱·최대 체력 비율 기억.</summary>
public sealed partial class Hero
{
    /// <summary>허리띠를 다시 마실 수 있는 스테이지 틱(방 수명 — 새 방에서는 바로 마실 수 있다).</summary>
    public int BeltReadyTick;
    /// <summary>마지막으로 계산한 (1 + max_hp_pct). 비율이 바뀌면 지금 체력도 같은 비율로(기획 4.1·11.4).</summary>
    public float MaxHpFactor = 1f;

    partial void ResetCombat()
    {
        BeltReadyTick = 0;
        MaxHpFactor = 1f;
    }
}
