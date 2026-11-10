using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary>플로우 필드·A* 를 손 격자·무작위 격자로 (설계서 4.3·4.4, 10.1 <c>Nav</c>).</summary>
public class GridSearchTests
{
    private const float Open = 100f, Wall = -1f;

    /// <summary>문자열 격자: '#' 막힘, 그 밖 통과.</summary>
    private static NavGrid Grid(params string[] rows)
    {
        var h = rows.Length;
        var w = rows[0].Length;
        var c = new float[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++) c[y * w + x] = rows[y][x] == '#' ? Wall : Open;
        return new NavGrid(w, h, c);
    }

    [Fact]
    public void FlowField_HandGrid_DirectionsAndNoCornerCut()
    {
        // 주인공은 (0,0) 칸. (2,1) 은 위가 벽이라 대각선 (1,0) 쪽으로 못 가고 왼쪽으로
        var g = Grid(
            "..#..",
            "...#.",
            ".....");
        var f = new FlowField(g, 1f);
        f.Compute(new Vector2(4, 4), 0f);   // 칸 (0,0) 안, 이웃 칸 중심은 16px 넘게
        Assert.Equal(0, f.Dist[g.Index(0, 0)]);
        Assert.Equal(10, f.Dist[g.Index(1, 0)]);
        Assert.Equal(14, f.Dist[g.Index(1, 1)]);
        Assert.Equal(4, f.Dir[g.Index(1, 0)]);                       // 서
        Assert.Equal(5, f.Dir[g.Index(1, 1)]);                       // 북서(대각선, 두 옆 칸 열림)
        Assert.Equal(4, f.Dir[g.Index(2, 1)]);                       // (1,0) 대각선은 (2,0) 벽이라 금지 → 서
        Assert.Equal(NavConst.Unreachable, f.Dist[g.Index(2, 0)]);
        // (4,0) 은 (4,1) 로만 — 대각선 (3,1) 은 벽
        Assert.Equal(2, f.Dir[g.Index(4, 0)]);                       // 남
        Assert.Equal(1, f.StartCount);
    }

    [Fact]
    public void FlowField_NoStartWithinRadius_SeedsNearestPassable()
    {
        // 주인공이 벽 칸 안 — 반경 안 통과 칸이 없으면 BFS 로 가장 가까운 통과 칸들이 시작점
        var g = Grid(
            "#####",
            "##.##",
            "#####",
            "....#");
        var f = new FlowField(g, 1f);
        f.Compute(new Vector2(40, 4), 0f);
        Assert.Equal(1, f.StartCount);
        Assert.Equal(0, f.Dist[g.Index(2, 1)]);
        Assert.Equal(NavConst.Unreachable, f.Dist[g.Index(0, 3)]);   // 막힌 칸 너머는 못 감
    }

    [Fact]
    public void AStar_RandomGrids_OptimalVsDijkstra()
    {
        var rng = new Random(12345);
        for (var n = 0; n < 200; n++)
        {
            var (w, h) = (rng.Next(6, 28), rng.Next(6, 28));
            var c = new float[w * h];
            var wallP = rng.NextDouble() * 0.35;
            for (var i = 0; i < c.Length; i++) c[i] = rng.NextDouble() < wallP ? Wall : Open;
            var g = new NavGrid(w, h, c);
            var free = Enumerable.Range(0, c.Length).Where(i => c[i] > 0).ToList();
            if (free.Count < 2) continue;
            var s = free[rng.Next(free.Count)];
            var t = free[rng.Next(free.Count)];
            var astar = new GridAStar(g);
            var found = astar.Search(s, t, 1f, float.PositiveInfinity, int.MaxValue, out var cost, out _);
            var reference = Dijkstra(g, s, t);
            Assert.Equal(float.IsFinite(reference), found);
            if (!found) continue;
            Assert.True(MathF.Abs(cost - reference) < 1e-3f, $"격자 {n}: A* {cost} ≠ 다익스트라 {reference}");
            // 끈 당기기 전 경로: 이웃끼리 한 걸음, 대각선은 두 옆 칸 열림, 길이 = 비용
            var path = new List<int>();
            astar.PathTo(t, path);
            Assert.Equal(s, path[0]);
            Assert.Equal(t, path[^1]);
            var len = 0f;
            for (var i = 1; i < path.Count; i++)
            {
                var (ax, ay, bx, by) = (path[i - 1] % w, path[i - 1] / w, path[i] % w, path[i] / w);
                var dir = Enumerable.Range(0, 8).Single(k => NavGrid.Dx[k] == bx - ax && NavGrid.Dy[k] == by - ay);
                Assert.True(g.CanStep(ax, ay, dir, 1f));
                len += (dir & 1) == 0 ? 1f : GridAStar.Sqrt2;
            }
            Assert.True(MathF.Abs(len - cost) < 1e-3f);
        }
    }

    [Fact]
    public void AStar_Unreachable_ReturnsClosestByHeuristic()
    {
        var g = Grid(
            ".....#...",
            ".....#...",
            ".....#...");
        var astar = new GridAStar(g);
        Assert.False(astar.Search(g.Index(0, 1), g.Index(8, 1), 1f, float.PositiveInfinity, int.MaxValue, out _, out var closest));
        Assert.Equal(4, closest % g.Width);   // 벽 바로 앞 열
    }

    [Fact]
    public void AStar_NearestPassable_WithinThreeCells()
    {
        var g = Grid(
            "........",
            ".######.",
            ".######.",
            ".######.",
            ".######.",
            "........");
        var astar = new GridAStar(g);
        Assert.Equal(g.Index(0, 2), astar.NearestPassable(g.Index(1, 2), 1f, 3));
        Assert.True(astar.NearestPassable(g.Index(3, 2), 1f, 3) >= 0);
        var deep = Grid(
            "#########",
            "#########",
            "#########",
            "#########",
            "#########",
            "#########",
            "#########",
            "#########",
            "........#");
        Assert.Equal(-1, new GridAStar(deep).NearestPassable(deep.Index(4, 3), 1f, 3));
    }

    private static float Dijkstra(NavGrid g, int s, int t)
    {
        var dist = Enumerable.Repeat(float.PositiveInfinity, g.Count).ToArray();
        var pq = new PriorityQueue<int, float>();
        dist[s] = 0;
        pq.Enqueue(s, 0);
        while (pq.TryDequeue(out var cur, out var d))
        {
            if (d > dist[cur]) continue;
            var (cx, cy) = (cur % g.Width, cur / g.Width);
            for (var k = 0; k < 8; k++)
            {
                if (!g.CanStep(cx, cy, k, 1f)) continue;
                var ni = g.Index(cx + NavGrid.Dx[k], cy + NavGrid.Dy[k]);
                var nd = d + ((k & 1) == 0 ? 1f : GridAStar.Sqrt2);
                if (nd < dist[ni] - 1e-6f)
                {
                    dist[ni] = nd;
                    pq.Enqueue(ni, nd);
                }
            }
        }
        return dist[t];
    }
}
