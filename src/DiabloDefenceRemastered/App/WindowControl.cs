using DiabloDefenceRemastered.Core.Dev;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 창·장치 설정 한 곳 (설계서 3.2·7.1·9절, WP7).
/// <list type="bullet">
/// <item>창을 만들기 전(<see cref="BeforeCreate"/>): <c>--background</c>·<c>--autoplay</c> 면 <c>SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN=1</c>(SDL2 는 힌트를 환경 변수에서도 읽는다 — P/Invoke 없음),
/// 개발 옵션 실행이고 <c>--sound-device default</c> 가 없으면 <c>ALSOFT_DRIVERS=null</c>(7.9 — 출력 없는 OpenAL 장치).</item>
/// <item>창(<see cref="Apply"/>): <see cref="GraphicsProfile.HiDef"/>, <c>IsFixedTimeStep = false</c>, vsync = <c>--vsync</c>, 크기 = <c>--window</c>(개발 옵션 실행 기본 1280×720),
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
        var size = options.Window ?? new Point(DevOptions.DefaultDevWindow.Width, DevOptions.DefaultDevWindow.Height);
        graphics.PreferredBackBufferWidth = size.X;
        graphics.PreferredBackBufferHeight = size.Y;
        if (!options.DevRun) graphics.IsFullScreen = settings.Fullscreen;

        game.IsFixedTimeStep = false;
        game.IsMouseVisible = true;
        game.Window.Title = "Diablo Defence Remastered";
        if (options.Background) game.InactiveSleepTime = TimeSpan.Zero;
        return graphics;
    }
}
