using DiabloDefenceRemastered.Core.Art;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// <c>--debug-nav</c> 겹침 (설계서 9절, 소유 WP5). 월드 타깃 위에 월드 px 로 그린다(카메라 행렬 그대로).
/// <list type="bullet">
/// <item><c>grid</c>: 막힌 칸(걷기 막힘 마름모). <c>tiles</c>: 하위 칸 성질 세 색 — 걷기 막힘 빨강·투사체 막힘 파랑·시야 막힘 노랑을 겹친다(8글자, 6.11).
/// <c>entries</c>: 입구 바깥 → 진입점 선, 기준점(흰 마름모), 문 틈 칸(초록).
/// <c>sight</c>: 시야 안 칸(초록, 9틱 안에 바뀐 칸은 밝게)·가 본 칸(회색)·시야 막힘 칸(노랑 테두리)·주인공 칸, 반경 R 과 R − edge 원, 화면 사각형을 view_inset 만큼 줄인 선.</item>
/// <item><c>flow</c>·<c>astar</c>·<c>shapes</c> 는 WP2 의 길찾기·충돌 상태를 읽어야 해 여기서 그리지 않는다(합친 뒤 WP2 가 이 파일에 더하거나 자기 겹침을 둔다).</item>
/// </list>
/// </summary>
public static class DebugNavOverlay
{
    public static bool Has(IReadOnlyList<string> layers, string name) =>
        layers.Contains("all", StringComparer.Ordinal) || layers.Contains(name, StringComparer.Ordinal);

    public static void Draw(SpriteBatch batch, Primitives prim, World w, IReadOnlyList<string> layers, RectF view, Vector2 heroDraw, int tick)
    {
        if (layers.Count == 0 || w.Arena is not { } grid) return;
        var iso = grid.Iso;
        bool grid_ = Has(layers, "grid"), tiles = Has(layers, "tiles"), sight = Has(layers, "sight");
        if (grid_ || tiles || sight)
            for (var ty = 0; ty < grid.Size; ty++)
                for (var tx = 0; tx < grid.Size; tx++)
                {
                    var c = iso.CellCenter(tx, ty);
                    if (c.X + 32 < view.X0 || c.X - 32 > view.X1 || c.Y + 16 < view.Y0 || c.Y - 16 > view.Y1) continue;
                    var rect = new Rectangle((int)c.X - 32, (int)c.Y - 16, 64, 32);
                    var f = grid.Flags(tx, ty);
                    if (grid_ && (f & CellFlags.BlocksWalk) != 0) batch.Draw(prim.Diamond, rect, new Rgb(255, 255, 255).ToXna(0.25f));
                    if (tiles)
                    {
                        if ((f & CellFlags.BlocksWalk) != 0) batch.Draw(prim.Diamond, rect, new Rgb(230, 40, 40).ToXna(0.3f));
                        if ((f & CellFlags.BlocksProjectiles) != 0) batch.Draw(prim.Diamond, Shrink(rect, 8), new Rgb(60, 110, 255).ToXna(0.35f));
                        if ((f & CellFlags.BlocksSight) != 0) batch.Draw(prim.Diamond, Shrink(rect, 16), new Rgb(255, 220, 40).ToXna(0.45f));
                    }
                    if (sight && w.Visibility.Enabled)
                    {
                        var v = w.Visibility;
                        if (v.InSight(tx, ty))
                        {
                            var fresh = tick - v.ChangedTick(tx, ty) < v.FadeTicks;
                            batch.Draw(prim.Diamond, Shrink(rect, 4), new Rgb(40, 220, 60).ToXna(fresh ? 0.5f : 0.25f));
                        }
                        else if (v.IsExplored(tx, ty)) batch.Draw(prim.Diamond, Shrink(rect, 4), new Rgb(150, 150, 150).ToXna(0.25f));
                        if ((f & CellFlags.BlocksSight) != 0) Outline(batch, prim, c, new Rgb(255, 220, 40).ToXna(0.8f));
                    }
                }

        if (Has(layers, "entries") && w.Resolved is { } resolved)
        {
            foreach (var entry in resolved.Entries)
            {
                BatchX.Line(batch, prim.Pixel, entry.Outside, entry.Inside, 2f, new Rgb(80, 255, 120).ToXna(0.9f));
                BatchX.Centered(batch, prim.Circle, entry.Inside, new Vector2(10), new Rgb(80, 255, 120).ToXna(1f));
            }
            foreach (var anchor in resolved.Anchors.Values)
                BatchX.Centered(batch, prim.Diamond, anchor, new Vector2(20, 10), Color.White);
            foreach (var gate in w.Gates.Values)
                foreach (var cell in gate.Cells)
                    Outline(batch, prim, iso.CellCenter(cell.X, cell.Y), new Rgb(80, 255, 120).ToXna(0.7f));
        }

        if (sight && w.Visibility.Enabled)
        {
            var v = w.Visibility;
            var heroCell = iso.CellOf(heroDraw);
            Outline(batch, prim, iso.CellCenter(heroCell.X, heroCell.Y), Color.White);
            BatchX.Centered(batch, prim.Ring, heroDraw, new Vector2(v.Radius * 2f), new Rgb(40, 220, 60).ToXna(0.9f));
            var inner = v.Radius - (v.Rules?.Edge ?? 0f);
            if (inner > 0f) BatchX.Centered(batch, prim.Ring, heroDraw, new Vector2(inner * 2f), new Rgb(40, 220, 60).ToXna(0.5f));
            var inset = v.Rules?.ViewInset ?? 0f;
            var r = v.ViewRect.Inflate(-inset);
            var col = new Rgb(255, 255, 255).ToXna(0.6f);
            BatchX.Line(batch, prim.Pixel, new Vector2(r.X0, r.Y0), new Vector2(r.X1, r.Y0), 1f, col);
            BatchX.Line(batch, prim.Pixel, new Vector2(r.X1, r.Y0), new Vector2(r.X1, r.Y1), 1f, col);
            BatchX.Line(batch, prim.Pixel, new Vector2(r.X1, r.Y1), new Vector2(r.X0, r.Y1), 1f, col);
            BatchX.Line(batch, prim.Pixel, new Vector2(r.X0, r.Y1), new Vector2(r.X0, r.Y0), 1f, col);
        }
    }

    private static Rectangle Shrink(Rectangle r, int px) => new(r.X + px, r.Y + px / 2, r.Width - 2 * px, r.Height - px);

    private static void Outline(SpriteBatch batch, Primitives prim, Vector2 c, Color color)
    {
        Vector2 top = c + new Vector2(0, -16), right = c + new Vector2(32, 0), bottom = c + new Vector2(0, 16), left = c + new Vector2(-32, 0);
        BatchX.Line(batch, prim.Pixel, top, right, 1f, color);
        BatchX.Line(batch, prim.Pixel, right, bottom, 1f, color);
        BatchX.Line(batch, prim.Pixel, bottom, left, 1f, color);
        BatchX.Line(batch, prim.Pixel, left, top, 1f, color);
    }
}
