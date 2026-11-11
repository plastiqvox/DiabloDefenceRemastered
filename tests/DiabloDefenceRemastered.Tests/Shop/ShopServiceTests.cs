using DiabloDefenceRemastered.Core.Sim.Shops;

namespace DiabloDefenceRemastered.Tests.Shops;

/// <summary>
/// 10.1 <c>Shop</c> 행(v0.9 WP3 단계 2 — 설계서 6.19 "상인 방 상태와 구매 명령", 기획 11.6·11.7·12.8): 실제 전투 모듈의 <see cref="ShopService"/>·<see cref="ShopInput"/> 을
/// 판 명령(<c>StageStepper.AppTick</c>)으로만 몬다 — 앱 창이 IShop 을 직접 부르지 않는 약속과 같은 길.
/// </summary>
public class ShopServiceTests
{
    private static ShopState ShopOf(TestWorld t) => t.World.Room!.Shop!;

    [Fact]
    public void Touch_Opens_OneShopOpenCue_SubjectMerchant()
    {
        var t = ShopKit.Room(merchantId: TestDefs.SmithId);
        var w = t.World;
        t.AppTick();
        Assert.False(ShopOf(t).Open);                                                   // 상인 자리 100px 밖
        t.PlaceHero(w.Room!.RewardSpot + new Vector2(w.Hero.Radius + ShopService.OpenReach + 1f, 0f));
        t.AppTick();
        Assert.False(ShopOf(t).Open);                                                   // 반경 + 20 바로 밖
        t.PlaceHero(w.Room.RewardSpot + new Vector2(w.Hero.Radius + ShopService.OpenReach - 1f, 0f));
        Assert.Equal(StageStep.Stepped, t.AppTick());
        Assert.True(ShopOf(t).Open);
        for (var i = 0; i < 40; i++) Assert.Equal(StageStep.AwaitingChoice, t.AppTick());
        var open = ShopKit.Cues(w, CueKind.ShopOpen);
        Assert.Single(open);
        Assert.Equal(TestDefs.SmithId, open[0].Subject!.Id);
        Assert.Equal(ChoiceKind.None, w.AwaitingChoice);                               // 레벨업 창이 아니다(앱은 Room.Shop.Open 으로 그림)
    }

    [Fact]
    public void AfterLeave_StayingDoesNotReopen_GoingOutAndBackReopens()
    {
        var t = ShopKit.Room(merchantId: TestDefs.SmithId);
        ShopKit.OpenReady(t);
        t.AppTick(ShopKit.Press(CommandButtons.Skip));                                   // 떠나기
        Assert.False(ShopOf(t).Open);
        Assert.True(ShopOf(t).Visited);
        for (var i = 0; i < 30; i++) t.AppTick();                                       // 그 자리에 머묾
        Assert.False(ShopOf(t).Open);
        t.PlaceHero(t.World.Room!.RewardSpot + new Vector2(80f, 0f));
        t.AppTick();
        Assert.False(ShopOf(t).Open);
        ShopKit.Touch(t);                                                                // 다시 들어옴(문 단계)
        Assert.Equal(RoomPhase.Doors, t.World.Room.Phase);
        Assert.True(ShopOf(t).Open);
        Assert.Equal(2, ShopKit.Cues(t.World, CueKind.ShopOpen).Count);
    }

    [Fact]
    public void InputLock_IgnoresChoices_ForInputLockAppTicks()
    {
        var t = ShopKit.Room(merchantId: TestDefs.SmithId);
        var w = t.World;
        w.Hero.Gold = 10_000;
        ShopKit.Touch(t);
        var lockTicks = (int)MathF.Round(w.Defs.Rules.LevelUp!.InputLock * 60f);
        Assert.Equal(24, lockTicks);
        for (var i = 0; i < lockTicks; i++)
        {
            Assert.Equal(StageStep.AwaitingChoice, t.AppTick(ShopKit.Press(CommandButtons.Choice1)));
            Assert.False(ShopOf(t).Cards[0].Bought);
        }
        t.AppTick(ShopKit.Press(CommandButtons.Skip));                                   // 잠금 뒤 첫 앱 틱 — 떠나기도 받는다
        Assert.False(ShopOf(t).Open);

        var u = ShopKit.Room(merchantId: TestDefs.SmithId);
        u.World.Hero.Gold = 10_000;
        ShopKit.Touch(u);
        for (var i = 0; i < lockTicks; i++) u.AppTick(ShopKit.Press(CommandButtons.Choice1));
        u.AppTick(ShopKit.Press(CommandButtons.Choice1));
        Assert.True(ShopOf(u).Cards[0].Bought);
    }

    [Fact]
    public void Buy_Success_EquipsSpendsWallet_NotEnough_ShopDenied()
    {
        var t = ShopKit.Room(merchantId: TestDefs.SmithId);
        var w = t.World;
        var shop = ShopKit.OpenReady(t);
        var card = shop.Cards[1];                                                        // 갑옷
        w.Hero.Gold = card.Price - 1;
        var earned = w.RunStats.Gold;
        t.AppTick(ShopKit.Press(CommandButtons.Choice2));
        Assert.False(shop.Cards[1].Bought);
        Assert.Null(w.Hero.Equipment[(int)EquipSlot.Body]);
        Assert.Equal(card.Price - 1, w.Hero.Gold);
        var denied = Assert.Single(ShopKit.Cues(w, CueKind.ShopDenied));
        Assert.Equal(TestDefs.SmithId, denied.Subject!.Id);

        w.Hero.Gold = card.Price + 5;
        t.AppTick(ShopKit.Press(CommandButtons.Choice2));
        Assert.True(shop.Cards[1].Bought);
        Assert.Equal(card.Item, w.Hero.Equipment[(int)EquipSlot.Body]);
        Assert.Equal(5, w.Hero.Gold);
        Assert.Equal(card.Price, w.RunStats.GoldSpent);
        Assert.Equal(earned, w.RunStats.Gold);                                          // 얻은 금화는 그대로
        var buy = Assert.Single(ShopKit.Cues(w, CueKind.ShopBuy));
        Assert.Equal(card.Item!.BaseId, buy.Subject!.Id);                               // 대상 = 산 장비의 ItemBaseDef
        Assert.IsType<ItemBaseDef>(buy.Subject);

        w.Hero.Gold = 10_000;
        t.AppTick(ShopKit.Press(CommandButtons.Choice2));                                // 산 카드는 다시 못 삼
        Assert.Equal(2, ShopKit.Cues(w, CueKind.ShopDenied).Count);
        Assert.Equal(10_000, w.Hero.Gold);
        Assert.True(shop.Open);                                                          // 산 뒤에도 창은 열린 채
    }

    [Fact]
    public void Rings_BothFull_RingPending_ThenReplaceRing_SkipCancels()
    {
        var t = ShopKit.Room(merchantId: TestDefs.WitchId);
        var w = t.World;
        var old1 = ShopKit.Item("t_ring", suffix: new RolledAffix("t_af_fox", 10f));
        var old2 = ShopKit.Item("t_ring", prefix: new RolledAffix("t_af_jagged", 0.03f));
        w.Hero.Equipment[(int)EquipSlot.Ring1] = old1;
        w.Hero.Equipment[(int)EquipSlot.Ring2] = old2;
        w.Hero.Gold = 10_000;
        var shop = ShopKit.OpenReady(t);
        var ring = shop.Cards[1];
        Assert.Equal(EquipKind.Ring, w.Defs.ItemBases.Get(ring.Item!.BaseId).Kind);

        t.AppTick(ShopKit.Press(CommandButtons.Choice2));
        Assert.Equal(1, shop.RingPending);
        Assert.Equal(10_000, w.Hero.Gold);                                              // 값은 바꿀 반지를 고를 때
        t.AppTick(ShopKit.Press(CommandButtons.Skip));                                   // 건너뛰기 = 사지 않음(창은 열린 채)
        Assert.Equal(-1, shop.RingPending);
        Assert.True(shop.Open);
        Assert.False(shop.Cards[1].Bought);

        t.AppTick(ShopKit.Press(CommandButtons.Choice2));
        t.AppTick(ShopKit.Press(CommandButtons.Choice3));                                // 바꿀 반지는 1·2 만 — 무시
        Assert.Equal(1, shop.RingPending);
        t.AppTick(ShopKit.Press(CommandButtons.Choice2));                                // 반지 2 를 바꿈
        Assert.Equal(-1, shop.RingPending);
        Assert.True(shop.Cards[1].Bought);
        Assert.Equal(old1, w.Hero.Equipment[(int)EquipSlot.Ring1]);
        Assert.Equal(ring.Item, w.Hero.Equipment[(int)EquipSlot.Ring2]);
        Assert.Equal(10_000 - ring.Price, w.Hero.Gold);

        // 빈 반지 칸이 있으면 바로 그 칸에
        var u = ShopKit.Room(merchantId: TestDefs.WitchId);
        u.World.Hero.Equipment[(int)EquipSlot.Ring1] = old1;
        u.World.Hero.Gold = 10_000;
        var s2 = ShopKit.OpenReady(u);
        u.AppTick(ShopKit.Press(CommandButtons.Choice3));
        Assert.Equal(-1, s2.RingPending);
        Assert.Equal(s2.Cards[2].Item, u.World.Hero.Equipment[(int)EquipSlot.Ring2]);
    }

    [Fact]
    public void Reroll_Prices30Then60_BoughtCardKeepsPlace_HealerNoReroll()
    {
        var t = ShopKit.Room(merchantId: TestDefs.SmithId);
        var w = t.World;
        w.Hero.Gold = 10_000;
        var shop = ShopKit.OpenReady(t);
        t.AppTick(ShopKit.Press(CommandButtons.Choice1));
        var bought = shop.Cards[0];
        Assert.True(bought.Bought);
        var gold = w.Hero.Gold;
        var spent = w.RunStats.GoldSpent;
        t.AppTick(ShopKit.Press(CommandButtons.Reroll));
        Assert.Equal((1, gold - 30, spent + 30), (shop.Rerolls, w.Hero.Gold, w.RunStats.GoldSpent));
        Assert.Equal(bought, shop.Cards[0]);
        t.AppTick(ShopKit.Press(CommandButtons.Reroll));
        Assert.Equal((2, gold - 90), (shop.Rerolls, w.Hero.Gold));
        Assert.Equal(bought, shop.Cards[0]);

        // 같은 씨앗·같은 누른 수면 같은 결과(상인 전용 흐름)
        var again = ShopStock.Create(w.Defs.Merchants.Get(TestDefs.SmithId), w.Room!.Plan.Act, w.Room.Plan.Number, w.Run!.Seed, w.Defs, new ShopPotions(0, 0, 0));
        again.Cards[0] = bought;
        ShopStock.Reroll(again, w.Defs.Merchants.Get(TestDefs.SmithId), w.Room.Plan.Act, w.Room.Plan.Number, w.Run.Seed, w.Defs);
        ShopStock.Reroll(again, w.Defs.Merchants.Get(TestDefs.SmithId), w.Room.Plan.Act, w.Room.Plan.Number, w.Run.Seed, w.Defs);
        Assert.Equal(again.Cards, shop.Cards);

        w.Hero.Gold = 89;                                                               // 다음 값 90
        t.AppTick(ShopKit.Press(CommandButtons.Reroll));
        Assert.Equal((2, 89), (shop.Rerolls, w.Hero.Gold));
        Assert.Single(ShopKit.Cues(w, CueKind.ShopDenied));

        var h = ShopKit.Room(merchantId: TestDefs.HealerId);
        h.World.Hero.Gold = 1000;
        var hs = ShopKit.OpenReady(h);
        var before = hs.Cards.ToArray();
        h.AppTick(ShopKit.Press(CommandButtons.Reroll));                                 // 페핀: 아무 일 없음
        Assert.Equal((0, 1000), (hs.Rerolls, h.World.Hero.Gold));
        Assert.Equal(before, hs.Cards);
        Assert.Empty(ShopKit.Cues(h.World, CueKind.ShopDenied));
    }

    [Fact]
    public void Healer_SlotsSpeedRefill_StepPrices_Maxed_RefillIsEmptyTimes25()
    {
        var t = ShopKit.Room(merchantId: TestDefs.HealerId);
        var w = t.World;
        w.Hero.Gold = 2000;
        w.Hero.Potions = 1;
        var shop = ShopKit.OpenReady(t);
        Assert.Equal([80, 100, 50], shop.Cards.Select(c => c.Price));                   // 물건은 들어온 틱의 물약 칸(0개 — 빈 칸 2)으로 정함

        t.AppTick(ShopKit.Press(CommandButtons.Choice1));                                // 물약 주머니 80
        Assert.Equal(1, w.Hero.PotionSlotsLevel);
        Assert.Equal([150, 100, 50], shop.Cards.Select(c => c.Price));                  // 다음 단계 값·채우기 = 빈 칸 2 × 25
        var buy = Assert.Single(ShopKit.Cues(w, CueKind.ShopBuy));
        Assert.Equal(TestDefs.HealerId, buy.Subject!.Id);                               // 페핀 카드 = MerchantDef
        t.AppTick(ShopKit.Press(CommandButtons.Choice1));
        t.AppTick(ShopKit.Press(CommandButtons.Choice1));                                // 150 + 220 → 칸 5
        Assert.Equal(3, w.Hero.PotionSlotsLevel);
        Assert.True(shop.Cards[0].Bought);                                              // 다 올림
        var gold = w.Hero.Gold;
        t.AppTick(ShopKit.Press(CommandButtons.Choice1));
        Assert.Equal(gold, w.Hero.Gold);
        Assert.Single(ShopKit.Cues(w, CueKind.ShopDenied));

        t.AppTick(ShopKit.Press(CommandButtons.Choice2));                                // 빠른 물약 100 → 200
        Assert.Equal((1, 200), (w.Hero.PotionSpeedLevel, shop.Cards[1].Price));
        t.AppTick(ShopKit.Press(CommandButtons.Choice2));
        Assert.Equal(2, w.Hero.PotionSpeedLevel);
        Assert.True(shop.Cards[1].Bought);

        Assert.Equal(4 * 25, shop.Cards[2].Price);                                      // 칸 5 − 물약 1
        gold = w.Hero.Gold;
        t.AppTick(ShopKit.Press(CommandButtons.Choice3));
        Assert.Equal((5, gold - 100), (w.Hero.Potions, w.Hero.Gold));
        Assert.True(shop.Cards[2].Bought);                                              // 가득 → 살 수 없음
        Assert.Equal(80 + 150 + 220 + 100 + 200 + 100, w.RunStats.GoldSpent);
    }

    [Fact]
    public void Leave_SetsVisited_DoorsOpen()
    {
        var t = ShopKit.Room(merchantId: TestDefs.SmithId);
        var w = t.World;
        for (var i = 0; i < 5; i++) t.AppTick();
        Assert.Equal(RoomPhase.Reward, w.Room!.Phase);                                  // 창을 한 번 닫기 전에는 문이 안 열림
        ShopKit.OpenReady(t);
        t.AppTick(new PlayerCommand(Vector2.Zero, CommandButtons.Skip | CommandButtons.Exit));
        Assert.Equal((false, true), (ShopOf(t).Open, ShopOf(t).Visited));
        Assert.Equal(RoomPhase.Reward, w.Room.Phase);                                   // 떠난 앱 틱은 판 틱이 아님
        t.AppTick();
        Assert.Equal(RoomPhase.Doors, w.Room.Phase);
    }

    [Fact]
    public void WhileOpen_WorldTickFrozen_DrinkDenied_PotionsKept()
    {
        var t = ShopKit.Room(merchantId: TestDefs.SmithId);
        var w = t.World;
        w.Hero.Potions = 2;
        w.Hero.Hp = w.Hero.MaxHp * 0.5f;
        ShopKit.Touch(t);
        var tick = w.Tick;
        var hp = w.Hero.Hp;
        t.AppTick(ShopKit.Press(CommandButtons.Drink));                                  // 잠금 중
        ShopKit.PassLock(t);
        t.AppTick(ShopKit.Press(CommandButtons.Drink));
        for (var i = 0; i < 60; i++) Assert.Equal(StageStep.AwaitingChoice, t.AppTick(new PlayerCommand(new Vector2(1f, 0f))));
        Assert.Equal(tick, w.Tick);
        Assert.Equal(2, ShopKit.Cues(w, CueKind.PotionDenied).Count);
        Assert.Empty(ShopKit.Cues(w, CueKind.PotionDrink));
        Assert.Equal((2, hp, false), (w.Hero.Potions, w.Hero.Hp, w.Hero.Heal.Active));
        Assert.Equal(0, w.RunStats.PotionsDrunk);
    }

    [Fact]
    public void NotOpened_DuringPendingChoice_NorInSurvive()
    {
        var t = ShopKit.Room(RoomPhase.Survive, merchantId: TestDefs.SmithId);
        t.PlaceHero(t.World.Room!.RewardSpot);
        t.AppTick();
        Assert.False(ShopOf(t).Open);                                                   // 보상·문 단계에서만

        var u = ShopKit.Room(merchantId: TestDefs.SmithId);
        var w = u.World;
        ((Core.Sim.Progression.ProgressionService)w.Progression!).AddXp(5);             // 레벨업 대기
        u.PlaceHero(w.Room!.RewardSpot);
        Assert.Equal(StageStep.Stepped, u.AppTick());
        Assert.False(ShopOf(u).Open);
        Assert.Equal(ChoiceKind.LevelUp, w.AwaitingChoice);
        w.Progression.Skip();
        u.AppTick();
        Assert.True(ShopOf(u).Open);                                                    // 창이 닫힌 다음 틱(자리에 그대로 있어도 — 닫은 것은 상인 창이 아님)
    }
}
