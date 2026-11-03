namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>단계 16 RoomFlow (v0.7, 3.5) — <see cref="RoomFlow.Step"/> 를 부르기만 한다. 방 없는 월드(<c>World.Room == null</c>)는 아무것도 안 한다.</summary>
public sealed class RoomFlowSystem : ISimSystem
{
    public void Tick(World world) => RoomFlow.Step(world);
}

/// <summary>
/// 방 흐름 상태 기계 (v0.7, 설계서 3.6 결정 3 — 구현 WP1e, 그 뒤 WP4. 단계 전이는 판 결과를 바꾸므로 계약 — 11.3).
/// 틱 끝(16단계, EndTick 앞)에 돈다 — 앞 단계들은 "이번 틱의 방 시계·단계" 를 읽고, 시계는 여기서 1 늘어난다.
/// <code>
/// Enter   방 시계 ≥ grace                         → Survive
/// Survive 일반: 방 시계 ≥ 생존 길이 / 정예: 중간 보스 Damage 처치 → Cleanup (+ RoomSurviveEnded)
///         보스: BossDefeated → 판의 마지막이면 승리, 아니면 무너짐 → Reward (+ RoomCleared(Boss))
/// Cleanup 살아 있는 적 0 / cleanup_limit 넘음(무너짐) → 판의 마지막이면 승리, 아니면 Reward (+ RoomCleared)
/// Reward  GatesExit 획득물·끌려오는 획득물·대기 0, 선택 창 없음 → 문 열기 → Doors
/// Doors   (나가는 자리에 들어섬 ∧ ExitArmed) 또는 CommandButtons.Exit — 대기 0·선택 창 없음·끌려오는 획득물 0 일 때만 → Exit
/// </code>
/// 주인공이 죽으면 어느 단계든 패배(EndTick) — <c>Outcome != Running</c> 이면 아무것도 안 한다.
/// <c>Hero.Dead</c> 는 나가는 전이(Reward → Doors, Doors → Exit)만 막는다 — 죽은 틱에 출구 틈에 들어서도 Exit 로 가지 않는다(EndTick(17)이 같은 틱 뒤에 <c>DefeatPending</c> 으로 바꾸므로).
/// 판의 마지막 방에서 같은 틱에 보스 처치·정리 끝이 오면 <see cref="World.BeginVictory"/> 가 먼저 불려 승리다(3.5 "같은 틱이면 승리가 먼저").
/// </summary>
public static class RoomFlow
{
    public static void Step(World world)
    {
        var room = world.Room;
        if (room is null || world.Outcome != RunOutcome.Running) return;
        var rules = world.Defs.Rules.Rooms ?? throw new InvalidOperationException("방 월드에 rules.rooms 가 없다");

        switch (room.Phase)
        {
            case RoomPhase.Enter:
                room.ClockTicks++;
                if (room.ClockTicks >= MathX.SecondsToTicks(rules.Grace, World.Dt)) room.EnterPhase(RoomPhase.Survive, world.Tick);
                break;
            case RoomPhase.Survive:
                room.ClockTicks++;
                StepSurvive(world, room, rules);
                break;
            case RoomPhase.Cleanup:
                room.ClockTicks++;
                room.CleanupTicks++;
                StepCleanup(world, room, rules);
                break;
            case RoomPhase.Reward:
                if (world.Hero.Dead || Blocked(world) || HasGatingPickup(world)) break;
                OpenDoors(world, room, rules);
                break;
            case RoomPhase.Doors:
                if (world.Hero.Dead) break;
                StepDoors(world, room);
                break;
        }
    }

    private static void StepSurvive(World world, RoomState room, RoomRulesDef rules)
    {
        switch (room.Plan.Kind)
        {
            case RoomKind.Normal:
                if (room.ClockTicks >= room.SurviveTicks) EndSurvive(world, room);
                break;
            case RoomKind.Elite:
                if (room.Plan.Def.Miniboss is not { } mb) break;
                var index = world.Defs.Enemies.IndexOf(mb.Id);
                foreach (ref readonly var k in world.Events.Current.EnemyKilled.AsSpan())
                    if (k.DefIndex == index && k.Cause == KillCause.Damage)
                    {
                        EndSurvive(world, room);
                        break;
                    }
                break;
            case RoomKind.Boss:
                if (world.Events.Current.BossDefeated.Count == 0) break;
                room.ClearTick = world.Tick;
                if (room.IsLastInRun) world.BeginVictory();
                else
                {
                    world.CollapseEnemies();
                    BeginReward(world, room, rules);
                }
                break;
        }
    }

    private static void EndSurvive(World world, RoomState room)
    {
        world.Events.Current.RoomSurviveEnded.Add(new RoomSurviveEnded(room.Plan.Kind));
        room.EnterPhase(RoomPhase.Cleanup, world.Tick);
    }

    private static void StepCleanup(World world, RoomState room, RoomRulesDef rules)
    {
        if (AliveEnemies(world) > 0)
        {
            if (room.CleanupTicks < room.CleanupLimitTicks) return;
            world.CollapseEnemies();
            room.Collapsed = true;
        }
        room.ClearTick = world.Tick;
        if (room.IsLastInRun) world.BeginVictory();
        else BeginReward(world, room, rules);
    }

    /// <summary>
    /// 보상 단계 시작: 보상 물체를 <c>reward_anchor</c> 에 놓고(<c>SpawnPickup</c> 이 <c>GatesExit</c> 를 단다 — WP3), 살아 있는 정예 상자(이번 틱에 생긴 것 포함)를
    /// <c>GatesExit</c> 로 묶어 그 둘레(<c>chest_ring</c>)로 옮긴다. 틱 이벤트·신호 <c>RoomCleared</c>.
    /// </summary>
    private static void BeginReward(World world, RoomState room, RoomRulesDef rules)
    {
        room.EnterPhase(RoomPhase.Reward, world.Tick);
        var center = world.Resolved!.Anchor(rules.RewardAnchor);

        var pickups = world.Pickups;
        var chests = 0;
        for (var i = 0; i < pickups.Count; i++)
            if (!pickups[i].Removed && pickups[i].Def.Kind == PickupKind.Chest) chests++;
        var k = 0;
        for (var i = 0; i < pickups.Count && chests > 0; i++)
        {
            var p = pickups[i];
            if (p.Removed || p.Def.Kind != PickupKind.Chest) continue;
            p.GatesExit = true;
            var angle = MathF.Tau * k / chests;
            var wish = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * rules.ChestRing;
            p.Pos = p.PrevPos = Place(world, center, wish, p.Radius);
            k++;
        }

        if (room.Reward is { } reward) world.Spawner.SpawnPickup(new PickupSpawn(reward.Object.Def, center, Reward: reward));

        world.Events.Current.RoomCleared.Add(new RoomCleared(room.Plan.Kind, room.Collapsed));
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.RoomCleared, center, null));
    }

    /// <summary>
    /// 상자 옮길 자리(4.10 <c>PlacementResolver.TryPlace</c> 와 같은 규칙: 맵 안이고 clearance ≥ 반경 + 4 면 그대로, 아니면 기준점 쪽으로 20px 씩 당김,
    /// 절반까지 없으면 기준점). TryPlace 본문(WP2)을 기다리지 않으려고 <c>INavWorld</c> 질의 둘로 직접 한다. <c>[해석]</c>
    /// </summary>
    private static Vector2 Place(World world, Vector2 center, Vector2 wish, float radius)
    {
        if (world.Nav is not { } nav) return wish;
        var toCenter = center - wish;
        var dist = toCenter.Length();
        var dir = dist > 1e-4f ? toCenter / dist : Vector2.Zero;
        for (var pulled = 0f; pulled <= dist * 0.5f; pulled += 20f)
        {
            var p = wish + dir * pulled;
            if (nav.IsInsideMap(p) && nav.Clearance(p) >= radius + 4f) return p;
        }
        return center;
    }

    /// <summary>
    /// 문 열기(3.6 "문 열기", 기획 11.4): 다음 방이 일반 방이면 <c>Plan.RewardOrder</c> 를 앞에서부터 보며 <c>IProgression.RewardAvailable</c> 인 것을 출구 수만큼,
    /// 모자라면 <c>fallback_reward</c>. 정예·보스 방이면 보상 없이 <c>NextKind</c>·<c>NextBoss</c>, 액트의 마지막 방이면 계단.
    /// </summary>
    private static void OpenDoors(World world, RoomState room, RoomRulesDef rules)
    {
        var plan = room.Plan;
        var gates = plan.ExitGates;
        var act = world.Run?.Act;
        var next = act is not null && plan.Index + 1 < act.Rooms.Count ? act.Rooms[plan.Index + 1] : null;
        var exits = new RoomExit[gates.Count];
        if (next is null)
        {
            var nextAct = act is null ? null : RunPlanner.Next(world.Defs, act.Act);
            for (var i = 0; i < gates.Count; i++) exits[i] = new RoomExit(gates[i], null, RoomKind.Normal, null, true, nextAct?.Name);
        }
        else if (next.Kind != RoomKind.Normal)
        {
            var who = next.Kind == RoomKind.Boss ? next.Def.Boss : next.Def.Miniboss;
            var boss = who is { IsBound: true } ? who.Def : who is not null && world.Defs.Enemies.TryGet(who.Id, out var d) ? d : null;
            for (var i = 0; i < gates.Count; i++) exits[i] = new RoomExit(gates[i], null, next.Kind, boss, false, null);
        }
        else
        {
            var picks = new List<RewardDef>(gates.Count);
            foreach (var r in plan.RewardOrder)
            {
                if (picks.Count == gates.Count) break;
                if (world.Progression?.RewardAvailable(r) ?? true) picks.Add(r);
            }
            while (picks.Count < gates.Count) picks.Add(rules.FallbackReward.Def);
            for (var i = 0; i < gates.Count; i++) exits[i] = new RoomExit(gates[i], picks[i], RoomKind.Normal, null, false, null);
        }

        room.Exits = exits;
        room.EnterPhase(RoomPhase.Doors, world.Tick);
        room.ExitArmed = HeroExitGate(world, room) is null;
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.DoorsOpened, world.Resolved!.Anchor(rules.RewardAnchor), null));
    }

    private static void StepDoors(World world, RoomState room)
    {
        var standing = HeroExitGate(world, room);
        if (standing is null) room.ExitArmed = true;
        if (Blocked(world)) return;

        string? chosen = null;
        if (standing is not null && room.ExitArmed) chosen = standing;
        else if ((world.Command.Buttons & CommandButtons.Exit) != 0 && world.Command.ExitIndex < room.Exits.Count)
            chosen = room.Exits[world.Command.ExitIndex].GateId;
        if (chosen is null) return;

        room.ChosenExit = chosen;
        room.EnterPhase(RoomPhase.Exit, world.Tick);
    }

    /// <summary>주인공 발 칸이 든 열린 출구 문(<c>Exits</c> 순서로 첫 번째). 없으면 null.</summary>
    private static string? HeroExitGate(World world, RoomState room)
    {
        if (world.Arena is not { } arena) return null;
        var cell = arena.Iso.CellOf(world.Hero.Pos);
        foreach (var e in room.Exits)
            if (world.Gates.TryGetValue(e.GateId, out var zone) && zone.Contains(cell)) return e.GateId;
        return null;
    }

    /// <summary>나가지 못하는 상태: 선택 창·대기(레벨업·상자·보상)·끌려오는 획득물.</summary>
    private static bool Blocked(World world)
    {
        if (world.AwaitingChoice != ChoiceKind.None) return true;
        if (world.Progression is { } p && (p.PendingLevelUps > 0 || p.PendingChests > 0 || p.PendingRewards > 0)) return true;
        var pickups = world.Pickups;
        for (var i = 0; i < pickups.Count; i++)
            if (!pickups[i].Removed && pickups[i].Magnetized) return true;
        return false;
    }

    private static bool HasGatingPickup(World world)
    {
        var pickups = world.Pickups;
        for (var i = 0; i < pickups.Count; i++)
            if (!pickups[i].Removed && pickups[i].GatesExit) return true;
        return false;
    }

    private static int AliveEnemies(World world)
    {
        var n = 0;
        var enemies = world.Enemies;
        for (var i = 0; i < enemies.Count; i++)
            if (enemies[i].IsAlive) n++;
        return n;
    }
}
