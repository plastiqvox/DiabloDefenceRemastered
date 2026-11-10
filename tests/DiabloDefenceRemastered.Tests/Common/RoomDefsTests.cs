using System.Text.Json;

namespace DiabloDefenceRemastered.Tests.Common;

/// <summary>v0.7 방 구조 정의 모양(WP1e-형 — 6.10 <c>rules.rooms</c>·6.11 <c>room_kinds</c>·6.12 액트·6.18 문 보상).</summary>
public class RoomDefsTests
{
    // 6.12 예에서 이벤트·생성 구간을 뺀 것(부품·구간 변환기는 로더 WP7)
    private const string ActJson = """
        { "id": "stage_cathedral", "name": "대성당", "next_act": "stage_catacombs",
          "room_maps": { "normal": ["cathedral_room_a", "cathedral_room_b"], "elite": ["cathedral_room_a", "cathedral_room_b"], "boss": "cathedral_arena" },
          "rooms": [
            { "kind": "normal", "from": "00:00", "to": "01:00" },
            { "kind": "normal", "from": "01:00", "to": "02:15" },
            { "kind": "elite",  "from": "05:00", "to": "06:00", "miniboss": "rotfeast" },
            { "kind": "normal", "from": "06:00", "to": "07:30", "number": 6 },
            { "kind": "boss",   "from": "10:00", "boss": "skeleton_king" } ],
          "rewards": { "weights": { "reward_tome": 30, "reward_chest": 30, "shrine_enchanted": 25, "reward_potion": 15 },
                       "first_room": "reward_tome", "gold_scale": 1.0 },
          "lighting": { "ambient": [200, 200, 215], "darkness": 0.5, "hero_light": { "color": [255, 240, 220], "radius": 420 }, "unit_min_light": 0.6, "lights": [] },
          "scaling": { "hp": [1.0, 0.08], "damage": [1.0, 0.03] },
          "elite_chance": { "from": "02:00", "at": "00:30", "chance": 0.25 },
          "slice": true }
        """;

    [Fact]
    public void Act_ReadsRoomsPoolsRewardsNextAct()
    {
        var s = TestJson.Read<StageDef>(ActJson);
        Assert.Equal(5, s.Rooms.Count);
        Assert.Equal([RoomKind.Normal, RoomKind.Normal, RoomKind.Elite, RoomKind.Normal, RoomKind.Boss], s.Rooms.Select(r => r.Kind));
        Assert.Equal(60f, s.Rooms[0].To!.Value.Seconds);
        Assert.Equal(135f, s.Rooms[1].To!.Value.Seconds);
        Assert.Equal("rotfeast", s.Rooms[2].Miniboss!.Id);
        Assert.Null(s.Rooms[4].To);
        Assert.Equal("skeleton_king", s.Rooms[4].Boss!.Id);
        Assert.Equal(0, s.Rooms[0].Number);              // 적지 않음 = 0 → 로더가 배열 자리 + 1
        Assert.Equal(6, s.Rooms[3].Number);
        Assert.Equal(["cathedral_room_a", "cathedral_room_b"], s.RoomMaps.Normal.Select(m => m.Id));
        Assert.Equal("cathedral_arena", s.RoomMaps.Boss!.Id);
        Assert.Equal(["reward_tome", "reward_chest", "shrine_enchanted", "reward_potion"], s.Rewards.Weights.Select(w => w.Ref.Id));   // JSON 순서 배열(3.4)
        Assert.Equal([30f, 30f, 25f, 15f], s.Rewards.Weights.Select(w => w.Weight));
        Assert.Equal("reward_tome", s.Rewards.FirstRoom!.Id);
        Assert.Equal(1f, s.Rewards.GoldScale);
        Assert.Equal("stage_catacombs", s.NextAct!.Id);
        Assert.True(s.Slice);
        Assert.Equal(30f, s.EliteChance!.At.Seconds);
        Assert.Equal(120f, s.EliteChance.From.Seconds);
    }

    [Fact]
    public void EliteChance_AtDefaults30s()
    {
        var s = TestJson.Read<StageDef>("""{ "id": "a", "elite_chance": { "from": "02:00", "chance": 0.25 } }""");
        Assert.Equal(30f, s.EliteChance!.At.Seconds);
        Assert.False(TestJson.Read<StageDef>("""{ "id": "a" }""").Slice);
    }

    [Theory]
    [InlineData("""{ "id": "a", "map": "cathedral_arena" }""")]
    [InlineData("""{ "id": "a", "unlock": "stage_cathedral" }""")]
    [InlineData("""{ "id": "a", "victory": { "type": "boss" } }""")]
    [InlineData("""{ "id": "a", "elite_chance": { "every": 30, "from": "02:00", "chance": 0.25 } }""")]
    [InlineData("""{ "id": "a", "rooms": [ { "kind": "treasure", "from": "00:00" } ] }""")]
    public void OldActShape_UnknownPropertyOrKind_Throws(string json)
    {
        Assert.Throws<JsonException>(() => TestJson.Read<StageDef>(json));
    }

    // v0.8(WP1f) → v0.9(WP1g) — 6.18·6.19 보상 다섯 종류. v0.7 의 level_up·full_heal·blessing_cards·skip.gold·gold·skip_when_hp_at_least, v0.8 의 belt_potions 는 오류
    private const string RewardsJson = """
        [ { "id": "reward_tome",   "name": "마법서",         "kind": "weapon_cards",  "object": "obj_tome",   "icon": { "sprite": "obj_book_icon" },
            "cards": { "owned_weapon": 100, "new_weapon": 100 }, "skip": { "heal": 10 } },
          { "id": "reward_chest",  "name": "보물 상자",      "kind": "weapon_bundle", "object": "obj_chest",  "icon": { "sprite": "obj_chest_icon" },
            "bundle": { "weapon_levels": 1, "potions": 1, "gold": [30, 60] }, "drop": "pk_book" },
          { "id": "reward_potion", "name": "회복", "kind": "heal_next_room", "icon": { "sprite": "obj_potion_icon" } },
          { "id": "shrine_enchanted",   "name": "마법에 걸린 성소", "kind": "shrine", "object": "obj_shrine",
            "shrine": { "all_weapons": 1, "random_weapon": -1 } },
          { "id": "shrine_fascinating", "name": "매혹의 성소",     "kind": "shrine", "object": "obj_shrine_red",
            "shrine": { "pick_weapon": 2, "pick_max_level": 7, "stats": { "max_hp_pct": -0.10 } } },
          { "id": "shrine_gloomy",      "name": "음산한 성소",     "kind": "shrine", "object": "obj_shrine_dark",
            "shrine": { "stats": { "armor": 2, "damage_pct": -0.05 } } },
          { "id": "reward_smith", "name": "그리스월드", "kind": "merchant", "merchant": "smith" } ]
        """;

    [Fact]
    public void Rewards_ReadV09Kinds()
    {
        var r = TestJson.Read<RewardDef[]>(RewardsJson);
        Assert.Equal([RewardKind.WeaponCards, RewardKind.WeaponBundle, RewardKind.HealNextRoom, RewardKind.Shrine, RewardKind.Shrine, RewardKind.Shrine, RewardKind.Merchant],
                     r.Select(x => x.Kind));
        Assert.Equal(100f, r[0].Cards!.NewWeapon);
        Assert.Equal(10f, r[0].Skip!.Heal);
        Assert.Equal(1, r[1].Bundle!.WeaponLevels);
        Assert.Equal(1, r[1].Bundle!.Potions);
        Assert.Equal("pk_book", r[1].Drop!.Id);
        Assert.Equal([30, 60], r[1].Bundle!.Gold!);
        Assert.Null(r[2].Object);   // 회복 문 — 방 안 물체 없음(v0.9)
        Assert.Equal("smith", r[6].Merchant!.Id);
        Assert.Null(r[6].Object);
        Assert.Equal("obj_potion_icon", r[2].Icon!.Sprite.Id);
        Assert.Equal((1, -1), (r[3].Shrine!.AllWeapons, r[3].Shrine!.RandomWeapon));
        Assert.Equal((2, 7), (r[4].Shrine!.PickWeapon, r[4].Shrine!.PickMaxLevel));
        Assert.Equal(-0.10f, r[4].Shrine!.Stats[StatKey.MaxHpPct]);
        Assert.Equal(2f, r[5].Shrine!.Stats[StatKey.Armor]);
        Assert.Equal(-0.05f, r[5].Shrine!.Stats[StatKey.DamagePct]);
        Assert.Equal(1, new RewardBundleDef().WeaponLevels);   // 기본 1
        Assert.Throws<JsonException>(() => TestJson.Read<RewardDef>("""{ "id": "x", "kind": "gold_pile", "object": "o" }"""));
        Assert.Throws<JsonException>(() => TestJson.Read<RewardDef>("""{ "id": "x", "object": "o" }"""));   // kind 필수
    }

    [Theory]
    [InlineData("""{ "id": "x", "kind": "level_up", "object": "o" }""")]                                  // 모르는 값
    [InlineData("""{ "id": "x", "kind": "full_heal", "object": "o" }""")]
    [InlineData("""{ "id": "x", "kind": "blessing_cards", "object": "o" }""")]
    [InlineData("""{ "id": "x", "kind": "weapon_cards", "object": "o", "skip": { "gold": 25 } }""")]       // 모르는 속성
    [InlineData("""{ "id": "x", "kind": "weapon_bundle", "object": "o", "gold": [30, 60] }""")]
    [InlineData("""{ "id": "x", "kind": "weapon_cards", "object": "o", "skip_when_hp_at_least": 0.7 }""")]
    [InlineData("""{ "id": "x", "kind": "belt_potions", "object": "o" }""")]                              // v0.9: 모르는 값
    [InlineData("""{ "id": "x", "kind": "heal_next_room", "belt_potions": 2 }""")]                         // v0.9: 모르는 속성
    [InlineData("""{ "id": "x", "kind": "weapon_bundle", "object": "o", "bundle": { "belt_potions": 1 } }""")]
    public void Rewards_OldV07Shape_Throws(string json)
    {
        Assert.Throws<JsonException>(() => TestJson.Read<RewardDef>(json));
    }

    // v0.8(WP1f) — 6.10 rules.rooms 새 모양: remaining_marker·reward_anchor·chest_ring 없앰, reward_spot·stairs·transition_floor 더함
    private const string RoomRulesJson = """
        { "grace": 3, "cleanup_limit": 25, "elite_spawn_cap": "02:30", "transition": 0.5, "transition_floor": 1.0,
          "gates": { "entry": ["gate_sw", "gate_se"], "boss_entry": "gate_sw", "exits": ["gate_nw", "gate_ne"], "single_exit": "gate_ne" },
          "exits": { "three_chance": 0.4 },
          "area_scale": { "reference": "cathedral_arena", "min": 0.6, "max": 1.0 },
          "reward_spot": { "distance": 100, "spacing": 40, "clearance": 30 }, "fallback_reward": "reward_chest",
          "stairs": { "weapon_slots": 1, "passive_slots": 1 },
          "autoplay_door_skip": 20,
          "rewards": { "heal_skip_at_least": 0.75, "heal_guarantee_below": 0.40, "heal_low_hp": 0.50, "heal_low_hp_weight": 30,
                       "merchant_first_room": 3, "merchant_per_act": 2, "merchant_guarantee_room": [4, 5], "merchant_met_other": 1.5,
                       "healer_boost": 1.5, "healer_boost_below": 0.50 } }
        """;

    [Fact]
    public void RoomRules_ReadAllKeys()
    {
        var r = TestJson.Read<RulesDef>("""{ "rooms": """ + RoomRulesJson + " }").Rooms!;
        Assert.Equal(3f, r.Grace);
        Assert.Equal(25f, r.CleanupLimit);
        Assert.Equal(150f, r.EliteSpawnCap.Seconds);
        Assert.Equal(0.5f, r.Transition);
        Assert.Equal(1.0f, r.TransitionFloor);
        Assert.Equal(["gate_sw", "gate_se"], r.Gates.Entry);
        Assert.Equal("gate_ne", r.Gates.SingleExit);
        Assert.Equal(0.4f, r.Exits.ThreeChance);
        Assert.Equal("cathedral_arena", r.AreaScale.Reference.Id);
        Assert.Equal(0.6f, r.AreaScale.Min);
        Assert.Equal((100f, 40f, 30f), (r.RewardSpot.Distance, r.RewardSpot.Spacing, r.RewardSpot.Clearance));
        Assert.Equal((1, 1), (r.Stairs.WeaponSlots, r.Stairs.PassiveSlots));
        Assert.Equal("reward_chest", r.FallbackReward.Id);
        Assert.Equal(20f, r.AutoplayDoorSkip);
        Assert.Equal((0.75f, 0.40f, 30f), (r.Rewards.HealSkipAtLeast, r.Rewards.HealGuaranteeBelow, r.Rewards.HealLowHpWeight));   // v0.9
        Assert.Equal([4, 5], r.Rewards.MerchantGuaranteeRoom);
        Assert.Equal((3, 2, 1.5f), (r.Rewards.MerchantFirstRoom, r.Rewards.MerchantPerAct, r.Rewards.HealerBoost));
    }

    [Theory]
    [InlineData("\"grace\": 3, ")]
    [InlineData("\"fallback_reward\": \"reward_chest\",")]
    [InlineData("\"single_exit\": \"gate_ne\" ")]
    [InlineData("\"three_chance\": 0.4 ")]
    [InlineData("\"reward_spot\": { \"distance\": 100, \"spacing\": 40, \"clearance\": 30 }, ")]
    [InlineData("\"stairs\": { \"weapon_slots\": 1, \"passive_slots\": 1 },")]
    [InlineData(", \"transition_floor\": 1.0")]
    [InlineData("\"clearance\": 30")]
    [InlineData("\"heal_skip_at_least\": 0.75, ")]   // v0.9 rules.rooms.rewards — 모두 필수
    [InlineData("\"merchant_guarantee_room\": [4, 5], ")]
    public void RoomRules_MissingKey_Throws(string key)
    {
        var json = RoomRulesJson.Replace(key, "");
        Assert.NotEqual(RoomRulesJson, json);
        Assert.Throws<JsonException>(() => TestJson.Read<RoomRulesDef>(json));
    }

    [Theory]
    [InlineData("\"remaining_marker\": 10, ")]
    [InlineData("\"reward_anchor\": \"center\", ")]
    [InlineData("\"chest_ring\": 48, ")]
    public void RoomRules_RemovedV07Key_Throws(string old)
    {
        var json = RoomRulesJson.Replace("\"grace\": 3, ", "\"grace\": 3, " + old);
        Assert.Throws<JsonException>(() => TestJson.Read<RoomRulesDef>(json));
    }

    [Fact]
    public void MapRoomKinds_PassiveBlessing_PickupReward_RewardSounds()
    {
        var map = TestJson.Read<MapDef>("""
            { "id": "m", "tileset": "l1", "size": 16, "layout": { "border": "wall" }, "hero_start": { "tile": [15.5, 15.5] },
              "room_kinds": ["normal", "elite"] }
            """);
        Assert.Equal([RoomKind.Normal, RoomKind.Elite], map.RoomKinds);
        Assert.Empty(TestJson.Read<MapDef>("""{ "id": "m", "tileset": "l1", "layout": { "border": "wall" }, "hero_start": { "tile": [1, 1] } }""").RoomKinds);

        Assert.True(TestJson.Read<PassiveDef>("""{ "id": "bless_divine", "max_level": 3, "blessing": true }""").Blessing);
        Assert.False(TestJson.Read<PassiveDef>("""{ "id": "p" }""").Blessing);
        Assert.Equal(PickupKind.Reward, TestJson.Read<PickupDef>("""{ "id": "obj_tome", "kind": "reward", "draw": { "procedural": "gem" } }""").Kind);

        var sb = TestJson.Read<SoundBindingsDef>("""{ "rewards": { "reward_tome": { "collect": "snd_book", "open": "snd_page" } } }""");
        Assert.Equal("snd_book", sb.Rewards["reward_tome"].Collect!.Id);
        Assert.Equal("snd_page", sb.Rewards["reward_tome"].Open!.Id);
    }

    [Fact]
    public void LevelUpOffer_OldConstruction_DefaultsToLevelUpSource()
    {
        var offer = new LevelUpOffer(2, [], 3);
        Assert.Equal(OfferSource.LevelUp, offer.Source);
        Assert.Equal(OfferSource.Shrine, (offer with { Source = OfferSource.Shrine }).Source);
    }

    [Fact]
    public void GameDefs_KindsHasRewards_RunStatsHasNoReset()
    {
        Assert.Contains("rewards", GameDefs.Kinds);
        Assert.Null(typeof(RunStats).GetMethod("Reset"));   // v0.7.1 — 방마다 조용히 0 이 되는 길을 없앰
    }
}
