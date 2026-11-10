using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Loading;

namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>로딩 6단계 공통 의미 검사(6.14 의 Data 줄 — WP7): 고장 난 작은 JSON 으로 오류가 하나씩.</summary>
public sealed class DataRulesTests
{
    private static string Dump(IEnumerable<DataError> errors) => string.Join("\n", errors.Select(e => e.ToString()));

    private static DataError One(LoaderFixture f, string contains)
    {
        var r = f.Load();
        var hits = LoaderFixture.ErrorsOf(r).Where(e => e.Message.Contains(contains, StringComparison.Ordinal)).ToList();
        Assert.True(hits.Count == 1, $"'{contains}' 오류가 {hits.Count}개다:\n{Dump(r.Errors)}");
        return hits[0];
    }

    private static void NoErrors(LoaderFixture f)
    {
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
    }

    /// <summary>slice 가 아닌 액트 1 모양: 일반 6(0:00~6:00) + 정예 1 + 일반 1 + 보스. 보스·중간 보스 적과 이벤트·풀까지.</summary>
    private static LoaderFixture FullAct()
    {
        var f = new LoaderFixture();
        var enemies = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        enemies.Add(LoaderFixture.Obj("""{ "id": "mini", "sprites": "set_skelbow", "rank": "miniboss", "hp": 300, "scale": 1.3, "radius": 16 }"""));
        enemies.Add(LoaderFixture.Obj("""{ "id": "king", "sprites": "set_skelaxe", "rank": "boss", "hp": 2000, "scale": 1.5, "radius": 18 }"""));
        var s = f.Stage;
        s["slice"] = false;
        var rooms = new JsonArray();
        for (var i = 0; i < 6; i++) rooms.Add(LoaderFixture.Obj($$"""{ "kind": "normal", "from": "0{{i}}:00", "to": "0{{i + 1}}:00" }"""));
        rooms.Add(LoaderFixture.Obj("""{ "kind": "elite", "from": "06:00", "to": "07:00", "miniboss": "mini" }"""));
        rooms.Add(LoaderFixture.Obj("""{ "kind": "normal", "from": "07:00", "to": "08:00" }"""));
        rooms.Add(LoaderFixture.Obj("""{ "kind": "boss", "from": "08:00", "boss": "king" }"""));
        s["rooms"] = rooms;
        s["room_maps"] = LoaderFixture.Obj("""{ "normal": ["cathedral_arena"], "elite": ["cathedral_arena"], "boss": "cathedral_arena" }""");
        s["spawn_intervals"] = new JsonArray(
            LoaderFixture.Obj("""{ "from": "00:00", "to": "04:00", "max_alive": 20, "per_second": 1, "weights": { "skeleton": 100 } }"""),
            LoaderFixture.Obj("""{ "from": "04:00", "to": "08:00", "max_alive": 30, "per_second": 1, "weights": { "skeleton": 100 } }"""),
            LoaderFixture.Obj("""{ "during": "boss", "max_alive": 10, "per_second": 0.5, "weights": { "skeleton": 100 } }"""));
        s["events"] = new JsonArray(
            LoaderFixture.Obj("""{ "at": "06:00", "type": "miniboss", "enemy": "mini" }"""),
            LoaderFixture.Obj("""{ "at": "08:00", "type": "boss", "enemy": "king" }"""));
        return f;
    }

    private static JsonArray Rooms(LoaderFixture f) => (JsonArray)f.Stage["rooms"]!;

    [Fact]
    public void FullAct_Passes() => NoErrors(FullAct());

    // ── 13.5-23 ① ──

    [Fact]
    public void Rooms_FiveNormals_IsError()
    {
        var f = FullAct();
        // 일곱 중 둘을 빼 다섯(t 범위가 깨지는 것은 다른 오류로 같이 나온다)
        Rooms(f).RemoveAt(0);
        Rooms(f).RemoveAt(0);
        var r = f.Load();
        Assert.Contains(LoaderFixture.ErrorsOf(r), e => e.Message.Contains("일반 방이 5개다", StringComparison.Ordinal));
    }

    [Fact]
    public void Rooms_BossInMiddle_IsError()
    {
        var f = FullAct();
        var boss = Rooms(f)[8]!.DeepClone();
        Rooms(f).Insert(3, boss);
        var e = One(f, "보스 방은 마지막 하나뿐");
        Assert.Equal("$.stages[0].rooms[3].kind", e.JsonPath);
    }

    [Fact]
    public void Rooms_Gap_IsError_ButSliceAllowsGapAndNoBoss()
    {
        var f = FullAct();
        Rooms(f)[2]!["from"] = "02:10";
        Rooms(f)[2]!["to"] = "03:00";
        var e = One(f, "빈틈");
        Assert.Equal("$.stages[0].rooms[2].from", e.JsonPath);

        var g = new LoaderFixture();
        Rooms(g).Add(LoaderFixture.Obj("""{ "kind": "normal", "from": "01:30", "to": "02:30" }"""));
        g.Stage["spawn_intervals"]![0]!["to"] = "02:30";
        NoErrors(g);   // slice: 빈틈·보스 방 없음·일반 방 수가 통과
    }

    [Fact]
    public void Rooms_EliteWithoutMiniboss_IsError()
    {
        var f = FullAct();
        ((JsonObject)Rooms(f)[6]!).Remove("miniboss");
        One(f, "정예 방에 miniboss 가 없다");
    }

    [Fact]
    public void Rooms_MinibossRankWrong_AndBossMismatchEvent()
    {
        var f = FullAct();
        Rooms(f)[6]!["miniboss"] = "skeleton";
        One(f, "rank: miniboss 가 아니다");

        var g = FullAct();
        g.Stage["events"]![1]!["enemy"] = "mini";
        One(g, "boss 이벤트가 없다");
    }

    [Fact]
    public void Rooms_NumberRules()
    {
        var f = FullAct();
        Rooms(f)[1]!["number"] = 5;
        One(f, "배열 자리 + 1");
    }

    [Fact]
    public void Rooms_FloorRules()
    {
        var f = FullAct();
        Rooms(f)[0]!["floor"] = 1;
        One(f, "floor 는 모든 방에");
    }

    [Fact]
    public void Rooms_NormalLength_OutOfRange()
    {
        var f = new LoaderFixture();
        Rooms(f)[0]!["to"] = "02:30";
        f.Stage["spawn_intervals"]![0]!["to"] = "02:30";
        One(f, "1:00~2:00 밖");
    }

    // ── 13.5-23 ② ──

    [Fact]
    public void Pool_MissingKind_IsError()
    {
        var f = FullAct();
        ((JsonObject)f.Stage["room_maps"]!).Remove("elite");
        One(f, "elite 방이 있는데 맵 풀이 비었다");
    }

    [Fact]
    public void Pool_RoomKindsMismatch_IsError()
    {
        var f = FullAct();
        f.First("maps/cathedral_arena.json", "maps")["room_kinds"] = new JsonArray("normal", "boss");
        var e = One(f, "room_kinds 에 elite 이 없다");
        Assert.Equal("$.stages[0].room_maps.elite[0]", e.JsonPath);
    }

    // ── 13.5-23 ④ ──

    [Fact]
    public void Rewards_OneWeighted_IsError()
    {
        var f = new LoaderFixture();
        f.Stage["rewards"]!["weights"] = LoaderFixture.Obj("""{ "rw_tome": 50, "rw_chest": 0 }""");
        One(f, "둘 이상");
    }

    [Fact]
    public void Rewards_NoFirstRoom_IsError()
    {
        var f = new LoaderFixture();
        ((JsonObject)f.Stage["rewards"]!).Remove("first_room");
        One(f, "first_room 이 없다");
    }

    [Fact]
    public void Rewards_ObjectKindNotReward_AndFallbackMissing()
    {
        var f = new LoaderFixture();
        f.First("pickups.json", "pickups")["kind"] = "gold";
        One(f, "kind 가 reward 가 아니다");

        var g = new LoaderFixture();
        g.Files["rules/rooms.json"]["rules"]!["rooms"]!["fallback_reward"] = "rw_chst";
        var e = One(g, "'rw_chst' 가 없다");
        Assert.Equal("rules/rooms.json", e.File);
        Assert.Equal("$.rules.rooms.fallback_reward", e.JsonPath);
    }

    [Fact]
    public void Passive_BlessingAndTrait_IsError()
    {
        var f = new LoaderFixture();
        f.Files["passives.json"] = LoaderFixture.Obj("""{ "passives": [ { "id": "bl", "max_level": 3, "blessing": true, "trait": true } ] }""");
        One(f, "trait 이기도 하다");
    }

    [Fact]
    public void Rules_GateNameOutside_AndTemperMissing()
    {
        var f = new LoaderFixture();
        f.Files["rules/rooms.json"]["rules"]!["rooms"]!["gates"]!["single_exit"] = "gate_n";
        One(f, "문 이름 'gate_n'");

        var g = new LoaderFixture();
        g.Files["rules/hero.json"]["rules"]!["level_up"]!["temper"] = "temper";
        One(g, "temper 'temper' 가 fillers 에 없다");
    }

    [Fact]
    public void Stage_WithoutRoomsOrSightRules_IsError()
    {
        var f = new LoaderFixture();
        f.Files.Remove("rules/rooms.json");
        var r = f.Load();
        Assert.Contains(LoaderFixture.ErrorsOf(r), e => e.Message.Contains("rules.rooms 가 없다", StringComparison.Ordinal));

        var g = new LoaderFixture();
        g.Files.Remove("rules/sight.json");
        One(g, "rules.sight 가 없다");
    }

    // ── 13.5-3 ──

    [Fact]
    public void Intervals_GapOverlapAndBossDuring()
    {
        var f = FullAct();
        f.Stage["spawn_intervals"]![1]!["from"] = "04:30";
        One(f, "구간에 빈틈");

        var g = FullAct();
        g.Stage["spawn_intervals"]![1]!["to"] = "07:00";
        One(g, "보스 방 t 시작");

        var h = new LoaderFixture();
        ((JsonArray)h.Stage["spawn_intervals"]!).Add(LoaderFixture.Obj("""{ "during": "boss", "weights": { "skeleton": 1 } }"""));
        One(h, "during: boss 는 보스 방이 있을 때만");

        var k = new LoaderFixture();
        k.Stage["spawn_intervals"]![0]!["weights"] = LoaderFixture.Obj("""{ "skeleton": 0 }""");
        One(k, "가중치 합이 0");
    }

    // ── 13.5-15 ──

    [Fact]
    public void HeroLight_OutsideRange_IsError()
    {
        var f = new LoaderFixture();
        f.Stage["lighting"]!["hero_light"]!["radius"] = 360;
        One(f, "주인공 빛 반경 360");

        var g = new LoaderFixture();
        g.Stage["events"] = new JsonArray(LoaderFixture.Obj("""{ "at": "00:30", "type": "light_change", "set": [ { "path": "hero_light.radius", "value": [500] } ] }"""));
        var e = One(g, "주인공 빛 반경 500");
        Assert.Equal("$.stages[0].events[0].set[0]", e.JsonPath);
    }

    // ── 13.5-20 ──

    [Fact]
    public void Bundles_BaseStageWithHellfireEnemy_IsError()
    {
        var f = new LoaderFixture();
        f.First("enemies/skeleton.json", "enemies")["requires"] = new JsonArray("hellfire");
        One(f, "requires [hellfire] 가 스테이지 requires [] 밖");
    }

    [Fact]
    public void Bundles_SummonedHellfireEnemy_IsError()
    {
        var f = new LoaderFixture();
        var enemies = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        enemies.Add(LoaderFixture.Obj("""{ "id": "hf", "sprites": "set_skelaxe", "requires": ["hellfire"] }"""));
        ((JsonArray)f.First("enemies/skeleton.json", "enemies")["behaviors"]!).Add(LoaderFixture.Obj("""{ "type": "summon", "trigger": "on_death", "enemy": "hf" }"""));
        One(f, "내는 적 hf");
    }

    [Fact]
    public void Bundles_Hfvoice_AndHeroClassBundled_AndMusicFallback()
    {
        var f = new LoaderFixture();
        f.Stage["requires"] = new JsonArray("hfvoice");
        One(f, "hfvoice");

        var g = new LoaderFixture();
        g.First("heroes/sorcerer.json", "hero_classes")["requires"] = new JsonArray("hfmonk");
        One(g, "기본 직업이어야 한다");

        var h = new LoaderFixture();
        h.Files["audio/music.json"] = LoaderFixture.Obj("""
            { "music": [ { "id": "m_base", "file": "a.wav" }, { "id": "m_hf", "file": "b.wav", "requires": ["hfmusic"], "fallback": "m_hf2" },
                         { "id": "m_hf2", "file": "c.wav", "requires": ["hfmusic"] } ] }
            """);
        One(h, "fallback m_hf2 의 requires 가 비지 않았다");
    }

    [Fact]
    public void Bundles_NextActChain()
    {
        var f = new LoaderFixture();
        var stages = (JsonArray)f.Files["stages/stage_test.json"]["stages"]!;
        var hf = (JsonObject)f.Stage.DeepClone();
        hf["id"] = "stage_hf";
        hf["requires"] = new JsonArray("hellfire");
        stages.Add(hf);
        f.Stage["next_act"] = "stage_hf";
        One(f, "next_act stage_hf 가 묶음 액트다");

        var g = new LoaderFixture();
        g.Stage["next_act"] = "stage_test";
        One(g, "고리");
    }

    // ── 13.5-21 ──

    [Fact]
    public void HeroClass_WeaponClassOutside_IsError()
    {
        var f = new LoaderFixture();
        f.Files["heroes/warrior.json"] = LoaderFixture.Obj("""{ "hero_classes": [ { "id": "warrior", "sprites": "set_sorcerer", "start_weapons": ["firebolt"] } ] }""");
        f.First("weapons/firebolt.json", "weapons")["classes"] = new JsonArray("warrior");
        var e = One(f, "시작 무기 firebolt 를 이 직업이 고를 수 없다");
        Assert.Equal("heroes/sorcerer.json", e.File);
    }

    [Fact]
    public void HeroClass_MissingClip_AndAttackClip()
    {
        var f = new LoaderFixture();
        f.First("heroes/sorcerer.json", "hero_classes")["sprites"] = "set_skelaxe";
        var r = f.Load();
        Assert.Contains(LoaderFixture.ErrorsOf(r), e => e.Message.Contains("'cast_fire' 클립이 없다", StringComparison.Ordinal));

        var g = new LoaderFixture();
        g.First("weapons/firebolt.json", "weapons")["cast_clip"] = "attack";
        One(g, "attack 클립을 쓰는데");
    }

    [Fact]
    public void HeroClass_TraitRules_AndRadius()
    {
        var f = new LoaderFixture();
        f.Files["passives.json"] = LoaderFixture.Obj("""{ "passives": [ { "id": "tr", "max_level": 2 } ] }""");
        f.First("heroes/sorcerer.json", "hero_classes")["traits"] = new JsonArray("tr");
        f.First("heroes/sorcerer.json", "hero_classes")["radius"] = 16;
        var r = f.Load();
        var msgs = LoaderFixture.ErrorsOf(r).Select(e => e.Message).ToList();
        Assert.Contains(msgs, m => m.Contains("trait: true 가 아니다", StringComparison.Ordinal));
        Assert.Contains(msgs, m => m.Contains("max_level 이 1 이 아니다", StringComparison.Ordinal));
        Assert.Contains(msgs, m => m.Contains("radius 16 > 14", StringComparison.Ordinal));
    }

    // ── 경고 ──

    [Fact]
    public void Warnings_UnreachableEnemy_AndScale()
    {
        var f = new LoaderFixture();
        ((JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!).Add(LoaderFixture.Obj("""{ "id": "lonely", "sprites": "set_skelaxe", "scale": 2.0, "radius": 24 }"""));
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        Assert.Contains(r.Errors, e => e.Severity == DataSeverity.Warning && e.Message.Contains("lonely 가 어느 스테이지에서도", StringComparison.Ordinal));
        Assert.Contains(r.Errors, e => e.Severity == DataSeverity.Warning && e.JsonPath == "$.enemies[1].scale");
    }
}
