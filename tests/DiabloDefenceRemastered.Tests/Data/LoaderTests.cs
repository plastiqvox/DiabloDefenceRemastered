using System.Text.Json.Nodes;
using DiabloDefenceRemastered.Core.Data.Loading;

namespace DiabloDefenceRemastered.Tests.Data;

/// <summary>데이터 로더(설계서 6.2, 10.1 <c>Data</c> — WP7): 병합·페이즈·부품·연결·오류 위치·순서.</summary>
public sealed class LoaderTests
{
    private static string Dump(IEnumerable<DataError> errors) => string.Join("\n", errors.Select(e => e.ToString()));

    private static DataError Single(DataLoadResult r, string contains)
    {
        var hits = LoaderFixture.ErrorsOf(r).Where(e => e.Message.Contains(contains, StringComparison.Ordinal)).ToList();
        Assert.True(hits.Count == 1, $"'{contains}' 오류가 {hits.Count}개다:\n{Dump(r.Errors)}");
        return hits[0];
    }

    [Fact]
    public void Base_HasNoErrors()
    {
        var r = new LoaderFixture().Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        Assert.Equal("sorcerer", r.Defs.Rules.Hero!.Class.Def.Id);
        Assert.Same(r.Defs.Maps.Get("cathedral_arena"), r.Defs.Stages[0].RoomMaps.Normal[0].Def);
        Assert.Equal(1, r.Defs.Stages[0].Rooms[0].Number);   // number 를 적지 않으면 배열 자리 + 1
        Assert.NotNull(r.Defs.Rules.Sight);
        Assert.NotNull(r.Defs.Rules.Rooms);
    }

    [Fact]
    public void Weights_KeepJsonOrder()
    {
        var f = new LoaderFixture();
        f.Stage["rewards"]!["weights"] = LoaderFixture.Obj("""{ "rw_chest": 10, "rw_tome": 90 }""");
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        Assert.Equal(["rw_chest", "rw_tome"], r.Defs.Stages[0].Rewards.Weights.Select(w => w.Ref.Id));
        Assert.Equal(10f, r.Defs.Stages[0].Rewards.Weights[0].Weight);
    }

    [Fact]
    public void Extends_MergesDeep_ReplacesArrays_AndAddsPatches()
    {
        var f = new LoaderFixture();
        var arr = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        arr.Add(LoaderFixture.Obj("""
            { "id": "skeleton_fast", "extends": "skeleton", "speed": 100, "glow": { "alpha": 0.2 },
              "behaviors_patch": { "melee": { "cooldown": 0.7 } },
              "behaviors_add": [ { "id": "melee2", "type": "chase_melee", "cooldown": 2 } ] }
            """));
        arr.Add(LoaderFixture.Obj("""{ "id": "skeleton_child", "extends": "skeleton_fast", "hp": 3, "behaviors": [] }"""));
        AddToStage(f, "skeleton_fast", "skeleton_child");
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        var fast = r.Defs.Enemies.Get("skeleton_fast");
        Assert.Equal(100f, fast.Speed);
        Assert.Equal(10f, fast.Hp);                  // 부모 값
        Assert.Equal("white", fast.Variant);
        Assert.Equal(["melee", "melee2"], fast.Behaviors.Select(b => b.Id));
        Assert.Equal(0.7f, ((TestChaseParams)fast.Behaviors[0].Params).Cooldown);
        Assert.Equal(1.0f, ((TestChaseParams)r.Defs.Enemies.Get("skeleton").Behaviors[0].Params).Cooldown);   // 부모는 그대로
        var child = r.Defs.Enemies.Get("skeleton_child");
        Assert.Empty(child.Behaviors);               // 배열은 통째 교체
        Assert.Equal(3f, child.Hp);
        Assert.Equal(100f, child.Speed);
    }

    [Fact]
    public void Abstract_IsNotInTable_AndCannotBeReferenced()
    {
        var f = new LoaderFixture();
        var arr = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        arr.Add(LoaderFixture.Obj("""{ "id": "skel_base", "abstract": true, "sprites": "set_skelaxe", "hp": 1 }"""));
        arr.Add(LoaderFixture.Obj("""{ "id": "skel_real", "extends": "skel_base" }"""));
        AddToStage(f, "skel_real", "skel_base");
        var r = f.Load();
        Assert.False(r.Defs.Enemies.TryGet("skel_base", out _));
        Assert.True(r.Defs.Enemies.TryGet("skel_real", out _));
        var e = Single(r, "abstract");
        Assert.Equal("stages/stage_test.json", e.File);
        Assert.Equal("$.stages[0].spawn_intervals[0].weights.skel_base", e.JsonPath);
    }

    [Fact]
    public void Extends_CycleAndMissingParent_AreErrors()
    {
        var f = new LoaderFixture();
        var arr = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        arr.Add(LoaderFixture.Obj("""{ "id": "a", "extends": "b", "sprites": "set_skelaxe" }"""));
        arr.Add(LoaderFixture.Obj("""{ "id": "b", "extends": "a", "sprites": "set_skelaxe" }"""));
        arr.Add(LoaderFixture.Obj("""{ "id": "c", "extends": "skeletn", "sprites": "set_skelaxe" }"""));
        var r = f.Load();
        Assert.Contains(LoaderFixture.ErrorsOf(r), e => e.Message.Contains("고리", StringComparison.Ordinal) && e.JsonPath.EndsWith(".extends", StringComparison.Ordinal));
        var missing = Single(r, "부모 'skeletn'");
        Assert.Contains("혹시 'skeleton'", missing.Message, StringComparison.Ordinal);
        Assert.Equal("$.enemies[3].extends", missing.JsonPath);
    }

    [Fact]
    public void Patch_MissingId_IsErrorAtPath()
    {
        var f = new LoaderFixture();
        var arr = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        arr.Add(LoaderFixture.Obj("""{ "id": "s2", "extends": "skeleton", "behaviors_patch": { "mele": { "cooldown": 2 } } }"""));
        var e = Single(f.Load(), "고칠 id 'mele'");
        Assert.Equal("$.enemies[1].behaviors_patch.mele", e.JsonPath);
        Assert.Contains("혹시 'melee'", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Phases_AccumulatePatchAddRemove_KeepingPlace()
    {
        var f = new LoaderFixture();
        var arr = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        arr.Add(LoaderFixture.Obj("""
            { "id": "boss", "sprites": "set_skelaxe", "rank": "boss", "hp": 500,
              "patterns": [
                { "id": "fan", "cooldown": 3, "actions": [ { "id": "spread", "type": "spread", "count": 3, "arc": 40 } ] },
                { "id": "raise", "cooldown": 8, "actions": [ { "type": "summon", "enemy": "skeleton" } ] },
                { "id": "slam", "cooldown": 2, "actions": [] } ],
              "phases": [
                { "hp_below": 0.6, "patch": { "fan": { "cooldown": 2.5, "actions_patch": { "spread": { "count": 5 } } } },
                  "add": [ { "id": "rain", "cooldown": 4, "actions": [] } ], "remove": ["slam"] },
                { "hp_below": 0.3, "add": [ { "id": "fan", "cooldown": 1, "actions": [] } ], "remove": [] } ] }
            """));
        AddToStage(f, "boss");
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        var boss = r.Defs.Enemies.Get("boss");
        Assert.Equal(["fan", "raise", "slam"], boss.Patterns.Select(p => p.Id));
        var p1 = boss.Phases[0].Patterns;
        Assert.Equal(["fan", "raise", "rain"], p1.Select(p => p.Id));
        Assert.Equal(2.5f, p1[0].Cooldown);
        Assert.Equal(5, ((TestSpreadParams)p1[0].Actions[0].Params).Count);
        Assert.Equal(40f, ((TestSpreadParams)p1[0].Actions[0].Params).Arc);
        var p2 = boss.Phases[1].Patterns;
        Assert.Equal(["fan", "raise", "rain"], p2.Select(p => p.Id));   // 같은 ID 는 원래 자리
        Assert.Equal(1f, p2[0].Cooldown);
        // 페이즈 패턴의 소환 참조도 연결되고 ReferencedEnemies 에 든다
        Assert.Equal("skeleton", Assert.Single(p1[1].Actions[0].ReferencedEnemies).Def.Id);
    }

    [Fact]
    public void Phase_RemoveMissing_IsError()
    {
        var f = new LoaderFixture();
        var arr = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        arr.Add(LoaderFixture.Obj("""
            { "id": "boss", "sprites": "set_skelaxe", "patterns": [ { "id": "a", "actions": [] } ], "phases": [ { "hp_below": 0.5, "remove": ["b"] } ] }
            """));
        var e = Single(f.Load(), "지울 패턴 'b'");
        Assert.Equal("$.enemies[1].phases[0].remove[0]", e.JsonPath);
    }

    [Fact]
    public void UnknownProperty_AndMultipleItems_AllCollected()
    {
        var f = new LoaderFixture();
        var arr = (JsonArray)f.Files["enemies/skeleton.json"]["enemies"]!;
        arr.Add(LoaderFixture.Obj("""{ "id": "x1", "sprites": "set_skelaxe", "hpp": 3 }"""));
        arr.Add(LoaderFixture.Obj("""{ "id": "x2", "sprites": "set_skelaxe", "speed": "fast" }"""));
        arr.Add(LoaderFixture.Obj("""{ "id": "x3", "sprites": "set_skelaxx" }"""));
        var r = f.Load();
        var errs = LoaderFixture.ErrorsOf(r).Where(e => e.File == "enemies/skeleton.json").ToList();
        Assert.Contains(errs, e => e.JsonPath.StartsWith("$.enemies[1]", StringComparison.Ordinal) && e.Message.Contains("hpp", StringComparison.Ordinal));
        Assert.Contains(errs, e => e.JsonPath == "$.enemies[2].speed");
        var missing = Assert.Single(errs, e => e.JsonPath == "$.enemies[3].sprites");
        Assert.Contains("혹시 'set_skelaxe'", missing.Message, StringComparison.Ordinal);
        Assert.True(r.Defs.Enemies.TryGet("x3", out _));   // 연결 오류는 정의를 빼지 않는다
    }

    [Fact]
    public void Part_Unregistered_IsErrorWithSuggestion_UnlessAllowed()
    {
        var f = new LoaderFixture();
        f.First("enemies/skeleton.json", "enemies")["behaviors"]![0]!["type"] = "chase_mele";
        var r = f.Load();
        var e = Single(r, "등록되지 않았다");
        Assert.Equal("$.enemies[0].behaviors[0].type", e.JsonPath);
        Assert.Contains("혹시 'chase_melee'", e.Message, StringComparison.Ordinal);

        var allowed = f.Load(new DataLoadOptions { AllowUnknownParts = true });
        Assert.DoesNotContain(allowed.Errors, x => x.Message.Contains("등록되지 않았다", StringComparison.Ordinal));
        Assert.IsType<JsonObject>(allowed.Defs.Enemies.Get("skeleton").Behaviors[0].Params);
    }

    [Fact]
    public void Part_ParamErrors_HaveFullPath()
    {
        var f = new LoaderFixture();
        var b = (JsonObject)f.First("enemies/skeleton.json", "enemies")["behaviors"]![0]!;
        b["cooldwn"] = 1;
        var e = Single(f.Load(), "cooldwn");
        Assert.StartsWith("$.enemies[0].behaviors[0]", e.JsonPath, StringComparison.Ordinal);

        var g = new LoaderFixture();
        g.First("enemies/skeleton.json", "enemies")["behaviors"]![0]!["cooldown"] = -1;
        var v = Single(g.Load(), "음수다");
        Assert.Equal("$.enemies[0].behaviors[0].cooldown", v.JsonPath);
    }

    [Fact]
    public void Trigger_LooksUpEnemyAction()
    {
        var f = new LoaderFixture();
        var behaviors = (JsonArray)f.First("enemies/skeleton.json", "enemies")["behaviors"]!;
        behaviors.Add(LoaderFixture.Obj("""{ "type": "summon", "trigger": "on_death", "enemy": "skeleton", "count": 2 }"""));
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        var hook = r.Defs.Enemies.Get("skeleton").Behaviors[1];
        Assert.Equal(PartKind.EnemyAction, hook.Kind);
        Assert.Equal("on_death", hook.Trigger);
        Assert.Same(r.Defs.Enemies.Get("skeleton"), Assert.Single(hook.ReferencedEnemies).Def);
    }

    [Fact]
    public void StageEvent_AtGoesToPartDef()
    {
        var f = new LoaderFixture();
        f.Stage["events"] = new JsonArray(LoaderFixture.Obj("""{ "at": "00:30", "type": "boss", "enemy": "skeleton" }"""));
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        var ev = Assert.Single(r.Defs.Stages[0].Events);
        Assert.Equal(30f, ev.At!.Value.Seconds);

        f.Stage["events"] = new JsonArray(LoaderFixture.Obj("""{ "type": "boss", "enemy": "skeleton" }"""));
        Single(f.Load(), "at(");
    }

    [Fact]
    public void Rules_MergeSubkeys_AndDuplicateIsError()
    {
        var f = new LoaderFixture();
        f.Files["rules/extra.json"] = LoaderFixture.Obj("""{ "rules": { "caps": { "enemies": 250 } } }""");
        var r = f.Load();
        Assert.False(r.HasErrors, Dump(r.Errors));
        Assert.Equal(250, r.Defs.Rules.Caps!.Enemies);
        Assert.NotNull(r.Defs.Rules.Sight);

        f.Files["rules/dup.json"] = LoaderFixture.Obj("""{ "rules": { "sight": { "edge": 1 } } }""");
        var e = Single(f.Load(), "두 파일");
        Assert.Equal("rules/sight.json", e.File);   // 서수 순: rules/dup.json 이 먼저 읽힌다
        Assert.Equal("$.rules.sight", e.JsonPath);
    }

    [Fact]
    public void DuplicateId_AndDuplicateKey_AndBadSyntax()
    {
        var f = new LoaderFixture();
        f.Files["enemies/dup.json"] = LoaderFixture.Obj("""{ "enemies": [ { "id": "skeleton", "sprites": "set_skelaxe" } ] }""");
        Single(f.Load(), "두 번 있다(처음:");

        var files = f.DataFiles.Append(new DataFile("broken.json", "{ \"enemies\": [ { \"id\": \"q\", \"id\": \"r\" } ] }"))
                               .Append(new DataFile("syntax.json", "{ \"enemies\": [ "));
        var r = DataLoader.Load(files, LoaderFixture.Registry(), []);
        Assert.Contains(r.Errors, e => e.File == "broken.json" && e.JsonPath == "$.enemies[0].id");
        Assert.Contains(r.Errors, e => e.File == "syntax.json" && e.Message.Contains("문법", StringComparison.Ordinal));
    }

    [Fact]
    public void ShuffledFileOrder_GivesSameDefs()
    {
        var f = new LoaderFixture();
        var files = f.DataFiles.ToList();
        var a = DataLoader.Load(files, LoaderFixture.Registry(), []);
        var rng = new Random(7);
        var shuffled = files.OrderBy(_ => rng.Next()).ToList();
        var b = DataLoader.Load(shuffled, LoaderFixture.Registry(), []);
        Assert.Equal(DefsFingerprint(a.Defs), DefsFingerprint(b.Defs));
        Assert.Equal(a.Errors.Select(e => e.ToString()), b.Errors.Select(e => e.ToString()));
    }

    [Fact]
    public void Validators_RunInOrder_AndExceptionBecomesError()
    {
        var order = new List<string>();
        var r = new LoaderFixture().Load(validators: [new FnValidator("a", (_, _) => order.Add("a")), new FnValidator("b", (_, _) => throw new InvalidOperationException("펑"))]);
        Assert.Equal(["a"], order);
        Single(r, "검사기 b 가 예외로");
    }

    [Fact]
    public void Sprites_AtlasForbiddenKeys_AndGridColorKeyDefault()
    {
        var f = new LoaderFixture();
        f.Files["sprites/ui.json"] = LoaderFixture.Obj("""
            { "sprites": [
                { "id": "g1", "source": "grid", "file": "Gold.png", "sheet": [96, 1600], "frame": [96, 160], "frames": 10 },
                { "id": "g2", "source": "grid", "file": "LOGO.png", "sheet": [550, 3240], "frame": [550, 216], "frames": 15, "color_key": null },
                { "id": "g3", "source": "grid", "file": "x.png", "sheet": [100, 100], "frame": [96, 160], "frames": 10 } ] }
            """);
        f.First("sprites/hero.json", "sprites")["slack"] = new JsonArray(1, 1);
        var r = f.Load();
        Assert.Equal(new Rgb(0, 0, 0), r.Defs.Sprites.Get("g1").ColorKey);
        Assert.Null(r.Defs.Sprites.Get("g2").ColorKey);
        var sheet = Single(r, "시트 100×100");
        Assert.Equal("$.sprites[2].sheet", sheet.JsonPath);
        var atlas = Single(r, "atlas 그림에는 slack");
        Assert.Equal("sprites/hero.json", atlas.File);
    }

    [Fact]
    public void Variant_NotInSet_IsError()
    {
        var f = new LoaderFixture();
        f.First("enemies/skeleton.json", "enemies")["variant"] = "whte";
        var e = Single(f.Load(), "변종 'whte'");
        Assert.Equal("$.enemies[0].variant", e.JsonPath);
        Assert.Contains("혹시 'white'", e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_SameAsFiles()
    {
        var f = new LoaderFixture();
        using var dir = new TempDir();
        foreach (var (rel, obj) in f.Files) dir.Write(rel, obj.ToJsonString());
        var a = DataLoader.Load(dir.Path, LoaderFixture.Registry(), []);
        Assert.False(a.HasErrors, Dump(a.Errors));
        Assert.Equal(DefsFingerprint(f.Load().Defs), DefsFingerprint(a.Defs));
        var missing = DataLoader.Load(Path.Combine(dir.Path, "없음"), LoaderFixture.Registry(), []);
        Assert.True(missing.HasErrors);
    }

    internal static void AddToStage(LoaderFixture f, params string[] enemies)
    {
        var w = (JsonObject)f.Stage["spawn_intervals"]![0]!["weights"]!;
        foreach (var e in enemies) w[e] = 10;
    }

    /// <summary>정의 표 순서·ID·참조 ID 를 한 줄로(파일 순서와 무관해야 함).</summary>
    internal static string DefsFingerprint(GameDefs d) => string.Join("|",
        new[]
        {
            string.Join(",", d.Sprites.All.Select(x => x.Id)), string.Join(",", d.SpriteSets.All.Select(x => x.Id)),
            string.Join(",", d.Enemies.All.Select(x => $"{x.Id}:{x.Hp}:{x.Sprites.Id}:{string.Join("/", x.Behaviors.Select(b => b.Type))}")),
            string.Join(",", d.Maps.All.Select(x => x.Id)), string.Join(",", d.Tilesets.All.Select(x => x.Id)),
            string.Join(",", d.Stages.All.Select(x => $"{x.Id}:{string.Join("/", x.SpawnIntervals.SelectMany(i => i.Weights).Select(w => w.Ref.Id))}")),
            string.Join(",", d.Rewards.All.Select(x => x.Id)), string.Join(",", d.HeroClasses.All.Select(x => x.Id)),
        });

    private sealed class FnValidator(string name, Action<GameDefs, ValidationContext> fn) : IDataValidator
    {
        public string Name => name;
        public void Validate(GameDefs defs, ValidationContext ctx) => fn(defs, ctx);
    }
}
