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
          "rewards": { "weights": { "reward_tome": 30, "reward_chest": 30, "reward_shrine": 25, "reward_potion": 15 },
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
        Assert.Equal(["reward_tome", "reward_chest", "reward_shrine", "reward_potion"], s.Rewards.Weights.Select(w => w.Ref.Id));   // JSON 순서 배열(3.4)
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

    private const string RewardsJson = """
        [ { "id": "reward_tome",   "name": "마법서",         "kind": "weapon_cards",   "object": "obj_tome",   "icon": { "sprite": "obj_book_icon" },
            "cards": { "owned_weapon": 100, "new_weapon": 100 }, "skip": { "gold": 25 } },
          { "id": "reward_chest",  "name": "보물 상자",      "kind": "level_up",       "object": "obj_chest",  "icon": { "sprite": "obj_chest_icon" }, "gold": [30, 60] },
          { "id": "reward_shrine", "name": "성소",           "kind": "blessing_cards", "object": "obj_shrine", "icon": { "sprite": "obj_shrine_icon" }, "skip": { "gold": 25 } },
          { "id": "reward_potion", "name": "완전 회복 물약", "kind": "full_heal",      "object": "obj_potion", "icon": { "sprite": "obj_potion_icon" }, "skip_when_hp_at_least": 0.7 } ]
        """;

    [Fact]
    public void Rewards_ReadFourKinds()
    {
        var r = TestJson.Read<RewardDef[]>(RewardsJson);
        Assert.Equal([RewardKind.WeaponCards, RewardKind.LevelUp, RewardKind.BlessingCards, RewardKind.FullHeal], r.Select(x => x.Kind));
        Assert.Equal(100f, r[0].Cards!.NewWeapon);
        Assert.Equal(25, r[0].Skip!.Gold);
        Assert.Equal([30, 60], r[1].Gold!);
        Assert.Equal("obj_shrine", r[2].Object.Id);
        Assert.Equal(0.7f, r[3].SkipWhenHpAtLeast);
        Assert.Equal("obj_potion_icon", r[3].Icon!.Sprite.Id);
        Assert.Throws<JsonException>(() => TestJson.Read<RewardDef>("""{ "id": "x", "kind": "gold_pile", "object": "o" }"""));
        Assert.Throws<JsonException>(() => TestJson.Read<RewardDef>("""{ "id": "x", "object": "o" }"""));   // kind 필수
    }

    private const string RoomRulesJson = """
        { "grace": 3, "cleanup_limit": 45, "remaining_marker": 10, "elite_spawn_cap": "02:30", "transition": 0.5,
          "gates": { "entry": ["gate_sw", "gate_se"], "boss_entry": "gate_sw", "exits": ["gate_nw", "gate_ne"], "single_exit": "gate_ne" },
          "exits": { "three_chance": 0.4 },
          "area_scale": { "reference": "cathedral_arena", "min": 0.6, "max": 1.0 },
          "reward_anchor": "center", "chest_ring": 48, "fallback_reward": "reward_chest",
          "autoplay_door_skip": 20 }
        """;

    [Fact]
    public void RoomRules_ReadAllKeys()
    {
        var r = TestJson.Read<RulesDef>("""{ "rooms": """ + RoomRulesJson + " }").Rooms!;
        Assert.Equal(3f, r.Grace);
        Assert.Equal(45f, r.CleanupLimit);
        Assert.Equal(10, r.RemainingMarker);
        Assert.Equal(150f, r.EliteSpawnCap.Seconds);
        Assert.Equal(["gate_sw", "gate_se"], r.Gates.Entry);
        Assert.Equal("gate_ne", r.Gates.SingleExit);
        Assert.Equal(0.4f, r.Exits.ThreeChance);
        Assert.Equal("cathedral_arena", r.AreaScale.Reference.Id);
        Assert.Equal(0.6f, r.AreaScale.Min);
        Assert.Equal("center", r.RewardAnchor);
        Assert.Equal(48f, r.ChestRing);
        Assert.Equal("reward_chest", r.FallbackReward.Id);
        Assert.Equal(20f, r.AutoplayDoorSkip);
    }

    [Theory]
    [InlineData("\"grace\": 3, ")]
    [InlineData("\"fallback_reward\": \"reward_chest\",")]
    [InlineData("\"single_exit\": \"gate_ne\" ")]
    [InlineData("\"three_chance\": 0.4 ")]
    public void RoomRules_MissingKey_Throws(string key)
    {
        var json = RoomRulesJson.Replace(key, "");
        Assert.NotEqual(RoomRulesJson, json);
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
