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
        var nav = Nav.Value;
        var hero = ResolvedMap.Resolve(RealData.Cathedral).HeroStart;
        foreach (var field in FieldsAt(nav, hero))
        {
            var g = nav.Grid;
            var passCount = 0;
            for (var i = 0; i < g.Count; i++)
                if (field.Dist[i] != NavConst.Unreachable) passCount++;
            for (var i = 0; i < g.Count; i++)
            {
                if (field.Dist[i] == NavConst.Unreachable) continue;
                var cur = i;
                var steps = 0;
                while (field.Dist[cur] != 0)
                {
                    var d = field.Dir[cur];
                    Assert.NotEqual(FlowField.NoDir, d);
                    cur = g.Index(cur % g.Width + NavGrid.Dx[d], cur / g.Width + NavGrid.Dy[d]);
                    Assert.True(++steps <= passCount, $"칸 {i} 에서 고리");
                }
            }
        }
    }

    [Fact]
    public void FlowField_PointUnits_ReachHeroWithin10s()
    {
        // (b) 모든 통과 칸 중심의 점 유닛이 이중선형 방향으로 주인공 120px 안에 온다(맴돌지 않음).
        // 시간 한계: 10초(설계 문장) — 단 2560px 맵의 먼 칸은 길이 1,300px 를 넘어 70px/s 로 10초 안에 못 온다.
        // 그래서 한계 = max(10초, 필드 길 길이 × 1.5 / 속도 + 2초)(WP2 구현 기록)
        var nav = Nav.Value;
        var hero = ResolvedMap.Resolve(RealData.Cathedral).HeroStart;
        var field = FieldsAt(nav, hero)[0];
        var g = nav.Grid;
        const float speed = 70f;
        var failed = new List<int>();
        for (var i = 0; i < g.Count; i++)
        {
            if (field.Dist[i] == NavConst.Unreachable) continue;
            var p = g.Center(i);
            var ok = false;
            var pathPx = field.Dist[i] * (g.CellSize / (float)NavConst.StraightCost);
            var limit = (int)(MathF.Max(10f, pathPx * 1.5f / speed + 2f) * World.TicksPerSecond);
            for (var t = 0; t < limit; t++)
            {
                if (Vector2.Distance(p, hero) <= 120f) { ok = true; break; }
                var dir = field.Direction(p, hero - p);
                if (dir == Vector2.Zero) dir = MathX.NormalizeOr(nav.Grid.Gradient(p), Vector2.Zero);
                p += dir * speed * World.Dt;
            }
            if (!ok) failed.Add(i);
        }
        Assert.True(failed.Count == 0, $"{failed.Count}칸이 시간 한계 안에 못 옴(첫 칸 중심 {(failed.Count > 0 ? g.Center(failed[0]) : default)})");
    }

    [Fact]
    public void FlowField_HeroAtCornersPillarsAndRooms_AllPassableReached()
    {
        // (c) 주인공을 마름모 네 꼭짓점 쪽·기둥 옆·안쪽 방 기준점에 둬도 S·M 필드가 모든 통과 칸에서 dist < 65535(같은 연결 성분)
        var nav = Nav.Value;
        var map = RealData.Cathedral;
        var r = ResolvedMap.Resolve(map);
        var iso = r.Iso;
        var n = iso.N;
        var spots = new List<Vector2>
        {
            iso.ToWorld(new TileCoord(3, 3)), iso.ToWorld(new TileCoord(n - 4, 3)), iso.ToWorld(new TileCoord(3, n - 4)), iso.ToWorld(new TileCoord(n - 4, n - 4)),
        };
        spots.AddRange(r.Anchors.Where(a => a.Key.StartsWith("room_", StringComparison.Ordinal)).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => a.Value));
        // 기둥 옆: open_center 기둥 칸 오른쪽 아래 두 칸
        var grid = ArenaLayout.Expand(map);
        for (var my = 0; my < grid.MegaSize; my++)
            for (var mx = 0; mx < grid.MegaSize; mx++)
                if (grid.MegaTile(mx, my) == RealData.L1.Pieces["pillar"]) spots.Add(iso.ToWorld(new TileCoord(mx * 2 + 2, my * 2 + 1)));

        var reference = FieldsAt(nav, r.HeroStart).Select(f => f.Dist.Select(d => d != NavConst.Unreachable).ToArray()).ToArray();
        foreach (var spot in spots)
        {
            var fields = FieldsAt(nav, spot);
            for (var k = 0; k < fields.Length; k++)
                for (var i = 0; i < nav.Grid.Count; i++)
                    if (reference[k][i]) Assert.True(fields[k].Dist[i] != NavConst.Unreachable, $"필드 {k} 주인공 {spot}: 칸 {nav.Grid.Center(i)} 못 감");
        }
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
    }

    internal static FlowField[] FieldsAt(NavWorld nav, Vector2 hero)
    {
        var s = new FlowField(nav.Grid, NavConst.FieldS);
        var m = new FlowField(nav.Grid, NavConst.FieldM);
        s.Compute(hero, NavConst.HeroCheckRadius);
        m.Compute(hero, NavConst.HeroCheckRadius);
        return [s, m];
    }
}
