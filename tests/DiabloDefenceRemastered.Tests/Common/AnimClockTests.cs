namespace DiabloDefenceRemastered.Tests.Common;

public class AnimClockTests
{
    private const float Dt = 1f / 60f;

    /// <summary>이벤트 프레임이 몇 번, 몇 번째 진행에서 났는지.</summary>
    private static (int Events, int FirstEventStep, int FinishedStep) Run(ref AnimClock clock, float frameTime, int frames, int eventFrame1, int maxSteps)
    {
        int events = 0, first = -1, finished = -1;
        for (var step = 0; step < maxSteps; step++)
        {
            var r = clock.Advance(Dt, frameTime, frames, eventFrame1);
            if ((r & AnimStep.EventFrame) != 0) { events++; if (first < 0) first = step; }
            if ((r & AnimStep.Finished) != 0 && finished < 0) finished = step;
        }
        return (events, first, finished);
    }

    [Fact]
    public void EventFrame_FiresOnce_WhenEnteringThatFrame()
    {
        var clock = new AnimClock();
        clock.Play(ClipKind.Attack, loop: false);
        // 프레임 0.06s × 12, 7번째 프레임(데이터 값 7 그대로) 시작 = 0.36s → 22번째 진행(시간 0.3667)에서 넘음
        var (events, first, finished) = Run(ref clock, 0.06f, 12, 7, 120);
        Assert.Equal(1, events);
        Assert.Equal(21, first);
        Assert.True(finished > first);
        Assert.True(clock.Done);
        Assert.Equal(11, clock.Frame(0.06f, 12));
    }

    [Fact]
    public void PlayRate_MakesEventEarlier()
    {
        var slow = new AnimClock();
        slow.Play(ClipKind.Attack, loop: false, rate: 1f);
        var fast = new AnimClock();
        fast.Play(ClipKind.Attack, loop: false, rate: 2f);
        var a = Run(ref slow, 0.06f, 12, 7, 120);
        var b = Run(ref fast, 0.06f, 12, 7, 120);
        Assert.Equal(1, b.Events);
        Assert.True(b.FirstEventStep < a.FirstEventStep);
    }

    [Fact]
    public void Loop_FiresEveryCycle()
    {
        var clock = new AnimClock();
        clock.Play(ClipKind.Walk, loop: true);
        // 길이 0.06 × 8 = 0.48s, 120틱 = 2s → 4바퀴 남짓
        var (events, _, finished) = Run(ref clock, 0.06f, 8, 4, 120);
        Assert.Equal(4, events);
        Assert.Equal(-1, finished);
        Assert.False(clock.Done);
    }

    [Fact]
    public void EventFrameOne_FiresOnFirstAdvance()
    {
        var clock = new AnimClock();
        clock.Play(ClipKind.Special, loop: false);
        var (events, first, _) = Run(ref clock, 0.06f, 8, 1, 60);
        Assert.Equal(1, events);
        Assert.Equal(0, first);
    }

    [Theory]
    [InlineData(0)]   // 없음
    [InlineData(9)]   // 프레임 수(8) 밖
    public void EventFrameOutOfRange_NeverFires(int eventFrame1)
    {
        var clock = new AnimClock();
        clock.Play(ClipKind.Special, loop: false);
        var (events, _, finished) = Run(ref clock, 0.06f, 8, eventFrame1, 60);
        Assert.Equal(0, events);
        Assert.True(finished >= 0);
    }

    [Fact]
    public void Continue_KeepsTime_ForSameClip()
    {
        var clock = new AnimClock();
        clock.Play(ClipKind.Walk, loop: true);
        clock.Advance(0.1f, 0.06f, 8);
        var t = clock.Time;
        clock.Continue(ClipKind.Walk, loop: true, rate: 1.5f);
        Assert.Equal(t, clock.Time);
        Assert.Equal(1.5f, clock.Rate);
        clock.Continue(ClipKind.Attack, loop: false);
        Assert.Equal(0f, clock.Time);
        Assert.Equal(ClipKind.Attack, clock.Clip);
    }
}

public class CameraMathTests
{
    private static readonly Vector2 Map = new(1536, 1024);
    private static readonly Vector2 View = new(960, 540);

    [Fact]
    public void Centered_WhenFarFromEdges()
    {
        var r = CameraMath.ViewRect(new Vector2(775, 495), Map, View);
        Assert.Equal(new Vector2(775, 495), r.Center);
        Assert.Equal(960f, r.Width);
    }

    [Fact]
    public void Clamped_AtMapEdges()
    {
        var tl = CameraMath.ViewRect(new Vector2(10, 10), Map, View);
        Assert.Equal(0f, tl.X0);
        Assert.Equal(0f, tl.Y0);
        var br = CameraMath.ViewRect(new Vector2(1530, 1020), Map, View);
        Assert.Equal(1536f, br.X1);
        Assert.Equal(1024f, br.Y1);
    }

    [Fact]
    public void SmallMap_IsCentered()
    {
        var r = CameraMath.ViewRect(new Vector2(0, 0), new Vector2(480, 270), View);
        Assert.Equal(new Vector2(240, 135), r.Center);
    }
}
