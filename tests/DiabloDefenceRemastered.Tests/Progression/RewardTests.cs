using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Combat;

namespace DiabloDefenceRemastered.Tests.Progression;

/// <summary>
/// 10.1 <c>Progression</c> v0.7~v0.8 — 문 보상 넷의 받는 길, 0장 → <c>단련</c>, <c>RewardAvailable</c>, 재추첨 나눠 씀, 대기 순서, 허리띠 저절로,
/// 성소 한 판에 한 번, 시간 효과가 보상·문·들어옴 단계에서 안 줄어듦, 칸 5 에서 무기 다섯, <c>RoomCleared</c> 끌어당김, <c>SpawnPickup(Reward)</c> → <c>GatesExit</c>.
/// </summary>
public class RewardTests
{
    private static RewardDef R(World w, string id) => w.Defs.Rewards.Get(id);

    /// <summary>보상 물체를 주인공 발밑에 놓고 한 틱(실제 Pickups 단계가 줍는다).</summary>
    private static void Pick(TestWorld t, RewardDef reward)
    {
        CombatKit.Drop(t.World, reward.Object!.Id, t.Hero.Pos, reward: reward);
        t.Step();
    }

    private static TestWorld Fresh(GameDefs? defs = null, ulong seed = 1)
    {
        var t = CombatKit.World(defs, seed);
        CombatKit.ClearWeapons(t.World);
        return t;
    }

    [Fact]
    public void SpawnPickup_WithReward_GatesExit_AndPickedEvenAtFullHp()
    {
        var t = Fresh();
        var w = t.World;
        var r = R(w, TestDefs.RewardChestId);   // v0.9: 회복 문은 방 안 물체가 없다 — 보물 상자로
        var p = CombatKit.Drop(w, r.Object!.Id, t.Hero.Pos + new Vector2(200, 0), reward: r);
        Assert.True(p.GatesExit);
        Assert.Same(r, p.Reward);
        Assert.Equal(w.Hero.MaxHp, w.Hero.Hp);
        Pick(t, r);
        Assert.Equal(1, w.Hero.Potions);                            // 상자의 물약 1 → 물약 칸(v0.9.1)
        Assert.Equal(1, w.Pickups.LiveCount);                       // 멀리 둔 것은 그대로(끌려오지 않음)
        Assert.False(w.Pickups.Items.ToArray().Single(x => !x.Removed).Magnetized);
    }

    [Fact]
    public void Tome_PendingReward_WeaponCardsOnly_SourceTome_ChooseApplies()
    {
        var t = Fresh();
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 3);
        w.Hero.Passives[0] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Power), Level = 1 };
        Pick(t, R(w, TestDefs.RewardTomeId));
        Assert.Equal(1, w.Progression!.PendingRewards);
        Assert.Equal(ChoiceKind.Reward, w.AwaitingChoice);
        var offer = w.Progression.RewardOffer();
        Assert.Equal(OfferSource.Tome, offer.Source);
        Assert.All(offer.Cards, c => Assert.True(c.Kind is CardKind.NewWeapon or CardKind.WeaponUpgrade));
        var i = offer.Cards.ToList().FindIndex(c => c.Kind == CardKind.WeaponUpgrade);
        if (i >= 0)
        {
            w.Progression.ChooseReward(i);
            Assert.Equal(4, w.Hero.Weapons[0].Level);
        }
        else w.Progression.ChooseReward(0);
        Assert.Equal(0, w.Progression.PendingRewards);
        Assert.Equal(ChoiceKind.None, w.AwaitingChoice);
    }

    [Fact]
    public void Tome_ZeroCards_TemperInstead_NoWindow()
    {
        var t = Fresh();
        var w = t.World;
        w.Hero.WeaponSlotsOpen = 2;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        CombatKit.SetWeapon(w, 1, CombatKit.Fb, 8);
        Assert.False(w.Progression!.RewardAvailable(R(w, TestDefs.RewardTomeId)));
        var max = w.Hero.MaxHp;
        Pick(t, R(w, TestDefs.RewardTomeId));
        Assert.Equal(0, w.Progression.PendingRewards);
        Assert.Equal(max + 5, w.Hero.MaxHp);
        Assert.Equal(ChoiceKind.None, w.AwaitingChoice);
    }

    [Fact]
    public void Chest_RandomWeaponPlusOne_Potion_GoldTimesScale_ElseTemper()
    {
        var t = Fresh();
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 3);
        CombatKit.SetWeapon(w, 2, CombatKit.Orbit, 8);              // 만렙은 고르지 않음
        Pick(t, R(w, TestDefs.RewardChestId));
        Assert.Equal(4, w.Hero.Weapons[0].Level);
        Assert.Equal(8, w.Hero.Weapons[2].Level);
        Assert.Equal(1, w.Hero.Potions);
        Assert.InRange(w.RunStats.Gold, 30, 60);
        Assert.Equal(0, w.Progression!.PendingRewards);

        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        var max = w.Hero.MaxHp;
        Pick(t, R(w, TestDefs.RewardChestId));
        Assert.Equal(max + 5, w.Hero.MaxHp);                        // 올릴 무기 없음 → 단련
        Assert.Equal(2, w.Hero.Potions);
    }

    /// <summary>v0.9.1(6.19 "물약 칸"): 물약 칸이 가득이면 상자의 물약은 빠진다 — 마시지 않는다(v0.8 은 넘치면 그 자리에서 마심). 회복 문·상인은 거르기가 <c>RewardDraw</c> 라 늘 참.</summary>
    [Fact]
    public void Chest_PotionDroppedWhenFull_NoDrink_HealAndMerchantAlwaysAvailable()
    {
        var t = Fresh();
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 3);                 // 올릴 무기가 있게(없으면 단련이 체력 +5)
        w.Hero.Potions = 2;
        w.Hero.Hp = 60f;
        Pick(t, R(w, TestDefs.RewardChestId));
        Assert.Equal(2, w.Hero.Potions);
        Assert.Equal(60f, w.Hero.Hp);
        Assert.True(w.Progression!.RewardAvailable(R(w, TestDefs.RewardPotionId)));
        Assert.True(w.Progression.RewardAvailable(R(w, TestDefs.RewardSmithId)));
    }

    [Fact]
    public void GloomyShrine_ModsAndOncePerRun_SurvivesCarry()
    {
        var t = Fresh();
        var w = t.World;
        var shrine = R(w, TestDefs.RewardShrineId);
        Assert.True(w.Progression!.RewardAvailable(shrine));
        Pick(t, shrine);
        Assert.Equal(2f, w.Hero.Stats[StatKey.Armor]);
        Assert.Equal(-0.05f, w.Hero.Stats[StatKey.DamagePct], 5);
        Assert.False(w.Progression.RewardAvailable(shrine));

        var carry = new HeroCarrier().Capture(w);
        Assert.Equal([TestDefs.RewardShrineId], carry.Shrines);
        var t2 = Fresh();
        new HeroCarrier().Restore(t2.World, carry);
        Assert.False(t2.World.Progression!.RewardAvailable(shrine));
        Assert.Equal(2f, t2.Hero.Stats[StatKey.Armor]);
    }

    [Fact]
    public void EnchantedShrine_AllPlusOne_OneRandomMinusOne_NotLv1()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var t = Fresh(seed: seed);
            var w = t.World;
            CombatKit.SetWeapon(w, 0, CombatKit.Mm, 1);
            CombatKit.SetWeapon(w, 1, CombatKit.Fb, 4);
            CombatKit.SetWeapon(w, 2, CombatKit.Orbit, 8);
            var shrine = R(w, CombatKit.Enchanted);
            Assert.True(w.Progression!.RewardAvailable(shrine));
            Pick(t, shrine);
            var levels = new[] { w.Hero.Weapons[0].Level, w.Hero.Weapons[1].Level, w.Hero.Weapons[2].Level };
            Assert.Equal(2, levels[0]);                              // Lv1 은 −1 을 맞지 않음
            Assert.True(levels[1] == 3 && levels[2] == 8 || levels[1] == 5 && levels[2] == 7);
        }
    }

    [Fact]
    public void FascinatingShrine_Window_PlusTwo_MaxHpMinus10Pct_HpSameRatio()
    {
        var t = Fresh();
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 3);
        CombatKit.SetWeapon(w, 1, CombatKit.Fb, 8);                 // Lv8 은 후보 밖
        w.Hero.Hp = 50f;
        Pick(t, R(w, CombatKit.Fascinating));
        Assert.Equal(90f, w.Hero.MaxHp);
        Assert.Equal(45f, w.Hero.Hp, 3);
        Assert.Equal(1, w.Progression!.PendingRewards);
        var offer = w.Progression.RewardOffer();
        Assert.Equal(OfferSource.Shrine, offer.Source);
        var card = Assert.Single(offer.Cards);
        Assert.Equal(CombatKit.Mm, card.Id);
        Assert.Equal(5, card.ToLevel);
        w.Progression.ChooseReward(0);
        Assert.Equal(5, w.Hero.Weapons[0].Level);
    }

    [Fact]
    public void FascinatingShrine_NoCandidate_Temper_NotAvailable()
    {
        var t = Fresh();
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        Assert.False(w.Progression!.RewardAvailable(R(w, CombatKit.Fascinating)));
        Assert.False(w.Progression.RewardAvailable(R(w, CombatKit.Enchanted)));     // 진화 아닌 무기 둘 미만
        Assert.True(w.Progression.RewardAvailable(R(w, TestDefs.RewardChestId)));   // 보물 상자는 늘 참
        Pick(t, R(w, CombatKit.Fascinating));
        Assert.Equal(0, w.Progression.PendingRewards);
        Assert.Equal(5f, w.Combat.Mods[(int)StatKey.MaxHp]);
    }

    [Fact]
    public void RerollsShared_LevelUpAndReward_SkipRewardHeals()
    {
        var t = Fresh();
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 3);
        ((ProgressionService)w.Progression!).AddXp(5);
        w.Progression.Reroll();
        w.Progression.Skip();
        Pick(t, R(w, TestDefs.RewardTomeId));
        Assert.Equal(2, w.Progression.RewardOffer().RerollsLeft);
        w.Progression.RerollReward();
        Assert.Equal(1, w.Combat.RerollsLeft);
        w.Hero.Hp = 20f;
        w.Progression.SkipReward();
        Assert.Equal(30f, w.Hero.Hp);
    }

    [Fact]
    public void PendingOrder_LevelUpThenChestThenReward()
    {
        var t = Fresh();
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 3);
        w.Combat.Chests.Add(1);
        ((ProgressionService)w.Progression!).AddXp(5);
        Pick(t, R(w, TestDefs.RewardTomeId));
        Assert.Equal(ChoiceKind.LevelUp, w.AwaitingChoice);
        w.Progression.Skip();
        Assert.Equal(ChoiceKind.Chest, w.AwaitingChoice);
        w.Progression.OpenChest();
        Assert.Equal(ChoiceKind.Reward, w.AwaitingChoice);
        w.Progression.SkipReward();
        Assert.Equal(ChoiceKind.None, w.AwaitingChoice);
    }

    // ── 물약 칸(v0.9.1 — 허리띠 저절로 마심은 없앰, WP1g) ────────────────────

    [Fact]
    public void Potions_NoAutoDrink_PickupStacksUntilFull()
    {
        var t = Fresh();
        var w = t.World;
        w.Hero.Potions = 1;
        w.Damage.ToHero(EntityId.None, 80f);                        // 20 — v0.8 은 여기서 저절로 마셨다
        t.Step(70);
        Assert.Equal(20f, w.Hero.Hp);
        Assert.Equal(1, w.Hero.Potions);
        CombatKit.Drop(w, "t_potion", t.Hero.Pos);
        t.Step();
        Assert.Equal(0, w.Pickups.LiveCount);
        Assert.Equal(2, w.Hero.Potions);                            // 칸 2 가득
        CombatKit.Drop(w, "t_potion", t.Hero.Pos);
        t.Step(5);
        Assert.Equal(1, w.Pickups.LiveCount);                        // 가득이면 안 집힘(그대로 남음)
        Assert.Equal(2, w.Hero.Potions);
        Assert.Equal(20f, w.Hero.Hp);                                // 닿아도 바로 마시지 않는다
    }

    // ── 시간 효과·칸 5 ───────────────────────────────────────────

    [Fact]
    public void Surge_TicksOnlyInSurviveOrCleanup_AndCarried()
    {
        var t = CombatKit.RoomWorld(RoomPhase.Survive);
        var w = t.World;
        CombatKit.Drop(w, "t_surge", t.Hero.Pos);
        t.Step();
        var start = w.Combat.SurgeTicks;
        Assert.Equal(8 * 60, start);
        Assert.Equal(0.5f, w.Combat.SurgePct);
        t.Step(10);
        Assert.Equal(start - 10, w.Combat.SurgeTicks);
        w.Room!.EnterPhase(RoomPhase.Reward, w.Tick);
        t.Step(30);
        Assert.Equal(start - 10, w.Combat.SurgeTicks);
        var carry = new HeroCarrier().Capture(w);
        var e = Assert.Single(carry.Effects);
        Assert.Equal(CombatState.SurgeEffectId, e.Id);
        Assert.Equal(start - 10, e.TicksLeft);
        var t2 = CombatKit.RoomWorld(RoomPhase.Enter);
        new HeroCarrier().Restore(t2.World, carry);
        t2.Step(30);
        Assert.Equal(start - 10, t2.World.Combat.SurgeTicks);       // 들어옴 단계에서도 안 줄어듦
        t2.World.Room!.EnterPhase(RoomPhase.Survive, t2.World.Tick);
        t2.Step(5);
        Assert.Equal(start - 15, t2.World.Combat.SurgeTicks);
    }

    [Fact]
    public void FiveOpenSlots_FiveWeapons_FifthSlotDamageCounted()
    {
        var a = CombatKit.Extra("t_w4");
        var b = CombatKit.Extra("t_w5");
        var t = CombatKit.World(CombatKit.Defs(extraWeapons: [a, b]));
        var w = t.World;
        w.Hero.WeaponSlotsOpen = 5;
        new Core.Sim.Heroes.HeroRunSetup().Apply(w, new RunOptions
        {
            Weapons = [new(CombatKit.Mm, 1), new(CombatKit.Fb, 1), new(CombatKit.Orbit, 1), new("t_w4", 1), new("t_w5", 1)],
        });
        for (var i = 0; i < 5; i++) Assert.False(w.Hero.Weapons[i].IsEmpty);
        Assert.True(w.Hero.Weapons[5].IsEmpty);
        var e = t.AddEnemy(t.Hero.Pos + new Vector2(150, 0), CombatKit.Skeleton);
        e.Hp = e.MaxHp = 10000f;
        t.Step(120);
        Assert.True(w.RunStats.DamageByWeapon[4] > 0f);
        Assert.Throws<ArgumentException>(() => new Core.Sim.Heroes.HeroRunSetup().Apply(w, new RunOptions
        {
            Weapons = [new(CombatKit.Mm, 1), new(CombatKit.Fb, 1), new(CombatKit.Orbit, 1), new("t_w4", 1), new("t_w5", 1), new(CombatKit.Mm, 2)],
        }));
    }

    [Fact]
    public void LevelUp_NewWeaponOnlyInOpenSlots()
    {
        var t = CombatKit.World();
        var w = t.World;
        w.Hero.WeaponSlotsOpen = 1;                                 // 시작 무기 하나로 꽉 참
        ((ProgressionService)w.Progression!).AddXp(5);
        Assert.DoesNotContain(w.Progression.Offer().Cards, c => c.Kind == CardKind.NewWeapon);
    }

    // ── RoomCleared ──────────────────────────────────────────────

    [Fact]
    public void RoomCleared_NextTick_MagnetizesGemsAndGold_NotPotions()
    {
        var t = CombatKit.RoomWorld(RoomPhase.Cleanup);
        var w = t.World;
        CombatKit.ClearWeapons(w);
        var gem = CombatKit.Drop(w, "t_gem_blue", t.Hero.Pos + new Vector2(400, 0), 3);
        var gold = CombatKit.Drop(w, "t_gold", t.Hero.Pos + new Vector2(-400, 0), 5);
        var potion = CombatKit.Drop(w, "t_potion", t.Hero.Pos + new Vector2(0, 300));
        t.Step();                                                    // 정리 끝(적 0) → 보상 단계 + RoomCleared
        Assert.Equal(RoomPhase.Reward, w.Room!.Phase);
        Assert.False(gem.Magnetized);
        t.Step();
        Assert.True(gem.Magnetized);
        Assert.True(gold.Magnetized);
        Assert.False(potion.Magnetized);
        t.Step(120);
        Assert.Equal(3, w.Progression!.Xp);
        Assert.Equal(5, w.RunStats.Gold);
    }

    [Fact]
    public void BossRoomCleared_FullHeal()
    {
        var t = CombatKit.World();
        var w = t.World;
        w.Hero.Hp = 10f;
        w.Events.Current.RoomCleared.Add(new RoomCleared(RoomKind.Boss, false));
        t.Step();
        Assert.Equal(w.Hero.MaxHp, w.Hero.Hp);
    }
}
