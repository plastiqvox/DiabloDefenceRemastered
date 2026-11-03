namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 걸을 넓이(v0.7, 6.11 — 계약·구현 WP1e). 넓이 배율(3.6)의 분자·분모가 같은 함수다.
/// </summary>
public static class MapArea
{
    /// <summary>
    /// 주인공 시작 칸에서 걷기 막힘 없는 하위 칸을 4이웃으로 채운 칸 수(13.5-19 ① 과 같은 채우기). 기획의 "반경 14 걸을 넓이" 의 대리다 —
    /// 13.5-19 ② 가 하위 1칸 틈을 막으므로 반경 14 가 못 지나는 걷기 칸 띠가 남지 않는다. 시작 칸이 막혔거나 격자 밖이면 0.
    /// </summary>
    public static int WalkableCells(ArenaGrid grid)
    {
        var n = grid.Size;
        var start = grid.Map.HeroStart.Tile.Cell;
        if (!grid.IsWalkable(start.X, start.Y)) return 0;
        var reached = new bool[n * n];
        var queue = new Queue<int>();
        reached[start.Y * n + start.X] = true;
        queue.Enqueue(start.Y * n + start.X);
        var count = 0;
        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            count++;
            var (x, y) = (i % n, i / n);
            Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
        }
        return count;

        void Visit(int x, int y)
        {
            if (!grid.IsWalkable(x, y) || reached[y * n + x]) return;
            reached[y * n + x] = true;
            queue.Enqueue(y * n + x);
        }
    }
}
