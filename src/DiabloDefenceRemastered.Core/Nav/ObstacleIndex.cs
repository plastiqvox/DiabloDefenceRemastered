using DiabloDefenceRemastered.Core.Geometry;

namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 충돌 도형 색인 (설계서 4.5·4.7, 소유 WP2): 64px 칸마다 걸치는(외접 상자 기준) 도형 번호 목록. 투사체 선분 검사·충돌 해소·clearance 계산이 후보를 줄인다.
/// 칸 목록은 번호 오름차순이라 결과 순서가 결정적이다. 장애물이 움직이지 않으므로 월드마다 한 번 만든다.
/// </summary>
public sealed class ObstacleIndex
{
    private readonly int[] _start;   // 칸 → _items 시작(CSR), 길이 칸 수 + 1
    private readonly int[] _items;
    private readonly int[] _seen;    // 도형 → 마지막으로 본 질의 번호(중복 없이 한 번씩)
    private int _query;

    public ObstacleIndex(IReadOnlyList<CollisionShape> shapes, Vector2 boxSize, int cellSize = NavConst.ObstacleCellSize)
    {
        Shapes = shapes;
        CellSize = cellSize;
        Cols = Math.Max(1, (int)MathF.Ceiling(boxSize.X / cellSize));
        Rows = Math.Max(1, (int)MathF.Ceiling(boxSize.Y / cellSize));
        Polys = new Vector2[shapes.Count][];
        Min = new Vector2[shapes.Count];
        Max = new Vector2[shapes.Count];
        _seen = new int[shapes.Count];

        var counts = new int[Cols * Rows + 1];
        for (var s = 0; s < shapes.Count; s++)
        {
            var poly = shapes[s].Corners.ToArray();
            Polys[s] = poly;
            var min = poly[0];
            var max = poly[0];
            foreach (var v in poly)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            Min[s] = min;
            Max[s] = max;
            ForCells(min, max, (cell) => counts[cell + 1]++);
        }
        for (var i = 1; i < counts.Length; i++) counts[i] += counts[i - 1];
        _start = counts;
        _items = new int[counts[^1]];
        var fill = (int[])counts.Clone();
        for (var s = 0; s < shapes.Count; s++) ForCells(Min[s], Max[s], cell => _items[fill[cell]++] = s);
    }

    public IReadOnlyList<CollisionShape> Shapes { get; }
    /// <summary>도형 꼭짓점 배열(<see cref="CollisionShape.Corners"/> 복사 — 할당 없는 span 질의용).</summary>
    public Vector2[][] Polys { get; }
    /// <summary>도형 외접 상자.</summary>
    public Vector2[] Min { get; }
    public Vector2[] Max { get; }
    public int CellSize { get; }
    public int Cols { get; }
    public int Rows { get; }

    /// <summary>칸 하나에 걸친 도형 번호(오름차순). 밖이면 빈 목록.</summary>
    public ReadOnlySpan<int> At(int cx, int cy) =>
        (uint)cx < (uint)Cols && (uint)cy < (uint)Rows ? _items.AsSpan(_start[cy * Cols + cx], _start[cy * Cols + cx + 1] - _start[cy * Cols + cx]) : [];

    /// <summary>새 질의 번호(같은 도형을 여러 칸에서 한 번만 보려고). <see cref="FirstVisit"/> 와 짝.</summary>
    public int BeginQuery() => ++_query;

    /// <summary>이번 질의에서 처음 보는 도형이면 true.</summary>
    public bool FirstVisit(int shape, int query)
    {
        if (_seen[shape] == query) return false;
        _seen[shape] = query;
        return true;
    }

    /// <summary>
    /// 투사체가 a→b 를 지나갈 수 있나(4.5): 선분이 지나는 64px 칸의 <c>BlocksProjectiles</c> 도형과 정확한 교차 검사.
    /// 선분 외접 상자의 칸을 훑는다(선분이 짧아 칸 수가 적다).
    /// </summary>
    public bool ProjectileLineClear(Vector2 a, Vector2 b)
    {
        var q = BeginQuery();
        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        var x0 = Math.Max(0, (int)MathF.Floor(min.X / CellSize));
        var y0 = Math.Max(0, (int)MathF.Floor(min.Y / CellSize));
        var x1 = Math.Min(Cols - 1, (int)MathF.Floor(max.X / CellSize));
        var y1 = Math.Min(Rows - 1, (int)MathF.Floor(max.Y / CellSize));
        for (var cy = y0; cy <= y1; cy++)
            for (var cx = x0; cx <= x1; cx++)
                foreach (var s in At(cx, cy))
                {
                    if (!FirstVisit(s, q) || !Shapes[s].BlocksProjectiles) continue;
                    if (max.X < Min[s].X || min.X > Max[s].X || max.Y < Min[s].Y || min.Y > Max[s].Y) continue;
                    if (Geo.SegmentHitsPolygon(Polys[s], a, b)) return false;
                }
        return true;
    }

    /// <summary>
    /// 점에서 가장 가까운 걷기 막힘 도형까지 부호 있는 거리(밖 +, 안 −)와 그 도형 번호. <paramref name="boundaryOnly"/> 면 둘레 도형만(장애물 통과 유닛, 4.7-3).
    /// 도형이 없으면 +∞, −1. 모든 도형을 외접 상자 거리로 거른다(clearance 장을 만들 때 — 월드마다 한 번).
    /// </summary>
    public float NearestWalkDistance(Vector2 p, bool boundaryOnly, out int shape)
    {
        var best = float.PositiveInfinity;
        shape = -1;
        for (var s = 0; s < Polys.Length; s++)
        {
            var sh = Shapes[s];
            if (!sh.BlocksWalk || (boundaryOnly && !sh.IsBoundary)) continue;
            if (Geo.RectDistance(Min[s], Max[s], p) >= best) continue;
            var d = Geo.SignedDistance(Polys[s], p);
            if (d < best)
            {
                best = d;
                shape = s;
            }
        }
        return best;
    }

    private void ForCells(Vector2 min, Vector2 max, Action<int> visit)
    {
        var x0 = Math.Clamp((int)MathF.Floor(min.X / CellSize), 0, Cols - 1);
        var y0 = Math.Clamp((int)MathF.Floor(min.Y / CellSize), 0, Rows - 1);
        var x1 = Math.Clamp((int)MathF.Floor(max.X / CellSize), 0, Cols - 1);
        var y1 = Math.Clamp((int)MathF.Floor(max.Y / CellSize), 0, Rows - 1);
        for (var cy = y0; cy <= y1; cy++)
            for (var cx = x0; cx <= x1; cx++) visit(cy * Cols + cx);
    }
}
