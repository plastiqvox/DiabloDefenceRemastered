using DiabloDefenceRemastered.Core.Sim.Director;
using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Tests.Sim;
using SimWorld = DiabloDefenceRemastered.Core.Sim.World;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Director;

/// <summary>
/// 감독 시험 (설계서 10.1 <c>Director</c>·11.1 WP4 완료 기준): 생성 수(상한 무한·처치 없음 월드에서 구간마다 floor(초당 × 길이) ± 3),
/// 동시 최대·상한 300 과 출처별 막는 순서·지우는 순서(<c>Culled</c>), 마름모 모서리 주인공의 입구 대체 비율 로그(R23),
/// 포위 고리 반경 하한 300(실제 <c>cathedral_arena</c> 진입점의 주인공 — 생략 비율 로그), 방 모드(유예·정리 중 생성 없음, 방 문구).
/// 이웃은 <see cref="FakeArena"/> 가짜다.
/// </summary>
public sealed class DirectorTests(ITestOutputHelper output)
{
    private static readonly EnemyDef Grunt = Enemy("dir_grunt");

    private static SpawnIntervalDef Interval(float from, float to, float perSecond, int maxAlive) => new()
    {
        From = new ClockTime(from), To = new ClockTime(to), PerSecond = perSecond, MaxAlive = maxAlive,
        Weights = [new WeightEntry<EnemyDef>(DefRef<EnemyDef>.To(Grunt), 1f)],
    };

    private static GameDefs IntervalDefs(IReadOnlyList<SpawnIntervalDef> intervals, CapsDef? caps = null, int megaTiles = TestDefs.DefaultMegaTiles) =>
        Defs([Grunt], map => TestDefs.Stage(map) with { SpawnIntervals = intervals }, megaTiles: megaTiles, caps: caps);

    private static readonly CapsDef Unlimited = new() { Enemies = 100_000, EnemiesAbsolute = 100_000 };

    [Fact]
    public void Interval_spawn_count_matches_rate_in_uncapped_world()
    {
        var t = World(IntervalDefs([Interval(0f, 60f, 2f, 100_000), Interval(60f, 120f, 3.5f, 100_000)], Unlimited));
        t.Step(MathX.SecondsToTicks(60f, SimWorld.Dt));
        var first = t.World.DirectorState.IntervalSpawned;
        t.Step(MathX.SecondsToTicks(60f, SimWorld.Dt));
        var second = t.World.DirectorState.IntervalSpawned - first;
        output.WriteLine($"0:00–1:00 {first} (기대 120), 1:00–2:00 {second} (기대 210), 입구 대체 {t.World.DirectorState.EntryFallbacks}/{t.World.DirectorState.BandSpawns}");
        Assert.InRange(first, 117, 123);
        Assert.InRange(second, 207, 213);
    }

    [Fact]
    public void Interval_spawning_holds_at_max_alive_and_cap()
    {
        // 구간 동시 최대 40 → 그 위로 일반 생성 없음
        var t = World(IntervalDefs([Interval(0f, 600f, 20f, 40)]));
        var peak = 0;
        for (var i = 0; i < 600; i++)
        {
            t.Step();
            peak = Math.Max(peak, SpawnGate.Alive(t.World));
        }
        Assert.Equal(40, peak);
        Assert.True(t.World.DirectorState.Accumulator <= SpawnRules.AccumulatorHold + 20f * SimWorld.Dt + 1e-3f, "가득 찬 동안 누적기가 hold 값에 묶이지 않았다");

        // 상한(caps.enemies 50)이 구간 최대(1000)보다 먼저 막는다
        var c = World(IntervalDefs([Interval(0f, 600f, 30f, 1000)], new CapsDef { Enemies = 50, EnemiesAbsolute = 60 }));
        c.Step(600);
        Assert.Equal(50, SpawnGate.Alive(c.World));
    }

    [Fact]
    public void Event_spawns_cull_far_offscreen_interval_enemies_first()
    {
        var t = World(IntervalDefs([], new CapsDef { Enemies = 10, EnemiesAbsolute = 12 }));
        var w = t.World;
        var view = w.ViewRect;
        var hero = t.Hero.Pos;
        // 화면 밖 일반 적 8(거리 다르게)·화면 안 일반 적 2
        var far = new List<Enemy>();
        for (var i = 0; i < 8; i++)
            far.Add(EnemyFactory.Create(w, Grunt, EnemyRank.Normal, [], new Vector2(view.X1 + 20f + 20f * i, hero.Y), new SpawnContext(SpawnSource.Interval, EntityId.None)));
        var near = new[]
        {
            EnemyFactory.Create(w, Grunt, EnemyRank.Normal, [], hero + new Vector2(50f, 0f), new SpawnContext(SpawnSource.Interval, EntityId.None)),
            EnemyFactory.Create(w, Grunt, EnemyRank.Normal, [], hero + new Vector2(-50f, 0f), new SpawnContext(SpawnSource.Interval, EntityId.None)),
        };
        Assert.Equal(10, SpawnGate.Alive(w));

        Assert.Equal(0, SpawnGate.Reserve(w, SpawnSource.Interval, 3));     // 일반 = 막힘
        Assert.Equal(3, SpawnGate.Reserve(w, SpawnSource.ElitePack, 3));    // 이벤트 = 먼 화면 밖 일반 적 3 을 지움
        Assert.All(far.Skip(5), e => Assert.Equal(KillCause.Culled, e.KillCause));
        Assert.All(far.Take(5), e => Assert.False(e.Dead));
        Assert.All(near, e => Assert.False(e.Dead));
        // 분열·소환 = 절대 상한(12)까지 — 남은 화면 밖 5 를 다 지워도 화면 안 2 는 남는다
        Assert.Equal(10, SpawnGate.Reserve(w, SpawnSource.Summon, 50));
        Assert.All(near, e => Assert.False(e.Dead));
    }

    [Fact]
    public void Diamond_corner_hero_logs_entry_fallback_ratio()
    {
        // R23: 마름모 왼쪽 꼭짓점 가까이에 주인공을 두면 시야 사각형 둘레의 생성 띠 대부분이 맵 밖 — 입구 대체 비율을 로그로 낸다
        var t = World(IntervalDefs([Interval(0f, 600f, 3f, 100_000)], Unlimited));
        var iso = t.World.Arena!.Iso;
        var n = t.World.Arena.Size;
        var corner = iso.CellCenter(2, n - 3);
        t.PlaceHero(corner);
        t.Step(MathX.SecondsToTicks(60f, SimWorld.Dt));
        var st = t.World.DirectorState;
        var ratio = st.BandSpawns == 0 ? 0f : (float)st.EntryFallbacks / st.BandSpawns;
        output.WriteLine($"R23 corner=({corner.X:0},{corner.Y:0}) band_spawns={st.BandSpawns} entry_fallbacks={st.EntryFallbacks} ratio={ratio:P0} spawned={st.IntervalSpawned}");
        Assert.True(st.BandSpawns > 0);
        Assert.InRange(st.IntervalSpawned, 177, 183);   // 대체여도 마리 수는 같다
    }

    [Fact]
    public void Ring_keeps_min_radius_300_from_every_real_entry()
    {
        // 기획 10.3 v0.8.1: 작은 방 진입점의 주인공에게 좀비 24·420 고리 → 최소 거리 ≥ 300, 생략 비율을 로그로
        var defs = Wp4Data.Defs;
        var zombie = defs.Enemies.Get("zombie");
        var t = new TestWorldBuilder().WithDefs(defs).WithModules(FakeArena.Modules(weapons: false)).Build();
        var entries = t.World.Resolved!.Entries;
        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            var w = new TestWorldBuilder().WithDefs(defs).WithModules(FakeArena.Modules(weapons: false)).Build();
            w.PlaceHero(entry.Inside);
            var r = DirectorSpawns.Ring(w.World, zombie, 24, 420f, 300f);
            var skipped = 1f - (float)r.Placed / r.Wanted;
            output.WriteLine($"ring entry={entry.Id} placed={r.Placed}/{r.Wanted} skipped={skipped:P0} min_distance={r.MinDistance:0}");
            Assert.True(r.Placed > 0, $"{entry.Id}: 하나도 못 놓음");
            Assert.True(r.MinDistance >= 300f - 1e-3f, $"{entry.Id}: 최소 거리 {r.MinDistance:0} < 300");
        }
    }

    [Fact]
    public void Room_mode_spawns_only_while_spawning_open_and_shows_room_banner()
    {
        var defs = IntervalDefs([Interval(0f, 600f, 3f, 100_000)], Unlimited);
        var t = new TestWorldBuilder().WithDefs(defs).WithRoom(RoomPhase.Enter).WithModules(FakeArena.Modules(weapons: false)).Build();
        var w = t.World;
        t.Step();
        Assert.Equal($"{w.Stage!.Name} — 1", w.Banner.Text);

        var grace = MathX.SecondsToTicks(w.Defs.Rules.Rooms!.Grace, SimWorld.Dt);
        t.Step(grace - 2);
        Assert.Equal(0, w.DirectorState.IntervalSpawned);    // 유예(들어옴) 중 생성 없음
        t.Step(MathX.SecondsToTicks(10f, SimWorld.Dt));
        Assert.Equal(RoomPhase.Survive, w.Room!.Phase);
        Assert.True(w.DirectorState.IntervalSpawned > 0);

        // 생존 끝(방 0:00–1:00) 뒤 정리 단계에서는 생성 없음
        while (w.Room.Phase == RoomPhase.Survive) t.Step();
        var atEnd = w.DirectorState.IntervalSpawned;
        t.Step(MathX.SecondsToTicks(5f, SimWorld.Dt));
        Assert.False(w.Room.SpawningOpen);
        Assert.Equal(atEnd, w.DirectorState.IntervalSpawned);
    }
}
