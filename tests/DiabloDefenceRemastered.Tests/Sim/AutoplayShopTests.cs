using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 자동 조작 v0.9 (기획 15절·12.8, 설계 6.19 — WP4): 물약 마시기 문턱(<c>bot_threshold</c> 0.35·회복 중·물약 0), 상인 창 규칙(합성 <c>ShopState</c> —
/// 최대 체력 + 10 × 방어 차이 최대·살 수 없는 카드 건너뜀·반지 바꾸기는 값이 작은 반지·페핀 차례), 입력 잠금 동안 누르지 않음, 상인 자리로 걷기,
/// 문 우선순위의 상인(금화 ≥ 150 = 보물 상자 순위)·회복 문(체력 &lt; 50% ∧ 물약 0).
/// </summary>
public sealed class AutoplayShopTests
{
    private static TestWorld World(RoomPhase phase = RoomPhase.Survive) =>
        new TestWorldBuilder().WithDefs(TestDefs.Basic()).WithRoom(phase).WithModules(FakeArena.Modules(weapons: false)).Build();

    private static ShopCard Equip(string baseId, int price, bool bought = false, RolledAffix? suffix = null) =>
        new(ShopCardKind.Equip, new EquipItem(baseId, null, suffix, price), PotionUpgrade.Slots, price, bought);

    private static ShopCard Potion(PotionUpgrade u, int price, bool unbuyable = false) => new(ShopCardKind.Potion, null, u, price, unbuyable);

    private static ShopState Shop(string merchant, params ShopCard[] cards)
    {
        var s = new ShopState { MerchantId = merchant };
        s.Cards.AddRange(cards);
        return s;
    }

    [Fact]
    public void Drink_below_threshold_only_when_not_healing_and_has_potion()
    {
        var t = World();
        var hero = t.Hero;
        hero.MaxHp = 100f;
        hero.Potions = 1;
        hero.Hp = 34f;
        Assert.True(AutoplayPolicy.ShouldDrink(t.World));
        Assert.NotEqual(CommandButtons.None, new AutoplayPolicy(AutoplayVisit.None).Next(t.World).Buttons & CommandButtons.Drink);
        hero.Hp = 35f;                                                         // 문턱 = 35% 미만
        Assert.False(AutoplayPolicy.ShouldDrink(t.World));
        Assert.Equal(CommandButtons.None, new AutoplayPolicy(AutoplayVisit.None).Next(t.World).Buttons & CommandButtons.Drink);
        hero.Hp = 20f;
        hero.Potions = 0;
        Assert.False(AutoplayPolicy.ShouldDrink(t.World));                     // 물약 0
        hero.Potions = 2;
        hero.Heal = HealOverTime.Start(40f, 180);
        Assert.False(AutoplayPolicy.ShouldDrink(t.World));                     // 마시는 중
        hero.Heal = default;
        Assert.True(AutoplayPolicy.ShouldDrink(t.World));
    }

    [Fact]
    public void Equip_merchant_buys_largest_hp_plus_ten_armor_gain_skipping_bought_and_unaffordable()
    {
        var t = World(RoomPhase.Reward);
        var hero = t.Hero;
        // 값: 모자 10, 투구 20 + 10×2 = 40, 판금 갑옷 30 + 10×5 = 80(산 것), 갑옷(링 메일) 25 + 20 = 45(값 140)
        var shop = Shop(TestDefs.SmithId, Equip("t_cap", 40), Equip("t_helm", 130), Equip("t_breast_plate", 240, bought: true), Equip("t_ring_mail", 140));
        hero.Gold = 500;
        Assert.Equal(3, AutoplayPolicy.PickShopCard(t.World, shop));          // 링 메일 45 > 투구 40, 산 판금은 뺌
        hero.Gold = 135;
        Assert.Equal(1, AutoplayPolicy.PickShopCard(t.World, shop));          // 140 은 못 삼 → 투구
        hero.Gold = 100;
        Assert.Equal(0, AutoplayPolicy.PickShopCard(t.World, shop));
        hero.Gold = 39;
        Assert.Equal(-1, AutoplayPolicy.PickShopCard(t.World, shop));

        // 지금 칸 대비: 투구 칸에 이미 투구(40)가 있으면 모자는 −30, 링 메일이 가장 큼. 같은 값이면 왼쪽
        hero.Gold = 500;
        hero.Equipment[(int)EquipSlot.Head] = new EquipItem("t_helm", null, null, 130);
        Assert.Equal(-30f, AutoplayPolicy.EquipGain(t.World, shop.Cards[0].Item!));
        Assert.Equal(3, AutoplayPolicy.PickShopCard(t.World, shop));
        var tie = Shop(TestDefs.SmithId, Equip("t_quilted", 50), Equip("t_quilted", 50));
        Assert.Equal(0, AutoplayPolicy.PickShopCard(t.World, tie));
    }

    [Fact]
    public void Ring_swap_targets_weaker_ring()
    {
        var t = World(RoomPhase.Reward);
        var hero = t.Hero;
        var fox = new RolledAffix("t_af_fox", 10f);
        hero.Equipment[(int)EquipSlot.Ring1] = new EquipItem("t_ring", null, fox, 150);    // 최대 체력 10
        hero.Equipment[(int)EquipSlot.Ring2] = new EquipItem("t_ring", null, null, 50);    // 0
        Assert.Equal(1, AutoplayPolicy.WeakerRing(t.World));
        Assert.Equal(10f, AutoplayPolicy.EquipGain(t.World, new EquipItem("t_ring", null, fox, 150)));   // 둘째(0)를 바꿈
        hero.Equipment[(int)EquipSlot.Ring2] = new EquipItem("t_ring", null, fox, 150);
        Assert.Equal(0, AutoplayPolicy.WeakerRing(t.World));                                          // 같으면 첫째
    }

    [Fact]
    public void Pepin_refill_when_empty_then_pouch_then_quick()
    {
        var t = World(RoomPhase.Reward);
        var hero = t.Hero;
        hero.Gold = 500;
        var shop = Shop(TestDefs.HealerId, Potion(PotionUpgrade.Slots, 80), Potion(PotionUpgrade.Speed, 100), Potion(PotionUpgrade.Refill, 30));
        hero.Potions = 0;
        Assert.Equal(2, AutoplayPolicy.PickShopCard(t.World, shop));          // 비었으면 채우기
        hero.Potions = 1;
        Assert.Equal(0, AutoplayPolicy.PickShopCard(t.World, shop));          // 주머니
        shop.Cards[0] = Potion(PotionUpgrade.Slots, 80, unbuyable: true);
        Assert.Equal(1, AutoplayPolicy.PickShopCard(t.World, shop));          // 다 올림 → 빠른 물약
        hero.Gold = 50;
        Assert.Equal(-1, AutoplayPolicy.PickShopCard(t.World, shop));         // 채우기는 칸이 비었을 때만
        hero.Potions = 0;
        Assert.Equal(2, AutoplayPolicy.PickShopCard(t.World, shop));
    }

    [Fact]
    public void Walks_to_merchant_then_buys_one_after_input_lock_and_leaves()
    {
        var t = ShopKit.Room(RoomPhase.Reward, TestDefs.SmithId);
        var w = t.World;
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var spot = w.Room!.RewardSpot;
        var first = policy.Next(w);
        Assert.True(Vector2.Dot(first.Move, Vector2.Normalize(spot - w.Hero.Pos)) > 0.9f, $"상인 쪽으로 가지 않았다 ({first.Move})");

        w.Hero.Gold = 1000;
        ShopKit.Touch(t);
        var shop = w.Room.Shop!;
        Assert.True(shop.Open);
        var locked = 0;
        var pressedAt = -1;
        for (var app = 0; app < 200 && shop.Open; app++)
        {
            var cmd = policy.Next(w);
            if (w.Combat.ShopLockTicks > 0)
            {
                locked++;
                Assert.Equal(CommandButtons.None, cmd.Buttons);                // 잠금 동안 누르지 않음
            }
            else if (pressedAt < 0 && cmd.Buttons != CommandButtons.None) pressedAt = app;
            t.AppTick(cmd);
        }
        Assert.True(locked > 0);
        Assert.Equal(locked, pressedAt);                                       // 잠금이 풀린 첫 앱 틱에 누름
        Assert.False(shop.Open);
        Assert.True(shop.Visited);
        Assert.Equal(1, shop.Cards.Count(c => c.Bought));                      // 하나 사고 떠남
        Assert.True(w.Hero.Gold < 1000);
    }

    [Fact]
    public void Door_priority_merchant_with_gold_and_heal_door_needs_empty_potions()
    {
        int Pick(Func<GameDefs, RewardDef> a, Func<GameDefs, RewardDef> b, int gold, float hp, int potions)
        {
            var defs = TestDefs.Basic();
            var t = new TestWorldBuilder().WithDefs(defs).WithRoom(RoomPhase.Doors, exits: ["gate_nw", "gate_ne"]).WithModules(FakeArena.Modules(weapons: false)).Build();
            t.World.Room!.Exits = [new RoomExit("gate_nw", a(defs), RoomKind.Normal, null, false, null), new RoomExit("gate_ne", b(defs), RoomKind.Normal, null, false, null)];
            t.Hero.MaxHp = 100f;
            t.Hero.Hp = hp;
            t.Hero.Gold = gold;
            t.Hero.Potions = potions;
            var policy = new AutoplayPolicy(AutoplayVisit.None);
            t.Step(MathX.SecondsToTicks(defs.Rules.Rooms!.AutoplayDoorSkip, Core.Sim.World.Dt) + 2);
            return policy.Next(t.World).ExitIndex;
        }
        RewardDef R(GameDefs d, string id) => d.Rewards.Get(id);
        // 상인 문: 금화 ≥ 150 이면 보물 상자와 같은 순위(같으면 화면 왼쪽 문 — gate_nw), 모자라면 상자가 앞
        Assert.Equal(1, Pick(d => R(d, TestDefs.RewardSmithId), d => R(d, TestDefs.RewardChestId), gold: 100, hp: 90f, potions: 0));
        Assert.Equal(0, Pick(d => R(d, TestDefs.RewardSmithId), d => R(d, TestDefs.RewardChestId), gold: 150, hp: 90f, potions: 0));
        // 회복 문: 체력 < 50% 이고 물약 칸이 비었을 때만 맨 앞
        Assert.Equal(1, Pick(d => R(d, TestDefs.RewardChestId), d => R(d, TestDefs.RewardPotionId), gold: 0, hp: 40f, potions: 0));
        Assert.Equal(0, Pick(d => R(d, TestDefs.RewardChestId), d => R(d, TestDefs.RewardPotionId), gold: 0, hp: 40f, potions: 1));
    }
}
