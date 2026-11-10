using DiabloDefenceRemastered.Core.Sim.Projectiles;

namespace DiabloDefenceRemastered.Core.Sim.Weapons;

/// <summary>
/// 발사 패턴 (설계서 5.3, 기획 13.2 — WP3 소유 형). 키 목록 선언이 13.2 표를 코드 한 곳에 둔다(<see cref="WeaponStats.Resolve"/> 가 읽음, 검사 13.5-2 도 이것).
/// 패턴 객체는 상태가 없다 — 칸 상태는 <see cref="WeaponSlot.Pattern"/> 에 있다.
/// </summary>
public interface IFirePattern
{
    string Id { get; }
    /// <summary>검증 13.5-2: 레벨 표 열 이름 = 이것뿐.</summary>
    IReadOnlyList<string> LevelKeys { get; }
    /// <summary>null(상시/없음)을 허용하는 열.</summary>
    IReadOnlyList<string> NullableKeys { get; }
    /// <summary><c>projectile_count</c> 가 더해지는 키.</summary>
    string? CountKey { get; }
    IReadOnlyList<string> AreaKeys { get; }
    IReadOnlyList<string> DurationKeys { get; }

    /// <summary>쿨다운이 끝났을 때. 쏘지 않았으면(유도형의 대상 없음) false — 칸은 다음 틱에 다시 시도한다.</summary>
    bool Fire(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats);

    /// <summary>매 틱(간격 발사의 남은 발·궤도 관리). 쿨다운과 상관없이 부른다.</summary>
    void Tick(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats) { }

    /// <summary>켜져 있는 동안 쿨다운이 흐르지 않는 패턴(궤도 — "꺼진 뒤 쿨만큼 쉼").</summary>
    bool HoldsCooldownWhileActive => false;
}

/// <summary>발사 패턴 여섯 종(homing·straight_explode·orbit·ricochet·fan·ground_pool). 등록 이름 = <see cref="IFirePattern.Id"/>.</summary>
public static class FirePatterns
{
    public static readonly IReadOnlyList<IFirePattern> All =
    [
        new HomingPattern(), new StraightExplodePattern(), new OrbitPattern(), new RicochetPattern(), new FanPattern(), new GroundPoolPattern(),
    ];

    public static IFirePattern Get(string id)
    {
        foreach (var p in All)
            if (p.Id == id) return p;
        throw new KeyNotFoundException($"발사 패턴 '{id}' 가 없다");
    }

    public static bool TryGet(string? id, out IFirePattern pattern)
    {
        foreach (var p in All)
            if (p.Id == id) { pattern = p; return true; }
        pattern = null!;
        return false;
    }

    /// <summary>무기의 패턴(진화 무기는 재료 무기 것).</summary>
    public static IFirePattern Of(WeaponDef def) => Get(def.Pattern ?? def.BaseWeapon?.Def.Pattern ?? throw new InvalidOperationException($"무기 '{def.Id}' 에 패턴이 없다"));

    /// <summary>무기의 투사체(진화 덮어쓰기 → 자기 → 재료 무기).</summary>
    internal static ProjectileDef ProjectileOf(WeaponDef def) =>
        def.Override?.Projectile?.Def ?? def.Projectile?.Def ?? def.BaseWeapon?.Def.Projectile?.Def
        ?? throw new InvalidOperationException($"무기 '{def.Id}' 에 투사체가 없다");

    internal static ITargeting? TargetingOf(WeaponDef def) =>
        (def.Targeting ?? def.BaseWeapon?.Def.Targeting)?.Instance as ITargeting;

    internal static Vector2 Dir(Vector2 from, Vector2 to, Vector2 fallback)
    {
        var d = to - from;
        return d.LengthSquared() > 1e-6f ? Vector2.Normalize(d) : fallback.LengthSquared() > 1e-6f ? Vector2.Normalize(fallback) : new Vector2(0, 1);
    }
}

/// <summary>유도(매직 미사일): 대상이 없으면 쏘지 않고 대기. 여러 발은 <c>base.burst_interval</c> 간격.</summary>
internal sealed class HomingPattern : IFirePattern
{
    public string Id => "homing";
    public IReadOnlyList<string> LevelKeys { get; } = ["damage", "cooldown", "count", "pierce", "speed"];
    public IReadOnlyList<string> NullableKeys { get; } = [];
    public string? CountKey => "count";
    public IReadOnlyList<string> AreaKeys { get; } = [];
    public IReadOnlyList<string> DurationKeys { get; } = [];

    public bool Fire(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        if (!Shoot(world, slotIndex, ref slot, stats, 0)) return false;
        slot.Pattern.Counter = 1;
        slot.Pattern.BurstLeft = stats.Count - 1;
        slot.Pattern.BurstTimer = stats.BurstInterval;
        // 간격 0 이면 한 번에 모두
        while (slot.Pattern.BurstLeft > 0 && stats.BurstInterval <= 0f) Next(world, slotIndex, ref slot, stats);
        return true;
    }

    public void Tick(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        if (slot.Pattern.BurstLeft <= 0) return;
        slot.Pattern.BurstTimer -= World.Dt;
        while (slot.Pattern.BurstLeft > 0 && slot.Pattern.BurstTimer <= 1e-5f)
        {
            Next(world, slotIndex, ref slot, stats);
            slot.Pattern.BurstTimer += stats.BurstInterval;
        }
    }

    private static void Next(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        Shoot(world, slotIndex, ref slot, stats, slot.Pattern.Counter);   // 대상이 없으면 그 발은 버린다
        slot.Pattern.Counter++;
        slot.Pattern.BurstLeft--;
    }

    private static bool Shoot(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats, int shot)
    {
        var def = slot.Def!;
        var aim = FirePatterns.TargetingOf(def);
        var origin = world.Hero.Pos;
        if (aim is null || aim.Aim(world, origin, shot, out var point, out var target) != AimResult.Target) return false;
        var dir = FirePatterns.Dir(origin, point, world.Hero.Facing);
        var p = HeroShots.Spawn(world, slotIndex, def, stats, origin, dir, stats.Speed);
        p.Motion = MotionKind.Homing;
        p.Target = target;
        p.TurnRate = stats.TurnRate * (MathF.PI / 180f);
        p.PierceLeft = stats.Pierce;
        return true;
    }
}

/// <summary>직선 + 폭발(파이어볼): 맞거나 사거리 끝에서 반경 안 모든 적(처음 맞은 적 포함, 한 번씩). 대상이 없으면 바라보는 방향.</summary>
internal sealed class StraightExplodePattern : IFirePattern
{
    public string Id => "straight_explode";
    public IReadOnlyList<string> LevelKeys { get; } = ["damage", "radius", "cooldown", "count"];
    public IReadOnlyList<string> NullableKeys { get; } = [];
    public string? CountKey => "count";
    public IReadOnlyList<string> AreaKeys { get; } = ["radius"];
    public IReadOnlyList<string> DurationKeys { get; } = [];

    public bool Fire(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        var def = slot.Def!;
        var aim = FirePatterns.TargetingOf(def);
        var origin = world.Hero.Pos;
        for (var i = 0; i < stats.Count; i++)
        {
            var point = Targetings.FacingPoint(world, origin, 1f);
            aim?.Aim(world, origin, i, out point, out _);
            var dir = FirePatterns.Dir(origin, point, world.Hero.Facing);
            var p = HeroShots.Spawn(world, slotIndex, def, stats, origin, dir, stats.Speed);
            p.Motion = MotionKind.Straight;
            p.PierceLeft = 1;
            p.ExplodeRadius = stats.Radius;
            p.ExplodeDamage = stats.Damage;
            if (stats.Range > 0f) p.RangeLeft = stats.Range;
        }
        return stats.Count > 0;
    }
}

/// <summary>
/// 회전(비전 구체): 켜지면 N 개가 같은 간격으로 주인공 둘레를 돈다. 지속이 끝나면 지우고 쿨만큼 쉰다(켜진 동안 쿨이 흐르지 않음).
/// 상시(지속 null)면 끄지 않고 다시 쏘지 않는다. 레벨·범위가 바뀌어 수·반경이 달라지면 그 자리에서 다시 띄운다(같은 각에서).
/// </summary>
internal sealed class OrbitPattern : IFirePattern
{
    public string Id => "orbit";
    public IReadOnlyList<string> LevelKeys { get; } = ["damage", "count", "orbit_radius", "duration", "cooldown"];
    public IReadOnlyList<string> NullableKeys { get; } = ["duration", "cooldown"];
    public string? CountKey => "count";
    public IReadOnlyList<string> AreaKeys { get; } = ["orbit_radius"];
    public IReadOnlyList<string> DurationKeys { get; } = ["duration"];
    public bool HoldsCooldownWhileActive => true;

    public bool Fire(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        slot.Pattern.Active = true;
        slot.Pattern.Timer = stats.Duration;
        Spawn(world, slotIndex, ref slot, stats);
        return true;
    }

    public void Tick(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        if (!slot.Pattern.Active) return;
        slot.Pattern.Angle += stats.Rotation * (MathF.PI / 180f) * World.Dt;
        if (stats.Permanent) slot.Pattern.Timer = float.PositiveInfinity;
        else
        {
            if (float.IsPositiveInfinity(slot.Pattern.Timer)) slot.Pattern.Timer = stats.Duration;   // 상시였다가 바뀐 경우는 없지만 지킴
            slot.Pattern.Timer -= World.Dt;
            if (slot.Pattern.Timer <= 1e-5f)
            {
                Clear(world, slotIndex);
                slot.Pattern.Active = false;
                slot.Cooldown = stats.HasCooldown ? stats.Cooldown : 0f;
                return;
            }
        }
        var (count, radius) = Orbs(world, slotIndex);
        if (count != stats.Count || radius != stats.OrbitRadius) Spawn(world, slotIndex, ref slot, stats);
    }

    /// <summary>이 칸이 지금 띄운 구체 수·반경(레벨·범위가 바뀌었는지 보려고 — 칸 상태에 필드를 더하지 않는다).</summary>
    private static (int Count, float Radius) Orbs(World world, int slotIndex)
    {
        var n = 0;
        var r = 0f;
        foreach (var p in world.Projectiles.Items)
        {
            if (p.Removed || p.Faction != Faction.Hero || p.Motion != MotionKind.Orbit || p.WeaponSlot != slotIndex) continue;
            n++;
            r = p.OrbitRadius;
        }
        return (n, r);
    }

    private static void Spawn(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        Clear(world, slotIndex);
        var def = slot.Def!;
        var n = Math.Max(0, stats.Count);
        var w = stats.Rotation * (MathF.PI / 180f);
        for (var i = 0; i < n; i++)
        {
            var angle = slot.Pattern.Angle + MathF.Tau * i / n;
            var p = HeroShots.Spawn(world, slotIndex, def, stats, world.Hero.Pos, Vector2.UnitX, 0f);
            p.Motion = MotionKind.Orbit;
            p.OrbitAngle = angle;
            p.OrbitRadius = stats.OrbitRadius;
            p.AngularSpeed = w;
            p.HitInterval = stats.HitInterval;
            p.PierceLeft = int.MaxValue;
            p.TimeLeft = p.Duration = float.PositiveInfinity;
            ProjectileMotion.PlaceOrbit(p, world.Hero.Pos);
        }
    }

    /// <summary>이 칸의 구체를 지운다.</summary>
    private static void Clear(World world, int slotIndex)
    {
        var projectiles = world.Projectiles;
        for (var i = 0; i < projectiles.Count; i++)
        {
            var p = projectiles[i];
            if (!p.Removed && p.Faction == Faction.Hero && p.Motion == MotionKind.Orbit && p.WeaponSlot == slotIndex) projectiles.RemoveAt(i);
        }
    }
}

/// <summary>튕김(유령 화살): 맞으면 근처(<c>base.bounce_range</c>, 기본 200) 다른 적에게. 대상이 없으면 바라보는 방향.</summary>
internal sealed class RicochetPattern : IFirePattern
{
    public string Id => "ricochet";
    public IReadOnlyList<string> LevelKeys { get; } = ["damage", "bounces", "cooldown", "count"];
    public IReadOnlyList<string> NullableKeys { get; } = [];
    public string? CountKey => "count";
    public IReadOnlyList<string> AreaKeys { get; } = [];
    public IReadOnlyList<string> DurationKeys { get; } = [];

    public bool Fire(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        var def = slot.Def!;
        var aim = FirePatterns.TargetingOf(def);
        var origin = world.Hero.Pos;
        for (var i = 0; i < stats.Count; i++)
        {
            var point = Targetings.FacingPoint(world, origin, 1f);
            var target = EntityId.None;
            aim?.Aim(world, origin, i, out point, out target);
            var p = HeroShots.Spawn(world, slotIndex, def, stats, origin, FirePatterns.Dir(origin, point, world.Hero.Facing), stats.Speed);
            p.Motion = MotionKind.Ricochet;
            p.Target = target;
            p.PierceLeft = 1;
            p.BouncesLeft = stats.Bounces;
            p.BounceRange = stats.BounceRange;
        }
        return stats.Count > 0;
    }
}

/// <summary>부채꼴 관통(뼈 화살 일제사격): 바라보는 방향으로 <c>spread</c>° 안에 같은 간격(발 수가 늘면 간격이 좁아짐).</summary>
internal sealed class FanPattern : IFirePattern
{
    public string Id => "fan";
    public IReadOnlyList<string> LevelKeys { get; } = ["damage", "count", "pierce", "cooldown", "spread"];
    public IReadOnlyList<string> NullableKeys { get; } = [];
    public string? CountKey => "count";
    public IReadOnlyList<string> AreaKeys { get; } = [];
    public IReadOnlyList<string> DurationKeys { get; } = [];

    public bool Fire(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        var def = slot.Def!;
        var aim = FirePatterns.TargetingOf(def);
        var origin = world.Hero.Pos;
        var point = Targetings.FacingPoint(world, origin, 1f);
        aim?.Aim(world, origin, 0, out point, out _);
        var center = FirePatterns.Dir(origin, point, world.Hero.Facing);
        var baseAngle = MathF.Atan2(center.Y, center.X);
        var spread = stats.Spread * (MathF.PI / 180f);
        var n = stats.Count;
        for (var i = 0; i < n; i++)
        {
            var a = n <= 1 ? baseAngle : baseAngle - spread / 2f + spread * i / (n - 1);
            var p = HeroShots.Spawn(world, slotIndex, def, stats, origin, new Vector2(MathF.Cos(a), MathF.Sin(a)), stats.Speed);
            p.Motion = MotionKind.Straight;
            p.PierceLeft = stats.Pierce;
            if (stats.Range > 0f) p.RangeLeft = stats.Range;
        }
        return n > 0;
    }
}

/// <summary>장판(지옥불 장판): 조준 자리(<c>densest</c>)에 장판, 여러 개면 그 둘레에. 틱 간격 = <c>base.hit_interval</c>(적별 타격 간격과 같음).</summary>
internal sealed class GroundPoolPattern : IFirePattern
{
    public string Id => "ground_pool";
    public IReadOnlyList<string> LevelKeys { get; } = ["tick_damage", "radius", "duration", "count", "cooldown", "slow_pct"];
    public IReadOnlyList<string> NullableKeys { get; } = [];
    public string? CountKey => "count";
    public IReadOnlyList<string> AreaKeys { get; } = ["radius"];
    public IReadOnlyList<string> DurationKeys { get; } = ["duration"];

    public bool Fire(World world, int slotIndex, ref WeaponSlot slot, in WeaponStats stats)
    {
        var def = slot.Def!;
        var aim = FirePatterns.TargetingOf(def);
        var origin = world.Hero.Pos;
        var point = origin;
        aim?.Aim(world, origin, 0, out point, out _);
        for (var i = 0; i < stats.Count; i++)
        {
            var at = point;
            if (i > 0)
            {
                var a = MathF.Tau * (i - 1) / Math.Max(1, stats.Count - 1);
                at += new Vector2(MathF.Cos(a), MathF.Sin(a)) * stats.Radius;
            }
            HeroShots.Pool(world, slotIndex, def.Effect ?? def.BaseWeapon?.Def.Effect, at, stats.Radius, stats.Duration, stats.HitInterval,
                           stats.Damage, stats.DamagePct, stats.CritChance, stats.AreaMul);
        }
        return stats.Count > 0;
    }
}
