using DiabloDefenceRemastered.Core.Sim.Weapons;

namespace DiabloDefenceRemastered.Core.Sim.Projectiles;

/// <summary>주인공 무기의 투사체·장판 만들기 한 곳(발사 순간 damage_pct·치명 확률 복사 — 5.2, 안전 상한 — 5.4).</summary>
public static class HeroShots
{
    /// <summary>주인공 투사체 안전 상한 기본값(<c>rules.caps.hero_projectiles</c> 가 없을 때).</summary>
    public const int DefaultHeroCap = 400;
    /// <summary>수명·사거리가 모두 없는 투사체의 안전 수명(초).</summary>
    public const float FallbackLifetime = 10f;

    public static Projectile Spawn(World world, int slotIndex, WeaponDef weapon, in WeaponStats stats, Vector2 origin, Vector2 dir, float speed)
    {
        Cap(world, Faction.Hero, world.Defs.Rules.Caps?.HeroProjectiles ?? DefaultHeroCap);
        var def = FirePatterns.ProjectileOf(weapon);
        var p = world.Projectiles.Add();
        p.Def = def;
        p.Faction = Faction.Hero;
        p.Pos = p.PrevPos = origin;
        p.Speed = speed;
        p.Vel = dir * speed;
        p.Radius = def.Radius;
        p.Tint = def.Tint;
        p.Scale = def.Scale * stats.AreaMul;
        p.Age = 0f;
        var life = stats.Lifetime > 0f ? stats.Lifetime : def.Lifetime ?? float.PositiveInfinity;
        p.RangeLeft = def.Range ?? float.PositiveInfinity;
        if (float.IsPositiveInfinity(life) && float.IsPositiveInfinity(p.RangeLeft)) life = FallbackLifetime;
        p.TimeLeft = p.Duration = life;
        p.Owner = EntityId.None;
        p.WeaponSlot = (sbyte)slotIndex;
        p.SpawnTick = world.Tick;
        p.Damage = stats.Damage;
        p.DamagePct = stats.DamagePct;
        p.CritChance = stats.CritChance;
        p.Knockback = stats.Knockback;
        p.Weapon = weapon;
        return p;
    }

    /// <summary>주인공 쪽 장판(지옥불 장판·진화 <c>ground_pool</c> 효과). 틱은 만든 다음 틱부터.</summary>
    public static Hazard Pool(World world, int slotIndex, EffectRef? effect, Vector2 pos, float radius, float duration, float tickInterval,
                              float tickDamage, float damagePct, float critChance, float effectScale = 1f)
    {
        var h = world.Hazards.Add();
        h.Effect = effect?.Effect.Def;
        h.Tint = effect?.Tint ?? effect?.Effect.Def.Tint ?? Rgb.White;
        h.EffectScale = (effect?.Scale ?? 1f) * effectScale;
        h.Faction = Faction.Hero;
        h.Pos = h.PrevPos = pos;
        h.Radius = radius;
        h.Age = 0f;
        h.TimeLeft = h.Duration = duration;
        h.Owner = EntityId.None;
        h.SpawnTick = world.Tick;
        h.TickInterval = MathF.Max(World.Dt, tickInterval);
        h.NextTickIn = 0f;
        h.TickDamage = tickDamage;
        h.DamagePct = damagePct;
        h.CritChance = critChance;
        h.WeaponSlot = (sbyte)slotIndex;
        return h;
    }

    /// <summary>그 진영 투사체가 상한 이상이면 가장 오래된 것(밀집 순서 앞)부터 지운다.</summary>
    internal static void Cap(World world, Faction faction, int cap)
    {
        var store = world.Projectiles;
        var n = 0;
        for (var i = 0; i < store.Count; i++)
            if (!store[i].Removed && store[i].Faction == faction) n++;
        for (var i = 0; i < store.Count && n >= cap; i++)
        {
            var p = store[i];
            if (p.Removed || p.Faction != faction || p.Motion == MotionKind.Orbit) continue;
            store.RemoveAt(i);
            n--;
        }
    }
}
