using DiabloDefenceRemastered.Core.Sim.Heroes;

namespace DiabloDefenceRemastered.Core.Sim.Combat;

/// <summary>
/// 단계 12 DamageResolve (WP3, 설계서 5.1): 피해 요청을 넣은 순서대로 한 곳에서 적용하고 큐를 비운다.
/// 적: 대상 없음·죽음·조준 불가(입장·등장 중)면 버림 → 치명(무기 난수, 확률 &gt; 0 일 때만 뽑음) → <see cref="DamageMath.HeroToEnemy"/> → HP·번쩍임·넉백 →
/// <c>HitLanded</c>·피해 숫자·신호 <c>EnemyHit</c>·<c>RunStats.DamageByWeapon[칸]</c> → HP ≤ 0 이면 <c>Dead</c>(<c>KillCause.Damage</c>).
/// 주인공: 판이 끝나 가면 모두 무시, 피격 무적 중이면 무시(신호 없음), <c>--god</c> 면 피해만 0(넉백은 받음) → <c>HeroDamaged</c>·<c>HeroHurt</c>·허리띠 → 0 이하면 <c>Dead</c>·<c>HeroDied</c>.
/// </summary>
public sealed class DamageResolveSystem : ISimSystem
{
    /// <summary>넉백이 걸리는 시간(초). 거리 / 이 값 = 넉백 속도(WP2 가 적분).</summary>
    public const float KnockbackDuration = 0.15f;
    /// <summary>번쩍임 시간(초).</summary>
    public const float HitFlash = 0.1f;
    /// <summary>받는 넉백 % 하한(기획 4.1).</summary>
    public const float KnockbackTakenFloor = -0.8f;

    public void Tick(World world)
    {
        var requests = world.Damage.Requests;
        var ownFrom = world.Combat.OwnEnemyRequestsFrom;
        for (var i = 0; i < requests.Count; i++)
        {
            ref readonly var r = ref requests[i];
            if (r.Target == EntityId.Hero) ToHero(world, r, bodyHit: i < ownFrom && (r.Flags & DamageFlags.FromHazard) == 0);
            else ToEnemy(world, r);
        }
        world.Damage.Clear();
        world.Combat.OwnEnemyRequestsFrom = int.MaxValue;
    }

    private static void ToEnemy(World world, in DamageRequest r)
    {
        if (!world.Enemies.TryGet(r.Target, out var e) || !e.IsTargetable || e.Faction == r.From) return;
        // 페이즈 전환 무적(WP4 Enemy.InvulnUntilTick — 통합 때 이음): 주인공 피격 무적처럼 요청 전체를 버린다(신호 없음)
        if (world.Tick < e.InvulnUntilTick) return;
        var dir = r.KnockbackDir.LengthSquared() > 1e-6f ? Vector2.Normalize(r.KnockbackDir) : Vector2.Zero;
        if ((r.Flags & DamageFlags.NoDamage) == 0)
        {
            var crit = r.CritChance > 0f && world.Rng.Weapon.Chance(r.CritChance);
            // 버프 방어(우두머리 저항의 오라 armor — 2026-11-14)는 맞는 순간 더한다
            var armor = world.Defs.Buffs.Count == 0 ? e.Armor : e.Armor + e.Buffs.Sum(EnemyStatKey.Armor, world.Defs.Buffs);
            var applied = DamageMath.HeroToEnemy(r.Amount, r.DamagePct, crit, DamageMath.Round(armor));
            e.Hp -= applied;
            e.HitFlashTime = HitFlash;
            world.Events.Current.HitLanded.Add(new HitLanded(r.Attacker, e.Id, applied));
            world.Presentation.Damage(new DamageNumber(world.Tick, e.Id, e.Pos, applied, crit, false));
            world.Presentation.Cue(new CueEvent(world.Tick, CueKind.EnemyHit, e.Pos, e.Def, e.Id));
            if (r.WeaponSlot >= 0 && r.WeaponSlot < world.RunStats.DamageByWeapon.Length) world.RunStats.DamageByWeapon[r.WeaponSlot] += applied;
            if (e.Hp <= 0f)
            {
                e.Hp = 0f;
                e.Dead = true;
                e.KillCause = KillCause.Damage;
                e.KilledBy = r.Attacker;
            }
        }
        if (r.KnockbackDist > 0f && dir != Vector2.Zero && (e.Immunities & Immunity.Knockback) == 0)
        {
            var dist = r.KnockbackDist * MathF.Max(0f, 1f - e.KnockbackResist);
            if (dist > 0f)
            {
                e.KnockbackVel = dir * (dist / KnockbackDuration);
                e.KnockbackTime = KnockbackDuration;
            }
        }
    }

    private static void ToHero(World world, in DamageRequest r, bool bodyHit)
    {
        var hero = world.Hero;
        if (world.Outcome != RunOutcome.Running || hero.Dead || r.From == Faction.Hero) return;
        // 보스 묶음 B(6.20 ⑱): 순간이동 도착 무적 동안은 피해·넉백을 모두 버린다(장판·몸·폭발 — 탄은 HitHero 가 판정부터 하지 않음). IgnoreInvuln 만 지나간다
        if (hero.BlinkInvulnTime > 0f && (r.Flags & DamageFlags.IgnoreInvuln) == 0) return;
        var dir = r.KnockbackDir.LengthSquared() > 1e-6f ? Vector2.Normalize(r.KnockbackDir) : Vector2.Zero;
        if ((r.Flags & DamageFlags.NoDamage) == 0)
        {
            if (hero.InvulnTime > 0f && (r.Flags & DamageFlags.IgnoreInvuln) == 0) return;
            var applied = world.Options.God ? 0 : DamageMath.ToHero(DamageMath.Round(r.Amount), DamageMath.Round(hero.Stats[StatKey.Armor]));
            hero.Hp -= applied;
            hero.InvulnTime = world.Defs.Rules.Hero?.InvulnAfterHit ?? 0.25f;
            hero.HitFlashTime = HitFlash;
            // 냉기 강화(2026-11-14, 기획 9.3): 들어간 피해에만 — 피격 무적으로 버린 요청은 둔화도 없다
            if ((r.Flags & DamageFlags.Chill) != 0 && world.Defs.Rules.Hero?.Chill is { } chill) hero.ChillTime = MathF.Max(hero.ChillTime, chill.Duration);
            world.RunStats.DamageTaken += applied;
            world.Events.Current.HeroDamaged.Add(new HeroDamaged(r.Attacker, applied, (int)MathF.Ceiling(MathF.Max(0f, hero.Hp))));
            world.Events.Current.HitLanded.Add(new HitLanded(r.Attacker, EntityId.Hero, applied));
            world.Presentation.Damage(new DamageNumber(world.Tick, EntityId.Hero, hero.Pos, applied, false, true));
            world.Presentation.Cue(new CueEvent(world.Tick, CueKind.HeroHurt, hero.Pos, null));
            if (bodyHit) CountHiddenHit(world, r.Attacker);
            if (hero.Hp <= 0f)
            {
                hero.Hp = 0f;
                hero.Dead = true;
                hero.Intent = MoveIntent.Hold;
                hero.Anim.Play(ClipKind.Die, loop: false);
                world.Presentation.Cue(new CueEvent(world.Tick, CueKind.HeroDied, hero.Pos, null));
                return;
            }
        }
        if (r.KnockbackDist > 0f && dir != Vector2.Zero)
        {
            var dist = r.KnockbackDist * (1f + MathF.Max(KnockbackTakenFloor, hero.Stats[StatKey.KnockbackTakenPct]));
            if (dist > 0f)
            {
                hero.KnockbackVel = dir * (dist / KnockbackDuration);
                hero.KnockbackTime = KnockbackDuration;
            }
        }
    }

    /// <summary>
    /// 공정성 <c>hidden_hits</c>(10.1): 적 본체(근접·자폭·돌진 — WP3 의 투사체·장판·예고가 넣은 요청이 아닌 것, 중간 보스·보스 제외)가 때린 순간
    /// <c>FogMath.EnemyAlpha</c> 가 0 이면 +1. 요청에 원인 칸이 없어 "이번 틱 Projectiles 단계 전에 들어온 요청" 으로 가른다(WP1g 요청 — 13장 WP3 기록).
    /// </summary>
    private static void CountHiddenHit(World world, EntityId attacker)
    {
        if (!world.Visibility.Enabled || !world.Enemies.TryGet(attacker, out var e) || e.Def.Rank != DefRank.Normal) return;
        if (FogMath.EnemyAlpha(world, e, world.Tick) <= 0f) world.Fairness.HiddenHits++;
    }
}
