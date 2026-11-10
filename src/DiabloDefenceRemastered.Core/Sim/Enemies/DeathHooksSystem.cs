using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 13단계 <c>DeathHooks</c> (설계서 3.5·6.6 죽음). 이번 틱까지 죽은(아직 저장소에 있는) 적마다:
/// 진행 중 행동 끊기(<c>self_destruct</c> 는 폭발 없이 fizzle) → <c>Damage</c> 면 죽음 훅(분열·장판·폭발)·호위 공포 → <c>EnemyKilled</c> 틱 이벤트 →
/// 시체(<c>Culled</c> 는 없음) → 보스 처치면 <c>BossDefeated</c>(방 없는 월드면 <see cref="World.BeginVictory"/>) → 중간 보스·우두머리 처치 히트 스톱 →
/// 보스 막대에서 빼고 저장소에서 지움(압축은 EndTick).
/// <c>Collapse</c>·<c>Culled</c> 는 행동만 끊고 훅을 부르지 않는다(분열·폭발·웅덩이 없음).
/// </summary>
public sealed class DeathHooksSystem : ISimSystem
{
    /// <summary>중간 보스·우두머리 처치 히트 스톱(앱 틱, 0.08s — 기획 12.6).</summary>
    public const int HitStopTicks = 5;

    public void Tick(World world)
    {
        var enemies = world.Enemies;
        var count = enemies.Count;   // 죽음 훅이 더한 적(분열체)은 이번 틱에 다시 보지 않는다
        for (var i = 0; i < count; i++)
        {
            var e = enemies[i];
            if (e.Removed || !e.Dead) continue;
            Process(world, e);
        }
    }

    private static void Process(World world, Enemy e)
    {
        var ctx = new EnemyCtx(world, e);
        var why = e.KillCause switch
        {
            KillCause.Damage => InterruptReason.Death,
            KillCause.Collapse => InterruptReason.Collapse,
            _ => InterruptReason.Culled,
        };
        EnemyThinkSystem.Interrupt(ctx, why);

        if (e.KillCause == KillCause.Damage)
        {
            var kit = e.Kit;
            foreach (var s in kit.DeathHooks)
                if (kit.Part(e.PhaseIdx, s) is IDeathHookPart hook) hook.OnDeath(ctx);
            FearFollowers(world, e);
        }

        world.Events.Current.EnemyKilled.Add(new EnemyKilled(e.Id, e.DefIndex, e.Pos, e.Rank, e.KillCause, e.KilledBy));

        if (e.KillCause != KillCause.Culled && e.Def.Sprites.IsBound)
        {
            var v = e.Visual;
            world.Presentation.Corpse(new CorpseSpawned(world.Tick, e.Def, e.Def.Sprites.Def, e.Def.Variant, e.Pos, e.Facing, e.Def.Tint,
                                                        e.FinalScale, v.AlphaMul, v.GlowColor, v.GlowAlpha, e.Def.Death));
        }

        if (e.KillCause == KillCause.Damage)
        {
            if (e.Def.Rank == DefRank.Boss)
            {
                world.Events.Current.BossDefeated.Add(new BossDefeated(e.Id, e.DefIndex, e.Pos));
                if (world.Room is null) world.BeginVictory();
            }
            if (e.Def.Rank == DefRank.Miniboss || e.Rank == EnemyRank.Unique) world.Freeze(HitStopTicks);
        }

        for (var b = world.BossBars.Count - 1; b >= 0; b--)
            if (world.BossBars[b].Id == e.Id) world.BossBars.RemoveAt(b);
        world.Enemies.Remove(e.Id);
    }

    /// <summary>대장이 죽으면 그 대장을 <c>Leader</c> 로 가진 적만 공포(기획 8.3).</summary>
    private static void FearFollowers(World world, Enemy leader)
    {
        var fear = 0f;
        foreach (var s in leader.Kit.SpawnHooks)
            if (leader.Kit.Part(0, s) is Escort esc) fear = esc.Params.FearOnLeaderDeath;
        if (fear <= 0f) return;
        var until = world.Tick + MathX.SecondsToTicks(fear, World.Dt);
        foreach (var e in world.Enemies.Items)
        {
            if (!e.IsAlive || e.Leader != leader.Id) continue;
            e.Leader = EntityId.None;
            e.Order = new MoveOrder { Kind = OrderKind.Flee, UntilTick = until };
        }
    }
}
