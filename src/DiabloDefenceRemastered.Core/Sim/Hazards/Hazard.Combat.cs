namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>장판 — WP3 조각(설계서 5.5·11.2): 틱 피해·간격·붙이는 버프·상한 그룹.</summary>
public sealed partial class Hazard
{
    /// <summary>틱 간격(초)·다음 틱까지 남은 초.</summary>
    public float TickInterval;
    public float NextTickIn;
    /// <summary>주인공 쪽: 원값(damage_pct·치명 확률은 만든 순간 복사). 적 쪽: 최종 피해.</summary>
    public float TickDamage;
    public float DamagePct;
    public float CritChance;
    /// <summary>주인공 무기 칸(−1 = 무기 아님). 칸이 있으면 적별 타격 간격(<c>Enemy.NextHitBySlot</c>)을 쓴다.</summary>
    public sbyte WeaponSlot = -1;
    public BuffDef? Buff;
    public float BuffDuration;
    /// <summary>동시 상한 묶음(독 웅덩이 20 등 — 넘으면 같은 묶음의 오래된 것부터 지움, 5.5).</summary>
    public string? CapGroup;

    partial void ResetCombat()
    {
        TickInterval = NextTickIn = 0f;
        TickDamage = DamagePct = CritChance = 0f;
        WeaponSlot = -1;
        Buff = null;
        BuffDuration = 0f;
        CapGroup = null;
    }
}
