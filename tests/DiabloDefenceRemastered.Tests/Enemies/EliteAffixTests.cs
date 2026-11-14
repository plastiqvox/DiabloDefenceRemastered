using DiabloDefenceRemastered.Core.Sim.Director;
using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 디아2식 정예·우두머리(2026-11-14, 기획 9.1~9.3): 챔피언 무리 공통 접두사·우두머리 무리(고유 이름 + 졸개)·접두사 고르기 조건(등급·무리·필요 부품)·
/// 이름판 글자·등급 발광이 접두사에 덮이지 않음·번개 튐·흡혈·다중 사격.
/// </summary>
public sealed class EliteAffixTests
{
    private static readonly ProjectileDef Spark = new() { Id = "pj_t_spark", Sprite = DefRef<SpriteDef>.To(TestDefs.Sprite("spark")), Speed = 170f };

    private static AffixDef Unique(string id, string? group = null, params PartDef[] behaviors) => new()
    {
        Id = id, Name = id + "의", Label = id + "_label", Ranks = [DirectorSpawns.AffixRankUnique], Group = group, Behaviors = behaviors,
    };

    private static readonly AffixDef BerserkElite = Berserk with { Label = "광폭" };
    private static readonly AffixDef AuraA = Unique("aura_a", "aura");
    private static readonly AffixDef AuraB = Unique("aura_b", "aura");
    private static readonly AffixDef EnchantA = Unique("enchant_a", "enchant");
    private static readonly AffixDef Multi = Unique("multi") with { RequireParts = ["shoot_projectile"], ExtraShots = 2 };



    private static GameDefs DefsWith(IEnumerable<EnemyDef> enemies, IEnumerable<AffixDef> affixes) => Defs(enemies, affixes: affixes);

    private static List<Enemy> Alive(TestWorld t) => t.World.Enemies.Items.ToArray().Where(e => e.IsAlive).ToList();

    [Fact]
    public void ChampionPack_AllEliteSameAffix_EliteEligibleOnly()
    {
        var melee = Enemy("t_melee", B("chase_melee", new ChaseMeleeParams()));
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var t = World(DefsWith([melee], [BerserkElite, AuraA, EnchantA]), seed);
            var made = DirectorSpawns.ElitePack(t.World, melee, 4, 1, 0f);
            var pack = Alive(t);
            Assert.InRange(made, 2, 4);   // 자리 못 잡은 무리원은 건너뜀(원래 규칙)
            Assert.Equal(made, pack.Count);
            Assert.All(pack, e => Assert.Equal(EnemyRank.Elite, e.Rank));
            Assert.All(pack, e => Assert.Equal(0, e.AffixIdx[0]));   // 정예가 받을 수 있는 것은 광폭 하나뿐
            Assert.All(pack, e => Assert.Equal(-1, e.NameRoll));
        }
    }

    [Fact]
    public void UniquePack_LeaderPlusNormalMinions_NamedTwoAffixesDifferentGroups()
    {
        var melee = Enemy("t_melee", B("chase_melee", new ChaseMeleeParams()));
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var t = World(DefsWith([melee], [BerserkElite, AuraA, AuraB, EnchantA]), seed);
            var made = DirectorSpawns.ElitePack(t.World, melee, 4, 1, 1f);
            var pack = Alive(t);
            Assert.InRange(made, 2, 4);
            var lead = Assert.Single(pack, e => e.Rank == EnemyRank.Unique);   // 대장은 무리 가운데라 늘 놓인다
            Assert.Equal(made - 1, pack.Count(e => e.Rank == EnemyRank.Normal));
            Assert.All(pack.Where(e => e.Rank == EnemyRank.Normal), e => Assert.Equal(-1, e.AffixIdx[0]));   // 졸개는 접두사 없음
            Assert.True(lead.NameRoll >= 0);
            Assert.True(lead.AffixIdx[0] >= 0 && lead.AffixIdx[1] >= 0);
            var a0 = t.World.Defs.Affixes[lead.AffixIdx[0]];
            var a1 = t.World.Defs.Affixes[lead.AffixIdx[1]];
            Assert.NotEqual(a0.Id, a1.Id);
            Assert.False(a0.Group is not null && a0.Group == a1.Group, $"같은 무리 둘: {a0.Id}·{a1.Id}");   // 오라 둘은 안 됨
        }
    }

    [Fact]
    public void PickAffixes_RequireParts_OnlyShooters()
    {
        var melee = Enemy("t_melee", B("chase_melee", new ChaseMeleeParams()));
        var archer = Enemy("t_archer", B("shoot_projectile", new ShootProjectileParams { Projectile = DefRef<ProjectileDef>.To(Spark), Range = 300f, Cooldown = 1f }));
        var t = World(DefsWith([melee, archer], [Multi, AuraA]));
        Span<int> into = stackalloc int[2];
        for (var i = 0; i < 20; i++)
        {
            var n = DirectorSpawns.PickAffixes(t.World, melee, DirectorSpawns.AffixRankUnique, 2, into);
            Assert.Equal(1, n);
            Assert.Equal("aura_a", t.World.Defs.Affixes[into[0]].Id);
        }
        Assert.Equal(2, DirectorSpawns.PickAffixes(t.World, archer, DirectorSpawns.AffixRankUnique, 2, into));
        // 정예는 우두머리 전용을 받지 않는다
        Assert.Equal(0, DirectorSpawns.PickAffixes(t.World, archer, DirectorSpawns.AffixRankElite, 1, into));
    }

    [Fact]
    public void Plate_EliteBluePrefix_UniqueNameAndAffixLine()
    {
        var melee = Enemy("t_melee", B("chase_melee", new ChaseMeleeParams())) with { Name = "해골" };
        var t = World(DefsWith([melee], [BerserkElite, AuraA]));
        var elite = EnemyFactory.Create(t.World, melee, EnemyRank.Elite, [0], t.Hero.Pos + new Vector2(200f, 0f), new SpawnContext(SpawnSource.ElitePack, EntityId.None));
        Assert.Equal(("광폭한 해골", (string?)null), UniqueNames.Plate(t.World, elite));
        var unique = EnemyFactory.Create(t.World, melee, EnemyRank.Unique, [0, 1], t.Hero.Pos + new Vector2(-200f, 0f), new SpawnContext(SpawnSource.ElitePack, EntityId.None));
        unique.NameRoll = 1 | 0 << 10 | 1 << 20;   // 검은 + 이빨 + 파수꾼
        Assert.Equal(("검은이빨 파수꾼", (string?)"해골 · 광폭 · aura_a_label"), UniqueNames.Plate(t.World, unique));
        unique.NameRoll = -1;   // 이름 굴림 없음 → 종류 이름, 둘째 줄에 종류를 다시 쓰지 않음
        Assert.Equal(("해골", (string?)"광폭 · aura_a_label"), UniqueNames.Plate(t.World, unique));
    }

    [Fact]
    public void RankGlow_NotOverriddenByAffixGlow()
    {
        var melee = Enemy("t_melee", B("chase_melee", new ChaseMeleeParams()));
        var t = World(DefsWith([melee], [BerserkElite]));
        var elite = EnemyFactory.Create(t.World, melee, EnemyRank.Elite, [0], t.Hero.Pos + new Vector2(300f, 0f), new SpawnContext(SpawnSource.ElitePack, EntityId.None));
        var unique = EnemyFactory.Create(t.World, melee, EnemyRank.Unique, [0], t.Hero.Pos + new Vector2(-300f, 0f), new SpawnContext(SpawnSource.ElitePack, EntityId.None));
        t.Step(2);
        Assert.Equal(new Rgb(90, 140, 255), elite.Visual.GlowColor);    // 광폭 붉은 발광이 아니라 정예 파랑
        Assert.Equal(new Rgb(255, 200, 80), unique.Visual.GlowColor);   // 우두머리 금
    }

    [Fact]
    public void SparkOnHurt_FiresOnHit_WithCooldown()
    {
        var def = Enemy("t_spark", B("spark_on_hurt", new SparkOnHurtParams { Projectile = DefRef<ProjectileDef>.To(Spark), Count = 3, Cooldown = 0.5f }));
        var t = World(Defs([def]));
        var e = Spawn(t, def.Id, t.Hero.Pos + new Vector2(200f, 0f));
        t.Step();
        var spawner = Spawner(t);
        t.World.Damage.ToEnemy(e.Id, 1f, 0f, 0f, -1);
        t.Step(2);   // 맞음(이번 틱) → 다음 틱 EnemyThink 가 튐
        Assert.Equal(3, spawner.Projectiles.Count(p => p.Def == Spark));
        t.World.Damage.ToEnemy(e.Id, 1f, 0f, 0f, -1);
        t.Step(2);   // 쿨 0.5s 안 — 더 안 튐
        Assert.Equal(3, spawner.Projectiles.Count(p => p.Def == Spark));
        t.Step(30);
        t.World.Damage.ToEnemy(e.Id, 1f, 0f, 0f, -1);
        t.Step(2);
        Assert.Equal(6, spawner.Projectiles.Count(p => p.Def == Spark));
    }

    [Fact]
    public void SparkOnHurt_SkipsWhenHeroTooClose()
    {
        var def = Enemy("t_spark", B("spark_on_hurt", new SparkOnHurtParams { Projectile = DefRef<ProjectileDef>.To(Spark), Count = 3, Cooldown = 0.5f }));
        var t = World(Defs([def]));
        var e = Spawn(t, def.Id, t.Hero.Pos + new Vector2(20f, 0f));   // 생성 금지 48px 안
        t.Step();
        t.World.Damage.ToEnemy(e.Id, 1f, 0f, 0f, -1);
        t.Step(2);
        Assert.DoesNotContain(Spawner(t).Projectiles, p => p.Def == Spark);
    }

    [Fact]
    public void LifeSteal_HealsByDamageDealtToHero()
    {
        var def = Enemy("t_vamp", B("chase_melee", new ChaseMeleeParams { Cooldown = 0.5f }), B("life_steal", new LifeStealParams { Pct = 1f }));
        var t = World(Defs([def]));
        var e = Spawn(t, def.Id, t.Hero.Pos + new Vector2(25f, 0f));
        e.Hp = e.MaxHp - 50f;
        var before = e.Hp;
        t.Step(120);
        Assert.True(t.Hero.Hp < 100f, "근접이 주인공을 맞혀야 한다");
        Assert.True(e.Hp > before, $"흡혈로 회복해야 한다({before} → {e.Hp})");
    }

    [Fact]
    public void Multishot_FiresFanOfThree()
    {
        var archer = Enemy("t_archer", B("shoot_projectile", new ShootProjectileParams { Projectile = DefRef<ProjectileDef>.To(Spark), Range = 400f, Cooldown = 5f }));
        var t = World(DefsWith([archer], [Multi]));
        var e = EnemyFactory.Create(t.World, archer, EnemyRank.Unique, [0], t.Hero.Pos + new Vector2(200f, 0f), new SpawnContext(SpawnSource.ElitePack, EntityId.None));
        Assert.Equal(2, e.ExtraShots);
        t.Step(120);
        var shots = Spawner(t).Projectiles.Where(p => p.Owner == e.Id).ToList();
        Assert.Equal(3, shots.Count);   // 쿨 5s — 한 번 쏨, 세 발
        var angles = shots.Select(s => MathF.Atan2(s.Dir.Y, s.Dir.X)).Order().ToList();
        Assert.Equal(ShootProjectile.MultiShotStep, angles[1] - angles[0], 3);
    }

    [Fact]
    public void ChillAffix_SetsHitFlags_AndBodyHitCarriesIt()
    {
        var def = Enemy("t_cold", B("chase_melee", new ChaseMeleeParams { Cooldown = 0.5f }));
        var cold = new AffixDef { Id = "cold", Name = "얼어붙은", Ranks = [DirectorSpawns.AffixRankUnique], Chill = true };
        var t = World(DefsWith([def], [cold]));
        var e = EnemyFactory.Create(t.World, def, EnemyRank.Unique, [0], t.Hero.Pos + new Vector2(200f, 0f), new SpawnContext(SpawnSource.ElitePack, EntityId.None));
        Assert.Equal(DamageFlags.Chill, e.HitFlags);
        new EnemyCtx(t.World, e).HitHero(5);   // 근접·자폭·돌진이 모두 지나는 한 곳
        var r = t.World.Damage.Requests[t.World.Damage.Count - 1];
        Assert.Equal(DamageFlags.Chill, r.Flags & DamageFlags.Chill);
    }
}
