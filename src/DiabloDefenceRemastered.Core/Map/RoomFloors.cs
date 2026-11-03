namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 방 바닥 하나(v0.6.3, 설계서 6.11 "방 바닥"). <see cref="Cells"/> = 하위 칸(행 우선 순서), <see cref="Doors"/> = 바닥 밖으로 이어지는 걷기 칸 무리 수(문·트인 쪽 수).
/// 기준점이 영역 밖·격자 밖·걷기 막힘이면 <see cref="Cells"/> 가 비고 <see cref="Doors"/> 0 이다(13.5-19 가 오류로 낸다).
/// </summary>
public sealed record RoomFloor(string Id, RectI Area, IReadOnlyList<Int2> Cells, int Doors)
{
    /// <summary>문 하나인 방(또는 구역) — 방 체류 압박·방 보급 대상(기획 10.1).</summary>
    public bool OneDoor => Doors == 1;
}

/// <summary>
/// 맵의 방 바닥 전부(v0.6.3, 설계서 6.11, 기획 10.1 "방 바닥 = 기준점에서 문을 지나지 않고 닿는 칸"). 맵을 읽을 때 한 번 계산한다 — <c>World.Rooms</c>.
/// 방 체류 압박(WP4 Director)·방 보급 검사(WP7 13.5-19)·방 들르기의 "방 안"(WP4 <c>AutoplayPolicy</c>)·<c>ROOMS</c> 로그(WP7)가 같은 계산을 쓴다.
/// <para>
/// 계산: <c>room_areas</c> 의 메가 사각형을 하위 칸으로 넓혀(2배), 그 안에서 기준점 칸부터 걷기 막힘 없는 칸을 4이웃으로 채운다(13.5-19 ① 과 같은 걷기 기준).
/// 반경 14 의 대리로 충분하다 — 하위 1칸 틈은 13.5-19 ② 가 막으므로 사각형 안에서 반경 14 가 못 지나는 걷기 칸 띠가 남지 않는다.
/// 문 수 = 사각형 밖이면서 바닥 칸에 4이웃으로 붙은 걷기 칸들을 4이웃으로 이은 무리 수.
/// </para>
/// 결정적이다: 방 순서 = 기준점 ID 서수 순, 칸 순서 = 행 우선.
/// </summary>
public sealed class RoomFloors
{
    private readonly short[] _roomAt;
    private readonly int _size;
    private readonly IsoMath _iso;

    private RoomFloors(IReadOnlyList<RoomFloor> rooms, short[] roomAt, int size, IsoMath iso)
    {
        Rooms = rooms;
        _roomAt = roomAt;
        _size = size;
        _iso = iso;
    }

    /// <summary>방 없음(맵 없는 시험 월드·<c>room_areas</c> 없는 맵).</summary>
    public static RoomFloors Empty { get; } = new([], [], 0, default);

    /// <summary>방 바닥(기준점 ID 서수 순). 방 번호 = 이 목록의 위치.</summary>
    public IReadOnlyList<RoomFloor> Rooms { get; }

    /// <summary>그 하위 칸이 바닥인 방 번호. 어느 방 바닥도 아니면(격자 밖 포함) −1.</summary>
    public int IndexAt(int tx, int ty) => (uint)tx < (uint)_size && (uint)ty < (uint)_size ? _roomAt[ty * _size + tx] : -1;

    /// <summary>월드 px 가 선 하위 칸의 방 번호(−1 = 방 밖). 방 체류 압박·"방 안" 판정.</summary>
    public int IndexAt(Vector2 world)
    {
        if (_size == 0) return -1;
        var c = _iso.CellOf(world);
        return IndexAt(c.X, c.Y);
    }

    public bool TryGet(string id, out RoomFloor floor)
    {
        foreach (var r in Rooms)
            if (string.Equals(r.Id, id, StringComparison.Ordinal)) { floor = r; return true; }
        floor = null!;
        return false;
    }

    /// <summary>
    /// <c>map.RoomAreas</c> 의 방마다 바닥·문 수를 계산한다. 잘못된 입력(없는 기준점, 격자 밖·막힌 기준점, 기준점이 영역 밖)은 빈 바닥으로 두고 던지지 않는다 —
    /// 오류 문구는 <see cref="ArenaLayout.ValidateRooms"/> 가 낸다. 두 방 바닥이 겹치면(영역 겹침, 13.5-19 오류) 칸의 방 번호는 앞 방이다.
    /// </summary>
    public static RoomFloors Compute(MapDef map, ArenaGrid grid)
    {
        if (map.RoomAreas.Count == 0) return Empty;
        var n = grid.Size;
        var roomAt = new short[n * n];
        Array.Fill(roomAt, (short)-1);
        var rooms = new List<RoomFloor>(map.RoomAreas.Count);
        var seen = new bool[n * n];
        var queue = new Queue<int>();

        foreach (var (id, area) in map.RoomAreas.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            var sub = SubRect(area, n);
            var cells = new List<Int2>();
            var doors = 0;
            if (map.AnchorsTile.TryGetValue(id, out var at) && at.Cell is var a && sub.Contains(a.X, a.Y) && grid.IsWalkable(a.X, a.Y))
            {
                // 바닥 — 사각형 안 4이웃 채우기
                Array.Clear(seen);
                var floor = new bool[n * n];
                floor[a.Y * n + a.X] = true;
                queue.Enqueue(a.Y * n + a.X);
                while (queue.Count > 0)
                {
                    var i = queue.Dequeue();
                    var (x, y) = (i % n, i / n);
                    Fill(x - 1, y); Fill(x + 1, y); Fill(x, y - 1); Fill(x, y + 1);
                }
                void Fill(int x, int y)
                {
                    if (!sub.Contains(x, y) || !grid.IsWalkable(x, y) || floor[y * n + x]) return;
                    floor[y * n + x] = true;
                    queue.Enqueue(y * n + x);
                }

                // 바닥 밖으로 붙은 걷기 칸(문 후보)
                var edge = new bool[n * n];
                for (var y = sub.Y0; y < sub.Y1; y++)
                    for (var x = sub.X0; x < sub.X1; x++)
                    {
                        if (!floor[y * n + x]) continue;
                        cells.Add(new Int2(x, y));
                        if (roomAt[y * n + x] < 0) roomAt[y * n + x] = (short)rooms.Count;
                        Edge(x - 1, y); Edge(x + 1, y); Edge(x, y - 1); Edge(x, y + 1);
                    }
                void Edge(int x, int y)
                {
                    if (!sub.Contains(x, y) && grid.IsWalkable(x, y)) edge[y * n + x] = true;
                }

                // 문 수 = 문 후보 칸의 4이웃 무리 수(행 우선으로 찾아 결정적)
                for (var i = 0; i < edge.Length; i++)
                {
                    if (!edge[i] || seen[i]) continue;
                    doors++;
                    seen[i] = true;
                    queue.Enqueue(i);
                    while (queue.Count > 0)
                    {
                        var j = queue.Dequeue();
                        var (x, y) = (j % n, j / n);
                        Join(x - 1, y); Join(x + 1, y); Join(x, y - 1); Join(x, y + 1);
                    }
                }
                void Join(int x, int y)
                {
                    if ((uint)x >= (uint)n || (uint)y >= (uint)n) return;
                    var k = y * n + x;
                    if (!edge[k] || seen[k]) return;
                    seen[k] = true;
                    queue.Enqueue(k);
                }
            }
            rooms.Add(new RoomFloor(id, area, cells, doors));
        }
        return new RoomFloors(rooms, roomAt, n, grid.Iso);
    }

    /// <summary>메가 사각형 → 하위 칸 사각형(끝 미포함), 격자로 자름.</summary>
    internal static RectI SubRect(RectI area, int n) =>
        new(Math.Clamp(area.X0 * 2, 0, n), Math.Clamp(area.Y0 * 2, 0, n), Math.Clamp(area.X1 * 2, 0, n), Math.Clamp(area.Y1 * 2, 0, n));
}
