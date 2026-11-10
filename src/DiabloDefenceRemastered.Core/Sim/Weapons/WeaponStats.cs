using System.Text.Json;
using DiabloDefenceRemastered.Core.Sim.Combat;

namespace DiabloDefenceRemastered.Core.Sim.Weapons;

/// <summary>
/// 무기 한 칸의 지금 수치(설계서 5.3) — 레벨 표 + <c>base</c> + 진화 <c>override</c> + 주인공 능력치를 <see cref="Resolve"/> 한 곳에서 합친 읽기 전용 값.
/// 기획 13.2 표(패턴마다 <c>projectile_count</c> 가 더해지는 키, <c>area_pct</c>·<c>duration_pct</c> 가 곱해지는 키)는 패턴의 키 목록 선언대로 적용한다.
/// <b><c>damage_pct</c> 는 적용하지 않는다</b>(5.2 — <see cref="DamageMath.HeroToEnemy"/> 에서만 곱한다). <see cref="DamagePct"/>·<see cref="CritChance"/> 는 발사 순간 복사할 값일 뿐이다.
/// 레벨 표 <c>null</c>: <see cref="Cooldown"/> = NaN(발동 없음), <see cref="Duration"/> = +∞(상시, <c>duration_pct</c> 를 곱하지 않음).
/// </summary>
public readonly record struct WeaponStats
{
    public float Damage { get; init; }
    /// <summary>최종 쿨다운(쿨 감소·마력 폭주 적용). NaN = 발동 없음(레벨 표 null).</summary>
    public float Cooldown { get; init; }
    public int Count { get; init; }
    public int Pierce { get; init; }
    public float Speed { get; init; }
    /// <summary>폭발·장판 반경(area_pct 적용).</summary>
    public float Radius { get; init; }
    public float OrbitRadius { get; init; }
    /// <summary>지속(duration_pct 적용, 상시 = +∞).</summary>
    public float Duration { get; init; }
    public int Bounces { get; init; }
    /// <summary>부채꼴 퍼짐각(도).</summary>
    public float Spread { get; init; }
    public float SlowPct { get; init; }

    // base(레벨 무관)
    /// <summary>유도 회전율(도/초).</summary>
    public float TurnRate { get; init; }
    public float Lifetime { get; init; }
    public float BurstInterval { get; init; }
    public float Knockback { get; init; }
    public float HitInterval { get; init; }
    /// <summary>궤도 회전(도/초).</summary>
    public float Rotation { get; init; }
    public float Range { get; init; }
    public float BounceRange { get; init; }

    /// <summary>그림 크기 배율 1 + area_pct(기획 13.2 "그림 크기만").</summary>
    public float AreaMul { get; init; }
    /// <summary>발사 순간 복사할 값(적용은 DamageMath 에서만).</summary>
    public float DamagePct { get; init; }
    public float CritChance { get; init; }

    public bool HasCooldown => !float.IsNaN(Cooldown);
    public bool Permanent => float.IsPositiveInfinity(Duration);

    /// <summary>
    /// 칸 하나의 수치. 진화 무기는 <see cref="WeaponDef.BaseWeapon"/> 의 최고 레벨 값 위에 <see cref="WeaponDef.Override"/> 를 얹는다(6.8).
    /// <paramref name="surgePct"/> = 마력 폭주 쿨 감소(0 = 없음).
    /// </summary>
    public static WeaponStats Resolve(WeaponDef def, int level, StatBlock stats, IFirePattern pattern, float surgePct = 0f)
    {
        var table = def;
        var lv = level;
        if (def.BaseWeapon is { } baseRef)
        {
            table = baseRef.Def;
            lv = table.MaxLevel;
        }
        lv = Math.Clamp(lv, 1, Math.Max(1, table.MaxLevel));
        var src = new Source(table, lv, def.BaseWeapon is null ? null : def.Override);

        var areaMul = 1f + stats[StatKey.AreaPct];
        var durMul = 1f + stats[StatKey.DurationPct];
        var cd = src.Get("cooldown", float.NaN);
        var duration = src.Has("duration") ? src.Get("duration", float.PositiveInfinity) : float.PositiveInfinity;
        if (float.IsNaN(duration)) duration = float.PositiveInfinity;
        if (!float.IsPositiveInfinity(duration) && Contains(pattern.DurationKeys, "duration")) duration *= durMul;

        var count = (int)MathF.Round(src.Get("count", 1f));
        if (pattern.CountKey == "count") count += (int)MathF.Round(stats[StatKey.ProjectileCount]);

        var radius = src.Get("radius", 0f);
        if (Contains(pattern.AreaKeys, "radius")) radius *= areaMul;
        var orbit = src.Get("orbit_radius", 0f);
        if (Contains(pattern.AreaKeys, "orbit_radius")) orbit *= areaMul;

        return new WeaponStats
        {
            Damage = src.Has("tick_damage") ? src.Get("tick_damage", 0f) : src.Get("damage", 0f),
            Cooldown = float.IsNaN(cd) ? float.NaN : DamageMath.FinalCooldown(cd, stats[StatKey.CooldownPct], surgePct),
            Count = Math.Max(0, count),
            Pierce = Math.Max(1, (int)MathF.Round(src.Get("pierce", 1f))),
            Speed = src.Get("speed", def.Projectile?.Def.Speed ?? table.Projectile?.Def.Speed ?? 0f),
            Radius = radius,
            OrbitRadius = orbit,
            Duration = duration,
            Bounces = (int)MathF.Round(src.Get("bounces", 0f)),
            Spread = src.Get("spread", 0f),
            SlowPct = src.Get("slow_pct", 0f),
            TurnRate = src.Get("turn_rate", 0f),
            Lifetime = src.Get("lifetime", 0f),
            BurstInterval = src.Get("burst_interval", 0f),
            Knockback = src.Get("knockback", 0f),
            HitInterval = src.Get("hit_interval", 0.5f),
            Rotation = src.Get("rotation", 0f),
            Range = src.Get("range", 0f),
            BounceRange = src.Get("bounce_range", 200f),
            AreaMul = areaMul,
            DamagePct = stats[StatKey.DamagePct],
            CritChance = stats[StatKey.CritChance],
        };
    }

    private static bool Contains(IReadOnlyList<string> keys, string key)
    {
        for (var i = 0; i < keys.Count; i++)
            if (keys[i] == key) return true;
        return false;
    }

    /// <summary>값 찾기 순서: 진화 덮어쓰기 → 레벨 표(null = NaN) → base → 기본값.</summary>
    private readonly struct Source(WeaponDef table, int level, WeaponOverrideDef? over)
    {
        public bool Has(string key) =>
            (over?.Values?.ContainsKey(key) ?? false) || table.Levels.ContainsKey(key) || table.Base.ContainsKey(key);

        public float Get(string key, float fallback)
        {
            if (over?.Values is { } values && values.TryGetValue(key, out var el))
                return el.ValueKind == JsonValueKind.Number ? el.GetSingle() : float.NaN;
            if (table.Levels.TryGetValue(key, out var column))
                return level - 1 < column.Length ? column[level - 1] ?? float.NaN : fallback;
            return table.Base.TryGetValue(key, out var b) ? b : fallback;
        }
    }
}
