namespace DiabloDefenceRemastered.Core.Sim.Core;

/// <summary>
/// 단계 17 EndTick (WP1, 설계서 3.5 — v0.7 에 16 → 17): 승패 판정 타이머, 승리 대기·방 보상 단계 이후(<c>Room.Phase ≥ Reward</c>)면 적 투사체·장판·예고 지우기,
/// 레벨업 → 상자 → 보상 대기 순으로 AwaitingChoice, 저장소 압축, 디버그 빌드 불변식(위치가 유한한가), 스테이지 시계 진행.
/// v0.7: <c>survive</c> 승리는 없앴다 — <see cref="World.BeginVictory"/> 를 방 월드는 판의 마지막 방에서 <c>RoomFlow</c>(정리 끝·보스 처치)가, 방 없는 월드는 <c>DeathHooks</c>(보스)가 부른다.
/// </summary>
public sealed class EndTickSystem : ISimSystem
{
    /// <summary>승리 대기 → 결과까지(기획 2.2).</summary>
    public const float VictoryDelay = 3f;
    /// <summary>패배 대기 → 결과까지.</summary>
    public const float DefeatDelay = 2f;

    public void Tick(World world)
    {
        UpdateOutcome(world);
        UpdateChoice(world);

        world.Enemies.Compact();
        world.Projectiles.Compact();
        world.Hazards.Compact();
        world.Telegraphs.Compact();
        world.Pickups.Compact();

#if DEBUG
        CheckFinite(world);
#endif

        world.Tick++;
    }

    private static void UpdateOutcome(World world)
    {
        if (world.Outcome == RunOutcome.Running)
        {
            // 승리는 앞 단계(방 월드 RoomFlow·방 없는 월드 DeathHooks)가 이미 VictoryPending 으로 바꿨다 — 그러면 여기 오지 않는다
            // (같은 틱에 주인공 HP 가 0 이어도 승리가 먼저 — RoomFlow 는 Hero.Dead 여도 정리 끝·보스 처치를 본다)
            if (world.Hero.Dead)
            {
                world.Outcome = RunOutcome.DefeatPending;
                world.OutcomeTick = world.Tick;
            }
        }

        // 방 보상 단계부터(무너짐 뒤) 남은 적 공격을 지운다 — 승리 대기와 같은 함수(3.6)
        if (world.Room is { Phase: >= RoomPhase.Reward } && world.Outcome == RunOutcome.Running) ClearEnemyAttacks(world);

        var elapsed = (world.Tick - world.OutcomeTick + 1) * World.Dt;
        switch (world.Outcome)
        {
            case RunOutcome.VictoryPending:
                ClearEnemyAttacks(world);
                if (elapsed >= VictoryDelay - World.Dt * 0.5f) world.Outcome = RunOutcome.Victory;
                break;
            case RunOutcome.DefeatPending:
                if (elapsed >= DefeatDelay - World.Dt * 0.5f) world.Outcome = RunOutcome.Defeat;
                break;
        }
    }

    private static void ClearEnemyAttacks(World world)
    {
        var projectiles = world.Projectiles;
        for (var i = 0; i < projectiles.Count; i++)
            if (projectiles[i].Faction == Faction.Enemy) projectiles.RemoveAt(i);
        var hazards = world.Hazards;
        for (var i = 0; i < hazards.Count; i++)
            if (hazards[i].Faction == Faction.Enemy) hazards.RemoveAt(i);
        var telegraphs = world.Telegraphs;
        for (var i = 0; i < telegraphs.Count; i++)
            if (telegraphs[i].Faction == Faction.Enemy) telegraphs.RemoveAt(i);
    }

    private static void UpdateChoice(World world)
    {
        if (world.AwaitingChoice != ChoiceKind.None || world.Outcome != RunOutcome.Running) return;
        if (world.Progression is not { } progression) return;
        if (progression.PendingLevelUps > 0) world.AwaitingChoice = ChoiceKind.LevelUp;
        else if (progression.PendingChests > 0) world.AwaitingChoice = ChoiceKind.Chest;
        else if (progression.PendingRewards > 0) world.AwaitingChoice = ChoiceKind.Reward;
    }

#if DEBUG
    private static void CheckFinite(World world)
    {
        if (!MathX.IsFinite(world.Hero.Pos))
            throw new InvalidOperationException($"주인공 위치가 유한하지 않다: {world.Hero.Pos} (tick {world.Tick})");
        foreach (var e in world.Enemies.Items)
            if (!MathX.IsFinite(e.Pos))
                throw new InvalidOperationException($"적 {e.Id} ({e.Def?.Id}) 위치가 유한하지 않다: {e.Pos} (tick {world.Tick})");
        foreach (var p in world.Projectiles.Items)
            if (!MathX.IsFinite(p.Pos))
                throw new InvalidOperationException($"투사체 {p.Id} 위치가 유한하지 않다: {p.Pos} (tick {world.Tick})");
    }
#endif
}
