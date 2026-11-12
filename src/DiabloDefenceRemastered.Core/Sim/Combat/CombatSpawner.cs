using DiabloDefenceRemastered.Core.Sim.Projectiles;

namespace DiabloDefenceRemastered.Core.Sim.Combat;

/// <summary>
/// 적 쪽 투사체·장판·예고·획득물 만들기 (WP1 계약 <see cref="ICombatSpawner"/>, WP3 구현 — 설계서 5.5). 저장소·진행·맞음 판정은 WP3 몫이다.
/// 공정성(10.1): 적 진영 투사체를 만드는 순간 쏜 적이 또렷이 보이지 않았으면 <c>Fairness.UnseenShots</c> +1(중간 보스·보스 제외, 단 <c>SightGated</c> 발사는 셈).
/// </summary>
public sealed class CombatSpawner(World world) : ICombatSpawner
{
    /// <summary>적 투사체 안전 상한 기본값(<c>rules.caps.enemy_projectiles</c> 가 없을 때).</summary>
    public const int DefaultEnemyCap = 200;
    /// <summary>그림 크기를 모를 때 획득물 반경(px).</summary>
    public const float DefaultPickupRadius = 8f;

    public EntityId SpawnProjectile(in EnemyProjectileSpawn s)
    {
        HeroShots.Cap(world, Faction.Enemy, world.Defs.Rules.Caps?.EnemyProjectiles ?? DefaultEnemyCap);
        var def = s.Def;
        var p = world.Projectiles.Add();
        p.Def = def;
        p.Faction = Faction.Enemy;
        p.Pos = p.PrevPos = s.Pos;
        var speed = s.Speed > 0f ? s.Speed : def.Speed ?? 0f;
        var dir = s.Dir.LengthSquared() > 1e-6f ? Vector2.Normalize(s.Dir) : new Vector2(0, 1);
        p.Speed = speed;
        p.Vel = dir * speed;
        p.Radius = def.Radius;
        p.Tint = def.Tint;
        p.Scale = def.Scale;
        p.RangeLeft = s.Range > 0f ? s.Range : def.Range ?? float.PositiveInfinity;   // 보스 묶음 A 이음새(6.20 ⑥): 생성 사거리(바닥액)
        var life = def.Lifetime ?? float.PositiveInfinity;
        if (float.IsPositiveInfinity(life) && float.IsPositiveInfinity(p.RangeLeft)) life = HeroShots.FallbackLifetime;
        p.TimeLeft = p.Duration = life;
        p.Owner = s.Owner;
        p.WeaponSlot = -1;
        p.SpawnTick = world.Tick;
        p.Damage = s.Damage;
        p.PierceLeft = Math.Max(1, def.Pierce);
        p.Target = s.Target;
        p.SightGated = s.SightGated;
        if (def.TurnRate > 0f)
        {
            p.Motion = MotionKind.Homing;
            p.TurnRate = def.TurnRate * (MathF.PI / 180f);
        }
        world.Enemies.TryGet(s.Owner, out var owner);
        if (def.OnEnd is { } onEnd)
            p.OnEndDamage = owner is null
                ? DamageMath.Round(onEnd.Damage)
                : DamageMath.EnemyFinalDamage(onEnd.Damage, owner.DamageMul, owner.Buffs.Sum(EnemyStatKey.DamagePct, world.Defs.Buffs), 1f);
        CountUnseenShot(owner, s.SightGated);
        return p.Id;
    }

    public EntityId SpawnHazard(in HazardSpawn s)
    {
        if (s.CapGroup is { } group && s.Cap > 0) CapGroup(group, s.Cap);
        var h = world.Hazards.Add();
        h.Effect = s.Effect?.Effect.Def;
        h.Tint = s.Effect?.Tint ?? s.Effect?.Effect.Def.Tint ?? Rgb.White;
        h.EffectScale = s.Effect?.Scale ?? 1f;
        h.Faction = s.From;
        h.Pos = h.PrevPos = s.Pos;
        h.Radius = s.Radius;
        h.TimeLeft = h.Duration = s.Duration;
        h.Owner = s.Owner;
        h.SpawnTick = world.Tick;
        h.TickInterval = MathF.Max(World.Dt, s.TickInterval);
        h.NextTickIn = 0f;
        h.TickDamage = s.TickDamage;
        h.WeaponSlot = -1;
        h.Buff = s.Buff;
        h.BuffDuration = s.BuffDuration;
        h.CapGroup = s.CapGroup;
        return h.Id;
    }

    public EntityId SpawnTelegraph(in TelegraphSpawn s)
    {
        var t = world.Telegraphs.Add();
        t.Effect = s.Effect?.Effect.Def;
        t.Tint = s.Effect?.Tint ?? s.Effect?.Effect.Def.Tint ?? Rgb.White;
        t.Shape = s.Shape;
        t.Faction = s.From;
        t.Pos = t.PrevPos = s.Pos;
        t.End = s.End;
        t.Size = s.Size;
        t.TimeLeft = t.Duration = s.Time;
        t.Owner = s.Owner;
        t.SpawnTick = world.Tick;
        t.Blast = s.Blast;
        return t.Id;
    }

    /// <summary>획득물 놓기(v0.6.3 — 드롭도 같은 길). <c>Reward != null</c> 이면 <c>GatesExit</c>(v0.7.1). 신호 <c>PickupDropped</c>.</summary>
    public EntityId SpawnPickup(in PickupSpawn s)
    {
        var p = world.Pickups.Add();
        p.Def = s.Def;
        p.Pos = p.PrevPos = s.Pos;
        p.Radius = s.Def.Draw.Size is { } size ? MathF.Max(size.X, size.Y) * 0.5f : DefaultPickupRadius;
        p.Value = s.Value != 0 ? s.Value : s.Def.ValueRange is [var lo, ..] ? lo : 0;
        p.SpawnTick = world.Tick;
        p.SupplyRoom = s.SupplyRoom;
        p.Reward = s.Reward;
        p.GatesExit = s.Reward is not null;
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.PickupDropped, s.Pos, s.Def));
        return p.Id;
    }

    /// <summary>투사체 <c>on_end</c> 장판(맞음·사거리 끝·장애물 — 5.4). 피해는 발사 순간에 굳힌 값.</summary>
    internal static void OnEndHazard(World world, Projectile p, GroundHazardDef onEnd)
    {
        var damage = p.Faction == Faction.Hero ? onEnd.Damage : p.OnEndDamage;
        world.Spawner.SpawnHazard(new HazardSpawn(onEnd.Effect, p.Pos, onEnd.Radius, onEnd.Duration, onEnd.Tick, DamageMath.Round(damage),
                                                  p.Faction, p.Owner, CapGroup: onEnd.CapGroup, Cap: onEnd.Cap));
    }

    private void CapGroup(string group, int cap)
    {
        var store = world.Hazards;
        var n = 0;
        for (var i = 0; i < store.Count; i++)
            if (!store[i].Removed && store[i].CapGroup == group) n++;
        for (var i = 0; i < store.Count && n >= cap; i++)
        {
            if (store[i].Removed || store[i].CapGroup != group) continue;
            store.RemoveAt(i);
            n--;
        }
    }

    private void CountUnseenShot(Enemy? owner, bool sightGated)
    {
        if (owner is null || !world.Visibility.Enabled) return;
        if (owner.Def.Rank != DefRank.Normal && !sightGated) return;
        if (!world.Visibility.IsClearlyVisible(owner.Pos, owner.BodyTop)) world.Fairness.UnseenShots++;
    }
}

/// <summary>적대 대상 질의(WP1 계약, WP3 구현 — 5.1). MVP: 적·중립이 묻으면 주인공 하나(살아 있고 반경 안).</summary>
public sealed class HostileQuery(World world) : IHostileQuery
{
    public bool TryNearestHostile(Faction self, Vector2 from, float range, out EntityId target, out Vector2 pos)
    {
        var hero = world.Hero;
        if (self != Faction.Hero && self != Faction.Ally && !hero.Dead && Vector2.DistanceSquared(hero.Pos, from) <= range * range)
        {
            target = EntityId.Hero;
            pos = hero.Pos;
            return true;
        }
        target = EntityId.None;
        pos = from;
        return false;
    }
}
