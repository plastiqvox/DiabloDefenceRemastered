using System.Globalization;

namespace DiabloDefenceRemastered.Core.Dev;

/// <summary>캡처 하나: 멈출 앱 틱과 파일 경로.</summary>
public readonly record struct CaptureSlot(long AppTick, double Seconds, string Path);

/// <summary>
/// 개발 실행 일정 (설계서 9절, WP7) — <c>--capture-at</c>·<c>--quit-after</c> 를 앱 틱으로 바꾸고 끝날 때의 종료 코드를 정한다.
/// 그래픽과 무관해 Core 에서 시험한다. 게임 <c>App/DevRunner</c> 는 멈춤 지점을 <see cref="TickAccumulator.AddStop"/> 에 걸고 이것에 묻기만 한다.
/// <list type="bullet">
/// <item>앱 초 → 앱 틱은 반올림(<c>--capture-at 1</c> → 앱 틱 60 을 돌기 직전 상태).</item>
/// <item><c>--quit-after</c> 보다 늦은 캡처가 있으면 캡처를 먼저 한다 — 끝 틱 = 둘 중 늦은 것(9절 "캡처가 남아 있으면 먼저 한다").</item>
/// <item>끝날 때 요청한 캡처(<c>--capture-at</c> 시각마다, <c>--capture-on</c> 하나)가 하나라도 안 생겼으면 종료 코드 3.</item>
/// </list>
/// </summary>
public sealed class DevSchedule
{
    private readonly HashSet<string> _written = new(StringComparer.Ordinal);

    public DevSchedule(DevOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var captures = new List<CaptureSlot>();
        foreach (var s in options.CaptureAt.Distinct().Order())
            captures.Add(new CaptureSlot(ToAppTick(s), s, options.CapturePath(s)));
        Captures = captures;
        OnEventPath = options.CaptureOn is not null ? options.Capture ?? "capture.png" : null;
        if (options.QuitAfter is { } q)
        {
            var quit = ToAppTick(q);
            foreach (var c in captures) quit = Math.Max(quit, c.AppTick);
            QuitTick = quit;
        }
    }

    /// <summary>시각 캡처(앱 틱 오름차순).</summary>
    public IReadOnlyList<CaptureSlot> Captures { get; }

    /// <summary><c>--capture-on</c> 캡처 경로(없으면 null).</summary>
    public string? OnEventPath { get; }

    /// <summary>이 앱 틱이 되면(그 틱을 돌기 전) 끝낸다. null = <c>--quit-after</c> 없음.</summary>
    public long? QuitTick { get; }

    public static long ToAppTick(double seconds) => (long)Math.Round(seconds * 60.0, MidpointRounding.AwayFromZero);

    /// <summary>이 앱 틱에 찍을 시각 캡처(없으면 null).</summary>
    public CaptureSlot? CaptureAt(long appTick)
    {
        foreach (var c in Captures)
            if (c.AppTick == appTick) return c;
        return null;
    }

    /// <summary>파일을 다 썼다.</summary>
    public void MarkWritten(string path) => _written.Add(path);

    /// <summary>다음에 돌 앱 틱이 끝 틱에 닿았나.</summary>
    public bool ShouldQuit(long nextAppTick) => QuitTick is { } q && nextAppTick >= q;

    /// <summary>요청했는데 안 생긴 캡처 경로.</summary>
    public IReadOnlyList<string> Missing()
    {
        var missing = new List<string>();
        foreach (var c in Captures)
            if (!_written.Contains(c.Path)) missing.Add(c.Path);
        if (OnEventPath is { } p && !_written.Contains(p)) missing.Add(p);
        return missing;
    }

    /// <summary>정상 끝(<c>--quit-after</c>·<c>--exit-on-result</c>)의 종료 코드: 안 생긴 캡처가 있으면 3, 아니면 0.</summary>
    public int ExitCode() => Missing().Count > 0 ? ExitCodes.CaptureMissing : ExitCodes.Ok;

    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"captures={Captures.Count} on_event={(OnEventPath is null ? 0 : 1)} quit_tick={(QuitTick is { } q ? q.ToString(CultureInfo.InvariantCulture) : "-")}");
}
