using DiabloDefenceRemastered.Core.Sim.Enemies;

namespace DiabloDefenceRemastered.Tests.Combat;

/// <summary>
/// 냉기 둔화(2026-11-14, 기획 9.3 냉기 강화 — 실제 전투 모듈): <c>DamageFlags.Chill</c> 피해가 들어가면 <c>rules.hero.chill</c> 시간 동안 이동 × move_mul,
/// 피격 무적으로 버린 요청은 둔화도 없음, 적 탄은 쏜 적의 표시를 이어받음. 버프 방어(저항의 오라)는 맞는 순간 더한다.
/// </summary>
public class ChillTests
{
    private static readonly ChillDef Chill = new() { MoveMul = 0.5f, Duration = 2f };

    /// <summary>정의 묶음 복사(바꿀 칸만 고쳐서) — GameDefs 는 record 가 아니라 init 속성을 그대로 옮긴다.</summary>
    private static GameDefs Copy(GameDefs d, Func<RulesDef, RulesDef>? rules = null, DefTable<BuffDef>? buffs = null)
    {
        var copy = new GameDefs();
        foreach (var p in typeof(GameDefs).GetProperties())
            if (p.CanWrite) p.SetValue(copy, p.GetValue(d));
        if (rules is not null) typeof(GameDefs).GetProperty(nameof(GameDefs.Rules))!.SetValue(copy, rules(d.Rules));
        if (buffs is not null) typeof(GameDefs).GetProperty(nameof(GameDefs.Buffs))!.SetValue(copy, buffs);
        return copy;
    }

    private static TestWorld Quiet(GameDefs? defs = null)
    {
        var d = defs ?? CombatKit.Defs();
        d = Copy(d, r => r with { Hero = r.Hero! with { Chill = Chill } });
        var t = CombatKit.World(d);
        CombatKit.ClearWeapons(t.World);
        return t;
    }

    [Fact]
    public void ChillHit_SlowsMoveForDuration()
    {
        var t = Quiet();
        t.Step();
        var baseSpeed = t.Hero.MoveSpeed;
        t.World.Damage.ToHero(EntityId.None, 5f, default, 0f, DamageFlags.Chill);
        t.Step();
        Assert.Equal(2f, t.Hero.ChillTime, 3);
        t.Step();
        Assert.Equal(baseSpeed * 0.5f, t.Hero.MoveSpeed, 3);
        t.Step(130);   // 2s 지남
        Assert.Equal(0f, t.Hero.ChillTime);
        Assert.Equal(baseSpeed, t.Hero.MoveSpeed, 3);
    }

    [Fact]
    public void ChillIgnoredDuringInvuln_AndPlainHitDoesNotChill()
    {
        var t = Quiet();
        t.World.Damage.ToHero(EntityId.None, 5f);
        t.World.Damage.ToHero(EntityId.None, 5f, default, 0f, DamageFlags.Chill);   // 같은 틱 — 피격 무적으로 버림
        t.Step();
        Assert.Equal(0f, t.Hero.ChillTime);
    }

    [Fact]
    public void EnemyProjectile_InheritsOwnerChill()
    {
        var t = Quiet();
        var w = t.World;
        var skeleton = w.Defs.Enemies.Get(CombatKit.Skeleton);
        var owner = EnemyFactory.Create(w, skeleton, EnemyRank.Normal, [], t.Hero.Pos + new Vector2(150f, 0f), new SpawnContext(SpawnSource.Interval, EntityId.None));
        owner.HitFlags = DamageFlags.Chill;
        var arrow = CombatKit.Projectile("t_pj_cold", 5f, 300f, range: 400f);
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(arrow, owner.Pos, new Vector2(-1f, 0f), 5, owner.Id));
        owner.Dead = true;   // 쏜 적이 죽은 뒤(서리 고리)에도 탄이 표시를 지닌다
        for (var i = 0; i < 60 && t.Hero.ChillTime <= 0f; i++) t.Step();
        Assert.True(t.Hero.ChillTime > 0f);
        Assert.True(t.Hero.Hp < 100f);
    }

    [Fact]
    public void BuffArmor_ReducesHeroDamageToEnemy()
    {
        var defiance = new BuffDef { Id = "t_defiance", Stats = new Dictionary<EnemyStatKey, float> { [EnemyStatKey.Armor] = 3f } };
        var d = CombatKit.Defs();
        var t = Quiet(Copy(d, buffs: new DefTable<BuffDef>([defiance])));
        var w = t.World;
        var e = EnemyFactory.Create(w, w.Defs.Enemies.Get(CombatKit.Skeleton), EnemyRank.Normal, [], t.Hero.Pos + new Vector2(150f, 0f), new SpawnContext(SpawnSource.Interval, EntityId.None));
        e.Buffs.Apply(defiance, 0, w.Tick + 600, 1f, e.Immunities);
        var hp = e.Hp;
        w.Damage.ToEnemy(e.Id, 10f, 0f, 0f, -1);
        t.Step();
        Assert.Equal(hp - 7f, e.Hp);
    }
}
