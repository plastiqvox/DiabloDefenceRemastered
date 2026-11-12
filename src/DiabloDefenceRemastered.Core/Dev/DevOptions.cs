using System.Globalization;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Core.Sim.Autoplay;

namespace DiabloDefenceRemastered.Core.Dev;

/// <summary><c>--stress &lt;적 수&gt;[,&lt;투사체 수&gt;]</c>.</summary>
public readonly record struct StressSpec(int Enemies, int? Projectiles);

/// <summary><c>--window &lt;W&gt;x&lt;H&gt;</c>.</summary>
public readonly record struct WindowSize(int Width, int Height)
{
    public override string ToString() => $"{Width}x{Height}";
}

/// <summary><c>--map &lt;id&gt;[@m]</c>(v0.8 — <c>@m</c> 은 거울판).</summary>
public readonly record struct MapChoice(string Id, bool Mirror)
{
    public override string ToString() => Mirror ? Id + MapMirror.Suffix : Id;
}

/// <summary>옵션 판정 결과 종류(설계서 9절 종료 코드).</summary>
public enum OptionProblem : byte
{
    /// <summary>모르는 옵션·잘못된 값·데이터에 없는 ID — 종료 코드 64.</summary>
    Invalid,
    /// <summary>꺼진 스테이지·직업(묶음 없음) — 종료 코드 5.</summary>
    Disabled,
}

/// <summary>데이터를 읽은 뒤의 옵션 판정 하나(<see cref="DevOptions.CheckData"/>).</summary>
public sealed record OptionError(OptionProblem Problem, string Message)
{
    public int ExitCode => Problem == OptionProblem.Disabled ? ExitCodes.MissingAssets : ExitCodes.BadOptions;
    public override string ToString() => Message;
}

/// <summary>종료 코드(설계서 9절).</summary>
public static class ExitCodes
{
    public const int Ok = 0;
    public const int Crash = 1;
    public const int DataErrors = 2;
    public const int CaptureMissing = 3;
    public const int Timeout = 4;
    public const int MissingAssets = 5;
    public const int BadOptions = 64;
}

/// <summary>
/// 개발용 실행 옵션 (설계서 9절, WP7). <see cref="Parse"/> 가 명령줄을 읽고(모르는 옵션·잘못된 값 → 오류 목록 → 종료 코드 64),
/// 데이터에 기대는 판정(없는 ID·범위·<c>--start-at</c> 상한·칸 수·꺼진 액트·직업)은 데이터를 읽은 뒤 <see cref="CheckData"/> 가 한다.
/// 적용은 하지 않는다 — 시뮬레이션 쪽은 <see cref="ToRunOptions"/>(<c>IRunSetup</c> 을 등록한 묶음이 적용, 11.3), 앱 쪽은 게임이 <c>AppOptions</c> 로 옮긴다.
/// </summary>
public sealed record DevOptions
{
    /// <summary>개발 옵션 실행의 <c>--timeout</c> 기본값(초, 벽시계).</summary>
    public const double DefaultDevTimeout = 600;
    /// <summary>개발 옵션 실행의 <c>--window</c> 기본값.</summary>
    public static readonly WindowSize DefaultDevWindow = new(1280, 720);

    public static IReadOnlyList<string> PreviewKinds { get; } = ["sprites", "enemies", "effects", "tiles", "blend", "fog", "ui"];   // ui: 단계 2 wp6 — HUD·상인 창 견본(게임 UiPreviewScene)
    public static IReadOnlyList<string> DebugNavLayers { get; } = ["grid", "flow", "astar", "shapes", "entries", "tiles", "sight"];
    public static IReadOnlyList<string> CaptureEvents { get; } = ["levelup", "chest", "boss", "result", "doors", "reward"];

    /// <summary>받은 명령줄 그대로(로그 첫 줄 <c>args=</c>).</summary>
    public IReadOnlyList<string> Args { get; init; } = [];

    public ulong? Seed { get; init; }
    public bool Autoplay { get; init; }
    public AutoplayVisit AutoplayVisit { get; init; }
    /// <summary>--background(또는 --autoplay 가 함께 켬).</summary>
    public bool Background { get; init; }
    public string? Stage { get; init; }
    public RoomRef? Room { get; init; }
    public MapChoice? Map { get; init; }
    public string? Hero { get; init; }
    public ClockTime? StartAt { get; init; }
    public double TimeScale { get; init; } = 1;
    public bool God { get; init; }
    public int? Level { get; init; }
    public IReadOnlyList<LoadoutEntry> Weapons { get; init; } = [];
    public IReadOnlyList<LoadoutEntry> Passives { get; init; } = [];
    public StressSpec? Stress { get; init; }
    public bool FreezeAi { get; init; }
    public string? Capture { get; init; }
    /// <summary>앱 시간(초) 목록. 여러 개면 파일 이름 뒤에 <c>_&lt;초&gt;</c>(<see cref="CapturePath"/>).</summary>
    public IReadOnlyList<double> CaptureAt { get; init; } = [];
    public string? CaptureOn { get; init; }
    public double? QuitAfter { get; init; }
    public bool ExitOnResult { get; init; }
    /// <summary>적은 값. 없으면 개발 옵션 실행은 <see cref="DefaultDevTimeout"/>(<see cref="EffectiveTimeout"/>).</summary>
    public double? Timeout { get; init; }
    public WindowSize? Window { get; init; }
    public bool Vsync { get; init; } = true;
    public bool Mute { get; init; }
    public bool Sound { get; init; }
    public string? SoundDevice { get; init; }
    /// <summary>--debug-nav 층. 값 없이 주면 전부.</summary>
    public IReadOnlyList<string> DebugNav { get; init; } = [];
    public bool Reveal { get; init; }
    public string? Preview { get; init; }
    public bool Check { get; init; }
    public bool ValidateData { get; init; }
    public string? Log { get; init; }
    public string? LogTicks { get; init; }
    public string? LogHash { get; init; }
    public string? SaveDir { get; init; }

    // ── 판정 ─────────────────────────────────────────────

    /// <summary>
    /// 개발 옵션 실행(저장 보호 판정, 9절): <c>--autoplay</c>·<c>--stage</c>·<c>--room</c>·<c>--map</c>·<c>--capture*</c>·<c>--preview</c>·<c>--validate-data</c>·<c>--stress</c>·
    /// <c>--reveal</c>·<c>--god</c>·<c>--level</c>·<c>--weapons</c>·<c>--passives</c>·<c>--hero</c> 중 하나라도.
    /// </summary>
    public bool DevRun =>
        Autoplay || Stage is not null || Room is not null || Map is not null || Capture is not null || CaptureAt.Count > 0 || CaptureOn is not null
        || Preview is not null || ValidateData || Stress is not null || Reveal || God || Level is not null || Weapons.Count > 0 || Passives.Count > 0 || Hero is not null;

    /// <summary>개발 옵션 실행이고 <c>--save-dir</c> 가 없으면 사용자 저장을 읽지도 쓰지도 않는다(8.3).</summary>
    public bool ProtectUserSave => DevRun && SaveDir is null;

    /// <summary>소리를 재생하지 않음: <c>--mute</c>, 또는 개발 옵션 실행인데 <c>--sound</c>·<c>--sound-device</c> 가 없음(7.9).</summary>
    public bool Muted => Mute || DevRun && !Sound && SoundDevice is null;

    /// <summary>
    /// 장치를 출력 없는 것으로 연다(<c>ALSOFT_DRIVERS=null</c>): 개발 옵션 실행이고 <c>--sound-device default</c> 가 없을 때(7.9).
    /// </summary>
    public bool NullAudioDevice => DevRun && SoundDevice is null;

    /// <summary>창을 앞으로 끌어오지 않음(<c>--background</c> 또는 <c>--autoplay</c>).</summary>
    public bool EffectiveBackground => Background || Autoplay;

    /// <summary>벽시계 감시(초). 개발 옵션 실행 기본 600, 그 밖은 적었을 때만.</summary>
    public double? EffectiveTimeout => Timeout ?? (DevRun ? DefaultDevTimeout : null);

    /// <summary>창·캡처 크기. 개발 옵션 실행 기본 1280×720.</summary>
    public WindowSize? EffectiveWindow => Window ?? (DevRun ? DefaultDevWindow : null);

    /// <summary>
    /// 안내 화면을 띄운 채 캡처하고 5 로 끝나는 실행인가(1.4 끝, 10.2): 개발 옵션이 <c>--capture*</c>·<c>--quit-after</c>·<c>--window</c>·<c>--log</c> 뿐(<c>--autoplay</c> 없음).
    /// 아니면 자원 문제는 화면 없이 로그 + 5.
    /// </summary>
    public bool CaptureOnlyRun =>
        (Capture is not null || CaptureAt.Count > 0 || CaptureOn is not null) && !Autoplay && Stage is null && Room is null && Map is null && Preview is null
        && !ValidateData && Stress is null && !Reveal && !God && Level is null && Weapons.Count == 0 && Passives.Count == 0 && Hero is null;

    /// <summary>캡처 파일 경로 목록(앱 초 → 경로). 시각이 둘 이상이면 이름 뒤에 <c>_&lt;초&gt;</c>.</summary>
    public string CapturePath(double seconds)
    {
        var path = Capture ?? "capture.png";
        if (CaptureAt.Count <= 1) return path;
        var ext = Path.GetExtension(path);
        var stem = path[..^ext.Length];
        return $"{stem}_{seconds.ToString(CultureInfo.InvariantCulture)}{ext}";
    }

    /// <summary>시뮬레이션에 걸리는 옵션(11.4). <c>--stress</c> 는 <c>--god</c> 를 함께 켠다.</summary>
    public RunOptions ToRunOptions() => new()
    {
        God = God || Stress is not null,
        Level = Level,
        Weapons = Weapons,
        Passives = Passives,
        StartAt = StartAt,
        StressEnemies = Stress?.Enemies,
        StressProjectiles = Stress?.Projectiles,
        FreezeAi = FreezeAi,
        Check = Check,
    };

    // ── 파싱 ─────────────────────────────────────────────

    /// <summary>명령줄 → 옵션 + 오류 목록(모르는 옵션·잘못된 값·서로 안 맞는 옵션). 오류가 있으면 종료 코드 64.</summary>
    public static (DevOptions Options, IReadOnlyList<string> Errors) Parse(IReadOnlyList<string> args)
    {
        var p = new Parser(args);
        var o = p.Run();
        return (o, p.Errors);
    }

    private sealed class Parser(IReadOnlyList<string> args)
    {
        public List<string> Errors { get; } = [];
        private int _i;
        private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

        public DevOptions Run()
        {
            var o = new DevOptions { Args = [.. args] };
            for (_i = 0; _i < args.Count; _i++)
            {
                var name = args[_i];
                if (!name.StartsWith("--", StringComparison.Ordinal))
                {
                    Errors.Add($"옵션이 아닌 값 '{name}'");
                    continue;
                }
                if (!_seen.Add(name) && name != "--debug-nav") Errors.Add($"{name} 를 두 번 줬다");
                switch (name)
                {
                    case "--seed": o = Value(name, s => ulong.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : (ulong?)null, "0 이상의 정수") is { } seed ? o with { Seed = seed } : o; break;
                    case "--autoplay": o = o with { Autoplay = true }; break;
                    case "--autoplay-visit":
                        if (Text(name) is { } visit)
                        {
                            if (visit == "rooms") o = o with { AutoplayVisit = AutoplayVisit.Rooms };
                            else Errors.Add($"--autoplay-visit 값은 rooms 만 된다('{visit}')");
                        }
                        break;
                    case "--background": o = o with { Background = true }; break;
                    case "--stage": o = o with { Stage = Text(name) }; break;
                    case "--room": o = Value(name, ParseRoom, "<액트>:<순번>(1부터)") is { } room ? o with { Room = room } : o; break;
                    case "--map": o = Value(name, ParseMap, "<맵 ID>[@m]") is { } map ? o with { Map = map } : o; break;
                    case "--hero": o = o with { Hero = Text(name) }; break;
                    case "--start-at": o = Value(name, s => ClockTime.TryParse(s, out var t) ? t : (ClockTime?)null, "\"mm:ss\"") is { } at ? o with { StartAt = at } : o; break;
                    case "--time-scale": o = Value(name, s => PositiveDouble(s), "0 보다 큰 수") is { } ts ? o with { TimeScale = ts } : o; break;
                    case "--god": o = o with { God = true }; break;
                    case "--level": o = Value(name, s => int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v >= 1 ? v : (int?)null, "1 이상의 정수") is { } lv ? o with { Level = lv } : o; break;
                    case "--weapons": o = Value(name, ParseLoadout, "<id:lv,…>") is { } w ? o with { Weapons = w } : o; break;
                    case "--passives": o = Value(name, ParseLoadout, "<id:lv,…>") is { } pa ? o with { Passives = pa } : o; break;
                    case "--stress": o = Value(name, ParseStress, "<적 수>[,<투사체 수>]") is { } st ? o with { Stress = st } : o; break;
                    case "--freeze-ai": o = o with { FreezeAi = true }; break;
                    case "--capture": o = o with { Capture = Text(name) }; break;
                    case "--capture-at": o = Value(name, ParseSeconds, "<초[,초…]>") is { } ca ? o with { CaptureAt = ca } : o; break;
                    case "--capture-on":
                        if (Text(name) is { } on)
                        {
                            if (CaptureEvents.Contains(on)) o = o with { CaptureOn = on };
                            else Errors.Add($"--capture-on 값은 {string.Join("|", CaptureEvents)} 중 하나다('{on}')");
                        }
                        break;
                    case "--quit-after": o = Value(name, s => NonNegativeDouble(s), "0 이상의 초") is { } qa ? o with { QuitAfter = qa } : o; break;
                    case "--exit-on-result": o = o with { ExitOnResult = true }; break;
                    case "--timeout": o = Value(name, s => PositiveDouble(s), "0 보다 큰 초") is { } to ? o with { Timeout = to } : o; break;
                    case "--window": o = Value(name, ParseWindow, "<W>x<H>") is { } win ? o with { Window = win } : o; break;
                    case "--vsync":
                        if (Text(name) is { } vs)
                        {
                            if (vs is "on" or "off") o = o with { Vsync = vs == "on" };
                            else Errors.Add($"--vsync 값은 on|off 다('{vs}')");
                        }
                        break;
                    case "--mute": o = o with { Mute = true }; break;
                    case "--sound": o = o with { Sound = true }; break;
                    case "--sound-device":
                        if (Text(name) is { } dev)
                        {
                            if (dev == "default") o = o with { SoundDevice = dev, Sound = true };
                            else Errors.Add($"--sound-device 값은 default 만 된다('{dev}')");
                        }
                        break;
                    case "--debug-nav": o = o with { DebugNav = [.. o.DebugNav, .. DebugNav()] }; break;
                    case "--reveal": o = o with { Reveal = true }; break;
                    case "--preview":
                        if (Text(name) is { } pv)
                        {
                            if (PreviewKinds.Contains(pv)) o = o with { Preview = pv };
                            else Errors.Add($"--preview 값은 {string.Join("|", PreviewKinds)} 중 하나다('{pv}')");
                        }
                        break;
                    case "--check": o = o with { Check = true }; break;
                    case "--validate-data": o = o with { ValidateData = true }; break;
                    case "--log": o = o with { Log = Text(name) }; break;
                    case "--log-ticks": o = o with { LogTicks = Text(name) }; break;
                    case "--log-hash": o = o with { LogHash = Text(name) }; break;
                    case "--save-dir": o = o with { SaveDir = Text(name) }; break;
                    default:
                        Errors.Add($"모르는 옵션 {name}{IdSuggestHint(name)}");
                        break;
                }
            }
            CrossCheck(o);
            return o;
        }

        private void CrossCheck(DevOptions o)
        {
            if (o.Room is not null && o.Stage is not null) Errors.Add("--room 은 --stage 를 겸한다 — 같이 주지 않는다");
            if (o.AutoplayVisit != AutoplayVisit.None && !o.Autoplay) Errors.Add("--autoplay-visit 은 --autoplay 와 함께만 쓴다");
            if (o.Map is not null && o.Preview is not ("tiles" or "fog")) Errors.Add("--map 은 --preview tiles·fog 에서만 쓴다");
            if (o.Mute && o.Sound) Errors.Add("--mute 와 --sound 를 같이 주지 않는다");
            if (o.CaptureAt.Count > 0 && o.Capture is null) Errors.Add("--capture-at 에는 --capture <png> 가 있어야 한다");
            if (o.CaptureOn is not null && o.Capture is null) Errors.Add("--capture-on 에는 --capture <png> 가 있어야 한다");
        }

        private string? Text(string name)
        {
            if (_i + 1 >= args.Count || args[_i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Errors.Add($"{name} 에 값이 없다");
                return null;
            }
            return args[++_i];
        }

        private T? Value<T>(string name, Func<string, T?> parse, string expect) where T : struct
        {
            if (Text(name) is not { } s) return null;
            if (parse(s) is { } v) return v;
            Errors.Add($"{name} 값 '{s}' 이 잘못됐다({expect})");
            return null;
        }

        private T? Value<T>(string name, Func<string, T?> parse, string expect, bool _ = false) where T : class
        {
            if (Text(name) is not { } s) return null;
            if (parse(s) is { } v) return v;
            Errors.Add($"{name} 값 '{s}' 이 잘못됐다({expect})");
            return null;
        }

        private IReadOnlyList<string> DebugNav()
        {
            // 값이 없거나 다음이 옵션이면 전부
            if (_i + 1 >= args.Count || args[_i + 1].StartsWith("--", StringComparison.Ordinal)) return DebugNavLayers;
            var raw = args[++_i];
            if (raw == "all") return DebugNavLayers;
            var list = new List<string>();
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (DebugNavLayers.Contains(part)) list.Add(part);
                else Errors.Add($"--debug-nav 층 '{part}' 이 없다({string.Join(",", DebugNavLayers)})");
            }
            return list;
        }

        private static string IdSuggestHint(string name)
        {
            string? best = null;
            var bestD = 3;
            foreach (var known in Known)
            {
                var d = Distance(name, known);
                if (d < bestD) { best = known; bestD = d; }
            }
            return best is null ? "" : $" (혹시 {best}?)";
        }

        private static readonly string[] Known =
        [
            "--seed", "--autoplay", "--autoplay-visit", "--background", "--stage", "--room", "--map", "--hero", "--start-at", "--time-scale", "--god",
            "--level", "--weapons", "--passives", "--stress", "--freeze-ai", "--capture", "--capture-at", "--capture-on", "--quit-after",
            "--exit-on-result", "--timeout", "--window", "--vsync", "--mute", "--sound", "--sound-device", "--debug-nav", "--reveal", "--preview",
            "--check", "--validate-data", "--log", "--log-ticks", "--log-hash", "--save-dir",
        ];

        private static int Distance(string a, string b)
        {
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) prev[j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (var j = 1; j <= b.Length; j++)
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                (prev, cur) = (cur, prev);
            }
            return prev[b.Length];
        }
    }

    private static double? PositiveDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0 && double.IsFinite(v) ? v : null;

    private static double? NonNegativeDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v >= 0 && double.IsFinite(v) ? v : null;

    private static RoomRef? ParseRoom(string s)
    {
        var parts = s.Split(':');
        if (parts.Length != 2) return null;
        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var act) || act < 1) return null;
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var room) || room < 1) return null;
        return new RoomRef(act, room);
    }

    private static MapChoice? ParseMap(string s)
    {
        var mirror = s.EndsWith(MapMirror.Suffix, StringComparison.Ordinal);
        var id = mirror ? s[..^MapMirror.Suffix.Length] : s;
        return id.Length == 0 || id.Contains('@') ? null : new MapChoice(id, mirror);
    }

    private static List<LoadoutEntry>? ParseLoadout(string s)
    {
        var list = new List<LoadoutEntry>();
        foreach (var part in s.Split(',', StringSplitOptions.TrimEntries))
        {
            var colon = part.LastIndexOf(':');
            if (colon <= 0) return null;
            if (!int.TryParse(part.AsSpan(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var lv) || lv < 1) return null;
            var id = part[..colon];
            if (list.Exists(e => e.Id == id)) return null;
            list.Add(new LoadoutEntry(id, lv));
        }
        return list;
    }

    private static StressSpec? ParseStress(string s)
    {
        var parts = s.Split(',');
        if (parts.Length is < 1 or > 2) return null;
        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var e) || e < 1) return null;
        int? pj = null;
        if (parts.Length == 2)
        {
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var p) || p < 0) return null;
            pj = p;
        }
        return new StressSpec(e, pj);
    }

    private static List<double>? ParseSeconds(string s)
    {
        var list = new List<double>();
        foreach (var part in s.Split(',', StringSplitOptions.TrimEntries))
        {
            if (NonNegativeDouble(part) is not { } v) return null;
            if (list.Contains(v)) return null;
            list.Add(v);
        }
        list.Sort();
        return list;
    }

    private static WindowSize? ParseWindow(string s)
    {
        var x = s.IndexOf('x', StringComparison.OrdinalIgnoreCase);
        if (x <= 0) return null;
        if (!int.TryParse(s.AsSpan(0, x), NumberStyles.None, CultureInfo.InvariantCulture, out var w) || w < 320 || w > 7680) return null;
        if (!int.TryParse(s.AsSpan(x + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var h) || h < 180 || h > 4320) return null;
        return new WindowSize(w, h);
    }

    // ── 데이터를 읽은 뒤의 판정 ─────────────────────────────

    /// <summary>
    /// 데이터에 기대는 옵션 판정(9절): 없는 ID·<c>--room</c> 범위·<c>--map @m</c>·<c>--start-at</c> 상한(<see cref="RoomStartLimits"/>)·
    /// 무기·패시브 수 > 시작 액트의 열린 칸(<see cref="RunSlots.OpenAt(int, RulesDef)"/>)·고를 수 없는 무기 → <see cref="OptionProblem.Invalid"/>(64),
    /// 꺼진 액트·직업 → <see cref="OptionProblem.Disabled"/>(5). <paramref name="availability"/> 가 null 이면 모두 있다고 본다.
    /// </summary>
    public IReadOnlyList<OptionError> CheckData(GameDefs defs, ContentAvailability? availability = null)
    {
        ArgumentNullException.ThrowIfNull(defs);
        availability ??= ContentAvailability.All;
        var errors = new List<OptionError>();
        void Bad(string m) => errors.Add(new OptionError(OptionProblem.Invalid, m));
        void Off(string m) => errors.Add(new OptionError(OptionProblem.Disabled, m));

        // 직업
        HeroClassDef? hero = null;
        if (Hero is { } heroId)
        {
            if (!defs.HeroClasses.TryGet(heroId, out hero)) Bad($"--hero '{heroId}' 직업이 없다");
            else if (availability.MissingFor(hero) is { Count: > 0 } miss) Off($"--hero {heroId} 는 꺼져 있다(없는 묶음: {string.Join(", ", miss.Select(ContentBundles.Name))})");
        }
        hero ??= defs.Rules.Hero?.Class is { IsBound: true } rc ? rc.Def : null;

        // 시작 액트·방
        StageDef? act = null;
        RoomDef? startRoom = null;
        if (Stage is { } stageId)
        {
            if (!defs.Stages.TryGet(stageId, out act)) Bad($"--stage '{stageId}' 액트가 없다");
        }
        else if (Room is { } room)
        {
            act = RunPlanner.ActAt(defs, room.Act);
            if (act is null) Bad($"--room {room.Act}:{room.Room} — 액트 {room.Act} 가 없다");
            else
            {
                startRoom = act.Rooms.FirstOrDefault(r => r.Number == room.Room);
                if (startRoom is null)
                    Bad($"--room {room.Act}:{room.Room} — 액트 {act.Id} 에 방 번호 {room.Room} 이 없다({string.Join(",", act.Rooms.Select(r => r.Number))})");
            }
        }
        else act = RunPlanner.Root(defs);
        if (act is not null && (Stage is not null || Room is not null) && availability.MissingFor(act) is { Count: > 0 } off)
            Off($"액트 {act.Id} 는 꺼져 있다(없는 묶음: {string.Join(", ", off.Select(ContentBundles.Name))})");
        startRoom ??= act is { Rooms.Count: > 0 } ? act.Rooms[0] : null;

        // --start-at 상한(방 종류마다 — RoomStartLimits)
        if (StartAt is { } at && startRoom is not null && defs.Rules.Rooms is { } roomRules)
        {
            var ticks = MathX.SecondsToTicks(at.Seconds, World.Dt);
            if (!RoomStartLimits.Allows(startRoom, roomRules, ticks))
            {
                var max = RoomStartLimits.MaxStartClockTicks(startRoom, roomRules);
                Bad($"--start-at {at} 가 시작 방({startRoom.Kind.ToString().ToLowerInvariant()} {startRoom.Number}번)의 상한 {new ClockTime((max ?? 0) * World.Dt)} 이상이다");
            }
        }

        // --map
        if (Map is { } map)
        {
            if (!defs.Maps.TryGet(map.Id, out var mapDef)) Bad($"--map '{map.Id}' 맵이 없다");
            else if (map.Mirror && !mapDef.Mirror) Bad($"--map {map} — 맵 {map.Id} 는 mirror: true 가 아니다");
        }

        // 무기·패시브
        var actNumber = act is null ? 1 : RunPlanner.ActNumber(defs, act);
        (int Weapons, int Passives)? open = defs.Rules.Rooms is not null ? RunSlots.OpenAt(actNumber, defs.Rules) : null;
        foreach (var w in Weapons)
        {
            if (!defs.Weapons.TryGet(w.Id, out var wd)) Bad($"--weapons '{w.Id}' 무기가 없다");
            else
            {
                if (wd.MaxLevel > 0 && w.Level > wd.MaxLevel) Bad($"--weapons {w.Id}:{w.Level} — 최대 레벨 {wd.MaxLevel} 을 넘는다");
                if (hero is not null && wd.Classes.Count > 0 && !wd.Classes.Any(c => c.Id == hero.Id)) Bad($"--weapons {w.Id} 를 직업 {hero.Id} 가 고를 수 없다");
            }
        }
        foreach (var p in Passives)
        {
            if (!defs.Passives.TryGet(p.Id, out var pd)) Bad($"--passives '{p.Id}' 패시브가 없다");
            else if (pd.MaxLevel > 0 && p.Level > pd.MaxLevel) Bad($"--passives {p.Id}:{p.Level} — 최대 레벨 {pd.MaxLevel} 을 넘는다");
        }
        if (open is { } slots)
        {
            if (Weapons.Count > slots.Weapons) Bad($"--weapons {Weapons.Count}개가 시작 액트(액트 {actNumber})의 열린 무기 칸 {slots.Weapons} 보다 많다");
            if (Passives.Count > slots.Passives) Bad($"--passives {Passives.Count}개가 시작 액트(액트 {actNumber})의 열린 패시브 칸 {slots.Passives} 보다 많다");
        }
        return errors;
    }
}
