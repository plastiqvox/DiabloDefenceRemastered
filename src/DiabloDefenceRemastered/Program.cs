using DiabloDefenceRemastered.App;
using DiabloDefenceRemastered.Core.Dev;

namespace DiabloDefenceRemastered;

/// <summary>
/// 진입점 (소유 WP7, 설계서 9절): 옵션 파싱(오류 64) → <c>--validate-data</c>(창 없이 0/2) → 시작 판정(<see cref="Boot"/>) → SDL·OpenAL 환경 변수 → 창.
/// 종료 코드: 0 정상, 1 처리 안 된 예외(로그에 스택), 2 데이터 오류, 3 캡처 없음, 4 벽시계 시간 초과, 5 자원 없음·낡음·꺼진 내용, 64 옵션 오류. 끝 줄 <c>EXIT code=…</c>.
/// WinExe 라 셸이 기다리지 않는다 — 판정 근거는 로그 파일(9절).
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var (o, errors) = DevOptions.Parse(args);
        using var log = new Log(o.Log ?? Log.DefaultPath(o.DevRun, o.SaveDir ?? SaveStore.UserDir));
        var code = Run(o, errors, args, log);
        log.Exit(code);
        return code;
    }

    private static int Run(DevOptions o, IReadOnlyList<string> errors, string[] args, Log log)
    {
        try
        {
            if (errors.Count > 0)
            {
                log.Info(RunHeader.Format(o.Seed ?? 0, Boot.Config, Boot.Commit, "-", "-", "-", null, args));
                foreach (var e in errors) log.Error("OPTION " + e);
                return ExitCodes.BadOptions;
            }

            if (o.ValidateData) return ValidateData(o, args, log);

            var boot = Boot.Prepare(o, args, log);
            if (boot.ExitCode is { } early) return early;

            WindowControl.BeforeCreate(o);   // 창을 만들기 전에(SDL·OpenAL 은 장치를 열 때 환경 변수를 읽는다)
            return RunGame(boot, o, log);
        }
        catch (Exception e)
        {
            log.Error("CRASH " + e);
            return ExitCodes.Crash;
        }
    }

    /// <summary>창 없이 데이터 검사만(자원은 보지 않는다): 오류가 하나라도 있으면 2.</summary>
    private static int ValidateData(DevOptions o, string[] args, Log log)
    {
        var (_, data) = Boot.LoadData();
        log.Info(RunHeader.Format(o.Seed ?? 0, Boot.Config, Boot.Commit, RunHeader.DataHash(Boot.DataDir), "-", "-", null, args));
        Boot.LogDataErrors(data.Errors, log);
        var errorCount = data.Errors.Count(e => e.Severity == DataSeverity.Error);
        log.Info($"VALIDATE errors={errorCount} warnings={data.Errors.Count - errorCount}");
        return errorCount > 0 ? ExitCodes.DataErrors : ExitCodes.Ok;
    }

    /// <summary>
    /// 창을 띄워 돈다. 장치 초기화(소리 장치 포함)가 예외로 끝나면 로그를 쓰고 <c>ALSOFT_DRIVERS=null</c> 로 한 번 다시 만든다(7.9 — R21). 그것도 실패하면 1.
    /// </summary>
    private static int RunGame(BootResult boot, DevOptions o, Log log)
    {
        GameApp app;
        try
        {
            app = new GameApp(boot.Services!, o, boot.Screen);
        }
        catch (Exception e) when (WindowControl.FallBackToNullAudio())
        {
            log.Warn("SHELL 장치 초기화 실패 — ALSOFT_DRIVERS=null 로 다시 만든다: " + e.Message);
            log.Info("AUDIO device=\"No Output\" fallback=1");
            app = new GameApp(boot.Services!, o, boot.Screen);
        }
        using (app)
        {
            app.Run();
            return app.ExitCode;
        }
    }
}
