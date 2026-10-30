namespace DiabloDefenceRemastered.Tests.Map;

public class IsoMathTests
{
    private static readonly IsoMath Stage1 = new(40, 160);

    [Theory]
    [InlineData(40, 2560, 1408)]   // 스테이지 1(M 20)
    [InlineData(44, 2816, 1536)]   // 스테이지 2(M 22)
    public void BoxSize_Is64N_By_32NPlusH(int n, float w, float h)
    {
        Assert.Equal(new Vector2(w, h), new IsoMath(n, 160).BoxSize);
        Assert.Equal(128, new IsoMath(n, 160).H);
    }

    [Fact]
    public void CellCenter_Formula_AndTopWallReachesZero()
    {
        // C = (32N + 32(tx − ty), H + 16(tx + ty) + 16)
        Assert.Equal(new Vector2(1280, 144), Stage1.CellCenter(0, 0));
        Assert.Equal(new Vector2(1280 + 32 * 3, 128 + 16 * 7 + 16), Stage1.CellCenter(5, 2));
        // 맨 위 칸 기둥 왼쪽 위 = (C.x − 32, C.y + 16 − Hc) → y = 0 (7.6)
        Assert.Equal(0f, Stage1.CellCenter(0, 0).Y + 16 - Stage1.ColumnHeight);
    }

    [Fact]
    public void CellCenter_RoundTripsToCell_ForEveryCell()
    {
        for (var ty = 0; ty < Stage1.N; ty++)
            for (var tx = 0; tx < Stage1.N; tx++)
            {
                var c = Stage1.CellCenter(tx, ty);
                Assert.Equal(new Int2(tx, ty), Stage1.CellOf(c));
                var t = Stage1.ToTile(c);
                Assert.Equal(tx, t.Tx, 1e-4f);
                Assert.Equal(ty, t.Ty, 1e-4f);
            }
    }

    [Fact]
    public void DiamondCorners_AreCellEdges_AndRoundTrip()
    {
        // 칸 (tx, ty) 마름모 꼭짓점: 위 C − (0,16), 오른 C + (32,0) … → 그 안쪽 점은 같은 칸
        var c = Stage1.CellCenter(7, 11);
        Assert.Equal(new Int2(7, 11), Stage1.CellOf(c + new Vector2(0, -15.9f)));
        Assert.Equal(new Int2(7, 11), Stage1.CellOf(c + new Vector2(31.5f, 0)));
        Assert.Equal(new Int2(7, 11), Stage1.CellOf(c + new Vector2(-31.5f, 0)));
        Assert.Equal(new Int2(7, 11), Stage1.CellOf(c + new Vector2(0, 15.9f)));
        Assert.Equal(new Int2(8, 12), Stage1.CellOf(c + new Vector2(0, 32)));   // 바로 아래 칸

        var world = new Vector2(1000.25f, 700.5f);
        var back = Stage1.ToWorld(Stage1.ToTile(world));
        Assert.Equal(world.X, back.X, 1e-3f);
        Assert.Equal(world.Y, back.Y, 1e-3f);
    }

    [Fact]
    public void OuterDiamond_InsideAndOutside()
    {
        Assert.Equal(new Vector2(1280, 128), Stage1.DiamondTop);
        Assert.Equal(new Vector2(2560, 128 + 640), Stage1.DiamondRight);
        Assert.Equal(new Vector2(1280, 128 + 1280), Stage1.DiamondBottom);
        Assert.Equal(new Vector2(0, 128 + 640), Stage1.DiamondLeft);

        var center = (Stage1.DiamondTop + Stage1.DiamondBottom) * 0.5f;
        Assert.True(Stage1.IsInsideMap(center));
        Assert.True(Stage1.IsInsideMap(Stage1.DiamondTop + new Vector2(0, 2)));
        Assert.False(Stage1.IsInsideMap(Stage1.DiamondTop - new Vector2(0, 2)));
        Assert.False(Stage1.IsInsideMap(new Vector2(10, 10)));                    // 상자 모서리는 마름모 밖
        Assert.False(Stage1.IsInsideMap(new Vector2(2550, 1400)));
        Assert.True(Stage1.IsInsideMap(Stage1.CellCenter(0, 0)));
        Assert.True(Stage1.IsInsideMap(Stage1.CellCenter(39, 39)));
        Assert.False(Stage1.IsInsideMap(Stage1.ToWorld(new TileCoord(-4, 19.5f))));   // 입구 바깥 시작점
    }

    [Fact]
    public void SignedDistance_PositiveInside_ZeroOnEdge_NegativeOutside()
    {
        var center = (Stage1.DiamondTop + Stage1.DiamondBottom) * 0.5f;
        Assert.Equal(16f * 40 * 2f / MathF.Sqrt(5f), Stage1.SignedDistanceToBoundary(center), 1e-2f);
        Assert.Equal(0f, Stage1.SignedDistanceToBoundary(Stage1.DiamondTop), 1e-3f);
        Assert.Equal(0f, Stage1.SignedDistanceToBoundary((Stage1.DiamondTop + Stage1.DiamondRight) * 0.5f), 1e-3f);
        Assert.True(Stage1.SignedDistanceToBoundary(new Vector2(10, 10)) < 0);
        // 꼭짓점 위로 10px → 변까지 거리 10·2/√5
        Assert.Equal(-10f * 2f / MathF.Sqrt(5f), Stage1.SignedDistanceToBoundary(Stage1.DiamondTop - new Vector2(0, 10)), 1e-3f);
    }

    [Theory]
    [InlineData(19.5f, 20)]
    [InlineData(19.49f, 19)]
    [InlineData(-0.5f, 0)]
    [InlineData(-0.51f, -1)]
    [InlineData(0f, 0)]
    public void TileCoord_Cell_RoundsHalfUp(float t, int cell)
    {
        Assert.Equal(cell, new TileCoord(t, t).Cell.X);
    }

    [Fact]
    public void For_UsesMegaTilesTimesTwo_AndColumnHeight()
    {
        var ts = TestDefs.Tileset() with { ColumnHeight = 192 };
        var iso = IsoMath.For(TestDefs.Map(10, tileset: ts), ts);
        Assert.Equal(20, iso.N);
        Assert.Equal(160, iso.H);
    }
}
