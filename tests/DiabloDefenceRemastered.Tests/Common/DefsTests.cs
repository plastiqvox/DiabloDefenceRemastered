namespace DiabloDefenceRemastered.Tests.Common;

public class DefRefTests
{
    private static EffectDef Fx(string id) => new() { Id = id };

    [Fact]
    public void Bind_Once_ThenReadable()
    {
        var r = new DefRef<EffectDef>("fx_a");
        Assert.False(r.IsBound);
        var fx = Fx("fx_a");
        r.Bind(fx);
        Assert.True(r.IsBound);
        Assert.Same(fx, r.Def);
    }

    [Fact]
    public void BindTwice_Throws()
    {
        var r = new DefRef<EffectDef>("fx_a");
        r.Bind(Fx("fx_a"));
        Assert.Throws<InvalidOperationException>(() => r.Bind(Fx("fx_a")));
    }

    [Fact]
    public void ReadBeforeBind_Throws()
    {
        var r = new DefRef<EffectDef>("fx_a");
        Assert.Throws<InvalidOperationException>(() => r.Def);
    }

    [Fact]
    public void BindOtherId_Throws()
    {
        var r = new DefRef<EffectDef>("fx_a");
        Assert.Throws<ArgumentException>(() => r.Bind(Fx("fx_b")));
        Assert.False(r.IsBound);
    }
}

public class DefTableTests
{
    [Fact]
    public void KeepsOrder_AndFindsById()
    {
        var t = new DefTable<EffectDef>([new EffectDef { Id = "b" }, new EffectDef { Id = "a" }]);
        Assert.Equal("b", t[0].Id);
        Assert.Equal(1, t.IndexOf("a"));
        Assert.True(t.TryGet("a", out var a) && a.Id == "a");
        Assert.False(t.TryGet("c", out _));
        Assert.Equal(-1, t.IndexOf("c"));
    }

    [Fact]
    public void DuplicateId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DefTable<EffectDef>([new EffectDef { Id = "a" }, new EffectDef { Id = "a" }]));
    }
}

public class DefJsonShapeTests
{
    // 로더(WP7)와 같은 기본 설정: snake_case, 모르는 속성은 오류, 열거형은 snake_case 문자열
    private static readonly System.Text.Json.JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.SnakeCaseLower) },
    };

    [Fact]
    public void Rules_WithoutReferences_DeserializeFromDocumentShape()
    {
        const string json = """
        {
          "xp_curve": [ { "from": 1, "to": 1, "base": 5, "per": 0 },
                        { "from": 40, "to": null, "base": 445, "per": 16, "origin": 39 } ],
          "level_up": { "cards": 3, "weights": { "owned_weapon": 100, "owned_passive": 100, "new_weapon": 70, "new_passive": 70 },
                        "weapon_slots": 4, "passive_slots": 4, "rerolls": 3, "skip_heal": 10, "input_lock": 0.4,
                        "guarantee_new_weapon_at": [2, 3],
                        "stat_labels": { "cooldown": { "label": "쿨", "unit": "s" }, "damage_pct": { "label": "피해", "format": "pct" } } },
          "level_up_push": { "radius": 150, "distance": 80 },
          "caps": { "enemies": 300, "enemies_absolute": 360, "hero_projectiles": 400, "enemy_projectiles": 200, "gems": 300 },
          "spawn": { "band": [40, 200], "tries": 12, "edge_margin": 4, "group": [1, 3], "group_radius": 40, "accumulator_hold": 3 },
        }
        """;
        var rules = System.Text.Json.JsonSerializer.Deserialize<RulesDef>(json, Options)!;
        Assert.Null(rules.XpCurve[1].To);
        Assert.Equal(39, rules.XpCurve[1].Origin);
        Assert.Equal(70f, rules.LevelUp!.Weights.NewWeapon);
        Assert.Equal(StatFormat.Pct, rules.LevelUp.StatLabels["damage_pct"].Format);
        Assert.Equal(150f, rules.LevelUpPush!.Radius);
        Assert.Equal(360, rules.Caps!.EnemiesAbsolute);
        Assert.Equal([1, 3], rules.Spawn!.Group);
    }

    [Fact]
    public void UnknownProperty_IsError()
    {
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<CapsDef>("""{ "enemys": 300 }""", Options));
    }

    [Fact]
    public void MissingRequiredId_IsError()
    {
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<BuffDef>("""{ "stats": { "speed_pct": 0.25 } }""", Options));
        var buff = System.Text.Json.JsonSerializer.Deserialize<BuffDef>("""{ "id": "command", "stats": { "speed_pct": 0.25 } }""", Options)!;
        Assert.Equal(0.25f, buff.Stats[EnemyStatKey.SpeedPct]);
    }

    [Fact]
    public void Enums_UseSnakeCaseNames()
    {
        var kind = System.Text.Json.JsonSerializer.Deserialize<ObstacleKindDef>("""{ "id": "x", "decor": "round_ends", "draw": false }""", Options)!;
        Assert.Equal(DecorKind.RoundEnds, kind.Decor);
        Assert.False(kind.Draw);
        Assert.Equal(Compass.SouthWest, System.Text.Json.JsonSerializer.Deserialize<Compass>("\"south_west\"", Options));
        Assert.Equal(DeathStyle.KnightFade, System.Text.Json.JsonSerializer.Deserialize<DeathStyle>("\"knight_fade\"", Options));
    }
}

public class ClockTimeTests
{
    [Theory]
    [InlineData("00:00", 0f)]
    [InlineData("02:30", 150f)]
    [InlineData("10:00", 600f)]
    [InlineData("12:30.5", 750.5f)]
    public void Parse_MmSs(string text, float seconds)
    {
        Assert.Equal(seconds, ClockTime.Parse(text).Seconds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("10")]
    [InlineData("1:70")]
    [InlineData("-1:00")]
    [InlineData("ab:cd")]
    public void Parse_Rejects(string text)
    {
        Assert.False(ClockTime.TryParse(text, out _));
    }

    [Fact]
    public void ToString_IsMmSs() => Assert.Equal("03:05", new ClockTime(185f).ToString());
}

public class StatKeyTests
{
    [Fact]
    public void JsonNames_RoundTrip()
    {
        foreach (var k in Enum.GetValues<StatKey>())
            Assert.True(StatKeys.TryParse(StatKeys.JsonName(k), out StatKey back) && back == k);
        foreach (var k in Enum.GetValues<EnemyStatKey>())
            Assert.True(StatKeys.TryParse(StatKeys.JsonName(k), out EnemyStatKey back) && back == k);
        Assert.Equal(StatKeys.Count, Enum.GetValues<StatKey>().Length);
        Assert.Equal(StatKeys.EnemyCount, Enum.GetValues<EnemyStatKey>().Length);
        Assert.Equal("knockback_taken_pct", StatKeys.JsonName(StatKey.KnockbackTakenPct));
        Assert.False(StatKeys.TryParse("max_hpp", out StatKey _));
    }

    [Fact]
    public void StatDictionary_DeserializesFromJsonNames()
    {
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<StatKey, float>>("""{ "max_hp": 20, "move_pct": 0.08 }""")!;
        Assert.Equal(20f, dict[StatKey.MaxHp]);
        Assert.Equal(0.08f, dict[StatKey.MovePct]);
        var enemy = System.Text.Json.JsonSerializer.Deserialize<Dictionary<EnemyStatKey, float>>("""{ "attack_cooldown_pct": -0.3 }""")!;
        Assert.Equal(-0.3f, enemy[EnemyStatKey.AttackCooldownPct]);
    }
}

public class PartRegistryTests
{
    private sealed record SlamParams(float Radius);
    private sealed record Slam(float Radius);

    [Fact]
    public void Register_AndCreate()
    {
        var reg = new PartRegistry();
        reg.Register<SlamParams>(PartKind.EnemyAction, "melee_slam", p => new Slam(p.Radius),
            (p, ctx) => { if (p.Radius <= 0) ctx.Error("radius", "0 보다 커야 한다"); });

        Assert.True(reg.TryGet(PartKind.EnemyAction, "melee_slam", out var f));
        Assert.Equal(typeof(SlamParams), f!.ParamsType);
        Assert.Equal(new Slam(60), f.Create(new SlamParams(60)));

        var ctx = new ValidationContext { File = "enemies/x.json", JsonPath = "$.enemies[0].patterns[0].actions[0]" };
        f.Validate(new SlamParams(0), ctx);
        Assert.True(ctx.HasErrors);
        Assert.Equal("$.enemies[0].patterns[0].actions[0].radius", ctx.Errors[0].JsonPath);
    }

    [Fact]
    public void SameKindAndType_Twice_Throws()
    {
        var reg = new PartRegistry();
        reg.Register<SlamParams>(PartKind.EnemyAction, "summon", p => p);
        Assert.Throws<InvalidOperationException>(() => reg.Register<SlamParams>(PartKind.EnemyAction, "summon", p => p));
    }

    [Fact]
    public void SameType_DifferentKind_IsAllowed()
    {
        var reg = new PartRegistry();
        reg.Register<SlamParams>(PartKind.EnemyBehavior, "escort", p => p);
        reg.Register<SlamParams>(PartKind.StageEvent, "escort", p => p);
        Assert.True(reg.Contains(PartKind.EnemyBehavior, "escort"));
        Assert.True(reg.Contains(PartKind.StageEvent, "escort"));
        Assert.False(reg.Contains(PartKind.EnemyAction, "escort"));
        Assert.Equal(["escort"], reg.TypesOf(PartKind.StageEvent));
    }

    [Fact]
    public void DataError_Format()
    {
        var e = new DataError("enemies/skeleton_mage.json", "$.enemies[0].behaviors[1].projectile", "'pj_mage_boltt' 가 없다 (혹시 'pj_mage_bolt'?)");
        Assert.Equal("enemies/skeleton_mage.json $.enemies[0].behaviors[1].projectile: 'pj_mage_boltt' 가 없다 (혹시 'pj_mage_bolt'?)", e.ToString());
    }

    [Fact]
    public void GameModules_RegisterWithoutError()
    {
        var ctx = GameModules.CreateContext();
        Assert.NotNull(ctx.Parts);
        Assert.Equal(5, GameModules.Create().Count);
    }
}
