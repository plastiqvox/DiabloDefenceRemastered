namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 10.1 <c>Run</c> 행(v0.9 WP1g #7·#10·#11 — 설계서 6.19): <c>ShopStock</c>(같은 씨앗 같은 물건·다시 굴리기가 산 카드를 지킴·반지 둘 다른 접사·카드마다 흐름·페핀 카드),
/// 상인 방이 방 난수를 쓰지 않음(<c>RunSession</c>), 상인 방의 문 열기, 회복 문(들어온 첫 틱), 계단 저장의 회복 채우기.
/// </summary>
public class ShopAndHealTests
{
    private static readonly GameDefs Defs = TestDefs.Basic();
    private static MerchantDef M(string id) => Defs.Merchants.Get(id);
    private static readonly ShopPotions NoPotions = new(0, 0, 0);

    // ── ShopStock ─────────────────────────────────────────────────

    [Fact]
    public void Create_SameSeedActRoom_SameGoods_RingsDifferentAffixes()
    {
        var differs = 0;
        for (ulong seed = 1; seed <= 300; seed++)
        {
            var a = ShopStock.Create(M(TestDefs.WitchId), 1, 4, seed, Defs, NoPotions);
            var b = ShopStock.Create(M(TestDefs.WitchId), 1, 4, seed, Defs, NoPotions);
            Assert.Equal(a.Cards, b.Cards);
            Assert.Equal(3, a.Cards.Count);
            Assert.All(a.Cards, c => Assert.Equal(c.Item!.Price, c.Price));
            var r1 = a.Cards[1].Item!;
            var r2 = a.Cards[2].Item!;
            Assert.True(r1.Magic && r2.Magic);                                            // 반지는 늘 마법
            var ids1 = new[] { r1.Prefix?.Id, r1.Suffix?.Id }.OfType<string>().ToHashSet();
            Assert.DoesNotContain(r2.Prefix?.Id ?? "-", ids1);
            Assert.DoesNotContain(r2.Suffix?.Id ?? "-", ids1);                            // 서로 다른 접사
            if (!a.Cards.SequenceEqual(ShopStock.Create(M(TestDefs.WitchId), 1, 5, seed, Defs, NoPotions).Cards)) differs++;
        }
        Assert.True(differs > 250);                                                       // 다른 방이면 대개 다른 물건
        var smith = ShopStock.Create(M(TestDefs.SmithId), 1, 3, 9, Defs, NoPotions);
        Assert.Equal([EquipKind.Head, EquipKind.Body, EquipKind.Shield], smith.Cards.Select(c => Defs.ItemBases.Get(c.Item!.BaseId).Kind));
        Assert.All(smith.Cards, c => Assert.Contains(1, Defs.ItemBases.Get(c.Item!.BaseId).Acts));   // 액트 1 엔 판금 없음
    }

    [Fact]
    public void Reroll_KeepsBoughtCard_SameRerollCountSameResult()
    {
        var changed = 0;
        for (ulong seed = 1; seed <= 200; seed++)
        {
            var shop = ShopStock.Create(M(TestDefs.SmithId), 1, 4, seed, Defs, NoPotions);
            var bought = shop.Cards[1] with { Bought = true };
            shop.Cards[1] = bought;
            var before = shop.Cards.ToArray();
            ShopStock.Reroll(shop, M(TestDefs.SmithId), 1, 4, seed, Defs);
            Assert.Equal(1, shop.Rerolls);
            Assert.Equal(bought, shop.Cards[1]);                                          // 산 카드는 자리를 지킴
            if (!shop.Cards[0].Equals(before[0]) || !shop.Cards[2].Equals(before[2])) changed++;

            var again = ShopStock.Create(M(TestDefs.SmithId), 1, 4, seed, Defs, NoPotions);
            again.Cards[1] = bought;
            ShopStock.Reroll(again, M(TestDefs.SmithId), 1, 4, seed, Defs);
            Assert.Equal(shop.Cards, again.Cards);                                        // 누른 수가 같으면 같은 결과
        }
        Assert.True(changed > 150);
        Assert.Equal([30, 60, 90], new[] { 0, 1, 2 }.Select(n => ShopStock.RerollPrice(Defs.Rules.Equipment!, n)));
    }

    [Fact]
    public void AddingAffix_DoesNotShiftOtherCards()
    {
        // 정의 표에 투구 전용 접두사를 더해도 갑옷·방패·아드리아 카드는 그대로(카드마다 흐름 + 난수 수 고정)
        var extra = TestDefs.Affix("t_af_head_only", true, [EquipKind.Head], StatKey.MaxHp, 5f, 9f, 1f);
        var more = new GameDefs
        {
            Rules = Defs.Rules, ItemBases = Defs.ItemBases, Merchants = Defs.Merchants,
            ItemAffixes = new DefTable<ItemAffixDef>([.. Defs.ItemAffixes.All, extra]),
        };
        var headChanged = 0;
        for (ulong seed = 1; seed <= 300; seed++)
        {
            var a = ShopStock.Create(M(TestDefs.SmithId), 1, 4, seed, Defs, NoPotions);
            var b = ShopStock.Create(more.Merchants.Get(TestDefs.SmithId), 1, 4, seed, more, NoPotions);
            Assert.Equal(a.Cards[1], b.Cards[1]);
            Assert.Equal(a.Cards[2], b.Cards[2]);
            if (!a.Cards[0].Equals(b.Cards[0])) headChanged++;
            Assert.Equal(ShopStock.Create(M(TestDefs.WitchId), 1, 4, seed, Defs, NoPotions).Cards,
                         ShopStock.Create(more.Merchants.Get(TestDefs.WitchId), 1, 4, seed, more, NoPotions).Cards);
        }
        Assert.True(headChanged > 0);
    }

    [Fact]
    public void Healer_FixedCards_StepPrices_Maxed_RefillPerEmptySlot()
    {
        var healer = M(TestDefs.HealerId);
        var rules = Defs.Rules.Hero!.Potions!;
        var shop = ShopStock.Create(healer, 1, 4, 3, Defs, new ShopPotions(1, 0, 0));
        Assert.Equal([PotionUpgrade.Slots, PotionUpgrade.Speed, PotionUpgrade.Refill], shop.Cards.Select(c => c.Upgrade));
        Assert.Equal([80, 100, 25], shop.Cards.Select(c => c.Price));                     // 채우기 = 빈 칸 1 × 25
        Assert.All(shop.Cards, c => Assert.Equal(ShopCardKind.Potion, c.Kind));
        Assert.All(shop.Cards, c => Assert.False(c.Bought));

        ShopCard Card(PotionUpgrade k, ShopPotions p) => ShopStock.PotionCard(healer.Upgrades.Single(u => u.Kind == k), rules, p);
        Assert.Equal(150, Card(PotionUpgrade.Slots, new(0, 1, 0)).Price);
        Assert.Equal(220, Card(PotionUpgrade.Slots, new(0, 2, 0)).Price);
        Assert.True(Card(PotionUpgrade.Slots, new(0, 3, 0)).Bought);                        // 칸 5 = slots_max → 다 올림
        Assert.Equal(200, Card(PotionUpgrade.Speed, new(0, 0, 1)).Price);
        Assert.True(Card(PotionUpgrade.Speed, new(0, 0, 2)).Bought);                        // 1.2초 = 끝 단계
        Assert.Equal(4 * 25, Card(PotionUpgrade.Refill, new(1, 3, 0)).Price);              // 칸 5 − 1
        Assert.True(Card(PotionUpgrade.Refill, new(2, 0, 0)).Bought);                       // 가득 → 살 수 없음
        ShopStock.Reroll(shop, healer, 1, 4, 3, Defs);                                     // 페핀은 다시 굴리기 없음
        Assert.Equal(0, shop.Rerolls);
    }

    // ── RunSession 상인 방·회복 문 ─────────────────────────────────

    private static ModuleContext Modules(FakeHeroCarry? carry = null)
    {
        var m = new ModuleContext();
        m.AddWorldInstaller((w, _) =>
        {
            w.Presentation.Enabled = false;
            w.Nav = new StraightNav(w.Arena!.Iso);
            w.Spatial = new BruteForceSpatial(w);
            w.Spawner = new FakeSpawner(w);
        });
        if (carry is not null) m.SetHeroCarry(carry);
        return m;
    }

    /// <summary>RoomActs 의 액트 1 가중표를 <paramref name="rewardId"/> 하나로 바꾼 정의(앞 방 RewardOrder[0] = 그 보상 — <c>--room</c> 시작의 보상).</summary>
    private static GameDefs WithOnlyReward(string rewardId)
    {
        var basic = RoomActs.Defs();
        var act1 = basic.Stages.Get(RoomActs.Act1Id);
        var only = act1 with
        {
            Rewards = act1.Rewards with { Weights = [new WeightEntry<RewardDef>(DefRef<RewardDef>.To(basic.Rewards.Get(rewardId)), 1f)] },
        };
        return new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Weapons = basic.Weapons, Passives = basic.Passives,
            HeroClasses = basic.HeroClasses, Tilesets = basic.Tilesets, Maps = basic.Maps, Enemies = basic.Enemies, Rewards = basic.Rewards,
            Pickups = basic.Pickups, ItemBases = basic.ItemBases, ItemAffixes = basic.ItemAffixes, Merchants = basic.Merchants,
            Stages = new DefTable<StageDef>([only, basic.Stages.Get(RoomActs.Act2Id)]),
        };
    }

    private static RunSession Session(GameDefs defs, int startIndex, FakeHeroCarry? carry = null) =>
        new(defs, Modules(carry), new RunStart(21, defs.Stages.Get(RoomActs.Act1Id), null, RunOptions.Default, startIndex));

    private static string RngState(World w) =>
        string.Join("|", Enumerable.Range(0, SimRandom.StreamCount).Select(i => w.Rng[(RngStream)i].GetState().ToString()));

    [Fact]
    public void MerchantRoom_ShopAtEntry_RoomRandomUntouched_SameAsNonMerchantRoom()
    {
        var shopRun = Session(WithOnlyReward(TestDefs.RewardSmithId), startIndex: 1);
        var tomeRun = Session(WithOnlyReward(TestDefs.RewardTomeId), startIndex: 1);
        var shop = shopRun.World.Room!.Shop;
        Assert.NotNull(shop);
        Assert.Equal(TestDefs.SmithId, shop.MerchantId);
        Assert.Equal(3, shop.Cards.Count);
        Assert.Null(tomeRun.World.Room!.Shop);
        Assert.Equal(shopRun.World.Room.Plan.RoomSeed, tomeRun.World.Room.Plan.RoomSeed);
        Assert.Equal(RngState(tomeRun.World), RngState(shopRun.World));                     // 상인 물건이 방 난수를 쓰지 않음
        for (var i = 0; i < 300; i++)
        {
            StageStepper.AppTick(shopRun.World, PlayerCommand.None);
            StageStepper.AppTick(tomeRun.World, PlayerCommand.None);
        }
        Assert.Equal(RngState(tomeRun.World), RngState(shopRun.World));
        Assert.Equal(1, shopRun.Run.MerchantRoomsThisAct);
        Assert.True(shopRun.Run.LastRoomWasMerchant);
        Assert.Equal([TestDefs.SmithId], shopRun.Run.MerchantsMet);
        Assert.Equal(0, tomeRun.Run.MerchantRoomsThisAct);
    }

    [Fact]
    public void MerchantRoom_DoorsWaitForLeave_OpenWindowBlocksExit_NoPickupSpawned()
    {
        var s = Session(WithOnlyReward(TestDefs.RewardSmithId), startIndex: 1);
        var w = s.World;
        var room = w.Room!;
        for (var i = 0; i < 60 * 200 && room.Phase != RoomPhase.Reward; i++) StageStepper.AppTick(w, PlayerCommand.None);
        Assert.Equal(RoomPhase.Reward, room.Phase);
        Assert.DoesNotContain(w.Pickups.Items.ToArray(), p => !p.Removed && p.Reward is not null);   // 상인은 물체가 아님(보상 자리에 섬)
        StageStepper.AppTick(w, PlayerCommand.None);
        StageStepper.AppTick(w, PlayerCommand.None);
        Assert.Equal(RoomPhase.Reward, room.Phase);                                         // 창을 닫기 전에는 문이 안 열림
        room.Shop!.Open = true;                                                             // 닿음(WP3 자리)
        room.Shop.Open = false;
        room.Shop.Visited = true;                                                           // 떠나기
        StageStepper.AppTick(w, PlayerCommand.None);
        Assert.Equal(RoomPhase.Doors, room.Phase);
        room.Shop.Open = true;                                                              // 문 단계에 다시 열면 나가지 못함
        StageStepper.AppTick(w, new PlayerCommand(Vector2.Zero, CommandButtons.Exit));
        Assert.Equal(RoomPhase.Doors, room.Phase);
        room.Shop.Open = false;
        StageStepper.AppTick(w, new PlayerCommand(Vector2.Zero, CommandButtons.Exit));
        Assert.Equal(RoomPhase.Exit, room.Phase);
        Assert.Contains(room.Exits, e => e.Reward is not null && e.Reward.Kind != RewardKind.Merchant);   // 바로 앞 방이 상인 방 → 상인 문 없음
    }

    [Fact]
    public void HealDoor_FirstTickAfterEntering_HealsPart_EliteBonusToo()
    {
        foreach (var start in new[] { 1, 3 })   // 일반 방, 정예 방(덤 보상)
        {
            var s = Session(WithOnlyReward(TestDefs.RewardPotionId), startIndex: start);
            var w = s.World;
            Assert.True(w.Room!.HealOnEnter);
            w.Hero.MaxHp = 100f;
            w.Hero.Hp = 30f;
            Assert.Equal(30f, w.Hero.Hp);                                                   // 넘김 어둠 동안(첫 틱 전) 그대로
            StageStepper.AppTick(w, PlayerCommand.None);
            Assert.Equal(80f, w.Hero.Hp);                                                   // 들어옴 첫 틱에 최대 × heal_amount 0.5 만큼(전부가 아님)
            Assert.False(w.Room.HealOnEnter);
            w.Hero.Hp = 40f;
            StageStepper.AppTick(w, PlayerCommand.None);
            Assert.Equal(40f, w.Hero.Hp);                                                   // 한 번뿐
        }
    }

    [Fact]
    public void HealDoor_NeverAboveMax()
    {
        var s = Session(WithOnlyReward(TestDefs.RewardPotionId), startIndex: 1);
        var w = s.World;
        w.Hero.MaxHp = 100f;
        w.Hero.Hp = 70f;
        StageStepper.AppTick(w, PlayerCommand.None);
        Assert.Equal(100f, w.Hero.Hp);                                                      // 70 + 50 → 최대에서 멈춤
    }

    [Fact]
    public void HealDoor_ExitThroughIt_NextRoomHealsOnEnter_RecordHasHpIn()
    {
        var defs = WithOnlyReward(TestDefs.RewardPotionId);
        var s = Session(defs, startIndex: 0, carry: new FakeHeroCarry());
        var w = s.World;
        for (var i = 0; i < 60 * 200 && w.Room!.Phase != RoomPhase.Doors; i++)
        {
            if (w.Room.Phase == RoomPhase.Reward)
                for (var k = 0; k < w.Pickups.Count; k++)
                    if (w.Pickups[k].GatesExit) w.Pickups.Remove(w.Pickups[k].Id);
            StageStepper.AppTick(w, PlayerCommand.None);
        }
        Assert.Equal(TestDefs.RewardPotionId, w.Room!.Exits[0].Reward!.Id);
        StageStepper.AppTick(w, new PlayerCommand(Vector2.Zero, CommandButtons.Exit, 0));
        w.Hero.MaxHp = 100f;
        w.Hero.Hp = 25f;
        s.Advance();
        Assert.False(s.Run.HealOnEnter);                                                    // 다음 방 RoomState 로 넘어감
        Assert.True(s.World.Room!.HealOnEnter);
        Assert.Equal(25f, s.World.Hero.Hp);
        var max = s.World.Hero.MaxHp;
        StageStepper.AppTick(s.World, PlayerCommand.None);
        Assert.Equal(Math.Min(max, 25f + max * 0.5f), s.World.Hero.Hp, 3);
        Assert.Equal(TestDefs.RewardTomeId, s.Run.Rooms[0].RewardId);
        Assert.Equal(0f, s.Run.Rooms[0].HpIn);                                              // 첫 방: 시험 모듈은 주인공을 만들지 않아 들어올 때 체력 0
        Assert.Equal(0, s.Run.Rooms[0].GoldSpent);
        Assert.Null(s.Run.Rooms[0].Shop);
    }
}
