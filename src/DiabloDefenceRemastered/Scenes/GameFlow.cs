using System.Runtime.CompilerServices;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Rendering;
using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 화면 흐름의 공용 맥락 (WP6). <see cref="AppServices"/> 하나에 하나(<see cref="Of"/>) — 씬끼리 넘기는 것(자동 조작 운전자·앱 틱·꺼진 내용·화면 신호)을 한 곳에 둔다.
/// 계약 <see cref="AppServices"/> 를 고치지 않고 WP6 이 더한 것이다(설계서 13장 WP6 구현 기록).
/// <para>앱(WP7)이 쓰는 것: <see cref="Autoplay"/>(앱 틱마다 <c>Step</c>), <see cref="Moment"/>(<c>--capture-on</c>·<c>--exit-on-result</c>),
/// <see cref="QuitRequested"/>(타이틀 <c>종료</c>), <see cref="SettingsChanged"/>(전체 화면 적용), <see cref="RequestPause"/>(창이 비활성 — 3.3).</para>
/// </summary>
public sealed class GameFlow : IAppSignals
{
    private static readonly ConditionalWeakTable<AppServices, GameFlow> Flows = new();
    private UiCanvas? _canvas;

    private GameFlow(AppServices app)
    {
        App = app;
        Autoplay = app.Options.Autoplay ? new AutoplayDriver(app.Options.AutoplayVisit) : null;
        app.Signals ??= this;   // v0.9 WP1g: 계약 자리(IAppSignals) — 앱 쪽이 GameFlow 를 몰라도 받게(WP6 요청 2)
    }

    /// <summary>이 앱의 흐름 맥락(처음 부르면 만든다).</summary>
    public static GameFlow Of(AppServices app) => Flows.GetValue(app, a => new GameFlow(a));

    public AppServices App { get; }
    public GameDefs Defs => App.Defs;
    public IAppLog Log => App.Log;

    /// <summary><c>--autoplay</c> 면 운전자, 아니면 null.</summary>
    public AutoplayDriver? Autoplay { get; }

    /// <summary>지금 앱 틱(맨 위 씬의 <c>FixedTick</c> 이 넣는다 — <c>SCENE</c> 로그·메뉴 연출의 기준).</summary>
    public AppTick Now { get; set; }

    /// <summary>있는 자원 묶음(6.17). 시작 검사(WP7)가 <c>SceneFactory.CreateStart</c> 로 넘긴다 — 없으면 모두 있음.</summary>
    public ContentAvailability Content { get; set; } = ContentAvailability.All;

    /// <summary>
    /// 화면이 다 열린 순간(<c>--capture-on</c> 이름 그대로: <c>levelup</c>·<c>chest</c>·<c>boss</c>·<c>result</c>·<c>doors</c>·<c>reward</c>). 같은 이름이 여러 번 올 수 있다 — "처음" 은 받는 쪽이 고른다.
    /// </summary>
    public event Action<string>? Moment;

    /// <summary>타이틀 <c>종료</c>·데이터 오류 화면에서 나감.</summary>
    public event Action? QuitRequested;

    /// <summary>설정을 바꿈(저장 뒤). 전체 화면 적용은 창을 가진 쪽(WP7).</summary>
    public event Action<GameSettings>? SettingsChanged;

    /// <summary>판 시작 때 그 판 결과 화면이 쓰는 시작 씨앗(다시 하기 = 새 씨앗).</summary>
    public ulong NextSeed { get; set; }

    /// <summary>창이 비활성이 됨 → 사람 판이면 일시정지(3.3). 자동 조작·<c>--background</c> 는 앱이 부르지 않는다.</summary>
    public void RequestPause() => PauseRequested = true;

    internal bool PauseRequested { get; set; }

    public void Signal(string moment) => Moment?.Invoke(moment);

    public void Quit() => QuitRequested?.Invoke();

    /// <summary>씬이 들어올 때 한 줄(9절 <c>SCENE &lt;이름&gt; app_tick=…</c>).</summary>
    public void LogScene(string name) => Log.Info(RunLog.Scene(name, Now));

    // ── 설정 ─────────────────────────────────────────

    /// <summary>음량 셋(저장에 없으면 <c>rules.audio.defaults</c> — 8.3).</summary>
    public (float Master, float Sfx, float Music) Volumes(GameSettings s)
    {
        var d = Defs.Rules.Audio?.Defaults ?? new VolumeDefaultsDef();
        return (s.MasterVolume ?? d.Master, s.SfxVolume ?? d.Sfx, s.MusicVolume ?? d.Music);
    }

    public void ApplySettings(GameSettings s)
    {
        App.Save.SaveSettings(s);
        var (m, fx, mu) = Volumes(s);
        App.Audio?.SetVolumes(m, fx, mu);
        SettingsChanged?.Invoke(s);
    }

    // ── 그리기 ───────────────────────────────────────

    /// <summary>UI 캔버스(그림 서비스가 생긴 뒤 처음 그릴 때 만든다). 그래픽 없는 실행이면 null.</summary>
    public UiCanvas? Canvas
    {
        get
        {
            if (_canvas is null && App.Art is not null) _canvas = new UiCanvas(App);
            return _canvas;
        }
    }

    /// <summary>마우스의 가상 좌표(창 밖·캔버스 없음이면 null).</summary>
    public Vector2? MouseVirtual(InputFrame input)
    {
        if (!input.MouseInWindow || Canvas is not { } c) return null;
        return c.ToVirtual(new Vector2(input.MouseScreen.X, input.MouseScreen.Y));
    }

    // ── 판 시작 ──────────────────────────────────────

    /// <summary>직업이 꺼졌나(묶음 없음, 6.17).</summary>
    public bool IsDisabled(HeroClassDef hero) => Content.MissingFor(hero).Count > 0;

    /// <summary>기본 직업(<c>rules.hero.class</c>). 데이터에 없으면 null(시험 데이터).</summary>
    public HeroClassDef? DefaultHero()
    {
        var r = Defs.Rules.Hero?.Class;
        if (r is null) return null;
        return r.IsBound ? r.Def : Defs.HeroClasses.TryGet(r.Id, out var h) ? h : null;
    }

    /// <summary>
    /// 이 판의 직업(v0.6, 6.16): <c>--hero</c> → 저장의 <c>last_hero</c>(데이터에 있고 켜졌을 때) → <c>rules.hero.class</c>.
    /// <c>--hero</c> 의 없는 ID·꺼진 직업은 옵션 검사(WP7)가 먼저 끝낸다 — 여기서는 없으면 기본으로 내려간다.
    /// </summary>
    public HeroClassDef? ResolveHero()
    {
        if (App.Options.HeroClass is { } id && Defs.HeroClasses.TryGet(id, out var forced)) return forced;
        if (App.Save.LastHero is { } last && Defs.HeroClasses.TryGet(last, out var saved) && !IsDisabled(saved)) return saved;
        return DefaultHero();
    }

    /// <summary>
    /// 직업 고르기 화면을 거치나(직업 단계 — WP9): 켜진 직업이 둘 이상이고 <c>--hero</c> 가 없고 자동 조작이 아닐 때.
    /// 자동 조작은 고르기 없이 <see cref="ResolveHero"/>(9절 완료 기준 "타이틀 → 플레이, SCENE 두 줄").
    /// </summary>
    public bool NeedsHeroSelect()
    {
        if (App.Options.HeroClass is not null || Autoplay is not null) return false;
        var enabled = 0;
        foreach (var h in Defs.HeroClasses.All)
            if (!IsDisabled(h)) enabled++;
        return enabled > 1;
    }

    /// <summary>
    /// 판 시작 입력(3.6 — <see cref="AppOptions"/> 의 씨앗·<c>StageId</c>·<c>StartRoom</c>·직업·<c>Run</c>). 시작 액트가 없으면(데이터 오류) 예외.
    /// </summary>
    public RunStart CreateRunStart(HeroClassDef? hero, ulong seed)
    {
        var o = App.Options;
        StageDef act;
        var index = 0;
        if (o.StartRoom is { } room)
        {
            act = RunPlanner.ActAt(Defs, room.Act) ?? throw new InvalidOperationException($"--room {room}: 액트 {room.Act} 이 없다");
            index = -1;
            for (var i = 0; i < act.Rooms.Count; i++)
                if (act.Rooms[i].Number == room.Room) { index = i; break; }
            if (index < 0) throw new InvalidOperationException($"--room {room}: 액트 '{act.Id}' 에 방 번호 {room.Room} 이 없다");
        }
        else if (o.StageId is { } id)
            act = Defs.Stages.TryGet(id, out var s) ? s : throw new InvalidOperationException($"--stage {id}: 없는 액트");
        else
            act = RunPlanner.Root(Defs) ?? throw new InvalidOperationException("뿌리 액트가 없다(스테이지 데이터 없음)");
        return new RunStart(seed, act, hero, o.Run, index);
    }

    /// <summary>다시 하기의 새 씨앗(결정적 — 같은 씨앗의 자동 실행이 같은 두 번째 판을 돈다).</summary>
    public static ulong Reseed(ulong seed) => unchecked(seed * 6364136223846793005UL + 1442695040888963407UL);
}
