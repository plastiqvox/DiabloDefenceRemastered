using DiabloDefenceRemastered.Core.Sim.Combat;
using DiabloDefenceRemastered.Core.Sim.Weapons;

namespace DiabloDefenceRemastered.Core.Sim.Projectiles;

/// <summary>궤적 한 곳(설계서 5.4 — <see cref="MotionKind"/> switch).</summary>
public static class ProjectileMotion
{
    public static void Move(World world, Projectile p)
    {
        var dt = World.Dt;
        switch (p.Motion)
        {
            case MotionKind.Homing:
                if (TargetPos(world, p, out var tp))
                {
                    var cur = p.Vel.LengthSquared() > 1e-6f ? Vector2.Normalize(p.Vel) : FacingOr(tp - p.Pos);
                    var want = FacingOr(tp - p.Pos);
                    var maxTurn = p.TurnRate * dt;
                    var angle = MathF.Atan2(Cross(cur, want), Vector2.Dot(cur, want));
                    var turn = Math.Clamp(angle, -maxTurn, maxTurn);
                    var (s, c) = MathF.SinCos(turn);
                    var dir = new Vector2(cur.X * c - cur.Y * s, cur.X * s + cur.Y * c);
                    p.Vel = dir * p.Speed;
                }
                p.Pos += p.Vel * dt;
                break;
            case MotionKind.Orbit:
                p.OrbitAngle += p.AngularSpeed * dt;
                PlaceOrbit(p, world.Hero.Pos);
                break;
            default:
                p.Pos += p.Vel * dt;
                break;
        }
    }

    /// <summary>궤도 자리: 주인공 + 반경 × (cos, sin), <c>Vel</c> = 접선(그림 방향, 기획 5.2 — 각이 늘면 화면에서 시계 방향).</summary>
    public static void PlaceOrbit(Projectile p, Vector2 heroPos)
    {
        var (s, c) = MathF.SinCos(p.OrbitAngle);
        p.Pos = heroPos + new Vector2(c, s) * p.OrbitRadius;
        var sign = p.AngularSpeed >= 0f ? 1f : -1f;
        p.Vel = new Vector2(-s, c) * sign * MathF.Max(1f, MathF.Abs(p.AngularSpeed) * p.OrbitRadius);
    }

    /// <summary>유도 대상 자리: 주인공 투사체는 대상 적(조준 가능할 때만), 적 투사체는 주인공(MVP 적대 대상).</summary>
    private static bool TargetPos(World world, Projectile p, out Vector2 pos)
    {
        if (p.Faction == Faction.Hero)
        {
            if (world.Enemies.TryGet(p.Target, out var t) && t.IsTargetable) { pos = t.Pos; return true; }
        }
        else if (!world.Hero.Dead) { pos = world.Hero.Pos; return true; }
        pos = default;
        return false;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static Vector2 FacingOr(Vector2 d) => d.LengthSquared() > 1e-6f ? Vector2.Normalize(d) : new Vector2(0, 1);
}

/// <summary>
/// 단계 10 Projectiles (WP3, 설계서 5.4): 이동 → 장애물(투사체 막힘 칸만, 궤도는 무시) → 맞음(캡슐, 선분 시작에서 가까운 순으로 관통 수만큼) → 사거리·수명 →
/// 끝(폭발·맞음 이펙트·<c>on_end</c> 장판 — 맞음·사거리 끝·장애물 어느 쪽이든 하나). 이번 틱에 생긴 것은 다음 틱부터 움직인다(3.5).
/// </summary>
public sealed class ProjectilesSystem : ISimSystem
{
    /// <summary>캡슐 질의에 더하는 적 반경 여유(정확한 거리는 다시 거른다).</summary>
    public const float MaxEnemyRadius = 64f;

    public void Tick(World world)
    {
        world.Combat.OwnEnemyRequestsFrom = world.Damage.Count;
        var store = world.Projectiles;
        for (var i = 0; i < store.Count; i++)
        {
            var p = store[i];
            if (p.Removed || p.SpawnTick >= world.Tick) continue;
            p.Age += World.Dt;
            var from = p.Pos;
            ProjectileMotion.Move(world, p);
            var to = p.Pos;

            var blocked = false;
            if (p.Def.BlockedByObstacles && p.Motion != MotionKind.Orbit && world.Nav is { } nav && !nav.ProjectileLineClear(from, to))
            {
                to = LastClear(nav, from, to);
                p.Pos = to;
                blocked = true;
            }

            var ended = p.Faction == Faction.Hero ? HitEnemies(world, p, from, to) : HitHero(world, p, from, to);
            if (!ended && p.Motion != MotionKind.Orbit)
            {
                p.RangeLeft -= Vector2.Distance(from, p.Pos);
                p.TimeLeft -= World.Dt;
                ended = blocked || p.RangeLeft <= 0f || p.TimeLeft <= 1e-5f;
            }
            if (ended)
            {
                End(world, p);
                store.RemoveAt(i);
            }
        }
    }

    /// <summary>장애물 앞 마지막 트인 자리(이분 6번).</summary>
    private static Vector2 LastClear(INavWorld nav, Vector2 from, Vector2 to)
    {
        float lo = 0f, hi = 1f;
        for (var k = 0; k < 6; k++)
        {
            var mid = (lo + hi) * 0.5f;
            if (nav.ProjectileLineClear(from, Vector2.Lerp(from, to, mid))) lo = mid;
            else hi = mid;
        }
        return Vector2.Lerp(from, to, lo);
    }

    /// <summary>주인공 투사체의 맞음. 끝났으면 참(그때 <c>p.Pos</c> = 맞은 자리).</summary>
    private static bool HitEnemies(World world, Projectile p, Vector2 from, Vector2 to)
    {
        var buf = world.Combat.Query;
        var enemies = world.Enemies;
        if (p.Motion == MotionKind.Orbit)
        {
            var n = Math.Min(world.Spatial.Query(p.Pos, p.Radius + MaxEnemyRadius, buf), buf.Length);
            var now = world.StageTime;
            for (var k = 0; k < n; k++)
            {
                var e = enemies[buf[k]];
                if (Vector2.Distance(e.Pos, p.Pos) > p.Radius + e.Radius) continue;
                ref var next = ref e.NextHitBySlot[p.WeaponSlot];
                if (now < next - 1e-5f) continue;
                next = now + p.HitInterval;
                HitOne(world, p, e, FirePatternsDir(e.Pos - world.Hero.Pos, p.Vel), p.Damage);
            }
            return false;
        }

        var count = Math.Min(world.Spatial.QueryCapsule(from, to, p.Radius + MaxEnemyRadius, buf), buf.Length);
        if (count == 0) return false;
        // 선분 시작에서 가까운 순(같으면 밀집 인덱스 순)
        Span<int> order = count <= 256 ? stackalloc int[count] : new int[count];
        Span<float> ts = count <= 256 ? stackalloc float[count] : new float[count];
        var m = 0;
        var seg = to - from;
        var len2 = MathF.Max(seg.LengthSquared(), 1e-6f);
        for (var k = 0; k < count; k++)
        {
            var e = enemies[buf[k]];
            if (p.HasHit(e.Id)) continue;
            if (MathX.DistanceSqToSegment(e.Pos, from, to) > Sq(p.Radius + e.Radius)) continue;
            var t = Math.Clamp(Vector2.Dot(e.Pos - from, seg) / len2, 0f, 1f);
            var j = m++;
            while (j > 0 && (ts[j - 1] > t || ts[j - 1] == t && order[j - 1] > buf[k])) { ts[j] = ts[j - 1]; order[j] = order[j - 1]; j--; }
            ts[j] = t;
            order[j] = buf[k];
        }
        for (var k = 0; k < m; k++)
        {
            var e = enemies[order[k]];
            var at = Vector2.Lerp(from, to, ts[k]);
            p.AddHit(e.Id);
            if (p.ExplodeRadius > 0f)
            {
                p.Pos = at;
                return true;   // 폭발은 End 가 한 번(처음 맞은 적 포함)
            }
            HitOne(world, p, e, FirePatternsDir(p.Vel, e.Pos - at), p.Damage);
            p.PierceLeft--;
            if (p.Motion == MotionKind.Ricochet && p.BouncesLeft > 0)
            {
                p.Pos = at;
                if (Bounce(world, p, e)) return false;
                return true;
            }
            if (p.PierceLeft <= 0)
            {
                p.Pos = at;
                return true;
            }
        }
        return false;
    }

    /// <summary>튕김: 맞은 적 둘레 <c>BounceRange</c> 안 아직 안 맞은 가장 가까운 적으로. 없으면 끝.</summary>
    private static bool Bounce(World world, Projectile p, Enemy from)
    {
        var buf = world.Combat.Query;
        var n = Math.Min(world.Spatial.Query(from.Pos, p.BounceRange, buf), buf.Length);
        Enemy? best = null;
        var bestD = float.MaxValue;
        for (var k = 0; k < n; k++)
        {
            var e = world.Enemies[buf[k]];
            if (p.HasHit(e.Id)) continue;
            var d = Vector2.DistanceSquared(e.Pos, from.Pos);
            if (d < bestD) { bestD = d; best = e; }
        }
        if (best is null) return false;
        p.BouncesLeft--;
        p.PierceLeft = 1;
        p.Target = best.Id;
        p.Vel = FirePatternsDir(best.Pos - p.Pos, p.Vel) * p.Speed;
        return true;
    }

    private static void HitOne(World world, Projectile p, Enemy e, Vector2 dir, float damage)
    {
        world.Damage.ToEnemy(e.Id, damage, p.DamagePct, p.CritChance, p.WeaponSlot, dir, p.Knockback);
        if (p.Def.HitEffect is { } fx) CombatFx.Effect(world, fx, e.Pos, 0f, dir);
    }

    /// <summary>적 투사체의 맞음(적대 대상 = 주인공, MVP).</summary>
    private static bool HitHero(World world, Projectile p, Vector2 from, Vector2 to)
    {
        var hero = world.Hero;
        if (hero.Dead || p.Faction == Faction.Hero) return false;
        if (MathX.DistanceSqToSegment(hero.Pos, from, to) > Sq(p.Radius + hero.Radius)) return false;
        var dir = FirePatternsDir(p.Vel, hero.Pos - from);
        world.Damage.ToHero(p.Owner, p.Damage, dir, p.Knockback);
        p.Pos = Vector2.Lerp(from, to, Math.Clamp(Vector2.Dot(hero.Pos - from, to - from) / MathF.Max((to - from).LengthSquared(), 1e-6f), 0f, 1f));
        if (p.Def.HitEffect is { } fx) CombatFx.Effect(world, fx, hero.Pos, 0f, dir);
        return true;
    }

    /// <summary>끝: 주인공 폭발(반경 안 모든 적 한 번씩) 또는 적 쪽 <c>on_end</c> 장판.</summary>
    private static void End(World world, Projectile p)
    {
        if (p.Faction == Faction.Hero && p.ExplodeRadius > 0f) Explode(world, p, p.Pos);
        if (p.Def.OnEnd is { } onEnd) CombatSpawner.OnEndHazard(world, p, onEnd);
    }

    private static void Explode(World world, Projectile p, Vector2 at)
    {
        var mul = p.Def.Explosion?.DamageMul ?? 1f;
        var buf = world.Combat.Query;
        var n = Math.Min(world.Spatial.Query(at, p.ExplodeRadius + MaxEnemyRadius, buf), buf.Length);
        for (var k = 0; k < n; k++)
        {
            var e = world.Enemies[buf[k]];
            if (Vector2.Distance(e.Pos, at) > p.ExplodeRadius + e.Radius) continue;
            world.Damage.ToEnemy(e.Id, p.ExplodeDamage * mul, p.DamagePct, p.CritChance, p.WeaponSlot, FirePatternsDir(e.Pos - at, p.Vel), p.Knockback);
        }
        var fx = p.Weapon?.Override?.ExplosionEffect ?? p.Def.Explosion?.Effect;
        if (fx is not null) CombatFx.Effect(world, fx, at, p.ExplodeRadius, default);
        if (p.Weapon is { } w) WeaponEffects.Run(world, w.OnExplode, p, at);
    }

    private static Vector2 FirePatternsDir(Vector2 d, Vector2 fallback) =>
        d.LengthSquared() > 1e-6f ? Vector2.Normalize(d) : fallback.LengthSquared() > 1e-6f ? Vector2.Normalize(fallback) : new Vector2(0, 1);

    private static float Sq(float x) => x * x;
}
