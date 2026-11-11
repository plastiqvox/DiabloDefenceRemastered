using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Json;
using DiabloDefenceRemastered.Core.Data.Loading;

namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>시험용 부품 매개변수(로더 시험 전용 — 실제 부품은 WP3·WP4).</summary>
public sealed record TestChaseParams { public float Cooldown { get; init; } }
public sealed record TestSummonParams { public required DefRef<EnemyDef> Enemy { get; init; } public int Count { get; init; } = 1; }
public sealed record TestBossEventParams { public required DefRef<EnemyDef> Enemy { get; init; } }
public sealed record TestSpreadParams { public int Count { get; init; } public float Arc { get; init; } }

/// <summary>
/// 로더 시험의 작은 데이터 한 벌(WP7). 실제 <c>Data/</c> 의 그림·시야 규칙·L1 타일셋·대성당 맵(방 종류를 단 사본)에 합성 직업·무기·적·보상·규칙·액트를 더한다.
/// 기본 내용은 오류 0(<see cref="LoaderTests.Base_HasNoErrors"/>). 시험은 <see cref="Files"/> 의 JsonObject 를 고쳐 고장을 만든다.
/// </summary>
public sealed class LoaderFixture
{
    public Dictionary<string, JsonObject> Files { get; } = new(StringComparer.Ordinal);

    public LoaderFixture()
    {
        foreach (var rel in new[] { "sprites/hero.json", "sprites/monsters.json", "rules/sight.json", "tilesets/l1.json", "maps/cathedral_arena.json" })
            Files[rel] = (JsonObject)DefJson.ParseNode(File.ReadAllText(Path.Combine(RealData.Dir, rel)))!;
        var map = (JsonObject)Files["maps/cathedral_arena.json"]["maps"]![0]!;
        map["room_kinds"] = new JsonArray("normal", "elite", "boss");

        Files["heroes/sorcerer.json"] = Obj("""
            { "hero_classes": [ { "id": "sorcerer", "name": "마법사", "sprites": "set_sorcerer", "radius": 14, "start_weapons": ["firebolt"] } ] }
            """);
        Files["weapons/firebolt.json"] = Obj("""
            { "weapons": [ { "id": "firebolt", "name": "화염 화살", "cast_clip": "cast_fire", "max_level": 8 } ] }
            """);
        Files["enemies/skeleton.json"] = Obj("""
            { "enemies": [
                { "id": "skeleton", "name": "해골", "sprites": "set_skelaxe", "variant": "white", "hp": 10, "speed": 70, "damage": 5, "xp": 1,
                  "behaviors": [ { "id": "melee", "type": "chase_melee", "cooldown": 1.0 } ] } ] }
            """);
        Files["pickups.json"] = Obj("""
            { "pickups": [
                { "id": "pk_tome", "kind": "reward", "draw": { "procedural": "gem" } },
                { "id": "pk_chest", "kind": "reward", "draw": { "procedural": "gem" } } ] }
            """);
        Files["rewards.json"] = Obj("""
            { "rewards": [
                { "id": "rw_tome", "kind": "weapon_cards", "object": "pk_tome", "cards": { "owned_weapon": 1, "new_weapon": 1 } },
                { "id": "rw_chest", "kind": "weapon_bundle", "object": "pk_chest", "bundle": { "weapon_levels": 1 } } ] }
            """);
        Files["rules/hero.json"] = Obj("""
            { "rules": { "hero": { "class": "sorcerer" }, "level_up": { "cards": 3 } } }
            """);
        Files["rules/rooms.json"] = Obj("""
            { "rules": { "rooms": {
                "grace": 2, "cleanup_limit": 30, "elite_spawn_cap": "02:30", "transition": 0.6, "transition_floor": 1.2,
                "gates": { "entry": ["gate_nw", "gate_ne", "gate_se", "gate_sw"], "boss_entry": "gate_se", "exits": ["gate_nw", "gate_ne"], "single_exit": "gate_ne" },
                "exits": { "three_chance": 0.25 },
                "area_scale": { "reference": "cathedral_arena", "min": 0.5, "max": 1.0 },
                "reward_spot": { "distance": 100, "spacing": 60, "clearance": 20 },
                "fallback_reward": "rw_chest",
                "stairs": { "weapon_slots": 1, "passive_slots": 1 },
                "autoplay_door_skip": 20,
                "rewards": { "heal_skip_at_least": 0.75, "heal_guarantee_below": 0.40, "heal_low_hp": 0.50, "heal_low_hp_weight": 30, "heal_amount": 0.5,
                             "merchant_first_room": 3, "merchant_per_act": 2, "merchant_guarantee_room": [4, 5], "merchant_met_other": 1.5,
                             "healer_boost": 1.5, "healer_boost_below": 0.50 } } } }
            """);
        Files["stages/stage_test.json"] = Obj("""
            { "stages": [ {
                "id": "stage_test", "name": "시험 액트", "slice": true,
                "rooms": [ { "kind": "normal", "from": "00:00", "to": "01:00" } ],
                "room_maps": { "normal": ["cathedral_arena"] },
                "rewards": { "weights": { "rw_tome": 50, "rw_chest": 50 }, "first_room": "rw_tome" },
                "lighting": { "hero_light": { "radius": 420 } },
                "spawn_intervals": [ { "from": "00:00", "to": "01:00", "max_alive": 20, "per_second": 1, "weights": { "skeleton": 100 } } ] } ] }
            """);
    }

    public static JsonObject Obj(string json) => (JsonObject)DefJson.ParseNode(json)!;

    /// <summary>파일 하나의 종류 배열 첫 원소(가장 흔한 고치기 자리).</summary>
    public JsonObject First(string rel, string kind) => (JsonObject)Files[rel][kind]![0]!;

    public JsonObject Stage => First("stages/stage_test.json", "stages");

    public IEnumerable<DataFile> DataFiles => Files.Select(kv => new DataFile(kv.Key, kv.Value.ToJsonString()));

    public static PartRegistry Registry()
    {
        var r = new PartRegistry();
        r.Register<TestChaseParams>(PartKind.EnemyBehavior, "chase_melee", p => p,
            (p, ctx) => { if (p.Cooldown < 0) ctx.Error("cooldown", "음수다"); });
        r.Register<TestSummonParams>(PartKind.EnemyAction, "summon", p => p);
        r.Register<TestSpreadParams>(PartKind.EnemyAction, "spread", p => p);
        r.Register<TestBossEventParams>(PartKind.StageEvent, "boss", p => p);
        r.Register<TestBossEventParams>(PartKind.StageEvent, "miniboss", p => p);
        r.Register<LightChangeParams>(PartKind.StageEvent, "light_change", p => p);
        return r;
    }

    public DataLoadResult Load(DataLoadOptions? options = null, IReadOnlyList<IDataValidator>? validators = null) =>
        DataLoader.Load(DataFiles, Registry(), validators ?? [], options);

    /// <summary>오류(경고 제외)만.</summary>
    public static List<DataError> ErrorsOf(DataLoadResult r) => [.. r.Errors.Where(e => e.Severity == DataSeverity.Error)];
}
