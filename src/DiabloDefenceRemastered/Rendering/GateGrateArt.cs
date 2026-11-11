using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Loading;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 문 봉인 창살 그림 (v0.9.4 V13, 설계서 7.11 — 소유 WP5, 단계 2 wp5). 가져오기는 문 틈에 창살을 굽지 않으므로 같은 액트 맵 풀에서 그 창살 조각을 쓴 맵의
/// 맵 그림(<c>arena.json</c>·기둥 아틀라스)에서 그 메가의 하위 칸 기둥을 빌린다(<see cref="GateGrates.FindSource"/>). 조각마다 기둥 몇 개(하위 칸 자리 + 그림).
/// 못 찾으면 그 조각은 없음 — <see cref="WorldRenderer"/> 가 단계 1 코드 그림으로 돌아간다.
/// </summary>
public sealed class GateGrateArt
{
    /// <summary>창살 조각 하나의 기둥 그림(메가 안 하위 칸 <paramref name="Dx"/>·<paramref name="Dy"/>, 그 칸의 그림 종류·페이지·원본 사각형·메타 배율).</summary>
    public readonly record struct Column(int Dx, int Dy, ArenaCellKind Kind, Texture2D Page, SpriteRect Rect, int MetaScale);

    private readonly Dictionary<string, Column[]> _pieces = new(StringComparer.Ordinal);

    private GateGrateArt() { }

    /// <summary>찾은 조각 이름(<c>grate_h</c>·<c>grate_v</c>).</summary>
    public IEnumerable<string> Pieces => _pieces.Keys;

    public bool TryGet(string piece, out Column[] columns) => _pieces.TryGetValue(piece, out columns!);

    /// <summary>놓인 창살 기둥 하나: 페이지·원본 사각형(내려간 만큼 잘림)·자리(월드 px)·안 잘린 자리·하위 칸·그림 종류.</summary>
    public readonly record struct Placed(Texture2D Page, Microsoft.Xna.Framework.Rectangle Src, RectF Dest, RectF Full, int Tx, int Ty, ArenaCellKind Kind);

    /// <summary>
    /// 문 틈 하나의 창살 기둥들을 <paramref name="into"/> 에(문 틈 메가마다 변의 조각 — <see cref="GateGrates.PieceName"/>). <paramref name="sink"/>(0..1, <see cref="GateGrates.Sink"/>)만큼
    /// 그림을 아래로 밀고 바닥선(기둥 칸 아래 끝 = 원래 그림 아래 끝) 아래는 잘라 낸다. 그 변의 조각 그림이 없으면 거짓(코드 그림 봉인으로).
    /// </summary>
    public bool Place(GateZone zone, IsoMath iso, float sink, List<Placed> into)
    {
        if (!_pieces.TryGetValue(GateGrates.PieceName(zone.Side), out var cols)) return false;
        if (sink >= 1f) return true;
        foreach (var mega in GateGrates.Megas(zone))
            foreach (var col in cols)
            {
                var tx = mega.X * 2 + col.Dx;
                var ty = mega.Y * 2 + col.Dy;
                var tl = DrawOrder.ColumnTopLeft(iso, tx, ty);
                var k = 1f / col.MetaScale;
                var r = col.Rect;
                var full = new RectF(tl.X + r.Ox * k, tl.Y + r.Oy * k, tl.X + (r.Ox + r.W) * k, tl.Y + (r.Oy + r.H) * k);
                var floorY = tl.Y + iso.ColumnHeight;
                var d = Math.Clamp(sink, 0f, 1f) * (floorY - full.Y0);
                var visible = MathF.Min(full.Height, floorY - (full.Y0 + d));
                if (visible <= 0.5f) continue;
                var srcH = Math.Min(r.H, Math.Max(1, (int)MathF.Round(visible / k)));
                var dest = new RectF(full.X0, full.Y0 + d, full.X1, full.Y0 + d + srcH * k);
                into.Add(new Placed(col.Page, new Microsoft.Xna.Framework.Rectangle(r.X, r.Y, r.W, srcH), dest, full, tx, ty, col.Kind));
            }
        return true;
    }

    /// <summary>
    /// <paramref name="world"/> 의 맵 + 액트 맵 풀(거울판 포함 — 미리 읽은 그 맵 그림)에서 가로·세로 창살을 찾는다. 하나도 없으면 null.
    /// 맵 그림이 낡았으면(<c>layout_hash</c> 다름) 그 맵은 건너뛴다.
    /// </summary>
    /// <param name="extra">더 찾아볼 맵(미리보기 — 맵 하나만 둔 월드라 정의의 모든 맵·거울판을 준다). 액트 미리 읽기 밖의 맵 그림은 이 부름이 읽는다.</param>
    public static GateGrateArt? Build(World world, ArtServices art, IEnumerable<MapDef>? extra = null)
    {
        var maps = new List<MapDef>();
        if (world.Map is { } own) maps.Add(own);
        foreach (var m in ActPreload.Maps(world.Stage).Concat(extra ?? []))
            if (!maps.Exists(x => ReferenceEquals(x, m) || x.Id == m.Id)) maps.Add(m);
        var result = new GateGrateArt();
        foreach (var piece in new[] { "grate_h", "grate_v" })
        {
            var tried = new List<MapDef>(maps);
            while (GateGrates.FindSource(tried, piece) is { } src)
            {
                tried.Remove(src.Map);
                if (Columns(src, art) is { Length: > 0 } cols) { result._pieces[piece] = cols; break; }
            }
        }
        return result._pieces.Count > 0 ? result : null;
    }

    private static Column[]? Columns(GrateSource src, ArtServices art)
    {
        if (art.LoadMapArt(src.Map) is not { ColumnMeta: { } meta, Columns: { } action } ma) return null;
        if (!string.Equals(ma.Arena.LayoutHash, ArenaLayout.Expand(src.Map).LayoutHash, StringComparison.Ordinal)) return null;
        var list = new List<Column>(4);
        foreach (var c in ma.Arena.Cells)
        {
            if (c.Kind == ArenaCellKind.Overlay || c.Tx / 2 != src.Mega.X || c.Ty / 2 != src.Mega.Y) continue;
            if (c.Frame < 1 || c.Frame > action.Frames) continue;
            var r = action.Rect(0, c.Frame);
            if (r.IsEmpty) continue;
            list.Add(new Column(c.Tx - src.Mega.X * 2, c.Ty - src.Mega.Y * 2, c.Kind, ma.ColumnPages[r.Page], r, Math.Max(1, meta.Scale)));
        }
        return [.. list];
    }
}
