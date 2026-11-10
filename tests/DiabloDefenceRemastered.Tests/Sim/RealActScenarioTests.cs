using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Core.Sim.Movement.Invariants;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 합친 뒤의 액트 1 판 (설계서 10.1 <c>Sim</c> — 통합): 실제 <c>Data/</c> 를 모든 모듈로 읽고, 실제 모듈(이동·충돌·전투·적·감독)로 <c>stage_cathedral</c> 을
/// <c>RunSession</c> + <c>AutoplayPolicy</c> + <c>--god</c>·<c>--check</c> 로 끝까지 돈다. 가짜 이웃(<see cref="FakeArena"/>)을 쓰지 않는다는 점이 <see cref="Stage1ScenarioTests"/> 와 다르다.
/// 선택 창(레벨업·상자·문 보상)은 오버레이(WP6)가 하듯 정책의 <c>PickCard</c> 로 고른다.
/// </summary>
public sealed class RealActScenarioTests(ITestOutputHelper output)
{
    private sealed record Outcome(RunOutcome Result, RunState Run, AutoplayPolicy Policy, InvariantReport Invariants, FairnessCounters Fairness, int Ticks);

    [Fact, Trait("Category", TestCategories.Scenario)]
    public void Act1_RealDataAndModules_GodRunReachesVictory_InvariantsAndFairnessClean()
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var r = Run(7);
        foreach (var room in r.Run.Rooms)
            output.WriteLine($"ROOM act={room.Act} room={room.Number} map={room.MapId} reward={room.RewardId ?? "-"} door={room.Door ?? "-"} clear_tick={room.ClearTick}");
        output.WriteLine($"ticks={r.Ticks} ({r.Ticks * World.Dt / 60f:0.0}분) occluded={r.Invariants.Occluded} kite_switches={r.Fairness.KiteSwitches}");

        Assert.Equal(RunOutcome.Victory, r.Result);
        Assert.True(r.Invariants.Passed, $"불변식: nan={r.Invariants.Nan} out={r.Invariants.OutOfMap} over_cap={r.Invariants.OverCap} in_obstacle_longest={r.Invariants.InObstacleLongest}");
        Assert.Equal(0, r.Fairness.UnseenShots);
        Assert.Equal(0, r.Fairness.UnseenCharges);
        Assert.Equal(0, r.Fairness.HiddenHits);
        Assert.Empty(r.Policy.DoorSkips);
        Assert.Empty(r.Policy.RewardStalls);
    }

    [Fact, Trait("Category", TestCategories.Scenario)]
    public void Act1_RealDataAndModules_SameSeedSameRoomRecords()
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var a = Run(11);
        var b = Run(11);
        Assert.Equal(a.Run.Rooms, b.Run.Rooms);
        Assert.Equal(a.Ticks, b.Ticks);
    }

    private static Outcome Run(ulong seed)
    {
        var ctx = GameModules.CreateContext();
        var data = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(data.HasErrors, string.Join("\n", data.Errors));
        var defs = data.Defs;
        var session = new RunSession(defs, GameModules.CreateContext(),
            new RunStart(seed, defs.Stages.Get("stage_cathedral"), null, new RunOptions { God = true, Check = true }, 0));
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var invariants = new InvariantReport();
        var fairness = new FairnessCounters();
        World? attached = null;
        var ticks = 0;
        var limit = MathX.SecondsToTicks(40 * 60f, World.Dt);   // 판 하나 40분 상한(단계 1 판은 10분 남짓)
        while (true)
        {
            var w = session.World;
            if (!ReferenceEquals(attached, w))
            {
                Collect(attached);
                attached = w;
            }
            if (w.AwaitingChoice != ChoiceKind.None)
            {
                Choose(w, policy);
                continue;
            }
            Assert.True(ticks < limit, $"{ticks} 틱 안에 판이 끝나지 않았다(방 {w.Room?.Plan.Number} 단계 {w.Room?.Phase})");
            w.Command = policy.Next(w);
            w.Simulation!.Step();
            ticks++;
            if (w.Outcome is RunOutcome.Victory or RunOutcome.Defeat)
            {
                Collect(w);
                return new Outcome(w.Outcome, session.Conclude(), policy, invariants, fairness, ticks);
            }
            if (w.Room is { Phase: RoomPhase.Exit }) session.Advance();
        }

        void Collect(World? w)
        {
            if (w is null) return;
            w.Invariants.AddTo(invariants);
            w.Fairness.AddTo(fairness);
        }
    }

    /// <summary>오버레이(WP6 <c>LevelUpOverlay</c>·<c>ChestOverlay</c>)가 자동 조작에서 하는 고르기.</summary>
    private static void Choose(World w, AutoplayPolicy policy)
    {
        var p = w.Progression!;
        switch (w.AwaitingChoice)
        {
            case ChoiceKind.Chest:
                p.OpenChest();
                break;
            case ChoiceKind.Reward:
            {
                var offer = p.RewardOffer();
                p.ChooseReward(Math.Clamp(policy.PickCard(offer), 0, offer.Cards.Count - 1));
                break;
            }
            default:
            {
                var offer = p.Offer();
                if (offer.Cards.Count == 0) p.Skip();
                else p.Choose(Math.Clamp(policy.PickCard(offer), 0, offer.Cards.Count - 1));
                break;
            }
        }
    }
}
