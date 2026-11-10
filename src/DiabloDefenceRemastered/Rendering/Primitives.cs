using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 코드로 만드는 텍스처 (설계서 7.7·7.10·7.11, 소유 WP5) — 그래픽 장치 하나에 한 벌. 모두 premultiplied.
/// 흰 1px(채우기·선), 방사형 빛(256px, 가운데 1 → 가장자리 0, 부드러운 끝), 원(예고 채움)·고리(예고 테두리 2px), 바닥 마름모(64×32 — 자원 없는 맵·디버그).
/// </summary>
public sealed class Primitives : IDisposable
{
    public const int LightSize = 256;
    public const int CircleSize = 128;

    public Primitives(GraphicsDevice device)
    {
        Pixel = new Texture2D(device, 1, 1);
        Pixel.SetData([Color.White]);
        Light = Make(device, LightSize, LightSize, (x, y) =>
        {
            var d = Dist(x, y, LightSize);
            var v = Math.Clamp(1f - d, 0f, 1f);
            return v * v * (3f - 2f * v);   // 부드러운 끝
        });
        Circle = Make(device, CircleSize, CircleSize, (x, y) => Math.Clamp((1f - Dist(x, y, CircleSize)) * CircleSize * 0.5f, 0f, 1f));
        Ring = Make(device, CircleSize, CircleSize, (x, y) =>
        {
            var r = Dist(x, y, CircleSize) * CircleSize * 0.5f;     // px
            var outer = CircleSize * 0.5f;
            return Math.Clamp(Math.Min(outer - r, r - (outer - 2.5f)), 0f, 1f);
        });
        Diamond = Make(device, 64, 32, (x, y) =>
        {
            var dx = MathF.Abs(x + 0.5f - 32f) / 32f;
            var dy = MathF.Abs(y + 0.5f - 16f) / 16f;
            return dx + dy <= 1f ? 1f : 0f;
        });
    }

    public Texture2D Pixel { get; }
    public Texture2D Light { get; }
    public Texture2D Circle { get; }
    public Texture2D Ring { get; }
    public Texture2D Diamond { get; }

    /// <summary>
    /// 경계 흐림 띠(7.10 ②): 텍스처 반지름을 1 로 놓고 알파가 <paramref name="innerRatio"/>(= (R − edge)/덮는 반경) 안 1, <paramref name="outerRatio"/>(= R/덮는 반경)에서 0, 그 밖 0. 색 흰색.
    /// 비율이 바뀔 때만 다시 만든다.
    /// </summary>
    public Texture2D BandFor(GraphicsDevice device, float innerRatio, float outerRatio)
    {
        var key = ((int)MathF.Round(innerRatio * 1000f) << 12) | (int)MathF.Round(outerRatio * 1000f);
        if (_band is not null && _bandKey == key) return _band;
        _band?.Dispose();
        _bandKey = key;
        var outer = Math.Clamp(outerRatio, 0.001f, 1f);
        var inner = Math.Clamp(innerRatio, 0f, outer - 0.0005f);
        _band = Make(device, LightSize, LightSize, (x, y) =>
        {
            var d = Dist(x, y, LightSize);
            return d <= inner ? 1f : Math.Clamp((outer - d) / (outer - inner), 0f, 1f);
        });
        return _band;
    }

    private Texture2D? _band;
    private int _bandKey = -1;

    private static float Dist(int x, int y, int size)
    {
        var h = size * 0.5f;
        var dx = (x + 0.5f - h) / h;
        var dy = (y + 0.5f - h) / h;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static Texture2D Make(GraphicsDevice device, int w, int h, Func<int, int, float> alpha)
    {
        var data = new Color[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var a = (byte)Math.Clamp((int)MathF.Round(alpha(x, y) * 255f), 0, 255);
                data[y * w + x] = new Color(a, a, a, a);
            }
        var t = new Texture2D(device, w, h);
        t.SetData(data);
        return t;
    }

    public void Dispose()
    {
        Pixel.Dispose();
        Light.Dispose();
        Circle.Dispose();
        Ring.Dispose();
        Diamond.Dispose();
        _band?.Dispose();
    }
}

/// <summary>
/// 안개·빛 혼합 상태 (설계서 7.10 <c>FogBlend</c> 표·7.7 ④ — 정적 객체로 한 번). 값은 늘 <b>알파 채널</b>에 담는다(7.7 "혼합 상태 규칙").
/// "A 만" = <c>ColorWriteChannels.Alpha</c>, "RGB 만" = Red | Green | Blue.
/// </summary>
public static class FogBlend
{
    private const ColorWriteChannels Rgb = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

    /// <summary>dst.a = src.a</summary>
    public static readonly BlendState WriteA = new()
    {
        Name = "WriteA", ColorWriteChannels = ColorWriteChannels.Alpha,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.Zero,
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.Zero,
    };

    /// <summary>dst.a = max(dst.a, 그림 알파 × 틴트 알파)</summary>
    public static readonly BlendState MaxA = new()
    {
        Name = "MaxA", ColorWriteChannels = ColorWriteChannels.Alpha,
        AlphaBlendFunction = BlendFunction.Max, AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Max, ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
    };

    /// <summary>dst.a ×= src.a</summary>
    public static readonly BlendState MulA = new()
    {
        Name = "MulA", ColorWriteChannels = ColorWriteChannels.Alpha,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.SourceAlpha,
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.One,
    };

    /// <summary>dst.a ×= 1 − src.a</summary>
    public static readonly BlendState MulInvA = new()
    {
        Name = "MulInvA", ColorWriteChannels = ColorWriteChannels.Alpha,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.InverseSourceAlpha,
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.One,
    };

    /// <summary>dst.rgb ×= src.a</summary>
    public static readonly BlendState MulRgbBySrcA = new()
    {
        Name = "MulRgbBySrcA", ColorWriteChannels = Rgb,
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.SourceAlpha,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    /// <summary>dst.rgb += src.rgb × src.a</summary>
    public static readonly BlendState AddRgbBySrcA = new()
    {
        Name = "AddRgbBySrcA", ColorWriteChannels = Rgb,
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    /// <summary>dst.rgb = src.rgb·src.a + dst.rgb·(1 − src.a)</summary>
    public static readonly BlendState LerpRgbBySrcA = new()
    {
        Name = "LerpRgbBySrcA", ColorWriteChannels = Rgb,
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.SourceAlpha, ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    /// <summary>dst.rgb = max(dst.rgb, src.rgb)</summary>
    public static readonly BlendState MaxRgb = new()
    {
        Name = "MaxRgb", ColorWriteChannels = Rgb,
        ColorBlendFunction = BlendFunction.Max, ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    /// <summary>dst.rgb = src.rgb</summary>
    public static readonly BlendState CopyRgb = new()
    {
        Name = "CopyRgb", ColorWriteChannels = Rgb,
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.Zero,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };

    /// <summary>
    /// <c>WorldRT</c> 에 <c>LightRT</c> 를 곱하기(7.7): ColorSourceBlend = DestinationColor, ColorDestinationBlend = Zero. 알파는 그대로.
    /// </summary>
    public static readonly BlendState Multiply = new()
    {
        Name = "Multiply",
        ColorBlendFunction = BlendFunction.Add, ColorSourceBlend = Blend.DestinationColor, ColorDestinationBlend = Blend.Zero,
        AlphaBlendFunction = BlendFunction.Add, AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };
}

/// <summary>SpriteBatch 도우미: 실수 사각형으로 그리기(그림 배율 s 에서 반 픽셀 자리를 잃지 않게 — 위치·배율을 실수로 넘긴다, 7.1).</summary>
public static class BatchX
{
    public static void Blit(SpriteBatch batch, Texture2D tex, RectF dest, Microsoft.Xna.Framework.Rectangle src, Color color)
    {
        if (src.Width <= 0 || src.Height <= 0) return;
        var scale = new XnaVector2(dest.Width / src.Width, dest.Height / src.Height);
        batch.Draw(tex, new XnaVector2(dest.X0, dest.Y0), src, color, 0f, XnaVector2.Zero, scale, SpriteEffects.None, 0f);
    }

    /// <summary>텍스처 전체를 가운데 <paramref name="center"/>, 크기 <paramref name="size"/> 로.</summary>
    public static void Centered(SpriteBatch batch, Texture2D tex, Vector2 center, Vector2 size, Color color) =>
        batch.Draw(tex, new XnaVector2(center.X - size.X * 0.5f, center.Y - size.Y * 0.5f), null, color, 0f, XnaVector2.Zero,
                   new XnaVector2(size.X / tex.Width, size.Y / tex.Height), SpriteEffects.None, 0f);

    /// <summary>두 점 사이 선(두께 px).</summary>
    public static void Line(SpriteBatch batch, Texture2D pixel, Vector2 a, Vector2 b, float width, Color color)
    {
        var d = b - a;
        var len = d.Length();
        if (len <= 0.01f) return;
        batch.Draw(pixel, new XnaVector2(a.X, a.Y), null, color, MathF.Atan2(d.Y, d.X), new XnaVector2(0f, 0.5f),
                   new XnaVector2(len, width), SpriteEffects.None, 0f);
    }
}
