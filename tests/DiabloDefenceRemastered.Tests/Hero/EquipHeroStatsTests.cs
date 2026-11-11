using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Heroes;

/// <summary>
/// 10.1 <c>Hero</c> 행(v0.9 V4·v0.9.1 V6 WP3 단계 2 — 설계서 6.19 "능력치 합산", 기획 11.6): <c>HeroStats.Recompute</c> 의 장비 출처(<c>EquipStats.Add</c>) →
/// 모든 출처를 합친 뒤 <c>EquipStats.Clamp</c>(방어 ≤ 15, <c>move_pct</c> ≥ −0.15), 끼운 순간의 최대 체력 바뀜(<c>EquipStats.RebaseHp</c>), 지팡이 + 방패 함께.
/// 합성 장비 = <see cref="TestDefs.ItemBases"/>(판금 투구 방어 4·판금 갑옷 5·판금 방패 4).
/// </summary>
public class EquipHeroStatsTests
{
    private static World World()
    {
        var w = CombatKit.World(ShopKit.Defs()).World;
        CombatKit.ClearWeapons(w);
        return w;
    }

    private static readonly RolledAffix Fine2 = new("t_af_fine", 2f);

    [Fact]
    public void PlateSet_PlusFine_ArmorCappedAt15_WeightMoveFloor()
    {
        var w = World();
        HeroEquipment.Equip(w, EquipSlot.Head, ShopKit.Item("t_full_helm", prefix: Fine2));      // 4 + 2
        HeroEquipment.Equip(w, EquipSlot.Body, ShopKit.Item("t_breast_plate", prefix: Fine2));   // 5 + 2
        Assert.Equal(13f, w.Hero.Stats[StatKey.Armor]);
        Assert.Equal(-0.14f, w.Hero.Stats[StatKey.MovePct], 4);                                  // 판금 갑옷 −10 + 판금 투구 −4
        HeroEquipment.Equip(w, EquipSlot.Shield, ShopKit.Item("t_kite_shield"));                // + 4 = 17 → 15
        Assert.Equal(15f, w.Hero.Stats[StatKey.Armor]);
        Assert.Equal(-0.15f, w.Hero.Stats[StatKey.MovePct], 4);                                  // −18 → 하한 −15
        Assert.Equal(-0.4f, w.Hero.Stats[StatKey.KnockbackTakenPct], 4);
        Assert.Equal(95f * 0.85f, w.Hero.MoveSpeed, 3);

        // 다른 출처(성소 대가·단련 같은 짐 덧값)의 방어도 같은 상한 안 — 모든 출처 합을 자른다
        w.Combat.Mods[(int)StatKey.Armor] = 3f;
        HeroStats.Recompute(w);
        Assert.Equal(15f, w.Hero.Stats[StatKey.Armor]);
    }

    [Fact]
    public void Swiftness_WithPlate_ClampAfterSum_NotFloorThenAdd()
    {
        var w = World();
        HeroEquipment.Equip(w, EquipSlot.Head, ShopKit.Item("t_full_helm"));
        HeroEquipment.Equip(w, EquipSlot.Body, ShopKit.Item("t_breast_plate"));
        HeroEquipment.Equip(w, EquipSlot.Shield, ShopKit.Item("t_kite_shield", suffix: new RolledAffix("t_af_swift", 0.05f)));
        Assert.Equal(-0.13f, w.Hero.Stats[StatKey.MovePct], 4);                                  // −18 + 5 = −13(−15 + 5 = −10 이 아님)

        // 이동 패시브(+8%)도 같은 합에 든다
        w.Hero.Passives[0] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Runner), Level = 1 };
        HeroStats.Recompute(w);
        Assert.Equal(-0.05f, w.Hero.Stats[StatKey.MovePct], 4);
    }

    [Fact]
    public void MaxHpUp30_CurrentUp30_SwapDown20_ClampedToNewMax()
    {
        var w = World();
        var hero = w.Hero;
        hero.Hp = 70f;
        HeroEquipment.Equip(w, EquipSlot.Head, ShopKit.Item("t_helm", suffix: new RolledAffix("t_af_fox", 10f)));   // +20 +10
        Assert.Equal((130f, 100f), (hero.MaxHp, hero.Hp));                                       // 오른 만큼 지금 체력도
        Assert.Equal(2f, hero.Stats[StatKey.Armor]);

        hero.Hp = 130f;
        HeroEquipment.Equip(w, EquipSlot.Head, ShopKit.Item("t_cap"));                          // +10 — 최대 −20
        Assert.Equal((110f, 110f), (hero.MaxHp, hero.Hp));                                       // 새 최대에서 자름
        Assert.Equal(0f, hero.Stats[StatKey.Armor]);

        hero.Hp = 50f;
        HeroEquipment.Equip(w, EquipSlot.Head, ShopKit.Item("t_cap"));                          // 같은 최대 — 그대로
        Assert.Equal((110f, 50f), (hero.MaxHp, hero.Hp));

        // 짐을 넘겨도 장비 최대 체력이 그대로(다시 오르지 않음)
        hero.Hp = 80f;
        var carry = new Core.Sim.Progression.HeroCarrier().Capture(w);
        var w2 = World();
        new Core.Sim.Progression.HeroCarrier().Restore(w2, carry);
        Assert.Equal((110f, 80f), (w2.Hero.MaxHp, w2.Hero.Hp));
    }

    [Fact]
    public void StaffAndShield_BothCounted_LookIsStaff()
    {
        var w = World();
        var trait = w.Hero.Stats[StatKey.CooldownPct];                                          // 특성 0.05
        HeroEquipment.Equip(w, EquipSlot.Shield, ShopKit.Item("t_buckler"));
        HeroEquipment.Equip(w, EquipSlot.Staff, ShopKit.Item("t_staff", prefix: new RolledAffix("t_af_jagged", 0.04f)));
        Assert.Equal(1f, w.Hero.Stats[StatKey.Armor]);
        Assert.Equal(trait + 0.03f, w.Hero.Stats[StatKey.CooldownPct], 4);
        Assert.Equal(0.04f, w.Hero.Stats[StatKey.DamagePct], 4);
        Assert.NotNull(w.Hero.Equipment[(int)EquipSlot.Shield]);                                 // 지팡이가 방패를 비우지 않음
        Assert.Equal("lt", HeroLook.Key(w.Hero.Equipment, w.Defs));
    }
}
