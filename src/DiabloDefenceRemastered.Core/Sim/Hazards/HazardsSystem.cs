using DiabloDefenceRemastered.Core.Sim.Combat;
using DiabloDefenceRemastered.Core.Sim.Projectiles;

namespace DiabloDefenceRemastered.Core.Sim.Hazards;

/// <summary>
/// 단계 11 Hazards (WP3, 설계서 5.5): 장판 틱(주인공 쪽 = 반경 안 적, 무기 칸이 있으면 적별 타격 간격 / 적 쪽 = 주인공)·붙이는 버프·수명,
/// 예고 시간 → 끝나면 폭발(원·선). 이번 틱에 생긴 것은 다음 틱부터(3.5).
/// </summary>
public sealed class HazardsSystem : ISimSystem
{
    /// <summary>폭발·장판 피해의 밀어내기 거리 기본값(예고 폭발에 넉백이 없으면 0).</summary>
    public void Tick(World world)
    {
        TickHazards(world);
        TickTelegraphs(world);
    }

    private static void TickHazards(World world)
    {
        var store = world.Hazards;
        for (var i = 0; i < store.Count; i++)
        {
            var h = store[i];
            if (h.Removed || h.SpawnTick >= world.Tick) continue;
            h.Age += World.Dt;
            // 첫 틱은 생긴 다음 틱, 그 뒤 정확히 간격마다(확인 뒤에 줄인다 — 첫 틱이 한 틱을 더 먹지 않게)
            if (h.NextTickIn <= 1e-5f)
            {
                h.NextTickIn += h.TickInterval;
                if (h.Faction == Faction.Hero) HitEnemies(world, h);
                else HitHero(world, h);
            }
            h.NextTickIn -= World.Dt;
            h.TimeLeft -= World.Dt;
            if (h.TimeLeft <= 1e-5f) store.RemoveAt(i);
        }
    }

    private static void HitEnemies(World world, Hazard h)
    {
        var buf = world.Combat.Query;
        var n = Math.Min(world.Spatial.Query(h.Pos, h.Radius + ProjectilesSystem.MaxEnemyRadius, buf), buf.Length);
        var now = world.StageTime;
        var buffIndex = h.Buff is { } b ? world.Defs.Buffs.IndexOf(b.Id) : -1;
        for (var k = 0; k < n; k++)
        {
            var e = world.Enemies[buf[k]];
            if (Vector2.Distance(e.Pos, h.Pos) > h.Radius + e.Radius) continue;
            if (h.WeaponSlot >= 0)
            {
                ref var next = ref e.NextHitBySlot[h.WeaponSlot];
                if (now < next - 1e-5f) continue;
                next = now + h.TickInterval - World.Dt * 0.5f;   // 반 틱 여유 — 장판 틱과 적별 간격이 같을 때 틱 오차로 한 번 건너뛰지 않게
            }
            if (h.TickDamage > 0f)
                world.Damage.ToEnemy(e.Id, h.TickDamage, h.DamagePct, h.CritChance, h.WeaponSlot, default, 0f, DamageFlags.FromHazard);
            if (h.Buff is { } buff && buffIndex >= 0)
                e.Buffs.Apply(buff, buffIndex, world.Tick + Math.Max(1, MathX.SecondsToTicks(h.BuffDuration, World.Dt)), 1f, e.Immunities);
        }
    }

    private static void HitHero(World world, Hazard h)
    {
        var hero = world.Hero;
        if (hero.Dead || h.TickDamage <= 0f) return;
        if (Vector2.Distance(hero.Pos, h.Pos) > h.Radius + hero.Radius) return;
        world.Damage.ToHero(h.Owner, h.TickDamage, default, 0f, DamageFlags.FromHazard);
    }

    private static void TickTelegraphs(World world)
    {
        var store = world.Telegraphs;
        for (var i = 0; i < store.Count; i++)
        {
            var t = store[i];
            if (t.Removed || t.SpawnTick >= world.Tick) continue;
            t.Age += World.Dt;
            t.TimeLeft -= World.Dt;
            if (t.TimeLeft > 1e-5f) continue;
            if (t.Blast.Radius > 0f) Blast(world, t);
            store.RemoveAt(i);
        }
    }

    /// <summary>예고 끝 폭발: 원은 중심에서, 선은 선분에서 반경 안. 적 쪽이면 주인공, 주인공 쪽이면 적.</summary>
    private static void Blast(World world, Telegraph t)
    {
        var blast = t.Blast;
        var center = t.Shape == TelegraphShape.Line ? (t.Pos + t.End) * 0.5f : t.Pos;
        if (t.Faction == Faction.Hero)
        {
            var buf = world.Combat.Query;
            var reach = blast.Radius + (t.Shape == TelegraphShape.Line ? Vector2.Distance(t.Pos, t.End) : 0f) + ProjectilesSystem.MaxEnemyRadius;
            var n = Math.Min(world.Spatial.Query(center, reach, buf), buf.Length);
            for (var k = 0; k < n; k++)
            {
                var e = world.Enemies[buf[k]];
                if (Distance(t, e.Pos) > blast.Radius + e.Radius) continue;
                world.Damage.ToEnemy(e.Id, blast.Damage, 0f, 0f, -1, Away(e.Pos, t), blast.Knockback);
            }
        }
        else
        {
            var hero = world.Hero;
            if (!hero.Dead && Distance(t, hero.Pos) <= blast.Radius + hero.Radius)
                world.Damage.ToHero(t.Owner, blast.Damage, Away(hero.Pos, t), blast.Knockback);
        }
        if (blast.Effect is { } fx) CombatFx.Effect(world, fx, center, blast.Radius, default);
        if (blast.Shake is { } shake && world.Presentation.Enabled) world.Presentation.Shake(new ShakeEvent(world.Tick, shake.Px, shake.Duration));
    }

    private static float Distance(Telegraph t, Vector2 p) =>
        t.Shape == TelegraphShape.Line ? MathF.Sqrt(MathX.DistanceSqToSegment(p, t.Pos, t.End)) : Vector2.Distance(p, t.Pos);

    private static Vector2 Away(Vector2 p, Telegraph t)
    {
        var d = p - t.Pos;
        return d.LengthSquared() > 1e-6f ? Vector2.Normalize(d) : new Vector2(0, 1);
    }
}
