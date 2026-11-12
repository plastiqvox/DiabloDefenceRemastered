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
/// 패턴 데코레이터 (설계서 6.6·6.20 ②): 조건(<c>range_band</c>·<c>cooldown</c>·<c>hp_once</c>·<c>clear_sight</c>) → 클립 → 예고가 있으면 예고 시간 동안 클립을 이벤트 프레임 앞에서 멈춤
/// → 이벤트 프레임에서 <c>ActionState.BeginTick</c>·<c>Dir</c>(그 틱 주인공 쪽 — 돌진 패턴은 줄 예고를 그린 방향 그대로)를 쓰고 <c>actions</c> 를 순서대로 Begin
/// (<c>clear_sight</c> 면 그 프레임에 다시 보고 거짓이면 하나도 안 함) → 여러 틱 동작이 있으면 모두 Done 까지 함께 Tick(<c>barrage</c> 는 몇 개든, <c>dash</c>·<c>teleport</c> 는 마지막 하나) → 클립 끝.
/// 이벤트 프레임 = 패턴 <c>event_frame</c> → 그림 묶음 클립 값 → 없으면 클립 끝.
/// 예고 모양(<c>TelegraphDef.Shape</c>): 원 = 보스 자리(반경 = 첫 범위 동작 반경 또는 몸 반경 × 2), 줄 = 보스 → 주인공 쪽(폭 몸 지름, 길이 <c>length</c> 또는 <c>dash</c> 의 속도 × 시간).
/// </summary>
public sealed class PatternAbility : IAbilityPart
{
    /// <summary>줄 예고 기본 길이(px).</summary>
    public const float DefaultLineLength = 160f;

    private readonly IEnemyAction[] _actions;
    private readonly int[] _multi;
    private readonly bool _owns;

    public PatternAbility(PatternDef def)
    {
        Def = def;
        _actions = new IEnemyAction[def.Actions.Count];
        var multi = new List<int>();
        for (var i = 0; i < _actions.Length; i++)
        {
            _actions[i] = def.Actions[i].Instance as IEnemyAction
                ?? throw new InvalidOperationException($"패턴 '{def.Id}' 의 동작 '{def.Actions[i].Type}' 이 EnemyAction 이 아니다");
            if (_actions[i].MultiTick) multi.Add(i);
        }
        _multi = [.. multi];
        _owns = _actions.Length > 0 && _actions[^1].OwnsState;
    }

    public PatternDef Def { get; }
    public IReadOnlyList<IEnemyAction> Actions => _actions;
    public ClipKind Clip => Def.Clip ?? ClipKind.Attack;
    /// <summary>조건 키(<c>cooldown</c>·<c>hp_once</c>·<c>range_band</c>·<c>clear_sight</c>)가 하나도 없음 — 순서(<c>sequence</c>)에만 둘 수 있다(6.20 ③).</summary>
    public static bool Unconditioned(PatternDef def) => def.Cooldown is null && def.HpOnce is null && def.RangeBand is null && !def.ClearSight;

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
        action.BeginTick = ctx.Tick;
        ctx.PlayClip(Clip, Def.Cooldown is { } c ? ctx.Cooldown(c) : 0f);
        if (Def.Telegraph is { Time: > 0f } tel) Telegraph(ctx, tel, action.Dir);
        // 끝 내려찍기(돌진 하나): 줄 예고가 시작되는 지금 끝 자리 원 예고(6.20 ⑦) — 남은 줄 = 예고와 이벤트 프레임 중 늦은 쪽
        if (_actions.Length > 0 && _actions[^1] is Dash { Params: { Chain: <= 1, EndSlam: not null } } dash)
        {
            var eventT = ctx.EventTime(Clip, Def.EventFrame ?? 0) / MathF.Max(1e-3f, self.Anim.Rate);
            dash.SpawnEndSlam(ctx, self.Pos, action.Dir, MathF.Max(Def.Telegraph?.Time ?? 0f, eventT));
        }
        return true;
    }

    private void Telegraph(in EnemyCtx ctx, TelegraphDef tel, Vector2 dir)
    {
        var self = ctx.Self;
        var effect = new EffectRef { Effect = tel.Effect };
        if (tel.Shape == TelegraphShape.Line)
        {
            var length = tel.Length ?? (_actions.Length > 0 && _actions[^1] is Dash d ? d.Length : DefaultLineLength);
            ctx.World.Spawner.SpawnTelegraph(new TelegraphSpawn(effect, TelegraphShape.Line, self.Pos, self.Pos + dir * length, self.Radius, tel.Time,
                                                                Faction.Enemy, self.Id));
            return;
        }
        var radius = 0f;
        foreach (var a in _actions)
            if (a is IDamageInfo { AreaRadius: { } r }) { radius = r; break; }
        if (radius <= 0f) radius = self.Radius * 2f;
        ctx.World.Spawner.SpawnTelegraph(new TelegraphSpawn(effect, TelegraphShape.Circle, self.Pos, self.Pos, radius, tel.Time, Faction.Enemy, self.Id));
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var self = ctx.Self;
        switch (action.Stage)
        {
            case 0:
            {
                action.T += ctx.Dt;   // 예고 시간(이벤트 뒤의 T 는 상태를 쓰는 여러 틱 동작 — dash·teleport — 의 것)
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
                        // 6.20 ②: 그 틱 주인공 쪽(예고 동안 움직였으면 지금 자리) — 돌진은 줄 예고를 그린 방향 그대로
                        action.BeginTick = ctx.Tick;
                        if (!_owns || _actions[^1] is not Dash) action.Dir = ctx.DirToHero;
                        for (var i = 0; i < _actions.Length; i++)
                        {
                            action.ActionIndex = (sbyte)i;
                            _actions[i].Begin(ctx, ref action);
                        }
                        action.Stage = _multi.Length > 0 ? (byte)1 : (byte)2;
                    }
                }
                if (action.Stage != 1 && (step & AnimStep.Finished) != 0) return ActionStatus.Done;
                return ActionStatus.Running;
            }
            case 1:
            {
                // 반복 클립(돌진 몸통)은 동작이 돌린다
                if (self.Anim.Clip == Clip && !self.Anim.Done && !self.Anim.Loop) ctx.AdvanceClip(Def.EventFrame ?? 0);
                var running = false;
                foreach (var i in _multi)
                {
                    action.ActionIndex = (sbyte)i;
                    if (_actions[i].Tick(ctx, ref action) == ActionStatus.Running) running = true;
                }
                if (running) return ActionStatus.Running;
                action.Stage = 2;
                return self.Anim.Done || self.Anim.Loop || _owns ? ActionStatus.Done : ActionStatus.Running;
            }
            default:
            {
                var step = ctx.AdvanceClip(Def.EventFrame ?? 0);
                return (step & AnimStep.Finished) != 0 || self.Anim.Done ? ActionStatus.Done : ActionStatus.Running;
            }
        }
    }

    public void Interrupt(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action, InterruptReason why)
    {
        if (action.Stage != 1) return;
        foreach (var i in _multi) _actions[i].Interrupt(ctx, ref action);
    }
}

/// <summary>
/// 정해진 순서(v0.9.4, 6.20 ③ — 기획 9.5): 페이즈마다 하나, 순서가 있는 페이즈의 능력 목록 맨 뒤에 붙고 모든 페이즈의 시퀀서가 슬롯 하나를 나눠 쓴다
/// (순서 패턴은 능력 목록·슬롯에 들지 않는다). 슬롯 <c>Counter</c> = 다음 칸, <c>Cooldown</c> = 남은 쉼(쿨 배율 없음 — 정해진 박자).
/// <see cref="TryBegin"/>: 쉼 0 이면 그 칸 패턴의 <c>TryBegin</c>(조건이 없어 늘 시작). <see cref="Tick"/>: 안쪽 패턴(같은 슬롯·같은 행동 상태 — 안쪽 패턴은 슬롯 값을 쓰지 않음),
/// Done 이면 쉼 = 그 칸 <c>wait</c>, 다음 칸. 쉼 동안은 독점 행동이 없어 이동·순서 밖 패턴(내려치기)이 그대로 돈다.
/// </summary>
public sealed class PatternSequence(SequenceDef def, PatternAbility[] steps) : IAbilityPart
{
    public SequenceDef Def => def;
    public IReadOnlyList<PatternAbility> Steps => steps;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        if (slot.Cooldown > 0f || steps.Length == 0) return false;
        var k = Index(slot);
        var inner = new PartSlot();
        return steps[k].TryBegin(ctx, ref inner, ref action);
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        var k = Index(slot);
        var inner = new PartSlot();
        if (steps[k].Tick(ctx, ref inner, ref action) == ActionStatus.Running) return ActionStatus.Running;
        slot.Cooldown = def.Steps[k].Wait;
        slot.Counter = (k + 1) % steps.Length;
        return ActionStatus.Done;
    }

    public void Interrupt(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action, InterruptReason why)
    {
        if (steps.Length == 0) return;
        var inner = new PartSlot();
        steps[Index(slot)].Interrupt(ctx, ref inner, ref action, why);
    }

    private int Index(in PartSlot slot) => steps.Length == 0 ? 0 : ((slot.Counter % steps.Length) + steps.Length) % steps.Length;
}

/// <summary>
/// 페이즈 전환 (설계서 6.6·6.20 ④): HP 비율이 다음 페이즈 <c>hp_below</c> 밑이면 지금 쓰는 목록 번호(<see cref="Enemy.PhaseIdx"/>)만 바꾸고
/// 전환 연출(무적·흔들림·발광·속도, <c>voice</c> 면 신호 <c>BossPhase</c>). 진행 중 행동은 끊는다. 페이즈가 있는 정의에만 저절로 붙는다(EnemyThink 3번).
/// 시퀀서 슬롯을 지우고 쉼 = 새 페이즈 <c>sequence.start</c>(처음부터), <c>clear_projectiles</c> 면 이 적이 쏜 적 투사체를 모두 지운다(지운 자리 앞 <c>clear_fx_max</c> 곳에 그 탄의 맞음 그림).
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
            if (self.Kit.SequencerSlot >= 0)
                self.Parts[self.Kit.SequencerSlot] = new PartSlot { Cooldown = ph.Sequence?.Start ?? 0f };
            if (ph.ClearProjectiles) ClearProjectiles(w, self);
            if (ph.Invulnerable > 0f) self.InvulnUntilTick = Math.Max(self.InvulnUntilTick, w.Tick + MathX.SecondsToTicks(ph.Invulnerable, World.Dt));
            if (ph.Shake is { } s) w.Presentation.Shake(new ShakeEvent(w.Tick, s.Px, s.Duration));
            if (ph.Glow is { } g) self.PhaseGlow = g;
            if (ph.Speed is { } sp) self.PhaseSpeed = sp;
            if (ph.Voice) w.Presentation.Cue(new CueEvent(w.Tick, CueKind.BossPhase, self.Pos, self.Def, self.Id));
        }
    }

    /// <summary>이 적이 쏜 적 투사체를 모두 지운다(<c>on_end</c> 장판 없이). 앞 <c>clear_fx_max</c> 곳에 맞음 그림.</summary>
    public static int ClearProjectiles(World w, Enemy self)
    {
        var max = w.Defs.Rules.Enemies?.Barrage.ClearFxMax ?? 24;
        var store = w.Projectiles;
        var n = 0;
        for (var i = 0; i < store.Count; i++)
        {
            var p = store[i];
            if (p.Removed || p.Faction != Faction.Enemy || p.Owner != self.Id) continue;
            if (n < max) EnemyFx.Effect(w, p.Def.HitEffect, p.Pos, 0f);
            store.RemoveAt(i);
            n++;
        }
        return n;
    }
}
