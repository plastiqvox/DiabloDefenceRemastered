using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Loading;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// UI 그리기 (설계서 7.3 9~10층, 소유 WP5). 좌표는 가상 960×540 이고 <c>PresentRT</c> 의 레터박스 사각형 배율로 바꿔 그린다(<see cref="PresentMath"/>).
/// 글꼴은 크기 × 배율로 요청해 늘 선명하다(FontStashSharp 가 크기별로 굽는다). HUD·메뉴·오버레이(WP6)가 쓴다. Begin/End 사이에서만 그린다.
/// 색은 premultiplied(<c>Rgb.ToXna(alpha)</c>).
/// </summary>
public sealed class UiCanvas(AppServices app)
{
    private RenderContext? _rc;
    private Texture2D? _pixel;
    private Texture2D? _circle;

    /// <summary>가상 좌표 1 = 창 픽셀 몇 개.</summary>
    public float Scale => PresentMath.UiScale(Present);

    private Int2 Present
    {
        get
        {
            var size = (_rc ?? app.Render)?.PresentSize ?? RenderContext.VirtualSize;
            return new Int2(size.X, size.Y);
        }
    }

    public void Begin(RenderContext rc)
    {
        _rc = rc;
        _pixel ??= rc is GameRenderContext g ? g.Primitives.Pixel : MakePixel(rc.Device);
        _circle ??= (rc as GameRenderContext)?.Primitives.Circle;
        rc.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
    }

    public void End() => Batch.End();

    private SpriteBatch Batch => (_rc ?? throw new InvalidOperationException("UiCanvas.Begin 밖에서 그렸다")).Batch;

    /// <summary>가상 좌표 → 창 좌표.</summary>
    public Vector2 ToScreen(Vector2 virtualPos) => PresentMath.VirtualToScreen(virtualPos, Present);
    /// <summary>창 좌표 → 가상 좌표(버튼 누르기 판정).</summary>
    public Vector2 ToVirtual(Vector2 screenPos) => PresentMath.ScreenToVirtual(screenPos, Present);

    /// <summary><paramref name="size"/> = 가상 좌표 기준 글자 크기(px).</summary>
    public void Text(string text, Vector2 pos, float size, Rgb color, float alpha = 1f)
    {
        if (Font(size) is not { } font || string.IsNullOrEmpty(text) || alpha <= 0f) return;
        var p = ToScreen(pos);
        font.DrawText(Batch, text, new XnaVector2(MathF.Round(p.X), MathF.Round(p.Y)), color.ToXna(alpha));
    }

    public Vector2 MeasureText(string text, float size)
    {
        if (Font(size) is not { } font || string.IsNullOrEmpty(text)) return Vector2.Zero;
        var m = font.MeasureString(text);
        var s = Scale;
        return new Vector2(m.X / s, m.Y / s);
    }

    public void Fill(RectF rect, Rgb color, float alpha = 1f)
    {
        if (alpha <= 0f || _pixel is null) return;
        Batch.Draw(_pixel, ScreenRect(rect), color.ToXna(alpha));
    }

    /// <summary>
    /// 원 채우기(가상 좌표 중심·반지름). <paramref name="fill"/> = 아래에서부터 찬 높이 비율(1 = 원 전체) — 체력 구슬의 액체(기획 12.1).
    /// 원 텍스처(<see cref="Primitives.Circle"/>)의 아래쪽만 잘라 같은 자리에 그린다. 원 텍스처가 없으면(그래픽 없는 문맥) 사각형으로.
    /// </summary>
    public void Disc(Vector2 center, float radius, Rgb color, float alpha = 1f, float fill = 1f)
    {
        fill = Math.Clamp(fill, 0f, 1f);
        if (alpha <= 0f || fill <= 0f || radius <= 0f) return;
        var bottom = center.Y + radius;
        if (_circle is null)
        {
            Fill(new RectF(center.X - radius, bottom - 2f * radius * fill, center.X + radius, bottom), color, alpha);
            return;
        }
        const int size = Primitives.CircleSize;
        var top = Math.Clamp((int)MathF.Round(size * (1f - fill)), 0, size - 1);
        var dest = new RectF(center.X - radius, bottom - 2f * radius * (size - top) / size, center.X + radius, bottom);
        Batch.Draw(_circle, ScreenRect(dest), new Rectangle(0, top, size, size - top), color.ToXna(alpha));
    }

    /// <summary>
    /// 부채꼴 채우기(보스 묶음 B 이음새 — HUD 순간이동 충전 칸, 6.20 ⑳). <paramref name="startRad"/> 에서 <paramref name="sweepRad"/> 만큼(화면 좌표계 — 양수 = 시계 방향,
    /// 위 = −π/2). 중심에서 뻗는 1px 띠를 호 위 간격이 창 픽셀 하나보다 촘촘하게 돌려 그린다(원 텍스처가 없어도 됨). 띠가 중심에서 겹치므로 불투명 채우기에 쓴다.
    /// </summary>
    public void Wedge(Vector2 center, float radius, float startRad, float sweepRad, Rgb color, float alpha = 1f)
    {
        if (alpha <= 0f || _pixel is null || radius <= 0f || MathF.Abs(sweepRad) < 1e-4f) return;
        var c = ToScreen(center);
        var r = radius * Scale;
        var steps = Math.Max(2, (int)MathF.Ceiling(MathF.Abs(sweepRad) * r / 0.6f));
        var tint = color.ToXna(alpha);
        for (var i = 0; i <= steps; i++)
        {
            var a = startRad + sweepRad * i / steps;
            Batch.Draw(_pixel, new XnaVector2(c.X, c.Y), null, tint, a, new XnaVector2(0f, 0.5f), new XnaVector2(r, 1.2f), SpriteEffects.None, 0f);
        }
    }

    /// <summary>그림 정의 <paramref name="spriteId"/> 의 <paramref name="source"/> 영역(그림 픽셀, 원색 첫 페이지)을 그린다.</summary>
    public void Sprite(string spriteId, RectI source, RectF dest, Rgb tint, float alpha = 1f)
    {
        if (alpha <= 0f || app.Art is not ArtServices art) return;
        if (!app.Defs.Sprites.TryGet(spriteId, out var def)) return;
        if (!art.TryGetSprite(def, "", out var h)) return;
        Batch.Draw(h.Texture, ScreenRect(dest), new Rectangle(source.X0, source.Y0, source.Width, source.Height), tint.ToXna(alpha));
    }

    private Rectangle ScreenRect(RectF rect)
    {
        var a = ToScreen(rect.Min);
        var b = ToScreen(rect.Max);
        var x0 = (int)MathF.Round(a.X);
        var y0 = (int)MathF.Round(a.Y);
        return new Rectangle(x0, y0, (int)MathF.Round(b.X) - x0, (int)MathF.Round(b.Y) - y0);
    }

    private SpriteFontBase? Font(float size)
    {
        if (app.Art is ArtServices { HasFont: false }) return null;
        var fonts = app.Art?.Fonts;
        if (fonts is null) return null;
        return fonts.GetFont(Math.Max(1f, size * Scale));
    }

    private static Texture2D MakePixel(GraphicsDevice device)
    {
        var t = new Texture2D(device, 1, 1);
        t.SetData([Microsoft.Xna.Framework.Color.White]);
        return t;
    }
}
