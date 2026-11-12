using DiabloDefenceRemastered.Core.Dev;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 창·장치 설정 한 곳 (설계서 3.2·7.1·9절, WP7).
/// <list type="bullet">
/// <item>창을 만들기 전(<see cref="BeforeCreate"/>): <c>--background</c>·<c>--autoplay</c> 면 <c>SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN=1</c>(SDL2 는 힌트를 환경 변수에서도 읽는다 — P/Invoke 없음),
/// 개발 옵션 실행이고 <c>--sound-device default</c> 가 없으면 <c>ALSOFT_DRIVERS=null</c>(7.9 — 출력 없는 OpenAL 장치).</item>
/// <item>창(<see cref="Apply"/>): <see cref="GraphicsProfile.HiDef"/>, <c>IsFixedTimeStep = false</c>, vsync = <c>--vsync</c>, 크기 = <c>--window</c>(개발 옵션 실행 기본 1280×720, 보통 실행은 <see cref="DefaultWindow"/>),
/// 배경 실행은 <c>InactiveSleepTime = 0</c>·마우스를 가두지 않음, 보통 실행만 저장 설정의 전체 화면.</item>
/// </list>
/// </summary>
public static class WindowControl
{
    public const string NoActivationVar = "SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN";
    public const string OpenAlDriversVar = "ALSOFT_DRIVERS";

    public static void BeforeCreate(DevOptions o)
    {
        if (o.EffectiveBackground) Environment.SetEnvironmentVariable(NoActivationVar, "1");
        if (o.NullAudioDevice) Environment.SetEnvironmentVariable(OpenAlDriversVar, "null");
    }

    /// <summary>장치 초기화 실패 뒤 다시 만들 때(7.9 — R21): 출력 없는 OpenAL 장치로. 이미 그렇다면 false.</summary>
    public static bool FallBackToNullAudio()
    {
        if (Environment.GetEnvironmentVariable(OpenAlDriversVar) is not null) return false;
        Environment.SetEnvironmentVariable(OpenAlDriversVar, "null");
        return true;
    }

    public static GraphicsDeviceManager Apply(Game game, AppOptions options, GameSettings settings)
    {
        var graphics = new GraphicsDeviceManager(game)
        {
            GraphicsProfile = GraphicsProfile.HiDef,
            SynchronizeWithVerticalRetrace = options.Vsync,
        };
        var size = options.Window ?? (options.DevRun ? new Point(DevOptions.DefaultDevWindow.Width, DevOptions.DefaultDevWindow.Height) : DefaultWindow(SystemDpiScale(), DisplaySize()));
        graphics.PreferredBackBufferWidth = size.X;
        graphics.PreferredBackBufferHeight = size.Y;
        if (!options.DevRun) graphics.IsFullScreen = settings.Fullscreen;

        game.IsFixedTimeStep = false;
        game.IsMouseVisible = true;
        game.Window.Title = "Diablo Defence Remastered";
        if (options.Background) game.InactiveSleepTime = TimeSpan.Zero;
        return graphics;
    }

    /// <summary>
    /// 보통 실행의 기본 창(2026-11-13): 1280×720 × 화면 배율(96 dpi = 1 — 150% 면 1920×1080, 정수 확대 그대로라 가장 또렷). <c>app.manifest</c> 로 배율을 직접 다루므로
    /// 창 크기는 실제 픽셀이다. 화면의 90% 를 넘으면 16:9 를 지키며 줄인다. 개발 옵션 실행은 캡처가 같게 1280×720 그대로.
    /// </summary>
    public static Point DefaultWindow(float dpiScale, Point? display)
    {
        var baseSize = DevOptions.DefaultDevWindow;
        var scale = float.IsFinite(dpiScale) && dpiScale > 0f ? dpiScale : 1f;
        var w = (int)MathF.Round(baseSize.Width * scale);
        if (display is { X: > 0, Y: > 0 } d)
            w = Math.Min(w, (int)MathF.Min(d.X * 0.9f, d.Y * 0.9f * 16f / 9f));
        w = Math.Max(w, baseSize.Width / 2);
        return new Point(w, (int)MathF.Round(w * 9f / 16f));
    }

    private static float SystemDpiScale()
    {
        if (!OperatingSystem.IsWindows()) return 1f;
        try
        {
            var dpi = GetDpiForSystem();
            return dpi > 0 ? dpi / 96f : 1f;
        }
        catch (Exception e) when (e is EntryPointNotFoundException or DllNotFoundException)
        {
            return 1f;
        }
    }

    private static Point? DisplaySize()
    {
        try
        {
            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            return new Point(mode.Width, mode.Height);
        }
        catch (Exception)
        {
            return null;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();
}
