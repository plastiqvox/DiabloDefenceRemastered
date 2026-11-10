using System.Globalization;
using DiabloDefenceRemastered.Core.Dev;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 개발 실행 감독 (설계서 9절, WP7): 캡처(<c>--capture-at</c> 멈춤 지점), <c>--quit-after</c>, 벽시계 감시(<c>--timeout</c>, 종료 코드 4), 끝 종료 코드(캡처 없음 3).
/// 일정 계산은 Core <see cref="DevSchedule"/>(시험됨), 여기서는 멈춤 지점을 걸고 백 버퍼를 PNG 로 쓰고 끝을 요청한다.
/// <c>--capture-on</c>·<c>--exit-on-result</c> 는 씬(WP6)이 알리는 길이 계약에 없어 아직 걸지 않는다(WP1g 요청 — 13장 WP7 구현 기록).
/// </summary>
public sealed class DevRunner : IDisposable
{
    private readonly DevSchedule _schedule;
    private readonly FrameLoop _loop;
    private readonly IAppLog _log;
    private readonly Timer? _watchdog;
    private readonly System.Diagnostics.Stopwatch _wall = System.Diagnostics.Stopwatch.StartNew();
    private CaptureSlot? _pending;

    /// <param name="exitCodeAtQuit">끝 종료 코드를 덮어쓴다(자원 안내 화면 캡처 실행은 5, 1.4 끝). null 이면 <see cref="DevSchedule.ExitCode"/>.</param>
    public DevRunner(DevOptions options, FrameLoop loop, IAppLog log, int? exitCodeAtQuit = null)
    {
        _schedule = new DevSchedule(options);
        _loop = loop;
        _log = log;
        ExitCodeAtQuit = exitCodeAtQuit;
        foreach (var c in _schedule.Captures) loop.Accumulator.AddStop(c.AppTick);
        if (options.EffectiveTimeout is { } seconds)
            _watchdog = new Timer(_ => OnTimeout(seconds), null, TimeSpan.FromSeconds(seconds), Timeout.InfiniteTimeSpan);
        log.Info("DEV " + _schedule);
    }

    public int? ExitCodeAtQuit { get; }

    /// <summary>끝내자고 정했으면 종료 코드(아니면 null). GameApp 이 Update 끝에 보고 <c>Exit()</c> 한다.</summary>
    public int? QuitRequested { get; private set; }

    /// <summary>프레임을 돈 뒤: 멈춤 지점이면 이번 Draw 에 캡처한다. 캡처가 없으면 끝 틱을 본다.</summary>
    public void AfterUpdate(FrameTicks frame)
    {
        if (frame.Stopped && _schedule.CaptureAt(_loop.Accumulator.AppTick) is { } slot)
        {
            _pending = slot;
            return;
        }
        CheckQuit();
    }

    /// <summary>이번 프레임이 캡처 프레임인가(alpha = 1, 커서 없음).</summary>
    public bool Capturing => _pending is not null;

    /// <summary>Draw 끝(Present 전): 백 버퍼를 PNG 로 쓴다.</summary>
    public void AfterDraw(GraphicsDevice device)
    {
        if (_pending is not { } slot) return;
        _pending = null;
        try
        {
            Capture(device, slot.Path);
            _schedule.MarkWritten(slot.Path);
            _log.Info(string.Create(CultureInfo.InvariantCulture, $"CAPTURE path=\"{Path.GetFullPath(slot.Path)}\" app_tick={slot.AppTick} at={slot.Seconds}"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _log.Error($"CAPTURE path=\"{slot.Path}\" 실패: {e.Message}");
        }
        CheckQuit();
    }

    private void CheckQuit()
    {
        if (QuitRequested is not null || _pending is not null) return;
        if (!_schedule.ShouldQuit(_loop.Accumulator.AppTick)) return;
        foreach (var m in _schedule.Missing()) _log.Error($"CAPTURE 안 생김 path=\"{m}\"");
        QuitRequested = ExitCodeAtQuit ?? _schedule.ExitCode();
    }

    /// <summary>백 버퍼 → PNG(알파는 255 로 — 창 바탕이 투명하게 찍히지 않게).</summary>
    public static void Capture(GraphicsDevice device, string path)
    {
        var pp = device.PresentationParameters;
        var w = pp.BackBufferWidth;
        var h = pp.BackBufferHeight;
        if (w <= 0 || h <= 0) throw new InvalidOperationException($"백 버퍼 크기 {w}x{h}");
        var pixels = new Color[w * h];
        device.GetBackBufferData(pixels);
        for (var i = 0; i < pixels.Length; i++) pixels[i].A = 255;
        using var tex = new Texture2D(device, w, h, false, SurfaceFormat.Color);
        tex.SetData(pixels);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var fs = File.Create(path);
        tex.SaveAsPng(fs, w, h);
    }

    /// <summary>벽시계 감시: 마지막 앱 틱·씬·스택 요약을 쓰고 종료 코드 4(교착·무한 루프 대비 — 감시 스레드에서 프로세스를 끝낸다).</summary>
    private void OnTimeout(double seconds)
    {
        string stack;
        try { stack = string.Join(",", _loop.Stack.Scenes.ToArray().Select(s => s.GetType().Name)); }
        catch (InvalidOperationException) { stack = "?"; }
        _log.Error(string.Create(CultureInfo.InvariantCulture,
            $"TIMEOUT wall_s={_wall.Elapsed.TotalSeconds:0.0} limit_s={seconds} app_tick={_loop.Accumulator.AppTick} dropped_ticks={_loop.Accumulator.DroppedTicks} scene={_loop.Stack.Top?.GetType().Name ?? "-"} stack=[{stack}]"));
        if (_log is Log l) l.Exit(ExitCodes.Timeout);
        Environment.Exit(ExitCodes.Timeout);
    }

    public void Dispose() => _watchdog?.Dispose();
}
