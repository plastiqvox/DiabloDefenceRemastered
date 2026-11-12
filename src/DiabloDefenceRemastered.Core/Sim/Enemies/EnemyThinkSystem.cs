using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 5단계 <c>EnemyThink</c> (설계서 3.5·6.6 "매 틱 EnemyThink 순서"). 적 하나마다:
/// 1 생애 상태(입장·등장·분열 대기) → 2 지난 틱 HitLanded → 3 페이즈·광폭화·지속 효과 → 4 공포 → 5 진행 중 행동 → 6 능력 TryBegin(슬롯 순서)
/// → 7 이동 의도(지시 → 위치 잡기 → SeekHero) → 8 이동 속도·바라보는 방향·애니메이션·표시 상태.
/// 생긴 틱에는 행동하지 않는다(<c>SpawnTick &lt; Tick</c> 부터, 3.5). <c>--freeze-ai</c> 면 모두 멈춤.
/// v0.8 사냥 모드: 방이 정리 단계면 일반 적은 공포·위치 잡기(<c>kite</c>)를 끄고 주인공에게 간다(중간 보스·보스 본체는 해당 없음).
/// </summary>
public sealed class EnemyThinkSystem : ISimSystem
{
    /// <summary>입장이 끝났다고 보는 진입점까지 거리(px).</summary>
    public const float EntryReach = 8f;
    /// <summary>행진 목표 진입점에서 풀리는 거리(px, 6.6).</summary>
    public const float MarchRelease = 48f;
    /// <summary>호위가 대형 자리에 닿았다고 보는 거리(px) — 그 안이면 선다(wp4 단계 2).</summary>
    public const float FollowArrive = 12f;

    public void Tick(World world)
    {
        var enemies = world.Enemies;
        var count = enemies.Count;
        var freeze = world.Options.FreezeAi;
        for (var i = 0; i < count; i++)
        {
            var e = enemies[i];
            if (!e.IsAlive || e.SpawnTick >= world.Tick) continue;
            if (freeze)
            {
                e.Intent = MoveIntent.Hold;
                continue;
            }
            Think(world, e);
        }
    }

    public static void Think(World world, Enemy e)
    {
        var ctx = new EnemyCtx(world, e);
        var kit = e.Kit;

        // 1 생애 상태
        switch (e.Life)
        {
            case LifeState.Entering:
            {
                var to = e.EntryTarget - e.Pos;
                if (to.LengthSquared() <= EntryReach * EntryReach)
                {
                    e.Life = LifeState.Active;
                    if (e.SpawnHooksPending) EnemyFactory.RunSpawnHooks(world, e, e.PendingSpawn);
                    break;
                }
                e.Intent = MoveIntent.Straight(Vector2.Normalize(to));
                Finish(ctx, moving: true);
                return;
            }
            case LifeState.Emerging:
                if (world.Tick < e.WakeTick)
                {
                    e.Intent = MoveIntent.Hold;
                    Finish(ctx, moving: false);
                    return;
                }
                e.Life = LifeState.Active;
                if (e.SpawnHooksPending) EnemyFactory.RunSpawnHooks(world, e, e.PendingSpawn);
                break;
        }
        if (world.Tick < e.WakeTick)
        {
            e.Intent = MoveIntent.Hold;
            Finish(ctx, moving: false);
            return;
        }

        // 2 지난 틱 자기가 때린 것
        if (kit.HitModifiers.Length > 0)
            foreach (ref readonly var hit in world.Events.Previous.HitLanded.AsSpan())
            {
                if (hit.Attacker != e.Id) continue;
                foreach (var s in kit.HitModifiers)
                    if (kit.Part(e.PhaseIdx, s) is IHitModifierPart m) m.OnHitLanded(ctx, hit);
            }

        // 3 페이즈·광폭화·지속 효과·쿨다운
        PhaseController.Step(ctx);
        if (e.EnrageAt >= 0 && world.Tick >= e.EnrageAt && e.Def.Enrage is { } enrage)
        {
            e.EnrageDamageMul = enrage.DamageMul;
            e.EnrageSpeedMul = enrage.SpeedMul;
        }
        for (var s = 0; s < kit.Count; s++)
        {
            ref var slot = ref e.Parts[s];
            if (slot.Cooldown > 0f) slot.Cooldown = MathF.Max(0f, slot.Cooldown - World.Dt);
        }
        foreach (var s in kit.Auras)
            if (kit.Part(e.PhaseIdx, s) is IAuraPart aura) aura.Tick(ctx, ref e.Parts[s]);

        var hunting = world.Room is { Phase: RoomPhase.Cleanup } && e.Def.Rank == DefRank.Normal;

        // 4 공포
        if (e.Order.Kind == OrderKind.Flee)
        {
            if (hunting || world.Tick >= e.Order.UntilTick) e.Order = default;
            else
            {
                Interrupt(ctx, InterruptReason.Fear);
                e.Intent = MoveIntent.Away(world.Hero.Pos);
                Finish(ctx, moving: true);
                return;
            }
        }

        // 5 진행 중 행동
        if (e.Action.Active)
        {
            e.Intent = MoveIntent.Hold;
            if (kit.Part(e.PhaseIdx, e.Action.PartIndex) is IAbilityPart running)
            {
                if (running.Tick(ctx, ref e.Parts[e.Action.PartIndex], ref e.Action) == ActionStatus.Done)
                {
                    e.Action = ActionState.None;
                    if (e.Life == LifeState.Stunned) e.Life = LifeState.Active;
                }
            }
            else e.Action = ActionState.None;
        }
        else
        {
            // 6 능력(슬롯 순서 — 처음 시작한 것이 독점)
            var abilities = kit.Abilities[Math.Min(e.PhaseIdx, kit.Abilities.Length - 1)];
            foreach (var s in abilities)
            {
                if (kit.Part(e.PhaseIdx, s) is not IAbilityPart ability) continue;
                var state = ActionState.None;
                if (!ability.TryBegin(ctx, ref e.Parts[s], ref state)) continue;
                state.PartIndex = (sbyte)s;
                e.Action = state;
                e.Intent = MoveIntent.Hold;
                break;
            }
        }
        if (e.Dead) return;   // 자폭 등이 이번 틱에 끝냄
        if (e.Life == LifeState.Emerging)
        {
            // 보스 순간이동(6.20 ⑧)이 이번 틱에 숨김 — 나타날 때까지 1번이 세워 둔다
            e.Intent = MoveIntent.Hold;
            Finish(ctx, moving: false);
            return;
        }

        // 7 이동 의도
        if (!e.Action.Active)
        {
            e.Intent = Movement(ctx, hunting);
            Finish(ctx, moving: e.Intent.Kind != MoveKind.Hold);
        }
        else Finish(ctx, moving: e.Intent.Kind is MoveKind.Straight);
    }

    /// <summary>독점 행동 없을 때의 이동 의도: 지시(행진·따르기) → 위치 잡기 부품 → SeekHero.</summary>
    private static MoveIntent Movement(in EnemyCtx ctx, bool hunting)
    {
        var e = ctx.Self;
        var w = ctx.World;
        switch (e.Order.Kind)
        {
            case OrderKind.March:
            {
                var done = w.Tick >= e.Order.UntilTick;
                if (!done && e.Order.Target is { } target)
                {
                    var to = target - e.Pos;
                    if (to.LengthSquared() <= MarchRelease * MarchRelease || Vector2.Dot(e.Order.Dir, to) <= 0f) done = true;
                }
                if (!done) return MoveIntent.Heading(e.Order.Dir);
                e.Order = default;
                break;
            }
            case OrderKind.Follow:
            {
                var leader = w.Enemies.Get(e.Order.Leader);
                if (leader is null || !leader.IsAlive || ctx.DistToHero <= e.FollowBreak) e.Order = default;
                else
                {
                    // 대형 자리가 벽·맵 밖이면(작은 방 — 단계 2) 대장 자리로 모인다: 닿지 못할 자리를 향해 벽에 붙어 멈춰 있지 않게.
                    // 자리에 닿았으면(FollowArrive 안) 선다 — 대장이 서 있을 때 "쫓는데 안 움직임" 이 아니라 대형을 지키는 것이다
                    var slot = leader.Pos + e.Order.Offset;
                    if (w.Nav is { } nav && (!nav.IsInsideMap(slot) || nav.Clearance(slot) < e.Radius)) slot = leader.Pos;
                    if (Vector2.DistanceSquared(e.Pos, slot) <= FollowArrive * FollowArrive) return MoveIntent.Hold;
                    return MoveIntent.SeekPoint(slot);
                }
                break;
            }
        }
        if (!hunting || e.Def.Rank != DefRank.Normal)
        {
            var kit = e.Kit;
            foreach (var s in kit.Positioning)
                if (kit.Part(e.PhaseIdx, s) is IPositioningPart pos && (!hunting || pos is not Kite) && pos.TryPosition(ctx, ref e.Parts[s], out var intent))
                    return intent;
        }
        else
        {
            // 사냥 모드에도 날기(phase_through)는 그대로
            var kit = e.Kit;
            foreach (var s in kit.Positioning)
                if (kit.Part(e.PhaseIdx, s) is PhaseThrough pt && pt.TryPosition(ctx, ref e.Parts[s], out var intent)) return intent;
        }
        return MoveIntent.SeekHero();
    }

    /// <summary>진행 중 행동을 끊는다(공포·죽음).</summary>
    public static void Interrupt(in EnemyCtx ctx, InterruptReason why)
    {
        var e = ctx.Self;
        if (!e.Action.Active) return;
        if (e.Kit.Part(e.PhaseIdx, e.Action.PartIndex) is IAbilityPart running)
            running.Interrupt(ctx, ref e.Parts[e.Action.PartIndex], ref e.Action, why);
        e.Action = ActionState.None;
        e.Igniting = false;
        e.ChargeWarn = false;
        if (e.Life == LifeState.Stunned) e.Life = LifeState.Active;
    }

    /// <summary>8 이동 속도·바라보는 방향·애니메이션·표시 상태.</summary>
    private static void Finish(in EnemyCtx ctx, bool moving)
    {
        var e = ctx.Self;
        var w = ctx.World;
        var speedPct = e.AffixSpeedPct + ctx.BuffPct(EnemyStatKey.SpeedPct);
        var baseSpeed = e.PhaseSpeed > 0f ? e.PhaseSpeed * (EnemyFactory.RankOf(w, e.Rank)?.Speed ?? 1f) : e.BaseMoveSpeed;
        e.MoveSpeed = MathF.Max(0f, baseSpeed * (1f + speedPct) * e.EnrageSpeedMul);

        // 바라보는 방향
        var face = e.Action.Active ? e.Action.Dir : e.Intent.Kind switch
        {
            MoveKind.Heading or MoveKind.Straight => e.Intent.Dir,
            MoveKind.Away => e.Pos - w.Hero.Pos,
            MoveKind.SeekPoint => e.Intent.Point - e.Pos,
            _ => w.Hero.Pos - e.Pos,
        };
        if (e.Action.Active && e.Action.Dir.LengthSquared() < 1e-6f) face = w.Hero.Pos - e.Pos;
        e.Facing = MathX.NormalizeOr(face, e.Facing);

        // 행동 중이 아닐 때의 애니메이션(행동 클립은 부품이 진행)
        if (!e.Action.Active)
        {
            var set = ctx.Sprites;
            if (moving)
            {
                var rate = AnimClock.WalkRate(e.Vel.LengthSquared() > 1e-4f ? e.Vel : e.Facing * e.MoveSpeed, set.BaseSpeed);
                e.Anim.Continue(ClipKind.Walk, loop: true, MathF.Max(0.1f, rate) * (e.Def.AnimSpeed > 0f ? e.Def.AnimSpeed : 1f));
            }
            else e.Anim.Continue(ClipKind.Idle, loop: true, e.Def.AnimSpeed > 0f ? e.Def.AnimSpeed : 1f);
            var (frameTime, frames, _) = ctx.ClipInfo(e.Anim.Clip);
            if (frames > 0) e.Anim.Advance(World.Dt, frameTime, frames);
        }

        Visual(ctx);
    }

    /// <summary>표시 상태(6.6): 정의 발광·알파 → 등급 발광(정예 맥동) → 접두사 발광 색 > 등급 색 → 페이즈 발광 → 부품(점화·돌진 예고·장애물 위 알파) → 발밑 원.</summary>
    public static void Visual(in EnemyCtx ctx)
    {
        var e = ctx.Self;
        var w = ctx.World;
        var def = e.Def;
        var v = new EnemyVisual { AlphaMul = def.Alpha > 0f ? def.Alpha : 1f };
        if (def.Glow is { } g) { v.GlowColor = g.Color; v.GlowAlpha = g.Alpha; }

        var rank = EnemyFactory.RankOf(w, e.Rank);
        if (rank is not null)
        {
            v.GlowColor = rank.Glow;
            if (rank.Pulse is { Period: > 0f, Alpha.Length: >= 2 } pulse)
            {
                var t = (w.Tick * World.Dt % pulse.Period) / pulse.Period;
                var k = 0.5f - 0.5f * MathF.Cos(t * MathF.Tau);
                v.GlowAlpha = MathX.Lerp(pulse.Alpha[0], pulse.Alpha[1], k);
            }
            else v.GlowAlpha = MathF.Max(v.GlowAlpha, 0.35f);
        }
        var affix0 = e.AffixIdx[0];
        if (affix0 >= 0 && w.Defs.Affixes[affix0].Glow is { } ag)
        {
            v.GlowColor = ag;
            if (v.GlowAlpha <= 0f) v.GlowAlpha = 0.35f;
        }
        if (e.PhaseGlow is { } pg) { v.GlowColor = pg.Color; v.GlowAlpha = pg.Alpha; }

        if (e.Igniting)
        {
            var k = 0.5f + 0.5f * MathF.Sin(w.Tick * 0.9f);
            v.GlowAlpha = MathX.Lerp(0.35f, 0.9f, k);
            v.ForceShown = true;
        }
        if (e.ChargeWarn)
        {
            v.FlashColor = new Rgb(255, 40, 40);
            v.FlashAlpha = 0.6f;
        }
        // 보스 순간이동(6.20 ⑧): 사라짐(1 → 0) → 숨음(0) → 깨기 전 0.15s 밝아짐(0 → 1)
        if (e.FadeOutTicks > 0 && w.Tick >= e.FadeOutTick)
            v.AlphaMul *= 1f - Math.Clamp((w.Tick - e.FadeOutTick) / (float)e.FadeOutTicks, 0f, 1f);
        if (e.HiddenUntilTick > 0 && w.Tick < e.WakeTick)
            v.AlphaMul *= w.Tick < e.HiddenUntilTick ? 0f : Math.Clamp((w.Tick - e.HiddenUntilTick) / (float)Math.Max(1, e.WakeTick - e.HiddenUntilTick), 0f, 1f);
        if (e.PassThroughObstacles && w.Nav is not null && w.Nav.Clearance(e.Pos) < 0f)
            foreach (var s in e.Kit.Positioning)
                if (e.Kit.Part(0, s) is PhaseThrough pt) v.AlphaMul *= pt.Params.AlphaOver;

        if (w.Defs.Buffs.Count > 0)
            for (var i = 0; i < BuffSlots.SlotCount; i++)
            {
                var b = e.Buffs[i];
                if (!b.Active || b.EndTick <= w.Tick) continue;
                if (w.Defs.Buffs[b.BuffIndex].FootRing is { } ring)
                {
                    v.FootRingColor = ring;
                    v.FootRingAlpha = 0.6f;
                    break;
                }
            }
        e.Visual = v;
    }
}
