using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using DiabloDefenceRemastered.Tests.Data;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;
using SimWorld = DiabloDefenceRemastered.Core.Sim.World;

namespace DiabloDefenceRemastered.Tests.Sim;

/// <summary>
/// WP4 <c>Sim</c> 시험 (설계서 10.1 <c>Sim</c>·11.1 WP4 완료 기준): 결정성 30초(같은 씨앗 두 번의 해시 열 같음, 다른 씨앗은 다름),
/// 표시 이벤트 켬/끔과 상관없이 해시 같음, <c>Sim/Autoplay/</c> 소스가 <c>Visibility</c> 를 참조하지 않음(봇은 안개 무시),
/// 공정성 합성 맵 3분 <c>Scenario</c>(안쪽 <c>L</c>·<c>X</c> 벽 + 궁수·돌진·자폭·근접 섞은 구간표, 자동 조작 + god).
/// 이웃(WP2·WP3)은 <see cref="FakeArena"/> 가짜라 <c>unseen_shots</c>·<c>hidden_hits</c>(WP3 이 셈)는 여기서 볼 수 없다 — 합친 뒤.
/// </summary>
public sealed class Wp4SimTests(ITestOutputHelper output)
{
    private static readonly ProjectileDef Arrow = new() { Id = "pj_sim", Sprite = DefRef<SpriteDef>.To(TestDefs.Sprite("arrow")), Speed = 220f };
    private static readonly TelegraphDef Tel = new() { Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_sim" }), Time = 0.5f };

    private static readonly EnemyDef Melee = Enemy("sim_melee", B("chase_melee", new ChaseMeleeParams { Cooldown = 1f }));
    private static readonly EnemyDef Archer = Enemy("sim_archer",
        B("kite", new KiteParams { RangeBand = [220f, 320f] }),
        B("shoot_projectile", new ShootProjectileParams { Projectile = DefRef<ProjectileDef>.To(Arrow), Cooldown = 2.5f, CooldownJitter = 0.5f, Range = 320f })) with { Speed = 62f };
    private static readonly EnemyDef Charger = Enemy("sim_charger",
        B("charge", new ChargeParams { RangeBand = [0f, 260f], Telegraph = Tel, Damage = 10f })) with { Speed = 45f };
    private static readonly EnemyDef Bomber = Enemy("sim_bomber", B("self_destruct", new SelfDestructParams { Fuse = 0.6f, Radius = 60f, Damage = 15f })) with { Speed = 100f };

    /// <summary>시야만 막는 L 줄 둘·시야 막는 안쪽 벽 줄 둘(합성 맵 12 메가타일).</summary>
    private static readonly IReadOnlyList<PiecePlacementDef> Walls =
    [
        new PiecePlacementDef { Piece = "sight_only", At = [new(4, 3), new(5, 3), new(7, 8)] },
        new PiecePlacementDef { Piece = "inner_wall", At = [new(3, 7), new(8, 4)] },
    ];

    private static GameDefs MixedDefs(bool sight)
    {
        var all = new[] { Melee, Archer, Charger, Bomber };
        return Defs(all, map => TestDefs.Stage(map) with
        {
            SpawnIntervals =
            [
                new SpawnIntervalDef { From = new ClockTime(0f), To = new ClockTime(90f), MaxAlive = 40, PerSecond = 1.5f,
                                       Weights = [new(DefRef<EnemyDef>.To(Melee), 50f), new(DefRef<EnemyDef>.To(Archer), 20f), new(DefRef<EnemyDef>.To(Charger), 15f), new(DefRef<EnemyDef>.To(Bomber), 15f)] },
                new SpawnIntervalDef { From = new ClockTime(90f), To = new ClockTime(600f), MaxAlive = 60, PerSecond = 2.5f,
                                       Weights = [new(DefRef<EnemyDef>.To(Melee), 40f), new(DefRef<EnemyDef>.To(Archer), 25f), new(DefRef<EnemyDef>.To(Charger), 20f), new(DefRef<EnemyDef>.To(Bomber), 15f)] },
            ],
        }, sight: sight ? TestDefs.Sight : null, heroLight: sight ? 420f : 0f, place: Walls);
    }

    private sealed record RunResult(List<ulong> Hashes, SimWorld World, float KiteSeconds);

    private static RunResult Run(GameDefs defs, ulong seed, int ticks, bool presentation = false, int hashEvery = 30)
    {
        var t = World(defs, seed, weapons: true, presentation: presentation);
        t.World.Options = new RunOptions { God = true };
        var policy = new AutoplayPolicy(AutoplayVisit.None);
        var hashes = new List<ulong>();
        var kiteTicks = 0;
        for (var i = 0; i < ticks; i++)
        {
            t.World.Command = policy.Next(t.World);
            t.Step();
            foreach (var e in t.World.Enemies.Items)
                if (e.IsAlive && e.Kit.Positioning.Any(s => e.Kit.Part(0, s) is Kite)) kiteTicks++;
            if (t.World.Tick % hashEvery == 0) hashes.Add(ActRunner.Hash(t.World));
        }
        return new RunResult(hashes, t.World, kiteTicks * SimWorld.Dt);
    }

    [Fact]
    public void Same_seed_thirty_seconds_same_hashes()
    {
        var defs = MixedDefs(sight: true);
        var ticks = MathX.SecondsToTicks(30f, SimWorld.Dt);
        var a = Run(defs, 5, ticks);
        var b = Run(defs, 5, ticks);
        var c = Run(defs, 6, ticks);
        Assert.Equal(a.Hashes, b.Hashes);
        Assert.NotEqual(a.Hashes, c.Hashes);
        Assert.True(a.World.DirectorState.IntervalSpawned > 20);
    }

    [Fact]
    public void Presentation_on_or_off_does_not_change_hashes()
    {
        var defs = MixedDefs(sight: true);
        var ticks = MathX.SecondsToTicks(20f, SimWorld.Dt);
        Assert.Equal(Run(defs, 9, ticks, presentation: false).Hashes, Run(defs, 9, ticks, presentation: true).Hashes);
    }

    [Fact]
    public void Autoplay_source_does_not_read_visibility()
    {
        // 9절: 봇은 안개를 무시한다 — Sim/Autoplay/ 소스가 Visibility 를 참조하지 않는다(소스 문자열 검사)
        var dir = Path.Combine(RealData.RepoRoot(), "src", "DiabloDefenceRemastered.Core", "Sim", "Autoplay");
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        foreach (var f in files)
            Assert.DoesNotContain("Visibility", File.ReadAllText(f));
    }

    [Fact]
    [Trait("Category", TestCategories.Scenario)]
    public void Fairness_synthetic_map_three_minutes()
    {
        var r = Run(MixedDefs(sight: true), 21, MathX.SecondsToTicks(180f, SimWorld.Dt), hashEvery: 600);
        var w = r.World;
        var nonFinite = 0;
        foreach (var e in w.Enemies.Items)
            if (e.IsAlive && !(float.IsFinite(e.Pos.X) && float.IsFinite(e.Pos.Y))) nonFinite++;
        output.WriteLine($"spawned={w.DirectorState.IntervalSpawned} unseen_charges={w.Fairness.UnseenCharges} kite_switches={w.Fairness.KiteSwitches} kite_s={r.KiteSeconds:0} projectiles={((RecordingSpawner)w.Spawner).Projectiles.Count}");
        Assert.Equal(RunOutcome.Running, w.Outcome);
        Assert.Equal(0, nonFinite);
        Assert.Equal(0, w.Fairness.UnseenCharges);
        Assert.True(w.Fairness.KiteSwitches <= r.KiteSeconds + 1f, $"kite_switches {w.Fairness.KiteSwitches} > kite 적 초 {r.KiteSeconds:0}");
        Assert.True(w.DirectorState.IntervalSpawned > 200);
    }
}
