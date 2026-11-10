using DiabloDefenceRemastered.Core.Sim.Enemies;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// WP4 시험용 가짜 이웃(WP2·WP3 본문 전 — 11.3 "그 묶음에 기대는 시험은 합성·가짜로"). 실제 구현이 아니라 감독·적·자동 조작이 돌 만큼만:
/// <list type="bullet">
/// <item><see cref="FakeMotion"/>(6 Locomotion): 이동 의도 → 직선 이동, 맵 밖·경계 여유 부족이면 멈추고 <c>MoveBlockedTime</c> 누적. 주인공은 명령 이동(95px/s). 입장 중 적은 맵 밖에서도 움직인다.</item>
/// <item><see cref="FakeWeapons"/>(9 Weapons): 주인공이 0.2s 마다 가장 가까운 적(사거리 안)에 피해 요청.</item>
/// <item><see cref="FakeDamage"/>(12 DamageResolve): 요청 적용 — 적 HP·<c>Dead</c>(Damage)·<c>HitLanded</c>, 주인공은 <c>--god</c> 가 아니면 HP·<c>Dead</c>·<c>HeroDamaged</c>. 페이즈 무적(<c>InvulnUntilTick</c>)을 지킨다.</item>
/// <item><see cref="FakePickups"/>(15 Pickups): 주인공 반경 40 안 획득물을 줍는다(문 보상·보급).</item>
/// <item><see cref="RecordingSpawner"/>: 투사체·장판·예고는 수만 세고(저장소에 만들지 않음), 획득물은 <c>FakeSpawner</c> 와 같은 규칙으로 놓는다.</item>
/// </list>
/// </summary>
public static class FakeArena
{
    /// <summary>EnemyModule + 가짜 이웃 모듈.</summary>
    public static ModuleContext Modules(float weaponRange = 360f, float weaponDamage = 14f, bool weapons = true)
    {
        var m = new ModuleContext();
        new EnemyModule().Register(m);
        m.AddWorldInstaller((w, b) => Install(w, b, weaponRange, weaponDamage, weapons));
        return m;
    }

    public static void Install(World w, SimulationBuilder b, float weaponRange = 360f, float weaponDamage = 14f, bool weapons = true)
    {
        if (w.Nav is null)
        {
            var nav = w.Arena is { } arena ? new StraightNav(arena.Iso) : new StraightNav(w.MapSize);
            nav.HeroPos = () => w.Hero.Pos;
            w.Nav = nav;
        }
        w.Spatial ??= new BruteForceSpatial(w);
        w.Spawner = new RecordingSpawner(w);
        w.Hero.MaxHp = w.Hero.Hp = 100f;
        w.Hero.MoveSpeed = 95f;
        b.Add(SimPhase.Locomotion, new FakeMotion());
        if (weapons) b.Add(SimPhase.Weapons, new FakeWeapons(weaponRange, weaponDamage));
        b.Add(SimPhase.DamageResolve, new FakeDamage());
        b.Add(SimPhase.Pickups, new FakePickups());
    }
}

public sealed class FakeMotion : ISimSystem
{
    public void Tick(World world)
    {
        var hero = world.Hero;
        if (!hero.Dead)
        {
            var v = world.Command.Move * (hero.MoveSpeed > 0f ? hero.MoveSpeed : 95f);
            var next = hero.Pos + v * World.Dt;
            if (world.Nav.IsInsideMap(next) && world.Nav.Clearance(next) >= hero.Radius) hero.Pos = next;
            hero.Vel = v;
            if (v.LengthSquared() > 1e-4f) hero.Facing = Vector2.Normalize(v);
        }
        foreach (var e in world.Enemies.Items)
        {
            if (!e.IsAlive || e.SpawnTick >= world.Tick) continue;
            var dir = e.Intent.Kind switch
            {
                MoveKind.SeekHero => hero.Pos - e.Pos,
                MoveKind.SeekPoint => e.Intent.Point - e.Pos,
                MoveKind.Away => e.Pos - e.Intent.Point,
                MoveKind.Heading or MoveKind.Straight => e.Intent.Dir,
                _ => Vector2.Zero,
            };
            if (dir.LengthSquared() < 1e-6f) { e.Vel = Vector2.Zero; continue; }
            // 주인공에 닿으면(근접 사거리) 더 다가가지 않는다(충돌 단계 흉내)
            if (e.Intent.Kind == MoveKind.SeekHero && Vector2.Distance(e.Pos, hero.Pos) <= e.Radius + hero.Radius) { e.Vel = Vector2.Zero; continue; }
            var v = Vector2.Normalize(dir) * e.MoveSpeed * (e.Intent.SpeedMul > 0f ? e.Intent.SpeedMul : 1f);
            var next = e.Pos + v * World.Dt;
            var free = e.Life == LifeState.Entering || (world.Nav.IsInsideMap(next) && world.Nav.Clearance(next) >= e.Radius * 0.5f);
            if (free)
            {
                e.PrevPos = e.Pos;
                e.Pos = next;
                e.Vel = v;
                e.MoveBlockedTime = 0f;
            }
            else
            {
                e.Vel = Vector2.Zero;
                e.MoveBlockedTime += World.Dt;
            }
        }
    }
}

public sealed class FakeWeapons(float range, float damage) : ISimSystem
{
    public const int Interval = 12;

    public void Tick(World world)
    {
        if (world.Hero.Dead || world.Tick % Interval != 0) return;
        Enemy? best = null;
        var bestD = range * range;
        foreach (var e in world.Enemies.Items)
        {
            if (!e.IsTargetable) continue;
            var d = Vector2.DistanceSquared(e.Pos, world.Hero.Pos);
            if (d <= bestD) { bestD = d; best = e; }
        }
        if (best is not null) world.Damage.ToEnemy(best.Id, damage, 0f, 0f, 0);
    }
}

public sealed class FakeDamage : ISimSystem
{
    public int HeroHits { get; private set; }

    public void Tick(World world)
    {
        foreach (ref readonly var r in world.Damage.Requests.AsSpan())
        {
            if ((r.Flags & DamageFlags.NoDamage) != 0) continue;
            if (r.Target.IsHero)
            {
                if (r.From == Faction.Hero || world.Outcome != RunOutcome.Running) continue;
                HeroHits++;
                var applied = (int)r.Amount;
                world.Events.Current.HeroDamaged.Add(new HeroDamaged(r.Attacker, applied, (int)world.Hero.Hp));
                world.Events.Current.HitLanded.Add(new HitLanded(r.Attacker, EntityId.Hero, applied));
                if (world.Options.God) continue;
                world.Hero.Hp -= applied;
                if (world.Hero.Hp <= 0f) world.Hero.Dead = true;
                continue;
            }
            var e = world.Enemies.Get(r.Target);
            if (e is null || !e.IsAlive || r.From == Faction.Enemy || world.Tick < e.InvulnUntilTick) continue;
            e.Hp -= r.Amount;
            world.Events.Current.HitLanded.Add(new HitLanded(r.Attacker, e.Id, (int)r.Amount));
            if (e.Hp <= 0f)
            {
                e.Dead = true;
                e.KillCause = KillCause.Damage;
            }
        }
        world.Damage.Clear();
    }
}

public sealed class FakePickups : ISimSystem
{
    public void Tick(World world)
    {
        foreach (var p in world.Pickups.Items)
        {
            if (p.Removed || Vector2.Distance(p.Pos, world.Hero.Pos) > 40f) continue;
            if (p.SupplyRoom is { } room) world.Events.Current.SupplyCollected.Add(new SupplyCollected(room, p.Def, p.Pos));
            world.Pickups.Remove(p.Id);
        }
    }
}

/// <summary>투사체·장판·예고는 세기만, 획득물은 놓는다(<c>FakeSpawner</c> 와 같은 규칙).</summary>
public sealed class RecordingSpawner(World world) : ICombatSpawner
{
    public List<EnemyProjectileSpawn> Projectiles { get; } = [];
    public List<HazardSpawn> Hazards { get; } = [];
    public List<TelegraphSpawn> Telegraphs { get; } = [];

    public EntityId SpawnProjectile(in EnemyProjectileSpawn spawn)
    {
        Projectiles.Add(spawn);
        return EntityId.None;
    }

    public EntityId SpawnHazard(in HazardSpawn spawn)
    {
        Hazards.Add(spawn);
        return EntityId.None;
    }

    public EntityId SpawnTelegraph(in TelegraphSpawn spawn)
    {
        Telegraphs.Add(spawn);
        return EntityId.None;
    }

    public EntityId SpawnPickup(in PickupSpawn spawn)
    {
        var p = world.Pickups.Add();
        p.Def = spawn.Def;
        p.Pos = p.PrevPos = spawn.Pos;
        p.Radius = 12f;
        p.Value = spawn.Value;
        p.SpawnTick = world.Tick;
        p.SupplyRoom = spawn.SupplyRoom;
        p.Reward = spawn.Reward;
        p.GatesExit = spawn.Reward is not null;
        return p.Id;
    }
}
