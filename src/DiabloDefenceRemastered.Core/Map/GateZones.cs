namespace DiabloDefenceRemastered.Core.Map;

/// <summary>문 하나의 나가는 자리(v0.7, 6.11): 그 문의 열린 메가타일 둘의 하위 칸 중 걷기 막힘 없는 칸(행 우선 — y 다음 x).</summary>
public sealed record GateZone(string GateId, GateSide Side, IReadOnlyList<Int2> Cells)
{
    /// <summary>칸 하나가 이 문의 나가는 자리인가.</summary>
    public bool Contains(Int2 cell)
    {
        foreach (var c in Cells)
            if (c == cell) return true;
        return false;
    }
}

/// <summary>
/// 문 틈 칸·맞은편 문 (v0.7, 설계서 6.11 "방 맵" — 계약·구현 WP1e, 결정적). <c>World.Gates</c> 가 생성자에서 한 번 계산한다.
/// 쓰는 곳: 나가는 자리 판정(<c>RoomFlow</c>), 봉인 그리기·문 위 아이콘 자리(WP5, 7.11), 미니맵 문 점(WP6), 문 틈 닿음 검사(WP2, 13.5-23 ③).
/// 나가는 자리 = 기획 10.8 "문 틈 4칸 × 안쪽 2칸" — 문 틈 그 자체(앞 턱 문은 두 줄, 뒷벽 문도 메가 두께라 두 줄). 충돌은 바뀌지 않는다. <c>[해석]</c>
/// </summary>
public static class GateZones
{
    public static IReadOnlyDictionary<string, GateZone> Empty { get; } = new Dictionary<string, GateZone>(StringComparer.Ordinal);

    /// <summary>
    /// 맵의 문마다 나가는 자리. 문 자리는 배치 풀기(<c>ArenaLayout</c> 3단계)와 같은 규칙(<c>at</c>, 없으면 (M − 2) / 2)으로 찾는다.
    /// 배치가 잘못된 문(자리가 변 밖)은 건너뛴다 — 그 오류는 <c>ArenaLayout.TryExpand</c> 가 낸다.
    /// </summary>
    public static IReadOnlyDictionary<string, GateZone> Compute(MapDef map, ArenaGrid grid)
    {
        var m = grid.MegaSize;
        var result = new Dictionary<string, GateZone>(map.Layout.Gates.Count, StringComparer.Ordinal);
        foreach (var gate in map.Layout.Gates)
        {
            var at = gate.At ?? (m - 2) / 2;
            if (at < 1 || at + 1 > m - 2 || result.ContainsKey(gate.Id)) continue;
            var cells = new List<Int2>(8);
            for (var k = at; k <= at + 1; k++)
            {
                var (mx, my) = ArenaLayout.SideCell(gate.Side, k, m);
                for (var dy = 0; dy < 2; dy++)
                    for (var dx = 0; dx < 2; dx++)
                    {
                        var (tx, ty) = (mx * 2 + dx, my * 2 + dy);
                        if (grid.IsWalkable(tx, ty)) cells.Add(new Int2(tx, ty));
                    }
            }
            cells.Sort(static (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            result.Add(gate.Id, new GateZone(gate.Id, gate.Side, cells));
        }
        return result;
    }

    /// <summary>변이 맞은편(nw ↔ se, ne ↔ sw)인 문 ID. 그 문이 없거나 맞은편 문이 없으면 null. 들어온 문 바꾸기(<c>RoomState.RemapEntry</c>)가 쓴다.</summary>
    public static string? Opposite(MapDef map, string gateId)
    {
        GateSide? side = null;
        foreach (var g in map.Layout.Gates)
            if (string.Equals(g.Id, gateId, StringComparison.Ordinal)) { side = g.Side; break; }
        if (side is not { } s) return null;
        var opposite = OppositeSide(s);
        foreach (var g in map.Layout.Gates)
            if (g.Side == opposite) return g.Id;
        return null;
    }

    public static GateSide OppositeSide(GateSide side) => side switch
    {
        GateSide.Nw => GateSide.Se,
        GateSide.Se => GateSide.Nw,
        GateSide.Ne => GateSide.Sw,
        _ => GateSide.Ne,
    };
}
