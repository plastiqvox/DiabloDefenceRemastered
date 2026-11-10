namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>적 — WP3 조각(설계서 5.4·11.2): 주인공 무기 칸별 다음 허용 시각(회전 구체·장판의 적별 타격 간격).</summary>
public sealed partial class Enemy
{
    /// <summary>무기 칸별 다음 타격 허용 스테이지 시각(초). 칸 번호로 바로 찾는다(사전 없음). 길이 = <see cref="WeaponSlots.SlotCount"/>(v0.8 — 설계 표의 4 는 그릇 6 에 맞춤).</summary>
    public readonly float[] NextHitBySlot = new float[WeaponSlots.SlotCount];

    partial void ResetCombat() => Array.Clear(NextHitBySlot);
}
