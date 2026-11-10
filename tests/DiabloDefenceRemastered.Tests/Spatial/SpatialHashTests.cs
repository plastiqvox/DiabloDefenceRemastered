using DiabloDefenceRemastered.Core.Spatial;

namespace DiabloDefenceRemastered.Tests.Spatial;

/// <summary>해시 질의 = 전수 비교(거리로 거른 정확한 집합)·결정적 순서·넘침 규칙 (설계서 4.8, 10.1 <c>Spatial</c>).</summary>
public class SpatialHashTests
{
    private static (TestWorld T, SpatialHash Hash, BruteForceSpatial Brute) Scatter(int count, int seed, Func<Random, Vector2>? at = null)
    {
        var t = new TestWorldBuilder().Build();
        var rng = new Random(seed);
        var box = t.World.MapSize;
        for (var i = 0; i < count; i++)
        {
            var pos = at?.Invoke(rng) ?? new Vector2((float)rng.NextDouble() * box.X, (float)rng.NextDouble() * box.Y);
            var life = rng.Next(10) switch { 0 => LifeState.Entering, 1 => LifeState.Emerging, 2 => LifeState.Stunned, _ => LifeState.Active };
            t.AddEnemy(pos, life: life);
        }
        var hash = new SpatialHash(t.World);
        hash.Rebuild();
        return (t, hash, new BruteForceSpatial(t.World));
    }

    [Fact]
    public void Query_EqualsBruteForce_AsSets()
    {
        var (t, hash, brute) = Scatter(400, 3);
        var rng = new Random(9);
        var a = new int[512];
        var b = new int[512];
        var box = t.World.MapSize;
        for (var q = 0; q < 300; q++)
        {
            var c = new Vector2((float)rng.NextDouble() * box.X, (float)rng.NextDouble() * box.Y);
            var r = (float)rng.NextDouble() * 250f;
            var na = hash.Query(c, r, a);
            var nb = brute.Query(c, r, b);
            Assert.Equal(nb, na);
            Assert.Equal(b[..nb].Order(), a[..na].Order());

            var e = c + new Vector2((float)rng.NextDouble() * 400 - 200, (float)rng.NextDouble() * 400 - 200);
            na = hash.QueryCapsule(c, e, r * 0.3f, a);
            nb = brute.QueryCapsule(c, e, r * 0.3f, b);
            Assert.Equal(nb, na);
            Assert.Equal(b[..nb].Order(), a[..na].Order());
        }
        // 칸 수: 전수와 같다(입장·등장 중 제외)
        for (var cy = 0; cy < hash.Rows; cy++)
            for (var cx = 0; cx < hash.Cols; cx++)
                Assert.Equal(brute.CellCount(cx, cy), hash.CellCount(cx, cy));
        Assert.Equal(t.World.Enemies.Items.ToArray().Count(e => e.IsTargetable), hash.Count);
    }

    [Fact]
    public void Query_OrderIsDeterministic()
    {
        var (_, h1, _) = Scatter(300, 5);
        var (_, h2, _) = Scatter(300, 5);
        var a = new int[300];
        var b = new int[300];
        var n1 = h1.Query(new Vector2(500, 300), 300, a);
        var n2 = h2.Query(new Vector2(500, 300), 300, b);
        Assert.Equal(a[..n1], b[..n2]);
    }

    [Fact]
    public void Overflow_200InOneCell_CountAndHalveRadius()
    {
        // 한 칸(32px)에 200개: 128칸 질의는 실제 개수 200 을 돌려주고 앞 128 을 채운다. 반경을 줄여 다시 물으면 전수와 같은 집합
        var (_, hash, brute) = Scatter(200, 11, rng => new Vector2(500 + (float)rng.NextDouble() * 30f, 300 + (float)rng.NextDouble() * 30f));
        var a = new int[128];
        var b = new int[128];
        var center = new Vector2(515, 315);
        var n = hash.Query(center, 40f, a);
        var expected = brute.Query(center, 40f, new int[256]);
        Assert.Equal(expected, n);
        Assert.True(n > 128);
        var radius = 40f;
        while (n > 128)
        {
            radius *= 0.5f;
            n = hash.Query(center, radius, a);
        }
        var nb = brute.Query(center, radius, b);
        Assert.Equal(nb, n);
        Assert.Equal(b[..nb].Order(), a[..n].Order());
    }
}
