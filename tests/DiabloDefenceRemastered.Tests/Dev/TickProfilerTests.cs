using DiabloDefenceRemastered.Core.Dev;

namespace DiabloDefenceRemastered.Tests.Dev;

/// <summary>단계별 시간 재기(10.3 — <c>--log-ticks</c> 열·<c>slow_frames</c>·요약) — WP7. 시계는 가짜(1 = 1ms).</summary>
public sealed class TickProfilerTests
{
    private long _now;

    private TickProfiler Make() => new(() => _now, 1000);

    [Fact]
    public void Header_has_one_column_per_phase_and_row_matches()
    {
        var p = Make();
        var header = TickProfiler.Header.Split(',');
        Assert.Equal(8 + SimulationBuilder.PhaseCount + 7, header.Length);
        Assert.Contains("enemy_think_ms", header);
        Assert.Contains("room_flow_ms", header);
        Assert.Equal(header.Length, p.Row(1, 0, default, 0).Split(',').Length);
    }

    [Fact]
    public void Phases_ticks_and_slow_frames_are_averaged_per_row_then_reset()
    {
        var p = Make();
        for (var i = 0; i < 2; i++)
        {
            p.BeginTick();
            p.Begin(SimPhase.Weapons);
            _now += 3 + i * 2;   // 3ms, 5ms
            p.End(SimPhase.Weapons);
            _now += 1;
            p.EndTick();         // 4ms, 6ms
        }
        p.Frame(new FrameSample(10, 4, 1, 100), 1);
        p.Frame(new FrameSample(20, 6, 1, 300), 1);
        var cols = TickProfiler.Header.Split(',');
        var row = p.Row(1, 0.5, new PerfCounts(7, 8, 9, 10), 3).Split(',');
        string Col(string name) => row[Array.IndexOf(cols, name)];
        Assert.Equal("7", Col("enemies"));
        Assert.Equal("5", Col("tick_avg_ms"));
        Assert.Equal("6", Col("tick_max_ms"));
        Assert.Equal("4", Col("weapons_ms"));
        Assert.Equal("0", Col("collision_ms"));
        Assert.Equal("5", Col("draw_ms"));
        Assert.Equal("15", Col("frame_ms"));
        Assert.Equal("1", Col("slow_frames"));
        Assert.Equal("200", Col("draw_calls"));
        Assert.Equal("3", Col("dropped_ticks"));

        var next = p.Row(2, 1.5, default, 3).Split(',');
        Assert.Equal("0", next[Array.IndexOf(cols, "slow_frames")]);
        Assert.Equal("0", next[Array.IndexOf(cols, "weapons_ms")]);
    }

    [Fact]
    public void Summary_skips_first_five_seconds()
    {
        var p = Make();
        p.Frame(new FrameSample(99, 0, 0, 999), 4.9);
        for (var i = 1; i <= 100; i++) p.Frame(new FrameSample(i, 0, 0, i), 5 + i / 60.0);
        var s = p.Summary();
        Assert.StartsWith("PERF frames=100 frame_ms p50=50 p95=95 p99=99 max=100", s);
        Assert.Contains("tick_ms p50=- p95=- p99=- max=-", s);
    }
}
