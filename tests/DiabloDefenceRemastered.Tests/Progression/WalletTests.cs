using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Progression;

/// <summary>
/// 10.1 <c>Progression</c> 행(v0.9 V3 WP3 단계 2 — 설계서 6.19 "금화", 기획 11.7): 얻으면 지갑 <c>Hero.Gold</c> 와 얻은 금화 <c>RunStats.Gold</c> 둘 다,
/// 쓰면 지갑만 줄고 <c>RunStats.GoldSpent</c> 가 는다.
/// </summary>
public class WalletTests
{
    [Fact]
    public void GoldPickup_GoesToWalletAndEarned()
    {
        var t = CombatKit.World(ShopKit.Defs());
        var w = t.World;
        CombatKit.Drop(w, "t_gold", w.Hero.Pos, 7);
        t.Step();
        Assert.Equal((7, 7, 0), (w.Hero.Gold, w.RunStats.Gold, w.RunStats.GoldSpent));
    }

    [Fact]
    public void ChestGold_GoesToWalletAndEarned()
    {
        var t = CombatKit.World(ShopKit.Defs());
        var w = t.World;
        var chest = w.Defs.Rewards.Get(TestDefs.RewardChestId);
        CombatKit.Drop(w, chest.Object!.Id, w.Hero.Pos, reward: chest);
        t.Step();
        Assert.InRange(w.Hero.Gold, 30, 60);
        Assert.Equal(w.Hero.Gold, w.RunStats.Gold);
    }

    [Fact]
    public void Spend_OnlyWalletShrinks_GoldSpentGrows_NotEnoughRefused()
    {
        var t = CombatKit.World(ShopKit.Defs());
        var w = t.World;
        Wallet.Earn(w, 100);
        Assert.True(Wallet.TrySpend(w, 70));
        Assert.Equal((30, 100, 70), (w.Hero.Gold, w.RunStats.Gold, w.RunStats.GoldSpent));
        Assert.False(Wallet.TrySpend(w, 31));
        Assert.Equal((30, 100, 70), (w.Hero.Gold, w.RunStats.Gold, w.RunStats.GoldSpent));
        Wallet.Earn(w, 0);
        Wallet.Earn(w, -5);
        Assert.Equal(30, w.Hero.Gold);

        // 상인에게 사기(판 명령 길)도 같은 규칙
        var s = ShopKit.Room(merchantId: TestDefs.SmithId);
        Wallet.Earn(s.World, 500);
        var shop = ShopKit.OpenReady(s);
        var price = shop.Cards[0].Price;
        s.AppTick(ShopKit.Press(CommandButtons.Choice1));
        Assert.Equal((500 - price, 500, price), (s.Hero.Gold, s.World.RunStats.Gold, s.World.RunStats.GoldSpent));
    }

    [Fact]
    public void TraitGold_OnAcquire_ToWallet()
    {
        var basic = ShopKit.Defs();
        var hero = basic.HeroClasses.All[0];
        var rich = new PassiveDef
        {
            Id = "t_rich_trait", Name = "t_rich_trait", Trait = true, MaxLevel = 1, PerLevel = new Dictionary<StatKey, float>(),
            OnAcquire = new OnAcquireDef { Gold = 25 },
        };
        var cls = hero with { Traits = [DefRef<PassiveDef>.To(rich)] };
        var t = CombatKit.World(ShopKit.Defs(), configure: b => b.WithHeroClass(cls));
        Assert.Equal((25, 25), (t.Hero.Gold, t.World.RunStats.Gold));
    }
}
