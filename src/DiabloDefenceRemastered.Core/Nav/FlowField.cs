namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 무리용 플로우 필드 한 벌 (설계서 4.3, 소유 WP2). 목표 = 주인공 둘레 (주인공 반경 + 16)px 안의 통과 칸 전부(거리 0)인 다중 시작 다익스트라.
/// 그 반경 안에 통과 칸이 없으면 주인공 칸에서 BFS 로 찾은 가장 가까운 통과 칸들(같은 걸음 수 전부). 8방향, 비용 10/14, 코너 자르기 금지.
/// 결과 <see cref="Dist"/>(65535 = 못 감)·<see cref="Dir"/>(가장 작은 이웃 방향, 255 = 없음 — 시작점·못 가는 칸). 한 틱 안에서 동기로 끝나므로 제자리에서 다시 쓴다.
/// </summary>
public sealed class FlowField
{
    public const byte NoDir = 255;
    /// <summary>보간 기울기를 믿는 자기 칸 방향과의 각도 한계(cos 60°).</summary>
    public const float SaddleCos = 0.5f;

    private readonly NavGrid _grid;
    private readonly IndexedMinHeap _heap;
    private readonly int[] _queue;
    private readonly int[] _bfsStamp;
    private int _bfsCurrent;

    internal FlowField(NavGrid grid, float threshold)
    {
        _grid = grid;
        Threshold = threshold;
        Dist = new ushort[grid.Count];
        Dir = new byte[grid.Count];
        _heap = new IndexedMinHeap(grid.Count);
        _queue = new int[grid.Count];
        _bfsStamp = new int[grid.Count];
        Array.Fill(Dist, NavConst.Unreachable);
        Array.Fill(Dir, NoDir);
    }

    /// <summary>통과 문턱(clearance ≥ 이 값).</summary>
    public float Threshold { get; }
    public ushort[] Dist { get; }
    public byte[] Dir { get; }
    /// <summary>마지막 계산 때 주인공 칸(−1 = 아직).</summary>
    public int GoalCell { get; private set; } = -1;
    /// <summary>계산 횟수(시험 계측).</summary>
    public int Computations { get; private set; }
    /// <summary>마지막 계산의 시작점 수.</summary>
    public int StartCount { get; private set; }

    public bool Passable(int cell) => _grid.Clear[cell] >= Threshold;

    /// <summary>주인공 위치·반경으로 다시 계산한다.</summary>
    public void Compute(Vector2 heroPos, float heroRadius)
    {
        var g = _grid;
        Array.Fill(Dist, NavConst.Unreachable);
        Array.Fill(Dir, NoDir);
        _heap.Clear();
        Computations++;
        GoalCell = g.ClampedIndex(heroPos);

        // 시작점: 주인공 둘레 (반경 + 16)px 안 통과 칸
        var reach = heroRadius + NavConst.FieldGoalMargin;
        var c = g.CellOf(heroPos);
        var span = (int)MathF.Ceiling(reach / g.CellSize) + 1;
        var starts = 0;
        for (var cy = c.Y - span; cy <= c.Y + span; cy++)
            for (var cx = c.X - span; cx <= c.X + span; cx++)
            {
                if (!g.InGrid(cx, cy)) continue;
                var i = g.Index(cx, cy);
                if (!Passable(i) || Vector2.DistanceSquared(g.Center(i), heroPos) > reach * reach) continue;
                Dist[i] = 0;
                _heap.Push(i, 0f);
                starts++;
            }
        if (starts == 0) starts = SeedNearest(GoalCell);
        StartCount = starts;

        while (_heap.Count > 0)
        {
            var cur = _heap.Pop();
            var cx = cur % g.Width;
            var cy = cur / g.Width;
            var dc = Dist[cur];
            for (var d = 0; d < 8; d++)
            {
                if (!g.CanStep(cx, cy, d, Threshold)) continue;
                var ni = g.Index(cx + NavGrid.Dx[d], cy + NavGrid.Dy[d]);
                var nd = dc + ((d & 1) == 0 ? NavConst.StraightCost : NavConst.DiagonalCost);
                if (nd >= NavConst.Unreachable || nd >= Dist[ni]) continue;
                Dist[ni] = (ushort)nd;
                _heap.Push(ni, nd);
            }
        }

        // 방향: 갈 수 있는 이웃 중 거리가 가장 작은 쪽(같으면 번호 작은 방향)
        for (var i = 0; i < Dist.Length; i++)
        {
            var di = Dist[i];
            if (di == 0 || di == NavConst.Unreachable) continue;
            var cx = i % g.Width;
            var cy = i / g.Width;
            var best = di;
            var bestDir = NoDir;
            for (var d = 0; d < 8; d++)
            {
                if (!g.CanStep(cx, cy, d, Threshold)) continue;
                var nd = Dist[g.Index(cx + NavGrid.Dx[d], cy + NavGrid.Dy[d])];
                if (nd < best)
                {
                    best = nd;
                    bestDir = (byte)d;
                }
            }
            Dir[i] = bestDir;
        }
    }

    /// <summary>주인공 칸에서 4이웃 BFS 로 가장 가까운 통과 칸들(같은 걸음 수 전부)을 시작점으로.</summary>
    private int SeedNearest(int from)
    {
        var g = _grid;
        _bfsCurrent++;
        var head = 0;
        var tail = 0;
        _queue[tail++] = from;
        _bfsStamp[from] = _bfsCurrent;
        var found = 0;
        while (head < tail && found == 0)
        {
            // 한 걸음 수(줄)씩: 그 줄에서 통과 칸을 찾으면 그 줄까지만
            var levelEnd = tail;
            for (; head < levelEnd; head++)
            {
                var cur = _queue[head];
                if (Passable(cur))
                {
                    Dist[cur] = 0;
                    _heap.Push(cur, 0f);
                    found++;
                    continue;
                }
                var cx = cur % g.Width;
                var cy = cur / g.Width;
                for (var d = 0; d < 8; d += 2)
                {
                    var nx = cx + NavGrid.Dx[d];
                    var ny = cy + NavGrid.Dy[d];
                    if (!g.InGrid(nx, ny)) continue;
                    var ni = g.Index(nx, ny);
                    if (_bfsStamp[ni] == _bfsCurrent) continue;
                    _bfsStamp[ni] = _bfsCurrent;
                    _queue[tail++] = ni;
                }
            }
        }
        return found;
    }

    /// <summary>
    /// 자리 p 에서 갈 방향(4.3 "방향 읽기"): 둘러싼 4칸의 dist 를 이중선형 보간한 기울기의 반대. 4칸 중 못 가는 칸이 섞이면 자기 칸의 <see cref="Dir"/>.
    /// 자기 칸이 시작점이면 <paramref name="toHero"/> 쪽, 못 가는 칸이면 0(부르는 쪽이 clearance 기울기로 빠져나온다).
    /// </summary>
    public Vector2 Direction(Vector2 p, Vector2 toHero)
    {
        var g = _grid;
        var own = g.ClampedIndex(p);
        var od = Dist[own];
        if (od == NavConst.Unreachable) return Vector2.Zero;
        if (od == 0) return MathX.NormalizeOr(toHero, Vector2.Zero);

        var fx = p.X / g.CellSize - 0.5f;
        var fy = p.Y / g.CellSize - 0.5f;
        var x0 = (int)MathF.Floor(fx);
        var y0 = (int)MathF.Floor(fy);
        if (g.InGrid(x0, y0) && g.InGrid(x0 + 1, y0 + 1))
        {
            var a = Dist[g.Index(x0, y0)];
            var b = Dist[g.Index(x0 + 1, y0)];
            var c = Dist[g.Index(x0, y0 + 1)];
            var d = Dist[g.Index(x0 + 1, y0 + 1)];
            if (a != NavConst.Unreachable && b != NavConst.Unreachable && c != NavConst.Unreachable && d != NavConst.Unreachable)
            {
                var tx = fx - x0;
                var ty = fy - y0;
                var gx = MathX.Lerp(b - a, d - c, ty);
                var gy = MathX.Lerp(c - a, d - b, tx);
                var grad = new Vector2(-gx, -gy);
                var ownDir = DirVector(Dir[own]);
                // 벽 모서리의 안장(saddle)에서는 보간 기울기가 자기 칸 방향과 엇갈려 제자리를 맴돈다 — 60° 넘게 엇갈리면 자기 칸 방향
                if (grad.LengthSquared() > 1e-6f)
                {
                    grad = Vector2.Normalize(grad);
                    if (ownDir == Vector2.Zero || Vector2.Dot(grad, ownDir) >= SaddleCos) return grad;
                }
            }
        }
        return DirVector(Dir[own]);
    }

    /// <summary>방향 번호 → 단위 벡터(255 → 0).</summary>
    public static Vector2 DirVector(byte dir) =>
        dir == NoDir ? Vector2.Zero : Vector2.Normalize(new Vector2(NavGrid.Dx[dir], NavGrid.Dy[dir]));
}
