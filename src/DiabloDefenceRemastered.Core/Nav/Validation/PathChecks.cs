using System.Globalization;
using DiabloDefenceRemastered.Core.Geometry;

namespace DiabloDefenceRemastered.Core.Nav.Validation;

/// <summary>
/// 길·기하 검사 13.5-4(도형 거리 쪽)·13.5-5·13.5-6 + 13.5-23 ③ 의 반경 격자 쪽(문 틈 닿음, 작은 방 진입점 150px 정밀) (설계서 6.14, 소유 WP2).
/// 실제 충돌 도형·16px clearance 격자로 본다 — 13.5-19 의 하위 칸 검사(<c>ArenaLayout.ValidateRooms</c>, 성질만) 위의 정밀 검사다. 자원 없이 늘 돈다.
/// 결과는 사람이 읽는 문장 목록(빈 목록 = 통과). 맵 하나가 여러 줄을 낼 수 있다.
/// </summary>
public static class PathChecks
{
    /// <summary>"붙음" 으로 보는 거리(px) — 탐욕 직사각형끼리 변을 같이 쓰면 0.</summary>
    public const float TouchEpsilon = 0.5f;

    /// <summary>맵 하나(배치 풀기 성공한 것)의 모든 검사.</summary>
    public static List<string> Check(MapDef map, ArenaGrid arena)
    {
        var errors = new List<string>();
        var nav = new NavWorld(arena, arena.Iso.BoxSize);
        var resolved = ResolvedMap.Resolve(map, arena.Iso);
        ShapeGaps(nav, errors);
        var reach = HeroReach(nav, resolved.HeroStart);
        Paths(map, arena, nav, resolved, reach, errors);
        Entries(map, arena, nav, resolved, errors);
        if (map.RoomKinds.Count > 0) GateReach(map, arena, nav, reach, errors);
        return errors;
    }

    // ── 13.5-4 도형 묶음 거리 ─────────────────────────────────────────

    /// <summary>
    /// 걷기 막힘 도형을 "붙음" 으로 묶은 묶음끼리·경계(바깥 마름모)와의 거리가 80px 이상이거나 붙어 있어야 한다.
    /// 둘레에 닿은 묶음(둘레 도형을 품거나 바깥 마름모에 붙음)끼리는 보지 않는다(같은 경계). 좁은 틈은 큰 적이 못 지나 무리가 비빈다(R3).
    /// </summary>
    public static void ShapeGaps(NavWorld nav, List<string> errors)
    {
        var idx = nav.Obstacles;
        var shapes = idx.Shapes;
        var walk = new List<int>();
        for (var i = 0; i < shapes.Count; i++)
            if (shapes[i].BlocksWalk) walk.Add(i);
        var parent = new int[shapes.Count];
        for (var i = 0; i < parent.Length; i++) parent[i] = i;
        var dist = new Dictionary<(int, int), float>();
        for (var a = 0; a < walk.Count; a++)
            for (var b = a + 1; b < walk.Count; b++)
            {
                var (i, j) = (walk[a], walk[b]);
                // 외접 상자가 80px 넘게 떨어지면 거리 계산 없이 넘어간다
                if (BoxGap(idx, i, j) >= NavConst.ShapeGap) continue;
                var d = Geo.PolygonDistance(idx.Polys[i], idx.Polys[j]);
                dist[(i, j)] = d;
                if (d <= TouchEpsilon) Union(parent, i, j);
            }

        var boundary = new HashSet<int>();
        foreach (var i in walk)
        {
            var onEdge = shapes[i].IsBoundary;
            if (!onEdge)
                foreach (var v in idx.Polys[i])
                    if (nav.BoundaryDistance(v) <= TouchEpsilon) { onEdge = true; break; }
            if (onEdge) boundary.Add(Find(parent, i));
        }

        var reported = new HashSet<(int, int)>();
        foreach (var ((i, j), d) in dist.OrderBy(p => p.Key.Item1).ThenBy(p => p.Key.Item2))
        {
            if (d <= TouchEpsilon || d >= NavConst.ShapeGap) continue;
            var (gi, gj) = (Find(parent, i), Find(parent, j));
            if (gi == gj || (boundary.Contains(gi) && boundary.Contains(gj))) continue;
            var key = gi < gj ? (gi, gj) : (gj, gi);
            if (!reported.Add(key)) continue;
            errors.Add(string.Create(CultureInfo.InvariantCulture,
                $"13.5-4 충돌 도형 사이 {d:0.#}px (80px 이상이거나 붙어야 함): 하위 칸 {shapes[i].Tiles} ↔ {shapes[j].Tiles}"));
        }
        // 둘레에 안 닿은 묶음과 바깥 마름모
        var edgeReported = new HashSet<int>();
        foreach (var i in walk)
        {
            var g = Find(parent, i);
            if (boundary.Contains(g)) continue;
            var d = float.MaxValue;
            foreach (var v in idx.Polys[i]) d = MathF.Min(d, nav.BoundaryDistance(v));
            if (d < NavConst.ShapeGap && edgeReported.Add(g))
                errors.Add(string.Create(CultureInfo.InvariantCulture,
                    $"13.5-4 충돌 도형과 맵 경계 사이 {d:0.#}px (80px 이상이거나 붙어야 함): 하위 칸 {shapes[i].Tiles}"));
        }
    }

    // ── 13.5-5 길 ─────────────────────────────────────────────────────

    /// <summary>주인공 시작에서 반경 14 로 닿는 내비 칸(8방향·코너 자르기 금지 채우기). 시작 3칸 안에 통과 칸이 없으면 모두 거짓.</summary>
    public static bool[] HeroReach(NavWorld nav, Vector2 heroStart) => Flood(nav, heroStart, NavConst.HeroCheckRadius);

    /// <summary>반경 radius 로 from 에서 닿는 내비 칸.</summary>
    public static bool[] Flood(NavWorld nav, Vector2 from, float radius)
    {
        var g = nav.Grid;
        var reached = new bool[g.Count];
        var start = nav.AStar.NearestPassable(g.ClampedIndex(from), radius, NavConst.StartSearchCells);
        if (start < 0) return reached;
        var queue = new int[g.Count];
        var head = 0;
        var tail = 0;
        queue[tail++] = start;
        reached[start] = true;
        while (head < tail)
        {
            var cur = queue[head++];
            var cx = cur % g.Width;
            var cy = cur / g.Width;
            for (var d = 0; d < 8; d++)
            {
                if (!g.CanStep(cx, cy, d, radius)) continue;
                var ni = g.Index(cx + NavGrid.Dx[d], cy + NavGrid.Dy[d]);
                if (reached[ni]) continue;
                reached[ni] = true;
                queue[tail++] = ni;
            }
        }
        return reached;
    }

    /// <summary>
    /// 점 p 에 닿는가: p 의 칸에서 3칸(A* 의 시작·목표 대체 범위, 4.4) 안에 닿는 칸이 있다. 기준점이 기둥 옆이라 큰 몸이 그 칸에 정확히 못 서도
    /// 3칸 안까지 오면 닿은 것으로 본다(A* 가 "갈 수 있는 가장 가까운 곳" 까지 가는 것과 같은 뜻). <c>[해석]</c>
    /// </summary>
    public static bool Reaches(NavWorld nav, bool[] reached, Vector2 p, float radius)
    {
        var g = nav.Grid;
        var c = g.CellOf(p);
        var range = NavConst.StartSearchCells;
        for (var y = c.Y - range; y <= c.Y + range; y++)
            for (var x = c.X - range; x <= c.X + range; x++)
                if (g.InGrid(x, y) && reached[g.Index(x, y)]) return true;
        return false;
    }

    /// <summary>
    /// 하위 칸 (tx, ty) 에 주인공 몸이 걸칠 수 있나: 닿는 내비 칸 중심이 그 칸 마름모에서 14px 안(6.14 — 갇힌 바닥 칸·문 틈 닿음의 같은 판정).
    /// </summary>
    public static bool Touches(NavWorld nav, ArenaGrid arena, bool[] reached, int tx, int ty)
    {
        var g = nav.Grid;
        Span<Vector2> diamond = stackalloc Vector2[4];
        var c = arena.Iso.CellCenter(tx, ty);
        diamond[0] = c - new Vector2(0, IsoMath.HalfHeight);
        diamond[1] = c + new Vector2(IsoMath.HalfWidth, 0);
        diamond[2] = c + new Vector2(0, IsoMath.HalfHeight);
        diamond[3] = c - new Vector2(IsoMath.HalfWidth, 0);
        var r = NavConst.HeroCheckRadius;
        var x0 = (int)MathF.Floor((c.X - IsoMath.HalfWidth - r) / g.CellSize);
        var x1 = (int)MathF.Floor((c.X + IsoMath.HalfWidth + r) / g.CellSize);
        var y0 = (int)MathF.Floor((c.Y - IsoMath.HalfHeight - r) / g.CellSize);
        var y1 = (int)MathF.Floor((c.Y + IsoMath.HalfHeight + r) / g.CellSize);
        for (var cy = y0; cy <= y1; cy++)
            for (var cx = x0; cx <= x1; cx++)
            {
                if (!g.InGrid(cx, cy)) continue;
                var i = g.Index(cx, cy);
                if (reached[i] && Geo.SignedDistance(diamond, g.Center(i)) <= r) return true;
            }
        return false;
    }

    /// <summary>
    /// 13.5-5: 반경 14(주인공 시작 → 모든 입구 진입점), 갇힌 바닥 칸, 16·26·34(입구 진입점 → <c>center</c>·모든 <c>room_*</c>),
    /// 주인공이 설 수 있는 모든 칸에서 <c>S</c> 필드 시작점 ≥ 1.
    /// </summary>
    public static void Paths(MapDef map, ArenaGrid arena, NavWorld nav, ResolvedMap resolved, bool[] reach, List<string> errors)
    {
        foreach (var e in resolved.Entries)
            if (!Reaches(nav, reach, e.Inside, NavConst.HeroCheckRadius))
                errors.Add($"13.5-5 주인공(반경 14)이 시작에서 입구 진입점 {e.Id} 에 못 닿음");

        // 갇힌 바닥 칸 — 메가타일로 묶어 한 줄씩
        var trapped = new SortedDictionary<(int My, int Mx), List<string>>();
        for (var ty = 0; ty < arena.Size; ty++)
            for (var tx = 0; tx < arena.Size; tx++)
            {
                if (!arena.IsWalkable(tx, ty) || Touches(nav, arena, reach, tx, ty)) continue;
                var key = (ty >> 1, tx >> 1);
                if (!trapped.TryGetValue(key, out var list)) trapped[key] = list = [];
                list.Add($"({tx},{ty})");
            }
        foreach (var ((my, mx), cells) in trapped)
            errors.Add($"13.5-5 갇힌 바닥 칸 메가 [{mx},{my}]: {string.Join(" ", cells)}");

        // 큰 적: 입구 진입점마다 center·room_* 까지
        var targets = resolved.Anchors.Keys
            .Where(k => k == RoomMapRules.CenterAnchor || k.StartsWith("room_", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToList();
        foreach (var radius in new[] { NavConst.FieldS, NavConst.FieldM, NavConst.LargeCheckRadius })
            foreach (var e in resolved.Entries)
            {
                var flood = Flood(nav, e.Inside, radius);
                foreach (var t in targets)
                    if (!Reaches(nav, flood, resolved.Anchor(t), radius))
                        errors.Add(string.Create(CultureInfo.InvariantCulture, $"13.5-5 반경 {radius:0} 이 입구 진입점 {e.Id} 에서 기준점 {t} 에 못 닿음"));
            }

        // 주인공이 설 수 있는 칸마다 S 필드 시작점(주인공 반경 + 16 안 통과 칸) ≥ 1
        var g = nav.Grid;
        var s = nav.Fields[0];
        var span = (int)MathF.Ceiling((NavConst.HeroCheckRadius + NavConst.FieldGoalMargin) / g.CellSize) + 1;
        var reachR = NavConst.HeroCheckRadius + NavConst.FieldGoalMargin;
        var noStart = 0;
        var first = -1;
        for (var i = 0; i < reach.Length; i++)
        {
            if (!reach[i]) continue;
            var p = g.Center(i);
            var cx = i % g.Width;
            var cy = i / g.Width;
            var ok = false;
            for (var y = cy - span; y <= cy + span && !ok; y++)
                for (var x = cx - span; x <= cx + span && !ok; x++)
                    ok = g.InGrid(x, y) && s.Passable(g.Index(x, y)) && Vector2.DistanceSquared(g.Center(x, y), p) <= reachR * reachR;
            if (!ok)
            {
                noStart++;
                if (first < 0) first = i;
            }
        }
        if (noStart > 0)
            errors.Add($"13.5-5 S 필드 시작점이 없는 주인공 자리 {noStart}칸(첫 칸 중심 {g.Center(first)})");
    }

    // ── 13.5-6 입구 ───────────────────────────────────────────────────

    /// <summary>
    /// 13.5-6: 입구 진입점·시작 위치의 clearance ≥ 38, 바깥 시작점은 맵 밖, 진입점에서 둘레가 아닌 걷기 막힘 도형까지 &gt; 60px.
    /// 작은 방(N ≤ 36, 방 맵)은 그 거리가 &gt; 150px(13.5-23 ③ 의 정밀판 — 같은 함수에 거리만 바꿈).
    /// </summary>
    public static void Entries(MapDef map, ArenaGrid arena, NavWorld nav, ResolvedMap resolved, List<string> errors)
    {
        var heroClear = nav.ExactClearance(resolved.HeroStart);
        if (heroClear < NavConst.EntryClearance)
            errors.Add(string.Create(CultureInfo.InvariantCulture, $"13.5-6 주인공 시작 clearance {heroClear:0.#} < 38"));
        var small = map.RoomKinds.Count > 0 && arena.Size <= RoomMapRules.SmallRoomMaxN;
        foreach (var e in resolved.Entries)
        {
            var c = nav.ExactClearance(e.Inside);
            if (c < NavConst.EntryClearance)
                errors.Add(string.Create(CultureInfo.InvariantCulture, $"13.5-6 입구 진입점 {e.Id} clearance {c:0.#} < 38"));
            if (nav.IsInsideMap(e.Outside))
                errors.Add($"13.5-6 입구 바깥 시작점 {e.Id} 가 맵 안");
            var inner = InnerDistance(nav, e.Inside, out var shape);
            if (inner <= NavConst.EntryInnerDistance)
                errors.Add(string.Create(CultureInfo.InvariantCulture,
                    $"13.5-6 입구 진입점 {e.Id} 에서 안쪽 걷기 막힘 도형(하위 칸 {nav.Collision[shape].Tiles})까지 {inner:0.#}px — 60px 넘어야 함"));
            else if (small && inner <= RoomMapRules.DoorClearance)
                errors.Add(string.Create(CultureInfo.InvariantCulture,
                    $"13.5-23 작은 방 문 진입점 {e.Id} 에서 안쪽 걷기 막힘 도형(하위 칸 {nav.Collision[shape].Tiles})까지 {inner:0.#}px — 150px 넘어야 함"));
        }
    }

    /// <summary>점에서 둘레가 아닌 걷기 막힘 도형까지 가장 짧은 거리(안이면 0). 없으면 +∞.</summary>
    public static float InnerDistance(NavWorld nav, Vector2 p, out int shape)
    {
        var best = float.PositiveInfinity;
        shape = -1;
        var idx = nav.Obstacles;
        for (var s = 0; s < idx.Shapes.Count; s++)
        {
            var sh = idx.Shapes[s];
            if (!sh.BlocksWalk || sh.IsBoundary) continue;
            var d = MathF.Max(0f, Geo.SignedDistance(idx.Polys[s], p));
            if (d < best)
            {
                best = d;
                shape = s;
            }
        }
        return best;
    }

    // ── 13.5-23 ③ 문 틈 닿음 ──────────────────────────────────────────

    /// <summary>방 맵 문 넷마다 나가는 자리 칸(<c>GateZones</c>) 중 하나 이상에 주인공 시작에서 반경 14 로 닿음(<see cref="Touches"/>).</summary>
    public static void GateReach(MapDef map, ArenaGrid arena, NavWorld nav, bool[] reach, List<string> errors)
    {
        foreach (var (id, zone) in GateZones.Compute(map, arena).OrderBy(z => z.Key, StringComparer.Ordinal))
        {
            var ok = false;
            foreach (var c in zone.Cells)
                if (Touches(nav, arena, reach, c.X, c.Y)) { ok = true; break; }
            if (!ok) errors.Add($"13.5-23 문 틈에 못 닿음 {id}");
        }
    }

    private static float BoxGap(ObstacleIndex idx, int i, int j)
    {
        var dx = MathF.Max(0f, MathF.Max(idx.Min[i].X - idx.Max[j].X, idx.Min[j].X - idx.Max[i].X));
        var dy = MathF.Max(0f, MathF.Max(idx.Min[i].Y - idx.Max[j].Y, idx.Min[j].Y - idx.Max[i].Y));
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }
        return i;
    }

    private static void Union(int[] parent, int a, int b)
    {
        var ra = Find(parent, a);
        var rb = Find(parent, b);
        if (ra == rb) return;
        if (ra < rb) parent[rb] = ra;
        else parent[ra] = rb;
    }
}

/// <summary>
/// 등록식 검사기 (설계서 6.2 7단계·6.14 13.5-4~6·13.5-23 ③, 소유 WP2): 정의의 모든 맵(모든 액트 풀 — 정의 표가 곧 풀의 합)과
/// 거울판(<c>MapDef.Mirrored</c>, v0.8)마다 <see cref="PathChecks.Check"/>. 배치 풀기가 실패한 맵은 건너뛴다(그 오류는 맵 읽기가 낸다).
/// </summary>
public sealed class NavPathValidator : IDataValidator
{
    public string Name => "nav.paths";

    public void Validate(GameDefs defs, ValidationContext ctx)
    {
        foreach (var map in defs.Maps.All)
        {
            Run(map, ctx);
            if (map.Mirrored is { } mirrored) Run(mirrored, ctx);
        }
    }

    /// <summary>맵 하나의 검사 결과(배치 풀기가 안 되면 빈 목록).</summary>
    public static List<string> Check(MapDef map)
    {
        var layoutErrors = new List<LayoutError>();
        var arena = ArenaLayout.TryExpand(map, map.Tileset.Def, layoutErrors);
        return arena is null ? [] : PathChecks.Check(map, arena);
    }

    private static void Run(MapDef map, ValidationContext ctx)
    {
        foreach (var message in Check(map))
            ctx.Add($"maps/{map.MirrorOf ?? map.Id}.json", "$", map.IsMirror ? $"(거울판 {map.Id}) {message}" : message);
    }
}
