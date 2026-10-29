using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Point = Microsoft.Xna.Framework.Point;

namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>앱 로그(WP7 Log 가 구현). 판정용 고정 문장은 설계서 9절.</summary>
public interface IAppLog
{
    void Info(string line);
    void Warn(string line);
    void Error(string line);
}

/// <summary>설정(저장 파일의 일부).</summary>
public sealed record GameSettings
{
    public bool Fullscreen { get; init; }
    public bool ScreenShake { get; init; } = true;
    public bool ShowMinimap { get; init; } = true;
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

/// <summary>그림 서비스(WP5 RenderServices 가 만든다): 텍스처·글꼴·커서.</summary>
public interface IArtServices
{
    /// <summary>정의 ID 의 텍스처(색 키 적용).</summary>
    Texture2D Texture(string spriteId);
    FontSystem Fonts { get; }
    MouseCursor? Cursor { get; }
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
    /// <summary>--preview 대상(sprites / enemies / obstacles / effects).</summary>
    public string? Preview { get; init; }
    /// <summary>--debug-nav 층(빈 목록 = 끔, "all" = 전부).</summary>
    public IReadOnlyList<string> DebugNav { get; init; } = [];
    public double TimeScale { get; init; } = 1;
    public bool Vsync { get; init; } = true;
    /// <summary>--window 크기 = 창 클라이언트 크기이자 캡처 크기.</summary>
    public Point? Window { get; init; }
    public RunOptions Run { get; init; } = RunOptions.Default;
}

/// <summary>
/// 합성 뿌리(GameApp, WP7)가 한 번 조립해 씬에 넘기는 묶음: 정의·로그·저장·개발 옵션·그림 서비스.
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
