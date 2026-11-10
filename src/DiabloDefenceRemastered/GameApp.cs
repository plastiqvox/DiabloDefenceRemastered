using System.Globalization;
using DiabloDefenceRemastered.App;
using DiabloDefenceRemastered.Audio;
using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Rendering;
using DiabloDefenceRemastered.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered;

/// <summary>
/// 합성 뿌리 (소유 WP7, 설계서 2.1). GameModules·Scenes/SceneFactory·Rendering/RenderServices·Audio/AudioServices 만 부른다.
/// 시작 판정(데이터·저장·자원·옵션)은 창을 띄우기 전 <see cref="Boot"/> 가 끝냈고, 여기서는 창·그래픽 장치·씬 스택·프레임 루프·개발 실행 감독을 잇는다.
/// 창·장치 설정은 <see cref="WindowControl"/>(HiDef·고정 틱 끔·vsync·크기·배경 실행).
/// 다른 묶음 본문이 아직 없으면(<see cref="NotImplementedException"/>) 빈 씬 <see cref="BlankScene"/> 과 <see cref="ShellRenderContext"/> 로 뜬다 — 앱 셸을 홀로 확인하는 길.
/// </summary>
public sealed class GameApp : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly AppServices _app;
    private readonly DevOptions _options;
    private readonly StartScreen _screen;
    private readonly int? _exitCodeAtQuit;
    private readonly SceneStack _stack = new();
    private FrameLoop? _loop;
    private DevRunner? _runner;
    private RenderContext? _render;
    private bool _quitting;
    private PerfLog? _perf;

    public GameApp(AppServices app, DevOptions options, StartScreen screen)
    {
        _app = app;
        _options = options;
        _screen = screen;
        _exitCodeAtQuit = screen == StartScreen.AssetGuide ? ExitCodes.MissingAssets : null;

        _graphics = WindowControl.Apply(this, app.Options, app.Save.Settings);
        if (options.LogTicks is { } csv) _perf = new PerfLog(csv);
    }

    /// <summary>끝 종료 코드(Run 이 돌아온 뒤 Program 이 읽는다). 창을 사람이 닫으면 0(안내 화면이면 5).</summary>
    public int ExitCode { get; private set; }

    protected override void LoadContent()
    {
        var log = _app.Log;
        log.Info(string.Create(CultureInfo.InvariantCulture,
            $"GRAPHICS profile={GraphicsDevice.GraphicsProfile} backbuffer={GraphicsDevice.PresentationParameters.BackBufferWidth}x{GraphicsDevice.PresentationParameters.BackBufferHeight} vsync={(_graphics.SynchronizeWithVerticalRetrace ? 1 : 0)}"));

        _app.Art = Try(() => RenderServices.CreateArt(GraphicsDevice, _app), "RenderServices.CreateArt");
        _render = Try(() => RenderServices.CreateContext(GraphicsDevice, _app), "RenderServices.CreateContext") ?? new ShellRenderContext(GraphicsDevice);
        if (_app.Options.Window is { } w) _render.PresentSize = w;
        _app.Render = _render;
        _app.Audio = AudioServices.Create(_app);

        _stack.Entered = scene => log.Info($"SCENE {SceneName(scene)} app_tick={_loop?.Now.Value ?? 0}");
        // 자동 조작 메뉴 넘김(AutoplayDriver)과 사람 입력(KeyboardMouseSource)은 WP6 본문 — 만드는 길이 계약에 없어 아직 잇지 않는다(13장 WP7 구현 기록)
        _loop = new FrameLoop(_stack, _app.Options.TimeScale);
        _loop.Ticked += t => _app.Audio?.Update(t);
        if (_options.DevRun || _options.QuitAfter is not null || _options.Timeout is not null)
            _runner = new DevRunner(_options, _loop, log, _exitCodeAtQuit);

        var start = _screen == StartScreen.Normal
            ? Try(() => SceneFactory.CreateStart(_app), "SceneFactory.CreateStart")
            : Try(() => SceneFactory.CreateDataError(_app), "SceneFactory.CreateDataError");
        _stack.Reset(start ?? new BlankScene());
        if (_screen == StartScreen.AssetGuide) ExitCode = ExitCodes.MissingAssets;
    }

    /// <summary>다른 묶음의 연결 파일이 아직 빈 본문이면(단계 1) 경고만 남기고 null.</summary>
    private T? Try<T>(Func<T> make, string what) where T : class
    {
        try { return make(); }
        catch (NotImplementedException)
        {
            _app.Log.Warn($"SHELL {what} 본문 없음 — 앱 셸 대체로 뜬다");
            return null;
        }
    }

    private static string SceneName(IScene scene)
    {
        var name = scene.GetType().Name;
        return name.EndsWith("Scene", StringComparison.Ordinal) && name.Length > 5 ? name[..^5] : name;
    }

    protected override void Update(GameTime gameTime)
    {
        if (_loop is null) return;
        var frame = _loop.RunFrame(gameTime.ElapsedGameTime, readInput: null);
        _runner?.AfterUpdate(frame);
        if (_runner?.QuitRequested is { } code) Quit(code);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_loop is null || _render is null) return;
        var capturing = _runner?.Capturing ?? false;
        _render.AppTick = _loop.Now;
        _render.Capturing = capturing;
        _perf?.BeginDraw();
        _stack.Draw(_render, capturing ? 1f : _loop.Accumulator.Alpha);
        _perf?.EndDraw(_loop.Now.Value, _loop.Accumulator.DroppedTicks, (int)GraphicsDevice.Metrics.DrawCount);
        _runner?.AfterDraw(GraphicsDevice);
        base.Draw(gameTime);
        if (_runner?.QuitRequested is { } code) Quit(code);
    }

    private void Quit(int code)
    {
        if (_quitting) return;
        _quitting = true;
        ExitCode = code;
        _app.Log.Info(string.Create(CultureInfo.InvariantCulture,
            $"FRAMES app_tick={_loop?.Now.Value ?? 0} dropped_ticks={_loop?.Accumulator.DroppedTicks ?? 0}"));
        if (_perf is not null) _app.Log.Info(_perf.Profiler.Summary());
        Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _runner?.Dispose();
            _perf?.Dispose();
        }
        base.Dispose(disposing);
    }
}
