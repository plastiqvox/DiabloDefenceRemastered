using DiabloDefenceRemastered.Core.Geometry;

namespace DiabloDefenceRemastered.Tests.Geometry;

/// <summary>원·사각형·다각형(오목 포함) 부호 거리, 밀어내기 방향, 선분 교차, 원 중심에 놓인 점 (설계서 10.1 <c>Geometry</c>).</summary>
public class GeoTests
{
    private static readonly Vector2[] Square = [new(0, 0), new(10, 0), new(10, 10), new(0, 10)];
    // ㄷ 자 오목 다각형(오른쪽이 파였다)
    private static readonly Vector2[] Notch = [new(0, 0), new(30, 0), new(30, 10), new(10, 10), new(10, 20), new(30, 20), new(30, 30), new(0, 30)];

    [Fact]
    public void SignedDistance_Square_OutsideInsideEdge()
    {
        Assert.Equal(5f, Geo.SignedDistance(Square, new Vector2(15, 5)), 4);
        Assert.Equal(-2f, Geo.SignedDistance(Square, new Vector2(8, 5)), 4);
        Assert.Equal(MathF.Sqrt(2f), Geo.SignedDistance(Square, new Vector2(11, 11)), 4);   // 꼭짓점
    }

    [Fact]
    public void SignedDistance_Concave_NotchIsOutside()
    {
        // 파인 곳 안(20,15)은 다각형 밖이고 가장 가까운 변(10,·)까지 5... 위아래 변(y 10·20)까지도 5
        Assert.Equal(5f, Geo.SignedDistance(Notch, new Vector2(20, 15)), 4);
        Assert.False(Geo.Contains(Notch, new Vector2(20, 15)));
        Assert.True(Geo.Contains(Notch, new Vector2(5, 15)));
        Assert.Equal(-5f, Geo.SignedDistance(Notch, new Vector2(5, 15)), 4);
    }

    [Fact]
    public void PushOut_CircleOverlappingEdge_MovesOutward()
    {
        Assert.True(Geo.PushOut(Square, new Vector2(12, 5), 4f, Vector2.UnitY, out var push));
        Assert.Equal(new Vector2(2, 0), push);
        // 중심이 안: 가장 가까운 변 밖으로 d + r
        Assert.True(Geo.PushOut(Square, new Vector2(9, 5), 3f, Vector2.UnitY, out push));
        Assert.Equal(4f, push.X, 4);
        Assert.False(Geo.PushOut(Square, new Vector2(20, 5), 3f, Vector2.UnitY, out _));
    }

    [Fact]
    public void PushOut_CenterExactlyOnEdge_UsesFallback()
    {
        Assert.True(Geo.PushOut(Square, new Vector2(10, 5), 3f, Vector2.UnitX, out var push));
        Assert.Equal(new Vector2(3, 0), push);
    }

    [Fact]
    public void Segments_IntersectTouchAndMiss()
    {
        Assert.True(Geo.SegmentsIntersect(new(0, 0), new(10, 10), new(0, 10), new(10, 0)));
        Assert.True(Geo.SegmentsIntersect(new(0, 0), new(10, 0), new(10, 0), new(10, 10)));   // 끝점 닿음
        Assert.False(Geo.SegmentsIntersect(new(0, 0), new(10, 0), new(0, 1), new(10, 1)));
        Assert.True(Geo.SegmentHitsPolygon(Square, new Vector2(-5, 5), new Vector2(5, 5)));
        Assert.True(Geo.SegmentHitsPolygon(Square, new Vector2(2, 2), new Vector2(3, 3)));    // 다각형 안 선분
        Assert.False(Geo.SegmentHitsPolygon(Square, new Vector2(-5, 15), new Vector2(15, 15)));
        Assert.True(Geo.SegmentHitsPolygon(Notch, new Vector2(20, -5), new Vector2(20, 35)));
        Assert.False(Geo.SegmentHitsPolygon(Notch, new Vector2(15, 12), new Vector2(29, 18)));   // 파인 곳 안
    }

    [Fact]
    public void PolygonDistance_TouchingAndApart()
    {
        Vector2[] right = [new(10, 0), new(20, 0), new(20, 10), new(10, 10)];
        Vector2[] far = [new(13, 14), new(20, 14), new(20, 20), new(13, 20)];
        Assert.Equal(0f, Geo.PolygonDistance(Square, right));
        Assert.Equal(5f, Geo.PolygonDistance(Square, far), 4);
    }

    [Fact]
    public void Circle_AndRectDistance()
    {
        Assert.Equal(-1f, Geo.CircleDistance(new(0, 0), 3, new(5, 0), 3), 4);
        Assert.Equal(0f, Geo.RectDistance(new(0, 0), new(10, 10), new(5, 5)));
        Assert.Equal(5f, Geo.RectDistance(new(0, 0), new(10, 10), new(13, 14)), 4);
    }
}
