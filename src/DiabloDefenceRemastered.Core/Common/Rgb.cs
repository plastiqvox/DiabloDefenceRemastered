namespace DiabloDefenceRemastered.Core.Common;

/// <summary>색 [r,g,b]. 알파는 따로 둔다.</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static readonly Rgb White = new(255, 255, 255);
    public static readonly Rgb Black = new(0, 0, 0);

    /// <summary>RGB 유클리드 거리(색 구별 경고, 설계서 6.14).</summary>
    public static float Distance(Rgb a, Rgb b)
    {
        float dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
        return MathF.Sqrt(dr * dr + dg * dg + db * db);
    }

    public override string ToString() => $"[{R},{G},{B}]";
}
