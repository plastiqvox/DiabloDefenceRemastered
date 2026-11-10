using System.Reflection;
using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Core.Modules;
using Point = Microsoft.Xna.Framework.Point;

namespace DiabloDefenceRemastered.App;

/// <summary>창을 띄운 뒤 첫 씬.</summary>
public enum StartScreen : byte
{
    /// <summary><c>SceneFactory.CreateStart</c>(타이틀 / --stage·--room / --preview).</summary>
    Normal,
    /// <summary>데이터 오류 화면(보통 실행).</summary>
    DataError,
    /// <summary>자원 없음·낡음 안내 화면(DataError 화면 재사용, 1.4 끝). 끝 종료 코드 5.</summary>
    AssetGuide,
}

/// <summary>창 없이 정한 시작 결과: 창을 띄울지(<see cref="Services"/>), 아니면 바로 끝낼 종료 코드.</summary>
public sealed record BootResult(int? ExitCode, AppServices? Services, StartScreen Screen, AssetCheckResult? Assets);

/// <summary>
/// 창을 띄우기 전 시작 판정 (설계서 9절·1.4 끝, WP7): 데이터 읽기 → 첫 줄 → 저장 → 자원 검사 → 데이터 뒤 옵션 판정.
/// 개발 옵션 실행은 화면 없이 로그 + 종료 코드로 끝낸다(데이터 오류 2, 자원 5 — 캡처만 준 실행은 안내 화면을 캡처하려 창을 띄움, 옵션 64/5).
/// 그래픽과 무관하지만 실행 파일 옆 폴더(<c>Data\</c>·<c>Assets\</c>)를 읽으므로 게임 쪽에 둔다 — 판정 조각은 Core(<see cref="DevOptions"/>·<see cref="AssetStartupCheck"/>·<see cref="RunHeader"/>)가 시험된다.
/// </summary>
public static class Boot
{
    public static string DataDir => Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>모듈을 등록하고 <c>Data\</c> 를 읽는다(<c>--validate-data</c> 와 게임 시작이 같은 길).</summary>
    public static (ModuleContext Modules, DataLoadResult Data) LoadData()
    {
        var modules = GameModules.CreateContext();
        var data = DataLoader.Load(DataDir, modules.Parts, modules.Validators);
        return (modules, data);
    }

    public static void LogDataErrors(IReadOnlyList<DataError> errors, IAppLog log)
    {
        foreach (var e in errors)
        {
            if (e.Severity == DataSeverity.Error) log.Error("DATA " + e);
            else log.Warn("DATA " + e);
        }
    }

    /// <summary>빌드 구성(첫 줄 <c>config=</c>).</summary>
    public static string Config =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    public static string? Commit => RunHeader.CommitFrom(typeof(Boot).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>
    /// 시작 판정. 첫 줄을 맨 먼저 쓰도록 저장 서비스의 로그는 첫 줄 뒤로 미룬다.
    /// </summary>
    public static BootResult Prepare(DevOptions o, IReadOnlyList<string> args, Log log)
    {
        var (modules, data) = LoadData();
        var defs = data.Defs;
        var seed = o.Seed ?? RunHeader.SeedFromClock(DateTime.UtcNow);

        var pending = new BufferedLog();
        var save = SaveStore.Create(o.SaveDir, o.ProtectUserSave, defs, pending);

        var start = o.Stage ?? (o.Room is { } room ? RunPlanner.ActAt(defs, room.Act)?.Id : null) ?? RunPlanner.Root(defs)?.Id ?? "-";
        var hero = o.Hero ?? KnownHero(defs, save.LastHero) ?? (defs.Rules.Hero?.Class is { IsBound: true } rc ? rc.Def.Id : "-");
        log.Info(RunHeader.Format(seed, Config, Commit, RunHeader.DataHash(DataDir), hero, start, o.Room, args));
        pending.FlushTo(log);
        log.Info(o.NullAudioDevice ? "AUDIO device=\"No Output\"" : $"AUDIO device=\"{o.SoundDevice ?? "default"}\" muted={(o.Muted ? 1 : 0)}");

        LogDataErrors(data.Errors, log);
        var options = ToAppOptions(o, seed);
        AppServices Services(IReadOnlyList<DataError> errors) => new()
        {
            Defs = defs,
            DataErrors = errors,
            Modules = modules,
            Options = options,
            Log = log,
            Save = save,
        };

        if (data.HasErrors)
        {
            if (o.DevRun)
            {
                log.Error("DATA 데이터 오류 — 개발 옵션 실행은 화면 없이 끝낸다");
                return new BootResult(ExitCodes.DataErrors, null, StartScreen.DataError, null);
            }
            return new BootResult(null, Services(data.Errors), StartScreen.DataError, null);
        }

        var assets = AssetCheck.Run(defs, log);
        if (!assets.Ok)
        {
            if (o.DevRun && !o.CaptureOnlyRun)
            {
                log.Error($"ASSETS 자원 없음·낡음 — {assets.Command}");
                return new BootResult(ExitCodes.MissingAssets, null, StartScreen.AssetGuide, assets);
            }
            var guide = new List<DataError>(data.Errors.Count + assets.Problems.Count + 1);
            foreach (var p in assets.Problems) guide.Add(new DataError("Assets", "$", p));
            guide.Add(new DataError("Assets", "$", assets.Command));
            return new BootResult(null, Services(guide), StartScreen.AssetGuide, assets);
        }

        var optionErrors = o.CheckData(defs, assets.Availability);
        if (optionErrors.Count > 0)
        {
            foreach (var e in optionErrors) log.Error("OPTION " + e);
            var code = optionErrors.Any(e => e.ExitCode == ExitCodes.BadOptions) ? ExitCodes.BadOptions : ExitCodes.MissingAssets;
            return new BootResult(code, null, StartScreen.Normal, assets);
        }

        return new BootResult(null, Services(data.Errors), StartScreen.Normal, assets);
    }

    private static string? KnownHero(GameDefs defs, string? id) => id is not null && defs.HeroClasses.TryGet(id, out _) ? id : null;

    /// <summary>파싱한 옵션 → 앱 계약 옵션(9절 판정값을 그대로 옮김).</summary>
    public static AppOptions ToAppOptions(DevOptions o, ulong seed) => new()
    {
        DevRun = o.DevRun,
        Autoplay = o.Autoplay,
        Background = o.EffectiveBackground,
        Seed = seed,
        StageId = o.Stage,
        StartRoom = o.Room,
        Map = o.Map?.ToString(),
        HeroClass = o.Hero,
        AutoplayVisit = o.AutoplayVisit,
        Preview = o.Preview,
        DebugNav = o.DebugNav,
        Reveal = o.Reveal,
        TimeScale = o.TimeScale,
        Vsync = o.Vsync,
        Window = o.EffectiveWindow is { } w ? new Point(w.Width, w.Height) : null,
        Muted = o.Muted,
        SoundDevice = o.SoundDevice,
        Run = o.ToRunOptions(),
    };

    /// <summary>첫 줄보다 먼저 생긴 줄을 모아 두는 로그. <see cref="FlushTo"/> 뒤로는 그 로그로 바로 보낸다(저장 서비스가 이 로그를 계속 쥔다).</summary>
    private sealed class BufferedLog : IAppLog
    {
        private readonly List<(int Kind, string Line)> _lines = [];
        private IAppLog? _target;

        public void Info(string line) => Add(0, line);
        public void Warn(string line) => Add(1, line);
        public void Error(string line) => Add(2, line);

        public void FlushTo(IAppLog log)
        {
            _target = log;
            foreach (var (kind, line) in _lines) Add(kind, line);
            _lines.Clear();
        }

        private void Add(int kind, string line)
        {
            if (_target is null) _lines.Add((kind, line));
            else if (kind == 0) _target.Info(line);
            else if (kind == 1) _target.Warn(line);
            else _target.Error(line);
        }
    }
}
