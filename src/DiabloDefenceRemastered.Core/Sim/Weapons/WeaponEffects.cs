using DiabloDefenceRemastered.Core.Sim.Projectiles;

namespace DiabloDefenceRemastered.Core.Sim.Weapons;

/// <summary>진화 무기 추가 효과 부품(<c>PartKind.WeaponEffect</c>, 설계서 5.3·6.8). 투사체·장판이 그 순간에 목록 순서대로 부른다.</summary>
public interface IWeaponEffect
{
    void Apply(World world, Projectile source, Vector2 at);
}

/// <summary>
/// <c>ground_pool</c> 효과 매개변수(연옥불 <c>on_explode</c> — 6.8 예): 그 자리에 주인공 쪽 장판.
/// <c>area_scaled</c>·<c>duration_scaled</c> 면 주인공 <c>area_pct</c>·<c>duration_pct</c> 를 곱한다. 틱 간격 <c>tick</c>(기본 0.5).
/// </summary>
public sealed record GroundPoolEffectParams
{
    public EffectRef? Effect { get; init; }
    public float TickDamage { get; init; }
    public float Radius { get; init; }
    public float Duration { get; init; }
    public float Tick { get; init; } = 0.5f;
    public bool AreaScaled { get; init; }
    public bool DurationScaled { get; init; }
}

internal sealed class GroundPoolEffect(GroundPoolEffectParams p) : IWeaponEffect
{
    public void Apply(World world, Projectile source, Vector2 at)
    {
        var stats = world.Hero.Stats;
        var radius = p.Radius * (p.AreaScaled ? 1f + stats[StatKey.AreaPct] : 1f);
        var duration = p.Duration * (p.DurationScaled ? 1f + stats[StatKey.DurationPct] : 1f);
        HeroShots.Pool(world, source.WeaponSlot, p.Effect, at, radius, duration, p.Tick, p.TickDamage, source.DamagePct, source.CritChance);
    }
}

/// <summary>무기 효과 부품 등록·실행.</summary>
public static class WeaponEffects
{
    public static void Register(PartRegistry parts)
    {
        parts.Register<GroundPoolEffectParams>(PartKind.WeaponEffect, "ground_pool", p => new GroundPoolEffect(p), (p, ctx) =>
        {
            if (p.Radius <= 0f) ctx.Error("radius", "ground_pool 반경은 0 보다 커야 한다");
            if (p.Duration <= 0f) ctx.Error("duration", "ground_pool 지속은 0 보다 커야 한다");
            if (p.Tick <= 0f) ctx.Error("tick", "ground_pool 틱 간격은 0 보다 커야 한다");
        });
    }

    public static void Run(World world, IReadOnlyList<PartDef> effects, Projectile source, Vector2 at)
    {
        for (var i = 0; i < effects.Count; i++)
            if (effects[i].Instance is IWeaponEffect e) e.Apply(world, source, at);
    }
}
