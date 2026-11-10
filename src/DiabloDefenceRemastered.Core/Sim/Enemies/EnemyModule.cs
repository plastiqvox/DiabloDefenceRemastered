using DiabloDefenceRemastered.Core.Sim.Director;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 적·감독 모듈 (소유 WP4, 설계서 11.1): 부품 등록(일반 12 + 동작 7 — <c>ground_hazard</c> 는 동작으로 한 번, 행동 안에서는 trigger 로 감쌈),
/// 이벤트 9종(<see cref="PartKind.StageEvent"/>), <c>Director</c>·<c>EnemyThink</c>·<c>DeathHooks</c> 시스템, 검사기(13.5-13·14·15·17·18·23 ⑥), 개발 옵션(<c>--stress</c>).
/// </summary>
public sealed class EnemyModule : IGameModule
{
    public void Register(ModuleContext ctx)
    {
        RegisterParts(ctx.Parts);
        ctx.AddWorldInstaller(static (world, builder) =>
        {
            builder.Add(SimPhase.Director, new DirectorSystem());
            builder.Add(SimPhase.EnemyThink, new EnemyThinkSystem());
            builder.Add(SimPhase.DeathHooks, new DeathHooksSystem());
        });
        ctx.AddValidator(new EnemyDataValidator());
        ctx.AddRunSetup(new EnemyRunSetup());
    }

    /// <summary>부품·동작·이벤트 등록(시험이 레지스트리만 쓸 때도 부른다).</summary>
    public static void RegisterParts(PartRegistry parts)
    {
        const PartKind B = PartKind.EnemyBehavior, A = PartKind.EnemyAction, E = PartKind.StageEvent;

        parts.Register<ChaseMeleeParams>(B, "chase_melee", p => new ChaseMelee(p), (p, c) => Positive(c, "cooldown", p.Cooldown));
        parts.Register<ShootProjectileParams>(B, "shoot_projectile", p => new ShootProjectile(p), (p, c) =>
        {
            Positive(c, "range", p.Range);
            Positive(c, "cooldown", p.Cooldown);
            if (p.CooldownJitter < 0f || p.CooldownJitter >= p.Cooldown) c.Error("cooldown_jitter", "0 이상 cooldown 미만이어야 한다");
        });
        parts.Register<KiteParams>(B, "kite", p => new Kite(p), (p, c) => Band(c, p.RangeBand));
        parts.Register<SelfDestructParams>(B, "self_destruct", p => new SelfDestruct(p), (p, c) => Positive(c, "radius", p.Radius));
        parts.Register<ChargeParams>(B, "charge", p => new Charge(p), (p, c) => { Band(c, p.RangeBand); Positive(c, "speed", p.Speed); });
        parts.Register<CommandAuraParams>(B, "command_aura", p => new CommandAura(p), (p, c) => Positive(c, "radius", p.Radius));
        parts.Register<EscortParams>(B, "escort", p => new Escort(p), (p, c) => { if (p.DefaultCount < 0) c.Error("default_count", "0 이상이어야 한다"); });
        parts.Register<SplitOnDeathParams>(B, "split_on_death", p => new SplitOnDeath(p), (p, c) => { if (p.Count < 1) c.Error("count", "1 이상이어야 한다"); });
        parts.Register<PanicOnAllyDeathParams>(B, "panic_on_ally_death", p => new PanicOnAllyDeath(p), (p, c) => Positive(c, "radius", p.Radius));
        parts.Register<PhaseThroughParams>(B, "phase_through", p => new PhaseThrough(p), (p, c) =>
        {
            if (p.AlphaOver is < 0f or > 1f) c.Error("alpha_over", "0~1 이어야 한다");
        });
        parts.Register<HeavyStrikeParams>(B, "heavy_strike", p => new HeavyStrike(p));
        parts.Register<WarCryParams>(B, "war_cry", p => new WarCry(p), (p, c) => { Positive(c, "radius", p.Radius); Positive(c, "cooldown", p.Cooldown); });

        parts.Register<MeleeSlamParams>(A, "melee_slam", p => new MeleeSlam(p));
        parts.Register<ProjectileSpreadParams>(A, "projectile_spread", p => new ProjectileSpread(p), (p, c) => { if (p.Count < 1) c.Error("count", "1 이상이어야 한다"); });
        parts.Register<TelegraphedBlastParams>(A, "telegraphed_blast", p => new TelegraphedBlast(p), (p, c) => Positive(c, "radius", p.Radius));
        parts.Register<DashParams>(A, "dash", p => new Dash(p), (p, c) => Positive(c, "speed", p.Speed));
        parts.Register<SummonParams>(A, "summon", p => new Summon(p), (p, c) => { if (p.Spawns.Count == 0) c.Error("spawns", "비었다"); });
        parts.Register<GroundHazardParams>(A, "ground_hazard", p => new GroundHazard(p), (p, c) => { Positive(c, "radius", p.Radius); Positive(c, "duration", p.Duration); });
        parts.Register<BuffAreaParams>(A, "buff_area", p => new BuffArea(p), (p, c) => Positive(c, "radius", p.Radius));

        parts.Register<BannerParams>(E, "banner", p => new BannerEvent(p));
        parts.Register<MarchParams>(E, "march", p => new MarchEvent(p), (p, c) =>
        {
            if (p.Count < 1) c.Error("count", "1 이상이어야 한다");
            if (p.Toward is not null && p.TowardEntry is not null) c.Error("toward", "toward 와 toward_entry 중 하나만 쓴다");
        });
        parts.Register<RingParams>(E, "ring", p => new RingEvent(p), (p, c) =>
        {
            if (p.MinRadius > p.Radius) c.Error("min_radius", "radius 보다 클 수 없다");
        });
        parts.Register<ElitePackParams>(E, "elite_pack", p => new ElitePackEvent(p));
        parts.Register<EscortEventParams>(E, "escort", p => new EscortEvent(p));
        parts.Register<BossEventParams>(E, "miniboss", p => new BossEvent(p, final: false));
        parts.Register<BossEventParams>(E, "boss", p => new BossEvent(p, final: true));
        parts.Register<LightChangeParams>(E, RoomEvents.LightChange, p => new LightChangeEvent(p));
        parts.Register<RoomSupplyParams>(E, "room_supply", p => new RoomSupplyEvent(p));
    }

    private static void Positive(ValidationContext c, string key, float value)
    {
        if (!(value > 0f)) c.Error(key, "0 보다 커야 한다");
    }

    private static void Band(ValidationContext c, float[] band)
    {
        if (band is not { Length: 2 } || band[0] < 0f || band[0] > band[1]) c.Error("range_band", "[최소, 최대](0 ≤ 최소 ≤ 최대)여야 한다");
    }
}

/// <summary>
/// <c>--stress &lt;적 수&gt;</c>(9절): 스테이지 구성 가중치대로 생성 띠·맵 전체에 즉시 채운다(출처 <see cref="SpawnSource.Stress"/> — 상한 검사 없음).
/// <c>--freeze-ai</c> 는 <c>EnemyThinkSystem</c> 이 <c>World.Options</c> 로 읽는다. 투사체 수 유지는 WP3 무기 쪽이 없어 하지 않는다(WP4 구현 기록).
/// </summary>
public sealed class EnemyRunSetup : IRunSetup
{
    public void Apply(World world, RunOptions options)
    {
        if (options.StressEnemies is not { } n || n <= 0 || world.Stage is null) return;
        var interval = DirectorSystem.CurrentInterval(world) ?? (world.Stage.SpawnIntervals.Count > 0 ? world.Stage.SpawnIntervals[0] : null);
        if (interval is null || interval.Weights.Count == 0) return;
        var rng = world.Rng.Spawn;
        var sum = 0f;
        foreach (var w in interval.Weights) sum += w.Weight;
        var size = world.MapSize;
        for (var i = 0; i < n; i++)
        {
            var x = rng.NextFloat() * sum;
            var def = interval.Weights[^1].Ref.Def;
            foreach (var w in interval.Weights)
            {
                x -= w.Weight;
                if (x < 0f) { def = w.Ref.Def; break; }
            }
            for (var t = 0; t < 16; t++)
            {
                var p = new Vector2(rng.Range(0f, size.X), rng.Range(0f, size.Y));
                if (!world.Nav.IsInsideMap(p) || world.Nav.Clearance(p) < 16f) continue;
                EnemyFactory.Create(world, def, EnemyRank.Normal, [], p, new SpawnContext(SpawnSource.Stress, EntityId.None));
                break;
            }
        }
    }
}
