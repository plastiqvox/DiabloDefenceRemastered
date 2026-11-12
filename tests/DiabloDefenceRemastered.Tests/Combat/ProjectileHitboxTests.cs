namespace DiabloDefenceRemastered.Tests.Combat;

/// <summary>
/// 적 탄 주인공 판정(설계서 6.20 ⑤ — 보스 묶음 A 이음새, 기획 38.1 ①·열린 질문 57): <c>hero_hit_radius</c> 가 있으면 몸 반경 대신 그 값.
/// 실제 전투 모듈(WP3 <c>ProjectilesSystem</c>)에서 탄을 주인공 옆으로 지나가게 한다. 주인공 몸 반경 14, 탄 반경 5.
/// </summary>
public sealed class ProjectileHitboxTests
{
    private static readonly ProjectileDef Small = CombatKit.Projectile("t_pj_small", 5f, 200f, range: 600f) with { HeroHitRadius = 6f };
    private static readonly ProjectileDef Normal = CombatKit.Projectile("t_pj_normal", 5f, 200f, range: 600f);

    /// <summary>주인공 발에서 가로 <paramref name="side"/> 떨어진 줄로 위에서 아래로 지나가게 쏘고 받은 피해.</summary>
    private static float Pass(ProjectileDef def, float side)
    {
        var t = CombatKit.World(configure: b => b.WithPresentation(false));
        CombatKit.ClearWeapons(t.World);
        var hp = t.Hero.Hp;
        t.World.Spawner.SpawnProjectile(new EnemyProjectileSpawn(def, t.Hero.Pos + new Vector2(side, -150f), new Vector2(0f, 1f), 9, EntityId.None));
        t.Step(150);
        return hp - t.Hero.Hp;
    }

    [Fact]
    public void SmallHitbox_Passes10pxHits5px()
    {
        // 판정 = 탄 반경 5 + 6 = 11: 탄 가장자리가 주인공 발에서 10px(가운데 15px) 옆이면 안 맞고, 5px(가운데 10px) 이면 맞는다
        Assert.Equal(0f, Pass(Small, 15f));
        Assert.Equal(9f, Pass(Small, 10f));
    }

    [Fact]
    public void NormalArrow_Unchanged()
    {
        // 판정 값이 없는 탄은 지금처럼 탄 반경 + 몸 반경 14 = 19 — 가운데 15px 옆도 맞는다
        Assert.Equal(9f, Pass(Normal, 15f));
        Assert.Equal(0f, Pass(Normal, 20f));
    }
}
