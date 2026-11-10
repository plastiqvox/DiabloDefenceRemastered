namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 거울판 만들기 (v0.8, 설계서 6.11 "거울판" — 계약·구현 WP1f, 순수·결정적). 대각선 뒤집기: 하위 (tx,ty) ↔ (ty,tx), 메가 [mx,my] ↔ [my,mx] — 화면 좌우 뒤집기.
/// 그림을 뒤집지 않는다(원작 타일 그림은 빛 방향이 있다) — <b>조각 이름을 짝으로 바꿔 다시 깐다</b>(타일셋 <c>mirror</c> 짝 표).
/// <list type="bullet">
/// <item>바꾸는 것: <c>place</c> 의 <c>at</c>·<c>line</c> 좌표와 <c>origin</c> 뒤집기, <c>grid</c> 블록 전치(줄 ↔ 칸)·조각 이름 짝, 문의 변(nw ↔ ne, sw ↔ se)과 ID(<see cref="GateZones.MirrorGate"/>),
/// <c>entries</c> 좌표 뒤집기와 ID 짝(v0.8.1 — 입구 ID = 문 ID), <c>hero_start</c>·<c>anchors</c> 좌표(기준점 ID 는 그대로), <c>hero_start.facing</c> 좌우,
/// <c>open_center</c>·<c>room_areas</c> 사각형 <c>[x0,y0,x1,y1]</c> → <c>[y0,x0,y1,x1]</c>.</item>
/// <item>그대로: <c>border</c>·<c>palette</c>·<c>size</c>·<c>room_kinds</c>·<c>wall_fade</c>·문 <c>at</c>(변 위 번호는 뒤집어도 같다). 둘레·문·바닥 조각은 자리로 다시 고른다.</item>
/// <item>거울판 정의: <c>Id</c> = <c>&lt;원판&gt;@m</c>, <c>MirrorOf</c> = 원판 ID, <c>Art</c> = <c>&lt;원판 art&gt;@m</c>. 바닥 섞기 해시는 거울판 ID 로 하므로 바닥 무늬는 원판과 다르다(성질은 같음).</item>
/// </list>
/// 거울판을 아는 코드는 이 파일·<c>RunPlanner</c> ①·<c>RoomState.RemapEntry</c>/<c>RemapMarch</c> 셋뿐이다 — <c>ArenaLayout</c>·<c>GateZones</c>·<c>RoomFloors</c>·<c>MapArea</c> 는 거울판 <see cref="MapDef"/> 를 그대로 받는다.
/// </summary>
public static class MapMirror
{
    /// <summary>거울판 ID·그림 폴더 꼬리.</summary>
    public const string Suffix = "@m";

    /// <summary>거울판을 만든다. 짝 표에 없는 조각이 있으면 모든 오류를 담은 <see cref="InvalidDataException"/>.</summary>
    public static MapDef Of(MapDef map, TilesetDef tileset)
    {
        var errors = new List<LayoutError>();
        var mirrored = TryOf(map, tileset, errors);
        if (mirrored is null) throw new InvalidDataException($"맵 '{map.Id}' 의 거울판을 만들 수 없다: " + string.Join(" / ", errors));
        return mirrored;
    }

    /// <summary>
    /// 거울판을 만든다. 오류(짝 표에 없는 조각 — <c>거울판 — 조각 grate_h 의 짝이 타일셋 l1 짝 표에 없다</c>)는 <paramref name="errors"/> 에 모두 넣고 하나라도 있으면 null.
    /// 타일셋에 짝 표가 없으면 쓴 조각마다 오류다. 모양 오류(at·line·grid 가 둘)는 원판 배치 풀기가 내므로 여기서는 보지 않는다.
    /// </summary>
    public static MapDef? TryOf(MapDef map, TilesetDef tileset, ICollection<LayoutError> errors)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(tileset);
        var pairs = PieceMap(tileset);
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);

        string Piece(string name, string path)
        {
            if (pairs.TryGetValue(name, out var other)) return other;
            if (missing.Add(name)) paths[name] = path;
            return name;
        }

        var place = new List<PiecePlacementDef>(map.Layout.Place.Count);
        for (var p = 0; p < map.Layout.Place.Count; p++)
        {
            var src = map.Layout.Place[p];
            var path = $"$.layout.place[{p}]";
            if (src.Grid is { } rows)
            {
                var origin = src.Origin ?? Int2.Zero;
                place.Add(src with { Grid = TransposeGrid(rows, n => Piece(n, $"{path}.grid")), Origin = Swap(origin) });
                continue;
            }
            place.Add(src with
            {
                Piece = src.Piece is { } piece ? Piece(piece, $"{path}.piece") : null,
                At = [.. src.At.Select(Swap)],
                Line = src.Line is { } line ? [.. line.Select(Swap)] : null,
            });
        }

        foreach (var name in missing)
            errors.Add(new(paths[name], $"거울판 — 조각 {name} 의 짝이 타일셋 {tileset.Id} 짝 표에 없다(mirror.pairs·self)"));
        if (missing.Count > 0) return null;

        var anchors = new Dictionary<string, TileCoord>(map.AnchorsTile.Count, StringComparer.Ordinal);
        foreach (var (id, at) in map.AnchorsTile) anchors[id] = Swap(at);
        var areas = new Dictionary<string, RectI>(map.RoomAreas.Count, StringComparer.Ordinal);
        foreach (var (id, area) in map.RoomAreas) areas[id] = Swap(area);

        return map with
        {
            Id = map.Id + Suffix,
            Art = map.Art.Length == 0 ? "" : map.Art + Suffix,
            Layout = map.Layout with
            {
                Gates = [.. map.Layout.Gates.Select(g => g with { Id = GateZones.MirrorGate(g.Id), Side = GateZones.MirrorSide(g.Side) })],
                Place = place,
            },
            Entries = [.. map.Entries.Select(e => e with { Id = GateZones.MirrorGate(e.Id), OutsideTile = Swap(e.OutsideTile), InsideTile = Swap(e.InsideTile) })],
            HeroStart = map.HeroStart with { Tile = Swap(map.HeroStart.Tile), Facing = MirrorFacing(map.HeroStart.Facing) },
            AnchorsTile = anchors,
            OpenCenter = map.OpenCenter is { } oc ? Swap(oc) : null,
            RoomAreas = areas,
            Mirror = false,
            Mirrored = null,
            MirrorOf = map.Id,
        };
    }

    /// <summary>
    /// <paramref name="map"/> 에 <c>mirror: true</c> 와 거울판(<see cref="MapDef.Mirrored"/>)을 단 새 정의(읽기 <c>MapDataReader</c>·코드로 만드는 시험 정의가 같은 함수).
    /// 타일셋은 <c>map.Tileset</c> 이 연결돼 있어야 한다.
    /// </summary>
    public static MapDef WithMirror(MapDef map) => map with { Mirror = true, Mirrored = Of(map, map.Tileset.Def) };

    /// <summary>
    /// 성질 글자 넷(TIL 순서 위-왼, 위-오른, 아래-왼, 아래-오른)의 전치 T — 위-오른 ↔ 아래-왼. 거울판 메가 [my,mx] 의 하위 칸 = 원판 메가 [mx,my] 의 T.
    /// 길이가 4 가 아니면 그대로(형식 오류는 성질 검사가 낸다).
    /// </summary>
    public static string TransposeProps(string text) =>
        text.Length == 4 ? string.Concat(text[0], text[2], text[1], text[3]) : text;

    /// <summary>방위 좌우 뒤집기(서 ↔ 동, 남서 ↔ 남동, 북서 ↔ 북동 — 남·북 그대로).</summary>
    public static Compass MirrorFacing(Compass c) => c switch
    {
        Compass.West => Compass.East,
        Compass.East => Compass.West,
        Compass.SouthWest => Compass.SouthEast,
        Compass.SouthEast => Compass.SouthWest,
        Compass.NorthWest => Compass.NorthEast,
        Compass.NorthEast => Compass.NorthWest,
        _ => c,
    };

    /// <summary>짝 표를 조각 이름 → 거울판 조각 이름으로(짝은 양쪽, self 는 자기). 표가 없으면 빈 사전. 표 자체 검사는 <c>ArenaLayout.ValidateTileset</c>.</summary>
    public static IReadOnlyDictionary<string, string> PieceMap(TilesetDef tileset)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (tileset.Mirror is not { } mirror) return map;
        foreach (var pair in mirror.Pairs)
        {
            if (pair.Count != 2) continue;
            map.TryAdd(pair[0], pair[1]);
            map.TryAdd(pair[1], pair[0]);
        }
        foreach (var s in mirror.Self) map.TryAdd(s, s);
        return map;
    }

    private static Int2 Swap(Int2 c) => new(c.Y, c.X);
    private static TileCoord Swap(TileCoord c) => new(c.Ty, c.Tx);
    private static RectI Swap(RectI r) => new(r.Y0, r.X0, r.Y1, r.X1);

    /// <summary><c>grid</c> 블록 전치: 원판 줄 r·칸 c → 거울판 줄 c·칸 r. 짧은 줄의 빈 자리는 <c>.</c>(건드리지 않음), 줄 끝의 <c>.</c> 는 줄인다.</summary>
    private static List<string> TransposeGrid(IReadOnlyList<string> rows, Func<string, string> piece)
    {
        var cells = rows.Select(r => r.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();
        var width = cells.Count == 0 ? 0 : cells.Max(r => r.Length);
        var result = new List<string>(width);
        for (var c = 0; c < width; c++)
        {
            var line = new List<string>(cells.Count);
            for (var r = 0; r < cells.Count; r++)
            {
                var name = c < cells[r].Length ? cells[r][c] : ".";
                line.Add(name == "." ? "." : piece(name));
            }
            while (line.Count > 1 && line[^1] == ".") line.RemoveAt(line.Count - 1);
            result.Add(string.Join(' ', line));
        }
        return result;
    }
}
