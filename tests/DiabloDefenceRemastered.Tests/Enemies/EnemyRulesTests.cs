using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 적 규칙 시험 (설계서 10.1 <c>Enemies</c>·11.1 WP4 완료 기준): 등급 크기 자르기, 오라·함성의 <c>include_bosses</c>, 함성 대상(결정 C),
/// 시야 조건(빛 반경 R − 40 밖 궁수는 쏘지 않고 다가옴 — 시야 끈 대조군은 쏨), <c>kite</c> 떨림(R29).
/// 이웃(WP2 이동·WP3 피해)은 <see cref="Sim.FakeArena"/> 가짜다.
/// </summary>
public sealed class EnemyRulesTests(ITestOutputHelper output)
{
    private static readonly DefRef<BuffDef> CommandRef = DefRef<BuffDef>.To(Command);
    private static readonly DefRef<BuffDef> CryRef = DefRef<BuffDef>.To(WarCryBuff);
    private static readonly ProjectileDef Arrow = new() { Id = "pj_rules", Sprite = DefRef<SpriteDef>.To(TestDefs.Sprite("arrow")), Speed = 200f };

    private static bool HasBuff(World w, Enemy e, BuffDef buff)
    {
        var index = w.Defs.Buffs.IndexOf(buff.Id);
        var slots = e.Buffs;
        for (var i = 0; i < BuffSlots.SlotCount; i++)
            if (slots[i].Active && slots[i].BuffIndex == index && slots[i].EndTick > w.Tick) return true;
        return false;
    }

    // ── 등급 크기 ──────────────────────────────────────────────────

    [Fact]
    public void Unique_rotting_carcass_is_clamped_to_max_scale_with_radius_22()
    {
        // 썩은 시체 = 정의 scale 1.35, 그림 묶음 base_radius 14(설계서 6.5 예). 우두머리 1.35 × 1.3 = 1.755 → 1.6, round(14 × 1.6) = 22.
        var set = TestDefs.SpriteSet(TestDefs.Sprite(), "set_carcass", baseRadius: 14f);
        var carcass = TestDefs.Enemy(set, "rotting_carcass") with { Scale = 1.35f };
        var t = World(Defs([carcass]));   // 그림 묶음은 정의가 직접 가리킨다(표에 없어도 됨)

        Assert.Equal(1.6f, EnemyFactory.FinalScale(t.World, carcass, EnemyRank.Unique), 3);
        Assert.Equal(22f, EnemyFactory.BodyRadius(t.World, carcass, EnemyRank.Unique));
        Assert.Equal(22f, EnemyFactory.BodyRadius(t.World, carcass, EnemyRank.Elite));    // 1.35 × 1.15 = 1.5525 → round(21.7)
        Assert.Equal(19f, EnemyFactory.BodyRadius(t.World, carcass, EnemyRank.Normal));   // round(18.9)

        var e = EnemyFactory.Create(t.World, carcass, EnemyRank.Unique, [], t.Hero.Pos + new Vector2(200f, 0f), new SpawnContext(SpawnSource.ElitePack, EntityId.None));
        Assert.Equal(1.6f, e.FinalScale, 3);
        Assert.Equal(22f, e.Radius);
    }

    // ── 오라·함성 대상 ─────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Command_aura_skips_bosses_unless_include_bosses(bool includeBosses)
    {
        var leader = Enemy("aura_leader", B("command_aura", new CommandAuraParams { Buff = CommandRef, TargetTag = "skeleton", IncludeBosses = includeBosses }));
        var grunt = Enemy("aura_grunt");
        var mini = Enemy("aura_mini") with { Rank = DefRank.Miniboss };
        var boss = Enemy("aura_boss") with { Rank = DefRank.Boss };
        var t = World(Defs([leader, grunt, mini, boss]));
        var at = t.Hero.Pos + new Vector2(400f, 0f);
        var l = Spawn(t, "aura_leader", at, source: SpawnSource.Escort);
        var g = Spawn(t, "aura_grunt", at + new Vector2(60f, 0f), source: SpawnSource.Escort);
        var m = Spawn(t, "aura_mini", at + new Vector2(0f, 60f), source: SpawnSource.Miniboss);
        var b = Spawn(t, "aura_boss", at + new Vector2(-60f, 0f), source: SpawnSource.Boss);
        t.Step(10);

        Assert.True(HasBuff(t.World, g, Command));
        Assert.False(HasBuff(t.World, l, Command));                 // 자신 제외
        Assert.Equal(includeBosses, HasBuff(t.World, m, Command));
        Assert.Equal(includeBosses, HasBuff(t.World, b, Command));
    }

    [Fact]
    public void Miniboss_war_cry_buffs_normal_allies_but_not_itself_or_other_bosses()
    {
        // 결정 C: 중간 보스가 외치는 함성은 자기 자신·반경 안 다른 중간 보스·보스에게 걸리지 않는다(include_bosses 거짓 — MVP 데이터에 참 없음)
        var caster = Enemy("cry_mini", B("war_cry", new WarCryParams { Buff = CryRef, Cooldown = 5f, Clip = ClipKind.Attack })) with { Rank = DefRank.Miniboss };
        var grunt = Enemy("cry_grunt");
        var other = Enemy("cry_other") with { Rank = DefRank.Miniboss };
        var boss = Enemy("cry_boss") with { Rank = DefRank.Boss };
        var t = World(Defs([caster, grunt, other, boss]));
        var at = t.Hero.Pos + new Vector2(300f, 0f);
        var c = Spawn(t, "cry_mini", at, source: SpawnSource.Miniboss);
        var g = Spawn(t, "cry_grunt", at + new Vector2(80f, 0f), source: SpawnSource.Escort);
        var o = Spawn(t, "cry_other", at + new Vector2(0f, 80f), source: SpawnSource.Miniboss);
        var b = Spawn(t, "cry_boss", at + new Vector2(0f, -80f), source: SpawnSource.Boss);

        var buffed = false;
        for (var i = 0; i < 120 && !buffed; i++)
        {
            t.Step();
            buffed = HasBuff(t.World, g, WarCryBuff);
        }
        Assert.True(buffed, "2초 안에 함성이 일반 적에게 걸리지 않았다");
        Assert.False(HasBuff(t.World, c, WarCryBuff));
        Assert.False(HasBuff(t.World, o, WarCryBuff));
        Assert.False(HasBuff(t.World, b, WarCryBuff));
    }

    [Fact]
    public void Normal_war_cry_includes_itself()
    {
        var caster = Enemy("cry_normal", B("war_cry", new WarCryParams { Buff = CryRef, Cooldown = 5f, Clip = ClipKind.Attack }));
        var t = World(Defs([caster]));
        var c = Spawn(t, "cry_normal", t.Hero.Pos + new Vector2(300f, 0f), source: SpawnSource.Escort);
        var buffed = false;
        for (var i = 0; i < 120 && !buffed; i++)
        {
            t.Step();
            buffed = HasBuff(t.World, c, WarCryBuff);
        }
        Assert.True(buffed);
    }

    // ── 시야 조건(v0.5) ────────────────────────────────────────────

    private static EnemyDef Archer(string id) => Enemy(id,
        B("kite", new KiteParams { RangeBand = [150f, 250f] }),
        B("shoot_projectile", new ShootProjectileParams { Projectile = DefRef<ProjectileDef>.To(Arrow), Cooldown = 0.5f, Range = 300f })) with { Speed = 60f };

    /// <summary>궁수를 주인공 오른쪽 280px 에 두고 4초. (쏜 수, 쐈을 때 가장 먼 거리, 마지막 거리)</summary>
    private static (int Shots, float FarthestShot, float LastDistance) RunArcher(bool sight, float light)
    {
        var archer = Archer("sight_archer");
        var t = World(Defs([archer], sight: sight ? TestDefs.Sight : null, heroLight: light));
        Assert.Equal(sight, t.World.Visibility.Enabled);
        var a = Spawn(t, "sight_archer", t.Hero.Pos + new Vector2(280f, 0f), source: SpawnSource.Escort);
        var spawner = Spawner(t);
        var farthest = 0f;
        for (var i = 0; i < 240; i++)
        {
            var before = spawner.Projectiles.Count;
            t.Step();
            if (spawner.Projectiles.Count > before) farthest = MathF.Max(farthest, Vector2.Distance(a.Pos, t.Hero.Pos));
        }
        return (spawner.Projectiles.Count, farthest, Vector2.Distance(a.Pos, t.Hero.Pos));
    }

    [Fact]
    public void Archer_outside_clear_sight_approaches_instead_of_shooting()
    {
        // 빛 반경 240 → 또렷이 보임 = R − 40 = 200 안. 280 에서 시작한 궁수는 200 안으로 들어오기 전에는 쏘지 않는다.
        var lit = RunArcher(sight: true, light: 240f);
        // 대조군: 시야를 끈 월드에서는 같은 자리(띠 최대 250 언저리, 사거리 300 안)에서 쏜다 — 시야 조건이 판정을 바꾼다는 확인
        var dark = RunArcher(sight: false, light: 0f);
        output.WriteLine($"sight: shots={lit.Shots} farthest={lit.FarthestShot:0} last={lit.LastDistance:0} / control: shots={dark.Shots} farthest={dark.FarthestShot:0} last={dark.LastDistance:0}");

        Assert.True(dark.Shots > 0, "대조군이 쏘지 않았다");
        Assert.True(dark.FarthestShot > 200f, "대조군이 R − 40 밖에서 쏘지 않아 대조가 되지 않는다");
        Assert.True(lit.LastDistance < 200f, $"궁수가 또렷이 보이는 거리로 다가오지 않았다({lit.LastDistance:0})");
        Assert.True(lit.FarthestShot <= 200f, $"또렷이 보이지 않는 거리 {lit.FarthestShot:0} 에서 쐈다");
        Assert.True(lit.Shots > 0, "보이게 된 뒤에도 쏘지 않았다");
    }

    [Fact]
    public void Kite_archer_above_still_hero_does_not_jitter()
    {
        // R29: 주인공 바로 위(화면 위쪽)에 궁수를 두고 주인공이 멈춰 있을 때 10초 동안 kite 상태 전환 ≤ 6
        var archer = Archer("jitter_archer");
        var t = World(Defs([archer], sight: TestDefs.Sight, heroLight: 420f));
        Spawn(t, "jitter_archer", t.Hero.Pos + new Vector2(0f, -230f), source: SpawnSource.Escort);
        t.Step(600);
        output.WriteLine($"kite_switches={t.World.Fairness.KiteSwitches} shots={Spawner(t).Projectiles.Count}");
        Assert.True(t.World.Fairness.KiteSwitches <= 6, $"kite 전환 {t.World.Fairness.KiteSwitches} > 6");
        Assert.True(Spawner(t).Projectiles.Count > 0, "위쪽 궁수가 그 자리에서 쏘지 않았다");
    }

    [Theory]
    [InlineData(-1f)]   // ① 주인공이 위(궁수 쪽)로 다가감
    [InlineData(1f)]    // ③ 주인공이 아래로 물러남 — 궁수가 따라옴
    public void Kite_archer_above_moving_hero_does_not_jitter(float dirY)
    {
        var archer = Archer("jitter_archer2");
        var t = World(Defs([archer], megaTiles: 20, sight: TestDefs.Sight, heroLight: 420f));
        var a = Spawn(t, "jitter_archer2", t.Hero.Pos + new Vector2(0f, -230f), source: SpawnSource.Escort);
        var startY = a.Pos.Y;
        for (var i = 0; i < 600; i++)
        {
            t.World.Command = new PlayerCommand(new Vector2(0f, dirY));
            t.Step();
        }
        var moved = a.Pos.Y - startY;
        output.WriteLine($"dir={dirY} kite_switches={t.World.Fairness.KiteSwitches} archer_dy={moved:0} shots={Spawner(t).Projectiles.Count}");
        Assert.True(t.World.Fairness.KiteSwitches <= 6, $"kite 전환 {t.World.Fairness.KiteSwitches} > 6");
        // 궁수(62px/s)는 주인공(95px/s)보다 느리다 — 10초 동안 아래로 300px 넘게 따라왔으면 따라온 것
        if (dirY > 0f) Assert.True(moved > 300f, $"물러나는 주인공을 따라오지 않았다(아래로 {moved:0}px)");
    }

    [Fact]
    public void Archer_with_head_above_screen_top_does_not_shoot()
    {
        // 화면 위 경우: 발은 화면 안쪽인데 몸 위 끝(BodyTop 72)이 화면 안쪽 16px 밖 — 또렷이 보이지 않음. 대조군(시야 끔)은 같은 자리에서 쏨.
        int Shots(bool sight, out int total)
        {
            var archer = Archer("top_archer") with { Behaviors = [B("kite", new KiteParams { RangeBand = [150f, 300f] }),
                B("shoot_projectile", new ShootProjectileParams { Projectile = DefRef<ProjectileDef>.To(Arrow), Cooldown = 0.5f, Range = 320f })] };
            var t = World(Defs([archer], megaTiles: 20, sight: sight ? TestDefs.Sight : null, heroLight: 420f));
            t.Step();
            var view = t.World.ViewRect;
            var a = Spawn(t, "top_archer", new Vector2(t.Hero.Pos.X, view.Y0 + 30f), source: SpawnSource.Escort);
            var sp = Spawner(t);
            var hidden = 0;
            for (var i = 0; i < 180; i++)
            {
                var headOut = a.Pos.Y - a.BodyTop < t.World.ViewRect.Y0 + TestDefs.Sight.ViewInset;
                var before = sp.Projectiles.Count;
                t.Step();
                if (headOut) hidden += sp.Projectiles.Count - before;
            }
            total = sp.Projectiles.Count;
            return hidden;
        }
        var lit = Shots(sight: true, out var litTotal);
        var dark = Shots(sight: false, out _);
        output.WriteLine($"shots while head above screen: sight={lit} (total {litTotal}) control={dark}");
        Assert.True(dark > 0, "대조군이 머리가 화면 밖인 자리에서 쏘지 않아 대조가 되지 않는다");
        Assert.Equal(0, lit);
    }
}
