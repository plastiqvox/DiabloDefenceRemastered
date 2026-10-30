using DiabloDefenceRemastered.Core.Modules;

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
    // 로더(WP7)·MapDataReader·ImportTable 이 같이 쓰는 공용 설정(WP1b Data/Json/DefJson): snake_case, 모르는 속성은 오류, 열거형은 snake_case 문자열
    private static readonly System.Text.Json.JsonSerializerOptions Options = DiabloDefenceRemastered.Core.Data.Json.DefJson.Options;

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
        var weapon = System.Text.Json.JsonSerializer.Deserialize<WeaponDef>("""{ "id": "fireball", "cast_clip": "cast_fire" }""", Options)!;
        Assert.Equal(ClipKind.CastFire, weapon.CastClip);
        Assert.Equal(ClipKind.CastMagic, System.Text.Json.JsonSerializer.Deserialize<WeaponDef>("""{ "id": "mm" }""", Options)!.CastClip);
        Assert.Equal(Compass.SouthWest, System.Text.Json.JsonSerializer.Deserialize<Compass>("\"south_west\"", Options));
        Assert.Equal(DeathStyle.Anim, System.Text.Json.JsonSerializer.Deserialize<DeathStyle>("\"anim\"", Options));
        Assert.Equal(GateSide.Nw, System.Text.Json.JsonSerializer.Deserialize<GateSide>("\"nw\"", Options));
        Assert.Equal(SpriteSource.Atlas, System.Text.Json.JsonSerializer.Deserialize<SpriteSource>("\"atlas\"", Options));
        // v0.2 의 knight_fade 는 없앴다, 숫자 열거값은 받지 않는다
        Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<DeathStyle>("\"knight_fade\"", Options));
        Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<Compass>("1", Options));
    }

    [Fact]
    public void StatKeyDictionaries_StillUseJsonNames_WithCommonOptions()
    {
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<StatKey, float>>("""{ "max_hp": 20, "move_pct": 0.08 }""", Options)!;
        Assert.Equal(20f, dict[StatKey.MaxHp]);
        Assert.Equal(0.08f, dict[StatKey.MovePct]);
    }

    [Fact]
    public void Rules_V03Keys_Deserialize()
    {
        const string json = """
        {
          "ranks": { "elite": { "hp": 3.5 }, "unique": { "hp": 8 }, "pack": { "pack_size": [3, 5] }, "max_scale": 1.6 },
          "enemies": { "telegraph_required": { "min_damage": 10, "min_time": 0.4, "melee_reach_over_contact": 50,
                                               "heavy_melee_damage": 25, "heavy_melee_windup": 0.35 } },
          "audio": { "voices": 24, "same_sound": 3, "min_interval": 0.05, "monster_hit_interval": 0.08,
                     "monster_death": { "voices": 4, "same_kind_interval": 0.1 }, "offscreen_margin": 100, "pan": 0.5,
                     "defaults": { "master": 0.8, "sfx": 0.8, "music": 0.6 }, "music_fade": 0.5,
                     "voice_duck": { "to": 0.5, "time": 2.0 }, "pause_music": 0.5,
                     "categories": [ { "id": "boss_voice", "volume": 1.0, "offscreen": true }, { "id": "monster_hit", "volume": 0.5, "offscreen": false } ] }
        }
        """;
        var rules = System.Text.Json.JsonSerializer.Deserialize<RulesDef>(json, Options)!;
        Assert.Equal(1.6f, rules.Ranks!.MaxScale);
        Assert.Equal(0.35f, rules.Enemies!.TelegraphRequired.HeavyMeleeWindup);
        Assert.Equal(24, rules.Audio!.Voices);
        Assert.Equal(0.1f, rules.Audio.MonsterDeath.SameKindInterval);
        Assert.Equal(["boss_voice", "monster_hit"], rules.Audio.Categories.Select(c => c.Id));
        Assert.False(rules.Audio.Categories[1].Offscreen);
        Assert.Equal(1.6f, new RanksDef { Elite = new(), Unique = new(), Pack = new() }.MaxScale);   // 기본 1.6(6.10)
    }

    [Fact]
    public void SpriteAndEffect_V03Shapes_Deserialize()
    {
        var sprite = System.Text.Json.JsonSerializer.Deserialize<SpriteDef>("""
            { "id": "zombie_walk", "source": "atlas", "file": "d1/monsters/zombie.png", "action": "walk",
              "frame": [128, 96], "directions": 8, "frames": 24, "origin": [64, 80] }
            """, Options)!;
        Assert.Equal(SpriteSource.Atlas, sprite.Source);
        Assert.Equal("walk", sprite.Action);
        Assert.Equal(new Int2(128, 96), sprite.Frame);
        Assert.Null(sprite.ColorKey);   // "없음 → 검정" 은 로더 몫(6.3)

        var icon = System.Text.Json.JsonSerializer.Deserialize<SpriteDef>("""
            { "id": "icon", "source": "grid", "file": "d1/icons/magic_missile.png", "sheet": [48, 48], "frame": [48, 48], "color_key": null }
            """, Options)!;
        Assert.Null(icon.ColorKey);
        var gold = System.Text.Json.JsonSerializer.Deserialize<SpriteDef>("""
            { "id": "Gold", "source": "grid", "file": "Gold.png", "sheet": [96, 1600], "frame": [96, 160], "color_key": [0, 0, 0] }
            """, Options)!;
        Assert.Equal(Rgb.Black, gold.ColorKey);

        var clips = System.Text.Json.JsonSerializer.Deserialize<ClipSetDef>("""{ "special": null, "cast_lightning": null }""", Options)!;
        Assert.Null(clips.Get(ClipKind.Special));
        Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<ClipSetDef>("""{ "cast": null }""", Options));

        var fx = System.Text.Json.JsonSerializer.Deserialize<EffectDef>("""
            { "id": "fx_hellfire_pool", "scatter": { "per_radius": 20, "min": 3, "center": true }, "loop": true }
            """, Options)!;
        Assert.Equal(20f, fx.Scatter!.PerRadius);
        Assert.True(fx.Scatter.Center);

        var enemy = System.Text.Json.JsonSerializer.Deserialize<PhaseDef>("""{ "id": "p2", "hp_below": 0.5, "voice": true }""", Options)!;
        Assert.True(enemy.Voice);
    }

    [Fact]
    public void Audio_SoundAndMusic_Deserialize()
    {
        var snd = System.Text.Json.JsonSerializer.Deserialize<SoundDef>("""
            { "id": "snd_gem", "files": ["d1/sound/sfx/items/invgrab.wav"], "category": "pickup", "volume": 0.4, "min_interval": 0.06 }
            """, Options)!;
        Assert.Equal("pickup", snd.Category);
        Assert.Equal(0.06f, snd.MinInterval);
        Assert.Null(snd.MaxInstances);
        var mus = System.Text.Json.JsonSerializer.Deserialize<MusicDef>("""{ "id": "mus_title", "file": "d1/sound/music/dintro.wav" }""", Options)!;
        Assert.True(mus.Loop);
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
        Assert.Equal(6, GameModules.Create().Count);   // + AudioModule(WP1b)
    }
}
