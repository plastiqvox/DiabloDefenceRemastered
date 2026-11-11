using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Tests.Sim;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 액트 1 중간 보스·보스·해골 대장 (기획 8.2·8.3·9.4·9.5, 설계 6.5·6.6 — 단계 2 wp4): 실제 <c>Data/enemies</c>(<see cref="Wp4Data"/> — 페이즈는 로더와 같은 풀기)를
/// 가짜 이웃 월드(<see cref="FakeArena"/> — 투사체·장판·예고는 기록만)에서 돌린다.
/// 해골왕 50% 페이즈 한 번·1s 무적·속도 70, 부른 적 상한 30, 파이어볼 부채 3 → 5발·±20° → ±40°; 로트피스트 독 웅덩이(80·4s·0.5s·6)·HP 50% 좀비 6;
/// 블랙애시 kite 띠(220~320)·불화살 부채 5발 ±30° 는 또렷이 보일 때만(<c>clear_sight</c>); 해골 대장 지휘 오라가 반경 안 해골(호위 포함)에 걸리고 보스·자신에는 안 걸림.
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

    [Fact]
    public void Skeleton_king_values_match_design()
    {
        var k = Defs.Enemies.Get("skeleton_king");
        Assert.Equal((10000f, 55f, 30f, DefRank.Boss, false), (k.Hp, k.Speed, k.Radius!.Value, k.Rank, k.TimeScaling));
        Assert.Contains("knockback", k.Immunities);
        Assert.Contains("slow", k.Immunities);
        var p2 = Assert.Single(k.Phases);
        Assert.Equal((0.5f, 1.0f, 70f), (p2.HpBelow, p2.Invulnerable, p2.Speed!.Value));
        Assert.Equal(3, p2.Patterns.Count);                                     // 같은 ID 패턴은 자리를 지킴
        Assert.Equal("dt_boss", k.DropTable!.Id);
        Assert.Equal(100f, Defs.Enemies.Get("rotfeast").Xp);
    }

    [Fact]
    public void Skeleton_king_phase_switches_once_with_one_second_invulnerability()
    {
        var t = World();
        var king = Spawn(t, "skeleton_king", t.Hero.Pos + new Vector2(400f, 0f));
        t.Step(2);
        Assert.Equal(0, king.PhaseIdx);
        king.Hp = king.MaxHp * 0.49f;
        t.Step();
        Assert.Equal(1, king.PhaseIdx);
        var until = king.InvulnUntilTick;
        Assert.Equal(t.World.Tick - 1 + 60, until);                             // 1s = 60틱
        Assert.Equal(70f, king.PhaseSpeed);
        king.Hp = king.MaxHp * 0.2f;
        t.Step(120);
        Assert.Equal(1, king.PhaseIdx);                                         // 한 번만
        Assert.Equal(until, king.InvulnUntilTick);
    }

    [Fact]
    public void Skeleton_king_fire_fan_three_then_five()
    {
        var t = World();
        var king = Spawn(t, "skeleton_king", t.Hero.Pos + new Vector2(300f, 0f));
        var rec = Rec(t);
        for (var i = 0; i < 240 && rec.Projectiles.Count == 0; i++) t.Step();
        var first = rec.Projectiles.Where(p => p.Def.Id == "pj_boss_fireball").ToList();
        Assert.Equal(3, first.Count);
        Assert.InRange(Spread(first), 39f, 41f);                                // ±20°
        Assert.All(first, p => Assert.False(p.SightGated));                     // 보스 패턴에는 시야 조건이 없다(기획 9.5)

        king.Hp = king.MaxHp * 0.45f;
        rec.Projectiles.Clear();
        for (var i = 0; i < 600 && rec.Projectiles.Count == 0; i++) t.Step();
        var second = rec.Projectiles.Where(p => p.Def.Id == "pj_boss_fireball").ToList();
        Assert.Equal(5, second.Count);
        Assert.InRange(Spread(second), 79f, 81f);                               // ±40°

    }

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
    public void Skeleton_king_summons_stop_at_alive_cap_thirty()
    {
        var t = World();
        var king = Spawn(t, "skeleton_king", t.Hero.Pos + new Vector2(500f, 0f));
        for (var i = 0; i < 25; i++)
        {
            var s = Spawn(t, "skeleton", king.Pos + MathX.FromAngle(i) * (150f + 4f * i), SpawnSource.Summon);
            s.Summoner = king.Id;
        }
        for (var i = 0; i < 20 * 60 && Summoned(t, king) <= 25; i++) t.Step();
        Assert.Equal(30, Summoned(t, king));                                    // 8 마리를 부르려다 상한 30 에서 멈춤
        t.Step(16 * 60);
        Assert.True(Summoned(t, king) <= 30);
    }

    [Fact]
    public void Rotfeast_leaves_poison_pool_and_raises_six_zombies_at_half_hp()
    {
        var t = World();
        var rot = Spawn(t, "rotfeast", t.Hero.Pos + new Vector2(400f, 0f), SpawnSource.Miniboss);
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
        Assert.DoesNotContain(Rec(hidden).Projectiles, p => p.Def.Id == "pj_fire_arrow");
        Assert.DoesNotContain(Rec(hidden).Telegraphs, _ => true);               // 불비(clear_sight)도 없음

        var t = World();
        var ash = Spawn(t, "blackash", t.Hero.Pos + new Vector2(260f, 0f), SpawnSource.Miniboss);
        var rec = Rec(t);
        // 옆 260px(띠 안, 또렷이 보임) — 부채를 쏜다: 5발 ±30°, 시야 조건 표시
        for (var i = 0; i < 300 && !rec.Projectiles.Any(p => p.Def.Id == "pj_fire_arrow"); i++) t.Step();
        var fan = rec.Projectiles.Where(p => p.Def.Id == "pj_fire_arrow").ToList();
        Assert.Equal(5, fan.Count);
        Assert.All(fan, p => Assert.True(p.SightGated));
        Assert.InRange(Spread(fan), 59f, 61f);
        Assert.Equal(10, fan[0].Damage);

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
