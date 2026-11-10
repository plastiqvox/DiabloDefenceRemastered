using DiabloDefenceRemastered.Core.Sim.Heroes;

namespace DiabloDefenceRemastered.Core.Sim.Weapons;

/// <summary>
/// 단계 9 Weapons (WP3, 설계서 5.3): 칸마다 패턴 <c>Tick</c>(간격 발사·궤도) → 쿨다운 줄이기(궤도는 켜진 동안 멈춤) → 0 이 되면 <c>Fire</c>.
/// 쏘면 쿨다운 += 최종 쿨다운(레벨 표 null 이면 다시 쏘지 않음), 쏘지 못하면(유도형 대상 없음) 0 에서 기다린다.
/// 발동 한 번에 신호 <c>WeaponFired</c> 하나(여러 발이어도), 서 있으면 무기 <c>cast_clip</c> 재생(재생 중이면 그대로).
/// 주인공이 죽었거나 판이 끝나 가면 쏘지 않는다.
/// </summary>
public sealed class WeaponsSystem : ISimSystem
{
    public void Tick(World world)
    {
        var hero = world.Hero;
        if (hero.Dead || world.Outcome != RunOutcome.Running) return;
        var state = world.Combat;
        if (state.StatsDirty) HeroStats.Recompute(world);
        var surge = state.SurgeTicks > 0 ? state.SurgePct : 0f;
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
        {
            ref var slot = ref hero.Weapons[i];
            if (slot.Def is not { } def) continue;
            var pattern = FirePatterns.Of(def);
            var stats = WeaponStats.Resolve(def, slot.Level, hero.Stats, pattern, surge);
            pattern.Tick(world, i, ref slot, stats);
            if (pattern.HoldsCooldownWhileActive && slot.Pattern.Active) continue;
            if (float.IsPositiveInfinity(slot.Cooldown)) continue;
            slot.Cooldown -= World.Dt;
            if (slot.Cooldown > 1e-5f) continue;
            slot.Pattern.Counter = 0;
            if (!pattern.Fire(world, i, ref slot, stats))
            {
                slot.Cooldown = 0f;
                continue;
            }
            slot.Cooldown = stats.HasCooldown ? MathF.Max(0f, slot.Cooldown) + stats.Cooldown : float.PositiveInfinity;
            world.Presentation.Cue(new CueEvent(world.Tick, CueKind.WeaponFired, hero.Pos, def));
            if (hero.Intent.Kind == MoveKind.Hold && !(hero.Anim.Clip == def.CastClip && !hero.Anim.Done)) hero.Anim.Play(def.CastClip, loop: false);
        }
    }
}
