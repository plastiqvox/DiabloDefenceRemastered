using DiabloDefenceRemastered.Core.Sim.Combat;
using DiabloDefenceRemastered.Core.Sim.Hazards;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Pickups;
using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Core.Sim.Projectiles;

namespace DiabloDefenceRemastered.Core.Sim.Weapons;

/// <summary>
/// 주인공·전투·무기·성장 모듈 (소유 WP3, 설계서 11.1): 발사 패턴 6·조준 5·무기 효과 부품 등록, 무기 레벨 표 검사(13.5-2),
/// 월드마다 <c>Hostiles</c>·<c>Spawner</c>·<c>Progression</c> 을 넣고 주인공을 만든 뒤 단계 시스템(HeroControl·Weapons·Projectiles·Hazards·DamageResolve·DeathRewards·Pickups)을 등록,
/// 개발 옵션(<c>--level</c>·<c>--weapons</c>·<c>--passives</c>), 방 사이 짐(<see cref="HeroCarrier"/>).
/// 모듈 인스턴스에는 상태가 없다(11.2 — 판 수명 상태는 짐·<c>RunState</c> 에만).
/// </summary>
public sealed class CombatModule : IGameModule
{
    public void Register(ModuleContext ctx)
    {
        foreach (var pattern in FirePatterns.All)
        {
            var p = pattern;
            ctx.Parts.Register<FirePatternParams>(PartKind.FirePattern, p.Id, _ => p);
        }
        foreach (var id in Targetings.Ids)
        {
            var type = id;
            ctx.Parts.Register<TargetingParams>(PartKind.Targeting, type, prm => Targetings.Create(type, prm.Radius), (prm, v) =>
            {
                if (type != "none" && prm.Radius <= 0f) v.Error("radius", $"조준 '{type}' 반경은 0 보다 커야 한다");
            });
        }
        WeaponEffects.Register(ctx.Parts);
        ctx.AddValidator(new WeaponTableValidator());
        ctx.AddWorldInstaller(Install);
        ctx.AddRunSetup(new HeroRunSetup());
        ctx.SetHeroCarry(new HeroCarrier());
    }

    /// <summary>월드 하나에 넣기(시험도 같은 함수 — <c>TestWorldBuilder.WithModules</c>).</summary>
    public static void Install(World world, SimulationBuilder builder)
    {
        world.Hostiles = new HostileQuery(world);
        world.Spawner = new CombatSpawner(world);
        world.Progression = new ProgressionService(world);
        HeroSetup.Create(world);
        builder.Add(SimPhase.HeroControl, new HeroControlSystem());
        builder.Add(SimPhase.Weapons, new WeaponsSystem());
        builder.Add(SimPhase.Projectiles, new ProjectilesSystem());
        builder.Add(SimPhase.Hazards, new HazardsSystem());
        builder.Add(SimPhase.DamageResolve, new DamageResolveSystem());
        builder.Add(SimPhase.DeathRewards, new DeathRewardsSystem());
        builder.Add(SimPhase.Pickups, new PickupsSystem());
    }
}

/// <summary>발사 패턴은 무기 정의의 type 이름으로만 쓰인다(매개변수 없음 — <c>WeaponDef.Pattern</c> 은 키가 아님).</summary>
public sealed record FirePatternParams;

/// <summary>
/// 무기 레벨 표 검사(13.5-2, WP3): 배열 길이 = <c>max_level</c>, 키 = 패턴 <c>LevelKeys</c> 뿐(모두 있어야 함), null 은 <c>NullableKeys</c> 에서만,
/// <c>cooldown</c> null 은 같은 레벨 <c>duration</c> 이 null 일 때만, 단조성(쿨다운은 늘지 않고 나머지는 줄지 않음 — <c>duration</c> null = 가장 큼, <c>cooldown</c> null 은 뺌).
/// 진화 무기: 레벨 표 없음·<c>override</c> 의 수치 키 ∈ 재료 패턴 <c>LevelKeys</c> ∪ 재료 <c>base</c> 키.
/// </summary>
public sealed class WeaponTableValidator : IDataValidator
{
    public string Name => "weapon_tables";

    public void Validate(GameDefs defs, ValidationContext ctx)
    {
        foreach (var w in defs.Weapons.All)
        {
            var path = $"weapons[{w.Id}]";
            if (w.BaseWeapon is not null)
            {
                if (w.Levels.Count > 0) ctx.Add(ctx.File, path, "진화 무기는 레벨 표가 없다(재료 무기 최고 레벨 위에 override)");
                if (!w.BaseWeapon.IsBound) continue;
                var b = w.BaseWeapon.Def;
                if (!FirePatterns.TryGet(b.Pattern, out var bp)) continue;
                if (w.Override?.Values is { } values)
                    foreach (var key in values.Keys)
                        if (!bp.LevelKeys.Contains(key) && !b.Base.ContainsKey(key))
                            ctx.Add(ctx.File, $"{path}.override.{key}", $"재료 패턴 '{bp.Id}' 의 레벨 표 키·재료 base 키가 아니다");
                continue;
            }
            if (!FirePatterns.TryGet(w.Pattern, out var pattern)) continue;   // 패턴 없는 무기(시작 자리만 채우는 시험 무기)·모르는 패턴은 로더가 본다
            foreach (var key in w.Levels.Keys)
                if (!pattern.LevelKeys.Contains(key)) ctx.Add(ctx.File, $"{path}.levels.{key}", $"패턴 '{pattern.Id}' 의 레벨 표 키가 아니다({string.Join(", ", pattern.LevelKeys)})");
            foreach (var key in pattern.LevelKeys)
            {
                if (!w.Levels.TryGetValue(key, out var column))
                {
                    if (key != "slow_pct") ctx.Add(ctx.File, $"{path}.levels", $"레벨 표에 '{key}' 가 없다(패턴 '{pattern.Id}')");
                    continue;
                }
                if (column.Length != w.MaxLevel) ctx.Add(ctx.File, $"{path}.levels.{key}", $"길이 {column.Length} ≠ max_level {w.MaxLevel}");
                var nullable = pattern.NullableKeys.Contains(key);
                float? prev = null;
                for (var i = 0; i < column.Length; i++)
                {
                    var v = column[i];
                    if (v is null)
                    {
                        if (!nullable) { ctx.Add(ctx.File, $"{path}.levels.{key}[{i}]", "null 은 상시·발동 없음 열에서만"); continue; }
                        if (key == "cooldown" && !(w.Levels.TryGetValue("duration", out var dur) && i < dur.Length && dur[i] is null))
                            ctx.Add(ctx.File, $"{path}.levels.cooldown[{i}]", "cooldown null 은 같은 레벨 duration 이 null 일 때만");
                        if (key == "duration") prev = float.PositiveInfinity;
                        continue;
                    }
                    if (prev is { } p)
                    {
                        var bad = key == "cooldown" ? v.Value > p : v.Value < p;
                        if (bad) ctx.Add(ctx.File, $"{path}.levels.{key}[{i}]", key == "cooldown" ? "쿨다운이 레벨이 오르며 늘었다" : "값이 레벨이 오르며 줄었다");
                    }
                    prev = v.Value;
                }
            }
        }
    }
}
