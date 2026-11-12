using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Movement.Invariants;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// 해골왕 보스 방(보스 묶음 A — 설계서 6.20, 기획 9.5·38.1): 실제 데이터·실제 모듈(Core 만)로 <c>--room 1:9 --god --level 25 --weapons magic_missile:8,fireball:8</c> 자동 조작.
/// 엄격 로딩(해골왕 3페이즈·슬롯 ≤ 12·순서 ID 모두 있음)은 늘 돌고, 판(시나리오)은 <c>DDR_RUN=Scenario</c> 일 때만 —
/// 방 틱 상한 안 해골왕 처치·P2·P3 를 모두 지남·예외 0·공정성 셋 0·보스 방 최대 동시 적 탄 ≤ 240·같은 씨앗 두 번 같은 판 해시, <c>--boss-hp 0.39</c> 면 P3 가 첫 틱에.
/// <c>--god</c> 없는 판은 기준 없이 결과(살았나·받은 피해·<c>boss_s</c>)만 적는다(열린 질문 56 의 첫 자료).
/// </summary>
public sealed class BossRoomScenarioTests(ITestOutputHelper output)
{
    private static readonly Lazy<DataLoadResult> Loaded = new(() =>
    {
        var ctx = GameModules.CreateContext();
        return DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
    });

    private static GameDefs Real
    {
        get
        {
            Assert.False(Loaded.Value.HasErrors, string.Join("\n", Loaded.Value.Errors));
            return Loaded.Value.Defs;
        }
    }

    [Fact]
    public void RealData_SkeletonKing_ThreePhases_StrictLoad_SlotsWithin12()
    {
        var defs = Real;
        foreach (var w in Loaded.Value.Errors) output.WriteLine(w.ToString());   // 경고(소리 없는 새 탄 등)
        var king = defs.Enemies.Get("skeleton_king");
        Assert.Equal(2, king.Phases.Count);
        Assert.NotNull(king.Sequence);
        var kit = EnemyKit.Build(king, 0, []);                                    // 순서 ID 가 목록에 없으면 여기서 예외
        Assert.True(kit.Count <= 12);
        Assert.Equal(2, kit.Count);
        foreach (var id in new[] { "pj_sk_orb", "pj_sk_orb_big", "pj_ash_arrow", "pj_rot_glob" })
            Assert.Equal(6f, defs.Projectiles.Get(id).HeroHitRadius);
        Assert.Equal(("rot_glob", 8, 40f, 5f), (defs.Projectiles.Get("pj_rot_glob").OnEnd!.CapGroup, defs.Projectiles.Get("pj_rot_glob").OnEnd!.Cap,
                                               defs.Projectiles.Get("pj_rot_glob").OnEnd!.Radius, defs.Projectiles.Get("pj_rot_glob").OnEnd!.Duration));
        Assert.Null(defs.Projectiles.Get("pj_fire_arrow").HeroHitRadius);          // 일반 불타는 해골 궁수는 그대로(열린 질문 57)
        Assert.Equal(280, defs.Rules.Caps!.EnemyProjectiles);
        Assert.Equal((48f, 0.4f, 24), (defs.Rules.Enemies!.Barrage.SpawnClear, defs.Rules.Enemies.Barrage.TelegraphMin, defs.Rules.Enemies.Barrage.ClearFxMax));
        var boss = defs.Stages.Get("stage_cathedral").SpawnIntervals.Single(i => i.During is not null);
        Assert.Equal((40, 1.2f), (boss.MaxAlive, boss.PerSecond));
    }

    private sealed record Outcome(RunOutcome Result, RunState Run, InvariantReport Invariants, FairnessCounters Fairness, int Ticks, int MaxPhase,
                                  int PeakShots, int BossHits, float Damage, int HeroHits, IReadOnlyList<ulong> Hashes, int FirstActivePhase, float HpLeft,
                                  int Drunk, int BossBodyHits);

    private static RunOptions Options(bool god, float? bossHp = null) => new()
    {
        God = god, Check = true, Level = 25, BossHp = bossHp,
        Weapons = [new LoadoutEntry("magic_missile", 8), new LoadoutEntry("fireball", 8)],
    };

    private static Outcome Run(ulong seed, RunOptions options)
    {
        var defs = Real;
        var act = defs.Stages.Get("stage_cathedral");
        var session = new RunSession(defs, GameModules.CreateContext(), new RunStart(seed, act, null, options, StartRoomIndex: 8));
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var invariants = new InvariantReport();
        var fairness = new FairnessCounters();
        var hashes = new List<ulong>();
        var limit = MathX.SecondsToTicks(8 * 60f, World.Dt);   // 보스 방 6:00 + 여유
        int ticks = 0, maxPhase = 0, peak = 0, bossHits = 0, heroHits = 0, firstActive = -1, bodyHits = 0;
        var damage = 0f;
        while (true)
        {
            var w = session.World;
            Assert.Equal(9, w.Room!.Plan.Number);
            if (w.AwaitingChoice != ChoiceKind.None)
            {
                Choose(w, policy);
                continue;
            }
            Assert.True(ticks < limit, $"보스 방 {ticks} 틱 안에 끝나지 않았다(단계 {w.Room.Phase})");
            w.Command = policy.Next(w);
            if (StageStepper.AppTick(w, w.Command) == StageStep.Stepped) ticks++;
            var shots = 0;
            foreach (var p in w.Projectiles.Items) if (!p.Removed && p.Faction == Faction.Enemy) shots++;
            peak = Math.Max(peak, shots);
            Enemy? king = null;
            foreach (var e in w.Enemies.Items) if (e.IsAlive && e.Def.Id == "skeleton_king") king = e;
            if (king is not null)
            {
                maxPhase = Math.Max(maxPhase, king.PhaseIdx);
                if (firstActive < 0 && king.Life == LifeState.Active) firstActive = king.PhaseIdx;
            }
            foreach (ref readonly var d in w.Events.Previous.HeroDamaged.AsSpan())
            {
                heroHits++;
                damage += d.Applied;
                if (king is not null && d.Attacker == king.Id)
                {
                    bossHits++;
                    // 몸(내려치기·돌진·끝 내려찍기 반경 70)인지 탄인지 어림: 맞은 틱에 보스가 두 반경 + 70 안이면 몸
                    if (Vector2.Distance(king.Pos, w.Hero.Pos) <= king.Radius + w.Hero.Radius + 70f) bodyHits++;
                }
            }
            if (w.Tick % 60 == 0) hashes.Add(ActRunner.Hash(w));
            if (w.Outcome is RunOutcome.Victory or RunOutcome.Defeat)
            {
                w.Invariants.AddTo(invariants);
                w.Fairness.AddTo(fairness);
                var hp = w.Hero.MaxHp > 0f ? w.Hero.Hp / w.Hero.MaxHp : 0f;
                var run = session.Conclude();
                return new Outcome(w.Outcome, run, invariants, fairness, ticks, maxPhase, peak, bossHits, damage, heroHits, hashes, firstActive, hp,
                                   run.Rooms.Count > 0 ? run.Rooms[^1].Drunk : 0, bodyHits);
            }
            if (w.Room is { Phase: RoomPhase.Exit }) session.Advance();
        }
    }

    private static void Choose(World w, AutoplayPolicy policy)
    {
        var p = w.Progression!;
        switch (w.AwaitingChoice)
        {
            case ChoiceKind.Chest: p.OpenChest(); break;
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

    private void Log(string label, Outcome r)
    {
        var room = r.Run.Rooms.Count > 0 ? r.Run.Rooms[^1] : default;
        output.WriteLine($"{label} outcome={r.Result} ticks={r.Ticks} ({r.Ticks * World.Dt:0.0}s) boss_s={(room.BossClockTicks >= 0 ? room.BossClockTicks * World.Dt : -1):0.0} " +
                         $"max_phase={r.MaxPhase} peak_enemy_shots={r.PeakShots} hero_hits={r.HeroHits} boss_hits={r.BossHits}(몸 어림 {r.BossBodyHits}) damage={r.Damage:0} hp_left={r.HpLeft:0.00} drunk={r.Drunk} " +
                         $"unseen_shots={r.Fairness.UnseenShots} unseen_charges={r.Fairness.UnseenCharges} hidden_hits={r.Fairness.HiddenHits}");
    }

    [Theory, Trait("Category", TestCategories.Scenario)]
    [InlineData(1ul)]
    [InlineData(2ul)]
    public void BossRoom_God_KillsKing_AllPhases_Clean(ulong seed)
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var a = Run(seed, Options(god: true));
        Log($"god seed={seed}", a);
        Assert.Equal(RunOutcome.Victory, a.Result);
        Assert.Equal(2, a.MaxPhase);                                              // P2·P3 를 모두 지남
        Assert.True(a.Invariants.Passed, $"불변식: nan={a.Invariants.Nan} out={a.Invariants.OutOfMap} over_cap={a.Invariants.OverCap}");
        Assert.Equal(0, a.Fairness.UnseenShots);
        Assert.Equal(0, a.Fairness.UnseenCharges);
        Assert.Equal(0, a.Fairness.HiddenHits);
        Assert.True(a.PeakShots <= 240, $"보스 방 최대 동시 적 탄 {a.PeakShots} > 240");
        var b = Run(seed, Options(god: true));
        Assert.Equal(a.Ticks, b.Ticks);
        Assert.Equal(a.Hashes, b.Hashes);                                         // 같은 씨앗 두 번 = 같은 판
    }

    [Fact, Trait("Category", TestCategories.Scenario)]
    public void BossRoom_BossHp039_StartsInP3()
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var r = Run(1, Options(god: true, bossHp: 0.39f));
        Log("boss_hp=0.39", r);
        Assert.Equal(2, r.FirstActivePhase);                                      // 들어와 첫 틱에 P3(전환 연출 포함)
        Assert.Equal(RunOutcome.Victory, r.Result);
    }

    [Theory, Trait("Category", TestCategories.Scenario)]
    [InlineData(1ul)]
    [InlineData(2ul)]
    public void BossRoom_NoGod_Record(ulong seed)
    {
        TestCategories.RequireEnabled(TestCategories.Scenario);
        var r = Run(seed, Options(god: false));
        Log($"nogod seed={seed}", r);                                              // 기준 없음 — 열린 질문 56 의 첫 자료
        Assert.True(r.Invariants.Passed);
    }
}
