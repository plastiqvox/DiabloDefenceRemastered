namespace DiabloDefenceRemastered.Tests.Combat;

/// <summary>
/// 10.1 공정성 열 중 WP3 이 세는 둘(v0.5.1): <c>unseen_shots</c>(적 진영 투사체를 만드는 순간, <c>ICombatSpawner</c>)와
/// <c>hidden_hits</c>(주인공 피해를 적용할 때 원인이 적 본체 — 이번 틱 <c>Projectiles</c> 단계 전에 들어온 요청, 13장 WP3 기록).
/// 시야를 켠 합성 월드(주인공 빛 160px, 가장자리 40px)에서 가까운 적(30px)은 보이고 먼 적(300px)은 안 보인다.
/// </summary>
public class FairnessTests
{
    private static TestWorld Sighted()
    {
        var boss = TestDefs.Enemy(CombatKit.Defs().SpriteSets[0], "t_boss_rank", hp: 500f) with { Rank = DefRank.Boss };
        var c = CombatKit.Defs(extraEnemies: [boss]);
        var s = TestDefs.Basic(sight: TestDefs.Sight, heroLight: 160f);
        var defs = new GameDefs
        {
            Rules = c.Rules with { Sight = s.Rules.Sight },
            Sprites = c.Sprites, SpriteSets = c.SpriteSets, Projectiles = c.Projectiles, Weapons = c.Weapons, Passives = c.Passives, HeroClasses = c.HeroClasses,
            Tilesets = s.Tilesets, Maps = s.Maps, Stages = s.Stages, Enemies = c.Enemies, Rewards = c.Rewards, Pickups = c.Pickups,
        };
        var t = CombatKit.World(defs);
        CombatKit.ClearWeapons(t.World);
        Assert.True(t.World.Visibility.Enabled);
        return t;
    }

    private static void Shoot(World w, Enemy owner, bool sightGated = false) =>
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(CombatKit.PjAcid, owner.Pos, Vector2.UnitX, 5, owner.Id, SightGated: sightGated));

    [Fact]
    public void UnseenShots_FarNormalCounts_NearNot_BossOnlyWhenSightGated()
    {
        var t = Sighted();
        var w = t.World;
        var near = t.AddEnemy(t.Hero.Pos + new Vector2(30, 0), CombatKit.Skeleton);
        var far = t.AddEnemy(t.Hero.Pos + new Vector2(300, 0), CombatKit.Skeleton);
        var boss = t.AddEnemy(t.Hero.Pos + new Vector2(0, 300), "t_boss_rank");
        near.Hp = near.MaxHp = far.Hp = far.MaxHp = 1000f;
        t.Step(10);
        Assert.True(w.Visibility.IsClearlyVisible(near.Pos, near.BodyTop));
        Assert.False(w.Visibility.IsClearlyVisible(far.Pos, far.BodyTop));

        Shoot(w, near);
        Assert.Equal(0, w.Fairness.UnseenShots);
        Shoot(w, far);
        Assert.Equal(1, w.Fairness.UnseenShots);
        Shoot(w, boss);
        Assert.Equal(1, w.Fairness.UnseenShots);                    // 보스 — 세지 않음
        Shoot(w, boss, sightGated: true);
        Assert.Equal(2, w.Fairness.UnseenShots);                    // clear_sight 패턴 발사는 셈(v0.5.3)
    }

    [Fact]
    public void HiddenHits_FarBodyHitCounts_NearNot_ProjectileHitNot()
    {
        var t = Sighted();
        var w = t.World;
        var near = t.AddEnemy(t.Hero.Pos + new Vector2(30, 0), CombatKit.Skeleton);
        var far = t.AddEnemy(t.Hero.Pos + new Vector2(300, 0), CombatKit.Skeleton);
        near.Hp = near.MaxHp = far.Hp = far.MaxHp = 1000f;
        t.Step(10);

        w.Damage.ToHero(near.Id, 5f);
        t.Step();
        Assert.Equal(0, w.Fairness.HiddenHits);
        t.Step(30);
        w.Damage.ToHero(far.Id, 5f);                                // 본체(Projectiles 단계 전 요청)
        t.Step();
        Assert.Equal(1, w.Fairness.HiddenHits);

        // 먼 적의 투사체가 맞힌 것은 본체가 아님
        t.Step(30);
        var hp = t.Hero.Hp;
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(CombatKit.PjAcid, t.Hero.Pos - new Vector2(20, 0), Vector2.UnitX, 5, far.Id));
        t.Step(5);
        Assert.True(t.Hero.Hp < hp);
        Assert.Equal(1, w.Fairness.HiddenHits);
    }
}
