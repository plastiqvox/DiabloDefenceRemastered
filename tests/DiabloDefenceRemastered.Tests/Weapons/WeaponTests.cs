using DiabloDefenceRemastered.Core.Sim.Weapons;
using DiabloDefenceRemastered.Tests.Combat;

namespace DiabloDefenceRemastered.Tests.Weapons;

/// <summary>10.1 <c>Weapons</c> — <c>WeaponStats.Resolve</c>·조준 5종·패턴별 발사 결과·관통·튕김·적별 타격 간격·레벨 표 검사(13.5-2).</summary>
public class WeaponTests
{
    private static StatBlock Stats(params (StatKey Key, float Value)[] values)
    {
        var s = new StatBlock();
        foreach (var (k, v) in values) s[k] = v;
        return s;
    }

    // ── Resolve ──────────────────────────────────────────────────

    [Fact]
    public void Resolve_CountAddsProjectileCount_DamageIgnoresDamagePct()
    {
        var mm = CombatKit.MagicMissile();
        var r = WeaponStats.Resolve(mm, 5, Stats((StatKey.ProjectileCount, 1), (StatKey.DamagePct, 0.5f)), FirePatterns.Of(mm));
        Assert.Equal(4, r.Count);                                   // 3 + 1
        Assert.Equal(14f, r.Damage);                                // damage_pct 는 DamageMath 에서만
        Assert.Equal(0.5f, r.DamagePct);
        Assert.Equal(330f, r.Speed);
        Assert.Equal(360f, r.TurnRate);
        Assert.Equal(0.08f, r.BurstInterval);
    }

    [Fact]
    public void Resolve_AreaPctOnlyOnDeclaredKeys()
    {
        var fb = CombatKit.Fireball();
        var r = WeaponStats.Resolve(fb, 1, Stats((StatKey.AreaPct, 0.2f)), FirePatterns.Of(fb));
        Assert.Equal(60f, r.Radius, 3);                             // radius 50 × 1.2
        var mm = CombatKit.MagicMissile();
        var m = WeaponStats.Resolve(mm, 1, Stats((StatKey.AreaPct, 0.2f)), FirePatterns.Of(mm));
        Assert.Equal(0f, m.Radius);                                 // homing 에 area 키 없음(그림 크기만)
        Assert.Equal(1.2f, m.AreaMul, 3);
        var orb = CombatKit.ArcaneOrbit();
        var o = WeaponStats.Resolve(orb, 1, Stats((StatKey.AreaPct, 0.2f), (StatKey.DurationPct, 0.5f)), FirePatterns.Of(orb));
        Assert.Equal(84f, o.OrbitRadius, 3);
        Assert.Equal(4.5f, o.Duration, 3);
    }

    [Fact]
    public void Resolve_CooldownCap_AndSurge()
    {
        var mm = CombatKit.MagicMissile();
        var p = FirePatterns.Of(mm);
        Assert.Equal(1.2f * 0.4f, WeaponStats.Resolve(mm, 1, Stats((StatKey.CooldownPct, 0.9f)), p).Cooldown, 4);
        Assert.Equal(1.2f * 0.2f, WeaponStats.Resolve(mm, 1, Stats((StatKey.CooldownPct, 0.5f)), p, surgePct: 0.5f).Cooldown, 4);
    }

    [Fact]
    public void Resolve_NullDuration_IsPermanent_NoDurationPct_NoCooldown()
    {
        var orb = CombatKit.ArcaneOrbit();
        var r = WeaponStats.Resolve(orb, 8, Stats((StatKey.DurationPct, 0.5f)), FirePatterns.Of(orb));
        Assert.True(r.Permanent);
        Assert.False(r.HasCooldown);
        Assert.Equal(5, r.Count);
    }

    [Fact]
    public void Resolve_Evolved_UsesBaseMaxLevelPlusOverride()
    {
        var mm = CombatKit.MagicMissile();
        var evo = new WeaponDef
        {
            Id = "t_storm", BaseWeapon = DefRef<WeaponDef>.To(mm),
            Override = new WeaponOverrideDef { Values = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["damage"] = System.Text.Json.JsonDocument.Parse("26").RootElement,
                ["turn_rate"] = System.Text.Json.JsonDocument.Parse("540").RootElement,
            } },
        };
        var r = WeaponStats.Resolve(evo, 1, Stats(), FirePatterns.Of(evo));
        Assert.Equal(26f, r.Damage);
        Assert.Equal(540f, r.TurnRate);
        Assert.Equal(4, r.Count);                                   // 재료 Lv8
    }

    // ── 조준 ─────────────────────────────────────────────────────

    [Fact]
    public void Targeting_Nearest_DistinctInOrder_ThenWraps_FallbackFacing()
    {
        var t = CombatKit.World();
        var w = t.World;
        var aim = Targetings.Create("nearest", 450f);
        var o = t.Hero.Pos;
        Assert.Equal(AimResult.Fallback, aim.Aim(w, o, 0, out var fp, out _));
        Assert.Equal(o + Vector2.Normalize(t.Hero.Facing) * 450f, fp);
        var far = t.AddEnemy(o + new Vector2(200, 0), CombatKit.Skeleton);
        var near = t.AddEnemy(o + new Vector2(0, 100), CombatKit.Skeleton);
        t.AddEnemy(o + new Vector2(500, 0), CombatKit.Skeleton);   // 반경 밖
        aim.Aim(w, o, 0, out _, out var a);
        aim.Aim(w, o, 1, out _, out var b);
        aim.Aim(w, o, 2, out _, out var c);
        Assert.Equal(near.Id, a);
        Assert.Equal(far.Id, b);
        Assert.Equal(near.Id, c);
    }

    [Fact]
    public void Targeting_Random_InRadius_Facing_None()
    {
        var t = CombatKit.World();
        var w = t.World;
        var o = t.Hero.Pos;
        var e1 = t.AddEnemy(o + new Vector2(100, 0), CombatKit.Skeleton);
        var e2 = t.AddEnemy(o + new Vector2(-100, 0), CombatKit.Skeleton);
        var random = Targetings.Create("random_in_radius", 400f);
        var seen = new HashSet<EntityId>();
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(AimResult.Target, random.Aim(w, o, i, out _, out var id));
            seen.Add(id);
        }
        Assert.Equal(new HashSet<EntityId> { e1.Id, e2.Id }, seen);
        Assert.Equal(AimResult.Fallback, Targetings.Create("facing", 300f).Aim(w, o, 0, out var fp, out _));
        Assert.Equal(o + Vector2.Normalize(t.Hero.Facing) * 300f, fp);
        Assert.Equal(AimResult.None, Targetings.Create("none", 0f).Aim(w, o, 0, out var np, out _));
        Assert.Equal(o, np);
    }

    [Fact]
    public void Targeting_Densest_Picks64pxCellWithMostEnemies()
    {
        var t = CombatKit.World();
        var w = t.World;
        var o = new Vector2(MathF.Floor(t.Hero.Pos.X / 64f) * 64f + 32f, MathF.Floor(t.Hero.Pos.Y / 64f) * 64f + 32f);
        t.PlaceHero(o);
        t.AddEnemy(o + new Vector2(128, 0), CombatKit.Skeleton);
        var cluster = o + new Vector2(-128, 64);
        for (var i = 0; i < 3; i++) t.AddEnemy(cluster + new Vector2(i * 8 - 8, 0), CombatKit.Skeleton);
        Assert.Equal(AimResult.Target, Targetings.Create("densest", 220f).Aim(w, o, 0, out var p, out _));
        Assert.Equal(cluster, p);
    }

    // ── 패턴 ─────────────────────────────────────────────────────

    private static List<Projectile> HeroShots(World w)
    {
        var list = new List<Projectile>();
        foreach (var p in w.Projectiles.Items) if (!p.Removed && p.Faction == Faction.Hero) list.Add(p);
        return list;
    }

    [Fact]
    public void Homing_NoTarget_Waits_ThenFiresWhenEnemyAppears()
    {
        var t = CombatKit.World();
        t.Step(90);
        Assert.Empty(HeroShots(t.World));
        Assert.True(t.Hero.Weapons[0].Cooldown <= 0f);
        t.AddEnemy(t.Hero.Pos + new Vector2(300, 0), CombatKit.Skeleton);
        t.Step(2);
        Assert.Single(HeroShots(t.World));
    }

    [Fact]
    public void StraightExplode_CountShots_ExplosionHitsAllInRadiusOnce()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Fb, 1);                // 반경 50, 피해 20
        var target = t.AddEnemy(t.Hero.Pos + new Vector2(200, 0), CombatKit.Skeleton);
        var side = t.AddEnemy(t.Hero.Pos + new Vector2(200, 40), CombatKit.Skeleton);
        var outside = t.AddEnemy(t.Hero.Pos + new Vector2(200, 120), CombatKit.Skeleton);
        t.Step(1);
        Assert.Single(HeroShots(w));
        t.Step(40);
        Assert.Equal(0f, target.Hp);                                // 20 피해 = 죽음(첫 맞은 적도 폭발로 한 번)
        Assert.True(side.Hp <= 0f || side.Dead || side.Removed);
        Assert.Equal(20f, outside.Hp);
    }

    [Fact]
    public void Orbit_EvenSpacing_TangentVel_OffAfterDuration_ThenCooldown()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Orbit, 1);             // 2개, 반경 70, 지속 3s, 쿨 4s
        t.Step(1);
        var orbs = HeroShots(w);
        Assert.Equal(2, orbs.Count);
        foreach (var p in orbs)
        {
            Assert.Equal(70f, Vector2.Distance(p.Pos, t.Hero.Pos), 2);
            Assert.Equal(0f, Vector2.Dot(Vector2.Normalize(p.Vel), Vector2.Normalize(p.Pos - t.Hero.Pos)), 3);   // 접선
        }
        Assert.Equal(-70f, Vector2.Dot(orbs[0].Pos - t.Hero.Pos, orbs[1].Pos - t.Hero.Pos) / 70f, 1);           // 맞은편
        t.Step(180);
        Assert.Empty(HeroShots(w));
        Assert.False(w.Hero.Weapons[0].Pattern.Active);
        t.Step(60 * 3);                                             // 쿨 4 × 0.95 = 3.8s 안
        Assert.Empty(HeroShots(w));
        t.Step(60);
        Assert.Equal(2, HeroShots(w).Count);
    }

    [Fact]
    public void Orbit_Permanent_NeverTurnsOff_LevelChangeRespawns()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Orbit, 7);
        t.Step(1);
        Assert.Equal(5, HeroShots(w).Count);
        w.Hero.Weapons[0].Level = 8;                                // 상시
        t.Step(60 * 10);
        Assert.Equal(5, HeroShots(w).Count);
        Assert.True(w.Hero.Weapons[0].Pattern.Active);
        w.Hero.Stats[StatKey.ProjectileCount] = 0;
        w.Combat.Mods[(int)StatKey.ProjectileCount] = 1;            // 발 수 +1 → 그 자리에서 다시 띄움
        w.Combat.StatsDirty = true;
        t.Step(2);
        Assert.Equal(6, HeroShots(w).Count);
    }

    [Fact]
    public void Orbit_PerEnemyHitInterval()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Orbit, 8);              // 피해 20, 적별 0.5s
        var e = t.AddEnemy(t.Hero.Pos + new Vector2(100, 0), CombatKit.Skeleton);
        e.Hp = e.MaxHp = 1000f;
        t.Step(60);                                                 // 1s 동안 5개가 지나가도 0.5s 마다 한 번
        var hits = (1000f - e.Hp) / 20f;
        Assert.InRange(hits, 2f, 3f);
    }

    [Fact]
    public void Pierce_NeverHitsSameEnemyTwice()
    {
        var t = CombatKit.World();
        var w = t.World;
        var shot = Core.Sim.Projectiles.HeroShots.Spawn(w, 0, w.Defs.Weapons.Get(CombatKit.Mm),
            WeaponStats.Resolve(w.Defs.Weapons.Get(CombatKit.Mm), 1, w.Hero.Stats, FirePatterns.Get("homing")), t.Hero.Pos, new Vector2(1, 0), 300f);
        shot.SpawnTick = w.Tick - 1;
        shot.PierceLeft = 5;
        CombatKit.ClearWeapons(w);
        var e = t.AddEnemy(t.Hero.Pos + new Vector2(60, 0), CombatKit.Skeleton);
        e.Hp = e.MaxHp = 1000f;
        t.Step(30);
        Assert.Equal(990f, e.Hp);
    }

    [Fact]
    public void Ricochet_BouncesToOtherEnemy_NotBack()
    {
        var t = CombatKit.World();
        var w = t.World;
        CombatKit.ClearWeapons(w);
        var arrow = CombatKit.MagicMissile() with { Id = "t_arrow", Pattern = "ricochet", Levels = new Dictionary<string, float?[]>
        {
            ["damage"] = [10], ["bounces"] = [2], ["cooldown"] = [5], ["count"] = [1],
        }, MaxLevel = 1, Base = new Dictionary<string, float> { ["speed"] = 500, ["lifetime"] = 3 } };
        var defs = CombatKit.Defs(extraWeapons: [arrow]);
        t = CombatKit.World(defs);
        w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, "t_arrow", 1);
        var a = t.AddEnemy(t.Hero.Pos + new Vector2(100, 0), CombatKit.Skeleton);
        var b = t.AddEnemy(t.Hero.Pos + new Vector2(150, 80), CombatKit.Skeleton);
        a.Hp = a.MaxHp = b.Hp = b.MaxHp = 1000f;
        t.Step(60);
        Assert.Equal(990f, a.Hp);
        Assert.Equal(990f, b.Hp);
    }

    [Fact]
    public void Fan_SpreadEvenly_AroundFacing()
    {
        var bow = CombatKit.MagicMissile() with { Id = "t_bow", Pattern = "fan", Targeting = Targetings.Part("facing", 300f), MaxLevel = 1,
            Levels = new Dictionary<string, float?[]> { ["damage"] = [8], ["count"] = [3], ["pierce"] = [2], ["cooldown"] = [5], ["spread"] = [30] },
            Base = new Dictionary<string, float> { ["speed"] = 520, ["lifetime"] = 0.9f } };
        var t = CombatKit.World(CombatKit.Defs(extraWeapons: [bow]));
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, "t_bow", 1);
        t.Hero.Facing = new Vector2(1, 0);
        t.Step(1);
        var angles = HeroShots(w).Select(p => MathF.Atan2(p.Vel.Y, p.Vel.X) * 180f / MathF.PI).OrderBy(a => a).ToArray();
        Assert.Equal(3, angles.Length);
        Assert.Equal(-15f, angles[0], 2);
        Assert.Equal(0f, angles[1], 2);
        Assert.Equal(15f, angles[2], 2);
    }

    [Fact]
    public void GroundPool_HitsInRadius_PerEnemyInterval()
    {
        var pool = new WeaponDef { Id = "t_pool", Name = "t_pool", Pattern = "ground_pool", Targeting = Targetings.Part("densest", 220f), MaxLevel = 1,
            Levels = new Dictionary<string, float?[]> { ["tick_damage"] = [5], ["radius"] = [60], ["duration"] = [3], ["count"] = [1], ["cooldown"] = [10], ["slow_pct"] = [0] },
            Base = new Dictionary<string, float> { ["hit_interval"] = 0.5f } };
        var t = CombatKit.World(CombatKit.Defs(extraWeapons: [pool]));
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, "t_pool", 1);
        var e = t.AddEnemy(t.Hero.Pos + new Vector2(100, 0), CombatKit.Skeleton);
        e.Hp = e.MaxHp = 1000f;
        t.Step(62);
        Assert.Equal(1000f - 3 * 5f, e.Hp);
    }

    // ── 13.5-2 ───────────────────────────────────────────────────

    [Fact]
    public void Validator_PassesOnSynthetic_FailsOnBadTables()
    {
        var ok = CombatKit.Defs();
        var ctx = new ValidationContext();
        new WeaponTableValidator().Validate(ok, ctx);
        Assert.Empty(ctx.Errors);

        var bad = CombatKit.MagicMissile() with { Id = "t_bad", Levels = new Dictionary<string, float?[]>
        {
            ["damage"] = [10, 9, 14, 14, 14, 18, 18, 22],                       // 줄어듦
            ["cooldown"] = [1.2f, 1.3f, 1.2f, 1.0f, 1.0f, 1.0f, 0.85f, null],   // 늘어남 + null
            ["count"] = [1, 2, 2],                                              // 길이
            ["pierce"] = [1, 1, 1, 1, 1, 2, 2, 2],
            ["radius"] = [1, 1, 1, 1, 1, 1, 1, 1],                              // 패턴 키 아님(speed 빠짐)
        } };
        var ctx2 = new ValidationContext();
        new WeaponTableValidator().Validate(CombatKit.Defs(extraWeapons: [bad]), ctx2);
        var messages = string.Join("\n", ctx2.Errors);
        Assert.Contains("t_bad", messages);
        Assert.Contains("줄었다", messages);
        Assert.Contains("늘었다", messages);
        Assert.Contains("null 은", messages);
        Assert.Contains("max_level", messages);
        Assert.Contains("'speed'", messages);
        Assert.Contains("levels.radius", messages);
    }
}
