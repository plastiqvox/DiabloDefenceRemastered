using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Common;

/// <summary>
/// WP1g 완료 기준 — 형(설계서 11.1 WP1g·6.18·6.19): 읽기 시험(보상 다섯 종류·장비·접사·상인·규칙 셋·옛 <c>rules.hero.belt</c>), 로딩 검사(13.5-23 ④ 종류마다 칸·6.19),
/// 실제 데이터(기본 아이템 18·접사 11·상인 셋), 짐·스냅숏 JSON 왕복(다시 쓴 바이트 같음), 새 계약 멤버.
/// </summary>
public class ContractV09Tests
{
    // ── 읽기: 6.19 정의·규칙 ─────────────────────────────────────

    [Fact]
    public void ItemBase_Affix_Merchant_Read()
    {
        var b = TestJson.Read<ItemBaseDef>("""{ "id": "eq_helm", "name": "Helm", "kind": "head", "weight": "medium", "stats": { "max_hp": 20, "armor": 2 }, "price": 130, "acts": [1, 2] }""");
        Assert.Equal((EquipKind.Head, ArmorWeight.Medium, 130, 2f), (b.Kind, b.Weight, b.Price, b.Stats[StatKey.Armor]));
        Assert.Equal(ArmorWeight.None, TestJson.Read<ItemBaseDef>("""{ "id": "s", "kind": "staff", "price": 60 }""").Weight);
        Assert.Throws<JsonException>(() => TestJson.Read<ItemBaseDef>("""{ "id": "s", "kind": "staff" }"""));            // price 필수
        Assert.Throws<JsonException>(() => TestJson.Read<ItemBaseDef>("""{ "id": "s", "kind": "amulet", "price": 1 }"""));   // 목걸이 없음

        var a = TestJson.Read<ItemAffixDef>("""{ "id": "af_fox", "name": "of the Fox", "prefix": false, "kinds": ["head", "ring"], "stat": "max_hp", "min": 8, "max": 12, "step": 1, "price": 80 }""");
        Assert.Equal((false, 8f, 12f, 1, 80), (a.Prefix, a.Min, a.Max, a.Stage, a.Price));
        Assert.Throws<JsonException>(() => TestJson.Read<ItemAffixDef>("""{ "id": "x", "name": "x", "prefix": true, "kinds": [], "stat": "armor", "min": 1, "price": 1 }"""));   // max 필수

        var m = TestJson.Read<MerchantDef>("""{ "id": "healer", "name": "페핀", "stock": "potions", "rerollable": false, "upgrades": [ { "kind": "slots", "prices": [80, 150, 220] }, { "kind": "refill", "per_slot": 25 } ] }""");
        Assert.Equal((MerchantStock.Potions, false, 2), (m.Stock, m.Rerollable, m.Upgrades.Count));
        Assert.Null(m.Sprite);
    }

    [Fact]
    public void Rules_Equipment_Potions_RoomRewards_Read_OldBeltThrows()
    {
        var r = TestJson.Read<RulesDef>("""
            { "equipment": { "magic_chance": [0.35, 0.60], "two_affix_chance": 0.25, "price_roll": [0.75, 0.5], "price_round": 5, "armor_cap": 15,
                             "weight_move": { "body": { "light": 0, "medium": -0.05, "heavy": -0.10 }, "head": { "heavy": -0.04 } },
                             "weight_move_floor": -0.15, "reroll_price": 30, "reroll_step": 30 },
              "hero": { "class": "sorcerer", "potions": { "slots": 2, "slots_max": 5, "heal_pct": 0.40, "durations": [3.0, 2.0, 1.2], "bot_threshold": 0.35 } } }
            """);
        var eq = r.Equipment!;
        Assert.Equal(15f, eq.ArmorCap);
        Assert.Equal(-0.10f, eq.WeightMove[EquipKind.Body][ArmorWeight.Heavy]);
        Assert.Equal(-0.04f, eq.WeightMove[EquipKind.Head][ArmorWeight.Heavy]);
        Assert.Equal((0.35f, 0.60f, 0.60f), (eq.MagicChanceAt(1), eq.MagicChanceAt(2), eq.MagicChanceAt(3)));
        var p = r.Hero!.Potions!;
        Assert.Equal((2, 5), (p.Capacity(0), p.Capacity(9)));
        Assert.Equal((3.0f, 1.2f), (p.Duration(0), p.Duration(7)));
        Assert.Throws<JsonException>(() => TestJson.Read<HeroRulesDef>("""{ "class": "x", "belt": { "slots": 2, "threshold": 0.3, "cooldown": 1.0 } }"""));   // 옛 belt
        Assert.Throws<JsonException>(() => TestJson.Read<PotionRulesDef>("""{ "slots": 2, "heal_pct": 0.4, "durations": [3], "bot_threshold": 0.35 }"""));     // slots_max 필수
        Assert.Throws<JsonException>(() => TestJson.Read<EquipmentRulesDef>("""{ "magic_chance": [0.3] }"""));
    }

    // ── 로딩 검사(13.5-23 ④·6.19) ────────────────────────────────

    private static List<DataError> Errors(LoaderFixture f) => LoaderFixture.ErrorsOf(f.Load());

    private static void HasError(LoaderFixture f, string contains)
    {
        var errors = Errors(f);
        Assert.True(errors.Any(e => e.Message.Contains(contains, StringComparison.Ordinal)), $"'{contains}' 없음:\n{string.Join("\n", errors)}");
    }

    private static LoaderFixture WithEquipment()
    {
        var f = new LoaderFixture();
        f.Files["rules/equipment.json"] = LoaderFixture.Obj("""
            { "rules": { "equipment": { "magic_chance": [0.35, 0.6], "two_affix_chance": 0.25, "price_roll": [0.75, 0.5], "price_round": 5, "armor_cap": 15,
                                        "weight_move": { "body": { "heavy": -0.1 } }, "weight_move_floor": -0.15, "reroll_price": 30, "reroll_step": 30 } } }
            """);
        f.Files["items/bases.json"] = LoaderFixture.Obj("""
            { "items": [ { "id": "eq_cap", "name": "Cap", "kind": "head", "weight": "light", "stats": { "max_hp": 10 }, "price": 40 },
                         { "id": "eq_ring", "name": "Ring", "kind": "ring", "always_magic": true, "price": 50 } ] }
            """);
        f.Files["items/affixes.json"] = LoaderFixture.Obj("""
            { "item_affixes": [ { "id": "af_fine", "name": "Fine", "prefix": true, "kinds": ["head"], "stat": "armor", "min": 1, "max": 2, "step": 1, "price": 90 } ] }
            """);
        f.Files["merchants/smith.json"] = LoaderFixture.Obj("""{ "merchants": [ { "id": "smith", "name": "그리스월드", "stock": "equipment", "kinds": ["head"] } ] }""");
        var rewards = (JsonArray)f.Files["rewards.json"]["rewards"]!;
        rewards.Add(LoaderFixture.Obj("""{ "id": "rw_heal", "kind": "heal_next_room" }"""));
        rewards.Add(LoaderFixture.Obj("""{ "id": "rw_smith", "kind": "merchant", "merchant": "smith" }"""));
        return f;
    }

    [Fact]
    public void Loader_EquipmentKinds_HealDoorWithoutObject_MerchantRef_NoErrors()
    {
        var r = WithEquipment().Load();
        Assert.False(r.HasErrors, string.Join("\n", r.Errors));
        Assert.Equal(2, r.Defs.ItemBases.Count);
        Assert.Equal("af_fine", r.Defs.ItemAffixes[0].Id);
        Assert.Same(r.Defs.Merchants.Get("smith"), r.Defs.Rewards.Get("rw_smith").Merchant!.Def);
        Assert.Null(r.Defs.Rewards.Get("rw_heal").Object);
        Assert.Contains("items", GameDefs.Kinds);
        Assert.Contains("item_affixes", GameDefs.Kinds);
        Assert.Contains("merchants", GameDefs.Kinds);

        var objectToo = WithEquipment();
        ((JsonArray)objectToo.Files["rewards.json"]["rewards"]!)[^2]!["object"] = objectToo.First("pickups.json", "pickups")["id"]!.GetValue<string>();
        Assert.False(objectToo.Load().HasErrors);                                            // heal_next_room 에 object 가 있어도 됨
    }

    [Theory]
    [InlineData("merchant_without_ref")]
    [InlineData("merchant_field_on_tome")]
    [InlineData("tome_without_object")]
    [InlineData("affix_min_gt_max")]
    [InlineData("affix_step_zero")]
    [InlineData("affix_stage_three")]
    [InlineData("potions_merchant_with_kinds")]
    [InlineData("staff_with_weight")]
    [InlineData("no_equipment_rules")]
    [InlineData("potion_durations_empty")]
    [InlineData("potion_slots_over_max")]
    [InlineData("bad_look_key")]
    public void Loader_Broken_IsError(string broken)
    {
        var f = WithEquipment();
        var rewards = (JsonArray)f.Files["rewards.json"]["rewards"]!;
        var affix = (JsonObject)f.Files["items/affixes.json"]["item_affixes"]![0]!;
        string expect;
        switch (broken)
        {
            case "merchant_without_ref":
                rewards.Add(LoaderFixture.Obj("""{ "id": "rw_m2", "kind": "merchant" }"""));
                expect = "merchant 보상에 상인(merchant)이 없다";
                break;
            case "merchant_field_on_tome":
                rewards[0]!["merchant"] = "smith";
                expect = "merchant 칸은 merchant 보상에만";
                break;
            case "tome_without_object":
                ((JsonObject)rewards[0]!).Remove("object");
                expect = "방 안 물체(object)가 없다";
                break;
            case "affix_min_gt_max":
                affix["min"] = 3;
                expect = "범위가 거꾸로다";
                break;
            case "affix_step_zero":
                affix["step"] = 0;
                expect = "굴림 간격은 0 보다";
                break;
            case "affix_stage_three":
                affix["stage"] = 3;
                expect = "단계는 1";
                break;
            case "potions_merchant_with_kinds":
                f.Files["merchants/healer.json"] = LoaderFixture.Obj("""{ "merchants": [ { "id": "healer", "name": "페핀", "stock": "potions", "rerollable": false, "kinds": ["ring"], "upgrades": [ { "kind": "refill", "per_slot": 25 } ] } ] }""");
                expect = "kinds 는 장비 상인";
                break;
            case "staff_with_weight":
                ((JsonArray)f.Files["items/bases.json"]["items"]!).Add(LoaderFixture.Obj("""{ "id": "eq_staff", "kind": "staff", "weight": "heavy", "price": 1 }"""));
                expect = "무게는 방어구";
                break;
            case "no_equipment_rules":
                f.Files.Remove("rules/equipment.json");
                expect = "rules.equipment 가 없다";
                break;
            case "potion_durations_empty":
                f.Files["rules/hero.json"]["rules"]!["hero"]!["potions"] = LoaderFixture.Obj("""{ "slots": 2, "slots_max": 5, "heal_pct": 0.4, "durations": [], "bot_threshold": 0.35 }""");
                expect = "durations";
                break;
            case "potion_slots_over_max":
                f.Files["rules/hero.json"]["rules"]!["hero"]!["potions"] = LoaderFixture.Obj("""{ "slots": 6, "slots_max": 5, "heal_pct": 0.4, "durations": [3], "bot_threshold": 0.35 }""");
                expect = "slots_max";
                break;
            default:
                var hero = f.First("heroes/sorcerer.json", "hero_classes");
                hero["looks"] = LoaderFixture.Obj("""{ "lx": "set_skelaxe" }""");
                expect = "겉모습 키 'lx'";
                break;
        }
        HasError(f, expect);
    }

    // ── 실제 데이터 ─────────────────────────────────────────────

    [Fact]
    public void RealData_Items18_Affixes11_Merchants3_Rules()
    {
        var ctx = GameModules.CreateContext();
        var r = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(r.HasErrors, string.Join("\n", r.Errors));
        var d = r.Defs;
        Assert.Equal(18, d.ItemBases.Count);
        Assert.Equal(11, d.ItemAffixes.Count);
        Assert.Equal(new HashSet<string> { "smith", "witch", "healer" }, d.Merchants.All.Select(m => m.Id).ToHashSet());
        Assert.Equal(15f, d.Rules.Equipment!.ArmorCap);                                     // v0.9.2 열린 질문 52 답
        Assert.Equal(13f, new[] { "eq_full_helm", "eq_breast_plate", "eq_kite_shield" }.Sum(id => d.ItemBases.Get(id).Stats[StatKey.Armor]));   // 판금 셋 13 ≤ 15
        Assert.Equal((2, 5, 0.40f), (d.Rules.Hero!.Potions!.Slots, d.Rules.Hero.Potions.SlotsMax, d.Rules.Hero.Potions.HealPct));
        Assert.Equal([3.0f, 2.0f, 1.2f], d.Rules.Hero.Potions.Durations);
        Assert.Equal([4, 5], d.Rules.Rooms!.Rewards.MerchantGuaranteeRoom);
        Assert.Equal(PickupKind.PotionStack, d.Pickups.Get("healing_potion").Kind);
        Assert.Equal(RewardKind.HealNextRoom, d.Rewards.Get("reward_potion").Kind);
        Assert.Null(d.Rewards.Get("reward_potion").Object);
        Assert.Equal("healer", d.Rewards.Get("reward_healer").Merchant!.Id);
        Assert.All(d.ItemBases.All.Where(b => b.Kind is EquipKind.Head or EquipKind.Body or EquipKind.Shield), b => Assert.NotEqual(ArmorWeight.None, b.Weight));
        Assert.All(d.ItemBases.All.Where(b => b.Weight == ArmorWeight.Heavy), b => Assert.Equal([2], b.Acts));   // 판금은 액트 2 만
        Assert.True(d.ItemBases.Get("eq_ring").AlwaysMagic);
        // 실제 상인 물건이 정의·규칙으로 만들어진다(굴림이 실제 표에서 돈다)
        var witch = ShopStock.Create(d.Merchants.Get("witch"), 1, 4, 1234, d, new ShopPotions(0, 0, 0));
        Assert.Equal(3, witch.Cards.Count);
        Assert.All(witch.Cards, c => Assert.Equal(0, c.Price % 5));
    }

    // ── 짐·스냅숏 왕복 ───────────────────────────────────────────

    private static HeroCarry FullCarry() => new()
    {
        Level = 12, Xp = 345, RerollsLeft = 2, Hp = 77.5f,
        Weapons = [new CarriedSlot("w1", 8, false), new CarriedSlot("", 0, false), new CarriedSlot("w2", 8, true)],
        Passives = [new CarriedSlot("p1", 2, false)],
        Blessings = [new CarriedSlot("b1", 1, false)],
        WeaponSlotsOpen = 5, PassiveSlotsOpen = 5,
        Mods = [new CarriedStat(StatKey.MaxHp, 10f)],
        Shrines = ["shrine_gloomy"],
        Effects = [new CarriedEffect("buff_mana_surge", 90, 0.5f)],
        MaxedAtTick = [1, -1, 0, -1, -1, -1],
        Equipment =
        [
            new CarriedEquip(EquipSlot.Head, new EquipItem("eq_cap", new RolledAffix("af_fine", 2f), new RolledAffix("af_fox", 11f), 210)),
            new CarriedEquip(EquipSlot.Staff, new EquipItem("eq_short_staff", null, null, 60)),
            new CarriedEquip(EquipSlot.Ring2, new EquipItem("eq_ring", null, new RolledAffix("af_light", 0.25f), 110)),
        ],
        Gold = 345, Potions = 3, PotionSlotsLevel = 1, PotionSpeedLevel = 2,
    };

    [Fact]
    public void HeroCarry_V09Fields_JsonRoundTrip_SameBytes()
    {
        var carry = FullCarry();
        var json = JsonSerializer.Serialize(carry, RunSnapshot.JsonOptions);
        Assert.Contains("\"equipment\"", json);
        Assert.Contains("\"maxed_at_tick\"", json);
        Assert.DoesNotContain("\"belt\"", json);
        var back = JsonSerializer.Deserialize<HeroCarry>(json, RunSnapshot.JsonOptions)!;
        Assert.Equal(json, JsonSerializer.Serialize(back, RunSnapshot.JsonOptions));
        Assert.Equal(carry.Equipment, back.Equipment);                                      // 원소별(record 의 == 는 목록을 참조로 견줌)
        Assert.Equal(carry.MaxedAtTick, back.MaxedAtTick);
        Assert.Equal((345, 3, 1, 2), (back.Gold, back.Potions, back.PotionSlotsLevel, back.PotionSpeedLevel));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<HeroCarry>(json.Replace("\"potions\":3", "\"belt\":2"), RunSnapshot.JsonOptions));   // 옛 belt → 모르는 속성
    }

    [Fact]
    public void RunSnapshot_Format2_MerchantsMet_RoundTrip_MissingPositionalArgThrows()
    {
        var stats = new RunStats { Gold = 500, GoldSpent = 180, PotionsDrunk = 4 };
        var room = new RoomRecord(1, 4, "m", "gate_sw", "reward_smith", "gate_ne", 100, 20, false, 5000, 9,
                                  HpIn: 62.5f, GoldSpent: 180, Shop: "smith", Potions: 2, Drunk: 1);
        var snap = new RunSnapshot(RunSnapshot.CurrentFormat, 9ul, "a1", null, "a2", 2, "abcd1234", FullCarry(), stats, new FairnessCounters(), 777,
                                   new RunDepth(1, 6), [room], ["smith", "witch"]);
        Assert.Equal(3, RunSnapshot.CurrentFormat);   // v0.9 WP1g 에서 2, 보스 묶음 B(StatKey 셋)에서 3
        var json = snap.ToJson();
        var back = RunSnapshot.FromJson(json);
        Assert.Equal(json, back.ToJson());
        Assert.Equal(["smith", "witch"], back.MerchantsMet);
        Assert.Equal(room, back.Rooms[0]);
        Assert.Equal((180, 4), (back.Stats.GoldSpent, back.Stats.PotionsDrunk));
        Assert.Throws<JsonException>(() => RunSnapshot.FromJson(json.Replace(",\"merchants_met\":[\"smith\",\"witch\"]", "")));   // 위치 인자 필수(기본값으로 채우지 않음)
        Assert.Equal(-1f, new RoomRecord(1, 1, "m", "gate_sw", null, null, -1, 0, false, 0, 1).HpIn);                        // 새 열은 기본값 있음
    }

    [Fact]
    public void Resume_UnknownEquipmentOrMerchant_Mismatch()
    {
        var defs = RoomActs.Defs();
        RunSnapshot Snap(HeroCarry? carry, IReadOnlyList<string> met) =>
            new(RunSnapshot.CurrentFormat, 3ul, RoomActs.Act1Id, null, RoomActs.Act2Id, 2, RunSnapshot.RoomsHash(defs.Stages.Get(RoomActs.Act2Id)),
                carry, new RunStats(), new FairnessCounters(), 0, new RunDepth(1, 6), [], met);
        ModuleContext Mods(bool carry)
        {
            var m = new ModuleContext();
            m.AddWorldInstaller((w, _) => { w.Presentation.Enabled = false; w.Nav = new StraightNav(w.Arena!.Iso); w.Spatial = new BruteForceSpatial(w); w.Spawner = new FakeSpawner(w); });
            if (carry) m.SetHeroCarry(new FakeHeroCarry());
            return m;
        }
        var ok = new HeroCarry { Level = 1, Xp = 0, RerollsLeft = 0, Hp = 10, WeaponSlotsOpen = 5, PassiveSlotsOpen = 5,
                                 Equipment = [new CarriedEquip(EquipSlot.Body, new EquipItem("t_quilted", null, new RolledAffix("t_af_fox", 10f), 150))] };
        var resumed = new RunSession(defs, Mods(true), Snap(ok, [TestDefs.SmithId]), RunOptions.Default);
        Assert.Equal("t_quilted", resumed.World.Hero.Equipment[(int)EquipSlot.Body]!.BaseId);
        Assert.Equal([TestDefs.SmithId], resumed.Run.MerchantsMet);
        Assert.Throws<RunSnapshotMismatchException>(() => new RunSession(defs, Mods(true), Snap(ok with { Equipment = [new CarriedEquip(EquipSlot.Body, new EquipItem("eq_nope", null, null, 1))] }, []), RunOptions.Default));
        Assert.Throws<RunSnapshotMismatchException>(() => new RunSession(defs, Mods(true), Snap(ok with { Equipment = [new CarriedEquip(EquipSlot.Body, new EquipItem("t_quilted", new RolledAffix("af_nope", 1), null, 1))] }, []), RunOptions.Default));
        Assert.Throws<RunSnapshotMismatchException>(() => new RunSession(defs, Mods(true), Snap(ok, ["nobody"]), RunOptions.Default));
        Assert.Throws<RunSnapshotMismatchException>(() => new RunSession(defs, Mods(false), Snap(null, []) with { Format = 1 }, RunOptions.Default));   // 옛 판 1
    }

    // ── 새 계약 멤버 ───────────────────────────────────────────────

    [Fact]
    public void NewMembers_V09()
    {
        Assert.Equal(1 << 10, (int)CommandButtons.Drink);
        foreach (var name in new[] { "PotionDrink", "PotionDenied", "HealOnEnter", "ShopOpen", "ShopBuy", "ShopDenied", "ChestDrop" })
            Assert.True(Enum.TryParse<CueKind>(name, out _), name);
        Assert.False(Enum.TryParse<CueKind>("BeltPotion", out _));
        Assert.Equal(["Evolution", "WeaponUpgrade", "PassiveUpgrade", "Temper"], Enum.GetNames<ChestItemKind>());
        Assert.Equal(5, typeof(ChestItem).GetConstructors().Single().GetParameters().Length);   // Gold 인자 없앰
        Assert.Null(new LevelUpOffer(1, [], 0).Reward);
        Assert.NotNull(typeof(Enemy).GetField(nameof(Enemy.InvulnUntilTick)));
        Assert.Equal(DamageCause.None, new DamageRequest(EntityId.Hero, EntityId.None, Faction.Enemy, 1, 0, 0, -1, default, 0, DamageFlags.None).Cause);
        Assert.True(new NullabilityInfoContext().Create(typeof(INavDebugView).GetProperty(nameof(INavDebugView.Arena))!).ReadState == NullabilityState.Nullable);
        Assert.NotNull(typeof(IShop).GetMethod(nameof(IShop.ReplaceRing)));
        var hero = new Hero { Potions = 2, Gold = 50, Heal = HealOverTime.Start(40f, 180) };
        hero.Equipment[0] = new EquipItem("x", null, null, 1);
        hero.Reset();
        Assert.Equal((0, 0, false), (hero.Potions, hero.Gold, hero.Heal.Active));
        Assert.Null(hero.Equipment[0]);
        Assert.Equal(EquipSlots.Count, hero.Equipment.Length);
    }

    [Fact]
    public void HealOverTime_EvenPerTick_LastTickRemainder_SumExact()
    {
        var h = HealOverTime.Start(40f, 180);
        var sum = 0f;
        var ticks = 0;
        while (h.Active)
        {
            (var amount, h) = h.Step();
            Assert.InRange(amount, 40f / 180f - 1e-4f, 40f / 180f + 1e-4f);
            sum += amount;
            ticks++;
        }
        Assert.Equal(180, ticks);
        Assert.Equal(40f, sum, 3);
        Assert.Equal((0f, default(HealOverTime)), h.Step());
    }
}
