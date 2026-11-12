using DiabloDefenceRemastered.Loading;
using DiabloDefenceRemastered.Rendering;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.UI;

/// <summary>
/// 체력 구슬 원작 그림을 <b>둥글게</b> 오려 둔 텍스처(기획 12.1 V12). 조작판(<c>ui_panel8</c>)의 구슬 칸을 네모로 잘라 그리면 판의 돌 무늬·악마 조각이 모서리에 붙어
/// "오려 붙인" 느낌이 나서(사용자 관찰), 칸 하나를 구슬 원(<see cref="Center"/>·<see cref="Radius"/>, 칸 좌표) 밖은 투명하게 만든 텍스처로 한 번 굽는다.
/// 가장자리 1px 은 덮인 비율만큼 부드럽게(premultiplied — 네 성분 모두 곱함). 원본 텍스처가 바뀌면(자원 다시 읽기) 다시 굽는다.
/// </summary>
public static class OrbArt
{
    /// <summary>구슬 원의 가운데·반지름(칸 좌표 88×88 — 원작 조작판 구슬 자리, 가져온 그림에서 잼).</summary>
    public static readonly Vector2 Center = new(42f, 45f);
    public const float Radius = 41f;

    private static Baked? _full;

    private sealed record Baked(Texture2D Source, Texture2D Texture, int Scale);

    /// <summary>가득 찬 구슬(조작판 구슬 칸)을 둥글게 오린 것과 그 배율(칸 1px = 텍스처 몇 px). 그림이 없으면 거짓.</summary>
    public static bool TryGet(AppServices app, RectI orbCell, out Texture2D full, out int scale)
    {
        full = null!;
        scale = 1;
        if (Bake(app, "ui_panel8", orbCell, ref _full) is not { } f) return false;
        (full, scale) = (f.Texture, f.Scale);
        return true;
    }

    private static Baked? Bake(AppServices app, string spriteId, RectI cell, ref Baked? cache)
    {
        if (app.Art is not ArtServices art || !app.Defs.Sprites.TryGet(spriteId, out var sprite)) return null;
        if (!art.TryGetSprite(sprite, "", out var h) || h.Action is not { } action || h.Meta is not { } meta) return null;
        var r = action.Rect(0, 1);
        if (r.IsEmpty || r.Page != 0) return null;
        var source = h.Texture;
        if (cache is { } c && ReferenceEquals(c.Source, source) && !c.Texture.IsDisposed) return c;

        var ms = Math.Max(1, meta.Scale);
        int w = cell.Width * ms, hgt = cell.Height * ms;
        int sx0 = cell.X0 * ms, sy0 = cell.Y0 * ms;
        // 칸 사각형과 아틀라스에 남은(잘린) 사각형이 겹치는 곳만 읽는다
        int ix0 = Math.Max(sx0, r.Ox), iy0 = Math.Max(sy0, r.Oy), ix1 = Math.Min(sx0 + w, r.Ox + r.W), iy1 = Math.Min(sy0 + hgt, r.Oy + r.H);
        var pixels = new Color[w * hgt];
        if (ix1 > ix0 && iy1 > iy0)
        {
            var part = new Color[(ix1 - ix0) * (iy1 - iy0)];
            source.GetData(0, new Rectangle(r.X + ix0 - r.Ox, r.Y + iy0 - r.Oy, ix1 - ix0, iy1 - iy0), part, 0, part.Length);
            for (var y = iy0; y < iy1; y++)
                Array.Copy(part, (y - iy0) * (ix1 - ix0), pixels, (y - sy0) * w + (ix0 - sx0), ix1 - ix0);
        }
        var center = Center * ms;
        var radius = Radius * ms;
        for (var y = 0; y < hgt; y++)
        for (var x = 0; x < w; x++)
        {
            var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            var cover = Math.Clamp(radius + 0.5f - d, 0f, 1f);
            if (cover >= 1f) continue;
            var i = y * w + x;
            var p = pixels[i];
            pixels[i] = cover <= 0f ? Color.Transparent : new Color((byte)(p.R * cover), (byte)(p.G * cover), (byte)(p.B * cover), (byte)(p.A * cover));
        }
        cache?.Texture.Dispose();
        var tex = new Texture2D(source.GraphicsDevice, w, hgt);
        tex.SetData(pixels);
        cache = new Baked(source, tex, ms);
        return cache;
    }
}
