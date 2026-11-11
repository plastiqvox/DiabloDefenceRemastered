using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Core.Sim.Movement.Invariants;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 실제 데이터·실제 모듈의 액트 1 판 (설계서 10.1 <c>Sim</c>, 11.1 WP4 단계 2 — 지시 7): 실제 <c>Data/</c> 를 모든 모듈로 읽고 <c>stage_cathedral</c>(방 아홉 — 정예 5·7, 보스 9 해골왕)을
/// <c>RunSession</c> + <c>AutoplayPolicy</c> + <c>--god</c>·<c>--check</c> 로 해골왕 처치(승리, 깊이 1:9)까지. 선택 창은 오버레이(WP6)가 하듯 정책의 <c>PickCard</c> 로 고르고,
/// 상인 창은 게임처럼 열린 동안 앱 틱마다 정책 명령을 <c>StageStepper.AppTick</c> 에 준다(WP3 <c>ShopInput</c>).
/// 두 판: 상인 가중을 0 으로 바꾼 액트(시험 안 <c>with</c>)와 실제 가중 그대로(상인 방에서 하나 사고 떠남 — wp3 의 <c>World.ShopService</c> 가 main 에 합쳐져 늘 돈다).
/// </summary>
public sealed class RealActScenarioTests(ITestOutputHelper output)
{
    private sealed record Outcome(RunOutcome Result, RunState Run, AutoplayPolicy Policy, InvariantReport Invariants, FairnessCounters Fairness, int Ticks);

    private static readonly Lazy<GameDefs> Real = new(() =>
    {
        var ctx = GameModules.CreateContext();
        var data = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(data.HasErrors, string.Join("\n", data.Errors));
        return data.Defs;
    });

    /// <summary>상인 가중을 0 으로 바꾼 액트 1(시험 안에서만 — 저장소 데이터는 실제 가중).</summary>
    private static StageDef NoMerchants(StageDef act) => act with
    {
        Rewards = act.Rewards with { Weights = [.. act.Rewards.Weights.Where(w => w.Ref.Def.Kind != RewardKind.Merchant)] },
    };

    [Theory, Trait("Category", TestCategories.Scenario)]
    [InlineData(7ul)]
    [InlineData(3ul)]
    public void Act1_NoMerchants_GodRunReachesSkeletonKingVictory_InvariantsAndFairnessClean(ulong seed)
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var act = NoMerchants(Real.Value.Stages.Get("stage_cathedral"));
        Assert.DoesNotContain(act.Rewards.Weights, w => w.Ref.Def.Kind == RewardKind.Merchant);
        var r = Run(seed, act);
        Log(r);
        AssertClean(r);
    }

    [Fact, Trait("Category", TestCategories.Scenario)]
    public void Act1_RealWeights_MerchantRoomBuysOneAndLeaves()
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        Assert.NotNull(GameModules.CreateContext());   // ShopService 는 WP3 모듈이 World 에 넣는다(main c84ea8e 부터)
        // 상인 문이 나와 자동 조작이 고르는 씨앗을 찾는다(상인 문 = 금화 ≥ 150 일 때 보물 상자 순위, 기획 15절) — 여러 씨앗 중 첫 판
        Outcome? found = null;
        foreach (var seed in new ulong[] { 7, 3, 11, 5, 13, 17, 19, 23 })
        {
            var r = Run(seed, Real.Value.Stages.Get("stage_cathedral"));
            AssertClean(r);
            output.WriteLine($"seed={seed} shops={string.Join(",", r.Run.Rooms.Where(x => x.Shop is not null).Select(x => $"{x.Number}:{x.Shop}"))} gold_spent={r.Run.Stats.GoldSpent}");
            if (r.Run.Rooms.Any(x => x.Shop is not null)) { found = r; Log(r); break; }
        }
        Assert.NotNull(found);
        var run = found!.Run;
        var shopRoom = run.Rooms.First(x => x.Shop is not null);
        // 상인 방이 보상 단계를 지났다(다음 방 기록이 있음 = 문이 열려 나감 — 창을 닫아야 열린다)
        Assert.Contains(run.Rooms, x => x.Number > shopRoom.Number);
        Assert.True(shopRoom.GoldSpent > 0, "상인 방에서 산 것이 없다");                    // 하나 사고 떠남(RunStats.GoldSpent 의 방 차이)
        output.WriteLine($"shop room={shopRoom.Number} shop={shopRoom.Shop} gold_spent={shopRoom.GoldSpent} run_gold_spent={run.Stats.GoldSpent}");
    }

    [Fact, Trait("Category", TestCategories.Scenario)]
    public void Act1_RealData_SameSeedSameRoomRecords()
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var act = Real.Value.Stages.Get("stage_cathedral");
        var a = Run(11, act);
        var b = Run(11, act);
        Assert.Equal(a.Run.Rooms, b.Run.Rooms);
        Assert.Equal(a.Ticks, b.Ticks);
    }

    private void Log(Outcome r)
    {
        foreach (var room in r.Run.Rooms)
            output.WriteLine($"ROOM act={room.Act} room={room.Number} map={room.MapId} reward={room.RewardId ?? "-"} door={room.Door ?? "-"} clear_tick={room.ClearTick} " +
                             $"boss_s={room.BossClockTicks * World.Dt:0.0} gold_spent={room.GoldSpent} shop={room.Shop ?? "-"} potions={room.Potions} drunk={room.Drunk}");
        output.WriteLine($"ticks={r.Ticks} ({r.Ticks * World.Dt / 60f:0.0}분) occluded={r.Invariants.Occluded} kite_switches={r.Fairness.KiteSwitches} " +
                         $"unseen_shots={r.Fairness.UnseenShots} unseen_charges={r.Fairness.UnseenCharges} hidden_hits={r.Fairness.HiddenHits}");
    }

    private static void AssertClean(Outcome r)
    {
        Assert.Equal(RunOutcome.Victory, r.Result);
        Assert.Equal(9, r.Run.Rooms[^1].Number);                                     // 깊이 1:9 — 해골왕 처치
        Assert.Equal("cathedral_arena", r.Run.Rooms[^1].MapId);
        Assert.True(r.Invariants.Passed, $"불변식: nan={r.Invariants.Nan} out={r.Invariants.OutOfMap} over_cap={r.Invariants.OverCap} in_obstacle_longest={r.Invariants.InObstacleLongest} stuck_max={r.Invariants.StuckMax}/{r.Invariants.StuckMaxAlive}");
        Assert.Equal(0, r.Fairness.UnseenShots);
        Assert.Equal(0, r.Fairness.UnseenCharges);
        Assert.Equal(0, r.Fairness.HiddenHits);
        Assert.Empty(r.Policy.DoorSkips);
        Assert.Empty(r.Policy.RewardStalls);
    }

    private static Outcome Run(ulong seed, StageDef act)
    {
        var defs = Real.Value;
        var session = new RunSession(defs, GameModules.CreateContext(), new RunStart(seed, act, null, new RunOptions { God = true, Check = true }, 0));
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var invariants = new InvariantReport();
        var fairness = new FairnessCounters();
        World? attached = null;
        var ticks = 0;
        var appTicks = 0;
        var limit = MathX.SecondsToTicks(40 * 60f, World.Dt);   // 판 하나 40분 상한(액트 1 은 약 16분)
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
            Assert.True(ticks < limit && appTicks < 2 * limit, $"{ticks} 틱 안에 판이 끝나지 않았다(방 {w.Room?.Plan.Number} 단계 {w.Room?.Phase})");
            // 게임 PlayScene 과 같은 호출 순서: 앱 틱마다 Next → AppTick(상인 창이 열려 있으면 판 틱 대신 창 입력)
            w.Command = policy.Next(w);
            appTicks++;
            if (StageStepper.AppTick(w, w.Command) == StageStep.Stepped) ticks++;
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
