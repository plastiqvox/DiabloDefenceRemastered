using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Loading;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 문 봉인 나무 문 그림(2026-11-13 사용자 요청 "창살이 내려가는 대신 문으로" → 2026-11-15 "문 한 짝씩만", 설계서 7.11). 가져오기가 방마다 맵 그림 아틀라스에 원작 문 하위 칸(닫힌·열린)을
/// 덤으로 굽고 <c>arena.json</c> <c>doors</c> 에 적는다(타일셋 <c>doors</c>). 문 틈마다 그 변의 하위 칸 목록(문 틈 첫 메가 원점 기준)을 닫힘·열림으로 놓는다 —
/// 벽 줄 하위 4칸에 원작 문 한 짝 + 문틀. 출구로 열렸거나 적이 드나드는 동안 열림(원작 문처럼 다가오면 열렸다 닫힘). 그림만 바뀌고 걷기·막힘은 문 봉인이 그대로 정한다.
/// 지금 방 맵 그림에 문이 없으면(예전 그림) null — <see cref="GateGrateArt"/> 창살로 돌아간다.
/// </summary>
public sealed class GateDoorArt
{
    /// <summary>문 칸 하나: 하위 칸 자리, 닫힌·열린 기둥 그림(없으면 빈 사각형), 열린 문 위 아치 덧그림(없으면 null), 메타 배율.</summary>
    public readonly record struct Cell(int Dx, int Dy, Texture2D? ClosedPage, SpriteRect Closed, Texture2D? OpenPage, SpriteRect Open, Texture2D? OverlayPage, SpriteRect Overlay, int MetaScale);

    private readonly Dictionary<string, List<Cell>> _sides = new(StringComparer.Ordinal);

    private GateDoorArt() { }

    /// <summary>변 → 타일셋 <c>doors</c> 키(2026-11-15 — 변마다: 뒤 벽 문과 앞 흙 턱 문의 문틀이 다르다).</summary>
    public static string SideOf(GateSide side) => side switch { GateSide.Nw => "nw", GateSide.Ne => "ne", GateSide.Se => "se", _ => "sw" };

    public bool Has(GateSide side) => _sides.ContainsKey(SideOf(side));

    /// <summary>그 맵 그림의 문 기둥. 문이 없거나 그림이 낡았으면(<c>layout_hash</c> 다름) null.</summary>
    public static GateDoorArt? Build(MapDef map, ArtServices art)
    {
        if (art.LoadMapArt(map) is not { ColumnMeta: { } meta, Columns: { } action } ma || ma.Arena.Doors.Count == 0) return null;
        if (!string.Equals(ma.Arena.LayoutHash, ArenaLayout.Expand(map).LayoutHash, StringComparison.Ordinal)) return null;
        var result = new GateDoorArt();
        bool Ok(int f) => f >= 0 && f <= action.Frames;
        foreach (var d in ma.Arena.Doors)
        {
            // 0 = 그 상태에서 그리지 않음(열면 비는 통로 칸 등)
            if (!Ok(d.Closed) || !Ok(d.Open) || d.Closed + d.Open == 0) continue;
            var rc = d.Closed >= 1 ? action.Rect(0, d.Closed) : default;
            var ro = d.Open >= 1 ? action.Rect(0, d.Open) : default;
            var ov = d.OpenOverlay >= 1 && d.OpenOverlay <= action.Frames ? action.Rect(0, d.OpenOverlay) : default;
            if (!result._sides.TryGetValue(d.Side, out var list)) result._sides[d.Side] = list = new List<Cell>(4);
            list.Add(new Cell(d.Dx, d.Dy,
                rc.IsEmpty ? null : ma.ColumnPages[rc.Page], rc,
                ro.IsEmpty ? null : ma.ColumnPages[ro.Page], ro,
                ov.IsEmpty ? null : ma.ColumnPages[ov.Page], ov, Math.Max(1, meta.Scale)));
        }
        return result._sides.Count > 0 ? result : null;
    }

    /// <summary>문 틈 하나의 문 칸들을 <paramref name="into"/> 에. 그 변의 문 그림이 없으면 거짓.</summary>
    public bool Place(GateZone zone, IsoMath iso, bool open, List<GateGrateArt.Placed> into)
    {
        if (!_sides.TryGetValue(SideOf(zone.Side), out var cells)) return false;
        var megas = GateGrates.Megas(zone);
        if (megas.Count == 0) return true;
        // 자리는 문 틈 첫 메가(변 위 번호가 작은 쪽 — 칸을 y·x 순으로 정렬했으므로 앞 것) 원점 기준
        var first = megas[0];
        foreach (var c in cells)
        {
            // 원작 문 칸 자리 그대로(앞 변에선 흙 턱 줄). 문 뒤로 걸어 들어가는 것은 닫힌 문 틈을 주인공에게 막아 없앤다(HeroGateBlock)
            var tx = first.X * 2 + c.Dx;
            var ty = first.Y * 2 + c.Dy;
            var tl = DrawOrder.ColumnTopLeft(iso, tx, ty);
            var k = 1f / c.MetaScale;
            var (page, r) = open ? (c.OpenPage, c.Open) : (c.ClosedPage, c.Closed);
            if (page is not null) Add(page, r, ArenaCellKind.Wall);
            if (open && c.OverlayPage is { } op) Add(op, c.Overlay, ArenaCellKind.Overlay);   // 열린 문 위 아치(원작 dSpecial)

            void Add(Texture2D p, SpriteRect s, ArenaCellKind kind)
            {
                if (s.IsEmpty) return;
                var dest = new RectF(tl.X + s.Ox * k, tl.Y + s.Oy * k, tl.X + (s.Ox + s.W) * k, tl.Y + (s.Oy + s.H) * k);
                into.Add(new GateGrateArt.Placed(p, new Microsoft.Xna.Framework.Rectangle(s.X, s.Y, s.W, s.H), dest, dest, tx, ty, kind));
            }
        }
        return true;
    }
}
