namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 16px 격자 A* (설계서 4.4, 소유 WP2). 통과 = clearance ≥ 반경, 8방향, 비용 1 / √2(칸 단위 실수), 휴리스틱 옥타일 거리(허용적), 같은 f 면 h 가 작은 쪽.
/// 대각선은 두 직교 이웃이 통과 가능할 때만(코너 자르기 금지). 칸별 g·parent·stamp 배열을 한 번 잡고 탐색 번호로 "아직 안 봄" 을 판정한다(할당 0).
/// </summary>
public sealed class GridAStar
{
    public const float Sqrt2 = 1.41421356f;

    private readonly NavGrid _grid;
    private readonly float[] _g;
    private readonly int[] _parent;
    private readonly int[] _stamp;
    private readonly bool[] _closed;
    private readonly IndexedMinHeap _open;
    private int _current;

    public GridAStar(NavGrid grid)
    {
        _grid = grid;
        _g = new float[grid.Count];
        _parent = new int[grid.Count];
        _stamp = new int[grid.Count];
        _closed = new bool[grid.Count];
        _open = new IndexedMinHeap(grid.Count);
    }

    /// <summary>마지막 탐색의 펼친 칸 수.</summary>
    public int Expanded { get; private set; }

    /// <summary>
    /// start → goal 탐색. 목표에 닿으면 true 와 그 비용(칸 단위). 못 닿으면(막힘·<paramref name="maxCost"/> 넘음·펼침 상한) false 이고
    /// <paramref name="closest"/> = 탐색 중 h 가 가장 작았던 칸(4.4 "갈 수 있는 가장 가까운 곳"). 시작 칸은 통과 가능해야 한다(부르는 쪽이 <see cref="NearestPassable"/>).
    /// </summary>
    public bool Search(int start, int goal, float radius, float maxCost, int expandLimit, out float cost, out int closest)
    {
        var g = _grid;
        _current++;
        if (_current == int.MaxValue)
        {
            Array.Clear(_stamp);
            _current = 1;
        }
        _open.Clear();
        Expanded = 0;
        cost = float.PositiveInfinity;
        var gx = goal % g.Width;
        var gy = goal / g.Width;
        Touch(start);
        _g[start] = 0f;
        _parent[start] = -1;
        var h0 = Heuristic(start % g.Width, start / g.Width, gx, gy);
        _open.Push(start, h0, h0);
        closest = start;
        var closestH = h0;

        while (_open.Count > 0)
        {
            var cur = _open.Pop();
            if (_closed[cur]) continue;
            _closed[cur] = true;
            Expanded++;
            if (cur == goal)
            {
                cost = _g[cur];
                closest = cur;
                return true;
            }
            if (Expanded > expandLimit) break;
            var cx = cur % g.Width;
            var cy = cur / g.Width;
            var hc = Heuristic(cx, cy, gx, gy);
            if (hc < closestH || (hc == closestH && cur < closest))
            {
                closestH = hc;
                closest = cur;
            }
            var gc = _g[cur];
            for (var d = 0; d < 8; d++)
            {
                if (!g.CanStep(cx, cy, d, radius)) continue;
                var nx = cx + NavGrid.Dx[d];
                var ny = cy + NavGrid.Dy[d];
                var ni = g.Index(nx, ny);
                var ng = gc + ((d & 1) == 0 ? 1f : Sqrt2);
                var h = Heuristic(nx, ny, gx, gy);
                if (ng + h > maxCost) continue;
                if (_stamp[ni] == _current)
                {
                    if (_closed[ni] || ng >= _g[ni]) continue;
                }
                else Touch(ni);
                _g[ni] = ng;
                _parent[ni] = cur;
                _open.Push(ni, ng + h, h);
            }
        }
        return false;
    }

    /// <summary>마지막 탐색에서 cell 까지 경로(시작 → cell, 칸 번호)를 채운다.</summary>
    public void PathTo(int cell, List<int> cells)
    {
        cells.Clear();
        for (var c = cell; c >= 0; c = _parent[c]) cells.Add(c);
        cells.Reverse();
    }

    /// <summary>마지막 탐색의 경로 비용(칸 단위). 그 칸을 안 봤으면 +∞.</summary>
    public float CostTo(int cell) => _stamp[cell] == _current ? _g[cell] : float.PositiveInfinity;

    /// <summary>
    /// cell 이 반경 radius 에 막혀 있으면 <paramref name="range"/> 칸 안에서 가장 가까운 통과 칸(칸 중심 거리, 같으면 번호 작은 쪽). 없으면 −1.
    /// </summary>
    public int NearestPassable(int cell, float radius, int range)
    {
        var g = _grid;
        if (g.Passable(cell, radius)) return cell;
        var cx = cell % g.Width;
        var cy = cell / g.Width;
        var best = -1;
        var bestD = int.MaxValue;
        for (var y = cy - range; y <= cy + range; y++)
            for (var x = cx - range; x <= cx + range; x++)
            {
                if (!g.InGrid(x, y)) continue;
                var i = g.Index(x, y);
                if (!g.Passable(i, radius)) continue;
                var d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d < bestD || (d == bestD && i < best))
                {
                    bestD = d;
                    best = i;
                }
            }
        return best;
    }

    /// <summary>옥타일 거리(칸 단위).</summary>
    public static float Heuristic(int ax, int ay, int bx, int by)
    {
        var dx = Math.Abs(ax - bx);
        var dy = Math.Abs(ay - by);
        return Math.Max(dx, dy) + (Sqrt2 - 1f) * Math.Min(dx, dy);
    }

    private void Touch(int i)
    {
        _stamp[i] = _current;
        _closed[i] = false;
    }
}
