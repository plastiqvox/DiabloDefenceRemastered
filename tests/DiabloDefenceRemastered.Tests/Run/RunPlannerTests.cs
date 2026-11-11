namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>10.1 <c>Run</c> 행 — <c>RunSeeds</c>·<c>RunPlanner</c>(합성 액트 <see cref="RoomActs"/>).</summary>
public class RunPlannerTests
{
    private static readonly GameDefs Defs = RoomActs.Defs();
    private static StageDef Act1 => Defs.Stages.Get(RoomActs.Act1Id);
    private static StageDef Act2 => Defs.Stages.Get(RoomActs.Act2Id);
    private static RoomRulesDef Rules => Defs.Rules.Rooms!;

    private static ActPlan Plan(ulong seed, StageDef? act = null, int actNumber = 1, RoomRulesDef? rules = null) =>
        RunPlanner.PlanAct(act ?? Act1, actNumber, seed, rules ?? Rules);

    private static string Key(RoomPlan p) =>
        $"{p.Number}:{p.Map.Id}:{p.GateIn}:{string.Join(",", p.ExitGates)}:{string.Join(",", p.RewardOrder.Select(r => r.Id))}:{p.RoomSeed}";

    [Fact]
    public void RunSeeds_TwoStreamsDiffer_AndAreStable()
    {
        Assert.NotEqual(RunSeeds.Flow(7, 1, 1), RunSeeds.Room(7, 1, 1));
        Assert.NotEqual(RunSeeds.Flow(7, 1, 1), RunSeeds.Flow(7, 1, 2));
        Assert.NotEqual(RunSeeds.Flow(7, 1, 1), RunSeeds.Flow(7, 2, 1));
        Assert.NotEqual(RunSeeds.Flow(7, 1, 1), RunSeeds.Flow(8, 1, 1));
        Assert.Equal(RunSeeds.Flow(7, 1, 1), RunSeeds.Flow(7, 1, 1));
        // 섞기 고정 값 — 씨앗 이름·순서를 바꾸면 모든 판이 바뀐다(계약, 11.3). 바꾸면 이 값을 함께 고치고 메인 세션에 알린다
        Assert.Equal(8263982438568649945ul, RunSeeds.Flow(7, 1, 1));
        Assert.Equal(1188890678179892026ul, RunSeeds.Room(7, 1, 1));
        Assert.Equal(7075775467138543616ul, RunSeeds.Flow(1234, 2, 9));
    }

    [Fact]
    public void SameInput_SamePlan_SeedChangesIt()
    {
        var a = Plan(42).Rooms.Select(Key).ToList();
        var b = Plan(42).Rooms.Select(Key).ToList();
        Assert.Equal(a, b);
        var differs = Enumerable.Range(43, 20).Count(s => !Plan((ulong)s).Rooms.Select(Key).SequenceEqual(a));
        Assert.True(differs >= 19);
    }

    [Fact]
    public void RoomSeed_IsRunSeedsRoom_ByVisibleNumber()
    {
        var plan = Plan(5);
        foreach (var r in plan.Rooms)
        {
            Assert.Equal(RunSeeds.Room(5, 1, r.Number), r.RoomSeed);
            Assert.Equal(r.Index + 1, r.Number);
            Assert.Equal(1, r.Act);
        }
    }

    [Fact]
    public void GateExitsRewards_DependOnlyOnFlowOfThatRoom()
    {
        // 맵은 앞 방 계획에 이어지지만(①), 들어온 문·출구 수·보상 순서(②~④)는 Flow(씨앗, 액트, 번호) 만의 함수다(v0.7.1)
        // → 같은 방 번호를 쓰는 다른 액트 정의(앞 방이 다름)에서도 같은 씨앗이면 ②~④ 가 같다
        var full = Plan(9);
        var shorter = Act1 with { Rooms = [.. Act1.Rooms.Skip(1).Take(2)] };   // 번호 2·3 만(앞 방 없음)
        var part = RunPlanner.PlanAct(shorter, 1, 9, Rules);
        for (var i = 0; i < part.Rooms.Count; i++)
        {
            var a = full.Rooms[i + 1];
            var b = part.Rooms[i];
            Assert.Equal(a.Number, b.Number);
            Assert.Equal(a.GateIn, b.GateIn);
            Assert.Equal(a.RewardOrder.Select(r => r.Id), b.RewardOrder.Select(r => r.Id));
            Assert.Equal(a.RoomSeed, b.RoomSeed);
        }
        // 출구 수는 액트의 마지막 방(계단)인지에 따라 다르므로(v0.8 — 다음 방 종류와는 무관) 두 계획 모두 마지막이 아닌 번호 2 에서만 견준다
        Assert.Equal(full.Rooms[1].ExitGates, part.Rooms[0].ExitGates);
    }

    [Fact]
    public void PublicFunctions_TakeNoWorld()
    {
        foreach (var m in typeof(RunPlanner).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            Assert.DoesNotContain(m.GetParameters(), p => p.ParameterType == typeof(World));
    }

    [Fact]
    public void TwoMapPool_NeighboursNeverSame_OneMapPool_AlwaysIt()
    {
        for (ulong s = 0; s < 1000; s++)
        {
            var rooms = Plan(s).Rooms;
            for (var i = 1; i < rooms.Count; i++)
                if (rooms[i].Kind != RoomKind.Boss && rooms[i - 1].Kind != RoomKind.Boss)
                    Assert.NotEqual(rooms[i - 1].Map.Id, rooms[i].Map.Id);
        }
        var one = RoomActs.Defs(twoNormalMaps: false);
        var plan = RunPlanner.PlanAct(one.Stages.Get(RoomActs.Act1Id), 1, 3, one.Rules.Rooms!);
        Assert.All(plan.Rooms.Where(r => r.Kind != RoomKind.Boss), r => Assert.Equal(RoomActs.RoomA, r.Map.Id));
    }

    [Fact]
    public void BossRoom_UsesBossMapAndEntry_OnlyStairsHasOneExit()
    {
        for (ulong s = 0; s < 50; s++)
        {
            var rooms = Plan(s).Rooms;
            var boss = rooms[5];
            Assert.Equal(RoomKind.Boss, boss.Kind);
            Assert.Equal(RoomActs.BossRoom, boss.Map.Id);
            Assert.Equal("gate_sw", boss.GateIn);
            Assert.Equal(["gate_ne"], boss.ExitGates);              // 액트 보스 방 = 계단 하나
            Assert.Contains(rooms[2].ExitGates.Count, new[] { 2, 3 }); // 다음이 정예 방이어도 둘·셋(v0.8 — v0.7 은 하나)
            Assert.Contains(rooms[4].ExitGates.Count, new[] { 2, 3 }); // 다음이 보스 방이어도
            Assert.Equal(["gate_nw", "gate_ne"], rooms[4].ExitGates.Take(2));
            Assert.Contains(rooms[0].ExitGates.Count, new[] { 2, 3 });
            Assert.Contains(rooms[0].GateIn, new[] { "gate_sw", "gate_se" });

            var last = RunPlanner.PlanAct(Act2, 2, s, Rules).Rooms[^1];
            Assert.Empty(last.ExitGates);                            // 판의 마지막 방
        }
    }

    [Fact]
    public void ThreeExits_Ratio_AndOrder()
    {
        var three = 0;
        var total = 0;
        for (ulong s = 0; s < 10000; s++)
        {
            var r = RunPlanner.PlanAct(Act1, 1, s, Rules).Rooms[0];
            total++;
            if (r.ExitGates.Count == 3)
            {
                three++;
                Assert.Equal("gate_nw", r.ExitGates[0]);
                Assert.Equal("gate_ne", r.ExitGates[1]);
                Assert.NotEqual(r.GateIn, r.ExitGates[2]);          // 남은 앞 문
                Assert.Contains(r.ExitGates[2], new[] { "gate_sw", "gate_se" });
            }
            else Assert.Equal(["gate_nw", "gate_ne"], r.ExitGates);
        }
        Assert.InRange(three / (double)total, 0.37, 0.43);
    }

    [Fact]
    public void RewardOrder_IsWeightedPermutation()
    {
        var first = new Dictionary<string, int>(StringComparer.Ordinal);
        const int n = 20000;
        for (ulong s = 0; s < n; s++)
        {
            var order = RunPlanner.PlanAct(Act1, 1, s, Rules).Rooms[0].RewardOrder;
            Assert.Equal(3, order.Count);
            Assert.Equal(3, order.Select(r => r.Id).Distinct().Count());    // 모든 종류 한 번씩
            first[order[0].Id] = first.GetValueOrDefault(order[0].Id) + 1;
        }
        Assert.InRange(first[TestDefs.RewardTomeId] / (double)n, 30 / 75.0 - 0.02, 30 / 75.0 + 0.02);
        Assert.InRange(first[TestDefs.RewardChestId] / (double)n, 30 / 75.0 - 0.02, 30 / 75.0 + 0.02);
        Assert.InRange(first[TestDefs.RewardPotionId] / (double)n, 15 / 75.0 - 0.02, 15 / 75.0 + 0.02);
    }

    [Fact]
    public void DrawOrder_IsFixed_MapGateExitsRewards()
    {
        // 뽑기 순서(맵 → 문 → 출구 → 보상)와 섞기의 고정 값. 순서를 바꾸면 이 값이 깨진다(계약, 11.3)
        var r = Plan(1234).Rooms[1];
        var rng = new RandomStream(RunSeeds.Flow(1234, 1, r.Number));
        var prev = Plan(1234).Rooms[0].Map.Id;
        var candidates = new[] { RoomActs.RoomA, RoomActs.RoomB }.Where(m => m != prev).ToArray();
        var u1 = rng.NextUInt();
        Assert.Equal(candidates[candidates.Length <= 1 ? 0 : (int)((ulong)u1 * (uint)candidates.Length >> 32)], r.Map.Id);
        var u2 = rng.NextUInt();
        Assert.Equal(Rules.Gates.Entry[(int)((ulong)u2 * 2u >> 32)], r.GateIn);
        var three = rng.NextFloat() < Rules.Exits.ThreeChance;
        Assert.Equal(three ? 3 : 2, r.ExitGates.Count);
    }

    [Fact]
    public void FixedPlan_Golden()
    {
        // 고정 값 — RunSeeds 섞기·뽑기 순서(맵 → 문 → 출구 → 보상)를 바꾸면 깨진다(계약, 11.3). v0.9(wp4): 맵은 덱 순서 — 문·출구·보상은 v0.8 값 그대로
        var keys = Plan(2024).Rooms.Select(r => $"{r.Number}:{r.Map.Id}:{r.GateIn}:{string.Join(",", r.ExitGates)}:{string.Join(",", r.RewardOrder.Select(x => x.Id))}");
        Assert.Equal(
        [
            "1:test_room_b:gate_sw:gate_nw,gate_ne,gate_se:test_reward_tome,test_reward_chest,test_reward_potion",
            "2:test_room_a:gate_sw:gate_nw,gate_ne,gate_se:test_reward_potion,test_reward_tome,test_reward_chest",
            "3:test_room_b:gate_sw:gate_nw,gate_ne:test_reward_tome,test_reward_chest,test_reward_potion",       // v0.8: 다음이 정예여도 출구 둘
            "4:test_room_a:gate_se:gate_nw,gate_ne:test_reward_chest,test_reward_tome,test_reward_potion",
            "5:test_room_b:gate_sw:gate_nw,gate_ne,gate_se:test_reward_potion,test_reward_tome,test_reward_chest",   // v0.8: 다음이 보스여도 둘·셋
            "6:test_boss_room:gate_sw:gate_ne:test_reward_potion,test_reward_chest,test_reward_tome",
        ], keys);
    }

    [Fact]
    public void FixedPlan_Golden_Mirror()
    {
        // 고정 값(v0.8 → v0.9 덱 순서, wp4) — 덱 섞기·방향 뽑기·짝 이름 바꾸기를 바꾸면 깨진다(계약, 11.3)
        var defs = RoomActs.Defs(mirror: true);
        var keys = RunPlanner.PlanAct(defs.Stages.Get(RoomActs.Act1Id), 1, 2024, defs.Rules.Rooms!).Rooms
            .Select(r => $"{r.Number}:{r.Map.Id}:{r.GateIn}:{string.Join(",", r.ExitGates)}");
        Assert.Equal(
        [
            "1:test_room_b:gate_sw:gate_nw,gate_ne,gate_se",
            "2:test_room_a:gate_sw:gate_nw,gate_ne,gate_se",
            "3:test_room_b@m:gate_se:gate_ne,gate_nw",              // 거울판: 들어온 문·출구가 짝 이름(같은 씨앗 거울판 없는 계획의 방 3 = gate_sw · nw,ne)
            "4:test_room_a@m:gate_sw:gate_ne,gate_nw",
            "5:test_room_b:gate_sw:gate_nw,gate_ne,gate_se",
            "6:test_boss_room:gate_sw:gate_ne",
        ], keys);
    }

    [Fact]
    public void ActNumber_ActAt_FollowNextActChain()
    {
        Assert.Equal(1, RunPlanner.ActNumber(Defs, Act1));
        Assert.Equal(2, RunPlanner.ActNumber(Defs, Act2));
        Assert.Same(Act1, RunPlanner.ActAt(Defs, 1));
        Assert.Same(Act2, RunPlanner.ActAt(Defs, 2));
        Assert.Null(RunPlanner.ActAt(Defs, 3));
        Assert.Null(RunPlanner.ActAt(Defs, 0));
        var side = new StageDef { Id = "hf_side", Requires = [ContentBundle.Hellfire] };
        Assert.Equal(1, RunPlanner.ActNumber(Defs, side));         // 사슬 밖 = 1
    }

    [Fact]
    public void BadNumbers_Throw()
    {
        var bad = Act1 with { Rooms = [TestDefs.Room(RoomKind.Normal, 0, 60, 2), TestDefs.Room(RoomKind.Normal, 60, 120, 2)] };
        Assert.Throws<ArgumentException>(() => RunPlanner.PlanAct(bad, 1, 1, Rules));
        var unset = Act1 with { Rooms = [TestDefs.Room(RoomKind.Normal, 0, 60, 0)] };
        Assert.Throws<ArgumentException>(() => RunPlanner.PlanAct(unset, 1, 1, Rules));
    }

    [Fact]
    public void EliteWithoutMiniboss_BossWithoutBoss_Throw()
    {
        var elite = Act1 with { Rooms = [TestDefs.Room(RoomKind.Normal, 0, 60, 1), TestDefs.Room(RoomKind.Elite, 60, 120, 2)] };
        Assert.Throws<ArgumentException>(() => RunPlanner.PlanAct(elite, 1, 1, Rules));
        var boss = Act1 with { Rooms = [TestDefs.Room(RoomKind.Normal, 0, 60, 1), TestDefs.Room(RoomKind.Boss, 60, null, 2)] };
        Assert.Throws<ArgumentException>(() => RunPlanner.PlanAct(boss, 1, 1, Rules));
    }

    [Fact]
    public void SliceNumbers_SeedByVisibleNumber()
    {
        // 단계 1 slice 판: 번호 1·2·3·4·6·8 — 번호 6 방의 씨앗은 전체 판의 6번 방과 같다(v0.7.1)
        var slice = Act1 with
        {
            Slice = true,
            NextAct = null,
            Rooms = [.. new[] { 1, 2, 3, 4, 6, 8 }.Select((n, i) => TestDefs.Room(RoomKind.Normal, i * 60f, i * 60f + 60f, n))],
        };
        var plan = RunPlanner.PlanAct(slice, 1, 77, Rules);
        Assert.Equal([1, 2, 3, 4, 6, 8], plan.Rooms.Select(r => r.Number));
        Assert.Equal(RunSeeds.Room(77, 1, 6), plan.Rooms[4].RoomSeed);
        Assert.Empty(plan.Rooms[^1].ExitGates);                     // 8번 정리 = 판의 끝
    }

    // ── v0.8(WP1f #7) 거울판·출구 ───────────────────────────────────────

    [Fact]
    public void Mirror_OneMapPool_AlternatesOriginalAndMirror()
    {
        // 풀 1장 + 거울판: (맵, 방향) 후보 둘 → 바로 앞 쌍을 빼므로 원판·거울판이 번갈아(보스 방은 보스 맵 원판·거울판)
        var one = RoomActs.Defs(twoNormalMaps: false, mirror: true);
        for (ulong s = 0; s < 200; s++)
        {
            var rooms = RunPlanner.PlanAct(one.Stages.Get(RoomActs.Act1Id), 1, s, one.Rules.Rooms!).Rooms;
            for (var i = 1; i < 5; i++)
            {
                Assert.Equal(RoomActs.RoomA, rooms[i].MapId);
                Assert.NotEqual(rooms[i - 1].Mirrored, rooms[i].Mirrored);
            }
            Assert.Equal(RoomActs.BossRoom, rooms[5].MapId);
        }
    }

    [Fact]
    public void Mirror_TwoMapPool_NeverSamePairTwiceInARow_AllPairsAppear()
    {
        var defs = RoomActs.Defs(mirror: true);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (ulong s = 0; s < 1000; s++)
        {
            var rooms = RunPlanner.PlanAct(defs.Stages.Get(RoomActs.Act1Id), 1, s, defs.Rules.Rooms!).Rooms;
            for (var i = 1; i < rooms.Count; i++) Assert.NotEqual(rooms[i - 1].Map.Id, rooms[i].Map.Id);   // 같은 맵·같은 방향 0
            foreach (var r in rooms) seen.Add(r.Map.Id);
        }
        Assert.Equal(["test_boss_room", "test_boss_room@m", "test_room_a", "test_room_a@m", "test_room_b", "test_room_b@m"], seen.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Mirror_GatesArePairNames_DrawsSameAsOriginalPlan()
    {
        // ②~④ 는 Flow(n) 만의 함수 → 같은 씨앗의 거울판 없는 계획과 문은 짝 이름만 다르고 보상 순서·방 씨앗은 같다
        var mirror = RoomActs.Defs(mirror: true);
        var plain = RoomActs.Defs();
        var mirrored = 0;
        for (ulong s = 0; s < 100; s++)
        {
            var a = RunPlanner.PlanAct(mirror.Stages.Get(RoomActs.Act1Id), 1, s, mirror.Rules.Rooms!).Rooms;
            var b = RunPlanner.PlanAct(plain.Stages.Get(RoomActs.Act1Id), 1, s, plain.Rules.Rooms!).Rooms;
            for (var i = 0; i < a.Count; i++)
            {
                Assert.Equal(b[i].RewardOrder.Select(r => r.Id), a[i].RewardOrder.Select(r => r.Id));
                Assert.Equal(b[i].RoomSeed, a[i].RoomSeed);
                Assert.Equal(a[i].Mirrored, a[i].Map.IsMirror);
                Func<string, string> name = a[i].Mirrored ? GateZones.MirrorGate : g => g;
                Assert.Equal(name(b[i].GateIn), a[i].GateIn);
                Assert.Equal(b[i].ExitGates.Select(name), a[i].ExitGates);
                if (!a[i].Mirrored) continue;
                mirrored++;
                Assert.Contains(a[i].GateIn, new[] { "gate_sw", "gate_se" });   // 들어오는 문 {sw, se} 는 짝 바꾸기에 닫혀 있다
                if (a[i].Kind == RoomKind.Boss) Assert.Equal("gate_se", a[i].GateIn);
                if (a[i].ExitGates.Count >= 2) Assert.Equal(["gate_ne", "gate_nw"], a[i].ExitGates.Take(2));
            }
        }
        Assert.True(mirrored > 100);
    }

    [Fact]
    public void Exits_EliteAndBossFrontRoomsTwoOrThree_SingleIsOnlyStairs()
    {
        var two = 0;
        var three = 0;
        for (ulong s = 0; s < 2000; s++)
        {
            var rooms = Plan(s).Rooms;
            foreach (var i in new[] { 2, 4 })                          // 다음이 정예 4·보스 6
            {
                if (rooms[i].ExitGates.Count == 3) three++;
                else { Assert.Equal(2, rooms[i].ExitGates.Count); two++; }
            }
            foreach (var r in rooms)
                if (r.ExitGates.Count == 1) Assert.Equal(5, r.Index);   // 계단(액트의 마지막 방)
            Assert.Empty(RunPlanner.PlanAct(Act2, 2, s, Rules).Rooms[^1].ExitGates);
        }
        Assert.InRange(three / (double)(two + three), 0.37, 0.43);
    }

    [Fact]
    public void MirrorTrueWithoutMirroredDef_Throws()
    {
        var defs = RoomActs.Defs();
        var act = defs.Stages.Get(RoomActs.Act1Id);
        var map = defs.Maps.Get(RoomActs.RoomA) with { Mirror = true };   // 읽기를 거치지 않은 정의
        var bad = act with { RoomMaps = act.RoomMaps with { Normal = [DefRef<MapDef>.To(map)] } };
        Assert.Throws<ArgumentException>(() => RunPlanner.PlanAct(bad, 1, 1, Rules));
    }
}
