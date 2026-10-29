namespace DiabloDefenceRemastered.Core.Sim.Core;

/// <summary>
/// 단계 16 EndTick (WP1, 설계서 3.5): 승패 판정 타이머, 승리 대기면 적 투사체·장판·예고 지우기, 레벨업·상자 대기 → AwaitingChoice,
/// 저장소 압축, 디버그 빌드 불변식(위치가 유한한가), 스테이지 시계 진행.
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
            // 승리가 먼저다(같은 틱에 주인공 HP 도 0 이어도)
            if (world.Stage?.Victory is { Type: VictoryKind.Survive, At: { } at }
                && world.Tick + 1 >= MathX.SecondsToTicks(at.Seconds, World.Dt))
                world.BeginVictory();
            else if (world.Hero.Dead)
            {
                world.Outcome = RunOutcome.DefeatPending;
                world.OutcomeTick = world.Tick;
            }
        }

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
