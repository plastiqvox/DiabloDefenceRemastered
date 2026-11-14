namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 변 규칙(2026-11-15 사용자 요청 "타일 규칙을 먼저 만들고 지도를 — 끝에 있으면 안 되는 타일이 끝에 있어 잘려 보임", 설계서 6.11 "변 규칙").
/// 타일셋 <c>joins</c> 가 메가타일 종류마다 네 변(ne = −y, se = +x, sw = +y, nw = −x)에 걸친 것을 적는다.
/// 검사: ① 이웃 둘의 맞닿은 변이 맞음(같은 표시이거나 한쪽이 <c>x</c>, <c>a|b</c> 는 하나라도), ② 맵 바깥에 닿는 변은 <c>x</c> 또는 <c>fade</c>(그리기가 어둠으로 흐림).
/// 원작 대성당 생성기의 기본 종류 표·끝 조각 고치기(DevilutionX <c>drlg_l1.cpp</c> 의 <c>BaseTypes</c>·<c>FixTilesPatterns</c>)를 참고해 종류를 나눴다.
/// 손 맵과 이후 무작위 맵이 같은 규칙을 쓴다.
/// </summary>
public static class TileJoins
{
    /// <summary>변 순서(<c>sides</c> 글자 순서와 같음).</summary>
    public enum Dir : byte { Ne, Se, Sw, Nw }

    /// <summary>이 방향 이웃 메가 칸 차이.</summary>
    public static (int Dx, int Dy) Step(Dir d) => d switch
    {
        Dir.Ne => (0, -1),
        Dir.Se => (1, 0),
        Dir.Sw => (0, 1),
        _ => (-1, 0),
    };

    public static Dir Opposite(Dir d) => (Dir)(((int)d + 2) % 4);

    public static string Name(Dir d) => d switch { Dir.Ne => "ne", Dir.Se => "se", Dir.Sw => "sw", _ => "nw" };

    /// <summary>번호 → 네 변 표시(종류 이름 포함). 같은 번호가 두 종류에 있으면 앞 것.</summary>
    public sealed class Table
    {
        private readonly Dictionary<int, (string Kind, string[] Sides)> _byTile = new();

        public Table(TileJoinsDef def)
        {
            Fade = new HashSet<string>(def.Fade, StringComparer.Ordinal);
            foreach (var (kind, k) in def.Kinds.OrderBy(p => p.Key, StringComparer.Ordinal))
                if (TryParseSides(k.Sides, out var sides))
                    foreach (var t in k.Tiles)
                        _byTile.TryAdd(t, (kind, sides));
        }

        public IReadOnlySet<string> Fade { get; }

        public bool TryGet(int tile, out string kind, out string[] sides)
        {
            if (_byTile.TryGetValue(tile, out var v)) { kind = v.Kind; sides = v.Sides; return true; }
            kind = ""; sides = [];
            return false;
        }

        /// <summary>그 번호의 변 표시(없으면 null).</summary>
        public string? Side(int tile, Dir d) => _byTile.TryGetValue(tile, out var v) ? v.Sides[(int)d] : null;
    }

    /// <summary>"ne se sw nw" → 표시 넷. 넷이 아니면 거짓.</summary>
    public static bool TryParseSides(string text, out string[] sides)
    {
        sides = (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return sides.Length == 4 && sides.All(s => s.Split('|').All(p => p.Length > 0));
    }

    /// <summary>맞닿은 두 변이 맞는가: 한쪽이 <c>x</c> 이거나, <c>|</c> 로 나눈 표시가 하나라도 같다.</summary>
    public static bool Compatible(string a, string b)
    {
        if (a == "x" || b == "x") return true;
        foreach (var pa in a.Split('|'))
            foreach (var pb in b.Split('|'))
                if (pa == pb) return true;
        return false;
    }

    /// <summary>맵 바깥에 닿아도 되는 변: <c>x</c> 또는 <c>fade</c> 표시 하나라도.</summary>
    public static bool EdgeOk(string side, IReadOnlySet<string> fade) => side.Split('|').Any(p => p == "x" || fade.Contains(p));

    /// <summary>타일셋 검사(13.5-12 덧붙임): 표시가 넷, 성질 표의 번호마다 종류가 하나, 종류의 번호는 성질 표에 있음.</summary>
    public static void ValidateTileset(TilesetDef ts, ICollection<LayoutError> errors)
    {
        if (ts.Joins is not { } joins) return;
        var owner = new Dictionary<int, string>();
        foreach (var (kind, k) in joins.Kinds.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var path = $"$.joins.kinds.{kind}";
            if (!TryParseSides(k.Sides, out _)) errors.Add(new($"{path}.sides", $"변 표시 '{k.Sides}' 는 공백으로 나눈 넷(ne se sw nw)이어야 한다"));
            if (k.Tiles.Count == 0) errors.Add(new($"{path}.tiles", "번호가 하나도 없다"));
            for (var i = 0; i < k.Tiles.Count; i++)
            {
                var t = k.Tiles[i];
                if (!ts.Props.ContainsKey(t)) errors.Add(new($"{path}.tiles[{i}]", $"메가타일 {t} 은 성질 표에 없다"));
                if (!owner.TryAdd(t, kind)) errors.Add(new($"{path}.tiles[{i}]", $"메가타일 {t} 이 종류 '{owner[t]}' 에도 있다"));
            }
        }
        foreach (var t in ts.Props.Keys.Order())
            if (!owner.ContainsKey(t)) errors.Add(new("$.joins.kinds", $"성질 표의 메가타일 {t} 에 변 규칙 종류가 없다"));
    }

    /// <summary>
    /// 맵 검사: 이웃 변 맞음 + 바깥 변. 오류 경로는 <c>$.layout</c>, 글에 두 칸의 좌표·번호·종류·변을 적는다.
    /// 문 틈의 열린 바닥은 바깥 변이 <c>f</c> 라 <c>fade</c> 로 지나간다(문 그림이 덮고, 열면 어둠으로 흐림).
    /// </summary>
    public static void Validate(ArenaGrid grid, ICollection<LayoutError> errors)
    {
        if (grid.Tileset.Joins is not { } joins) return;
        var table = new Table(joins);
        var m = grid.MegaSize;
        var missing = new HashSet<int>();
        for (var my = 0; my < m; my++)
            for (var mx = 0; mx < m; mx++)
            {
                var tile = grid.MegaTile(mx, my);
                if (!table.TryGet(tile, out var kind, out var sides))
                {
                    if (missing.Add(tile)) errors.Add(new("$.layout", $"메가타일 {tile} 에 변 규칙 종류가 없다(joins.kinds)"));
                    continue;
                }
                foreach (var d in (ReadOnlySpan<Dir>)[Dir.Ne, Dir.Se, Dir.Sw, Dir.Nw])
                {
                    var (dx, dy) = Step(d);
                    int nx = mx + dx, ny = my + dy;
                    var side = sides[(int)d];
                    if ((uint)nx >= (uint)m || (uint)ny >= (uint)m)
                    {
                        if (!EdgeOk(side, table.Fade))
                            errors.Add(new("$.layout", $"({mx},{my}) 메가타일 {tile}({kind}) 의 {Name(d)} 변 '{side}' 가 맵 끝에 닿아 잘려 보인다"));
                        continue;
                    }
                    if (d is Dir.Ne or Dir.Nw) continue;   // 짝마다 한 번(se·sw 쪽에서)
                    var other = grid.MegaTile(nx, ny);
                    if (!table.TryGet(other, out var okind, out var osides)) continue;
                    var oside = osides[(int)Opposite(d)];
                    if (!Compatible(side, oside))
                        errors.Add(new("$.layout",
                            $"({mx},{my}) 메가타일 {tile}({kind}) 의 {Name(d)} 변 '{side}' 와 ({nx},{ny}) 메가타일 {other}({okind}) 의 {Name(Opposite(d))} 변 '{oside}' 가 맞지 않는다"));
                }
            }
    }
}
