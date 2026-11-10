using System.Diagnostics;
using DiabloDefenceRemastered.Core.Dev;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// <c>--log-ticks &lt;csv&gt;</c> 파일 (설계서 10.3, WP7): 프레임마다 <see cref="TickProfiler.Frame"/>, 앱 60틱마다 한 줄, 끝에 <c>PERF</c> 요약을 로그에.
/// 프레임 쪽(draw·frame·draw_calls·dropped_ticks)은 여기서 재고, 월드 쪽(<c>stage_s</c>·적·투사체 수·단계 시간)은 판을 가진 씬이 <see cref="Profiler"/> 를
/// <c>Simulation.Profiler</c> 로 걸어야 찬다 — 그 길이 계약에 없어 지금은 0(13장 WP7 구현 기록).
/// </summary>
public sealed class PerfLog : IDisposable
{
    private readonly StreamWriter _csv;
    private readonly Stopwatch _frame = Stopwatch.StartNew();
    private readonly Stopwatch _draw = new();
    private long _nextRowTick = 60;

    public PerfLog(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _csv = new StreamWriter(path, append: false) { AutoFlush = true };
        _csv.WriteLine(TickProfiler.Header);
    }

    public TickProfiler Profiler { get; } = new();

    public void BeginDraw() => _draw.Restart();

    /// <summary>Present 직전.</summary>
    public void EndDraw(long appTick, long droppedTicks, int drawCalls)
    {
        _draw.Stop();
        var frameMs = _frame.Elapsed.TotalMilliseconds;
        _frame.Restart();
        Profiler.Frame(new FrameSample(frameMs, _draw.Elapsed.TotalMilliseconds, 0, drawCalls), appTick / 60.0);
        if (appTick < _nextRowTick) return;
        _nextRowTick = appTick - appTick % 60 + 60;
        _csv.WriteLine(Profiler.Row(appTick / 60.0, 0, default, droppedTicks));
    }

    public void Dispose() => _csv.Dispose();
}
