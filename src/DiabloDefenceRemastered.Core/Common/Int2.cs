namespace DiabloDefenceRemastered.Core.Common;

/// <summary>정수 점·크기 [x,y]. Core 는 XNA Point 를 모른다.</summary>
public readonly record struct Int2(int X, int Y)
{
    public static readonly Int2 Zero = default;

    public static Int2 operator +(Int2 a, Int2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Int2 operator -(Int2 a, Int2 b) => new(a.X - b.X, a.Y - b.Y);

    public Vector2 ToVector2() => new(X, Y);

    public override string ToString() => $"[{X},{Y}]";
}
