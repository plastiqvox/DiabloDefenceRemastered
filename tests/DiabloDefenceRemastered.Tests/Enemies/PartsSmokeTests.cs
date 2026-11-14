using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 전수 연기 시험 (설계서 6.13·10.1 <c>Enemies</c>): 등록된 모든 적 부품·동작을 가진 합성 적을 주인공 둘레에 놓고 무기가 켜진 가짜 월드에서 10초 — 예외 없음,
/// 등록 목록과 시험 목록이 같음(새 부품을 등록하면 여기 한 줄을 더해야 시험이 통과한다).
/// </summary>
public sealed class PartsSmokeTests
{
    private static readonly DefRef<BuffDef> CommandRef = DefRef<BuffDef>.To(Command);
    private static readonly DefRef<BuffDef> CryRef = DefRef<BuffDef>.To(WarCryBuff);
    private static readonly EnemyDef Minion = TestDefs.Enemy(Set, "smoke_minion");
    private static readonly ProjectileDef Arrow = new() { Id = "pj_smoke", Sprite = DefRef<SpriteDef>.To(TestDefs.Sprite("arrow")), Speed = 200f };
    private static readonly TelegraphDef Tel = new() { Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_smoke" }), Time = 0.5f };

    /// <summary>행동 부품 type → 시험 정의.</summary>
    private static readonly Dictionary<string, Func<PartDef>> Behaviors = new()
    {
        ["chase_melee"] = () => B("chase_melee", new ChaseMeleeParams { Cooldown = 0.5f }),
        ["shoot_projectile"] = () => B("shoot_projectile", new ShootProjectileParams { Projectile = DefRef<ProjectileDef>.To(Arrow), Cooldown = 1f, Range = 400f }),
        ["kite"] = () => B("kite", new KiteParams { RangeBand = [100f, 200f] }),
        ["self_destruct"] = () => B("self_destruct", new SelfDestructParams()),
        ["charge"] = () => B("charge", new ChargeParams { Telegraph = Tel, Damage = 10f }),
        ["command_aura"] = () => B("command_aura", new CommandAuraParams { Buff = CommandRef, TargetTag = "skeleton" }),
        ["escort"] = () => B("escort", new EscortParams { EscortEnemy = DefRef<EnemyDef>.To(Minion), DefaultCount = 3 }),
        ["split_on_death"] = () => B("split_on_death", new SplitOnDeathParams { Spawn = DefRef<EnemyDef>.To(Minion) }),
        ["panic_on_ally_death"] = () => B("panic_on_ally_death", new PanicOnAllyDeathParams()),
        ["phase_through"] = () => B("phase_through", new PhaseThroughParams { AlphaOver = 1f }),
        ["heavy_strike"] = () => B("heavy_strike", new HeavyStrikeParams()),
        ["war_cry"] = () => B("war_cry", new WarCryParams { Buff = CryRef, Cooldown = 2f }),
        // 우두머리 접두사 부품(2026-11-14 디아2식): 번개 튐·흡혈·순간이동
        ["spark_on_hurt"] = () => B("spark_on_hurt", new SparkOnHurtParams { Projectile = DefRef<ProjectileDef>.To(Arrow), Count = 3, Cooldown = 0.3f }),
        ["life_steal"] = () => B("life_steal", new LifeStealParams { Pct = 1f }),
        ["teleport_ability"] = () => B("teleport_ability", new TeleportAbilityParams { Cooldown = 2f, RangeBand = [0f, 900f] }),
    };

    /// <summary>동작 type → 시험 동작(패턴 안).</summary>
    private static readonly Dictionary<string, Func<PartDef>> Actions = new()
    {
        ["melee_slam"] = () => A("melee_slam", new MeleeSlamParams { Radius = 60f, Damage = 12f, Knockback = 30f }),
        ["projectile_spread"] = () => A("projectile_spread", new ProjectileSpreadParams { Projectile = DefRef<ProjectileDef>.To(Arrow), Count = 5, Arc = 60f }),
        ["telegraphed_blast"] = () => A("telegraphed_blast", new TelegraphedBlastParams { Points = new BlastPoints { Hero = true, Around = 3, Radius = 100f }, Telegraph = Tel, Radius = 50f, Damage = 15f }),
        ["dash"] = () => A("dash", new DashParams { Speed = 300f, Time = 0.5f, Damage = 10f, Telegraph = Tel }),
        ["summon"] = () => A("summon", new SummonParams { Spawns = [new SummonEntry { Enemy = DefRef<EnemyDef>.To(Minion), Count = 4 }], AliveCap = 6 }),
        ["ground_hazard"] = () => A("ground_hazard", new GroundHazardParams { Radius = 40f, Duration = 2f, Damage = 3f, Cooldown = 1f, CapGroup = "smoke", Cap = 20 }),
        ["buff_area"] = () => A("buff_area", new BuffAreaParams { Buff = CryRef }),
        // 보스 묶음 A(6.20): 나선 고리·순간이동
        ["barrage"] = () => A("barrage", new BarrageParams { Shape = BarrageShape.Ring, Projectile = DefRef<ProjectileDef>.To(Arrow), Count = 8, Waves = 3, Interval = 0.3f, Turn = 15f }),
        ["teleport"] = () => A("teleport", new TeleportParams()),
    };

    [Fact]
    public void Every_registered_part_has_a_smoke_case()
    {
        var registry = new PartRegistry();
        EnemyModule.RegisterParts(registry);
        Assert.Equal(registry.TypesOf(PartKind.EnemyBehavior).Order(), Behaviors.Keys.Order());
        Assert.Equal(registry.TypesOf(PartKind.EnemyAction).Order(), Actions.Keys.Order());
        Assert.Equal(9, registry.TypesOf(PartKind.StageEvent).Count());
    }

    public static TheoryData<string> AllCases()
    {
        var data = new TheoryData<string>();
        foreach (var k in Behaviors.Keys) data.Add("b:" + k);
        foreach (var k in Actions.Keys) data.Add("a:" + k);
        data.Add("t:ground_hazard:on_death");
        data.Add("t:ground_hazard:cooldown");
        data.Add("t:telegraphed_blast:on_death");
        data.Add("t:summon:on_spawn");
        return data;
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void Part_runs_ten_seconds_without_exception(string key)
    {
        var parts = key.Split(':');
        EnemyDef def;
        if (parts[0] == "b") def = Enemy("smoke_" + parts[1], Behaviors[parts[1]]());
        else if (parts[0] == "a")
            def = Enemy("smoke_" + parts[1]) with
            {
                Patterns = [new PatternDef { Id = "p", Cooldown = 1f, Clip = ClipKind.Attack, Telegraph = parts[1] == "melee_slam" ? Tel : null, Actions = [Actions[parts[1]]()] }],
            };
        else
        {
            var action = Actions[parts[1]]();
            def = Enemy($"smoke_{parts[1]}_{parts[2]}", action with { Kind = PartKind.EnemyAction, Trigger = parts[2] });
        }
        var t = World(Defs([def, Minion]), weapons: true);
        var center = t.Hero.Pos;
        for (var i = 0; i < 6; i++)
            Spawn(t, def.Id, center + MathX.FromAngle(MathF.Tau * i / 6) * (60f + 30f * i), source: SpawnSource.Escort);
        t.Step(600);
        Assert.True(t.World.Tick == 600);
    }

    [Fact]
    public void Real_stage1_enemies_run_without_exception()
    {
        var defs = Sim.Wp4Data.Defs;
        foreach (var def in defs.Enemies.All)
        {
            var t = new TestWorldBuilder().WithDefs(defs).WithModules(Sim.FakeArena.Modules()).Build();
            for (var i = 0; i < 8; i++)
                EnemyFactory.Create(t.World, def, i == 0 ? EnemyRank.Elite : EnemyRank.Normal, [], t.Hero.Pos + MathX.FromAngle(i) * (80f + 30f * i),
                                    new SpawnContext(SpawnSource.Interval, EntityId.None));
            t.Step(900);
        }
    }
}
