using System.Globalization;
using System.Text;

namespace DiabloDefenceRemastered.Core.Map;

/// <summary>배치·타일셋 검사 오류. <see cref="Path"/> 는 그 정의 원소 기준(<c>$.layout.gates[1].side</c>).</summary>
public readonly record struct LayoutError(string Path, string Message)
{
    public override string ToString() => $"{Path}: {Message}";
}

/// <summary>
/// 배치 풀기 <c>Expand(MapDef, TilesetDef) → ArenaGrid</c> (설계서 6.11, 계약 WP1b, 결정적). 가져오기 도구(그림)와 WP2(충돌)가 같은 함수를 쓴다
/// → 그림을 구운 배치와 충돌을 만든 배치가 같은 코드에서 나온다(1.2).
/// <list type="number">
/// <item>모든 칸 = 바닥: <c>floor[StableHash(맵 ID, mx, my) % 개수]</c>.</item>
/// <item>테두리: 네 모서리·네 변을 <c>borders[border]</c> 번호로(둘레 표시).</item>
/// <item>문: 변 가운데(또는 <c>at</c>) 메가타일 2개를 <c>open</c> 으로, 양옆 한 칸씩을 <c>end_a</c>/<c>end_b</c> 로(끝 조각은 둘레 표시).</item>
/// <item><c>place</c>: 조각 번호를 <c>at</c> 의 [mx,my] 마다, 또는 <c>line</c>(가로·세로 줄)에.</item>
/// <item>결과: 번호 격자 + 성질 표로 푼 하위 칸 성질 + 둘레 표시 + <c>LayoutHash</c>.</item>
/// </list>
/// 검사(13.5-4 배치 쪽): 배치가 M×M 안, 쓰는 메가타일이 모두 성질 표에 있음, 문 칸이 비어 있음, 놓은 조각이 테두리·다른 조각과 겹치지 않음.
/// 타일셋 자체 검사(13.5-12)는 <see cref="ValidateTileset"/>.
/// </summary>
public static class ArenaLayout
{
    /// <summary>
    /// 성질 글자 8개(v0.5, 설계서 6.11 표). 글자 번호 = 원본 <c>sol &amp; 7</c>(bit0 걷기, bit1 시야, bit2 투사체) → 가져오기 도구는 <c>PropChars[sol &amp; 7]</c> 로 적는다.
    /// 대문자 = 시야 막힘, 소문자 = 시야 통과. v0.4 의 <c>W</c>(걷기만)·<c>M</c>(투사체만)은 <c>w</c>·<c>m</c> 이 되었고, 지금 <c>W</c>·<c>M</c> 은 시야도 막는다.
    /// 하위 칸 4개를 TIL 순서로 적는다.
    /// </summary>
    public const string PropChars = ".wLWmxMX";

    public static ArenaGrid Expand(MapDef map) => Expand(map, map.Tileset.Def);

    /// <summary>풀고, 오류가 있으면 모두 담은 <see cref="InvalidDataException"/>. 로더가 이미 검사한 정의에 쓴다.</summary>
    public static ArenaGrid Expand(MapDef map, TilesetDef tileset)
    {
        var errors = new List<LayoutError>();
        var grid = TryExpand(map, tileset, errors);
        if (grid is null || errors.Count > 0)
            throw new InvalidDataException($"맵 '{map.Id}' 배치를 풀 수 없다: " + string.Join(" / ", errors));
        return grid;
    }

    /// <summary>풀기. 오류를 모두 <paramref name="errors"/> 에 넣고, 하나라도 있으면 null.</summary>
    public static ArenaGrid? TryExpand(MapDef map, TilesetDef tileset, ICollection<LayoutError> errors)
    {
        var before = errors.Count;
        var m = map.Size;
        if (m < 3) { errors.Add(new("$.size", $"메가타일 수는 3 이상이어야 한다({m})")); return null; }
        if (tileset.Floor.Count == 0) { errors.Add(new("$.tileset", $"타일셋 '{tileset.Id}' 에 floor 가 없다")); return null; }
        if (tileset.ColumnHeight < 32) { errors.Add(new("$.tileset", $"타일셋 '{tileset.Id}' 의 column_height 는 32 이상이어야 한다")); return null; }

        var layout = map.Layout;
        var mega = new int[m * m];
        var source = new string[m * m];
        var boundary = new bool[m * m];
        var claimed = new bool[m * m];   // 문·조각이 차지한 칸(겹침 검사)

        // 1. 바닥
        var mapHash = StableHash.Of(map.Id);
        for (var my = 0; my < m; my++)
            for (var mx = 0; mx < m; mx++)
            {
                var i = my * m + mx;
                mega[i] = tileset.Floor[(int)(StableHash.Of(mapHash, (uint)mx, (uint)my) % (uint)tileset.Floor.Count)];
                source[i] = "floor";
            }

        // 2. 테두리
        if (!tileset.Borders.TryGetValue(layout.Border, out var border))
        {
            errors.Add(new("$.layout.border", $"타일셋 '{tileset.Id}' 에 테두리 '{layout.Border}' 가 없다"));
            return null;
        }
        void SetBorder(int mx, int my, int tile, string name)
        {
            var i = my * m + mx;
            mega[i] = tile;
            source[i] = $"borders.{layout.Border}.{name}";
            boundary[i] = true;
        }
        for (var k = 1; k < m - 1; k++)
        {
            SetBorder(k, 0, border.EdgeNe, "edge_ne");
            SetBorder(m - 1, k, border.EdgeSe, "edge_se");
            SetBorder(k, m - 1, border.EdgeSw, "edge_sw");
            SetBorder(0, k, border.EdgeNw, "edge_nw");
        }
        SetBorder(0, 0, border.CornerN, "corner_n");
        SetBorder(m - 1, 0, border.CornerE, "corner_e");
        SetBorder(m - 1, m - 1, border.CornerS, "corner_s");
        SetBorder(0, m - 1, border.CornerW, "corner_w");

        // 3. 문
        tileset.Gates.TryGetValue(layout.Border, out var gateSet);
        var gateIds = new HashSet<string>(StringComparer.Ordinal);
        var openCells = new List<(int Index, int Mx, int My)>();
        for (var g = 0; g < layout.Gates.Count; g++)
        {
            var gate = layout.Gates[g];
            var path = $"$.layout.gates[{g}]";
            if (!gateIds.Add(gate.Id)) { errors.Add(new($"{path}.id", $"문 ID '{gate.Id}' 가 두 번 있다")); continue; }
            var tiles = gateSet?.Get(gate.Side);
            var sideName = SideName(gate.Side);
            if (tiles is null)
            {
                errors.Add(new($"{path}.side", $"타일셋 '{tileset.Id}' 의 테두리 '{layout.Border}' 에 {sideName} 문이 없다(gates.{layout.Border}.{sideName})"));
                continue;
            }
            var at = gate.At ?? (m - 2) / 2;
            if (at < 1 || at + 1 > m - 2)
            {
                errors.Add(new($"{path}.at", $"문 자리 {at}: 열린 두 칸 {at}·{at + 1} 이 변 안(1..{m - 2})이어야 한다"));
                continue;
            }
            if (tiles.EndA is not null && at - 1 < 1) { errors.Add(new($"{path}.at", $"문 끝 조각 end_a 자리 {at - 1} 가 모서리다")); continue; }
            if (tiles.EndB is not null && at + 2 > m - 2) { errors.Add(new($"{path}.at", $"문 끝 조각 end_b 자리 {at + 2} 가 모서리다")); continue; }

            var gatePath = $"gates.{layout.Border}.{sideName}";
            var spots = new List<(int K, int Tile, string Name, bool Boundary)>
            {
                (at, tiles.Open, gatePath + ".open", false),
                (at + 1, tiles.Open, gatePath + ".open", false),
            };
            if (tiles.EndA is { } ea) spots.Add((at - 1, ea, gatePath + ".end_a", true));
            if (tiles.EndB is { } eb) spots.Add((at + 2, eb, gatePath + ".end_b", true));

            var overlap = false;
            foreach (var s in spots)
            {
                var (mx, my) = SideCell(gate.Side, s.K, m);
                if (claimed[my * m + mx]) overlap = true;
            }
            if (overlap) { errors.Add(new(path, $"문 '{gate.Id}' 이 다른 문과 겹친다")); continue; }

            foreach (var s in spots)
            {
                var (mx, my) = SideCell(gate.Side, s.K, m);
                var i = my * m + mx;
                mega[i] = s.Tile;
                source[i] = s.Name;
                boundary[i] = s.Boundary;
                claimed[i] = true;
                if (!s.Boundary) openCells.Add((g, mx, my));
            }
        }

        // 4. 안쪽 조각
        for (var p = 0; p < layout.Place.Count; p++)
        {
            var place = layout.Place[p];
            var path = $"$.layout.place[{p}]";
            if (!tileset.Pieces.TryGetValue(place.Piece, out var tile))
            {
                errors.Add(new($"{path}.piece", $"타일셋 '{tileset.Id}' 에 조각 '{place.Piece}' 가 없다"));
                continue;
            }
            var cells = new List<(Int2 Cell, string Path)>();
            if (place.Line is { } line)
            {
                if (place.At.Count > 0) { errors.Add(new(path, "at 과 line 중 하나만 쓴다")); continue; }
                if (line.Count != 2) { errors.Add(new($"{path}.line", "line 은 [[mx0,my0],[mx1,my1]] 두 점이다")); continue; }
                var (a, b) = (line[0], line[1]);
                if (a.X != b.X && a.Y != b.Y) { errors.Add(new($"{path}.line", $"line 은 가로·세로 줄이어야 한다({a} → {b})")); continue; }
                var steps = Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
                var d = new Int2(Math.Sign(b.X - a.X), Math.Sign(b.Y - a.Y));
                for (var s = 0; s <= steps; s++) cells.Add((new Int2(a.X + d.X * s, a.Y + d.Y * s), $"{path}.line"));
            }
            else if (place.At.Count == 0) { errors.Add(new(path, "at 또는 line 이 있어야 한다")); continue; }
            else
                for (var j = 0; j < place.At.Count; j++) cells.Add((place.At[j], $"{path}.at[{j}]"));

            foreach (var (c, cellPath) in cells)
            {
                if ((uint)c.X >= (uint)m || (uint)c.Y >= (uint)m) { errors.Add(new(cellPath, $"{c} 가 {m}×{m} 밖이다")); continue; }
                if (c.X == 0 || c.Y == 0 || c.X == m - 1 || c.Y == m - 1) { errors.Add(new(cellPath, $"{c} 가 테두리와 겹친다")); continue; }
                var i = c.Y * m + c.X;
                if (claimed[i]) { errors.Add(new(cellPath, $"{c} 에 조각이 이미 있다")); continue; }
                mega[i] = tile;
                source[i] = $"pieces.{place.Piece}";
                claimed[i] = true;
            }
        }

        // 5. 성질 풀기
        var n = m * 2;
        var cellsOut = new CellFlags[n * n];
        var missing = new HashSet<int>();
        Span<CellFlags> props = stackalloc CellFlags[4];
        for (var my = 0; my < m; my++)
            for (var mx = 0; mx < m; mx++)
            {
                var i = my * m + mx;
                var tile = mega[i];
                if (!tileset.Props.TryGetValue(tile, out var text))
                {
                    if (missing.Add(tile))
                        errors.Add(new("$.layout", $"메가타일 {tile}({source[i]})이 타일셋 '{tileset.Id}' 의 성질 표(props)에 없다"));
                    continue;
                }
                if (!TryParseProps(text, props))
                {
                    if (missing.Add(tile))
                        errors.Add(new("$.layout", $"타일셋 '{tileset.Id}' 의 메가타일 {tile} 성질 '{text}' 가 틀렸다(글자 4개, {PropChars} — 대문자 = 시야 막힘)"));
                    continue;
                }
                var extra = boundary[i] ? CellFlags.Boundary : CellFlags.None;
                cellsOut[(2 * my) * n + 2 * mx] = props[0] | extra;
                cellsOut[(2 * my) * n + 2 * mx + 1] = props[1] | extra;
                cellsOut[(2 * my + 1) * n + 2 * mx] = props[2] | extra;
                cellsOut[(2 * my + 1) * n + 2 * mx + 1] = props[3] | extra;
            }

        foreach (var (g, mx, my) in openCells)
        {
            var tile = mega[my * m + mx];
            if (tileset.Props.TryGetValue(tile, out var text) && text != "....")
                errors.Add(new($"$.layout.gates[{g}]", $"문 칸 ({mx},{my}) 이 비어 있지 않다(메가타일 {tile} 성질 '{text}')"));
        }

        if (errors.Count > before) return null;
        var iso = new IsoMath(n, tileset.ColumnHeight);
        return new ArenaGrid(map, tileset, iso, mega, cellsOut, ComputeLayoutHash(map, tileset, mega));
    }

    /// <summary>
    /// 타일셋 자체 검사(13.5-12, 늘 돈다): 성질 글자, <b>성질 표 키 ⊆ 이 타일셋이 이름 붙인 번호</b>(floor·borders·gates·pieces — SOL 덤프 막기, R24),
    /// 이름 붙인 번호는 모두 성질 표에 있음, 보정 목록의 칸이 성질 표에 있고 <c>use</c> 가 그 칸의 글자와 같음, <c>why</c> 가 비지 않음.
    /// </summary>
    public static void ValidateTileset(TilesetDef tileset, ICollection<LayoutError> errors)
    {
        if (tileset.ColumnHeight < 32) errors.Add(new("$.column_height", "기둥 높이는 32 이상이어야 한다"));
        if (tileset.Floor.Count == 0) errors.Add(new("$.floor", "바닥 메가타일이 하나도 없다"));

        Span<CellFlags> scratch = stackalloc CellFlags[4];
        foreach (var (tile, text) in tileset.Props.OrderBy(p => p.Key))
            if (!TryParseProps(text, scratch))
                errors.Add(new($"$.props.{tile}", $"성질 '{text}' 가 틀렸다(하위 칸 4개를 {PropChars} 글자로 — 대문자 = 시야 막힘)"));

        var named = NamedTiles(tileset);
        if (named.Count > 0 && named.Min < 0)
            errors.Add(new("$", $"메가타일 번호 {named.Min} — 번호는 0 이상이다(floor·borders·gates·pieces)"));
        foreach (var tile in tileset.Props.Keys.OrderBy(k => k))
            if (tile < 0) errors.Add(new($"$.props.{tile}", $"메가타일 번호 {tile} — 번호는 0 이상이다"));
        foreach (var tile in tileset.Props.Keys.OrderBy(k => k))
            if (!named.Contains(tile))
                errors.Add(new($"$.props.{tile}", $"메가타일 {tile} 은 이 타일셋이 이름 붙인 번호(floor·borders·gates·pieces)가 아니다 — 쓰지 않는 메가타일은 성질 표에 두지 않는다"));
        foreach (var tile in named.Order())
            if (!tileset.Props.ContainsKey(tile))
                errors.Add(new("$.props", $"메가타일 {tile} 이 성질 표에 없다(floor·borders·gates·pieces 가 이름 붙인 번호)"));

        CheckDiffs(tileset, tileset.SolKnownDiffs, "sol_known_diffs", errors);
        CheckDiffs(tileset, tileset.DesignDiffs, "design_diffs", errors);
    }

    private static void CheckDiffs(TilesetDef tileset, IReadOnlyList<SolDiffDef> diffs, string name, ICollection<LayoutError> errors)
    {
        for (var i = 0; i < diffs.Count; i++)
        {
            var d = diffs[i];
            var path = $"$.{name}[{i}]";
            if (string.IsNullOrWhiteSpace(d.Why)) errors.Add(new($"{path}.why", "이유(why)가 비어 있다"));
            if (d.Sub is < 0 or > 3) errors.Add(new($"{path}.sub", $"하위 칸은 0..3(TIL 순서)이다({d.Sub})"));
            if (d.Sol.Length != 1 || !PropChars.Contains(d.Sol[0])) errors.Add(new($"{path}.sol", $"원본 글자 '{d.Sol}' 는 {PropChars} 중 하나"));
            if (d.Use.Length != 1 || !PropChars.Contains(d.Use[0])) errors.Add(new($"{path}.use", $"쓰는 글자 '{d.Use}' 는 {PropChars} 중 하나"));
            else if (d.Sol == d.Use) errors.Add(new(path, "원본 글자와 쓰는 글자가 같다 — 차이가 아니다"));
            if (!tileset.Props.TryGetValue(d.Tile, out var text))
                errors.Add(new($"{path}.tile", $"메가타일 {d.Tile} 이 성질 표에 없다"));
            else if (d.Sub is >= 0 and <= 3 && text.Length == 4 && d.Use.Length == 1 && text[d.Sub] != d.Use[0])
                errors.Add(new($"{path}.use", $"쓰는 글자 '{d.Use}' 가 성질 표 메가타일 {d.Tile} 하위 칸 {d.Sub} 의 '{text[d.Sub]}' 와 다르다"));
        }
    }

    /// <summary>이 타일셋이 이름 붙인 메가타일 번호(floor ∪ borders ∪ gates ∪ pieces).</summary>
    public static SortedSet<int> NamedTiles(TilesetDef tileset)
    {
        var set = new SortedSet<int>(tileset.Floor);
        foreach (var b in tileset.Borders.Values)
            foreach (var t in new[] { b.CornerN, b.EdgeNe, b.CornerE, b.EdgeSe, b.CornerS, b.EdgeSw, b.CornerW, b.EdgeNw }) set.Add(t);
        foreach (var gs in tileset.Gates.Values)
            foreach (var side in Enum.GetValues<GateSide>())
                if (gs.Get(side) is { } g)
                {
                    set.Add(g.Open);
                    if (g.EndA is { } a) set.Add(a);
                    if (g.EndB is { } b) set.Add(b);
                }
        foreach (var p in tileset.Pieces.Values) set.Add(p);
        return set;
    }

    /// <summary>성질 글자 4개(<see cref="PropChars"/>) → 하위 칸 성질(TIL 순서). 틀리면 false. <see cref="ArenaGrid.PropChar"/> 의 역.</summary>
    public static bool TryParseProps(string text, Span<CellFlags> cells)
    {
        if (text.Length != 4 || cells.Length < 4) return false;
        for (var k = 0; k < 4; k++)
        {
            var sol = PropChars.IndexOf(text[k]);
            if (sol < 0) return false;
            cells[k] = ArenaGrid.FromSol(sol);
        }
        return true;
    }

    public static string SideName(GateSide side) => side switch
    {
        GateSide.Nw => "nw",
        GateSide.Ne => "ne",
        GateSide.Se => "se",
        _ => "sw",
    };

    /// <summary>변 위 k 번째 메가타일. nw = 왼쪽 열(x 0), ne = 윗줄(y 0), se = 오른쪽 열, sw = 아랫줄.</summary>
    public static (int Mx, int My) SideCell(GateSide side, int k, int m) => side switch
    {
        GateSide.Nw => (0, k),
        GateSide.Ne => (k, 0),
        GateSide.Se => (m - 1, k),
        _ => (k, m - 1),
    };

    private static string ComputeLayoutHash(MapDef map, TilesetDef ts, int[] mega)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder(mega.Length * 4 + 512);
        sb.Append("layout/1|tileset=").Append(ts.Id).Append("|palette=").Append(map.Palette)
          .Append("|m=").Append(map.Size.ToString(inv)).Append("|grid=");
        foreach (var t in mega) sb.Append(t.ToString(inv)).Append(',');

        sb.Append("|column_height=").Append(ts.ColumnHeight.ToString(inv)).Append("|floor=");
        foreach (var t in ts.Floor) sb.Append(t.ToString(inv)).Append(',');
        sb.Append("|borders=");
        foreach (var (name, b) in ts.Borders.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            // AppendJoin(int…) 은 현재 문화권으로 쓴다(음수 부호가 U+2212 인 문화권이 있다) → 불변 문화 문자열로 넘긴다
            int[] edge = [b.CornerN, b.EdgeNe, b.CornerE, b.EdgeSe, b.CornerS, b.EdgeSw, b.CornerW, b.EdgeNw];
            sb.Append(name).Append(':').AppendJoin(',', edge.Select(t => t.ToString(inv))).Append(';');
        }
        sb.Append("|gates=");
        foreach (var (name, gs) in ts.Gates.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            sb.Append(name).Append(':');
            foreach (var side in Enum.GetValues<GateSide>())
                if (gs.Get(side) is { } g)
                    sb.Append(SideName(side)).Append('=').Append(g.Open.ToString(inv)).Append('/')
                      .Append(g.EndA?.ToString(inv) ?? "-").Append('/').Append(g.EndB?.ToString(inv) ?? "-").Append(',');
            sb.Append(';');
        }
        sb.Append("|pieces=");
        foreach (var (name, t) in ts.Pieces.OrderBy(p => p.Key, StringComparer.Ordinal))
            sb.Append(name).Append('=').Append(t.ToString(inv)).Append(';');
        sb.Append("|props=");
        foreach (var (t, text) in ts.Props.OrderBy(p => p.Key))
            sb.Append(t.ToString(inv)).Append('=').Append(text).Append(';');
        AppendDiffs(sb.Append("|sol_known_diffs="), ts.SolKnownDiffs);
        AppendDiffs(sb.Append("|design_diffs="), ts.DesignDiffs);
        return StableHash.Of(sb.ToString()).ToString("x8", inv);
    }

    private static void AppendDiffs(StringBuilder sb, IReadOnlyList<SolDiffDef> diffs)
    {
        foreach (var d in diffs)
            sb.Append(d.Tile.ToString(CultureInfo.InvariantCulture)).Append('/').Append(d.Sub.ToString(CultureInfo.InvariantCulture))
              .Append('/').Append(d.Sol).Append('/').Append(d.Use).Append('/').Append(d.Why).Append(';');
    }
}
