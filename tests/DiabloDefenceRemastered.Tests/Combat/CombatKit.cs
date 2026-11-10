using DiabloDefenceRemastered.Core.Sim.Weapons;

namespace DiabloDefenceRemastered.Tests.Combat;

/// <summary>
/// WP3 시험 도우미 — 합성 정의(무기 셋·패시브 셋·특성·보석·문 보상·성소)와 실제 <see cref="CombatModule"/> 을 넣은 시험 월드,
/// WP4 가 아직 없는 자리의 가짜(<see cref="FakeDeathHooks"/>), 투사체 막힘 벽이 있는 내비(<see cref="WallNav"/>).
/// 값은 기획 표(5.3~5.5·6·4.3·4.4·11.1·11.4)를 줄여 옮긴 것 — 실제 <c>Data/</c> 에 기대지 않는다.
/// </summary>
public static class CombatKit
{
    public const string Mm = "t_magic_missile";
    public const string Fb = "t_fireball";
    public const string Orbit = "t_arcane_orbit";
    public const string Vitality = "t_vitality";
    public const string Power = "t_arcane_power";
    public const string Runner = "t_field_runner";
    public const string Trait = "t_trait";
    public const string Hero = "t_hero";
    public const string Skeleton = "t_skeleton";
    public const string Temper = "filler_temper";
    public const string Enchanted = "t_shrine_enchanted";
    public const string Fascinating = "t_shrine_fascinating";

    private static readonly SpriteDef Sprite = TestDefs.Sprite();

    public static ProjectileDef Projectile(string id, float radius, float? speed = null, float turn = 0f, float? range = null, float? lifetime = null,
                                           bool blocked = true, ExplosionDef? explosion = null, GroundHazardDef? onEnd = null) => new()
    {
        Id = id, Sprite = DefRef<SpriteDef>.To(Sprite), Radius = radius, Speed = speed, TurnRate = turn, Range = range, Lifetime = lifetime,
        BlockedByObstacles = blocked, Explosion = explosion, OnEnd = onEnd,
    };

    public static readonly ProjectileDef PjMm = Projectile("t_pj_mm", 6f, turn: 360f, lifetime: 3f);
    public static readonly ProjectileDef PjFb = Projectile("t_pj_fb", 8f, 380f, range: 450f, explosion: new ExplosionDef());
    public static readonly ProjectileDef PjOrb = Projectile("t_pj_orb", 12f, blocked: false);
    public static readonly ProjectileDef PjAcid = Projectile("t_pj_acid", 7f, 200f, range: 340f,
        onEnd: new GroundHazardDef { Radius = 35f, Duration = 2.5f, Tick = 0.5f, Damage = 3f, CapGroup = "acid", Cap = 20 });

    public static WeaponDef MagicMissile() => new()
    {
        Id = Mm, Name = "매직 미사일", CastClip = ClipKind.CastLightning, Pattern = "homing", Targeting = Targetings.Part("nearest", 450f),
        Projectile = DefRef<ProjectileDef>.To(PjMm), MaxLevel = 8,
        Base = new Dictionary<string, float> { ["turn_rate"] = 360, ["lifetime"] = 3, ["burst_interval"] = 0.08f, ["knockback"] = 20 },
        Levels = new Dictionary<string, float?[]>
        {
            ["damage"] = [10, 10, 14, 14, 14, 18, 18, 22],
            ["cooldown"] = [1.2f, 1.2f, 1.2f, 1.0f, 1.0f, 1.0f, 0.85f, 0.85f],
            ["count"] = [1, 2, 2, 2, 3, 3, 3, 4],
            ["pierce"] = [1, 1, 1, 1, 1, 2, 2, 2],
            ["speed"] = [300, 300, 300, 330, 330, 330, 360, 360],
        },
    };

    public static WeaponDef Fireball() => new()
    {
        Id = Fb, Name = "파이어볼", CastClip = ClipKind.CastFire, Pattern = "straight_explode", Targeting = Targetings.Part("random_in_radius", 400f),
        Projectile = DefRef<ProjectileDef>.To(PjFb), MaxLevel = 8,
        Base = new Dictionary<string, float> { ["speed"] = 380, ["range"] = 450, ["knockback"] = 40 },
        Levels = new Dictionary<string, float?[]>
        {
            ["damage"] = [20, 20, 28, 28, 36, 36, 44, 56],
            ["radius"] = [50, 60, 60, 60, 70, 80, 80, 90],
            ["cooldown"] = [2.5f, 2.5f, 2.5f, 2.2f, 2.2f, 2.0f, 2.0f, 1.8f],
            ["count"] = [1, 1, 1, 2, 2, 2, 3, 3],
        },
    };

    public static WeaponDef ArcaneOrbit() => new()
    {
        Id = Orbit, Name = "비전 구체", CastClip = ClipKind.CastMagic, Pattern = "orbit", Targeting = Targetings.Part("none", 0f),
        Projectile = DefRef<ProjectileDef>.To(PjOrb), MaxLevel = 8,
        Base = new Dictionary<string, float> { ["rotation"] = 180, ["hit_interval"] = 0.5f, ["knockback"] = 30 },
        Levels = new Dictionary<string, float?[]>
        {
            ["damage"] = [8, 8, 11, 11, 14, 14, 17, 20],
            ["count"] = [2, 3, 3, 3, 4, 4, 5, 5],
            ["orbit_radius"] = [70, 70, 80, 80, 90, 90, 100, 100],
            ["duration"] = [3f, 3f, 3.5f, 4f, 4f, 5f, 5f, null],
            ["cooldown"] = [4f, 4f, 4f, 3.5f, 3.5f, 3f, 3f, null],
        },
    };

    /// <summary>같은 패턴의 다른 무기 둘(칸 다섯을 채우는 시험용 — 매직 미사일 모양).</summary>
    public static WeaponDef Extra(string id) => MagicMissile() with { Id = id, Name = id };

    public static PassiveDef Passive(string id, StatKey key, float perLevel, int max = 5, float heal = 0f) => new()
    {
        Id = id, Name = id, MaxLevel = max, PerLevel = new Dictionary<StatKey, float> { [key] = perLevel },
        OnAcquire = heal > 0f ? new OnAcquireDef { Heal = heal } : null,
    };

    public static RulesDef Rules(HeroClassDef hero, RulesDef basic) => basic with
    {
        Hero = new HeroRulesDef
        {
            Class = DefRef<HeroClassDef>.To(hero),
            InvulnAfterHit = 0.25f,
            Stats = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 100, [StatKey.MoveSpeed] = 95, [StatKey.PickupRadius] = 60 },
            CrowdSlow = new CrowdSlowDef { Touching = 3, Mul = 0.6f },
            Belt = new BeltRulesDef { Slots = 2, Threshold = 0.3f, Cooldown = 1f },
        },
        XpCurve =
        [
            new XpCurveSegment { From = 1, To = 1, Base = 5, Per = 0 },
            new XpCurveSegment { From = 2, To = 19, Base = 5, Per = 10, Origin = 1 },
            new XpCurveSegment { From = 20, To = 39, Base = 185, Per = 30, Origin = 19 },
            new XpCurveSegment { From = 40, To = null, Base = 785, Per = 40, Origin = 39 },
        ],
        LevelUp = new LevelUpRulesDef
        {
            Cards = 3,
            Weights = new LevelUpWeightsDef { OwnedWeapon = 100, OwnedPassive = 100, NewWeapon = 70, NewPassive = 70 },
            WeaponSlots = 4, PassiveSlots = 4, Rerolls = 3, SkipHeal = 10, InputLock = 0.4f,
            GuaranteeNewWeaponAt = [2, 3],
            Fillers =
            [
                new FillerCardDef { Id = "filler_potion", Name = "체력 물약", Heal = 30 },
                new FillerCardDef { Id = Temper, Name = "단련", Heal = 5, Stats = new Dictionary<StatKey, float> { [StatKey.MaxHp] = 5 } },
            ],
            Temper = Temper,
        },
        LevelUpPush = new LevelUpPushDef { Radius = 150, Distance = 80 },
    };

    /// <summary>
    /// 합성 정의 한 벌. <paramref name="startWeapons"/> 가 없으면 매직 미사일 하나. 무기 표 = 매직 미사일·파이어볼·비전 구체(+ <paramref name="extraWeapons"/>).
    /// 보상 = <see cref="TestDefs.Rewards"/>(마법서·보물 상자·물약·음산한 성소) + 마법에 걸린·매혹의 성소(정의 표에만).
    /// </summary>
    public static GameDefs Defs(IReadOnlyList<WeaponDef>? startWeapons = null, IEnumerable<WeaponDef>? extraWeapons = null, HeroClassDef? heroClass = null,
                                IEnumerable<EnemyDef>? extraEnemies = null)
    {
        var basic = TestDefs.Basic();
        var set = basic.SpriteSets[0];
        var mm = MagicMissile();
        var weapons = new List<WeaponDef> { mm, Fireball(), ArcaneOrbit() };
        if (extraWeapons is not null) weapons.AddRange(extraWeapons);
        var trait = new PassiveDef { Id = Trait, Name = Trait, Trait = true, MaxLevel = 1, PerLevel = new Dictionary<StatKey, float> { [StatKey.CooldownPct] = 0.05f } };
        var passives = new List<PassiveDef>
        {
            Passive(Vitality, StatKey.MaxHp, 20, heal: 20), Passive(Power, StatKey.DamagePct, 0.10f),
            Passive(Runner, StatKey.MovePct, 0.08f) with { PerLevel = new Dictionary<StatKey, float> { [StatKey.MovePct] = 0.08f, [StatKey.PickupPct] = 0.20f } },
            trait,
        };
        var start = startWeapons ?? [mm];
        var hero = heroClass ?? new HeroClassDef
        {
            Id = Hero, Name = Hero, Sprites = DefRef<SpriteSetDef>.To(set), Radius = 14f,
            StartWeapons = [.. start.Select(DefRef<WeaponDef>.To)], Traits = [DefRef<PassiveDef>.To(trait)],
        };
        var (rewards, objects) = TestDefs.Rewards();
        var shrineObj = TestDefs.RewardObject("obj_t_shrine2");
        var allRewards = new List<RewardDef>(rewards)
        {
            new() { Id = Enchanted, Name = "마법에 걸린 성소", Kind = RewardKind.Shrine, Object = DefRef<PickupDef>.To(shrineObj),
                    Shrine = new ShrineDef { AllWeapons = 1, RandomWeapon = -1 } },
            new() { Id = Fascinating, Name = "매혹의 성소", Kind = RewardKind.Shrine, Object = DefRef<PickupDef>.To(shrineObj),
                    Shrine = new ShrineDef { PickWeapon = 2, PickMaxLevel = 7, Stats = new Dictionary<StatKey, float> { [StatKey.MaxHpPct] = -0.10f } } },
        };
        var enemies = new List<EnemyDef>(basic.Enemies.All) { TestDefs.Enemy(set, Skeleton, hp: 20f, speed: 0f) with { Xp = 1 } };
        if (extraEnemies is not null) enemies.AddRange(extraEnemies);
        return new GameDefs
        {
            Rules = Rules(hero, basic.Rules),
            Sprites = basic.Sprites,
            SpriteSets = basic.SpriteSets,
            Projectiles = new DefTable<ProjectileDef>([PjMm, PjFb, PjOrb, PjAcid]),
            Weapons = new DefTable<WeaponDef>(weapons),
            Passives = new DefTable<PassiveDef>(passives),
            HeroClasses = new DefTable<HeroClassDef>([hero]),
            Tilesets = basic.Tilesets,
            Maps = basic.Maps,
            Stages = basic.Stages,
            Enemies = new DefTable<EnemyDef>(enemies),
            Rewards = new DefTable<RewardDef>(allRewards),
            Pickups = new DefTable<PickupDef>([.. Gems(), .. objects, shrineObj, TestDefs.EliteChest(),
                                               new PickupDef { Id = "t_potion", Kind = PickupKind.Heal, Amount = 30, SkipWhenFull = true, Draw = Draw },
                                               new PickupDef { Id = "t_surge", Kind = PickupKind.Surge, Amount = 0.5f, Duration = 8, Draw = Draw },
                                               new PickupDef { Id = "t_gold", Kind = PickupKind.Gold, Magnet = true, Draw = Draw }]),
        };
    }

    private static readonly PickupDrawDef Draw = new() { Procedural = "gem", Size = new Int2(10, 14) };

    private static IEnumerable<PickupDef> Gems() =>
    [
        new PickupDef { Id = "t_gem_blue", Kind = PickupKind.Xp, ValueRange = [1, 4], Magnet = true, Draw = Draw },
        new PickupDef { Id = "t_gem_green", Kind = PickupKind.Xp, ValueRange = [5, 19], Magnet = true, Draw = Draw },
        new PickupDef { Id = "t_gem_red", Kind = PickupKind.Xp, ValueRange = [20, 1000000], Magnet = true, Draw = Draw },
    ];

    /// <summary>실제 <see cref="CombatModule"/> 하나만 등록한 모듈(다른 묶음 없이).</summary>
    public static ModuleContext Modules()
    {
        var ctx = new ModuleContext();
        new CombatModule().Register(ctx);
        return ctx;
    }

    /// <summary>합성 정의 + 실제 전투 모듈 + 가짜 DeathHooks 의 방 없는 시험 월드.</summary>
    public static TestWorld World(GameDefs? defs = null, ulong seed = 1, Action<TestWorldBuilder>? configure = null, bool deathHooks = true)
    {
        var b = new TestWorldBuilder().WithSeed(seed).WithDefs(defs ?? Defs()).WithModules(Modules());
        if (deathHooks) b.WithSystem(SimPhase.DeathHooks, new FakeDeathHooks());
        configure?.Invoke(b);
        return b.Build();
    }

    /// <summary>합성 방 월드(<c>WithRoom</c>) + 실제 전투 모듈.</summary>
    public static TestWorld RoomWorld(RoomPhase phase, GameDefs? defs = null, RewardDef? reward = null, ulong seed = 1)
        => new TestWorldBuilder().WithSeed(seed).WithDefs(defs ?? Defs()).WithRoom(phase, reward: reward, fakeSpawner: false)
            .WithModules(Modules()).WithSystem(SimPhase.DeathHooks, new FakeDeathHooks()).Build();

    /// <summary>칸 하나를 그 무기·레벨로(쿨다운 0 — 다음 틱에 쏜다).</summary>
    public static void SetWeapon(World world, int slot, string id, int level)
    {
        var def = world.Defs.Weapons.Get(id);
        world.Hero.Weapons[slot] = new WeaponSlot { Def = def, Level = level, MaxedAtTick = level >= def.MaxLevel ? 0 : -1 };
        world.Combat.StatsDirty = true;
    }

    public static void ClearWeapons(World world)
    {
        for (var i = 0; i < WeaponSlots.SlotCount; i++) world.Hero.Weapons[i] = new WeaponSlot { MaxedAtTick = -1 };
    }

    /// <summary>적 쪽 획득물 놓기를 거치지 않고 바로 하나(드롭과 같은 길 — 실제 <c>SpawnPickup</c>).</summary>
    public static Pickup Drop(World world, string id, Vector2 pos, int value = 0, RewardDef? reward = null)
    {
        var idOf = world.Spawner.SpawnPickup(new PickupSpawn(world.Defs.Pickups.Get(id), pos, value, Reward: reward));
        var p = world.Pickups.Get(idOf)!;
        p.SpawnTick = world.Tick - 1;
        return p;
    }
}

/// <summary>DeathHooks 자리(WP4 대신): 죽은 적마다 <c>EnemyKilled</c> 하나를 내고 지운다(훅·시체 없음).</summary>
public sealed class FakeDeathHooks : ISimSystem
{
    public void Tick(World world)
    {
        var store = world.Enemies;
        for (var i = 0; i < store.Count; i++)
        {
            var e = store[i];
            if (e.Removed || !e.Dead) continue;
            world.Events.Current.EnemyKilled.Add(new EnemyKilled(e.Id, e.DefIndex, e.Pos, e.Rank, e.KillCause, e.KilledBy));
            store.RemoveAt(i);
        }
    }
}

/// <summary>직선 내비 + x = <paramref name="wallX"/> 의 투사체 막힘 벽(그 x 를 가로지르는 선분은 막힘).</summary>
public sealed class WallNav(INavWorld inner, float wallX) : INavWorld
{
    public float Clearance(Vector2 p) => inner.Clearance(p);
    public bool HasClearLine(Vector2 a, Vector2 b, float radius) => inner.HasClearLine(a, b, radius);
    public bool ProjectileLineClear(Vector2 a, Vector2 b) => (a.X < wallX) == (b.X < wallX);
    public bool IsInsideMap(Vector2 p) => inner.IsInsideMap(p);
    public float PathLength(Vector2 from, Vector2 to, float radius, float maxLength) => inner.PathLength(from, to, radius, maxLength);
    public float PathLengthToHero(Vector2 from, float radius, float maxLength) => inner.PathLengthToHero(from, radius, maxLength);
    public bool TryPathStep(Vector2 from, Vector2 to, float radius, out Vector2 next) => inner.TryPathStep(from, to, radius, out next);
}
