using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

public sealed record TeleportParams
{
    /// <summary>나타날 자리의 주인공 거리(px).</summary>
    public float Min { get; init; } = 150f;
    public float Max { get; init; } = 210f;
    /// <summary>흐려지며 사라지는 시간(초).</summary>
    public float Vanish { get; init; } = 0.25f;
    /// <summary>나타날 자리 예고 원 시간(초) — 예고 필수(≥ <c>rules.enemies.barrage.telegraph_min</c>).</summary>
    public float Appear { get; init; } = 0.6f;
    public int Tries { get; init; } = 8;
    /// <summary>나타날 자리 원 예고 그림(없으면 그리기 기본 붉은 원).</summary>
    public DefRef<EffectDef>? Telegraph { get; init; }
    /// <summary>떠난 자리 그림.</summary>
    public EffectRef? Effect { get; init; }
}

/// <summary>
/// 보스 순간이동(v0.9.4, 6.20 ⑧ — 여러 틱, 목록의 마지막). <see cref="Begin"/>: 자리 고르기 — <c>World.Rng.Ai</c> 로 <c>tries</c> 번 주인공 + 방향 × U(min, max) 를
/// <c>PlacementResolver.TryPlace(몸 반경, 주인공에서 걸어 닿음)</c>, 주인공에서 <c>min</c> 이상인 첫 자리(없으면 제자리) → <c>ActionState.Point</c>,
/// 무적 = vanish + appear. <c>vanish</c> 동안 제자리에서 흐려짐(<c>Enemy.FadeOutTick</c>). 끝 틱: 떠난 자리 그림, 자리 옮김, <c>Life = Emerging</c>(조준·맞음·분리 안 됨),
/// <c>WakeTick</c> = appear 뒤, <c>HiddenUntilTick</c> = 그 0.15s 앞(그때부터 밝아짐), 자리에 원 예고(폭발 없음) → Done. 깰 때까지 EnemyThink 1번이 세워 둔다.
/// </summary>
public sealed class Teleport(TeleportParams p) : IEnemyAction
{
    /// <summary>나타나기 전 밝아지는 시간(초).</summary>
    public const float FadeIn = 0.15f;
    /// <summary>예고 원 반경 = 몸 반경 + 이 값.</summary>
    public const float RingPad = 30f;

    public TeleportParams Params => p;
    public bool MultiTick => true;
    public bool OwnsState => true;

    public void Begin(in EnemyCtx ctx, ref ActionState action)
    {
        var self = ctx.Self;
        action.T = 0f;
        action.Point = Pick(ctx);
        var vanish = MathX.SecondsToTicks(p.Vanish, World.Dt);
        self.FadeOutTick = ctx.Tick;
        self.FadeOutTicks = Math.Max(1, vanish);
        self.InvulnUntilTick = Math.Max(self.InvulnUntilTick, ctx.Tick + vanish + MathX.SecondsToTicks(p.Appear, World.Dt) + 1);
        self.Intent = MoveIntent.Hold;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref ActionState action)
    {
        var self = ctx.Self;
        var w = ctx.World;
        self.Intent = MoveIntent.Hold;
        if (ctx.Tick - action.BeginTick < MathX.SecondsToTicks(p.Vanish, World.Dt)) return ActionStatus.Running;

        EnemyFx.Effect(w, p.Effect, self.Pos, self.Radius);
        self.Pos = self.PrevPos = action.Point;
        self.Vel = Vector2.Zero;
        self.FadeOutTick = self.FadeOutTicks = 0;
        self.Life = LifeState.Emerging;
        self.WakeTick = ctx.Tick + MathX.SecondsToTicks(p.Appear, World.Dt);
        self.HiddenUntilTick = self.WakeTick - MathX.SecondsToTicks(FadeIn, World.Dt);
        var effect = p.Telegraph is { IsBound: true } t ? new EffectRef { Effect = t } : null;
        w.Spawner.SpawnTelegraph(new TelegraphSpawn(effect, TelegraphShape.Circle, action.Point, action.Point, self.Radius + RingPad, p.Appear, Faction.Enemy, self.Id));
        return ActionStatus.Done;
    }

    public void Interrupt(in EnemyCtx ctx, ref ActionState action)
    {
        ctx.Self.FadeOutTick = ctx.Self.FadeOutTicks = 0;
    }

    /// <summary>나타날 자리(없으면 제자리).</summary>
    private Vector2 Pick(in EnemyCtx ctx)
    {
        var self = ctx.Self;
        var w = ctx.World;
        var hero = ctx.HeroPos;
        if (w.Nav is not { } nav) return self.Pos;
        var detour = w.Defs.Rules.Spawn?.PathDetour ?? 1.5f;
        var rng = ctx.Rng;
        for (var i = 0; i < Math.Max(1, p.Tries); i++)
        {
            var dir = MathX.FromAngle(rng.Angle());
            var wish = hero + dir * rng.Range(p.Min, p.Max);
            if (!PlacementResolver.TryPlace(nav, hero, wish, self.Radius, new PlaceReach(hero, detour, FromHero: true), out var at)) continue;
            if (Vector2.DistanceSquared(at, hero) >= p.Min * p.Min - 1e-3f) return at;
        }
        return self.Pos;
    }
}
