namespace DiabloDefenceRemastered.Core.Sim.Heroes;

/// <summary>
/// 장비 끼우기 한 곳(v0.9 V4, 설계서 6.19 — WP3): 칸을 바꾸고(옛 것은 사라짐 — 되팔기·보관함 없음) 능력치를 다시 합친 뒤 지금 체력을 <c>EquipStats.RebaseHp</c> 로
/// (최대 체력이 오르면 오른 만큼 더하고, 내려가면 새 최대에서 자름 — 기획 11.6). 칸끼리 막지 않는다(지팡이 + 방패 함께 — v0.9.1).
/// 쓰는 곳: 상인 <c>Buy</c>·<c>ReplaceRing</c>. 짐 <c>Restore</c> 는 짐의 체력을 그대로 두므로 이것을 거치지 않는다.
/// </summary>
public static class HeroEquipment
{
    /// <summary>기본 아이템 칸 종류 → 장비 칸(반지는 반지 1 — 빈 칸 고르기는 부르는 쪽).</summary>
    public static EquipSlot SlotOf(EquipKind kind) => kind switch
    {
        EquipKind.Head => EquipSlot.Head,
        EquipKind.Body => EquipSlot.Body,
        EquipKind.Shield => EquipSlot.Shield,
        EquipKind.Staff => EquipSlot.Staff,
        _ => EquipSlot.Ring1,
    };

    /// <summary><paramref name="slot"/> 에 <paramref name="item"/> 을 끼운다(차 있으면 바꿈).</summary>
    public static void Equip(World world, EquipSlot slot, EquipItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var hero = world.Hero;
        var oldMax = hero.MaxHp;
        hero.Equipment[(int)slot] = item;
        HeroStats.Recompute(world);
        hero.Hp = EquipStats.RebaseHp(hero.Hp, oldMax, hero.MaxHp);
        hero.MoveSpeed = HeroStats.MoveSpeed(hero);
    }
}
