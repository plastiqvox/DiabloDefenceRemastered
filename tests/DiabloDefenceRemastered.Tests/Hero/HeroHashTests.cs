using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Heroes;

/// <summary>
/// WP3 단계 2 이음새 확인: <c>StateHasher.Room</c> 의 주인공 행(장비 칸마다 기본 ID·접사 ID·굴린 값, 지갑, 물약 수·주머니·빠른 물약, 마시던 회복 남은 틱·양)과
/// 보상 창 <c>LevelUpOffer.Reward</c>(마법서 창이 그 <c>RewardDef</c> 를 실음).
/// </summary>
public class HeroHashTests
{
    [Fact]
    public void RoomHash_ChangesWith_Equipment_Wallet_PotionSlots_Heal()
    {
        static World Fresh() => CombatKit.World(ShopKit.Defs()).World;
        var baseHash = StateHasher.Room(Fresh());
        Assert.Equal(baseHash, StateHasher.Room(Fresh()));

        var changes = new Action<World>[]
        {
            w => w.Hero.Gold = 1,
            w => w.Hero.PotionSlotsLevel = 1,
            w => w.Hero.PotionSpeedLevel = 1,
            w => w.Hero.Heal = new HealOverTime(10, 4f),
            w => w.Hero.Equipment[(int)EquipSlot.Ring2] = ShopKit.Item("t_ring", suffix: new RolledAffix("t_af_fox", 10f)),
        };
        var seen = new HashSet<ulong> { baseHash };
        foreach (var change in changes)
        {
            var w = Fresh();
            change(w);
            Assert.True(seen.Add(StateHasher.Room(w)));
        }

        // 같은 장비에 굴린 값만 다름 / 접사 ID 만 다름
        var a = Fresh();
        var b = Fresh();
        var c = Fresh();
        a.Hero.Equipment[(int)EquipSlot.Shield] = ShopKit.Item("t_buckler", suffix: new RolledAffix("t_af_swift", 0.03f));
        b.Hero.Equipment[(int)EquipSlot.Shield] = ShopKit.Item("t_buckler", suffix: new RolledAffix("t_af_swift", 0.04f));
        c.Hero.Equipment[(int)EquipSlot.Shield] = ShopKit.Item("t_buckler", suffix: new RolledAffix("t_af_fox", 0.03f));
        Assert.Equal(3, new[] { StateHasher.Room(a), StateHasher.Room(b), StateHasher.Room(c) }.Distinct().Count());
    }

    [Fact]
    public void TomeOffer_CarriesRewardDef_LevelUpOfferDoesNot()
    {
        var t = CombatKit.World(ShopKit.Defs());
        var w = t.World;
        var tome = w.Defs.Rewards.Get(TestDefs.RewardTomeId);
        CombatKit.Drop(w, tome.Object!.Id, w.Hero.Pos, reward: tome);
        t.Step();
        Assert.Same(tome, w.Progression!.RewardOffer().Reward);
        w.Progression.SkipReward();
        ((ProgressionService)w.Progression).AddXp(5);
        Assert.Null(w.Progression.Offer().Reward);
    }
}
