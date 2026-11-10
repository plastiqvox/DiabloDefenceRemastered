using DiabloDefenceRemastered.Core.Dev;

namespace DiabloDefenceRemastered.Tests.Dev;

/// <summary>개발 실행 일정(9절 — <c>--capture-at</c>·<c>--quit-after</c>·종료 코드 3)과 로그 첫 줄(3.4) — WP7.</summary>
public sealed class DevScheduleTests
{
    private static DevOptions Parse(params string[] args)
    {
        var (o, errors) = DevOptions.Parse(args);
        Assert.Empty(errors);
        return o;
    }

    [Fact]
    public void Capture_at_and_quit_after_become_app_ticks()
    {
        var s = new DevSchedule(Parse("--quit-after", "2", "--capture", "x.png", "--capture-at", "1"));
        var c = Assert.Single(s.Captures);
        Assert.Equal(60, c.AppTick);
        Assert.Equal("x.png", c.Path);
        Assert.Equal(120, s.QuitTick);
        Assert.False(s.ShouldQuit(119));
        Assert.True(s.ShouldQuit(120));
        Assert.Equal(c, s.CaptureAt(60));
        Assert.Null(s.CaptureAt(61));
    }

    [Fact]
    public void Several_capture_times_get_suffixed_paths_in_order()
    {
        var s = new DevSchedule(Parse("--capture", "out/s.png", "--capture-at", "120,30"));
        Assert.Equal([1800L, 7200L], s.Captures.Select(c => c.AppTick));
        Assert.Equal(["out/s_30.png", "out/s_120.png"], s.Captures.Select(c => c.Path));
        Assert.Null(s.QuitTick);
    }

    [Fact]
    public void Quit_waits_for_a_later_capture()
    {
        var s = new DevSchedule(Parse("--capture", "y.png", "--capture-at", "5", "--quit-after", "2"));
        Assert.Equal(300, s.QuitTick);
    }

    [Fact]
    public void Missing_capture_gives_exit_code_3()
    {
        var s = new DevSchedule(Parse("--capture", "a.png", "--capture-at", "1,2", "--quit-after", "3"));
        Assert.Equal(ExitCodes.CaptureMissing, s.ExitCode());
        s.MarkWritten("a_1.png");
        Assert.Equal(["a_2.png"], s.Missing());
        s.MarkWritten("a_2.png");
        Assert.Equal(ExitCodes.Ok, s.ExitCode());
    }

    [Fact]
    public void Capture_on_counts_as_requested_capture()
    {
        var s = new DevSchedule(Parse("--autoplay", "--capture", "r.png", "--capture-on", "result", "--exit-on-result"));
        Assert.Equal("r.png", s.OnEventPath);
        Assert.Equal(ExitCodes.CaptureMissing, s.ExitCode());
        s.MarkWritten("r.png");
        Assert.Equal(ExitCodes.Ok, s.ExitCode());
    }

    [Fact]
    public void No_capture_no_quit_exits_ok()
    {
        var s = new DevSchedule(Parse("--autoplay"));
        Assert.Empty(s.Captures);
        Assert.Null(s.OnEventPath);
        Assert.False(s.ShouldQuit(long.MaxValue));
        Assert.Equal(ExitCodes.Ok, s.ExitCode());
    }

    [Fact]
    public void Header_line_has_reproduction_fields()
    {
        var line = RunHeader.Format(42, "Release", "abc123", "00ff", "sorcerer", "stage_cathedral", new RoomRef(1, 3),
            ["--seed", "42", "--capture", "C:\\a b\\x.png"]);
        Assert.Equal("seed=42 config=Release commit=abc123 data=00ff hero=sorcerer start=stage_cathedral room=1:3 args=\"--seed 42 --capture \\\"C:\\a b\\x.png\\\"\"", line);
        var bare = RunHeader.Format(7, "Debug", null, "none", "warrior", "a1", null, []);
        Assert.Equal("seed=7 config=Debug data=none hero=warrior start=a1 args=\"\"", bare);
    }

    [Fact]
    public void Data_hash_ignores_enumeration_order_and_sees_content()
    {
        byte[] a = [1, 2, 3], b = [4];
        var h1 = RunHeader.DataHash([("rules/x.json", a), ("maps\\m.json", b)]);
        var h2 = RunHeader.DataHash([("maps/m.json", b), ("rules/x.json", a)]);
        var h3 = RunHeader.DataHash([("maps/m.json", b), ("rules/x.json", [1, 2, 4])]);
        Assert.Equal(h1, h2);
        Assert.NotEqual(h1, h3);
        Assert.Equal(16, h1.Length);
    }

    [Fact]
    public void Commit_comes_from_informational_version()
    {
        Assert.Equal("0123456789ab", RunHeader.CommitFrom("1.0.0+0123456789abcdef0123"));
        Assert.Null(RunHeader.CommitFrom("1.0.0"));
        Assert.Null(RunHeader.CommitFrom(null));
        Assert.NotEqual(0UL, RunHeader.SeedFromClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
    }
}
