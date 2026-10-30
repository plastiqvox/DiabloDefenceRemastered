using DiabloDefenceRemastered.Core.Assets;
using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>앱 로그(WP7 Log 가 구현). 판정용 고정 문장은 설계서 9절.</summary>
public interface IAppLog
{
    void Info(string line);
    void Warn(string line);
    void Error(string line);
}

/// <summary>
/// 설정(저장 파일의 일부, 설계서 8.3). 음량은 0~1(10% 단위). 음량 null = 저장 파일에 없음 → <c>rules.audio.defaults</c>(80·80·60%).
/// 기본값을 여기 두지 않는다 — 두 곳에 있으면 rules 를 바꿔도 옛 저장 파일 사용자는 조용히 옛 값을 받는다.
/// </summary>
public sealed record GameSettings
{
    public bool Fullscreen { get; init; }
    public bool ScreenShake { get; init; } = true;
    public bool ShowMinimap { get; init; } = true;
    public bool ShowDamageNumbers { get; init; } = true;
    public float? MasterVolume { get; init; }
    public float? SfxVolume { get; init; }
    public float? MusicVolume { get; init; }
}

/// <summary>한 판 결과(저장·RESULT 로그).</summary>
public sealed record RunResult(string StageId, bool Victory, float StageTime, int Level, int Kills, int Gold);

/// <summary>
/// 저장(WP7 SaveStore 가 구현, 설계서 8.3). 개발 옵션 실행인데 --save-dir 가 없으면 사용자 파일을 읽지도 쓰지도 않고
/// 기본값(창 모드, 스테이지 1 만 열림, 기록 없음)으로 메모리에만 둔다 — 그 판단은 구현 쪽.
/// </summary>
public interface ISaveService
{
    GameSettings Settings { get; }
    int TotalGold { get; }
    bool IsUnlocked(string stageId);
    void SaveSettings(GameSettings settings);
    void RecordResult(RunResult result);
}

/// <summary>
/// 그림 하나를 그릴 때 필요한 것(<see cref="IArtServices.Sprite"/>): 페이지 텍스처(변종 적용) + 메타 + 동작.
/// atlas 는 사각형·<c>ox,oy</c> 없이는 못 그리므로 텍스처만으로는 부족하다(설계서 6.3). grid 는 <see cref="Meta"/>·<see cref="Action"/> 이 null.
/// </summary>
/// <param name="Pages">페이지 텍스처(0 = 첫 페이지). grid 는 한 장.</param>
/// <param name="Meta">atlas 페이지 메타. grid 는 null.</param>
/// <param name="Action">정의의 <c>action</c> 으로 고른 메타 동작. grid 는 null.</param>
public readonly record struct SpriteHandle(IReadOnlyList<Texture2D> Pages, SpriteMeta? Meta, SpriteMetaAction? Action)
{
    public Texture2D Texture => Pages[0];
}

/// <summary>그림 서비스(WP5 RenderServices 가 만든다): 텍스처·글꼴·커서.</summary>
public interface IArtServices
{
    /// <summary>
    /// 정의의 그림(그 변종 페이지). atlas = 페이지·메타·동작, grid = 색 키를 적용한 텍스처 한 장.
    /// 스테이지 시작 때 미리 읽은 것만 돌려준다(없으면 예외 — 미리 읽기 계산이 빠진 버그).
    /// </summary>
    SpriteHandle Sprite(SpriteDef sprite, string variant);

    /// <summary>
    /// 프레임 하나 그리기(WP6 처럼 WP5 밖에서 그리는 쪽용). <paramref name="dir"/> 는 열(0부터, <c>FacingMath.ToColumn</c>),
    /// <paramref name="frame"/> 은 <b>1부터</b>(6.1). 위치는 발(origin) 기준, 색은 premultiplied(<c>Rgb.ToXna(alpha)</c>).
    /// </summary>
    void DrawFrame(SpriteBatch batch, SpriteDef sprite, string variant, int dir, int frame, XnaVector2 pos, float scale, Color color);

    FontSystem Fonts { get; }
    MouseCursor? Cursor { get; }
}

/// <summary>
/// 소리 서비스 (설계서 7.9, 계약 WP1b, 구현 WP8 <c>Audio/AudioServices.cs</c> 가 만든다).
/// 무음(<see cref="AppOptions.Muted"/>)이어도 소리 색인으로 고르기를 그대로 돌려 <c>AUDIO</c> 수가 소리 켬과 같다.
/// </summary>
public interface IAudioServices
{
    bool Muted { get; }
    /// <summary>PlayScene 시작: 그 스테이지에 쓸 소리·곡을 미리 읽는다.</summary>
    void PreloadStage(StageDef stage);
    /// <summary>PlayScene 프레임 끝: <c>FxSystem.Consume</c> 다음, <c>buffer.Clear</c> 전.</summary>
    void Consume(PresentationBuffer buffer, World world);
    /// <summary>"menu_move", "menu_select", "level_up", "chest_open" — 연결 표 <c>ui.&lt;키&gt;</c>.</summary>
    void PlayUi(string key);
    /// <summary>"title" 또는 스테이지 ID — 연결 표 <c>music.&lt;키&gt;</c>.</summary>
    void PlayMusic(string key);
    void SetPaused(bool paused);
    /// <summary>설정 화면·시작 때(0~1).</summary>
    void SetVolumes(float master, float sfx, float music);
    /// <summary>앱 틱마다: 페이드·덕킹(앱 틱으로 센다).</summary>
    void Update(AppTick now);
}

/// <summary>
/// 앱이 쓰는 실행 옵션 (설계서 9절 중 화면·앱 쪽). WP7 DevOptions 가 파싱해 이것으로 넘긴다.
/// 시뮬레이션에 걸리는 옵션은 <see cref="Run"/>.
/// </summary>
public sealed record AppOptions
{
    /// <summary>개발 옵션 실행(--autoplay·--stage·--capture*·--preview·--validate-data·--stress 중 하나라도).</summary>
    public bool DevRun { get; init; }
    public bool Autoplay { get; init; }
    public bool Background { get; init; }
    public ulong Seed { get; init; }
    public string? StageId { get; init; }
    /// <summary>--preview 대상(sprites / enemies / effects / tiles / blend).</summary>
    public string? Preview { get; init; }
    /// <summary>--debug-nav 층(빈 목록 = 끔, "all" = 전부).</summary>
    public IReadOnlyList<string> DebugNav { get; init; } = [];
    public double TimeScale { get; init; } = 1;
    public bool Vsync { get; init; } = true;
    /// <summary>--window 크기 = 창 클라이언트 크기이자 캡처 크기.</summary>
    public Point? Window { get; init; }
    /// <summary>소리를 재생하지 않는다(--mute, 개발 옵션 실행의 기본값). 길이 색인으로 고르기 수는 그대로 센다(7.9).</summary>
    public bool Muted { get; init; }
    /// <summary>--sound-device 값("default" = 실제 기본 장치, 사용자 동의를 받고). null 이면 개발 옵션 실행은 출력 없는 장치(ALSOFT_DRIVERS=null).</summary>
    public string? SoundDevice { get; init; }
    public RunOptions Run { get; init; } = RunOptions.Default;
}

/// <summary>
/// 합성 뿌리(GameApp, WP7)가 한 번 조립해 씬에 넘기는 묶음: 정의·로그·저장·개발 옵션·그림·소리 서비스.
/// 그림 서비스는 그래픽 장치가 생긴 뒤 <c>RenderServices</c>(WP5)가 만들어 넣는다.
/// </summary>
public sealed class AppServices
{
    public required GameDefs Defs { get; init; }
    public required IReadOnlyList<DataError> DataErrors { get; init; }
    public required ModuleContext Modules { get; init; }
    public required AppOptions Options { get; init; }
    public required IAppLog Log { get; init; }
    public required ISaveService Save { get; init; }
    public IArtServices? Art { get; set; }
    /// <summary>소리 서비스. GameApp(WP7)이 <c>Audio.AudioServices.Create</c>(WP8)로 만들어 넣는다.</summary>
    public IAudioServices? Audio { get; set; }

    /// <summary>
    /// 그리기 맥락(<c>RenderServices.CreateContext</c> 결과). 그래픽 장치가 생긴 뒤 GameApp(WP7)이 넣는다.
    /// FixedTick 안에서 마우스 창 좌표를 월드로 바꿀 때(WP6 KeyboardMouseSource) <see cref="RenderContext.ScreenToWorld"/> 를 이것으로 부른다.
    /// </summary>
    public RenderContext? Render { get; set; }

    public bool HasDataErrors
    {
        get
        {
            foreach (var e in DataErrors)
                if (e.Severity == DataSeverity.Error) return true;
            return false;
        }
    }
}
