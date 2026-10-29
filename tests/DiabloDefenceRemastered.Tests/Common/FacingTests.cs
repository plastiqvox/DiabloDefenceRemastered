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
    [InlineData(-1f, 1f, 2)]   // 남서
    [InlineData(1f, 1f, 14)]   // 남동
    public void SixteenDirections_CompassColumns(float dx, float dy, int expected)
    {
        Assert.Equal(expected, FacingMath.ToColumn(new Vector2(dx, dy), 16));
    }

    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(8)]
    public void FromColumn_RoundTrips(int dirs)
    {
        for (var col = 0; col < dirs; col++)
            Assert.Equal(col, FacingMath.ToColumn(FacingMath.FromColumn(col, dirs), dirs));
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
    }
}
