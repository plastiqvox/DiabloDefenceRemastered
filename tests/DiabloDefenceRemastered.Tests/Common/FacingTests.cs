namespace DiabloDefenceRemastered.Tests.Common;

public class FacingTests
{
    [Theory]
    [InlineData(0f, 1f, 0)]    // 남
    [InlineData(-1f, 0f, 3)]   // 서
    [InlineData(0f, -1f, 6)]   // 북
    [InlineData(1f, 0f, 9)]    // 동
    public void TwelveDirections_CompassColumns(float dx, float dy, int expected)
    {
        Assert.Equal(expected, FacingMath.ToColumn(new Vector2(dx, dy), 12));
    }

    [Theory]
    [InlineData(0f, 1f, 0)]
    [InlineData(-1f, 0f, 4)]
    [InlineData(0f, -1f, 8)]
    [InlineData(1f, 0f, 12)]
    [InlineData(-2f, 1f, 2)]   // 등각 남서(화면 (−32, 16))
    [InlineData(2f, 1f, 14)]   // 등각 남동
    [InlineData(-1f, 1f, 1)]   // 화면 45° 는 등각 보정 뒤 남남서 쪽(26.6°)
    [InlineData(1f, 1f, 15)]
    public void SixteenDirections_CompassColumns(float dx, float dy, int expected)
    {
        Assert.Equal(expected, FacingMath.ToColumn(new Vector2(dx, dy), 16));
    }

    [Fact]
    public void IsoDiagonal_ScreenMinus32Plus16_IsEightDirColumn1()
    {
        Assert.Equal(1, FacingMath.ToColumn(new Vector2(-32, 16), 8));              // 남서 그림(4.1)
        Assert.Equal(2, FacingMath.ToColumn(new Vector2(-32, 16), 16));
        Assert.Equal(1, FacingMath.ToColumn(new Vector2(-1, 1), 8, yScale: 1f));     // 보정 끔이면 화면 45° 가 8방향 열 1
        Assert.Equal(3, FacingMath.ToColumn(new Vector2(-32, 16), 16, yScale: 1f));  // 보정 끔이면 등각 남서가 16방향 열 3 으로 밀린다
    }

    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(8)]
    public void FromColumn_RoundTrips_WithIsoCorrection(int dirs)
    {
        for (var col = 0; col < dirs; col++)
        {
            var v = FacingMath.FromColumn(col, dirs);
            Assert.Equal(1f, v.Length(), 1e-5f);
            Assert.Equal(col, FacingMath.ToColumn(v, dirs));
        }
    }

    [Fact]
    public void FromColumn_EightDirColumn1_IsIsoSouthWest()
    {
        var v = FacingMath.FromColumn(1, 8);
        var expected = Vector2.Normalize(new Vector2(-2, 1));
        Assert.Equal(expected.X, v.X, 1e-5f);
        Assert.Equal(expected.Y, v.Y, 1e-5f);
    }

    [Fact]
    public void SingleDirection_IsAlwaysZero()
    {
        Assert.Equal(0, FacingMath.ToColumn(new Vector2(1, 0), 1));
    }

    [Fact]
    public void Compass_ToVector_MatchesColumns()
    {
        Assert.Equal(0, FacingMath.ToColumn(FacingMath.ToVector(Compass.South), 12));
        Assert.Equal(3, FacingMath.ToColumn(FacingMath.ToVector(Compass.West), 12));
        Assert.Equal(6, FacingMath.ToColumn(FacingMath.ToVector(Compass.North), 12));
        Assert.Equal(9, FacingMath.ToColumn(FacingMath.ToVector(Compass.East), 12));
        for (var c = 0; c < 8; c++)
            Assert.Equal(c, FacingMath.ToColumn(FacingMath.ToVector((Compass)c), 8));   // 방위 순서 = 8방향 열 순서
    }

    [Fact]
    public void Compass_SouthWest_IsIsoDiagonal()
    {
        var sw = FacingMath.ToVector(Compass.SouthWest);
        Assert.Equal(Vector2.Normalize(new Vector2(-2, 1)), sw);
        Assert.Equal(1, FacingMath.ToColumn(sw, 8));
        Assert.Equal(2, FacingMath.ToColumn(sw, 16));
    }

    [Fact]
    public void WalkRate_UsesGroundSpeed()
    {
        // 화면 가로 v → 배율 v/base, 화면 세로 v → 땅에서 2v → 배율 2v/base(4.1)
        Assert.Equal(40f, FacingMath.GroundSpeed(new Vector2(40, 0)));
        Assert.Equal(80f, FacingMath.GroundSpeed(new Vector2(0, -40)));
        Assert.Equal(0.5f, AnimClock.WalkRate(new Vector2(20, 0), 40f));
        Assert.Equal(1f, AnimClock.WalkRate(new Vector2(0, 20), 40f));
        Assert.Equal(1f, AnimClock.WalkRate(new Vector2(0, 20), 0f));   // base_speed 없음
    }
}
