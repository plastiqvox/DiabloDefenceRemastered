namespace DiabloDefenceRemastered.Tests.Visibility;

/// <summary>안개 알파 <see cref="FogMath"/> (설계서 4.11·7.10, 10.1 <c>Visibility</c> 행의 <c>FogMath</c>, WP1c).</summary>
public class FogMathTests
{
    private const float R = TestDefs.SightRadius;

    private static TestWorld Room(int heroTx = 20, int heroTy = 20, PiecePlacementDef[]? place = null)
    {
        var t = new TestWorldBuilder().WithMegaTiles(20).WithSight().WithPlace(place ?? []).Build();
        t.PlaceHero(t.CellCenter(heroTx, heroTy));
        return t;
    }

    private static PiecePlacementDef Wall => new() { Piece = "inner_wall", Line = [new(6, 8), new(14, 8)] };   // 칸 ty 16, tx 12..29 = X

    [Fact]
    public void CellLevel_RampsOverNineTicks()
    {
        // (35,35): 주인공 (20,20) 에서 범위 안, (19,19) 에서 범위 밖
        var t = Room();
        var f = t.World.Visibility;
        t.Step(20);
        Assert.Equal(1f, FogMath.CellLevel(f, 35, 35, 19));
        t.PlaceHero(t.CellCenter(19, 19));
        t.Step();                                         // 틱 20 에 꺼짐
        for (var tick = 20; tick <= 31; tick++)
            Assert.Equal(Math.Max(0f, 1f - (tick - 20) / 9f), FogMath.CellLevel(f, 35, 35, tick), 5);
        Assert.Equal(0f, FogMath.CellLevel(f, 70, 70, 20));   // 확장 격자 밖
    }

    [Fact]
    public void CellLevel_IsContinuous_WhenFlippingEveryTick()
    {
        var t = Room();
        var f = t.World.Visibility;
        t.Step(20);
        var prev = FogMath.CellLevel(f, 35, 35, 19);
        var min = 1f;
        for (var tick = 20; tick < 80; tick++)
        {
            t.PlaceHero(t.CellCenter(tick % 2 == 0 ? 19 : 20, tick % 2 == 0 ? 19 : 20));
            t.Step();
            Assert.Equal(tick % 2 == 1, f.InSight(35, 35));
            var level = FogMath.CellLevel(f, 35, 35, tick);
            Assert.True(MathF.Abs(level - prev) <= 1f / 9f + 1e-5f, $"틱 {tick}: {prev} → {level}");
            min = MathF.Min(min, level);
            prev = level;
        }
        Assert.True(min > 0.5f);   // 매 틱 뒤집혀도 0 으로 튀지 않는다

        // 9틱 안 왕복: 3틱 꺼졌다 다시 켜지면 바뀐 틱 앞뒤 차이 ≤ 1/9
        var u = Room();
        var g = u.World.Visibility;
        u.Step(20);
        u.PlaceHero(u.CellCenter(19, 19));
        u.Step(3);
        var before = FogMath.CellLevel(g, 35, 35, 22);
        u.PlaceHero(u.CellCenter(20, 20));
        u.Step();
        var after = FogMath.CellLevel(g, 35, 35, 23);
        Assert.Equal(7f / 9f, before, 5);
        Assert.Equal(6f / 9f, after, 5);
    }

    [Fact]
    public void PosAlpha_EdgeBlur_AndDrawingOverload()
    {
        var t = Room();
        t.Step();
        var f = t.World.Visibility;
        var p = t.Hero.Pos;
        Assert.Equal(1f, FogMath.PosAlpha(f, p + new Vector2(R - 40f, 0), 0), 5);
        Assert.Equal(0.5f, FogMath.PosAlpha(f, p + new Vector2(R - 20f, 0), 0), 5);
        Assert.Equal(0f, FogMath.PosAlpha(f, p + new Vector2(R, 0), 0), 5);
        Assert.Equal(0f, FogMath.PosAlpha(f, p + new Vector2(R + 30f, 0), 0), 5);
        // 그리기용: 보간된 주인공 위치 기준 거리(칸 값은 그대로)
        Assert.Equal(1f, FogMath.PosAlpha(f, p + new Vector2(R - 20f, 0), 0, p + new Vector2(20f, 0)), 5);
        Assert.Equal(0.25f, FogMath.PosAlpha(f, p + new Vector2(R - 20f, 0), 0, p - new Vector2(10f, 0)), 5);
    }

    [Fact]
    public void PosAlpha_ZeroBehindWall()
    {
        var t = Room(place: [Wall]);
        t.Step();
        Assert.Equal(0f, FogMath.PosAlpha(t.World.Visibility, t.CellCenter(20, 14), 0));
        Assert.Equal(1f, FogMath.PosAlpha(t.World.Visibility, t.CellCenter(20, 17), 0));
    }

    [Fact]
    public void EnemyAlpha_BossesTouchingAndForceShown_AreOne()
    {
        // 주인공은 벽 줄(ty 16) 바로 아래 칸 (20,17) 위쪽, 적은 벽 뒤 칸 (20,15) 아래쪽 — 벽 칸 하나(약 36px) 사이
        var t = Room(20, 17, [Wall]);
        var c17 = t.CellCenter(20, 17);
        var c16 = t.CellCenter(20, 16);
        var c15 = t.CellCenter(20, 15);
        t.PlaceHero(c17 + 0.45f * (c16 - c17));
        t.Step();
        var w = t.World;
        Assert.Equal(new Int2(20, 17), w.Visibility.CellOf(t.Hero.Pos));
        Assert.Equal(14f, t.Hero.Radius);

        var near = c15 + 0.45f * (c16 - c15);     // 주인공까지 약 39.4px
        var far = c15 + 0.2f * (c16 - c15);       // 약 48.3px
        var e = t.AddEnemy(near);
        Assert.Equal(16f, e.TouchReach);          // TestKit: touch_margin
        Assert.Equal(TestDefs.BodyHeight, e.BodyTop);
        Assert.Equal(0f, FogMath.PosAlpha(w.Visibility, near, 0));
        Assert.Equal(1f, FogMath.EnemyAlpha(w, e, 0));   // 12 + 14 + 16 = 42 ≥ 39.4 — 닿음
        e.TouchReach = 0f;
        Assert.Equal(0f, FogMath.EnemyAlpha(w, e, 0));   // 26 < 39.4

        // 근접 사거리가 긴 적: TouchReach 가 touch_margin 보다 크면 그 거리까지
        e.Pos = far;
        e.TouchReach = 16f;
        Assert.Equal(0f, FogMath.EnemyAlpha(w, e, 0));   // 42 < 48.3
        e.TouchReach = 25f;
        Assert.Equal(1f, FogMath.EnemyAlpha(w, e, 0));   // 51 ≥ 48.3

        // 시야 밖 먼 적: 0, 점화 중(ForceShown)이면 1
        var hidden = t.AddEnemy(t.CellCenter(20, 12));
        Assert.Equal(0f, FogMath.EnemyAlpha(w, hidden, 0));
        hidden.Visual.ForceShown = true;
        Assert.Equal(1f, FogMath.EnemyAlpha(w, hidden, 0));

        // 중간 보스·보스는 늘 1
        var set = w.Defs.SpriteSets[0];
        var boss = t.AddEnemy(TestDefs.Enemy(set, id: "boss") with { Rank = DefRank.Boss }, t.CellCenter(20, 12));
        var mini = t.AddEnemy(TestDefs.Enemy(set, id: "mini") with { Rank = DefRank.Miniboss }, t.CellCenter(20, 12));
        Assert.Equal(1f, FogMath.EnemyAlpha(w, boss, 0));
        Assert.Equal(1f, FogMath.EnemyAlpha(w, mini, 0));

        // 그리기용 오버로드: 그리는 발 위치로 판정
        Assert.Equal(1f, FogMath.EnemyAlpha(w, hidden, 0, t.Hero.Pos, t.Hero.Pos + new Vector2(0, 30)) );
        hidden.Visual.ForceShown = false;
        Assert.Equal(1f, FogMath.EnemyAlpha(w, hidden, 0, t.Hero.Pos, t.Hero.Pos + new Vector2(0, 30)));   // 닿은 거리
        Assert.Equal(0f, FogMath.EnemyAlpha(w, hidden, 0, t.Hero.Pos, t.CellCenter(20, 13)));
    }

    [Fact]
    public void Disabled_AllAlphasOne()
    {
        var t = new TestWorldBuilder().Build();   // rules.sight 없음
        t.Step();
        var w = t.World;
        Assert.False(w.Visibility.Enabled);
        var e = t.AddEnemy(new Vector2(-5000, -5000));
        Assert.Equal(1f, FogMath.EnemyAlpha(w, e, 0));
        Assert.Equal(1f, FogMath.PosAlpha(w.Visibility, new Vector2(-5000, -5000), 0));
        Assert.Equal(1f, FogMath.CellLevel(w.Visibility, 0, 0, 0));
    }
}
