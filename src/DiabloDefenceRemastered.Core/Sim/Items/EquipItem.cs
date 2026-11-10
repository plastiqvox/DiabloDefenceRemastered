namespace DiabloDefenceRemastered.Core.Sim.Items;

/// <summary>굴린 접사 하나(v0.9.1 V7, 6.19): 접사 정의 ID(<c>item_affixes</c>) + 굴린 값(능력치 키 단위 — 퍼센트 키는 비율).</summary>
public readonly record struct RolledAffix(string Id, float Value);

/// <summary>
/// 장비 인스턴스(v0.9, 설계서 6.19 — 계약 WP1g): 기본 아이템 ID + 굴린 접사 + 굴림을 반영한 값. 짐(<c>HeroCarry.Equipment</c>)·계단 저장·상인 카드가 같은 형을 쓴다.
/// 정의 쪽 값(기본 능력치·접사 능력치 키)은 ID 로 찾는다 — 저장에는 굴린 값만 든다.
/// </summary>
/// <param name="BaseId">기본 아이템 ID(<c>items</c>).</param>
/// <param name="Prefix">접두사(없으면 null).</param>
/// <param name="Suffix">접미사(없으면 null).</param>
/// <param name="Price">값 = 기본 값 + Σ 접사 값 × (0.75 + 0.5 × 굴림 위치), <c>price_round</c> 단위 반올림 — 사는 순간 그대로.</param>
public sealed record EquipItem(string BaseId, RolledAffix? Prefix, RolledAffix? Suffix, int Price)
{
    /// <summary>마법 등급(접사가 하나 이상 — 파란 이름).</summary>
    public bool Magic => Prefix is not null || Suffix is not null;
}

/// <summary>짐의 장비 한 칸(v0.9 — <c>HeroCarry.Equipment</c>, 칸 순서 = <see cref="EquipSlot"/> 순, 빈 칸은 담지 않는다).</summary>
public readonly record struct CarriedEquip(EquipSlot Slot, EquipItem Item);
