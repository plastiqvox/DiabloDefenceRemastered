namespace DiabloDefenceRemastered.Tests.Common;

public class TickAccumulatorTests
{
    private static readonly TimeSpan Frame60 = TimeSpan.FromSeconds(1.0 / 60.0);

    [Fact]
    public void OneFrame_OneTick_AtScale1()
    {
        var acc = new TickAccumulator();
        var ticks = new List<long>();
        for (var i = 0; i < 10; i++) acc.RunFrame(Frame60, 1, ticks.Add);
        Assert.Equal(Enumerable.Range(0, 10).Select(i => (long)i), ticks);
        Assert.Equal(0, acc.DroppedTicks);
        Assert.InRange(acc.Alpha, 0f, 1f);
    }

    [Fact]
    public void TickCap_IsEightPerFrame_AndCountsDropped()
    {
        var acc = new TickAccumulator();
        // 0.2s = 12틱 → 8틱만 돌고 4틱 버림
        var n = acc.Advance(TimeSpan.FromSeconds(0.2), 1);
        Assert.Equal(8, n);
        Assert.Equal(4, acc.DroppedTicks);
    }

    [Fact]
    public void Elapsed_IsClampedTo250ms()
    {
        var acc = new TickAccumulator();
        // 3초 멈춤 → 0.25s = 15틱으로 보고, 그중 8틱
        var n = acc.Advance(TimeSpan.FromSeconds(3), 1);
        Assert.Equal(8, n);
        Assert.Equal(7, acc.DroppedTicks);
    }

    [Fact]
    public void TimeScale_RaisesCap()
    {
        var acc = new TickAccumulator();
        Assert.Equal(8, acc.Advance(Frame60, 8));
        // 0.25s × 8 = 120틱 → 상한 8 × 8 = 64
        var big = new TickAccumulator();
        Assert.Equal(64, big.Advance(TimeSpan.FromSeconds(0.25), 8));
        Assert.Equal(56, big.DroppedTicks);
    }

    [Fact]
    public void CaptureStop_HaltsBeforeThatTick_AndDefersRest()
    {
        var acc = new TickAccumulator();
        acc.AddStop(60);   // --capture-at 1
        var ticks = new List<long>();
        FrameTicks r = default;
        var frames = 0;
        while (!r.Stopped && frames++ < 100) r = acc.RunFrame(Frame60, 8, ticks.Add);

        Assert.True(r.Stopped);
        Assert.Equal(60, acc.AppTick);              // 틱 0..59 를 돌고 60 직전에 멈춤
        Assert.Equal(59, ticks[^1]);
        Assert.Equal(4, r.Ticks);                   // 8틱 프레임의 56..59

        var next = acc.RunFrame(Frame60, 8, ticks.Add);
        Assert.False(next.Stopped);                 // 한 지점은 한 번만
        Assert.Equal(60, ticks[60]);                // 미룬 틱부터 이어 감
        Assert.Equal(Enumerable.Range(0, ticks.Count).Select(i => (long)i), ticks);
    }

    [Fact]
    public void TimeScale1And8_SameAppTickSequence_AndSameStops()
    {
        static (List<long> Ticks, List<long> Stops) Run(double scale)
        {
            var acc = new TickAccumulator();
            foreach (var s in new long[] { 60, 61, 125, 600 }) acc.AddStop(s);
            var ticks = new List<long>();
            var stops = new List<long>();
            for (var frame = 0; frame < 2000 && acc.AppTick < 700; frame++)
            {
                var r = acc.RunFrame(Frame60, scale, ticks.Add);
                if (r.Stopped) stops.Add(acc.AppTick);
            }
            return (ticks.Take(700).ToList(), stops);
        }

        var one = Run(1);
        var eight = Run(8);
        Assert.Equal(700, one.Ticks.Count);
        Assert.Equal(one.Ticks, eight.Ticks);
        Assert.Equal(new long[] { 60, 61, 125, 600 }, one.Stops);
        Assert.Equal(one.Stops, eight.Stops);
    }

    [Fact]
    public void StopAtFrameBoundary_StopsThatFrame()
    {
        var acc = new TickAccumulator();
        acc.AddStop(8);
        var r = acc.RunFrame(Frame60, 8, _ => { });
        Assert.True(r.Stopped);
        Assert.Equal(8, r.Ticks);
        Assert.Equal(8, acc.AppTick);
    }
}
