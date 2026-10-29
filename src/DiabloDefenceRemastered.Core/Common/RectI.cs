namespace DiabloDefenceRemastered.Core.Common;

/// <summary>정수 사각형 [x0,y0,x1,y1](최소·최대, 데이터 표기 그대로). x1·y1 은 포함하지 않는 끝으로 쓴다.</summary>
public readonly record struct RectI(int X0, int Y0, int X1, int Y1)
{
    public int Width => X1 - X0;
    public int Height => Y1 - Y0;
    public bool IsEmpty => X1 <= X0 || Y1 <= Y0;

    public bool Contains(int x, int y) => x >= X0 && x < X1 && y >= Y0 && y < Y1;
    public bool Contains(Vector2 p) => p.X >= X0 && p.X < X1 && p.Y >= Y0 && p.Y < Y1;

    public bool Intersects(RectI o) => X0 < o.X1 && o.X0 < X1 && Y0 < o.Y1 && o.Y0 < Y1;

    public override string ToString() => $"[{X0},{Y0},{X1},{Y1}]";
}

/// <summary>실수 사각형(카메라 시야 <c>World.ViewRect</c>). 최소·최대 표기.</summary>
public readonly record struct RectF(float X0, float Y0, float X1, float Y1)
{
    public float Width => X1 - X0;
    public float Height => Y1 - Y0;
    public Vector2 Min => new(X0, Y0);
    public Vector2 Max => new(X1, Y1);
    public Vector2 Center => new((X0 + X1) * 0.5f, (Y0 + Y1) * 0.5f);

    public bool Contains(Vector2 p) => p.X >= X0 && p.X < X1 && p.Y >= Y0 && p.Y < Y1;

    /// <summary>바깥으로 m 만큼 넓힌 사각형.</summary>
    public RectF Inflate(float m) => new(X0 - m, Y0 - m, X1 + m, Y1 + m);

    /// <summary>점에서 사각형까지 거리(안이면 0).</summary>
    public float DistanceTo(Vector2 p)
    {
        var dx = MathF.Max(MathF.Max(X0 - p.X, 0f), p.X - X1);
        var dy = MathF.Max(MathF.Max(Y0 - p.Y, 0f), p.Y - Y1);
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
