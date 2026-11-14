namespace DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

// 우두머리 접두사 부품 (2026-11-14 디아2식 정예·우두머리, 기획 9.3 — 번개 강화·흡혈·순간이동). 레지스트리 종류 EnemyBehavior.
// 불꽃 강화(죽음 폭발)·냉기 강화(죽음 서리 고리)는 기존 동작을 on_death 로 감싸고, 오라는 command_aura 를 쓴다(Data/affixes.json).

// ── spark_on_hurt ──────────────────────────────────────────────────

public sealed record SparkOnHurtParams
{
    public required DefRef<ProjectileDef> Projectile { get; init; }
    public int Count { get; init; } = 3;
    /// <summary>한 번 튄 뒤 다시 튀기까지(초). 주인공 무기가 초당 여러 번 맞히므로 꼭 둔다.</summary>
    public float Cooldown { get; init; } = 0.5f;
    /// <summary>기본 피해(없으면 그 적의 damage) — 등급·시간 배율을 거친다.</summary>
    public float? Damage { get; init; }
    public float Speed { get; init; }
}

/// <summary>
/// 번개 강화(기획 9.3, 디아2 "맞으면 전기 튐"): 맞을 때(쿨 지나면) 몸에서 사방으로 탄 <c>count</c> 개(첫 방향은 난수, 고르게 돌림 + 조금 흔듦).
/// 생성 금지를 지킨다(<see cref="EnemyShots.FireClear"/> — 주인공 발 48px 안이면 그 탄만 안 만듦).
/// </summary>
public sealed class SparkOnHurt(SparkOnHurtParams p) : IHurtHookPart
{
    public SparkOnHurtParams Params => p;

    public void OnHurt(in EnemyCtx ctx, ref PartSlot slot, in HitLanded hit)
    {
        if (slot.Cooldown > 0f || !p.Projectile.IsBound || hit.Applied <= 0) return;
        slot.Cooldown = p.Cooldown;
        var damage = ctx.FinalDamage(p.Damage);
        var n = Math.Max(1, p.Count);
        var start = ctx.Rng.Angle();
        for (var i = 0; i < n; i++)
        {
            var a = start + MathF.Tau * i / n + ctx.Rng.Range(-0.35f, 0.35f);
            EnemyShots.FireClear(ctx, p.Projectile.Def, ctx.Self.Pos, MathX.FromAngle(a), damage, sightGated: false, p.Speed, cue: i == 0);
        }
    }
}

// ── life_steal ─────────────────────────────────────────────────────

public sealed record LifeStealParams
{
    /// <summary>주인공에게 실제로 들어간 피해의 몇 배를 회복(1 = 100%).</summary>
    public float Pct { get; init; } = 1f;
}

/// <summary>흡혈(기획 9.3): 지난 틱 주인공에게 들어간 피해 × pct 만큼 회복(최대 체력까지). 근접·탄 모두(<c>HitLanded.Attacker</c> = 쏜 적).</summary>
public sealed class LifeSteal(LifeStealParams p) : IHitModifierPart
{
    public LifeStealParams Params => p;

    public void ModifyOutgoing(in EnemyCtx ctx, ref DamageRequest request) { }

    public void OnHitLanded(in EnemyCtx ctx, in HitLanded hit)
    {
        if (hit.Target != EntityId.Hero || hit.Applied <= 0) return;
        var self = ctx.Self;
        self.Hp = MathF.Min(self.MaxHp, self.Hp + hit.Applied * p.Pct);
    }
}

// ── teleport_ability ───────────────────────────────────────────────

public sealed record TeleportAbilityParams
{
    public float Cooldown { get; init; } = 7f;
    /// <summary>주인공 거리 [최소, 최대] 안일 때만(가까이 붙어 있으면 안 씀).</summary>
    public float[] RangeBand { get; init; } = [200f, 900f];
    /// <summary>순간이동 동작 매개변수(보스 <c>teleport</c> 와 같음 — 나타날 자리 예고 원 필수).</summary>
    public TeleportParams Teleport { get; init; } = new();
}

/// <summary>
/// 순간이동 접두사(기획 9.3, 디아2 Teleportation): 쿨이 지났고 주인공이 거리 띠 안이면 보스 순간이동(<see cref="Parts.Teleport"/>)을 독점 행동으로 —
/// 사라짐 → 주인공 둘레 min~max 자리 예고 원 → 나타남(그동안 무적). 패턴(클립) 없이 바로 시작한다.
/// </summary>
public sealed class TeleportAbility(TeleportAbilityParams p) : IAbilityPart
{
    private readonly Teleport _teleport = new(p.Teleport);

    public TeleportAbilityParams Params => p;

    public bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action)
    {
        if (slot.Cooldown > 0f || ctx.DistToHero < p.RangeBand[0] || ctx.DistToHero > p.RangeBand[1]) return false;
        slot.Cooldown = ctx.Cooldown(p.Cooldown);
        action.BeginTick = ctx.Tick;
        _teleport.Begin(ctx, ref action);
        return true;
    }

    public ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action) => _teleport.Tick(ctx, ref action);

    public void Interrupt(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action, InterruptReason why) => _teleport.Interrupt(ctx, ref action);
}
