namespace DiabloDefenceRemastered.Tests.Common;

public class SimRandomTests
{
    private static uint[] Draw(RandomStream s, int n)
    {
        var a = new uint[n];
        for (var i = 0; i < n; i++) a[i] = s.NextUInt();
        return a;
    }

    [Fact]
    public void SameSeed_SameSequence_PerStream()
    {
        var a = new SimRandom(42);
        var b = new SimRandom(42);
        foreach (var stream in Enum.GetValues<RngStream>())
            Assert.Equal(Draw(a[stream], 100), Draw(b[stream], 100));
    }

    [Fact]
    public void DifferentSeed_DifferentSequence()
    {
        Assert.NotEqual(Draw(new SimRandom(1).Spawn, 16), Draw(new SimRandom(2).Spawn, 16));
    }

    [Fact]
    public void Streams_AreIndependent()
    {
        var a = new SimRandom(7);
        var b = new SimRandom(7);
        // a 는 Ai 를 더 뽑는다 — Spawn 결과는 그대로여야 한다
        Draw(a.Ai, 37);
        Assert.Equal(Draw(a.Spawn, 50), Draw(b.Spawn, 50));
        Assert.NotEqual(Draw(new SimRandom(7).Spawn, 16), Draw(new SimRandom(7).Ai, 16));
    }

    [Fact]
    public void Ranges_StayInBounds()
    {
        var s = new SimRandom(3).Weapon;
        bool sawMin = false, sawMax = false;
        for (var i = 0; i < 10_000; i++)
        {
            var f = s.NextFloat();
            Assert.InRange(f, 0f, 0.99999994f);
            var r = s.RangeInclusive(1, 3);
            Assert.InRange(r, 1, 3);
            sawMin |= r == 1;
            sawMax |= r == 3;
            Assert.InRange(s.Index(5), 0, 4);
        }
        Assert.True(sawMin && sawMax);
    }

    [Fact]
    public void State_ReflectsDraws()
    {
        var s = new SimRandom(9).Drop;
        var before = s.GetState();
        s.NextUInt();
        Assert.NotEqual(before, s.GetState());
    }
}

public class StableHashTests
{
    [Fact]
    public void SameInput_SameOutput()
    {
        Assert.Equal(StableHash.Of(1u, 2u), StableHash.Of(1u, 2u));
        Assert.Equal(StableHash.Of("skeleton"), StableHash.Of("skeleton"));
        Assert.NotEqual(StableHash.Of(1u, 2u), StableHash.Of(2u, 1u));
        Assert.NotEqual(StableHash.Of("skeleton"), StableHash.Of("zombie"));
    }

    [Fact]
    public void ToUnit_InRange()
    {
        for (uint i = 0; i < 1000; i++) Assert.InRange(StableHash.ToUnit(StableHash.Of(i)), 0f, 0.99999994f);
    }

    [Fact]
    public void TieAngle_IsDeterministic_AndOpposite()
    {
        var a = new EntityId(3, 1);
        var b = new EntityId(7, 2);
        var ab = MathX.TieAngle(a, b);
        var ba = MathX.TieAngle(b, a);
        Assert.Equal(ab, MathX.TieAngle(a, b));
        var diff = MathX.WrapAngle(ab - ba);
        Assert.Equal(MathF.PI, diff, 1e-4f);
        var sum = MathX.TieDir(a, b) + MathX.TieDir(b, a);
        Assert.True(sum.Length() < 1e-4f);
    }

    [Fact]
    public void TieAngle_DiffersBetweenPairs()
    {
        var angles = new HashSet<float>();
        for (var i = 1; i < 20; i++) angles.Add(MathX.TieAngle(new EntityId(0, 1), new EntityId(i, 1)));
        Assert.True(angles.Count >= 18);
    }
}
