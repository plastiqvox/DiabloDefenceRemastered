using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

/// <summary>
/// 돌진 몸통(v0.9.4, 6.20 ⑦ — 기획 8.3 V19·9.5): <c>charge</c>(일반 적)·<c>dash</c>(패턴 동작) 공용.
/// 달리는 단계에 들어갈 때 공격 클립을 처음부터 반복(<c>anim_speed</c>)하고, 틱마다 클립을 돌린다(행동 중에는 EnemyThink 8번이 클립을 돌리지 않음).
/// 닿음은 지나간 길: 지난 틱 자리(<c>ActionState.From</c>) → 지금 자리 선분과 주인공 발의 거리 ≤ 두 반경 + 4 이면 한 번(<c>ActionState.Hit</c>, 돌진 하나에 한 번) —
/// 빠른 돌진(440px/s = 틱 7.3px)이 주인공을 한 틱에 건너뛰어도 맞는다. 원인은 <c>DamageCause.Body</c>(<see cref="EnemyCtx.HitHero"/>).
/// </summary>
public static class DashBody
{
    /// <summary>닿는 거리 여유(px, 두 반경 위).</summary>
    public const float Slack = 4f;

    /// <summary>달리기 시작: 공격 클립 반복, 지난 자리 = 지금 자리, 닿음 지움.</summary>
    public static void Start(in EnemyCtx ctx, ref ActionState action)
    {
        var self = ctx.Self;
        self.Anim.Play(ClipKind.Attack, loop: true, self.Def.AnimSpeed > 0f ? self.Def.AnimSpeed : 1f);
        action.From = self.Pos;
        action.Hit = false;
    }

    /// <summary>달리는 틱 하나: 클립 진행 + 지나간 길 닿음(한 번).</summary>
    public static void Step(in EnemyCtx ctx, ref ActionState action, int damage)
    {
        var self = ctx.Self;
        ctx.AdvanceClip();
        var reach = ctx.Contact + Slack;
        if (!action.Hit && !ctx.World.Hero.Dead && MathX.DistanceSqToSegment(ctx.HeroPos, action.From, self.Pos) <= reach * reach)
        {
            action.Hit = true;
            ctx.HitHero(damage, action.Dir, 0f);
        }
        action.From = self.Pos;
    }
}

// ── dash ───────────────────────────────────────────────────────────

/// <summary>꼬리 탄 <c>{ "projectile", "every", "speed", "damage", "range" }</c> — 지난 꼬리 자리에서 <c>every</c> 이상 움직였으면 양옆(±90°) 한 발씩.</summary>
public sealed record DashTailParams
{
    public required DefRef<ProjectileDef> Projectile { get; init; }
    public float Every { get; init; } = 48f;
    public float Speed { get; init; }
    public float? Damage { get; init; }
    /// <summary>0 = 정의 사거리.</summary>
    public float Range { get; init; }
}

/// <summary>끝 내려찍기 <c>{ "radius", "damage", "effect", "telegraph" }</c> — 마지막 돌진의 줄 예고가 시작될 때 끝 자리에 원 예고 + 폭발.</summary>
public sealed record DashEndSlamParams
{
    public float Radius { get; init; } = 70f;
    public float? Damage { get; init; }
    /// <summary>터질 때 그림.</summary>
    public EffectRef? Effect { get; init; }
    /// <summary>원 예고 그림(없으면 그리기 기본 붉은 원).</summary>
    public DefRef<EffectDef>? Telegraph { get; init; }
}

public sealed record DashParams
{
    public float Speed { get; init; } = 300f;
    public float Time { get; init; } = 0.8f;
    public float? Damage { get; init; }
    public TelegraphDef? Telegraph { get; init; }
    /// <summary>돌진 수(v0.9.4).</summary>
    public int Chain { get; init; } = 1;
    /// <summary>둘째부터 돌진 앞에 서서 줄 예고하는 시간(초).</summary>
    public float ChainPause { get; init; } = 0.45f;
    /// <summary>둘째부터 그때 주인공 쪽으로 다시 노림.</summary>
    public bool Reaim { get; init; } = true;
    public DashTailParams? Tail { get; init; }
    public DashEndSlamParams? EndSlam { get; init; }
}

/// <summary>
/// 보스 돌진(여러 틱 동작 — <c>actions</c> 의 마지막 하나, 다른 여러 틱 동작과 같이 못 둠, 6.20 ⑦): 직선, 지나간 길에 닿으면 돌진마다 한 번 피해(<see cref="DashBody"/>),
/// 시간 또는 막힐 때까지(0.1s 뒤 <c>MoveBlockedTime &gt; 0.05</c> 면 그 돌진만 끝). <c>chain</c> 이면 사이에 <c>chain_pause</c> 동안 서서 줄 예고(<c>reaim</c> 이면 그때 주인공 쪽),
/// <c>tail</c> 이면 양옆 꼬리 탄, <c>end_slam</c> 이면 마지막 돌진의 줄 예고가 시작되는 틱에 끝 자리(<c>PlacementResolver.Throw</c>)에 원 예고 + 폭발.
/// 첫 돌진의 방향은 패턴이 줄 예고를 그린 방향(<c>ActionState.Dir</c> — 패턴 시작 틱의 주인공 쪽)이다 — 보이는 길과 달리는 길이 같게.
/// 상태: <c>Count</c> = 몇 번째, <c>Sub</c> = 0 줄 예고 / 1 달림, <c>T</c>, <c>Point</c> = 지난 꼬리 자리, <c>From</c> = 지난 틱 자리, <c>Hit</c>.
/// </summary>
public sealed class Dash(DashParams p) : IEnemyAction, IDamageInfo
{
    /// <summary>끝 내려찍기 예고가 돌진 끝보다 더 남는 시간(초, 6.20 ⑦).</summary>
    public const float SlamAfter = 0.15f;

    public DashParams Params => p;
    public float? BaseDamage => p.Damage;
    public float? AreaRadius => null;
    public bool MultiTick => true;
    public bool OwnsState => true;
    /// <summary>한 번 달리는 거리(px).</summary>
    public float Length => p.Speed * p.Time;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        action.Count = 0;
        action.Sub = 1;
        action.T = 0f;
        action.Dir = MathX.NormalizeOr(action.Dir, ctx.DirToHero);
        action.Point = ctx.Self.Pos;
        DashBody.Start(ctx, ref action);
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref ActionState action)
    {
        var self = ctx.Self;
        action.T += ctx.Dt;
        if (action.Sub == 0)
        {
            self.Intent = MoveIntent.Hold;
            if (action.T + 1e-4f < p.ChainPause) return ActionStatus.Running;
            action.Sub = 1;
            action.T = 0f;
            action.Point = self.Pos;
            DashBody.Start(ctx, ref action);
        }

        self.Intent = MoveIntent.Straight(action.Dir, p.Speed / MathF.Max(1f, self.MoveSpeed));
        DashBody.Step(ctx, ref action, ctx.FinalDamage(p.Damage));
        Tail(ctx, ref action);
        if (action.T + 1e-4f < p.Time && !(action.T > 0.1f && self.MoveBlockedTime > 0.05f)) return ActionStatus.Running;

        if (action.Count + 1 >= Math.Max(1, p.Chain)) return ActionStatus.Done;
        // 다음 돌진: 서서 줄 예고(그때 주인공 쪽), 마지막이면 끝 자리 원 예고도
        action.Count++;
        action.Sub = 0;
        action.T = 0f;
        if (p.Reaim) action.Dir = ctx.DirToHero;
        self.Intent = MoveIntent.Hold;
        self.Anim.Play(ClipKind.Idle, loop: true);
        LineTelegraph(ctx, self.Pos, action.Dir, p.ChainPause);
        if (action.Count == p.Chain - 1) SpawnEndSlam(ctx, self.Pos, action.Dir, p.ChainPause);
        return ActionStatus.Running;
    }

    /// <summary>돌진 길 줄 예고(폭 = 몸 지름 — 예고 <c>Size</c> 는 반폭).</summary>
    public void LineTelegraph(in EnemyCtx ctx, Vector2 from, Vector2 dir, float time)
    {
        var self = ctx.Self;
        var effect = p.Telegraph is { } t ? new EffectRef { Effect = t.Effect } : null;
        ctx.World.Spawner.SpawnTelegraph(new TelegraphSpawn(effect, TelegraphShape.Line, from, from + dir * Length, self.Radius, time, Faction.Enemy, self.Id));
    }

    /// <summary>
    /// 끝 내려찍기(6.20 ⑦): 끝 자리 = <c>PlacementResolver.Throw(nav, from, dir, speed × time, 몸 반경)</c> 에 원 예고 + <c>BlastSpec</c>.
    /// 예고 시간 = 남은 줄 예고 + 돌진 시간 + 0.15 — 보스가 막혀 먼저 멈춰도 원 자리에서 터진다.
    /// </summary>
    public void SpawnEndSlam(in EnemyCtx ctx, Vector2 from, Vector2 dir, float lineLeft)
    {
        if (p.EndSlam is not { } s) return;
        var self = ctx.Self;
        var w = ctx.World;
        var at = w.Nav is { } nav ? PlacementResolver.Throw(nav, from, dir, Length, self.Radius) : from + dir * Length;
        var effect = s.Telegraph is { IsBound: true } t ? new EffectRef { Effect = t } : null;
        w.Spawner.SpawnTelegraph(new TelegraphSpawn(effect, TelegraphShape.Circle, at, at, s.Radius, lineLeft + p.Time + SlamAfter, Faction.Enemy, self.Id,
                                                    new BlastSpec(s.Radius, ctx.FinalDamage(s.Damage), s.Effect)));
    }

    /// <summary>꼬리 탄: 지난 꼬리 자리에서 <c>every</c> 이상 움직였으면 지금 자리에서 양옆 한 발씩(생성 금지 규칙).</summary>
    private void Tail(in EnemyCtx ctx, ref ActionState action)
    {
        if (p.Tail is not { } t || !t.Projectile.IsBound) return;
        var self = ctx.Self;
        if (Vector2.DistanceSquared(self.Pos, action.Point) < t.Every * t.Every) return;
        action.Point = self.Pos;
        var def = t.Projectile.Def;
        var side = new Vector2(-action.Dir.Y, action.Dir.X);
        var damage = ctx.FinalDamage(t.Damage);
        var a = EnemyShots.FireClear(ctx, def, self.Pos, side, damage, action.SightGated, t.Speed, t.Range);
        var b = EnemyShots.FireClear(ctx, def, self.Pos, -side, damage, action.SightGated, t.Speed, t.Range);
        if (a || b) EnemyShots.Cue(ctx, def);
    }

}
