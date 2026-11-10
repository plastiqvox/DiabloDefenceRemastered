using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary>
/// 설계서 10.1 <c>Nav</c> 전수 시험 (a)~(c) 를 맵 하나에 — 실제 맵(<see cref="RealMapNavTests"/>)과 작은 합성 방 맵(<see cref="SmallRoomNavTests"/>)이 같이 쓴다.
/// (d) 13.5-4~6 은 <c>NavPathValidator.Check</c> 그대로.
/// </summary>
internal static class NavExhaustive
{
    /// <summary>(b) 점 유닛 속도(px/s) — 해골 걸음.</summary>
    public const float PointSpeed = 70f;

    /// <summary>(a) 모든 통과 칸에서 <c>dir</c> 을 따라가면 칸 수 이하 걸음에 시작점(dist 0). S·M 필드 둘 다.</summary>
    public static void FollowDirReachesStart(NavWorld nav, Vector2 hero)
    {
        var g = nav.Grid;
        foreach (var field in FieldsAt(nav, hero))
        {
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

    /// <summary>
    /// (b) 모든 통과 칸 중심의 점 유닛이 이중선형 방향으로 주인공 120px 안에 온다(맴돌지 않음).
    /// 시간 한계 = max(10초, 필드 길 길이 × 1.5 / 속도 + 2초)(WP2 구현 기록 — 큰 맵의 먼 칸은 10초에 못 온다). 못 온 칸 목록을 낸다.
    /// </summary>
    public static List<int> PointUnitsFailing(NavWorld nav, Vector2 hero)
    {
        var field = FieldsAt(nav, hero)[0];
        var g = nav.Grid;
        var failed = new List<int>();
        for (var i = 0; i < g.Count; i++)
        {
            if (field.Dist[i] == NavConst.Unreachable) continue;
            var p = g.Center(i);
            var ok = false;
            var pathPx = field.Dist[i] * (g.CellSize / (float)NavConst.StraightCost);
            var limit = (int)(MathF.Max(10f, pathPx * 1.5f / PointSpeed + 2f) * World.TicksPerSecond);
            for (var t = 0; t < limit; t++)
            {
                if (Vector2.Distance(p, hero) <= 120f) { ok = true; break; }
                var dir = field.Direction(p, hero - p);
                if (dir == Vector2.Zero) dir = MathX.NormalizeOr(nav.Grid.Gradient(p), Vector2.Zero);
                p += dir * PointSpeed * World.Dt;
            }
            if (!ok) failed.Add(i);
        }
        return failed;
    }

    /// <summary>(c) 의 주인공 자리: 마름모 네 꼭짓점 쪽(3칸 안), 모든 <c>room_*</c> 기준점, 기둥 메가마다 그 오른쪽 아래 두 칸.</summary>
    public static List<Vector2> HeroSpots(MapDef map)
    {
        var r = ResolvedMap.Resolve(map);
        var iso = r.Iso;
        var n = iso.N;
        var spots = new List<Vector2>
        {
            iso.ToWorld(new TileCoord(3, 3)), iso.ToWorld(new TileCoord(n - 4, 3)), iso.ToWorld(new TileCoord(3, n - 4)), iso.ToWorld(new TileCoord(n - 4, n - 4)),
        };
        spots.AddRange(r.Anchors.Where(a => a.Key.StartsWith("room_", StringComparison.Ordinal)).OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => a.Value));
        if (map.Tileset.Def.Pieces.TryGetValue("pillar", out var pillar))
        {
            var grid = ArenaLayout.Expand(map);
            for (var my = 0; my < grid.MegaSize; my++)
                for (var mx = 0; mx < grid.MegaSize; mx++)
                    if (grid.MegaTile(mx, my) == pillar) spots.Add(iso.ToWorld(new TileCoord(mx * 2 + 2, my * 2 + 1)));
        }
        return spots;
    }

    /// <summary>(c) 주인공을 <paramref name="spots"/> 에 둬도 S·M 필드가 주인공 시작 자리 기준 통과 칸 모두에서 dist &lt; 65535(같은 연결 성분).</summary>
    public static void AllSpotsReachSameCells(NavWorld nav, Vector2 heroStart, IEnumerable<Vector2> spots)
    {
        var reference = FieldsAt(nav, heroStart).Select(f => f.Dist.Select(d => d != NavConst.Unreachable).ToArray()).ToArray();
        foreach (var spot in spots)
        {
            var fields = FieldsAt(nav, spot);
            for (var k = 0; k < fields.Length; k++)
                for (var i = 0; i < nav.Grid.Count; i++)
                    if (reference[k][i]) Assert.True(fields[k].Dist[i] != NavConst.Unreachable, $"필드 {k} 주인공 {spot}: 칸 {nav.Grid.Center(i)} 못 감");
        }
    }

    /// <summary>주인공 자리 하나로 S·M 필드를 새로 계산한다(주인공 반경 = <see cref="NavConst.HeroCheckRadius"/>).</summary>
    public static FlowField[] FieldsAt(NavWorld nav, Vector2 hero)
    {
        var s = new FlowField(nav.Grid, NavConst.FieldS);
        var m = new FlowField(nav.Grid, NavConst.FieldM);
        s.Compute(hero, NavConst.HeroCheckRadius);
        m.Compute(hero, NavConst.HeroCheckRadius);
        return [s, m];
    }
}
