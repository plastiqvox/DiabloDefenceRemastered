using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Combat;

namespace DiabloDefenceRemastered.Tests.Progression;

/// <summary>10.1 <c>Progression</c> — 경험치 곡선·레벨업 후보·재추첨·건너뛰기·상자·밀어내기·직업(v0.6)·특성(v0.6.1).</summary>
public class ProgressionTests
{
    private static ProgressionService P(World w) => (ProgressionService)w.Progression!;

    [Fact]
    public void XpCurve_V08()
    {
        var curve = CombatKit.Defs().Rules.XpCurve;
        Assert.Equal(5, XpCurve.Need(curve, 1));
        Assert.Equal(15, XpCurve.Need(curve, 2));
        Assert.Equal(185, XpCurve.Need(curve, 19));
        Assert.Equal(215, XpCurve.Need(curve, 20));
        Assert.Equal(785, XpCurve.Need(curve, 39));
        Assert.Equal(825, XpCurve.Need(curve, 40));
        Assert.Equal(int.MaxValue, XpCurve.Need([], 1));
    }

    [Fact]
    public void AddXp_MultipleLevels_PendingAndAwaiting()
    {
        var t = CombatKit.World();
        var w = t.World;
        P(w).AddXp(5 + 15 + 3);
        Assert.Equal(3, w.Progression!.Level);
        Assert.Equal(3, w.Progression.Xp);
        Assert.Equal(2, w.Progression.PendingLevelUps);
        Assert.Equal(2, w.Events.Current.LevelUpReady.Count);
        Assert.Equal(23, w.RunStats.XpCollected);
        t.Step();
        Assert.Equal(ChoiceKind.LevelUp, w.AwaitingChoice);
        var first = w.Progression.Offer();
        Assert.Equal(2, first.Level);
        w.Progression.Skip();
        Assert.Equal(ChoiceKind.LevelUp, w.AwaitingChoice);
        Assert.Equal(3, w.Progression.Offer().Level);
        w.Progression.Skip();
        Assert.Equal(ChoiceKind.None, w.AwaitingChoice);
    }

    [Fact]
    public void Offer_ThreeDistinct_NoTrait_GuaranteeNewWeaponAtLv2()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var t = CombatKit.World(seed: seed);
            var w = t.World;
            P(w).AddXp(5);
            var offer = w.Progression!.Offer();
            Assert.Equal(3, offer.Cards.Count);
            Assert.Equal(3, offer.Cards.Select(c => c.Id).Distinct().Count());
            Assert.DoesNotContain(offer.Cards, c => c.Id == CombatKit.Trait);
            Assert.Contains(offer.Cards, c => c.Kind == CardKind.NewWeapon);
            Assert.Equal(3, offer.RerollsLeft);
        }
    }

    [Fact]
    public void Offer_FullAndMaxed_FillersOnly_TwoCards()
    {
        var t = CombatKit.World();
        var w = t.World;
        w.Hero.WeaponSlotsOpen = 1;
        w.Hero.PassiveSlotsOpen = 1;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        w.Hero.Passives[0] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Power), Level = 5 };
        P(w).AddXp(5);
        var offer = w.Progression!.Offer();
        Assert.Equal(2, offer.Cards.Count);
        Assert.All(offer.Cards, c => Assert.Equal(CardKind.Filler, c.Kind));
        var hp = w.Hero.MaxHp;
        w.Progression.Choose(offer.Cards.ToList().FindIndex(c => c.Id == CombatKit.Temper));
        Assert.Equal(hp + 5, w.Hero.MaxHp);
        Assert.Equal(5f, w.Combat.Mods[(int)StatKey.MaxHp]);
    }

    [Fact]
    public void Choose_NewWeapon_FillsFirstOpenSlot_Upgrade_SetsMaxedTick()
    {
        var t = CombatKit.World();
        var w = t.World;
        P(w).AddXp(5);
        var offer = w.Progression!.Offer();
        var i = offer.Cards.ToList().FindIndex(c => c.Kind == CardKind.NewWeapon);
        var id = offer.Cards[i].Id;
        w.Progression.Choose(i);
        Assert.Equal(id, w.Hero.Weapons[1].Def!.Id);
        Assert.Equal(1, w.Hero.Weapons[1].Level);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 7);
        Core.Sim.Progression.Build.Apply(w, new LevelUpCard(CardKind.WeaponUpgrade, CombatKit.Mm, "", null, 7, 8, []));
        Assert.Equal(8, w.Hero.Weapons[0].Level);
        Assert.Equal(w.Tick, w.Hero.Weapons[0].MaxedAtTick);
    }

    [Fact]
    public void Reroll_UsesCount_SkipHeals10()
    {
        var t = CombatKit.World();
        var w = t.World;
        P(w).AddXp(5);
        w.Progression!.Reroll();
        w.Progression.Reroll();
        w.Progression.Reroll();
        w.Progression.Reroll();                                     // 0 이면 아무것도 안 함
        Assert.Equal(0, w.Progression.Offer().RerollsLeft);
        w.Hero.Hp = 50f;
        w.Progression.Skip();
        Assert.Equal(60f, w.Hero.Hp);
    }

    [Fact]
    public void Vitality_OnAcquireHeals_AndAddsMaxHp()
    {
        var t = CombatKit.World();
        var w = t.World;
        w.Hero.Hp = 50f;
        Core.Sim.Progression.Build.Apply(w, new LevelUpCard(CardKind.NewPassive, CombatKit.Vitality, "", null, 0, 1, []));
        Assert.Equal(120f, w.Hero.MaxHp);
        Assert.Equal(70f, w.Hero.Hp);
    }

    [Fact]
    public void LevelUpPush_NextTickAfterChoose_Radius150_Distance80()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        var near = t.AddEnemy(t.Hero.Pos + new Vector2(100, 0), CombatKit.Skeleton);
        var far = t.AddEnemy(t.Hero.Pos + new Vector2(200, 0), CombatKit.Skeleton);
        P(w).AddXp(5);
        w.Progression!.Skip();
        t.Step();
        Assert.True(near.KnockbackTime > 0f);
        Assert.Equal(80f, near.KnockbackVel.X * near.KnockbackTime, 2);
        Assert.Equal(0f, far.KnockbackTime);
        Assert.Equal(20f, near.Hp);
    }

    // ── 상자 ─────────────────────────────────────────────────────

    [Fact]
    public void Chest_EvolutionFirst_ThenUpgrades_ElseTemper()
    {
        var mm = CombatKit.MagicMissile();
        var power = CombatKit.Passive(CombatKit.Power, StatKey.DamagePct, 0.1f);
        var storm = new WeaponDef { Id = "t_storm", Name = "비전 폭풍", BaseWeapon = DefRef<WeaponDef>.To(mm) };
        var basic = CombatKit.Defs();
        var defs = new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Projectiles = basic.Projectiles,
            Weapons = new DefTable<WeaponDef>([.. basic.Weapons.All, storm]), Passives = basic.Passives, HeroClasses = basic.HeroClasses,
            Tilesets = basic.Tilesets, Maps = basic.Maps, Stages = basic.Stages, Enemies = basic.Enemies, Rewards = basic.Rewards, Pickups = basic.Pickups,
            Evolutions = new DefTable<EvolutionDef>([new EvolutionDef { Weapon = DefRef<WeaponDef>.To(mm), Passive = DefRef<PassiveDef>.To(power), Result = DefRef<WeaponDef>.To(storm) }]),
        };
        var t = CombatKit.World(defs);
        var w = t.World;
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        CombatKit.SetWeapon(w, 1, CombatKit.Fb, 3);
        w.Hero.Passives[0] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Power), Level = 1 };
        w.Combat.Chests.Add(3);
        var r = w.Progression!.OpenChest();
        Assert.Equal(3, r.Items.Count);
        Assert.Equal(ChestItemKind.Evolution, r.Items[0].Kind);
        Assert.Equal("t_storm", w.Hero.Weapons[0].Def!.Id);
        Assert.True(w.Hero.Weapons[0].Evolved);
        Assert.All(r.Items.Skip(1), i => Assert.NotEqual(ChestItemKind.Temper, i.Kind));

        // 올릴 것이 없으면 단련
        CombatKit.SetWeapon(w, 1, CombatKit.Fb, 8);
        w.Hero.Passives[0] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Power), Level = 5 };
        w.Combat.Chests.Add(1);
        var r2 = w.Progression.OpenChest();
        Assert.Equal(ChestItemKind.Temper, Assert.Single(r2.Items).Kind);
        Assert.Equal(5f, w.Combat.Mods[(int)StatKey.MaxHp]);
    }

    // ── 직업(v0.6)·특성(v0.6.1) ──────────────────────────────────

    private static GameDefs ClassDefs(out HeroClassDef warrior, out HeroClassDef mage)
    {
        var basic = CombatKit.Defs();
        var set = basic.SpriteSets[0];
        var fb = basic.Weapons.Get(CombatKit.Fb);
        var mm = basic.Weapons.Get(CombatKit.Mm);
        var crit = new PassiveDef { Id = "t_trait_crit", Trait = true, MaxLevel = 1, PerLevel = new Dictionary<StatKey, float> { [StatKey.CritChance] = 0.06f } };
        var regen = new PassiveDef { Id = "t_trait_regen", Trait = true, MaxLevel = 1, PerLevel = new Dictionary<StatKey, float> { [StatKey.Regen] = 0.2f } };
        warrior = new HeroClassDef
        {
            Id = "t_warrior", Sprites = DefRef<SpriteSetDef>.To(set), StartWeapons = [DefRef<WeaponDef>.To(fb)],
            Stats = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 130, [StatKey.MoveSpeed] = 100 },
            Traits = [DefRef<PassiveDef>.To(crit), DefRef<PassiveDef>.To(regen)],
        };
        mage = basic.HeroClasses[0];
        var warriorOnly = CombatKit.Extra("t_cleave") with { Classes = [DefRef<HeroClassDef>.To(warrior)] };
        var hellfire = CombatKit.Extra("t_hf_spell") with { Requires = [ContentBundle.Hellfire] };
        var warriorPassive = CombatKit.Passive("t_war_passive", StatKey.Armor, 1) with { Classes = [DefRef<HeroClassDef>.To(warrior)] };
        return new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Projectiles = basic.Projectiles,
            Weapons = new DefTable<WeaponDef>([mm, fb, basic.Weapons.Get(CombatKit.Orbit), warriorOnly, hellfire]),
            Passives = new DefTable<PassiveDef>([.. basic.Passives.All, crit, regen, warriorPassive]),
            HeroClasses = new DefTable<HeroClassDef>([mage, warrior]),
            Tilesets = basic.Tilesets, Maps = basic.Maps, Stages = basic.Stages, Enemies = basic.Enemies, Rewards = basic.Rewards, Pickups = basic.Pickups,
        };
    }

    [Fact]
    public void HeroSetup_BaseThenClassOverrideThenTraits_StartWeapon_TraitsUseNoSlot()
    {
        var defs = ClassDefs(out var warrior, out var mage);
        var tw = CombatKit.World(defs, configure: b => b.WithHeroClass(warrior));
        var h = tw.Hero;
        Assert.Equal(130f, h.MaxHp);
        Assert.Equal(130f, h.Hp);
        Assert.Equal(100f, h.Stats[StatKey.MoveSpeed]);
        Assert.Equal(0.06f, h.Stats[StatKey.CritChance], 5);
        Assert.Equal(0.2f, h.Stats[StatKey.Regen], 5);
        Assert.Equal(0f, h.Stats[StatKey.CooldownPct]);
        Assert.Equal(CombatKit.Fb, h.Weapons[0].Def!.Id);
        Assert.True(h.Passives[0].IsEmpty);
        Assert.Equal(4, h.WeaponSlotsOpen);

        var tm = CombatKit.World(defs);
        Assert.Equal(100f, tm.Hero.MaxHp);
        Assert.Equal(0.05f, tm.Hero.Stats[StatKey.CooldownPct], 5);
        Assert.Equal(CombatKit.Mm, tm.Hero.Weapons[0].Def!.Id);
        Assert.Same(mage, tm.World.HeroClass);
    }

    [Fact]
    public void Candidates_FilterClasses_AndRequires()
    {
        var defs = ClassDefs(out var warrior, out _);
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var tm = CombatKit.World(defs, seed);
            ((ProgressionService)tm.World.Progression!).AddXp(5);
            foreach (var c in tm.World.Progression!.Offer().Cards)
            {
                Assert.NotEqual("t_cleave", c.Id);
                Assert.NotEqual("t_hf_spell", c.Id);
                Assert.NotEqual("t_war_passive", c.Id);
                Assert.DoesNotContain("trait", c.Id);
            }
        }
        var seen = new HashSet<string>();
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var tw = CombatKit.World(defs, seed, b => b.WithHeroClass(warrior));
            ((ProgressionService)tw.World.Progression!).AddXp(5);
            foreach (var c in tw.World.Progression!.Offer().Cards) seen.Add(c.Id);
        }
        Assert.Contains("t_cleave", seen);
        Assert.DoesNotContain("t_hf_spell", seen);
    }

    [Fact]
    public void HeroSetup_DoesNotTouchRunStats()
    {
        var t = CombatKit.RoomWorld(RoomPhase.Survive);
        var w = t.World;
        w.RunStats.Kills = 9;
        w.RunStats.DamageByWeapon[2] = 4f;
        Core.Sim.Heroes.HeroSetup.Create(w);
        w.Hero.Reset();
        Assert.Equal(9, w.RunStats.Kills);
        Assert.Equal(4f, w.RunStats.DamageByWeapon[2]);
    }

    // ── 축복(v0.7)·진화 classes(v0.6.1) ─────────────────────────

    private static GameDefs BlessingDefs(HeroClassDef? evoClass = null)
    {
        var basic = CombatKit.Defs();
        var mm = basic.Weapons.Get(CombatKit.Mm);
        var power = basic.Passives.Get(CombatKit.Power);
        var storm = new WeaponDef { Id = "t_storm", Name = "비전 폭풍", BaseWeapon = DefRef<WeaponDef>.To(mm) };
        var blessing = CombatKit.Passive("t_bless_armor", StatKey.Armor, 3, max: 2) with { Blessing = true };
        var evo = new EvolutionDef
        {
            Weapon = DefRef<WeaponDef>.To(mm), Passive = DefRef<PassiveDef>.To(power), Result = DefRef<WeaponDef>.To(storm),
            Classes = evoClass is null ? [] : [DefRef<HeroClassDef>.To(evoClass)],
        };
        return new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Projectiles = basic.Projectiles,
            Weapons = new DefTable<WeaponDef>([.. basic.Weapons.All, storm]), Passives = new DefTable<PassiveDef>([.. basic.Passives.All, blessing]),
            HeroClasses = basic.HeroClasses, Tilesets = basic.Tilesets, Maps = basic.Maps, Stages = basic.Stages, Enemies = basic.Enemies,
            Rewards = basic.Rewards, Pickups = basic.Pickups, Evolutions = new DefTable<EvolutionDef>([evo]),
        };
    }

    [Fact]
    public void Blessing_NotInLevelUpOrChest_GoesToBlessingSlot_AddsStats()
    {
        var defs = BlessingDefs();
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var t = CombatKit.World(defs, seed);
            P(t.World).AddXp(5);
            Assert.DoesNotContain(t.World.Progression!.Offer().Cards, c => c.Id == "t_bless_armor");
        }
        var tw = CombatKit.World(defs);
        var w = tw.World;
        new Core.Sim.Heroes.HeroRunSetup().Apply(w, new RunOptions { Passives = [new("t_bless_armor", 2), new(CombatKit.Vitality, 1)] });
        Assert.Equal("t_bless_armor", w.Hero.Blessings[0].Def!.Id);
        Assert.Equal(CombatKit.Vitality, w.Hero.Passives[0].Def!.Id);                     // 축복은 패시브 칸을 쓰지 않음
        Assert.True(w.Hero.Passives[1].IsEmpty);
        Assert.Equal(6f, w.Hero.Stats[StatKey.Armor]);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        for (var i = 0; i < 5; i++)
        {
            w.Combat.Chests.Add(1);
            foreach (var item in w.Progression!.OpenChest().Items) Assert.NotEqual("t_bless_armor", item.Id);
        }
    }

    [Fact]
    public void Evolution_Classes_FilterChest()
    {
        var other = new HeroClassDef { Id = "t_other", Sprites = DefRef<SpriteSetDef>.To(CombatKit.Defs().SpriteSets[0]), StartWeapons = [] };
        foreach (var (defs, evolves) in new[] { (BlessingDefs(), true), (BlessingDefs(other), false) })
        {
            var w = CombatKit.World(defs).World;
            CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
            w.Hero.Passives[0] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Power), Level = 1 };
            w.Combat.Chests.Add(1);
            var r = w.Progression!.OpenChest();
            Assert.Equal(evolves, r.Items[0].Kind == ChestItemKind.Evolution);
            Assert.Equal(evolves ? "t_storm" : CombatKit.Mm, w.Hero.Weapons[0].Def!.Id);
        }
    }
}
