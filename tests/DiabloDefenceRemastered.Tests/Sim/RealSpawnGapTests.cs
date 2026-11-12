using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Core.Sim.Director;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 첫 플레이(2026-11-11) 관찰 "방 2 의 1:12 에 적이 하나도 없는 틈" — 실제 데이터 판에서 생존 단계(유예 뒤) 틱마다 살아 있는 적 수(시야 밖 포함)를 세어
/// 0 이 이어지는 가장 긴 구간을 본다. 방 첫머리(유예 3초 뒤 누적기가 0 에서 차오르는 동안 — 기획 2.4·2.5 대로)는 따로 적고,
/// 그 방에 첫 적이 나온 뒤의 틈도 단언한다. 관찰된 "방 2 의 1:12"(남은 시간 — 방 2 는 75초라 방 시계 0:03 = 유예 끝)는 이 첫머리였다(최대 2.5초) —
/// 방의 첫 무리를 생성이 열린 틱에 바로 내게 고친 뒤로는 첫머리도 0.5초 안이어야 한다.
/// </summary>
public sealed class RealSpawnGapTests(ITestOutputHelper output)
{
    /// <summary>생존 단계에서 적 0 이 이만큼(초) 넘게 이어지면 틈으로 본다.</summary>
    private const float MaxEmptySeconds = 2f;

    [Theory, Trait("Category", TestCategories.Scenario)]
    [InlineData(1UL)]
    [InlineData(7UL)]
    [InlineData(23UL)]
    public void Act1_SurvivePhase_NeverEmptyForLong(ulong seed)
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var ctx = GameModules.CreateContext();
        var data = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(data.HasErrors, string.Join("\n", data.Errors));
        var defs = data.Defs;
        var session = new RunSession(defs, GameModules.CreateContext(),
            new RunStart(seed, defs.Stages.Get("stage_cathedral"), null, new RunOptions { God = true }, 0));
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var limit = MathX.SecondsToTicks(40 * 60f, World.Dt);
        var worst = 0;
        var worstAt = "";
        var streak = 0;
        var startWorst = 0;
        var startStreak = 0;
        RoomState? seenRoom = null;
        for (var ticks = 0; ticks < limit; ticks++)
        {
            var w = session.World;
            while (w.AwaitingChoice != ChoiceKind.None) Choose(w, policy);
            w.Command = policy.Next(w);
            w.Simulation!.Step();
            if (w.Outcome is RunOutcome.Victory or RunOutcome.Defeat) break;
            // 해골왕 처치 뒤 승리 대기(3s — 남은 적은 무너짐)는 판이 끝나는 중이라 틈으로 보지 않는다. 보스 방 "보스 중" 이 40/1.2(v0.9.5 — 기획 10.3)로 줄어
            // 그 3초 안에 새 무리가 오지 않을 수 있다(보스 묶음 A 에서 드러남 — 씨앗 7 의 2.98s)
            if (w.Outcome == RunOutcome.VictoryPending) { streak = 0; continue; }
            if (w.Room is { } room && room.SpawningOpen)
            {
                var alive = SpawnGate.Alive(w);
                if (alive > 0) seenRoom = room;
                if (!ReferenceEquals(seenRoom, room))
                {
                    startStreak++;
                    startWorst = Math.Max(startWorst, startStreak);
                }
                else if (alive == 0) streak++;
                else streak = 0;
                if (streak > worst)
                {
                    worst = streak;
                    worstAt = $"방 {room.Plan.Number} 방 시계 {room.ClockTicks * World.Dt:0.0}s 누적 {w.DirectorState.Accumulator:0.00} 다음 {w.DirectorState.NextDef?.Id}×{w.DirectorState.NextSize}"
                              + $" 띠 {w.DirectorState.BandSpawns} 입구 대체 {w.DirectorState.EntryFallbacks} 낸 수 {w.DirectorState.IntervalSpawned}";
                }
            }
            else streak = startStreak = 0;
            if (w.Room is { Phase: RoomPhase.Exit }) session.Advance();
        }
        Assert.True(startWorst * World.Dt <= 0.5f, $"유예 뒤 첫 적까지 {startWorst * World.Dt:0.00}s — 방의 첫 무리는 생성이 열린 틱에 나와야 한다");
        output.WriteLine($"seed={seed} 방 첫머리(유예 뒤 첫 적까지) 가장 김 = {startWorst * World.Dt:0.00}s, 첫 적 뒤 가장 긴 적 0 = {worst * World.Dt:0.00}s ({worstAt})");
        Assert.True(worst * World.Dt <= MaxEmptySeconds, $"생존 단계에서 적 0 이 {worst * World.Dt:0.00}s 이어졌다: {worstAt}");
    }

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
