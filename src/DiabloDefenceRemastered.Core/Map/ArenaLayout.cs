using System.Globalization;
using System.Text;

namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 배치·타일셋·방 검사 오류. <see cref="Path"/> 는 그 정의 원소 기준(<c>$.layout.gates[1].side</c>).
/// <see cref="Warning"/>(v0.6.1): 참이면 경고(<c>closed_door</c>·<c>arch</c> 를 <c>decor_why</c> 없이 놓음, 13.5-19) — <c>MapDataReader</c> 가 <c>DataSeverity.Warning</c> 으로 바꾼다.
/// <see cref="ArenaLayout.TryExpand"/>·<see cref="ArenaLayout.ValidateTileset"/>·<see cref="ArenaLayout.ValidateRooms"/> 가 같은 형을 쓴다.
/// </summary>
public readonly record struct LayoutError(string Path, string Message, bool Warning = false)
{
    public override string ToString() => Warning ? $"{Path}: (경고) {Message}" : $"{Path}: {Message}";
}

/// <summary>
/// 배치 풀기 <c>Expand(MapDef, TilesetDef) → ArenaGrid</c> (설계서 6.11, 계약 WP1b, 결정적). 가져오기 도구(그림)와 WP2(충돌)가 같은 함수를 쓴다
/// → 그림을 구운 배치와 충돌을 만든 배치가 같은 코드에서 나온다(1.2).
/// <list type="number">
/// <item>모든 칸 = 바닥: <c>floor[StableHash(맵 ID, mx, my) % 개수]</c>.</item>
/// <item>테두리: 네 모서리·네 변을 <c>borders[border]</c> 번호로(둘레 표시).</item>
/// <item>문: 변 가운데(또는 <c>at</c>) 메가타일 2개를 <c>open</c> 으로, 양옆 한 칸씩을 <c>end_a</c>/<c>end_b</c> 로(끝 조각은 둘레 표시).</item>
/// <item><c>place</c>: 조각 번호를 <c>at</c> 의 [mx,my] 마다, 또는 <c>line</c>(가로·세로 줄)에, 또는 (v0.6) <c>grid</c> 블록(<c>origin</c> 부터 줄 = y, 칸 = 공백으로 나눈 이름, <c>.</c> 건너뜀).
/// <c>over_border</c> 놓기는 문 칸이 아닌 테두리 칸을 덮을 수 있다 — 둘레 표시는 남고, 원래 테두리 조각이 막던 비트를 풀면 오류(v0.6.1).</item>
/// <item>결과: 번호 격자 + 성질 표로 푼 하위 칸 성질 + 둘레 표시 + <c>LayoutHash</c>.</item>
/// </list>
/// 검사(13.5-4 배치 쪽): 배치가 M×M 안, 쓰는 메가타일이 모두 성질 표에 있음, 문 칸이 비어 있음, 놓은 조각이 테두리(<c>over_border</c> 가 아니면)·문·다른 조각과 겹치지 않음,
/// 놓기마다 <c>at</c>·<c>line</c>·<c>grid</c> 중 하나. 타일셋 자체 검사(13.5-12)는 <see cref="ValidateTileset"/>, 안쪽 방 검사(13.5-19)는 <see cref="ValidateRooms"/>.
/// </summary>
public static class ArenaLayout
{
    /// <summary>
    /// 성질 글자 8개(v0.5, 설계서 6.11 표). 글자 번호 = 원본 <c>sol &amp; 7</c>(bit0 걷기, bit1 시야, bit2 투사체) → 가져오기 도구는 <c>PropChars[sol &amp; 7]</c> 로 적는다.
    /// 대문자 = 시야 막힘, 소문자 = 시야 통과. v0.4 의 <c>W</c>(걷기만)·<c>M</c>(투사체만)은 <c>w</c>·<c>m</c> 이 되었고, 지금 <c>W</c>·<c>M</c> 은 시야도 막는다.
    /// 하위 칸 4개를 TIL 순서로 적는다.
    /// </summary>
    public const string PropChars = ".wLWmxMX";

    /// <summary>변형 해시 소금(바닥 섞기와 다른 무늬).</summary>
    private const uint AccentSalt = 0xACCE57EDu;

    public static ArenaGrid Expand(MapDef map) => Expand(map, map.Tileset.Def);

    /// <summary>풀고, 오류가 있으면 모두 담은 <see cref="InvalidDataException"/>. 로더가 이미 검사한 정의에 쓴다.</summary>
    public static ArenaGrid Expand(MapDef map, TilesetDef tileset)
    {
        var errors = new List<LayoutError>();
        var grid = TryExpand(map, tileset, errors);
        if (grid is null || errors.Any(e => !e.Warning))
            throw new InvalidDataException($"맵 '{map.Id}' 배치를 풀 수 없다: " + string.Join(" / ", errors));
        return grid;
    }

    /// <summary>풀기. 오류를 모두 <paramref name="errors"/> 에 넣고, 하나라도 있으면 null.</summary>
    public static ArenaGrid? TryExpand(MapDef map, TilesetDef tileset, ICollection<LayoutError> errors)
    {
        var before = errors.Count(e => !e.Warning);
        var m = map.Size;
        if (m < 3) { errors.Add(new("$.size", $"메가타일 수는 3 이상이어야 한다({m})")); return null; }
        if (tileset.Floor.Count == 0) { errors.Add(new("$.tileset", $"타일셋 '{tileset.Id}' 에 floor 가 없다")); return null; }
        if (tileset.ColumnHeight < 32) { errors.Add(new("$.tileset", $"타일셋 '{tileset.Id}' 의 column_height 는 32 이상이어야 한다")); return null; }

        var layout = map.Layout;
        var mega = new int[m * m];
        var source = new string[m * m];
        var boundary = new bool[m * m];
        var claimed = new bool[m * m];   // 문·조각이 차지한 칸(겹침 검사)
        var gateCell = new bool[m * m];  // 문(열린 칸·끝 조각) 칸 — over_border 도 덮지 못한다

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
                gateCell[i] = true;
                if (!s.Boundary) openCells.Add((g, mx, my));
            }
        }

        // 4. 안쪽 조각
        Span<CellFlags> was = stackalloc CellFlags[4];
        Span<CellFlags> now = stackalloc CellFlags[4];
        for (var p = 0; p < layout.Place.Count; p++)
        {
            var place = layout.Place[p];
            if (PlacementCells(place, $"$.layout.place[{p}]", tileset, errors) is not { } cells) continue;

            foreach (var (c, piece, tile, cellPath) in cells)
            {
                if ((uint)c.X >= (uint)m || (uint)c.Y >= (uint)m) { errors.Add(new(cellPath, $"{c} 가 {m}×{m} 밖이다")); continue; }
                var i = c.Y * m + c.X;
                var onBorder = c.X == 0 || c.Y == 0 || c.X == m - 1 || c.Y == m - 1;
                if (onBorder && !place.OverBorder) { errors.Add(new(cellPath, $"{c} 가 테두리와 겹친다(둘레 자리에 겹쳐 놓으려면 over_border)")); continue; }
                if (onBorder && gateCell[i]) { errors.Add(new(cellPath, $"{c} 는 문 칸이다 — over_border 도 문 칸은 덮지 않는다")); continue; }
                if (claimed[i]) { errors.Add(new(cellPath, $"{c} 에 조각이 이미 있다")); continue; }

                // over_border: 덮어도 막힘은 줄지 않는다(v0.6.1) — 원래 테두리 조각이 막던 걷기·시야·투사체 비트를 덮는 조각도 막아야 한다.
                // 성질 표에 없는 번호는 5단계가 따로 알린다
                if (onBorder
                    && tileset.Props.TryGetValue(mega[i], out var wasText) && TryParseProps(wasText, was)
                    && tileset.Props.TryGetValue(tile, out var nowText) && TryParseProps(nowText, now))
                {
                    for (var k = 0; k < 4; k++)
                    {
                        var lost = was[k] & ~now[k] & (CellFlags.BlocksWalk | CellFlags.BlocksSight | CellFlags.BlocksProjectiles);
                        if (lost != CellFlags.None)
                            errors.Add(new(cellPath, $"테두리 ({c.X},{c.Y}) 하위 칸 {SubName(k)} 의 {BitNames(lost)} 막힘을 {piece} 가 풀었다(over_border 는 둘레 막힘을 줄이지 않는다)"));
                    }
                }

                // 덮인 테두리 칸의 둘레 표시(boundary)는 그대로 둔다(4.2)
                mega[i] = tile;
                source[i] = $"pieces.{piece}";
                claimed[i] = true;
            }
        }

        // 4b. 변형(accents): 다 깐 칸을 같은 성질의 장식판으로 — 문 칸은 그대로, 바닥 섞기와 다른 해시
        if (tileset.Accents.Count > 0)
        {
            var accentHash = mapHash ^ AccentSalt;
            for (var my = 0; my < m; my++)
                for (var mx = 0; mx < m; mx++)
                {
                    var i = my * m + mx;
                    if (gateCell[i] || !tileset.Accents.TryGetValue(mega[i], out var accent) || accent.With.Count == 0) continue;
                    var h = StableHash.Of(accentHash, (uint)mx, (uint)my);
                    if ((h & 0xFFFF) / 65536f >= accent.Chance) continue;
                    source[i] += $" → accents.{mega[i]}";
                    mega[i] = accent.With[(int)((h >> 16) % (uint)accent.With.Count)];
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

        if (errors.Count(e => !e.Warning) > before) return null;
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

        // v0.6: marks 의 번호는 이름 붙인 번호 안(13.5-12 — 성질 표 키와 같은 집합)
        foreach (var (mark, tiles) in tileset.Marks.OrderBy(p => p.Key))
        {
            if (tiles.Count == 0) errors.Add(new($"$.marks.{MarkName(mark)}", "번호가 하나도 없다"));
            for (var k = 0; k < tiles.Count; k++)
                if (!named.Contains(tiles[k]))
                    errors.Add(new($"$.marks.{MarkName(mark)}[{k}]", $"메가타일 {tiles[k]} 은 이 타일셋이 이름 붙인 번호(floor·borders·gates·pieces)가 아니다"));
        }

        if (tileset.Mirror is { } mirror) ValidateMirror(tileset, mirror, errors);

        // 변형: 원래 번호는 변형이 아닌 이름 붙인 번호, 확률 0..1, 장식판은 원래 번호와 성질이 같음(그림만 바뀐다)
        foreach (var (tile, accent) in tileset.Accents.OrderBy(p => p.Key))
        {
            var path = $"$.accents.{tile}";
            if (!NamedTiles(tileset, withAccents: false).Contains(tile))
                errors.Add(new(path, $"메가타일 {tile} 은 floor·borders·gates·pieces 가 이름 붙인 번호가 아니다"));
            if (accent.Chance is < 0f or > 1f || float.IsNaN(accent.Chance))
                errors.Add(new($"{path}.chance", $"확률 {accent.Chance} 는 0..1 이다"));
            if (accent.With.Count == 0) errors.Add(new($"{path}.with", "장식판이 하나도 없다"));
            tileset.Props.TryGetValue(tile, out var baseText);
            for (var k = 0; k < accent.With.Count; k++)
            {
                var alt = accent.With[k];
                if (tileset.Accents.ContainsKey(alt))
                    errors.Add(new($"{path}.with[{k}]", $"장식판 {alt} 에 다시 변형이 있다"));
                if (baseText is not null && tileset.Props.TryGetValue(alt, out var altText) && altText != baseText)
                    errors.Add(new($"{path}.with[{k}]", $"장식판 {alt} 성질 '{altText}' 가 {tile} 의 '{baseText}' 와 다르다(변형은 그림만 바꾼다)"));
            }
        }
    }

    /// <summary>
    /// 거울판 짝 표 검사(v0.8 → v0.8.1, 13.5-12 — 6.11 "거울판"). 비교는 <b>최종 성질</b>(<c>props</c>)만 본다 — 원본 SOL·보정 목록과 무관.
    /// <list type="bullet">
    /// <item>짝: 이름이 <c>pieces</c> 에 있음, 한 이름은 한 번만, 짝은 이름 둘, (a, b) 마다 <c>props[b]</c> = T(<c>props[a]</c>), self 는 <c>props[a]</c> = T(<c>props[a]</c>).</item>
    /// <item>자리 짝(v0.8.1): 테두리마다 <c>edge_nw</c> ↔ <c>edge_ne</c>·<c>edge_sw</c> ↔ <c>edge_se</c>·<c>corner_w</c> ↔ <c>corner_e</c> 는 전치, <c>corner_n</c>·<c>corner_s</c> 는 자기 전치.
    /// 문은 변 짝 nw ↔ ne·sw ↔ se 의 <c>open</c>·<c>end_a</c>·<c>end_b</c> 끼리 같은 비교(한쪽만 있으면 오류).</item>
    /// </list>
    /// </summary>
    private static void ValidateMirror(TilesetDef ts, TilesetMirrorDef mirror, ICollection<LayoutError> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        bool Known(string name, string path)
        {
            if (!seen.Add(name)) errors.Add(new(path, $"짝 표에 조각 {name} 이 두 번 있다"));
            if (ts.Pieces.ContainsKey(name)) return true;
            errors.Add(new(path, $"짝 표의 조각 {name} 이 pieces 에 없다"));
            return false;
        }

        for (var i = 0; i < mirror.Pairs.Count; i++)
        {
            var pair = mirror.Pairs[i];
            var path = $"$.mirror.pairs[{i}]";
            if (pair.Count != 2) { errors.Add(new(path, $"짝은 조각 이름 둘이다({pair.Count}개)")); continue; }
            var okA = Known(pair[0], path);
            var okB = Known(pair[1], path);
            if (okA && okB) CompareTransposed(ts, ts.Pieces[pair[0]], ts.Pieces[pair[1]], $"짝 {pair[0]} ↔ {pair[1]}", path, errors);
        }
        for (var i = 0; i < mirror.Self.Count; i++)
        {
            var name = mirror.Self[i];
            var path = $"$.mirror.self[{i}]";
            if (Known(name, path)) CompareTransposed(ts, ts.Pieces[name], ts.Pieces[name], $"자기 짝 {name}", path, errors);
        }

        foreach (var (name, b) in ts.Borders.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var path = $"$.borders.{name}";
            CompareTransposed(ts, b.EdgeNw, b.EdgeNe, "자리 짝 edge_nw ↔ edge_ne", path, errors);
            CompareTransposed(ts, b.EdgeSw, b.EdgeSe, "자리 짝 edge_sw ↔ edge_se", path, errors);
            CompareTransposed(ts, b.CornerW, b.CornerE, "자리 짝 corner_w ↔ corner_e", path, errors);
            CompareTransposed(ts, b.CornerN, b.CornerN, "자리 짝 corner_n(자기)", path, errors);
            CompareTransposed(ts, b.CornerS, b.CornerS, "자리 짝 corner_s(자기)", path, errors);
        }
        foreach (var (name, gs) in ts.Gates.OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (var (a, b) in new[] { (GateSide.Nw, GateSide.Ne), (GateSide.Sw, GateSide.Se) })
            {
                var path = $"$.gates.{name}";
                var (ga, gb) = (gs.Get(a), gs.Get(b));
                var label = $"자리 짝 문 {SideName(a)} ↔ {SideName(b)}";
                if (ga is null && gb is null) continue;
                if (ga is null || gb is null) { errors.Add(new(path, $"{label}: 한쪽 변에만 문이 있다 — 거울판에서 그 문이 없는 변으로 간다")); continue; }
                CompareTransposed(ts, ga.Open, gb.Open, label + " open", path, errors);
                CompareEnd(ga.EndA, gb.EndA, "end_a");
                CompareEnd(ga.EndB, gb.EndB, "end_b");

                void CompareEnd(int? x, int? y, string end)
                {
                    if (x is null && y is null) return;
                    if (x is null || y is null) { errors.Add(new(path, $"{label} {end}: 한쪽 변에만 끝 조각이 있다")); return; }
                    CompareTransposed(ts, x.Value, y.Value, $"{label} {end}", path, errors);
                }
            }
    }

    /// <summary><c>props[b]</c> = T(<c>props[a]</c>) 인지. 성질 표에 없거나 형식이 틀린 번호는 다른 검사가 낸다(건너뜀).</summary>
    private static void CompareTransposed(TilesetDef ts, int a, int b, string what, string path, ICollection<LayoutError> errors)
    {
        if (!ts.Props.TryGetValue(a, out var pa) || !ts.Props.TryGetValue(b, out var pb) || pa.Length != 4 || pb.Length != 4) return;
        var want = MapMirror.TransposeProps(pa);
        if (!string.Equals(pb, want, StringComparison.Ordinal))
            errors.Add(new(path, $"{what}: 메가타일 {b} 성질 '{pb}' 가 {a} '{pa}' 의 전치 '{want}' 와 다르다 — 거울판의 충돌·시야가 원판의 전치와 달라진다"));
    }

    /// <summary>
    /// 안쪽 방 검사(13.5-19, v0.6 — 펼친 뒤 부른다, 자원·반경·격자 없이 성질 글자만 본다). 오류와 경고를 <paramref name="errors"/> 에 넣는다.
    /// <list type="bullet">
    /// <item><c>open_center</c> 가 비지 않고 테두리 안, 그 안 메가타일은 바닥 또는 <c>marks.pillar</c>, 기둥 ≤ 2(오류).</item>
    /// <item><c>marks.closed_door</c>·<c>arch</c> 번호를 놓은 놓기에 <c>decor_why</c> 가 없으면 경고.</item>
    /// <item>방 기준점(<c>anchors</c> 의 <c>room_*</c>)마다 격자 안·그 칸 걷기 막힘 없음(오류).</item>
    /// <item>(v0.6.3) 방 영역 <c>room_areas</c>: <c>room_*</c> 기준점마다 하나, 키는 <c>room_*</c> 기준점, 맵 안·서로 겹치지 않음, 기준점이 영역 안,
    /// 방 바닥(<see cref="RoomFloors"/>)에서 영역 밖으로 이어지는 걷기 칸이 있음(오류).</item>
    /// <item>(v0.6.1) ① 갇힌 바닥 — 걷기 막힘 없는 칸을 주인공 시작 칸에서 4이웃으로 채워 못 닿는 칸. ② 하위 1칸 틈 — 걷기 막힘 없는 칸인데 좌우 둘 다 또는 위아래 둘 다 걷기 막힘(격자 밖 포함).
    /// 둘 다 칸 목록을 메가타일로 묶어 한 줄씩(오류).</item>
    /// <item>(v0.7, 13.5-23 ③ 맵 쪽) <c>room_kinds</c> 가 빈 목록이 아닌 방 맵: 문이 정확히 <c>gate_nw</c>(nw)·<c>gate_ne</c>(ne)·<c>gate_se</c>(se)·<c>gate_sw</c>(sw) 넷, 입구도 그 넷, 기준점 <c>center</c>.
    /// 작은 방(N ≤ <see cref="RoomMapRules.SmallRoomMaxN"/>)은 문 하나인 방 바닥 없음, 문 넷 진입점에서 <see cref="RoomMapRules.DoorClearance"/> 안에 둘레가 아닌 걷기 막힘 칸 없음(오류).
    /// (v0.8.1) 입구마다 진입점에서 가장 가까운 문 틈 칸이 같은 ID 문의 칸(오류 — 거울판에서 입구 ID 를 그대로 두면 다른 문 앞에 놓인다).</item>
    /// </list>
    /// 거울판(v0.8)도 원판과 같은 검사를 돈다 — <c>MapDataReader</c> 가 <c>MapMirror.Of</c> 결과로 다시 부른다.
    /// <c>over_border</c> 의 막힘 줄어듦은 <see cref="TryExpand"/> 가 낸다. WP2 의 13.5-4(80px)·13.5-5(반경 14 격자)는 이 위의 정밀 검사로 남는다.
    /// </summary>
    public static void ValidateRooms(MapDef map, ArenaGrid grid, ICollection<LayoutError> errors)
    {
        var ts = grid.Tileset;
        var m = grid.MegaSize;

        // open_center
        if (map.OpenCenter is { } oc)
        {
            if (oc.IsEmpty || oc.X0 < 1 || oc.Y0 < 1 || oc.X1 > m - 1 || oc.Y1 > m - 1)
                errors.Add(new("$.open_center", $"open_center {oc} 는 비지 않고 테두리 안(메가 1..{m - 2}, 끝 미포함 {m - 1})이어야 한다"));
            else
            {
                var floors = FloorTiles(ts);
                var pillars = Marked(ts, TileMark.Pillar);
                var pillarCount = 0;
                for (var my = oc.Y0; my < oc.Y1; my++)
                    for (var mx = oc.X0; mx < oc.X1; mx++)
                    {
                        var tile = grid.MegaTile(mx, my);
                        if (floors.Contains(tile)) continue;
                        if (pillars.Contains(tile)) { pillarCount++; continue; }
                        errors.Add(new("$.open_center", $"open_center 안 메가 ({mx},{my}) 의 메가타일 {tile} 은 바닥도 기둥(marks.pillar)도 아니다"));
                    }
                if (pillarCount > 2)
                    errors.Add(new("$.open_center", $"open_center 안 기둥이 {pillarCount}개다 — 2개까지(무리를 끌고 도는 가운데를 넓게 둔다)"));
            }
        }

        // 닫힌 문·아치 = 문으로 쓰면 안 되는 조각(경고, decor_why 면 끔)
        var noDoor = Marked(ts, TileMark.ClosedDoor);
        noDoor.UnionWith(Marked(ts, TileMark.Arch));
        if (noDoor.Count > 0)
            for (var p = 0; p < map.Layout.Place.Count; p++)
            {
                var place = map.Layout.Place[p];
                if (!string.IsNullOrWhiteSpace(place.DecorWhy)) continue;
                if (PlacementCells(place, $"$.layout.place[{p}]", ts, null) is not { } cells) continue;
                var names = cells.Where(c => noDoor.Contains(c.Tile)).Select(c => c.Piece).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
                if (names.Count > 0)
                    errors.Add(new($"$.layout.place[{p}]",
                        $"조각 {string.Join(", ", names)} 는 marks 의 closed_door·arch 번호다 — 문으로 쓰면 안 된다(닫힌 문은 벽, 아치는 틈이 하위 1칸). 장식이면 decor_why 에 이유를 적는다",
                        Warning: true));
            }

        // 방 기준점
        foreach (var (id, at) in map.AnchorsTile.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            if (!id.StartsWith("room_", StringComparison.Ordinal)) continue;
            var c = at.Cell;
            if (!grid.IsInside(c.X, c.Y)) errors.Add(new($"$.anchors.{id}", $"방 기준점 {at} 가 {grid.Size}×{grid.Size} 격자 밖이다"));
            else if (!grid.IsWalkable(c.X, c.Y)) errors.Add(new($"$.anchors.{id}", $"방 기준점 {at} 의 칸 ({c.X},{c.Y}) 이 걷기 막힘이다(성질 '{grid.Prop(c.X, c.Y)}')"));
            if (!map.RoomAreas.ContainsKey(id))
                errors.Add(new("$.room_areas", $"방 기준점 {id} 의 영역(room_areas.{id})이 없다 — 방 바닥(방 체류 압박·방 보급·방 들르기)을 계산하지 못한다"));
        }

        // 방 영역(v0.6.3) — 키·범위·겹침, 기준점이 영역 안, 바닥에서 밖으로 나가는 길
        var roomFloors = RoomFloors.Compute(map, grid);
        var areas = map.RoomAreas.OrderBy(a => a.Key, StringComparer.Ordinal).ToList();
        for (var i = 0; i < areas.Count; i++)
        {
            var (id, area) = areas[i];
            var path = $"$.room_areas.{id}";
            if (!id.StartsWith("room_", StringComparison.Ordinal) || !map.AnchorsTile.TryGetValue(id, out var at))
            {
                errors.Add(new(path, $"room_areas 의 키 '{id}' 는 anchors 의 room_* 기준점이어야 한다"));
                continue;
            }
            if (area.IsEmpty || area.X0 < 0 || area.Y0 < 0 || area.X1 > m || area.Y1 > m)
            {
                errors.Add(new(path, $"방 영역 {area} 는 비지 않고 맵 안(메가 0..{m - 1}, 끝 미포함 {m})이어야 한다"));
                continue;
            }
            for (var j = i + 1; j < areas.Count; j++)
                if (area.Intersects(areas[j].Value))
                    errors.Add(new(path, $"방 영역 {area} 가 {areas[j].Key} 의 영역 {areas[j].Value} 와 겹친다 — 한 칸은 한 방 바닥에만 든다"));
            var c = at.Cell;
            if (!grid.IsInside(c.X, c.Y) || !grid.IsWalkable(c.X, c.Y)) continue;   // 위 기준점 검사가 이미 냄
            if (!RoomFloors.SubRect(area, grid.Size).Contains(c.X, c.Y))
            {
                errors.Add(new(path, $"방 기준점 {at} 가 방 영역 {area}(하위 칸 {area.X0 * 2}..{area.X1 * 2 - 1}, {area.Y0 * 2}..{area.Y1 * 2 - 1}) 밖이다"));
                continue;
            }
            if (roomFloors.TryGet(id, out var floor) && floor.Doors == 0)
                errors.Add(new(path, $"방 바닥({floor.Cells.Count}칸)에서 영역 밖으로 이어지는 걷기 칸이 없다 — 문이 없는 방"));
        }

        // ① 갇힌 바닥 — 주인공 시작 칸에서 4이웃 채우기
        var n = grid.Size;
        var start = map.HeroStart.Tile.Cell;
        if (grid.IsInside(start.X, start.Y))   // 격자 밖이면 MapDataReader 가 따로 오류
        {
            if (!grid.IsWalkable(start.X, start.Y))
                errors.Add(new("$.hero_start.tile", $"주인공 시작 칸 ({start.X},{start.Y}) 이 걷기 막힘이다 — 갇힌 바닥 검사를 하지 못했다"));
            else
            {
                var reached = new bool[n * n];
                var queue = new Queue<int>();
                reached[start.Y * n + start.X] = true;
                queue.Enqueue(start.Y * n + start.X);
                while (queue.Count > 0)
                {
                    var i = queue.Dequeue();
                    var (x, y) = (i % n, i / n);
                    Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
                }
                void Visit(int x, int y)
                {
                    if (!grid.IsWalkable(x, y) || reached[y * n + x]) return;
                    reached[y * n + x] = true;
                    queue.Enqueue(y * n + x);
                }

                var trapped = new List<Int2>();
                for (var ty = 0; ty < n; ty++)
                    for (var tx = 0; tx < n; tx++)
                        if (grid.IsWalkable(tx, ty) && !reached[ty * n + tx]) trapped.Add(new(tx, ty));
                ReportByMega(trapped, m, "갇힌 바닥", "주인공 시작에서 4이웃으로 닿지 않는다(벽 두 겹 사이 띠·막힌 방)", errors);
            }
        }

        // ② 하위 1칸 틈 — 좌우 둘 다 또는 위아래 둘 다 걷기 막힘(격자 밖 포함)
        var gaps = new List<Int2>();
        for (var ty = 0; ty < n; ty++)
            for (var tx = 0; tx < n; tx++)
            {
                if (!grid.IsWalkable(tx, ty)) continue;
                if ((!grid.IsWalkable(tx - 1, ty) && !grid.IsWalkable(tx + 1, ty)) || (!grid.IsWalkable(tx, ty - 1) && !grid.IsWalkable(tx, ty + 1)))
                    gaps.Add(new(tx, ty));
            }
        ReportByMega(gaps, m, "하위 1칸 틈", "좌우 둘 다 또는 위아래 둘 다 걷기 막힘(벽 맞물림 구멍·벽 두 겹 사이 띠·아치 틈 — 꺾이는 자리는 모서리 조각)", errors);

        // (v0.7) 13.5-23 ③ 방 맵
        if (map.RoomKinds.Count > 0) ValidateRoomMap(map, grid, roomFloors, errors);
    }

    /// <summary>13.5-23 ③ 맵 쪽(v0.7, 6.11 "방 맵"). 이벤트의 <c>entry:</c> 이름이 맵이 바뀌어도 같게 문 넷·<c>center</c> 를 요구하고, 작은 방은 막다른 방·문 앞 막힘을 막는다.</summary>
    private static void ValidateRoomMap(MapDef map, ArenaGrid grid, RoomFloors roomFloors, ICollection<LayoutError> errors)
    {
        var gates = map.Layout.Gates;
        var gatesOk = gates.Count == RoomMapRules.Gates.Count;
        foreach (var (id, side) in RoomMapRules.Gates)
            if (!gates.Any(g => string.Equals(g.Id, id, StringComparison.Ordinal) && g.Side == side)) gatesOk = false;
        if (!gatesOk)
            errors.Add(new("$.layout.gates", $"방 맵(room_kinds)의 문은 정확히 gate_nw(nw)·gate_ne(ne)·gate_se(se)·gate_sw(sw) 넷이어야 한다 — 지금 {string.Join(", ", gates.Select(g => $"{g.Id}({SideName(g.Side)})"))}"));
        var entriesOk = map.Entries.Count == RoomMapRules.Gates.Count;
        foreach (var (id, _) in RoomMapRules.Gates)
            if (!map.Entries.Any(e => string.Equals(e.Id, id, StringComparison.Ordinal))) entriesOk = false;
        if (!entriesOk)
            errors.Add(new("$.entries", $"방 맵(room_kinds)의 입구는 문 넷과 같은 ID 넷이어야 한다 — 지금 {string.Join(", ", map.Entries.Select(e => e.Id))}"));
        if (!map.AnchorsTile.ContainsKey(RoomMapRules.CenterAnchor))
            errors.Add(new("$.anchors", $"방 맵(room_kinds)에 기준점 {RoomMapRules.CenterAnchor} 가 없다 — 큰 적 길 검사(13.5-5)의 목적지(v0.8 — 보상 자리는 주인공 앞)"));

        // (v0.8.1) 입구와 문이 같은 자리: 진입점에서 가장 가까운 문 틈 칸(칸 마름모 중심 거리, 같으면 문 순서 앞)이 같은 ID 문의 칸
        var zones = GateZones.Compute(map, grid);
        for (var e = 0; e < map.Entries.Count; e++)
        {
            var entry = map.Entries[e];
            var at = grid.Iso.ToWorld(entry.InsideTile);
            string? nearest = null;
            var best = float.MaxValue;
            foreach (var g in gates)
            {
                if (!zones.TryGetValue(g.Id, out var zone)) continue;
                foreach (var c in zone.Cells)
                {
                    var d = Vector2.DistanceSquared(grid.Iso.CellCenter(c.X, c.Y), at);
                    if (d < best) { best = d; nearest = g.Id; }
                }
            }
            if (nearest is not null && !string.Equals(nearest, entry.Id, StringComparison.Ordinal))
                errors.Add(new($"$.entries[{e}]", $"입구 {entry.Id} 가 문 {nearest} 앞에 있음(진입점 {entry.InsideTile} 에서 가장 가까운 문 틈 칸) — 입구 ID 는 그 앞 문의 ID 여야 한다"));
        }

        if (grid.Size > RoomMapRules.SmallRoomMaxN) return;
        foreach (var floor in roomFloors.Rooms)
            if (floor.OneDoor)
                errors.Add(new($"$.room_areas.{floor.Id}", $"작은 방(N {grid.Size} ≤ {RoomMapRules.SmallRoomMaxN})에 문 하나인 방 바닥 {floor.Id} 가 있다 — 막다른 방(기획 10.8)"));
        var n = grid.Size;
        for (var e = 0; e < map.Entries.Count; e++)
        {
            var entry = map.Entries[e];
            var at = grid.Iso.ToWorld(entry.InsideTile);
            var blocked = new List<Int2>();
            for (var ty = 0; ty < n; ty++)
                for (var tx = 0; tx < n; tx++)
                {
                    if (grid.IsWalkable(tx, ty) || grid.IsBoundary(tx, ty)) continue;
                    if (Vector2.Distance(grid.Iso.CellCenter(tx, ty), at) < RoomMapRules.DoorClearance) blocked.Add(new(tx, ty));
                }
            if (blocked.Count > 0)
                errors.Add(new($"$.entries[{e}]", $"작은 방 진입점 {entry.Id} {entry.InsideTile} 에서 {RoomMapRules.DoorClearance}px 안에 둘레가 아닌 걷기 막힘 칸 {blocked.Count}개: {string.Join(" ", blocked.Take(8).Select(c => $"({c.X},{c.Y})"))}"));
        }
    }

    /// <summary>칸 목록을 메가타일(행 우선 순서)로 묶어 한 줄씩.</summary>
    private static void ReportByMega(List<Int2> cells, int m, string what, string why, ICollection<LayoutError> errors)
    {
        foreach (var group in cells.GroupBy(c => (c.Y / 2) * m + c.X / 2).OrderBy(g => g.Key))
        {
            var (mx, my) = (group.Key % m, group.Key / m);
            var list = string.Join(" ", group.Select(c => $"({c.X},{c.Y})"));
            errors.Add(new("$.layout", $"{what} — 메가 ({mx},{my}) 하위 칸 {list}: {why}"));
        }
    }

    /// <summary>타일셋 <c>marks</c> 의 그 표시 번호(없으면 빈 집합).</summary>
    private static HashSet<int> Marked(TilesetDef ts, TileMark mark) =>
        ts.Marks.TryGetValue(mark, out var tiles) ? [.. tiles] : [];

    /// <summary><see cref="TileMark"/> 의 JSON 이름(오류 경로용).</summary>
    public static string MarkName(TileMark mark) => mark switch
    {
        TileMark.Pillar => "pillar",
        TileMark.Grate => "grate",
        TileMark.ClosedDoor => "closed_door",
        _ => "arch",
    };

    /// <summary>
    /// 놓기 하나의 칸·조각 이름·번호·오류 경로(파일 순서, 줄 = y). 모양 오류(at·line·grid 중 하나가 아님, grid 에 piece, 없는 이름 …)면
    /// <paramref name="errors"/> 에 넣고(null 이면 버림) null. 범위·테두리·겹침은 부르는 쪽이 본다.
    /// </summary>
    private static List<(Int2 Cell, string Piece, int Tile, string Path)>? PlacementCells(PiecePlacementDef place, string path, TilesetDef tileset, ICollection<LayoutError>? errors)
    {
        var kinds = (place.At.Count > 0 ? 1 : 0) + (place.Line is not null ? 1 : 0) + (place.Grid is not null ? 1 : 0);
        if (kinds == 0) { errors?.Add(new(path, "at·line·grid 중 하나가 있어야 한다")); return null; }
        if (kinds > 1) { errors?.Add(new(path, "at·line·grid 중 하나만 쓴다")); return null; }

        var cells = new List<(Int2, string, int, string)>();
        if (place.Grid is { } rows)
        {
            if (place.Piece is not null) { errors?.Add(new($"{path}.piece", "grid 놓기에는 piece 를 쓰지 않는다 — 칸마다 조각 이름을 적는다")); return null; }
            if (place.Origin is not { } origin) { errors?.Add(new($"{path}.origin", "grid 놓기에는 origin [mx, my] 가 있어야 한다")); return null; }
            var ok = true;
            for (var r = 0; r < rows.Count; r++)
            {
                var names = rows[r].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (var c = 0; c < names.Length; c++)
                {
                    if (names[c] == ".") continue;
                    if (!tileset.Pieces.TryGetValue(names[c], out var t))
                    {
                        errors?.Add(new($"{path}.grid[{r}]", $"{c + 1}번째 칸: 타일셋 '{tileset.Id}' 에 조각 '{names[c]}' 가 없다"));
                        ok = false;
                        continue;
                    }
                    cells.Add((new Int2(origin.X + c, origin.Y + r), names[c], t, $"{path}.grid[{r}]"));
                }
            }
            // 빈 목록·'.' 만 있는 grid 는 방 블록을 통째로 지운 오타일 수 있다 — 다른 모양 오류처럼 조용히 넘기지 않는다
            if (ok && cells.Count == 0) { errors?.Add(new($"{path}.grid", "grid 에 놓을 조각이 하나도 없다(빈 목록 또는 '.' 만)")); return null; }
            return ok ? cells : null;
        }

        if (place.Origin is not null) { errors?.Add(new($"{path}.origin", "origin 은 grid 놓기에만 쓴다")); return null; }
        if (place.Piece is not { } piece) { errors?.Add(new($"{path}.piece", "at·line 놓기에는 piece 가 있어야 한다")); return null; }
        if (!tileset.Pieces.TryGetValue(piece, out var tile))
        {
            errors?.Add(new($"{path}.piece", $"타일셋 '{tileset.Id}' 에 조각 '{piece}' 가 없다"));
            return null;
        }
        if (place.Line is { } line)
        {
            if (line.Count != 2) { errors?.Add(new($"{path}.line", "line 은 [[mx0,my0],[mx1,my1]] 두 점이다")); return null; }
            var (a, b) = (line[0], line[1]);
            if (a.X != b.X && a.Y != b.Y) { errors?.Add(new($"{path}.line", $"line 은 가로·세로 줄이어야 한다({a} → {b})")); return null; }
            var steps = Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
            var d = new Int2(Math.Sign(b.X - a.X), Math.Sign(b.Y - a.Y));
            for (var s = 0; s <= steps; s++) cells.Add((new Int2(a.X + d.X * s, a.Y + d.Y * s), piece, tile, $"{path}.line"));
        }
        else
            for (var j = 0; j < place.At.Count; j++) cells.Add((place.At[j], piece, tile, $"{path}.at[{j}]"));
        return cells;
    }

    /// <summary>하위 칸 번호(TIL 순서 0..3) → 이름.</summary>
    private static string SubName(int k) => k switch { 0 => "위-왼", 1 => "위-오른", 2 => "아래-왼", _ => "아래-오른" };

    /// <summary>막힘 비트 → "걷기·시야·투사체" 중 켜진 것.</summary>
    private static string BitNames(CellFlags f)
    {
        var parts = new List<string>(3);
        if ((f & CellFlags.BlocksWalk) != 0) parts.Add("걷기");
        if ((f & CellFlags.BlocksSight) != 0) parts.Add("시야");
        if ((f & CellFlags.BlocksProjectiles) != 0) parts.Add("투사체");
        return string.Join("·", parts);
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

    /// <summary>바닥 번호와 그 변형 장식판(<c>open_center</c>·원작 방 대조가 "바닥" 으로 본다).</summary>
    public static HashSet<int> FloorTiles(TilesetDef tileset)
    {
        var set = new HashSet<int>(tileset.Floor);
        foreach (var t in tileset.Floor)
            if (tileset.Accents.TryGetValue(t, out var a)) set.UnionWith(a.With);
        return set;
    }

    /// <summary>장식판 → 원래 번호(원작 방 대조가 변형 전 배치로 비교하도록).</summary>
    public static int AccentBase(TilesetDef tileset, int tile)
    {
        foreach (var (key, a) in tileset.Accents)
            if (a.With.Contains(tile)) return key;
        return tile;
    }

    /// <summary>이 타일셋이 이름 붙인 메가타일 번호(floor ∪ borders ∪ gates ∪ pieces ∪ 변형 장식판).</summary>
    public static SortedSet<int> NamedTiles(TilesetDef tileset) => NamedTiles(tileset, withAccents: true);

    private static SortedSet<int> NamedTiles(TilesetDef tileset, bool withAccents)
    {
        var set = new SortedSet<int>(tileset.Floor);
        if (withAccents)
            foreach (var a in tileset.Accents.Values) set.UnionWith(a.With);
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
        if (ts.Doors.Count > 0)
        {
            // 문 그림(덤 기둥)도 맵 그림에 구우므로 바뀌면 그림이 낡는다
            sb.Append("|doors=");
            foreach (var (side, cells) in ts.Doors.OrderBy(p => p.Key, StringComparer.Ordinal))
                foreach (var c in cells)
                    sb.Append(side).Append(':').Append(c.At.X.ToString(inv)).Append(',').Append(c.At.Y.ToString(inv)).Append('=')
                      .Append(c.Closed.ToString(inv)).Append('/').Append(c.Open.ToString(inv))
                      .Append('+').Append(c.OpenOverlay?.ToString(inv) ?? "-").Append(';');
        }
        if (ts.Accents.Count > 0)
        {
            sb.Append("|accents=");
            foreach (var (t, a) in ts.Accents.OrderBy(p => p.Key))
                sb.Append(t.ToString(inv)).Append(':').Append(a.Chance.ToString("R", inv)).Append('/').AppendJoin(',', a.With.Select(w => w.ToString(inv))).Append(';');
        }
        return StableHash.Of(sb.ToString()).ToString("x8", inv);
    }

    private static void AppendDiffs(StringBuilder sb, IReadOnlyList<SolDiffDef> diffs)
    {
        foreach (var d in diffs)
            sb.Append(d.Tile.ToString(CultureInfo.InvariantCulture)).Append('/').Append(d.Sub.ToString(CultureInfo.InvariantCulture))
              .Append('/').Append(d.Sol).Append('/').Append(d.Use).Append('/').Append(d.Why).Append(';');
    }
}
