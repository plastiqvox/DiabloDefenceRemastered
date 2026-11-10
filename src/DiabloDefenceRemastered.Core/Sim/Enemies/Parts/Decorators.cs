namespace DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

/// <summary>
/// 훅 어댑터 (설계서 6.6): <c>behaviors</c> 원소에 <c>trigger</c> 가 있으면 그 동작을 감싼다.
/// <c>on_death</c> → <see cref="IDeathHookPart"/>, <c>on_spawn</c> → <see cref="ISpawnHookPart"/>, <c>cooldown</c> → 클립 없는 <see cref="IAbilityPart"/>(그 틱에 끝나는 즉시 동작 — 독점 행동을 남기지 않음, v0.5.3).
/// </summary>
public abstract class TriggeredAction(IEnemyAction action) : IEnemyBehavior
{
    public const string TriggerOnDeath = "on_death", TriggerOnSpawn = "on_spawn", TriggerCooldown = "cooldown";
    public static IReadOnlyList<string> Triggers { get; } = [TriggerOnDeath, TriggerOnSpawn, TriggerCooldown];

    public IEnemyAction Action => action;

    public static TriggeredAction Create(string trigger, IEnemyAction action) => trigger switch
    {
        TriggerOnDeath => new DeathTrigger(action),
        TriggerOnSpawn => new SpawnTrigger(action),
        TriggerCooldown => new CooldownTrigger(action),
        _ => throw new InvalidOperationException($"모르는 trigger '{trigger}'(on_death·on_spawn·cooldown)"),
    };

    protected void Run(in EnemyCtx ctx)
    {
        var state = ActionState.None;
        action.Begin(ctx, ref state);
    }

    private sealed class DeathTrigger(IEnemyAction a) : TriggeredAction(a), IDeathHookPart
    {
        public void OnDeath(in EnemyCtx ctx) => Run(ctx);
    }

    private sealed class SpawnTrigger(IEnemyAction a) : TriggeredAction(a), ISpawnHookPart
    {
        public void OnSpawn(in EnemyCtx ctx, in SpawnContext spawn) => Run(ctx);
    }

    private sealed class CooldownTrigger(IEnemyAction a) : TriggeredAction(a), IAbilityPart
    {
        private readonly float _cooldown = (a as ITriggerCooldown)?.Cooldown ?? 0f;

        public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
        {
            if (slot.Cooldown > 0f || _cooldown <= 0f) return false;
            slot.Cooldown = ctx.Cooldown(_cooldown);
            Run(ctx);
            return false;   // 즉시 끝남 — 같은 틱의 다음 능력·이동 의도가 그대로 돈다
        }

        public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action) => ActionStatus.Done;
    }
}

/// <summary>
/// 패턴 데코레이터 (설계서 6.6): 조건(<c>range_band</c>·<c>cooldown</c>·<c>hp_once</c>·<c>clear_sight</c>) → 클립 → 예고가 있으면 예고 시간 동안 클립을 이벤트 프레임 앞에서 멈춤
/// → 이벤트 프레임에서 <c>actions</c> 를 순서대로 Begin(<c>clear_sight</c> 면 그 프레임에 다시 보고 거짓이면 하나도 안 함) → 마지막 동작이 여러 틱이면 Done 까지 Tick → 클립 끝.
/// 이벤트 프레임 = 패턴 <c>event_frame</c> → 그림 묶음 클립 값 → 없으면 클립 끝.
/// </summary>
public sealed class PatternAbility : IAbilityPart
{
    private readonly IEnemyAction[] _actions;

    public PatternAbility(PatternDef def)
    {
        Def = def;
        _actions = new IEnemyAction[def.Actions.Count];
        for (var i = 0; i < _actions.Length; i++)
            _actions[i] = def.Actions[i].Instance as IEnemyAction
                ?? throw new InvalidOperationException($"패턴 '{def.Id}' 의 동작 '{def.Actions[i].Type}' 이 EnemyAction 이 아니다");
    }

    public PatternDef Def { get; }
    public IReadOnlyList<IEnemyAction> Actions => _actions;
    public ClipKind Clip => Def.Clip ?? ClipKind.Attack;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var self = ctx.Self;
        if (Def.HpOnce is { } once)
        {
            if (slot.Fired || self.MaxHp <= 0f || self.Hp / self.MaxHp > once) return false;
        }
        else if (slot.Cooldown > 0f) return false;
        if (Def.RangeBand is { Length: > 0 } band)
        {
            var d = ctx.DistToHero;
            if (d < band[0] || (band.Length > 1 && d > band[1])) return false;
        }
        if (Def.ClearSight && !ctx.SeenClearly) return false;

        if (Def.HpOnce is not null) slot.Fired = true;
        if (Def.Cooldown is { } cd) slot.Cooldown = ctx.Cooldown(cd);
        action.Stage = 0;
        action.T = 0f;
        action.ActionIndex = -1;
        action.Dir = ctx.DirToHero;
        action.SightGated = Def.ClearSight;
        ctx.PlayClip(Clip, Def.Cooldown is { } c ? ctx.Cooldown(c) : 0f);
        if (Def.Telegraph is { Time: > 0f } tel)
        {
            var radius = 0f;
            foreach (var a in _actions)
                if (a is IDamageInfo { AreaRadius: { } r }) { radius = r; break; }
            if (radius <= 0f) radius = self.Radius * 2f;
            ctx.World.Spawner.SpawnTelegraph(new TelegraphSpawn(new EffectRef { Effect = tel.Effect }, TelegraphShape.Circle, self.Pos, self.Pos,
                                                                radius, tel.Time, Faction.Enemy, self.Id));
        }
        return true;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var self = ctx.Self;
        action.T += ctx.Dt;
        switch (action.Stage)
        {
            case 0:
            {
                var eventT = ctx.EventTime(Clip, Def.EventFrame ?? 0);
                var hasEvent = (Def.EventFrame ?? ctx.ClipInfo(Clip).EventFrame) > 0;
                // 예고 동안 이벤트 프레임 앞에서 멈춤
                if (Def.Telegraph is { Time: > 0f } tel && action.T < tel.Time && hasEvent)
                {
                    var before = MathF.Max(0f, eventT - 1e-4f);
                    if (self.Anim.Time + World.Dt * self.Anim.Rate >= before)
                    {
                        self.Anim.Time = MathF.Min(self.Anim.Time, before);
                        return ActionStatus.Running;
                    }
                }
                var step = ctx.AdvanceClip(Def.EventFrame ?? 0);
                var fire = hasEvent ? (step & AnimStep.EventFrame) != 0 : (step & AnimStep.Finished) != 0;
                if (fire)
                {
                    if (Def.ClearSight && !ctx.SeenClearly)
                    {
                        action.Stage = 2;
                    }
                    else
                    {
                        for (var i = 0; i < _actions.Length; i++)
                        {
                            action.ActionIndex = (sbyte)i;
                            _actions[i].Begin(ctx, ref action);
                        }
                        action.Stage = _actions.Length > 0 && _actions[^1].MultiTick ? (byte)1 : (byte)2;
                    }
                }
                if (action.Stage != 1 && (step & AnimStep.Finished) != 0) return ActionStatus.Done;
                return ActionStatus.Running;
            }
            case 1:
            {
                if (self.Anim.Clip == Clip && !self.Anim.Done) ctx.AdvanceClip(Def.EventFrame ?? 0);
                if (_actions[^1].Tick(ctx, ref action) == ActionStatus.Running) return ActionStatus.Running;
                action.Stage = 2;
                return self.Anim.Done ? ActionStatus.Done : ActionStatus.Running;
            }
            default:
            {
                var step = ctx.AdvanceClip(Def.EventFrame ?? 0);
                return (step & AnimStep.Finished) != 0 || self.Anim.Done ? ActionStatus.Done : ActionStatus.Running;
            }
        }
    }
}

/// <summary>
/// 페이즈 전환 (설계서 6.6): HP 비율이 다음 페이즈 <c>hp_below</c> 밑이면 지금 쓰는 목록 번호(<see cref="Enemy.PhaseIdx"/>)만 바꾸고
/// 전환 연출(무적·흔들림·발광·속도, <c>voice</c> 면 신호 <c>BossPhase</c>). 진행 중 행동은 끊는다. 페이즈가 있는 정의에만 저절로 붙는다(EnemyThink 3번).
/// </summary>
public static class PhaseController
{
    public static void Step(in EnemyCtx ctx)
    {
        var self = ctx.Self;
        var phases = self.Def.Phases;
        if (phases.Count == 0 || self.MaxHp <= 0f) return;
        var w = ctx.World;
        while (self.PhaseIdx < phases.Count && self.Hp / self.MaxHp < phases[self.PhaseIdx].HpBelow)
        {
            var ph = phases[self.PhaseIdx];
            self.PhaseIdx++;
            if (self.Action.Active)
            {
                if (self.Kit.Part(self.PhaseIdx - 1, self.Action.PartIndex) is IAbilityPart running)
                    running.Interrupt(ctx, ref self.Parts[self.Action.PartIndex], ref self.Action, InterruptReason.Phase);
                self.Action = ActionState.None;
            }
            if (ph.Invulnerable > 0f) self.InvulnUntilTick = w.Tick + MathX.SecondsToTicks(ph.Invulnerable, World.Dt);
            if (ph.Shake is { } s) w.Presentation.Shake(new ShakeEvent(w.Tick, s.Px, s.Duration));
            if (ph.Glow is { } g) self.PhaseGlow = g;
            if (ph.Speed is { } sp) self.PhaseSpeed = sp;
            if (ph.Voice) w.Presentation.Cue(new CueEvent(w.Tick, CueKind.BossPhase, self.Pos, self.Def, self.Id));
        }
    }
}
