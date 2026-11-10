namespace DiabloDefenceRemastered.Tests.Combat;

/// <summary>10.1 <c>Combat</c> — 피격 무적·<c>--god</c>·승리 대기·입장 중 적·<c>on_end</c> 장판 세 경우·신호(WP3 완료 기준).</summary>
public class CombatTests
{
    private static int Cues(World w, CueKind kind)
    {
        var n = 0;
        foreach (var c in w.Presentation.Cues.AsSpan()) if (c.Kind == kind) n++;
        return n;
    }

    private static TestWorld Quiet(bool presentation = true)
    {
        var t = CombatKit.World(configure: b => b.WithPresentation(presentation));
        CombatKit.ClearWeapons(t.World);
        return t;
    }

    [Fact]
    public void HeroHurt_OncePerHit_NotDuringInvuln()
    {
        var t = Quiet();
        var w = t.World;
        Assert.Equal(100f, t.Hero.Hp);
        w.Damage.ToHero(EntityId.None, 10f);
        w.Damage.ToHero(EntityId.None, 10f);                       // 같은 틱 — 피격 무적
        t.Step();
        Assert.Equal(90f, t.Hero.Hp);
        Assert.Equal(1, Cues(w, CueKind.HeroHurt));
        w.Damage.ToHero(EntityId.None, 10f);                       // 0.25s 안
        t.Step();
        Assert.Equal(90f, t.Hero.Hp);
        Assert.Equal(1, Cues(w, CueKind.HeroHurt));
        t.Step(20);
        w.Damage.ToHero(EntityId.None, 10f);
        t.Step();
        Assert.Equal(80f, t.Hero.Hp);
        Assert.Equal(2, Cues(w, CueKind.HeroHurt));
    }

    [Fact]
    public void God_NoDamage_ButKnockback()
    {
        var t = Quiet();
        t.World.Options = new RunOptions { God = true };
        t.World.Damage.ToHero(EntityId.None, 50f, new Vector2(1, 0), 40f);
        t.Step();
        Assert.Equal(100f, t.Hero.Hp);
        Assert.True(t.Hero.KnockbackTime > 0f);
        Assert.True(t.Hero.KnockbackVel.X > 0f);
    }

    [Fact]
    public void VictoryPending_IgnoresHeroDamage()
    {
        var t = Quiet();
        t.World.Outcome = RunOutcome.VictoryPending;
        t.World.Damage.ToHero(EntityId.None, 50f);
        t.Step();
        Assert.Equal(100f, t.Hero.Hp);
    }

    [Fact]
    public void Armor_SubtractsMin1_AndDeathSignals()
    {
        var t = Quiet();
        t.Hero.Hp = 5f;
        t.World.Damage.ToHero(EntityId.None, 3f);
        t.Step();
        Assert.Equal(2f, t.Hero.Hp);
        t.Step(20);
        t.World.Damage.ToHero(EntityId.None, 30f);
        t.Step();
        Assert.True(t.Hero.Dead);
        Assert.Equal(1, Cues(t.World, CueKind.HeroDied));
        Assert.Equal(ClipKind.Die, t.Hero.Anim.Clip);
    }

    [Fact]
    public void EnteringEnemiesOnly_MissileWaits_PoolDoesNothing()
    {
        var t = CombatKit.World(configure: b => b.WithPresentation());
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);
        var e = t.AddEnemy(t.Hero.Pos + new Vector2(80, 0), CombatKit.Skeleton, LifeState.Entering);
        Core.Sim.Projectiles.HeroShots.Pool(w, 1, null, e.Pos, 60f, 5f, 0.5f, 10f, 0f, 0f);
        t.Step(120);
        Assert.Equal(0, w.Projectiles.LiveCount);
        Assert.Equal(0, Cues(w, CueKind.WeaponFired));
        Assert.Equal(20f, e.Hp);
    }

    [Fact]
    public void WeaponFired_OnePerActivation_EvenWithBurst()
    {
        var t = CombatKit.World(configure: b => b.WithPresentation());
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 8);                // 4발, 0.08s 간격
        for (var i = 0; i < 4; i++) t.AddEnemy(t.Hero.Pos + new Vector2(300 + i * 5, 0), CombatKit.Skeleton);
        t.Step(30);                                                 // 0.5s — 쿨 0.85 × 0.95 안
        Assert.Equal(1, Cues(w, CueKind.WeaponFired));
        var shots = 0;
        foreach (var p in w.Projectiles.Items) if (!p.Removed && p.Faction == Faction.Hero) shots++;
        Assert.Equal(4, shots);
    }

    [Fact]
    public void CastClip_PlaysWhenStanding()
    {
        var t = CombatKit.World();
        t.AddEnemy(t.Hero.Pos + new Vector2(100, 0), CombatKit.Skeleton);
        t.Step(2);
        Assert.Equal(ClipKind.CastLightning, t.Hero.Anim.Clip);
    }

    // ── on_end(맞음·사거리 끝·장애물 — 셋 다 장판 하나) ─────────────────

    private static int EnemyHazards(World w)
    {
        var n = 0;
        foreach (var h in w.Hazards.Items) if (!h.Removed && h.Faction == Faction.Enemy) n++;
        return n;
    }

    [Fact]
    public void OnEnd_OnHeroHit_OneHazard()
    {
        var t = Quiet(presentation: false);
        var w = t.World;
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(CombatKit.PjAcid, t.Hero.Pos + new Vector2(100, 0), new Vector2(-1, 0), 5, EntityId.None));
        t.Step(30);                                                 // (100 − 21)px / 200 ≈ 0.4s 에 맞음 — 장판 첫 틱은 피격 무적 안
        Assert.Equal(95f, t.Hero.Hp);
        Assert.Equal(0, w.Projectiles.LiveCount);
        Assert.Equal(1, EnemyHazards(w));
        var h = w.Hazards.Items.ToArray().Single(x => !x.Removed);
        Assert.Equal(3f, h.TickDamage);                             // 쏜 적이 없으면 정의 피해 그대로
        Assert.True(Vector2.Distance(h.Pos, t.Hero.Pos) < 30f);
    }

    [Fact]
    public void OnEnd_AtRangeEnd_OneHazard()
    {
        var t = Quiet(presentation: false);
        var w = t.World;
        var start = t.Hero.Pos + new Vector2(0, 40);
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(CombatKit.PjAcid, start, new Vector2(0, 1), 5, EntityId.None));
        t.Step(120);                                                // 340px / 200 = 1.7s
        Assert.Equal(0, w.Projectiles.LiveCount);
        Assert.Equal(1, EnemyHazards(w));
        var h = w.Hazards.Items.ToArray().Single(x => !x.Removed);
        Assert.InRange(Vector2.Distance(h.Pos, start), 335f, 345f);
        Assert.Equal(100f, t.Hero.Hp);
    }

    [Fact]
    public void OnEnd_AtObstacle_OneHazard_BeforeWall()
    {
        TestWorld? t = null;
        t = CombatKit.World(configure: b => b.Setup(w => w.Nav = new WallNav(w.Nav, w.Hero.Pos.X - 120f)));
        var world = t.World;
        CombatKit.ClearWeapons(world);
        var start = t.Hero.Pos + new Vector2(-40, 0);
        world.Spawner.SpawnProjectile(new EnemyProjectileSpawn(CombatKit.PjAcid, start, new Vector2(-1, 0), 5, EntityId.None));
        t.Step(60);
        Assert.Equal(0, world.Projectiles.LiveCount);
        Assert.Equal(1, EnemyHazards(world));
        var h = world.Hazards.Items.ToArray().Single(x => !x.Removed);
        Assert.True(h.Pos.X >= t.Hero.Pos.X - 120f - 0.5f);
        Assert.InRange(h.Pos.X, t.Hero.Pos.X - 125f, t.Hero.Pos.X - 70f);
    }

    [Fact]
    public void OnEnd_DamageFixedAtFire_FromOwnerMultiplier()
    {
        var t = Quiet(presentation: false);
        var w = t.World;
        var archer = t.AddEnemy(t.Hero.Pos + new Vector2(300, 300), CombatKit.Skeleton);
        archer.DamageMul = 2f;
        w.Spawner.SpawnProjectile(new EnemyProjectileSpawn(CombatKit.PjAcid, t.Hero.Pos + new Vector2(0, 40), new Vector2(0, 1), 5, archer.Id));
        archer.DamageMul = 10f;                                     // 날아가는 동안 바뀌어도 발사 순간 값
        t.Step(120);
        var h = w.Hazards.Items.ToArray().Single(x => !x.Removed);
        Assert.Equal(6f, h.TickDamage);
    }

    [Fact]
    public void Hazard_EnemySide_TicksOnHero()
    {
        var t = Quiet(presentation: false);
        var w = t.World;
        w.Spawner.SpawnHazard(new HazardSpawn(null, t.Hero.Pos, 40f, 2f, 0.5f, 4, Faction.Enemy, EntityId.None));
        t.Step(62);                                                 // 만든 다음 틱부터: 틱 1·31·61(간격 0.5s > 무적 0.25s)
        Assert.Equal(100f - 3 * 4f, t.Hero.Hp);
    }

    [Fact]
    public void Telegraph_BlastAtEnd_HitsHeroInside()
    {
        var t = Quiet(presentation: false);
        var w = t.World;
        w.Spawner.SpawnTelegraph(new TelegraphSpawn(null, TelegraphShape.Circle, t.Hero.Pos + new Vector2(20, 0), default, 60f, 0.5f, Faction.Enemy,
                                                    EntityId.None, new BlastSpec(60f, 25)));
        t.Step(20);
        Assert.Equal(100f, t.Hero.Hp);
        t.Step(20);
        Assert.Equal(75f, t.Hero.Hp);
        Assert.Equal(0, w.Telegraphs.LiveCount);
    }

    [Fact]
    public void HostileQuery_ReturnsHeroOnly_ForEnemySide()
    {
        var t = Quiet(presentation: false);
        var w = t.World;
        Assert.True(w.Hostiles.TryNearestHostile(Faction.Enemy, t.Hero.Pos + new Vector2(10, 0), 50f, out var id, out var pos));
        Assert.Equal(EntityId.Hero, id);
        Assert.Equal(t.Hero.Pos, pos);
        Assert.False(w.Hostiles.TryNearestHostile(Faction.Enemy, t.Hero.Pos + new Vector2(100, 0), 50f, out _, out _));
        Assert.False(w.Hostiles.TryNearestHostile(Faction.Hero, t.Hero.Pos, 50f, out _, out _));
    }

    [Fact]
    public void DamageByWeapon_SumsPerSlot()
    {
        var t = Quiet(presentation: false);
        var e = t.AddEnemy(t.Hero.Pos + new Vector2(200, 0), CombatKit.Skeleton);
        t.World.Damage.ToEnemy(e.Id, 7f, 0f, 0f, 4);
        t.Step();
        Assert.Equal(7f, t.World.RunStats.DamageByWeapon[4]);
        Assert.Equal(13f, e.Hp);
    }

    /// <summary>페이즈 전환 무적(WP4 <c>InvulnUntilTick</c>) 동안은 피해 요청을 버린다 — 그 틱이 지나면 다시 받는다(통합 때 이음).</summary>
    [Fact]
    public void EnemyPhaseInvuln_IgnoresDamageUntilTick()
    {
        var t = Quiet(presentation: false);
        var e = t.AddEnemy(t.Hero.Pos + new Vector2(200, 0), CombatKit.Skeleton);
        e.InvulnUntilTick = t.World.Tick + 2;
        t.World.Damage.ToEnemy(e.Id, 7f, 0f, 0f, 0);
        t.Step();
        Assert.Equal(20f, e.Hp);
        Assert.Equal(0f, t.World.RunStats.DamageByWeapon[0]);
        t.Step();
        t.World.Damage.ToEnemy(e.Id, 7f, 0f, 0f, 0);
        t.Step();
        Assert.Equal(13f, e.Hp);
    }
}
