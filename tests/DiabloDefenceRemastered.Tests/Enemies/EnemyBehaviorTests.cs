using DiabloDefenceRemastered.Core.Sim.Director;
using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;
using SimWorld = DiabloDefenceRemastered.Core.Sim.World;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 부품 행동 시험 (설계서 10.1 <c>Enemies</c>): 점화 중 죽으면 폭발 없음, 분열 3마리 일반 등급·튕김·대기, <c>panic_on_ally_death</c>(반경·정예·<c>Culled</c>),
/// 호위 출처별 수(이벤트 값 / 기본 8 / 구간 0), 목표 문 행진이 진입점에서 풀림, 돌진은 또렷이 보일 때만 시작(시야 끈 대조군).
/// 죽이기는 <c>FakeDamage</c> 처럼 틱 사이에 <c>Dead</c>·<c>KillCause.Damage</c> 를 놓는다(다음 틱 <c>DeathHooks</c>).
/// </summary>
public sealed class EnemyBehaviorTests(ITestOutputHelper output)
{
    private static readonly EnemyDef Minion = TestDefs.Enemy(Set, "beh_minion");

    private static void Kill(Enemy e, KillCause cause = KillCause.Damage)
    {
        e.Dead = true;
        e.KillCause = cause;
    }

    // ── self_destruct ──────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Self_destruct_killed_while_igniting_does_not_explode(bool killDuringFuse)
    {
        var bomber = Enemy("beh_bomber", B("self_destruct", new SelfDestructParams { Fuse = 0.6f, Radius = 60f, Damage = 15f }));
        var t = World(Defs([bomber]));
        var b = Spawn(t, "beh_bomber", t.Hero.Pos + new Vector2(t.Hero.Radius + 12f, 0f), source: SpawnSource.Escort);
        for (var i = 0; i < 10 && !b.Igniting; i++) t.Step();
        Assert.True(b.Igniting, "닿았는데 점화하지 않았다");
        if (killDuringFuse) Kill(b);
        // 저장소에서 빠진 적 객체는 비워지므로 죽음 원인은 그 틱의 EnemyKilled 이벤트로 읽는다
        KillCause? cause = killDuringFuse ? KillCause.Damage : null;
        for (var i = 0; i < 60; i++)
        {
            t.Step();
            foreach (ref readonly var k in t.World.Events.Current.EnemyKilled.AsSpan())
                if (cause is null && k.DefIndex == t.World.Defs.Enemies.IndexOf("beh_bomber")) cause = k.Cause;
        }
        if (killDuringFuse) Assert.Equal(100f, t.Hero.Hp);   // 폭발 없음
        else
        {
            Assert.Equal(KillCause.Culled, cause);              // 터진 적은 보상·시체 없음
            Assert.Equal(85f, t.Hero.Hp);
        }
        Assert.Equal(0, SpawnGate.Alive(t.World));
    }

    // ── split_on_death ─────────────────────────────────────────────

    [Fact]
    public void Split_makes_three_normal_children_thrown_80_and_waiting()
    {
        var carcass = Enemy("beh_carcass", B("split_on_death", new SplitOnDeathParams { Spawn = DefRef<EnemyDef>.To(Minion) }));
        var t = World(Defs([carcass, Minion]));
        var at = t.Hero.Pos + new Vector2(300f, 0f);
        var c = Spawn(t, "beh_carcass", at, EnemyRank.Elite, SpawnSource.ElitePack);
        t.Step();
        var pos = c.Pos;
        Kill(c);
        t.Step();
        var children = new List<Enemy>();
        foreach (var e in t.World.Enemies.Items)
            if (e.IsAlive && e.Source == SpawnSource.Split) children.Add(e);
        Assert.Equal(3, children.Count);
        foreach (var ch in children)
        {
            Assert.Equal(EnemyRank.Normal, ch.Rank);
            Assert.InRange(Vector2.Distance(ch.Pos, pos), 1f, 80f + 1f);
            Assert.Equal(t.World.Tick - 1 + MathX.SecondsToTicks(0.3f, SimWorld.Dt), ch.WakeTick);
        }
    }

    [Fact]
    public void Collapse_does_not_split()
    {
        var carcass = Enemy("beh_carcass2", B("split_on_death", new SplitOnDeathParams { Spawn = DefRef<EnemyDef>.To(Minion) }));
        var t = World(Defs([carcass, Minion]));
        var c = Spawn(t, "beh_carcass2", t.Hero.Pos + new Vector2(300f, 0f), source: SpawnSource.Escort);
        t.Step();
        Kill(c, KillCause.Collapse);
        t.Step();
        foreach (var e in t.World.Enemies.Items) Assert.NotEqual(SpawnSource.Split, e.Source);
    }

    // ── panic_on_ally_death ────────────────────────────────────────

    [Theory]
    [InlineData(KillCause.Damage, true)]
    [InlineData(KillCause.Culled, false)]
    [InlineData(KillCause.Collapse, false)]
    public void Panic_only_for_damage_deaths_in_radius_and_not_elites(KillCause cause, bool expectPanic)
    {
        var fallen = Enemy("beh_fallen", B("panic_on_ally_death", new PanicOnAllyDeathParams()));
        var other = Enemy("beh_other", B("panic_on_ally_death", new PanicOnAllyDeathParams()));
        var t = World(Defs([fallen, other]));
        var at = t.Hero.Pos + new Vector2(350f, 0f);
        var victim = Spawn(t, "beh_fallen", at, source: SpawnSource.Escort);
        var near = Spawn(t, "beh_fallen", at + new Vector2(0f, 100f), source: SpawnSource.Escort);
        var elite = Spawn(t, "beh_fallen", at + new Vector2(0f, -100f), EnemyRank.Elite, SpawnSource.ElitePack);
        var far = Spawn(t, "beh_fallen", at + new Vector2(0f, 220f), source: SpawnSource.Escort);
        var otherKind = Spawn(t, "beh_other", at + new Vector2(-100f, 0f), source: SpawnSource.Escort);
        t.Step();
        Kill(victim, cause);
        t.Step(3);   // 죽음 훅 틱 → 다음 틱 Previous.EnemyKilled
        Assert.Equal(expectPanic, near.Order.Kind == OrderKind.Flee);
        Assert.NotEqual(OrderKind.Flee, elite.Order.Kind);
        Assert.NotEqual(OrderKind.Flee, far.Order.Kind);
        Assert.NotEqual(OrderKind.Flee, otherKind.Order.Kind);   // same_def
        if (!expectPanic) return;

        // 1.5s 안에 다시 안 겁먹음: 공포가 끝난 0.9s 에 둘째가 죽어도 그대로
        t.Step(MathX.SecondsToTicks(0.9f, SimWorld.Dt));
        var second = Spawn(t, "beh_fallen", near.Pos + new Vector2(30f, 0f), source: SpawnSource.Escort);
        t.Step();
        Kill(second);
        t.Step(3);
        Assert.NotEqual(OrderKind.Flee, near.Order.Kind);
    }

    // ── escort ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(SpawnSource.Escort, -1, 8)]
    [InlineData(SpawnSource.Escort, 3, 3)]
    [InlineData(SpawnSource.Interval, -1, 0)]
    [InlineData(SpawnSource.ElitePack, -1, 0)]
    public void Escort_count_depends_on_source(SpawnSource source, int eventCount, int expected)
    {
        var captain = Enemy("beh_captain", B("escort", new EscortParams { EscortEnemy = DefRef<EnemyDef>.To(Minion) }));
        var t = World(Defs([captain, Minion]));
        var c = EnemyFactory.Create(t.World, captain, EnemyRank.Normal, [], t.Hero.Pos + new Vector2(400f, 0f), new SpawnContext(source, EntityId.None, eventCount));
        var members = 0;
        foreach (var e in t.World.Enemies.Items)
            if (e.IsAlive && e.Leader == c.Id) members++;
        Assert.Equal(expected, members);
    }

    // ── march ──────────────────────────────────────────────────────

    [Fact]
    public void March_toward_entry_releases_at_target_entry_before_straight_time()
    {
        var t = World(Defs([Minion]));
        var w = t.World;
        Assert.True(w.Resolved!.TryGetEntry("gate_nw", out var target));
        // 주인공은 행진 선에서 비켜 둔다(풀린 뒤 보통 행동 = 주인공 쪽)
        t.PlaceHero(w.Resolved.TryGetAnchor("center", out var c) ? c + new Vector2(0f, 200f) : t.Hero.Pos);
        new MarchEvent(new MarchParams { Enemy = DefRef<EnemyDef>.To(Minion), Count = 5, AtEntry = "gate_se", TowardEntry = "gate_nw", Straight = 120f }).Fire(w);
        var marchers = new List<Enemy>();
        foreach (var e in w.Enemies.Items) if (e.Source == SpawnSource.March) marchers.Add(e);
        Assert.Equal(5, marchers.Count);
        var until = marchers[0].Order.UntilTick;
        var releasedAt = new Dictionary<Enemy, float>();
        for (var i = 0; i < MathX.SecondsToTicks(100f, SimWorld.Dt) && releasedAt.Count < marchers.Count; i++)
        {
            t.Step();
            foreach (var m in marchers)
                if (m.Life == LifeState.Active && m.Order.Kind != OrderKind.March && !releasedAt.ContainsKey(m))
                    releasedAt[m] = Vector2.Distance(m.Pos, target.Inside);
        }
        output.WriteLine($"released {releasedAt.Count}/5 at tick {w.Tick} (until {until}), distances {string.Join(", ", releasedAt.Values.Select(d => d.ToString("0")))}");
        Assert.Equal(marchers.Count, releasedAt.Count);
        Assert.True(w.Tick < until, "직진 시간이 다 돼서야 풀렸다");
        Assert.All(releasedAt.Values, d => Assert.True(d <= EnemyThinkSystem.MarchRelease + 30f, $"진입점에서 {d:0}px 떨어져 풀림"));
    }

    // ── charge 시야 조건 ───────────────────────────────────────────

    private static (int Telegraphs, float FarthestStart) RunCharger(bool sight)
    {
        var tel = new TelegraphDef { Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_beh" }), Time = 0.5f };
        var ghoul = Enemy("beh_ghoul", B("charge", new ChargeParams { RangeBand = [0f, 260f], Telegraph = tel, Damage = 10f })) with { Speed = 45f };
        var t = World(Defs([ghoul], sight: sight ? TestDefs.Sight : null, heroLight: sight ? 200f : 0f));
        var g = Spawn(t, "beh_ghoul", t.Hero.Pos + new Vector2(240f, 0f), source: SpawnSource.Escort);
        var sp = Spawner(t);
        var farthest = 0f;
        for (var i = 0; i < 300; i++)
        {
            var before = sp.Telegraphs.Count;
            var d = Vector2.Distance(g.Pos, t.Hero.Pos);
            t.Step();
            if (sp.Telegraphs.Count > before) farthest = MathF.Max(farthest, d);
        }
        return (sp.Telegraphs.Count, farthest);
    }

    [Fact]
    public void Charge_starts_only_when_seen_clearly()
    {
        // 빛 200 → 또렷이 보임 160 안. 240 에서 시작한 구울은 160 밖에서 돌진을 시작하지 않는다. 대조군(시야 끔)은 240 에서 바로 시작.
        var lit = RunCharger(sight: true);
        var dark = RunCharger(sight: false);
        output.WriteLine($"sight: telegraphs={lit.Telegraphs} farthest={lit.FarthestStart:0} / control: telegraphs={dark.Telegraphs} farthest={dark.FarthestStart:0}");
        Assert.True(dark.Telegraphs > 0 && dark.FarthestStart > 160f, "대조군이 160 밖에서 돌진하지 않아 대조가 되지 않는다");
        Assert.True(lit.Telegraphs > 0, "보이게 된 뒤에도 돌진하지 않았다");
        Assert.True(lit.FarthestStart <= 160f, $"또렷이 보이지 않는 {lit.FarthestStart:0} 에서 돌진을 시작했다");
    }

    // ── 상한 ───────────────────────────────────────────────────────

    [Fact]
    public void Split_respects_absolute_cap()
    {
        var carcass = Enemy("beh_carcass3", B("split_on_death", new SplitOnDeathParams { Spawn = DefRef<EnemyDef>.To(Minion) }));
        var t = World(Defs([carcass, Minion], caps: new CapsDef { Enemies = 4, EnemiesAbsolute = 5 }));
        var c = Spawn(t, "beh_carcass3", t.Hero.Pos + new Vector2(100f, 0f), source: SpawnSource.Escort);
        for (var i = 0; i < 3; i++) Spawn(t, "beh_minion", t.Hero.Pos + new Vector2(-100f, 30f * i), source: SpawnSource.Escort);
        t.Step();
        Kill(c);
        t.Step();
        Assert.True(SpawnGate.Alive(t.World) <= 5);
    }
}
