using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Nav.Validation;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary>
/// 실제 <c>cathedral_arena</c>(WP0a)의 길 검사·전수 성질 — 자원 없이 (설계서 10.1 <c>Nav</c> (a)~(d), 11.1 WP2 완료 기준).
/// 스테이지 2 맵은 단계 3 전까지 없다(합성 맵으로 대신 — <see cref="SyntheticMapNavTests"/>).
/// </summary>
public class RealMapNavTests
{
    private static readonly Lazy<NavWorld> Nav = new(() => new NavWorld(ArenaLayout.Expand(RealData.Cathedral), default));

    [Fact]
    public void PathChecks_RealMap_NoErrors()
    {
        // (d) 13.5-4~6 + 문 틈 닿음(방 맵이면) — 원판과 거울판
        var errors = NavPathValidator.Check(RealData.Cathedral);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        if (RealData.Cathedral.Mirrored is { } m)
        {
            var mirrored = NavPathValidator.Check(m);
            Assert.True(mirrored.Count == 0, string.Join("\n", mirrored));
        }
    }

    [Fact]
    public void PathChecks_RealMapWithRoomKinds_GateReachPasses()
    {
        // 실제 맵에 room_kinds 를 단 사본(13.5-23 ③ 대상)이 문 틈 닿음까지 통과
        var map = RealData.Cathedral with { RoomKinds = [RoomKind.Normal] };
        var errors = NavPathValidator.Check(map);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void FlowField_FollowDir_ReachesStartWithoutLoops()
    {
        // (a) 모든 통과 칸에서 dir 을 따라가면 칸 수 이하 걸음에 시작점(dist 0)
        NavExhaustive.FollowDirReachesStart(Nav.Value, ResolvedMap.Resolve(RealData.Cathedral).HeroStart);
    }

    [Fact]
    public void FlowField_PointUnits_ReachHeroWithin10s()
    {
        // (b) 모든 통과 칸 중심의 점 유닛이 이중선형 방향으로 주인공 120px 안에 온다(맴돌지 않음).
        // 시간 한계: 10초(설계 문장) — 단 2560px 맵의 먼 칸은 길이 1,300px 를 넘어 70px/s 로 10초 안에 못 온다.
        // 그래서 한계 = max(10초, 필드 길 길이 × 1.5 / 속도 + 2초)(WP2 구현 기록)
        var nav = Nav.Value;
        var failed = NavExhaustive.PointUnitsFailing(nav, ResolvedMap.Resolve(RealData.Cathedral).HeroStart);
        Assert.True(failed.Count == 0, $"{failed.Count}칸이 시간 한계 안에 못 옴(첫 칸 중심 {(failed.Count > 0 ? nav.Grid.Center(failed[0]) : default)})");
    }

    [Fact]
    public void FlowField_HeroAtCornersPillarsAndRooms_AllPassableReached()
    {
        // (c) 주인공을 마름모 네 꼭짓점 쪽·기둥 옆·안쪽 방 기준점에 둬도 S·M 필드가 모든 통과 칸에서 dist < 65535(같은 연결 성분)
        var map = RealData.Cathedral;
        var spots = NavExhaustive.HeroSpots(map);
        Assert.True(spots.Count > 4);   // 방 기준점·기둥이 실제로 들어갔다
        NavExhaustive.AllSpotsReachSameCells(Nav.Value, ResolvedMap.Resolve(map).HeroStart, spots);
    }

    [Fact]
    public void RealMap_GrateCells_WalkOnly()
    {
        // 창살 w 칸: 걷기 도형만(투사체 통과)
        var nav = Nav.Value;
        var grid = nav.Arena;
        var walkOnly = nav.Collision.Where(s => s.BlocksWalk && !s.BlocksProjectiles).ToList();
        Assert.NotEmpty(walkOnly);
        foreach (var s in walkOnly)
            for (var ty = s.Tiles.Y0; ty < s.Tiles.Y1; ty++)
                for (var tx = s.Tiles.X0; tx < s.Tiles.X1; tx++)
                    Assert.Equal('w', grid.Prop(tx, ty));
        // 창살 칸 하나를 ty 축으로 가로지르는 선(위·아래 이웃 칸은 바닥): 투사체는 지나고 걷기 길은 막힘(창살 너머 궁수 — 10.1 공정성)
        var w = walkOnly[0].Tiles;
        var (gx, gy) = (w.X0, w.Y0);
        Assert.False(grid.BlocksProjectiles(gx, gy - 1) || grid.BlocksProjectiles(gx, gy + 1));
        var a = grid.Iso.CellCenter(gx, gy - 1);
        var b = grid.Iso.CellCenter(gx, gy + 1);
        Assert.True(nav.ProjectileLineClear(a, b));
        Assert.False(nav.HasClearLine(a, b, 1f));
    }

    [Fact]
    public void RoomEnemies_FlowOutThroughDoors_ToHero()
    {
        // 안쪽 방 기준점마다 작은 몸(12)·큰 몸(24 — M 필드) 적을 두면 문으로 나와 주인공에게 온다(갇힌 바닥 칸 없음의 실제 결과, 6.12 끝). 불변식 기준 통과
        var t = Movement.MovementPerfTests.CathedralNavWorld();
        var basic = t.World.Defs;
        var big = TestDefs.Enemy(basic.SpriteSets[0], "test_big", radius: 24f);
        var anchors = ResolvedMap.Resolve(RealData.Cathedral).Anchors
            .Where(a => a.Key.StartsWith("room_", StringComparison.Ordinal)).OrderBy(a => a.Key, StringComparer.Ordinal).ToList();
        Assert.Equal(4, anchors.Count);
        var list = new List<Enemy>();
        foreach (var (_, at) in anchors)
        {
            var small = t.AddEnemy(at);
            small.Intent = MoveIntent.SeekHero();
            var large = t.AddEnemy(big, at + new Vector2(0, 30));
            large.Intent = MoveIntent.SeekHero();
            list.Add(small);
            list.Add(large);
        }
        var checker = new Core.Sim.Movement.Invariants.InvariantChecker(t.World, (NavWorld)t.World.Nav) { Enabled = true };
        for (var i = 0; i < 60 * 40; i++)
        {
            t.Step();
            if (i % 60 == 0) checker.Sample();
        }
        foreach (var e in list) Assert.True(Vector2.Distance(e.Pos, t.Hero.Pos) < 120f, $"{e.Def.Id} {e.Pos} → 주인공 {t.Hero.Pos}");
        Assert.True(t.World.Invariants.Passed, t.World.Invariants.Format(t.World.Fairness));
    }
}
