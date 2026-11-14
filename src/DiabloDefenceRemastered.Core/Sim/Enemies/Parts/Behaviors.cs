using DiabloDefenceRemastered.Core.Sim.Director;

namespace DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

// 일반 적 행동 부품 (설계서 6.5·6.6, 기획 8.3·13.3 — 매개변수 이름은 WP4 가 정한다, 11.3). 레지스트리 종류 EnemyBehavior.
// ground_hazard 는 동작(EnemyAction)으로 한 번만 등록하고 behaviors 안에서는 trigger 로 감싼다(6.5).

// ── chase_melee ────────────────────────────────────────────────────

public sealed record ChaseMeleeParams
{
    public float Cooldown { get; init; } = 1f;
    /// <summary>기본 피해(없으면 그 적의 damage).</summary>
    public float? Damage { get; init; }
    /// <summary>공격을 시작하는 거리 = 두 반경 + 이 값(기획 8.1 — 6).</summary>
    public float Reach { get; init; } = 6f;
    /// <summary>타격 프레임에 맞는 거리 = 시작 거리 + 이 값(기획 8.1 — 10).</summary>
    public float HitSlack { get; init; } = 10f;
    /// <summary>타격 프레임(1부터, 없으면 그림 묶음 attack 클립 값).</summary>
    public int? EventFrame { get; init; }
}

/// <summary>추적·근접(기획 8.1·8.3): 사거리 안이면 멈춰 공격 클립, 타격 프레임에 아직 사거리 + 10 안이면 피해. 쿨은 공격 시작부터.</summary>
public sealed class ChaseMelee(ChaseMeleeParams p) : IAbilityPart, IMeleeReach, IDamageInfo
{
    public ChaseMeleeParams Params => p;
    public float ReachOverContact => p.Reach;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        if (slot.Cooldown > 0f || ctx.DistToHero > ctx.Contact + p.Reach) return false;
        var cd = ctx.Cooldown(p.Cooldown);
        slot.Cooldown = cd;
        action.Dir = ctx.DirToHero;
        ctx.PlayClip(ClipKind.Attack, cd);
        return true;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var step = ctx.AdvanceClip(p.EventFrame ?? 0);
        if ((step & AnimStep.EventFrame) != 0 && ctx.DistToHero <= ctx.Contact + p.Reach + p.HitSlack)
            ctx.HitHero(ctx.FinalDamage(p.Damage));
        return (step & AnimStep.Finished) != 0 ? ActionStatus.Done : ActionStatus.Running;
    }
}

// ── shoot_projectile ───────────────────────────────────────────────

public sealed record ShootProjectileParams
{
    public required DefRef<ProjectileDef> Projectile { get; init; }
    public ClipKind Clip { get; init; } = ClipKind.Attack;
    public float Cooldown { get; init; } = 2.5f;
    public float CooldownJitter { get; init; }
    public float Range { get; init; } = 320f;
    public float? Damage { get; init; }
    public int? EventFrame { get; init; }
}

/// <summary>
/// 투사체 쏘기(6.6 시야 조건): 거리·쿨·투사체 길(<c>ProjectileLineClear</c>)에 더해 또렷이 보일 때만 시작하고,
/// 발사 프레임에 한 번 더 보고 거짓이면 그 발사만 버린다(클립은 끝까지, 쿨은 씀 — v0.5.3).
/// </summary>
public sealed class ShootProjectile(ShootProjectileParams p) : IAbilityPart, IDamageInfo
{
    /// <summary>다중 사격 탄 사이 각(라디안, 12°).</summary>
    public const float MultiShotStep = 12f * MathF.PI / 180f;

    public ShootProjectileParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        if (slot.Cooldown > 0f || ctx.DistToHero > p.Range) return false;
        if (!ctx.World.Nav.ProjectileLineClear(ctx.Self.Pos, ctx.HeroPos) || !ctx.SeenClearly) return false;
        var jitter = p.CooldownJitter > 0f ? ctx.Rng.Range(-p.CooldownJitter, p.CooldownJitter) : 0f;
        var cd = ctx.Cooldown(MathF.Max(0.1f, p.Cooldown + jitter));
        slot.Cooldown = cd;
        action.Dir = ctx.DirToHero;
        action.SightGated = true;
        ctx.PlayClip(p.Clip, cd);
        return true;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var step = ctx.AdvanceClip(p.EventFrame ?? 0);
        if ((step & AnimStep.EventFrame) != 0 && ctx.SeenClearly && ctx.World.Nav.ProjectileLineClear(ctx.Self.Pos, ctx.HeroPos))
        {
            // 다중 사격 접두사(2026-11-14, 기획 9.3): 1 + extra_shots 발을 주인공 쪽 가운데로 MultiShotStep 간격 부채꼴
            var n = 1 + ctx.Self.ExtraShots;
            var dir = ctx.DirToHero;
            var damage = ctx.FinalDamage(p.Damage);
            if (n == 1) EnemyShots.Fire(ctx, p.Projectile.Def, dir, damage, sightGated: true);
            else
            {
                var baseAngle = MathF.Atan2(dir.Y, dir.X);
                for (var i = 0; i < n; i++)
                    EnemyShots.Fire(ctx, p.Projectile.Def, MathX.FromAngle(baseAngle + (i - (n - 1) * 0.5f) * MultiShotStep), damage, sightGated: true);
            }
        }
        return (step & AnimStep.Finished) != 0 ? ActionStatus.Done : ActionStatus.Running;
    }
}

/// <summary>적 투사체 생성 한 곳(신호 <c>EnemyShot</c> 포함).</summary>
public static class EnemyShots
{
    public static void Fire(in EnemyCtx ctx, ProjectileDef def, Vector2 dir, int damage, bool sightGated, float speed = 0f)
    {
        var w = ctx.World;
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(def, ctx.Self.Pos, dir, damage, ctx.Self.Id, EntityId.Hero, speed, sightGated));
        Cue(ctx, def);
    }

    /// <summary>
    /// 생성 금지를 지키는 발사(v0.9.4, 6.20 ① — 기획 38.1 공정 장치 ③): 자리가 주인공 발에서 <c>rules.enemies.barrage.spawn_clear</c> 안이면 그 탄만 만들지 않고 거짓.
    /// <c>barrage</c>·<c>dash</c> 꼬리 탄이 쓴다. <paramref name="range"/> 0 = 정의 사거리. 신호는 <paramref name="cue"/> 일 때만(탄막은 파도마다 한 번 — <see cref="Cue"/>).
    /// </summary>
    public static bool FireClear(in EnemyCtx ctx, ProjectileDef def, Vector2 pos, Vector2 dir, int damage, bool sightGated, float speed = 0f, float range = 0f,
                                 bool cue = false)
    {
        var w = ctx.World;
        var clear = w.Defs.Rules.Enemies?.Barrage.SpawnClear ?? 48f;
        if (Vector2.DistanceSquared(pos, w.Hero.Pos) < clear * clear) return false;
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(def, pos, dir, damage, ctx.Self.Id, EntityId.Hero, speed, sightGated, range));
        if (cue) Cue(ctx, def);
        return true;
    }

    /// <summary>신호 <c>EnemyShot</c>(투사체 <c>fire</c> 소리).</summary>
    public static void Cue(in EnemyCtx ctx, ProjectileDef def)
    {
        var w = ctx.World;
        w.Presentation.Cue(new CueEvent(w.Tick, CueKind.EnemyShot, ctx.Self.Pos, def, ctx.Self.Id));
    }
}

// ── kite ───────────────────────────────────────────────────────────

public sealed record KiteParams
{
    /// <summary>[최소, 최대] 거리.</summary>
    public required float[] RangeBand { get; init; }
}

/// <summary>
/// 거리 유지(6.6 v0.5.1 띠 당기기): L = 그 방향으로 또렷이 보이는 가장 먼 거리, 최대' = min(최대, L − 10), 최소' = min(최소, 최대' − 40).
/// ① 또렷이 보이지 않거나 거리 > 최대' → 다가감 ② 거리 &lt; 최소' → 물러남(0.5s 못 움직이면 멈춤) ③ 그 밖 → 멈춤(쏘기는 능력 부품).
/// 상태가 바뀔 때마다 <c>Fairness.KiteSwitches</c> + 1. 슬롯 <c>Counter</c> = 지난 상태 + 1(0 = 처음).
/// </summary>
public sealed class Kite(KiteParams p) : IPositioningPart
{
    public const int Seek = 1, Away = 2, Hold = 3;
    /// <summary>물러날 수 없다고 보는 막힘 시간(초, 기획 8.3).</summary>
    public const float BlockedSeconds = 0.5f;
    /// <summary>띠 경계 되돌이 여유(px).</summary>
    public const float Hysteresis = 20f;
    public KiteParams Params => p;
    public float Max => p.RangeBand.Length > 1 ? p.RangeBand[1] : p.RangeBand[0];
    public float Min => p.RangeBand.Length > 0 ? p.RangeBand[0] : 0f;

    public bool TryPosition(in EnemyCtx ctx, ref PartSlot slot, out MoveIntent intent)
    {
        var self = ctx.Self;
        var w = ctx.World;
        var d = ctx.DistToHero;
        var reach = w.Visibility.ClearReach(self.Pos - ctx.HeroPos, self.BodyTop);
        var maxP = MathF.Min(Max, reach - 10f);
        var minP = MathF.Min(Min, maxP - 40f);
        // 물러나다 0.5s 못 움직였으면 "물러날 수 없음" — 띠 최소 안쪽에 있는 동안 제자리(슬롯 Timer 가 그 표시, 띠 최소 밖으로 나가면 풂)
        if (slot.Counter == Away && self.MoveBlockedTime >= BlockedSeconds) slot.Timer = 1f;
        if (d >= minP) slot.Timer = 0f;
        // 되돌이 여유(WP4 구현 기록): 다가가는 중이면 띠 최대 − 20 까지, 물러나는 중이면 띠 최소 + 20 까지 계속한다 —
        // 주인공이 적보다 빨리 멀어질 때 띠 경계에서 매 틱 다가감·멈춤을 오가지 않게(띠 폭 ≥ 40 이라 가운데에 닿는다)
        var seekTo = slot.Counter == Seek ? maxP - Hysteresis : maxP;
        var awayTo = slot.Counter == Away ? minP + Hysteresis : minP;
        // 띠 최소 안쪽인데 또렷이 보이지 않으면(벽 모서리 뒤) 멈춘다: ①(안 보이면 다가감)을 먼저 따르면 1px 다가가 보이게 됨 → ② 물러남 → 1px 물러나 안 보임 → 다가감을
        // 매 틱 되풀이했다(씨앗 7 의 작은 방 — kite_switches 3921 > kite 적 초 2577). 너무 가까우니 다가가지 않고, 안 보이니 쏘지도 않는다.
        var seen = ctx.SeenClearly;
        int state;
        if (d < awayTo && slot.Timer <= 0f) state = seen ? Away : Hold;
        else if (!seen || d > seekTo) state = Seek;
        else state = Hold;

        if (slot.Counter != 0 && slot.Counter != state) w.Fairness.KiteSwitches++;
        slot.Counter = state;
        intent = state switch
        {
            Seek => MoveIntent.SeekHero(),
            Away => MoveIntent.Away(ctx.HeroPos),
            _ => MoveIntent.Hold,
        };
        return true;
    }
}

// ── self_destruct ──────────────────────────────────────────────────

public sealed record SelfDestructParams
{
    /// <summary>두 반경 + 이 값 안이면 점화(기획 8.3 — 10).</summary>
    public float TriggerRange { get; init; } = 10f;
    /// <summary>점화 번쩍임 시간 = 예고(13.5-13).</summary>
    public float Fuse { get; init; } = 0.6f;
    public float Radius { get; init; } = 60f;
    public float? Damage { get; init; }
    public EffectRef? Effect { get; init; }
    public EffectRef? FizzleEffect { get; init; }
}

/// <summary>
/// 자폭(기획 8.3): 닿으면 멈추고 fuse 동안 번쩍임(<c>Visual.ForceShown</c>) → 반경 안 주인공 피해·폭발 그림 → 자기는 사라진다.
/// 점화 중에 죽으면 폭발하지 않고 <c>fizzle_effect</c> 만(<see cref="Interrupt"/>).
/// 터진 적은 <see cref="KillCause.Culled"/> 로 없앤다 — 보상·시체·죽음 훅 없음(자기 폭발로 경험치를 주지 않게, WP4 구현 기록).
/// </summary>
public sealed class SelfDestruct(SelfDestructParams p) : IAbilityPart, IDamageInfo
{
    public SelfDestructParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => p.Radius;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        if (ctx.DistToHero > ctx.Contact + p.TriggerRange) return false;
        action.T = 0f;
        action.Dir = ctx.DirToHero;
        ctx.Self.Igniting = true;
        return true;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        action.T += ctx.Dt;
        if (action.T < p.Fuse) return ActionStatus.Running;
        var self = ctx.Self;
        var w = ctx.World;
        if (ctx.DistToHero <= p.Radius + w.Hero.Radius) ctx.HitHero(ctx.FinalDamage(p.Damage));
        EnemyFx.Effect(w, p.Effect, self.Pos, p.Radius);
        self.Igniting = false;
        self.Dead = true;
        self.KillCause = KillCause.Culled;
        self.KilledBy = self.Id;
        return ActionStatus.Done;
    }

    public void Interrupt(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action, InterruptReason why)
    {
        if (!ctx.Self.Igniting) return;
        ctx.Self.Igniting = false;
        if (why == InterruptReason.Death) EnemyFx.Effect(ctx.World, p.FizzleEffect, ctx.Self.Pos, 0f);
    }
}

/// <summary>이펙트 표시 이벤트 도우미(정의가 연결된 것만).</summary>
public static class EnemyFx
{
    public static void Effect(World w, EffectRef? fx, Vector2 pos, float radius)
    {
        if (fx is null || !fx.Effect.IsBound || !w.Presentation.Enabled) return;
        var def = fx.Effect.Def;
        w.Presentation.Effect(new FxEvent(w.Tick, def, pos, fx.Scale ?? def.Scale ?? 1f, fx.Tint ?? def.Tint, radius));
    }
}

// ── charge ─────────────────────────────────────────────────────────

public sealed record ChargeParams
{
    public float[] RangeBand { get; init; } = [0f, 260f];
    public TelegraphDef? Telegraph { get; init; }
    public float Speed { get; init; } = 280f;
    public float Time { get; init; } = 1f;
    public float Stun { get; init; } = 0.8f;
    public float Cooldown { get; init; } = 4f;
    public float? Damage { get; init; }
}

/// <summary>
/// 돌진(기획 8.3·6.6): 길 트임·거리 띠·또렷이 보임이면 예고(제자리 붉은 번쩍임 + 바닥 선) → 직선 돌진(시간 또는 막힐 때까지) → 멍함.
/// 돌진 중 안 보이게 되어도 끊지 않는다(시작 조건일 뿐). v0.9.4(V19, 6.20 ⑦): 달리는 동안 공격 클립을 반복하고 지나간 길로 닿음을 본다(<see cref="DashBody"/>, 돌진 하나에 한 번).
/// </summary>
public sealed class Charge(ChargeParams p) : IAbilityPart, IDamageInfo
{
    public ChargeParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        if (slot.Cooldown > 0f) return false;
        var d = ctx.DistToHero;
        if (d < p.RangeBand[0] || d > (p.RangeBand.Length > 1 ? p.RangeBand[1] : p.RangeBand[0])) return false;
        var self = ctx.Self;
        var w = ctx.World;
        if (!w.Nav.HasClearLine(self.Pos, ctx.HeroPos, self.Radius)) return false;
        var seen = ctx.SeenClearly;
        if (!seen) return false;
        slot.Cooldown = ctx.Cooldown(p.Cooldown);
        action.Stage = 0;
        action.T = 0f;
        action.Hit = false;
        action.Dir = ctx.DirToHero;
        self.ChargeWarn = true;
        if (p.Telegraph is { } tel && tel.Time > 0f && w.Spawner is not null)
            w.Spawner.SpawnTelegraph(new TelegraphSpawn(new EffectRef { Effect = tel.Effect }, TelegraphShape.Line, self.Pos,
                                                        self.Pos + action.Dir * (p.Speed * p.Time), self.Radius * 2f, tel.Time, Faction.Enemy, self.Id));
        return true;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var self = ctx.Self;
        action.T += ctx.Dt;
        switch (action.Stage)
        {
            case 0:
                self.Intent = MoveIntent.Hold;
                if (action.T >= (p.Telegraph?.Time ?? 0f))
                {
                    action.Stage = 1;
                    action.T = 0f;
                    self.ChargeWarn = false;
                    DashBody.Start(ctx, ref action);
                }
                return ActionStatus.Running;
            case 1:
                self.Intent = MoveIntent.Straight(action.Dir, p.Speed / MathF.Max(1f, self.MoveSpeed));
                DashBody.Step(ctx, ref action, ctx.FinalDamage(p.Damage));
                if (action.T >= p.Time || (action.T > 0.1f && self.MoveBlockedTime > 0.05f))
                {
                    action.Stage = 2;
                    action.T = 0f;
                    self.Life = LifeState.Stunned;
                    self.Anim.Play(ClipKind.Idle, loop: true);
                }
                return ActionStatus.Running;
            default:
                self.Intent = MoveIntent.Hold;
                if (action.T < p.Stun) return ActionStatus.Running;
                self.Life = LifeState.Active;
                return ActionStatus.Done;
        }
    }

    public void Interrupt(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action, InterruptReason why)
    {
        ctx.Self.ChargeWarn = false;
        if (ctx.Self.Life == LifeState.Stunned) ctx.Self.Life = LifeState.Active;
    }
}

// ── command_aura ───────────────────────────────────────────────────

public sealed record CommandAuraParams
{
    public float Radius { get; init; } = 200f;
    public string? TargetTag { get; init; }
    public required DefRef<BuffDef> Buff { get; init; }
    /// <summary>참이면 정의 rank miniboss·boss 에게도 건다(기본 거짓 — 6.5, MVP 데이터에 참 없음).</summary>
    public bool IncludeBosses { get; init; }
    /// <summary>참이면 자기에게도 건다(우두머리 오라 접두사 — 디아2 오라처럼 우두머리도 받고 발밑 고리가 보임, 2026-11-14).</summary>
    public bool IncludeSelf { get; init; }
}

/// <summary>지휘 오라: 반경 안 태그 적(<c>include_self</c> 가 아니면 자신 제외)에게 2틱짜리 버프를 매 틱 다시 붙인다 — 벗어나면 저절로 사라짐.</summary>
public sealed class CommandAura(CommandAuraParams p) : IAuraPart
{
    public CommandAuraParams Params => p;

    public void Tick(in EnemyCtx ctx, ref PartSlot slot) =>
        EnemyBuffs.ApplyArea(ctx, p.Radius, p.TargetTag, p.Buff, 2, p.IncludeBosses, includeSelf: p.IncludeSelf);
}

/// <summary>범위 버프 한 곳(오라·함성·buff_area). include_bosses 거짓이면 정의 rank miniboss·boss 를 건너뛴다.</summary>
public static class EnemyBuffs
{
    public static void ApplyArea(in EnemyCtx ctx, float radius, string? tag, DefRef<BuffDef> buff, int ticks, bool includeBosses, bool includeSelf)
    {
        var w = ctx.World;
        var index = w.Defs.Buffs.IndexOf(buff.Id);
        if (index < 0) return;
        var def = w.Defs.Buffs[index];
        // 공간 해시를 쓰지 않는다: EnemyThink(5)는 해시를 다시 짓기(Locomotion 6·SpatialRebuild 8) 전이라 해시의 밀집 인덱스가 앞 틱 EndTick 압축 전 것이다(4.8 — 그 틱에만 유효).
        // 앞 틱에 적이 지워졌으면 범위 밖 인덱스가 된다(단계 2 해골 대장 오라가 실제 판에서 처음 부르며 드러남). 오라·함성은 드물어 저장소를 한 번 훑는다(결정적 — 밀집 순서).
        var end = w.Tick + ticks;
        var r2 = radius * radius;
        foreach (var e in w.Enemies.Items)
        {
            if (!e.IsAlive || Vector2.DistanceSquared(e.Pos, ctx.Self.Pos) > r2) continue;
            if (!includeSelf && e.Id == ctx.Self.Id) continue;
            if (!includeBosses && e.Def.Rank != DefRank.Normal) continue;
            if (tag is not null && !HasTag(e.Def, tag)) continue;
            e.Buffs.Apply(def, index, end, 1f, e.Immunities);
        }
    }

    public static bool HasTag(EnemyDef def, string tag)
    {
        foreach (var t in def.Tags)
            if (string.Equals(t, tag, StringComparison.Ordinal)) return true;
        return false;
    }
}

// ── escort ─────────────────────────────────────────────────────────

public sealed record EscortParams
{
    public int DefaultCount { get; init; } = 8;
    public required DefRef<EnemyDef> EscortEnemy { get; init; }
    public float FormationRadius { get; init; } = 120f;
    public float BreakRange { get; init; } = 250f;
    public float FearOnLeaderDeath { get; init; } = 1.5f;
}

/// <summary>
/// 호위(기획 8.3·6.6): 출처가 <see cref="SpawnSource.Escort"/>(이벤트)일 때만 호위를 데려온다. 수 = <c>EscortCount ≥ 0</c> 이면 그 값, 아니면 <c>default_count</c>.
/// 호위는 대장 둘레 대형 자리(<c>formation_radius</c>)를 따르고(<c>Follow</c>), 주인공이 <c>break_range</c> 안에 들거나 대장이 죽으면 푼다.
/// 대장이 <c>Damage</c> 로 죽으면 그 대장을 따르던 호위만 공포(<c>DeathHooks</c>).
/// </summary>
public sealed class Escort(EscortParams p) : ISpawnHookPart
{
    public EscortParams Params => p;

    public void OnSpawn(in EnemyCtx ctx, in SpawnContext spawn)
    {
        if (spawn.Source != SpawnSource.Escort) return;
        var count = spawn.EscortCount >= 0 ? spawn.EscortCount : p.DefaultCount;
        if (count <= 0 || !p.EscortEnemy.IsBound) return;
        var w = ctx.World;
        var leader = ctx.Self;
        var def = p.EscortEnemy.Def;
        count = SpawnGate.Reserve(w, SpawnSource.EscortMember, count);
        var detour = w.Defs.Rules.Spawn?.PathDetour ?? 1.5f;
        var radius = EnemyFactory.BodyRadius(w, def, EnemyRank.Normal);
        for (var i = 0; i < count; i++)
        {
            var offset = MathX.FromAngle(MathF.Tau * i / count) * p.FormationRadius;
            if (!EnemyPlacement.TryPlace(w.Nav, leader.Pos, leader.Pos + offset, radius, new Nav.PlaceReach(leader.Pos, detour), out var pos)) continue;
            var m = EnemyFactory.Create(w, def, EnemyRank.Normal, [], pos, new SpawnContext(SpawnSource.EscortMember, leader.Id));
            m.Leader = leader.Id;
            m.FollowBreak = p.BreakRange;
            m.Order = new MoveOrder { Kind = OrderKind.Follow, Leader = leader.Id, Offset = offset };
        }
    }
}

// ── split_on_death ─────────────────────────────────────────────────

public sealed record SplitOnDeathParams
{
    public required DefRef<EnemyDef> Spawn { get; init; }
    public int Count { get; init; } = 3;
    public float Throw { get; init; } = 80f;
    public float Delay { get; init; } = 0.3f;
    /// <summary>분열체가 등급을 물려받는가(기본 거짓 — 정예에서 나와도 일반, 6.7).</summary>
    public bool InheritRank { get; init; }
}

/// <summary>분열(기획 8.3): 죽은 자리에서 바깥으로 튕겨(장애물에서 멈춤) <c>delay</c> 뒤부터 행동. 출처 Split, 분열·소환 상한 360.</summary>
public sealed class SplitOnDeath(SplitOnDeathParams p) : IDeathHookPart
{
    public SplitOnDeathParams Params => p;

    public void OnDeath(in EnemyCtx ctx)
    {
        if (!p.Spawn.IsBound) return;
        var w = ctx.World;
        var self = ctx.Self;
        var def = p.Spawn.Def;
        var rank = p.InheritRank ? self.Rank : EnemyRank.Normal;
        var count = SpawnGate.Reserve(w, SpawnSource.Split, p.Count);
        var radius = EnemyFactory.BodyRadius(w, def, rank);
        var start = w.Rng.Ai.Angle();
        var wake = w.Tick + MathX.SecondsToTicks(p.Delay, World.Dt);
        for (var i = 0; i < count; i++)
        {
            var dir = MathX.FromAngle(start + MathF.Tau * i / Math.Max(1, p.Count));
            var pos = EnemyPlacement.Throw(w.Nav, self.Pos, dir, p.Throw, radius);
            var child = EnemyFactory.Create(w, def, rank, [], pos, new SpawnContext(SpawnSource.Split, self.Id));
            child.WakeTick = wake;
        }
    }
}

// ── panic_on_ally_death ────────────────────────────────────────────

public sealed record PanicOnAllyDeathParams
{
    public float Radius { get; init; } = 150f;
    public float Fear { get; init; } = 0.8f;
    public float Rearm { get; init; } = 1.5f;
    public bool SameDef { get; init; } = true;
}

/// <summary>
/// 동료 죽음 공포(기획 8.3): 지난 틱 <c>EnemyKilled</c>(<c>Cause == Damage</c>) 중 같은 정의·반경 안이 있으면 <c>Flee(fear)</c>, 다시 겁먹기까지 <c>rearm</c>.
/// 정예·우두머리는 겁먹지 않는다.
/// </summary>
public sealed class PanicOnAllyDeath(PanicOnAllyDeathParams p) : IAuraPart
{
    public PanicOnAllyDeathParams Params => p;

    public void Tick(in EnemyCtx ctx, ref PartSlot slot)
    {
        var self = ctx.Self;
        var w = ctx.World;
        if (self.Rank != EnemyRank.Normal || w.Tick < self.PanicRearmTick) return;
        var r2 = p.Radius * p.Radius;
        foreach (ref readonly var k in w.Events.Previous.EnemyKilled.AsSpan())
        {
            if (k.Cause != KillCause.Damage || k.Id == self.Id) continue;
            if (p.SameDef && k.DefIndex != self.DefIndex) continue;
            if (Vector2.DistanceSquared(k.Pos, self.Pos) > r2) continue;
            self.Order = new MoveOrder { Kind = OrderKind.Flee, UntilTick = w.Tick + MathX.SecondsToTicks(p.Fear, World.Dt) };
            self.PanicRearmTick = w.Tick + MathX.SecondsToTicks(p.Rearm, World.Dt);
            return;
        }
    }
}

// ── phase_through ──────────────────────────────────────────────────

public sealed record PhaseThroughParams
{
    /// <summary>장애물 위 알파(기본 0.3, 핀드 1.0).</summary>
    public float AlphaOver { get; init; } = 0.3f;
}

/// <summary>장애물 통과(기획 8.3): 주인공 쪽 직선(안쪽 장애물만 무시 — 충돌은 WP2 가 <c>PassThroughObstacles</c> 로), 장애물 위 알파.</summary>
public sealed class PhaseThrough(PhaseThroughParams p) : IPositioningPart
{
    public PhaseThroughParams Params => p;

    public bool TryPosition(in EnemyCtx ctx, ref PartSlot slot, out MoveIntent intent)
    {
        intent = MoveIntent.SeekPoint(ctx.HeroPos);
        return true;
    }
}

// ── heavy_strike ───────────────────────────────────────────────────

public sealed record HeavyStrikeParams
{
    public float Knockback { get; init; } = 40f;
}

/// <summary>강타 넉백(기획 8.3): 이 적이 내는 피해 요청에 주인공 밀어내기를 더한다.</summary>
public sealed class HeavyStrike(HeavyStrikeParams p) : IHitModifierPart
{
    public HeavyStrikeParams Params => p;

    public void ModifyOutgoing(in EnemyCtx ctx, ref DamageRequest request)
    {
        if (request.Amount <= 0f) return;
        request = request with { KnockbackDir = ctx.DirToHero, KnockbackDist = MathF.Max(request.KnockbackDist, p.Knockback) };
    }
}

// ── war_cry ────────────────────────────────────────────────────────

public sealed record WarCryParams
{
    public float Cooldown { get; init; } = 15f;
    public ClipKind Clip { get; init; } = ClipKind.Special;
    public float Radius { get; init; } = 250f;
    public required DefRef<BuffDef> Buff { get; init; }
    public float Duration { get; init; } = 6f;
    public bool IncludeBosses { get; init; }
    public int? EventFrame { get; init; }
}

/// <summary>전투 함성(기획 8.3): 특수 클립의 이벤트 프레임(없으면 끝)에 반경 안 적(자기 포함, 중간 보스·보스 본체 제외)에게 버프 duration.</summary>
public sealed class WarCry(WarCryParams p) : IAbilityPart
{
    public WarCryParams Params => p;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        if (slot.Cooldown > 0f) return false;
        slot.Cooldown = ctx.Cooldown(p.Cooldown);
        action.Hit = false;
        ctx.PlayClip(p.Clip);
        return true;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var step = ctx.AdvanceClip(p.EventFrame ?? 0);
        if (!action.Hit && (step & (AnimStep.EventFrame | AnimStep.Finished)) != 0)
        {
            action.Hit = true;
            EnemyBuffs.ApplyArea(ctx, p.Radius, null, p.Buff, MathX.SecondsToTicks(p.Duration, World.Dt), p.IncludeBosses, includeSelf: true);
        }
        return (step & AnimStep.Finished) != 0 ? ActionStatus.Done : ActionStatus.Running;
    }
}
