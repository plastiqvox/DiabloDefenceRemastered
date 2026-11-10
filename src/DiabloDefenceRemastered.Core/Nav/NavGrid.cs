namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 16px 내비 격자와 clearance 장 (설계서 4.2, 소유 WP2). 칸 (cx, cy) 중심 = ((cx + 0.5)·16, (cy + 0.5)·16). 외곽 상자 전체를 덮는다.
/// clearance = 칸 중심에서 가장 가까운 걷기 막힘 도형·맵 경계까지 부호 있는 거리(도형 안·맵 밖이면 음수). "반경 r 이 설 수 있다" = clearance ≥ r.
/// 8방향 이웃 순서는 <see cref="Dx"/>·<see cref="Dy"/>(동·남동·남·남서·서·북서·북·북동 — 화면 기준, 0..3 직교·대각 섞임 없이 짝수 = 직교).
/// </summary>
public sealed class NavGrid
{
    /// <summary>8방향 이웃(짝수 번호 = 직교, 홀수 = 대각선).</summary>
    public static readonly int[] Dx = [1, 1, 0, -1, -1, -1, 0, 1];
    public static readonly int[] Dy = [0, 1, 1, 1, 0, -1, -1, -1];

    internal NavGrid(int width, int height, float[] clearance, int cellSize = NavConst.CellSize)
    {
        if (clearance.Length != width * height) throw new ArgumentException("clearance 크기가 격자와 다르다");
        Width = width;
        Height = height;
        CellSize = cellSize;
        Clear = clearance;
    }

    public int Width { get; }
    public int Height { get; }
    public int CellSize { get; }
    public int Count => Width * Height;
    /// <summary>칸마다 clearance(px).</summary>
    internal float[] Clear { get; }

    public float this[int cell] => Clear[cell];

    public float At(int cx, int cy) => InGrid(cx, cy) ? Clear[cy * Width + cx] : float.NegativeInfinity;

    public bool InGrid(int cx, int cy) => (uint)cx < (uint)Width && (uint)cy < (uint)Height;

    public int Index(int cx, int cy) => cy * Width + cx;

    public Int2 CellOf(Vector2 p) => new((int)MathF.Floor(p.X / CellSize), (int)MathF.Floor(p.Y / CellSize));

    /// <summary>격자 안 칸 번호(밖이면 가장자리로 자른다).</summary>
    public int ClampedIndex(Vector2 p)
    {
        var c = CellOf(p);
        return Index(Math.Clamp(c.X, 0, Width - 1), Math.Clamp(c.Y, 0, Height - 1));
    }

    public Vector2 Center(int cell) => new((cell % Width + 0.5f) * CellSize, (cell / Width + 0.5f) * CellSize);

    public Vector2 Center(int cx, int cy) => new((cx + 0.5f) * CellSize, (cy + 0.5f) * CellSize);

    public bool Passable(int cell, float radius) => Clear[cell] >= radius;

    /// <summary>
    /// 칸에서 방향 d 로 가는 걸음이 반경 radius 에 열려 있나: 이웃이 격자 안·통과 가능, 대각선이면 두 직교 이웃도 통과 가능(코너 자르기 금지, 4.3·4.4).
    /// </summary>
    public bool CanStep(int cx, int cy, int d, float radius)
    {
        var nx = cx + Dx[d];
        var ny = cy + Dy[d];
        if (!InGrid(nx, ny) || Clear[ny * Width + nx] < radius) return false;
        if ((d & 1) == 0) return true;
        return Clear[cy * Width + nx] >= radius && Clear[ny * Width + cx] >= radius;
    }

    /// <summary>이중선형 clearance(칸 중심 사이). 격자 밖 쪽은 가장자리 칸 값.</summary>
    public float Sample(Vector2 p)
    {
        var fx = p.X / CellSize - 0.5f;
        var fy = p.Y / CellSize - 0.5f;
        var x0 = (int)MathF.Floor(fx);
        var y0 = (int)MathF.Floor(fy);
        var tx = fx - x0;
        var ty = fy - y0;
        var a = AtClamped(x0, y0);
        var b = AtClamped(x0 + 1, y0);
        var c = AtClamped(x0, y0 + 1);
        var d = AtClamped(x0 + 1, y0 + 1);
        return MathX.Lerp(MathX.Lerp(a, b, tx), MathX.Lerp(c, d, tx), ty);
    }

    /// <summary>clearance 기울기(이웃 칸 차, 4.2 끝 — 벽 안으로 밀려 들어간 유닛이 나갈 방향). 평평하면 0.</summary>
    public Vector2 Gradient(Vector2 p)
    {
        var c = CellOf(p);
        var gx = AtClamped(c.X + 1, c.Y) - AtClamped(c.X - 1, c.Y);
        var gy = AtClamped(c.X, c.Y + 1) - AtClamped(c.X, c.Y - 1);
        return new Vector2(gx, gy);
    }

    private float AtClamped(int cx, int cy) =>
        Clear[Math.Clamp(cy, 0, Height - 1) * Width + Math.Clamp(cx, 0, Width - 1)];
}
