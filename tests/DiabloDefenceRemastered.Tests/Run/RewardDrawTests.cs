namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 10.1 <c>Run</c> 행(v0.9 WP1g #11 — 설계서 6.19 "회복 문", 기획 11.4 "뽑기"·11.7): 문 보상 뽑기 <c>RewardDraw.Pick</c> — 회복 문 보장(첫 문, 거울판이면 짝)·빠짐(75%),
/// 상인 조건(액트 1 은 3번 방 문부터·액트마다 둘·바로 앞 방·한 문 단계 하나)·상인 보장(둘째 문, 겹치면 회복이 첫 문)·가중 바꿈, 난수 수 고정(보장이 다른 문·다음 방을 밀지 않음).
/// </summary>
public class RewardDrawTests
{
    private static readonly GameDefs Base = RoomActs.Defs();
    private static RoomRulesDef Rules => Base.Rules.Rooms!;
    private static RewardDef R(string id) => Base.Rewards.Get(id);

    /// <summary>합성 액트 1 을 일반 방 다섯 + 보스로(상인은 다음 방이 일반 방일 때만 — RoomActs 의 4번 방은 정예), 가중 = 마법서 30·상자 30·회복 15·상인 8·8·6.</summary>
    private static StageDef Act(bool merchants = true, GameDefs? defs = null)
    {
        defs ??= Base;
        var act = defs.Stages.Get(RoomActs.Act1Id);
        List<WeightEntry<RewardDef>> w =
        [
            new(DefRef<RewardDef>.To(defs.Rewards.Get(TestDefs.RewardTomeId)), 30f),
            new(DefRef<RewardDef>.To(defs.Rewards.Get(TestDefs.RewardChestId)), 30f),
            new(DefRef<RewardDef>.To(defs.Rewards.Get(TestDefs.RewardPotionId)), 15f),
        ];
        if (merchants)
        {
            w.Add(new(DefRef<RewardDef>.To(defs.Rewards.Get(TestDefs.RewardSmithId)), 8f));
            w.Add(new(DefRef<RewardDef>.To(defs.Rewards.Get(TestDefs.RewardWitchId)), 8f));
            w.Add(new(DefRef<RewardDef>.To(defs.Rewards.Get(TestDefs.RewardHealerId)), 6f));
        }
        var boss = act.Rooms[^1];
        return act with
        {
            Rooms =
            [
                TestDefs.Room(RoomKind.Normal, 0f, 60f, 1), TestDefs.Room(RoomKind.Normal, 60f, 120f, 2), TestDefs.Room(RoomKind.Normal, 120f, 180f, 3),
                TestDefs.Room(RoomKind.Normal, 180f, 240f, 4), TestDefs.Room(RoomKind.Normal, 240f, 300f, 5), boss with { From = new ClockTime(300f) },
            ],
            Rewards = new StageRewardsDef { Weights = w, FirstRoom = DefRef<RewardDef>.To(defs.Rewards.Get(TestDefs.RewardTomeId)) },
            Events = [],
        };
    }

    private static RewardDrawState State(float hp = -1f, int potions = 1, int merchantRooms = 0, bool shown = false, bool lastMerchant = false, IReadOnlyList<string>? met = null) =>
        new(hp, potions, met ?? [], merchantRooms, shown, lastMerchant);

    private static RewardDef[] Pick(StageDef act, ActPlan plan, int index, in RewardDrawState st) =>
        RewardDraw.Pick(act, plan.Rooms[index], plan.Rooms[index + 1], plan.Rooms[index].ExitGates.Count, st, Rules, _ => true);

    private static bool IsMerchant(RewardDef r) => r.Kind == RewardKind.Merchant;

    [Fact]
    public void NoStateChange_SameAsOldOrder_FirstK()
    {
        // 바뀐 가중·보장·거르기가 없으면 v0.8 결과(RewardOrder 앞에서부터)와 같다 — 기존 판이 바뀌지 않음
        var act = Act(merchants: false);
        for (ulong seed = 1; seed <= 300; seed++)
        {
            var plan = RunPlanner.PlanAct(act, 1, seed, Rules);
            for (var i = 0; i < 4; i++)
                Assert.Equal(plan.Rooms[i].RewardOrder.Take(plan.Rooms[i].ExitGates.Count).Select(r => r.Id), Pick(act, plan, i, State()).Select(r => r.Id));
        }
    }

    [Fact]
    public void HealGuarantee_Below40_FirstDoor_MirrorPairName()
    {
        var defs = RoomActs.Defs(mirror: true);
        var act = Act(defs: defs);
        var mirrored = 0;
        for (ulong seed = 1; seed <= 400; seed++)
        {
            var plan = RunPlanner.PlanAct(act, 1, seed, Rules);
            for (var i = 0; i < 4; i++)
            {
                var picks = Pick(act, plan, i, State(hp: 0.39f));
                Assert.Equal(TestDefs.RewardPotionId, picks[0].Id);
                Assert.Equal(plan.Rooms[i].Mirrored ? "gate_ne" : "gate_nw", plan.Rooms[i].ExitGates[0]);   // 첫 문 = gate_nw, 거울판이면 짝
                if (plan.Rooms[i].Mirrored) mirrored++;
                Assert.Equal(picks.Length, picks.Select(p => p.Id).Distinct().Count());                    // 같은 ID 두 문 없음
            }
        }
        Assert.True(mirrored > 0);
    }

    [Fact]
    public void HealDoor_Skipped_At75Percent_AndLowHpWeightRaisesIt()
    {
        var act = Act(merchants: false);
        int low = 0, mid = 0;
        for (ulong seed = 1; seed <= 1000; seed++)
        {
            var plan = RunPlanner.PlanAct(act, 1, seed, Rules);
            Assert.DoesNotContain(Pick(act, plan, 1, State(hp: 0.76f)), r => r.Kind == RewardKind.HealNextRoom);
            if (Pick(act, plan, 1, State(hp: 0.45f)).Any(r => r.Kind == RewardKind.HealNextRoom)) low++;   // 50% 미만 = 가중 30(보장 40% 위)
            if (Pick(act, plan, 1, State(hp: 0.60f)).Any(r => r.Kind == RewardKind.HealNextRoom)) mid++;   // 가중 15
        }
        Assert.True(low > mid + 100, $"저체력 {low} · 보통 {mid}");
    }

    [Fact]
    public void Merchant_NotBeforeAct1Room3_NotAfterTwo_NotAfterMerchantRoom_NeverTwoInOnePhase()
    {
        var act = Act();
        var any = 0;
        for (ulong seed = 1; seed <= 1000; seed++)
        {
            var plan = RunPlanner.PlanAct(act, 1, seed, Rules);
            Assert.DoesNotContain(Pick(act, plan, 0, State(shown: true)), IsMerchant);                         // 1번 방 문(→ 2번 방)
            for (var i = 1; i < 4; i++)
            {
                var picks = Pick(act, plan, i, State(shown: true));
                Assert.True(picks.Count(IsMerchant) <= 1);                                                     // 한 문 단계에 상인 하나
                if (picks.Any(IsMerchant)) any++;
                Assert.DoesNotContain(Pick(act, plan, i, State(merchantRooms: 2, shown: true)), IsMerchant);   // 이 액트 상인 둘 뒤
                Assert.DoesNotContain(Pick(act, plan, i, State(lastMerchant: true, shown: true)), IsMerchant); // 바로 앞 방이 상인 방
            }
            // 액트 2 는 3번 방 문 조건이 없다(기획 11.7 "액트 1 은 3번 방으로 가는 문부터")
            var plan2 = RunPlanner.PlanAct(act, 2, seed, Rules);
            if (Pick(act, plan2, 0, State(shown: true)).Any(IsMerchant)) any++;
        }
        Assert.True(any > 0);
    }

    [Fact]
    public void MerchantGuarantee_Act1Room4_SecondDoor_HealFirstWhenBoth_SameDrawsElsewhere()
    {
        var act = Act();
        for (ulong seed = 1; seed <= 500; seed++)
        {
            var plan = RunPlanner.PlanAct(act, 1, seed, Rules);
            var draws = plan.Rooms[2].RewardDraws!.ToArray();
            var plain = Pick(act, plan, 2, State(shown: true));                     // 3번 방 문(→ 4번 방), 이미 상인 문이 보였음 → 보장 없음
            var guaranteed = Pick(act, plan, 2, State());                           // 안 보였음 → 둘째 문 상인
            Assert.True(IsMerchant(guaranteed[1]));
            Assert.Equal(1, guaranteed.Count(IsMerchant));
            if (!IsMerchant(plain[0])) Assert.Equal(plain[0].Id, guaranteed[0].Id); // 보장 밖 문은 같은 줄 순서(같은 난수)

            var both = Pick(act, plan, 2, State(hp: 0.30f));
            Assert.Equal(TestDefs.RewardPotionId, both[0].Id);                      // 겹치면 회복 첫 문·상인 둘째 문
            Assert.True(IsMerchant(both[1]));

            Assert.Equal(draws, plan.Rooms[2].RewardDraws!);                        // 뽑기가 계획 난수를 바꾸지 않음(난수 수 고정)
            Assert.Equal(Pick(act, plan, 3, State(shown: true)).Select(r => r.Id), Pick(act, plan, 3, State(shown: true)).Select(r => r.Id));
        }
        // 액트 2 는 5번 방 문(→ 5번 방 = 배열 자리 4)
        var plan2 = RunPlanner.PlanAct(act, 2, 9, Rules);
        Assert.True(IsMerchant(Pick(act, plan2, 3, State())[1]));
    }

    [Fact]
    public void MerchantWeights_MetOtherAndHealerBoost_ShiftDistribution()
    {
        var act = Act();
        int smithFresh = 0, smithMetWitch = 0, healerNormal = 0, healerBoost = 0;
        for (ulong seed = 1; seed <= 2000; seed++)
        {
            var plan = RunPlanner.PlanAct(act, 1, seed, Rules);
            bool Has(RewardDef[] p, string id) => p.Any(r => r.Id == id);
            if (Has(Pick(act, plan, 2, State(shown: true)), TestDefs.RewardSmithId)) smithFresh++;
            if (Has(Pick(act, plan, 2, State(shown: true, met: [TestDefs.WitchId])), TestDefs.RewardSmithId)) smithMetWitch++;
            if (Has(Pick(act, plan, 2, State(hp: 0.6f, potions: 0, shown: true)), TestDefs.RewardHealerId)) healerNormal++;
            if (Has(Pick(act, plan, 2, State(hp: 0.45f, potions: 0, shown: true)), TestDefs.RewardHealerId)) healerBoost++;
        }
        Assert.True(smithMetWitch > smithFresh, $"만난 뒤 다른 상인 × 1.5: {smithFresh} → {smithMetWitch}");
        Assert.True(healerBoost > healerNormal, $"물약 0 ∧ 체력 < 50% 페핀 × 1.5: {healerNormal} → {healerBoost}");
    }

    [Fact]
    public void PlanAct_RewardDraws_OnePerWeightPlusGuarantee_RewardOrderUnchanged()
    {
        var act = Act();
        var plan = RunPlanner.PlanAct(act, 1, 77, Rules);
        foreach (var room in plan.Rooms)
        {
            Assert.Equal(act.Rewards.Weights.Count + 1, room.RewardDraws!.Count);
            var weights = act.Rewards.Weights.Select(w => w.Weight).ToArray();
            Assert.Equal(room.RewardOrder.Select(r => r.Id), RewardDraw.Permute(act.Rewards.Weights, weights, room.RewardDraws).Select(r => r.Id));
        }
    }
}
