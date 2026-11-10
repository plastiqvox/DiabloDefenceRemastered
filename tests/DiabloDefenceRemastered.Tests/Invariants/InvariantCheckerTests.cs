using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Sim.Movement.Invariants;
using DiabloDefenceRemastered.Tests.Movement;

namespace DiabloDefenceRemastered.Tests.Invariants;

/// <summary>계측기가 일부러 만든 NaN·맵 밖·장애물 안·멈춘 적·상한 초과·가려진 적을 센다 (설계서 10.1 <c>Invariants</c>).</summary>
public class InvariantCheckerTests
{
    private static (TestWorld T, InvariantChecker C) Make()
    {
        var ctx = new ModuleContext();
        new NavModule().Register(ctx);
        var t = new TestWorldBuilder().WithModules(ctx).Build();
        return (t, new InvariantChecker(t.World, (NavWorld)t.World.Nav));
    }

    [Fact]
    public void Counts_NanOutOfMapObstacle()
    {
        var (t, c) = Make();
        var nan = t.AddEnemy(t.Hero.Pos);
        nan.Pos = new Vector2(float.NaN, 0);
        t.AddEnemy(new Vector2(5, 5));                       // 바깥 마름모 밖(외곽 상자 왼쪽 위)
        t.AddEnemy(new Vector2(5, 5), life: LifeState.Entering);   // 입장 중은 맵 밖이어도 됨
        t.AddEnemy(t.CellCenter(6, 6));                      // 기둥 안
        var ghost = t.AddEnemy(t.CellCenter(6, 6));
        ghost.PassThroughObstacles = true;                   // 통과 유닛은 안 셈
        c.Sample();
        var r = t.World.Invariants;
        Assert.Equal(1, r.Nan);
        Assert.Equal(1, r.OutOfMap);
        Assert.Equal(1, r.InObstacle);
        Assert.False(r.Passed);
        c.Sample();
        Assert.Equal(2, r.InObstacleLongest);   // 같은 적이 두 표본(1초 넘게) 이어짐
    }

    [Fact]
    public void Counts_StuckAfterThreeSeconds_NotNearHero()
    {
        var (t, c) = Make();
        var far = t.AddEnemy(t.Hero.Pos + new Vector2(300, 0));
        far.MoveSpeed = 0f;                      // 쫓는데 못 움직임
        var near = t.AddEnemy(t.Hero.Pos + new Vector2(30, 0));
        near.MoveSpeed = 0f;                     // 주인공 곁 — 사거리 안이라 안 셈
        for (var i = 0; i < 4; i++) c.Sample();
        Assert.Equal(1, t.World.Invariants.Stuck);
        Assert.Equal(1, t.World.Invariants.StuckMax);
        Assert.False(t.World.Invariants.Passed);   // 1 > 2 × 2%
    }

    [Fact]
    public void Counts_OverCap_AndPassesWhenClean()
    {
        var (t, c) = Make();
        c.Sample();
        Assert.True(t.World.Invariants.Passed);
        for (var i = 0; i < 361; i++)
        {
            var e = t.AddEnemy(t.Hero.Pos + new Vector2(40 + i % 20 * 3, i / 20 * 3));
            e.Intent = MoveIntent.Hold;
        }
        c.Sample();
        Assert.Equal(1, t.World.Invariants.OverCap);
    }

    [Fact]
    public void Occluded_RealMap_CountsNormalEnemiesBehindRoomWalls()
    {
        // 실제 cathedral_arena(안쪽 방 벽 = 시야 막힘): 반경 R 안인데 안 보이는 일반 적만 센다. 입장 중·보이는 적은 안 셈
        var t = Data.RealData.CathedralWorld();
        var c = new InvariantChecker(t.World, new NavWorld(t.World.Arena, t.World.MapSize));
        t.Step();   // 첫 BeginTick 이 시야 계산
        var vis = t.World.Visibility;
        var g = t.World.Arena!;
        Vector2? hidden = null, seen = null;
        for (var ty = 0; ty < g.Size && (hidden is null || seen is null); ty++)
            for (var tx = 0; tx < g.Size; tx++)
            {
                if (!g.IsWalkable(tx, ty)) continue;
                var p = g.Iso.CellCenter(tx, ty);
                if (Vector2.Distance(p, vis.HeroPos) > vis.Radius - 10f) continue;
                if (!vis.IsVisible(p)) hidden ??= p;
                else seen ??= p;
            }
        Assert.NotNull(hidden);
        Assert.NotNull(seen);
        t.AddEnemy(hidden!.Value).Intent = MoveIntent.Hold;
        t.AddEnemy(hidden.Value, life: LifeState.Entering).Intent = MoveIntent.Hold;
        t.AddEnemy(seen!.Value).Intent = MoveIntent.Hold;
        c.Sample();
        Assert.Equal(1, t.World.Invariants.Occluded);
    }

    [Fact]
    public void Report_AddToAndFormat()
    {
        var a = new InvariantReport { Samples = 2, Nan = 1, StuckMax = 2, StuckMaxAlive = 100, Occluded = 3, InObstacleLongest = 1 };
        var b = new InvariantReport { Samples = 3, StuckMax = 1, StuckMaxAlive = 50, Occluded = 4, InObstacleLongest = 2 };
        var total = new InvariantReport();
        a.AddTo(total);
        b.AddTo(total);
        Assert.Equal(5, total.Samples);
        Assert.Equal(1, total.Nan);
        Assert.Equal(7, total.Occluded);
        Assert.Equal(2, total.InObstacleLongest);
        Assert.Equal((2, 100), (total.StuckMax, total.StuckMaxAlive));
        Assert.StartsWith("INVARIANTS samples=5 nan=1", total.Format(new FairnessCounters()), StringComparison.Ordinal);
    }

    [Fact]
    public void SpatialRebuild_SamplesEvery60Ticks_WhenCheckOn()
    {
        var ctx = new ModuleContext();
        new NavModule().Register(ctx);
        var t = new TestWorldBuilder().WithModules(ctx).Build();
        ctx.ApplyRunSetups(t.World, RunOptions.Default with { Check = true });
        t.Step(121);
        Assert.Equal(3, t.World.Invariants.Samples);   // 틱 0·60·120
        var off = new TestWorldBuilder().WithModules(ctx).Build();
        off.Step(121);
        Assert.Equal(0, off.World.Invariants.Samples);
    }
}
