using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Tests.Sim;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 액트 1 중간 보스·보스·해골 대장 (기획 8.2·8.3·9.4·9.5, 설계 6.5·6.6 — 단계 2 wp4): 실제 <c>Data/enemies</c>(<see cref="Wp4Data"/> — 페이즈는 로더와 같은 풀기)를
/// 가짜 이웃 월드(<see cref="FakeArena"/> — 투사체·장판·예고는 기록만)에서 돌린다.
/// 해골왕 3페이즈(v0.9.5 — 70%·40%, 1.5s 무적·속도 70·75·순서 처음부터, P2 부르기 상한 8, 슬롯 2); 로트피스트 독 웅덩이(80·4s·0.5s·6)·HP 50% 좀비 6·바닥액 5발;
/// 블랙애시 kite 띠(220~320)·불화살 부채 5발 ±30°·연사 3발 × 5번은 또렷이 보일 때만(<c>clear_sight</c>, 탄 <c>pj_ash_arrow</c>); 해골 대장 지휘 오라가 반경 안 해골(호위 포함)에 걸리고 보스·자신에는 안 걸림.
/// </summary>
public sealed class BossAct1Tests
{
    private static GameDefs Defs => Wp4Data.Defs;

    private static TestWorld World()
    {
        var b = new TestWorldBuilder().WithDefs(Defs).WithModules(FakeArena.Modules(weapons: false));
        return b.Build();
    }

    private static Enemy Spawn(TestWorld t, string id, Vector2 pos, SpawnSource source = SpawnSource.Boss) =>
        EnemyFactory.Create(t.World, Defs.Enemies.Get(id), EnemyRank.Normal, [], pos, new SpawnContext(source, EntityId.None));

    private static RecordingSpawner Rec(TestWorld t) => (RecordingSpawner)t.World.Spawner;

    private static int Summoned(TestWorld t, Enemy by) => t.World.Enemies.Items.ToArray().Count(e => e.IsAlive && e.Summoner == by.Id);

    /// <summary>발사 방향 둘 사이 가장 큰 각(도) — ±180° 경계와 무관.</summary>
    private static float Spread(List<EnemyProjectileSpawn> shots)
    {
        var max = 0f;
        foreach (var a in shots)
            foreach (var b in shots)
                max = MathF.Max(max, MathF.Acos(Math.Clamp(Vector2.Dot(Vector2.Normalize(a.Dir), Vector2.Normalize(b.Dir)), -1f, 1f)) * 180f / MathF.PI);
        return max;
    }

    [Fact]
    public void Skeleton_king_values_match_design()
    {
        var k = Defs.Enemies.Get("skeleton_king");
        Assert.Equal((10000f, 55f, 30f, DefRank.Boss, false), (k.Hp, k.Speed, k.Radius!.Value, k.Rank, k.TimeScaling));
        Assert.Contains("knockback", k.Immunities);
        Assert.Contains("slow", k.Immunities);
        Assert.Equal(2, k.Phases.Count);
        Assert.Equal((0.7f, 1.5f, 70f, true), (k.Phases[0].HpBelow, k.Phases[0].Invulnerable, k.Phases[0].Speed!.Value, k.Phases[0].ClearProjectiles));
        Assert.Equal((0.4f, 1.5f, 75f, true), (k.Phases[1].HpBelow, k.Phases[1].Invulnerable, k.Phases[1].Speed!.Value, k.Phases[1].ClearProjectiles));
        // 순서(기획 9.5): P1 다섯 칸, P2 다섯 칸, P3 여덟 칸
        Assert.Equal(["fan5", "ring16a", "fan5", "ring16b", "dash1"], k.Sequence!.Steps.Select(s => s.Pattern));
        Assert.Equal(["spiral2", "burst7", "dash3", "raise4", "mix"], k.Phases[0].Sequence!.Steps.Select(s => s.Pattern));
        Assert.Equal(["blink", "appear20", "ring_spiral", "blink", "appear20", "wall", "dash3", "mix"], k.Phases[1].Sequence!.Steps.Select(s => s.Pattern));
        // 옛 파이어볼 부채·15초 해골 소환은 없음, 내려치기는 세 페이즈 모두
        Assert.All(new[] { k.Patterns, k.Phases[0].Patterns, k.Phases[1].Patterns }, list =>
        {
            Assert.DoesNotContain(list, p => p.Id is "fire_fan" or "raise");
            Assert.Contains(list, p => p.Id == "slam");
        });
        var kit = EnemyKit.Build(k, 0, []);
        Assert.Equal(2, kit.Count);                                             // 내려치기 + 시퀀서(12 안)
        Assert.Equal("dt_boss", k.DropTable!.Id);
        Assert.Equal(100f, Defs.Enemies.Get("rotfeast").Xp);
    }

    [Fact]
    public void Skeleton_king_phases_at_70_and_40_with_invulnerability_and_restart()
    {
        var t = World();
        var king = Spawn(t, "skeleton_king", t.Hero.Pos + new Vector2(400f, 0f));
        t.Step(2);
        Assert.Equal(0, king.PhaseIdx);
        king.Hp = king.MaxHp * 0.69f;
        t.Step();
        Assert.Equal(1, king.PhaseIdx);
        var until = king.InvulnUntilTick;
        Assert.Equal(t.World.Tick - 1 + 90, until);                             // 1.5s = 90틱
        Assert.Equal(70f, king.PhaseSpeed);
        var slot = king.Parts[king.Kit.SequencerSlot];
        Assert.Equal(0, slot.Counter);                                          // 순서 처음부터, 1.5s 쉼
        Assert.InRange(slot.Cooldown, 1.45f, 1.5f);
        t.Step(100);
        king.Hp = king.MaxHp * 0.39f;
        t.Step();
        Assert.Equal(2, king.PhaseIdx);
        Assert.Equal(75f, king.PhaseSpeed);
        king.Hp = king.MaxHp * 0.1f;
        t.Step(120);
        Assert.Equal(2, king.PhaseIdx);                                         // 문턱마다 한 번
    }

    [Fact]
    public void Skeleton_king_p1_opens_with_five_orb_fan_after_line_telegraph()
    {
        var t = World();
        var king = Spawn(t, "skeleton_king", t.Hero.Pos + new Vector2(300f, 0f));
        var rec = Rec(t);
        for (var i = 0; i < 300 && rec.Projectiles.Count == 0; i++) t.Step();
        var fan = rec.Projectiles.ToList();
        Assert.Equal(5, fan.Count);
        Assert.All(fan, p => Assert.Equal(("pj_sk_orb", 14, 150f, false), (p.Def.Id, p.Damage, p.Speed, p.SightGated)));   // 보스 패턴은 시야 조건 없음
        Assert.InRange(Spread(fan), 59f, 61f);                                  // 5발 60°
        Assert.Equal(6f, fan[0].Def.HeroHitRadius);
        var line = Assert.Single(rec.Telegraphs);
        Assert.Equal((TelegraphShape.Line, 0.5f), (line.Shape, line.Time));
    }

    [Fact]
    public void Skeleton_king_p2_summon_stops_at_alive_cap_eight()
    {
        var t = World();
        var king = Spawn(t, "skeleton_king", t.Hero.Pos + new Vector2(500f, 0f));
        king.Hp = king.MaxHp * 0.6f;
        for (var i = 0; i < 6; i++)
        {
            var s = Spawn(t, "skeleton", king.Pos + MathX.FromAngle(i) * (200f + 10f * i), SpawnSource.Summon);
            s.Summoner = king.Id;
        }
        for (var i = 0; i < 30 * 60 && Summoned(t, king) <= 6; i++) t.Step();
        Assert.Equal(8, Summoned(t, king));                                     // 4 마리를 부르려다 동시 8 에서 멈춤
    }

    [Fact]
    public void Rotfeast_leaves_poison_pool_and_raises_six_zombies_at_half_hp()
    {
        var t = World();
        var rot = Spawn(t, "rotfeast", t.Hero.Pos + new Vector2(300f, 0f), SpawnSource.Miniboss);   // 시야(빛 반경 380 — 2026-11-13) 안: 안 보이는 적은 패턴을 쓰지 않는다
        t.Step(2);
        var pool = Assert.Single(Rec(t).Hazards);
        Assert.Equal(("fx_poison_pool", 80f, 4f, 0.5f), (pool.Effect!.Effect.Id, pool.Radius, pool.Duration, pool.TickInterval));
        Assert.Equal(6, pool.TickDamage);                                       // 중간 보스 본체는 시간 배율 없음
        t.Step(8 * 60);
        Assert.Equal(2, Rec(t).Hazards.Count);                                  // 8s 마다

        Assert.Equal(0, Summoned(t, rot));
        rot.Hp = rot.MaxHp * 0.49f;
        for (var i = 0; i < 180 && Summoned(t, rot) == 0; i++) t.Step();
        Assert.Equal(6, Summoned(t, rot));
        Assert.All(t.World.Enemies.Items.ToArray().Where(e => e.Summoner == rot.Id), e => Assert.Equal("zombie", e.Def.Id));
        rot.Hp = rot.MaxHp * 0.2f;
        t.Step(300);
        Assert.Equal(6, Summoned(t, rot));                                      // 문턱 1회
    }

    [Fact]
    public void Blackash_fire_fan_five_arrows_only_when_seen_clearly()
    {
        // 안 보이는 자리: 주인공 바로 위 300px(띠 안·거리 ≤ 340 이지만 몸 위 끝이 화면 위 밖 — 또렷이 보이지 않음) → 1초 동안 불화살 없음
        var hidden = World();
        Assert.True(hidden.World.Visibility.Enabled);
        var up = Spawn(hidden, "blackash", hidden.Hero.Pos + new Vector2(0f, -300f), SpawnSource.Miniboss);
        hidden.Step(60);
        Assert.False(hidden.World.Visibility.IsClearlyVisible(up.Pos, up.BodyTop));
        Assert.DoesNotContain(Rec(hidden).Projectiles, p => p.Def.Id == "pj_ash_arrow");
        Assert.DoesNotContain(Rec(hidden).Telegraphs, _ => true);               // 불비(clear_sight)도 없음

        var t = World();
        var ash = Spawn(t, "blackash", t.Hero.Pos + new Vector2(260f, 0f), SpawnSource.Miniboss);
        var rec = Rec(t);
        // 옆 260px(띠 안, 또렷이 보임) — 차례(일으키기 둘 → 불비 → 연사 → 부채, v0.9.5): 불비 다음 연사(3발 24° × 5번, 9), 그다음 부채(5발 ±30°, 10). 모두 시야 조건 표시
        var volleys = new List<List<EnemyProjectileSpawn>>();
        List<EnemyProjectileSpawn>? fan = null;
        for (var i = 0; i < 900 && fan is null; i++)
        {
            var before = rec.Projectiles.Count;
            t.Step();
            var added = rec.Projectiles.Skip(before).Where(p => p.Def.Id == "pj_ash_arrow").ToList();
            if (added.Count == 3) volleys.Add(added);
            else if (added.Count == 5) fan = added;
        }
        Assert.Equal(5, volleys.Count);
        Assert.All(volleys, v => Assert.InRange(Spread(v), 23.5f, 24.5f));
        Assert.All(volleys, v => Assert.All(v, p => Assert.Equal((9, 240f, true), (p.Damage, p.Speed, p.SightGated))));
        Assert.NotNull(fan);
        Assert.All(fan!, p => Assert.True(p.SightGated));
        Assert.InRange(Spread(fan!), 59f, 61f);
        Assert.Equal(10, fan![0].Damage);
        Assert.Equal(6f, fan[0].Def.HeroHitRadius);                             // 블랙애시 전용 불화살(판정 6, 열린 질문 57)

        // 패턴 정의가 겨누는 둘(불비·불화살 부채)에 clear_sight 를 단다 — 시야 조건 자체는 PatternAbility(SightWallTests·EnemyRulesTests 가 본다)
        var def = Defs.Enemies.Get("blackash");
        Assert.True(def.Patterns.Single(p => p.Id == "fire_fan").ClearSight);
        Assert.True(def.Patterns.Single(p => p.Id == "fire_rain").ClearSight);
        Assert.False(def.Patterns.Single(p => p.Id == "raise_60").ClearSight);
    }

    [Fact]
    public void Blackash_kites_inside_band_220_320()
    {
        var t = World();
        var ash = Spawn(t, "blackash", t.Hero.Pos + new Vector2(120f, 0f), SpawnSource.Miniboss);
        var far = Spawn(t, "blackash", t.Hero.Pos + new Vector2(-500f, 0f), SpawnSource.Miniboss);
        // 부채·불비(독점 행동) 사이사이 kite: 가까운 쪽은 띠 쪽으로 물러나고 먼 쪽은 다가온다(6초)
        t.Step(6 * 60);
        Assert.InRange(Vector2.Distance(ash.Pos, t.Hero.Pos), 200f, 340f);
        Assert.InRange(Vector2.Distance(far.Pos, t.Hero.Pos), 200f, 499f);
    }

    [Fact]
    public void Captain_command_aura_reaches_escorts_but_not_self_or_bosses()
    {
        var t = World();
        var captain = Spawn(t, "skeleton_captain", t.Hero.Pos + new Vector2(600f, 0f), SpawnSource.Escort);
        var escorts = t.World.Enemies.Items.ToArray().Where(e => e.IsAlive && e.Leader == captain.Id).ToList();
        Assert.Equal(8, escorts.Count);                                         // 이벤트로 나온 대장은 호위 8(기본)
        var king = Spawn(t, "skeleton_king", captain.Pos + new Vector2(0f, 60f));
        var outside = Spawn(t, "skeleton", captain.Pos + new Vector2(0f, -260f), SpawnSource.Interval);
        t.Step(2);
        var command = Defs.Buffs.IndexOf("command");
        bool Has(Enemy e) => e.Buffs.Has(command);
        Assert.All(escorts.Where(e => Vector2.Distance(e.Pos, captain.Pos) <= 200f), e => Assert.True(Has(e), "호위에 오라가 없다"));
        Assert.False(Has(captain));                                             // 자신 제외
        Assert.False(Has(king));                                                // 보스 본체 제외
        Assert.False(Has(outside));                                             // 반경 200 밖
        Assert.Contains(escorts, Has);
    }
}
