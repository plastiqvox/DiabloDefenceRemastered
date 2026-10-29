using Color = Microsoft.Xna.Framework.Color;

namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>Core 형 → MonoGame 형. 그리기 직전에만 바꾼다(설계서 1.2).</summary>
public static class XnaInterop
{
    public static XnaVector2 ToXna(this Vector2 v) => new(v.X, v.Y);

    public static Vector2 ToNumerics(this XnaVector2 v) => new(v.X, v.Y);

    /// <summary>색조 × 알파(premultiplied, 설계서 7.5).</summary>
    public static Color ToXna(this Rgb c, float alpha = 1f)
    {
        var a = Math.Clamp(alpha, 0f, 1f);
        return new Color((byte)(c.R * a), (byte)(c.G * a), (byte)(c.B * a), (byte)(255 * a));
    }
}
