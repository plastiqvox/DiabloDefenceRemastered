using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Core.Spatial;

/// <summary>
/// 적 공간 해시 (설계서 4.8, 소유 WP2 — <see cref="ISpatialQuery"/> 구현). 칸 32px, 외곽 상자 기준(상자 밖 자리는 가장자리 칸).
/// 넣는 것: <see cref="Enemy.IsTargetable"/>(Active·Stunned, 입장·등장 중은 위치와 상관없이 빠짐). 계수 정렬(칸별 개수 → 누적합 → 채우기)로 배열 셋을 재사용 — 할당 0, O(n).
/// 결과 = 칸 순서(행 우선) + 칸 안 삽입 순서(밀집 순서), 거리로 거른 정확한 집합. 넘치면 앞에서부터 채우고 실제 개수를 돌려준다.
/// 질의의 거리는 그 적의 <b>지금</b> 위치로 잰다 — 짓고 나서 조금 움직인 적(충돌 단계)도 <see cref="Slack"/> 만큼은 칸 밖에서 찾는다.
/// </summary>
public sealed class SpatialHash : ISpatialQuery
{
    private readonly World _world;
    private readonly int[] _start;     // 칸 → entries 시작, 길이 칸 수 + 1
    private int[] _entries = new int[512];
    private int[] _cellOf = new int[512];
    private int _count;

    public SpatialHash(World world, int cellSize = NavConst.SpatialCellSize)
    {
        _world = world;
        CellSize = cellSize;
        Cols = Math.Max(1, (int)MathF.Ceiling(world.MapSize.X / cellSize));
        Rows = Math.Max(1, (int)MathF.Ceiling(world.MapSize.Y / cellSize));
        _start = new int[Cols * Rows + 1];
    }

    public int CellSize { get; }
    public int Cols { get; }
    public int Rows { get; }
    /// <summary>넣은 적 수.</summary>
    public int Count => _count;
    /// <summary>넣은 적 중 가장 큰 반경(분리·충돌 질의 반경에 더한다).</summary>
    public float MaxRadius { get; private set; }
    /// <summary>지은 뒤 움직인 거리 여유(px) — 칸 범위를 이만큼 넓혀 본다.</summary>
    public float Slack { get; set; } = 8f;

    /// <summary>지금 위치로 다시 짓는다(Locomotion 앞·SpatialRebuild).</summary>
    public void Rebuild()
    {
        var enemies = _world.Enemies.Items;
        if (_cellOf.Length < enemies.Length)
        {
            var size = Math.Max(enemies.Length, _cellOf.Length * 2);
            _cellOf = new int[size];
            _entries = new int[size];
        }
        Array.Clear(_start);
        var maxR = 0f;
        for (var i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i];
            if (!e.IsTargetable)
            {
                _cellOf[i] = -1;
                continue;
            }
            var c = CellIndex(e.Pos);
            _cellOf[i] = c;
            _start[c + 1]++;
            if (e.Radius > maxR) maxR = e.Radius;
        }
        for (var c = 1; c < _start.Length; c++) _start[c] += _start[c - 1];
        _count = _start[^1];
        // 채우기: 칸 시작 자리를 하나씩 밀며(밀집 순서 = 칸 안 삽입 순서). 끝나면 _start 를 한 칸 되돌린다
        for (var i = 0; i < enemies.Length; i++)
        {
            var c = _cellOf[i];
            if (c < 0) continue;
            _entries[_start[c]++] = i;
        }
        for (var c = _start.Length - 1; c > 0; c--) _start[c] = _start[c - 1];
        _start[0] = 0;
        MaxRadius = maxR;
    }

    public int Query(Vector2 center, float radius, Span<int> denseIdx)
    {
        var r2 = radius * radius;
        var enemies = _world.Enemies.Items;
        var count = 0;
        GetRange(center, center, radius + Slack, out var x0, out var y0, out var x1, out var y1);
        for (var cy = y0; cy <= y1; cy++)
            for (var cx = x0; cx <= x1; cx++)
            {
                var c = cy * Cols + cx;
                for (var k = _start[c]; k < _start[c + 1]; k++)
                {
                    var i = _entries[k];
                    if (Vector2.DistanceSquared(enemies[i].Pos, center) > r2) continue;
                    if (count < denseIdx.Length) denseIdx[count] = i;
                    count++;
                }
            }
        return count;
    }

    public int QueryCapsule(Vector2 a, Vector2 b, float radius, Span<int> denseIdx)
    {
        var r2 = radius * radius;
        var enemies = _world.Enemies.Items;
        var count = 0;
        GetRange(Vector2.Min(a, b), Vector2.Max(a, b), radius + Slack, out var x0, out var y0, out var x1, out var y1);
        for (var cy = y0; cy <= y1; cy++)
            for (var cx = x0; cx <= x1; cx++)
            {
                var c = cy * Cols + cx;
                for (var k = _start[c]; k < _start[c + 1]; k++)
                {
                    var i = _entries[k];
                    if (MathX.DistanceSqToSegment(enemies[i].Pos, a, b) > r2) continue;
                    if (count < denseIdx.Length) denseIdx[count] = i;
                    count++;
                }
            }
        return count;
    }

    public int CellCount(int cx, int cy)
    {
        if ((uint)cx >= (uint)Cols || (uint)cy >= (uint)Rows) return 0;
        var c = cy * Cols + cx;
        return _start[c + 1] - _start[c];
    }

    /// <summary>자리 → 칸 번호(상자 밖은 가장자리 칸).</summary>
    public int CellIndex(Vector2 p)
    {
        var cx = Math.Clamp((int)MathF.Floor(p.X / CellSize), 0, Cols - 1);
        var cy = Math.Clamp((int)MathF.Floor(p.Y / CellSize), 0, Rows - 1);
        return cy * Cols + cx;
    }

    private void GetRange(Vector2 min, Vector2 max, float r, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = Math.Clamp((int)MathF.Floor((min.X - r) / CellSize), 0, Cols - 1);
        y0 = Math.Clamp((int)MathF.Floor((min.Y - r) / CellSize), 0, Rows - 1);
        x1 = Math.Clamp((int)MathF.Floor((max.X + r) / CellSize), 0, Cols - 1);
        y1 = Math.Clamp((int)MathF.Floor((max.Y + r) / CellSize), 0, Rows - 1);
    }
}
