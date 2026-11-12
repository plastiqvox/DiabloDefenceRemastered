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
    /// <summary>
    /// 회복 문으로 들어온 방의 첫 틱에 채운 뒤의 체력(최대 × heal_amount 만큼, 최대에서 자름. 죽었거나 가득이면 그대로).
    /// <see cref="Step"/> 과 <c>RunSession</c> 의 ROOM <c>hp_in</c> 기록이 같이 쓴다 — 회복 방식을 바꾸면 이 하나만 고친다(단계 2 wp6).
    /// </summary>
    public static float HpAfterHealOnEnter(Hero hero, RoomRulesDef rules)
    {
        if (hero.Dead || hero.Hp >= hero.MaxHp) return hero.Hp;
        return Math.Min(hero.MaxHp, hero.Hp + hero.MaxHp * Math.Clamp(rules.Rewards.HealAmount, 0f, 1f));
    }

    public static void Step(World world)
    {
        var room = world.Room;
        if (room is null || world.Outcome != RunOutcome.Running) return;
        var rules = world.Defs.Rules.Rooms ?? throw new InvalidOperationException("방 월드에 rules.rooms 가 없다");

        // v0.9 회복 문: 들어온 방의 첫 시뮬레이션 틱(넘김 어둠이 걷힌 뒤)에 체력을 채운다(6.19 "회복 문").
        // 2026-11-12 사용자 결정: 체력 전부 → 최대 체력 × rules.rooms.rewards.heal_amount(최대를 넘지 않음)
        if (room.HealOnEnter)
        {
            room.HealOnEnter = false;
            var hero = world.Hero;
            hero.Hp = HpAfterHealOnEnter(hero, rules);
            world.Presentation.Cue(new CueEvent(world.Tick, CueKind.HealOnEnter, hero.Pos, room.Reward));
        }

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
                if (world.Hero.Dead || ExitBlocked(world) || HasGatingPickup(world)) break;
                if (room.Shop is { Visited: false }) break;   // v0.9 상인 방: 창을 한 번 닫아야 보상을 받은 것(6.19)
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
                        room.BossClockTicks = room.ClockTicks;
                        EndSurvive(world, room);
                        break;
                    }
                break;
            case RoomKind.Boss:
                if (world.Events.Current.BossDefeated.Count == 0) break;
                room.BossClockTicks = room.ClockTicks;
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
    /// 보상 단계 시작(v0.8 — 3.6 "보상 자리"): 보상 자리 = <c>RewardSpot.Find</c>(정리가 끝난 순간 주인공 앞). 놓을 것 = 방 보상(덤 보상 포함) 하나 + 살아 있는 정예 상자 전부
    /// (이번 틱에 생긴 것 포함 — 저장소 순서). 순서 = 보상 → 상자, 자리 = <c>RewardSpot.Row</c>(바라보는 방향의 수직 한 줄, <c>spacing</c> 간격).
    /// 보상은 <c>SpawnPickup</c>(<c>Reward != null</c> 이면 <c>GatesExit</c> — WP3), 상자는 여기서 <c>GatesExit</c> 를 달고 그 자리로 옮긴다. 틱 이벤트·신호 <c>RoomCleared</c>.
    /// v0.9: 방 안 물체가 없는 보상(회복 문 — 들어올 때 이미 받음, 상인 — <c>RoomState.Shop</c> 이 보상 자리에 섬)은 놓지 않는다(줄의 첫 자리 = 상인 자리).
    /// v0.7 의 <c>reward_anchor</c>(<c>center</c>)·<c>chest_ring</c> 은 없앴다.
    /// </summary>
    private static void BeginReward(World world, RoomState room, RoomRulesDef rules)
    {
        room.EnterPhase(RoomPhase.Reward, world.Tick);
        var spot = RewardSpot.Find(world, rules.RewardSpot, out var threshold);
        room.RewardSpot = spot;

        var pickups = world.Pickups;
        var count = room.Reward is null || (room.Reward.Object is null && room.Shop is null) ? 0 : 1;
        for (var i = 0; i < pickups.Count; i++)
            if (!pickups[i].Removed && pickups[i].Def.Kind == PickupKind.Chest) count++;
        var row = RewardSpot.Row(world.Nav, spot, world.Hero.Facing, count, rules.RewardSpot, threshold);   // 자리를 찾은 문턱으로 당김(v0.8.2)

        var k = 0;
        if (room.Reward is { Object: { } obj } reward) world.Spawner.SpawnPickup(new PickupSpawn(obj.Def, row[k++], Reward: reward));
        else if (room.Shop is not null && count > 0)
        {
            // 상인 자리 = 줄의 첫 자리(wp4 — 정예 상자가 함께 놓이면 줄 가운데가 spot 이 아니다). 그리기(WP5)·열기(WP3)·신호 자리가 RewardSpot 을 읽으므로 그것을 줄 첫 자리로
            room.RewardSpot = row[k++];
        }
        for (var i = 0; i < pickups.Count && k < row.Length; i++)
        {
            var p = pickups[i];
            if (p.Removed || p.Def.Kind != PickupKind.Chest) continue;
            p.GatesExit = true;
            p.Pos = p.PrevPos = row[k++];
        }

        world.Events.Current.RoomCleared.Add(new RoomCleared(room.Plan.Kind, room.Collapsed));
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.RoomCleared, spot, null));
    }

    /// <summary>
    /// 문 열기(3.6 "문 열기", 기획 11.4 — v0.8 → v0.9): 다음 방이 액트 안에 있으면 <b>종류와 상관없이</b> <c>RewardDraw.Pick</c>(문이 열리는 틱 상태의 함수 —
    /// 가중 바꿈·회복 문·상인 보장·거르기, 난수는 계획의 <c>RewardDraws</c> 만)으로 출구 수만큼, 모자라면 <c>fallback_reward</c>. 다음 방이 정예·보스 방이면 보상(= 덤)에 <c>NextKind</c>·<c>NextBoss</c> 를 더하고, 액트의 마지막 방이면 보상 없이 계단.
    /// 다음 방 층이 이 방과 다르면 <c>NextFloor</c>(계단 문도 — 다음 액트 첫 방의 층). 신호 <c>DoorsOpened</c> 의 자리 = <c>RoomState.RewardSpot</c>(v0.8.1).
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
            var floor = NextFloor(plan.Def, nextAct is { Rooms.Count: > 0 } ? nextAct.Rooms[0] : null);
            for (var i = 0; i < gates.Count; i++) exits[i] = new RoomExit(gates[i], null, RoomKind.Normal, null, true, nextAct?.Name, floor);
        }
        else
        {
            var progression = world.Progression;
            var picks = RewardDraw.Pick(act!.Act, plan, next, gates.Count, RewardDrawState.Of(world), rules, r => progression?.RewardAvailable(r) ?? true);

            EnemyDef? boss = null;
            if (next.Kind != RoomKind.Normal)
            {
                var who = next.Kind == RoomKind.Boss ? next.Def.Boss : next.Def.Miniboss;
                boss = who is { IsBound: true } ? who.Def : who is not null && world.Defs.Enemies.TryGet(who.Id, out var d) ? d : null;
            }
            var floor = NextFloor(plan.Def, next.Def);
            for (var i = 0; i < gates.Count; i++) exits[i] = new RoomExit(gates[i], picks[i], next.Kind, boss, false, null, floor);
        }

        room.Exits = exits;
        room.EnterPhase(RoomPhase.Doors, world.Tick);
        room.ExitArmed = HeroExitGate(world, room) is null;
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.DoorsOpened, room.RewardSpot, null));
    }

    /// <summary>다음 방 층이 이 방과 다르면 그 층, 같거나 다음 방이 층을 안 적었으면(0) 0.</summary>
    private static int NextFloor(RoomDef current, RoomDef? next) =>
        next is { Floor: > 0 } n && n.Floor != current.Floor ? n.Floor : 0;

    private static void StepDoors(World world, RoomState room)
    {
        var standing = HeroExitGate(world, room);
        if (standing is null) room.ExitArmed = true;
        if (ExitBlocked(world)) return;

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

    /// <summary>나가지 못하는 상태: 선택 창·대기(레벨업·상자·보상)·끌려오는 획득물·열린 상인 창(v0.9).</summary>
    internal static bool ExitBlocked(World world)
    {
        if (world.AwaitingChoice != ChoiceKind.None) return true;
        if (world.Room?.Shop is { Open: true }) return true;
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
