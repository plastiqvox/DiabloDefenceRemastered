using DiabloDefenceRemastered.Core.Sim.Autoplay;
using DiabloDefenceRemastered.Input;
using DiabloDefenceRemastered.Rendering;
using DiabloDefenceRemastered.Scenes.Overlays;
using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 플레이 (설계서 8.1 — v0.7: <see cref="RunSession"/> 소유, 3.6). 앱 틱마다 명령(사람 = <see cref="KeyboardMouseSource"/>, 자동 = <c>AutoplayPolicy.Next</c>)으로
/// <see cref="StageStepper.AppTick"/> 을 한 번 부르고 결과대로:
/// <list type="bullet">
/// <item>선택 대기(레벨업·상자·보상) — 스텝 직후에 보면 <b>같은 앱 틱 안에서</b> 오버레이를 올린다(3.3).</item>
/// <item><see cref="StageStep.RoomExit"/> — <c>rules.rooms.transition</c>(층이 바뀌는 문은 <c>transition_floor</c>) 어두워짐을 앱 틱으로 세고, 가운데 앱 틱에
/// 옛 월드 표시 이벤트 소비·<c>Clear</c> → <c>RunSession.Advance()</c> → <see cref="IWorldBound.Attach"/>(<c>WorldRenderer → FxSystem → Hud·미니맵 → 소리</c>, 7.11) → 액트가 바뀌었으면 미리 읽기·음악.</item>
/// <item><see cref="StageStep.Finished"/> 를 처음 받은 앱 틱 — <c>RunSession.Conclude()</c> 한 번 → 결과 화면(<c>RESULT</c>·<c>RecordResult</c> 는 결과 화면이 그 뒤의 <c>RunState</c> 로).</item>
/// </list>
/// 표시 이벤트는 스텝마다 <c>FxSystem.Consume → Audio.Consume → Presentation.Clear</c> 순서로 소비한다(3.5·7.9 — 프레임 끝이 아니라 스텝 끝: 그리지 않는 앱 틱·방 이동 앞에서도 빠지지 않게).
/// 로그: 방이 끝날 때마다 <c>ROOM</c>·<c>INNER_ROOMS</c>, 자동 조작 정책의 새 기록마다 <c>DOOR_SKIP</c>·<c>REWARD_STALL</c>·<c>VISIT</c>·<c>VISIT_SKIP</c>, 판 끝 <c>VISITS</c>(9절).
/// </summary>
public sealed class PlayScene : IScene
{
    private readonly GameFlow _flow;
    private readonly RunSession _session;
    private readonly bool _continued;
    private readonly Hud _hud;
    private SceneStack? _stack;
    private WorldRenderer? _renderer;
    private KeyboardMouseSource? _keys;
    private AutoplayPolicy? _policy;
    private readonly IInputSource? _input;
    private StageDef? _musicAct;

    private int _loggedRooms;
    private int _loggedDoorSkips;
    private int _loggedStalls;
    private int _loggedVisits;
    private RoomPhase _lastPhase;

    private int _transitionTicks;
    private int _transitionAge = -1;
    private bool _advanced;
    private bool _finished;
    private float _lastAlpha = 1f;

    /// <param name="continued">이어 하기(계단 저장)로 연 판 — <c>SetLastHero</c> 를 다시 쓰지 않는다.</param>
    /// <param name="input">명령 출처를 바꿔 끼움(그래픽 없는 흐름 시험·다시 보기). null 이면 자동 조작 정책 또는 키보드·마우스.</param>
    public PlayScene(GameFlow flow, RunSession session, bool continued = false, IInputSource? input = null)
    {
        _flow = flow;
        _session = session;
        _continued = continued;
        _input = input;
        _hud = new Hud(flow.App);
        _loggedRooms = session.Run.Rooms.Count;   // 이어 하기면 앞 판의 방 줄은 이미 로그에 있다
    }

    public bool PausesBelow => false;
    public bool DrawsBelow => false;

    public RunSession Session => _session;
    public World World => _session.World;
    public Hud Hud => _hud;
    /// <summary>방 이동 어두워짐 중(앱 틱 수, 아니면 −1).</summary>
    public int TransitionAge => _transitionAge;
    /// <summary>판이 끝나 결과 화면으로 넘겼음.</summary>
    public bool Finished => _finished;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        _flow.LogScene("Play");
        var app = _flow.App;
        if (_input is null)
        {
            if (_flow.Autoplay is { } bot) _policy = bot.BeginRun();
            else _keys = new KeyboardMouseSource(app);
        }
        if (app.Art is not null) _renderer = new WorldRenderer(app);
        _hud.Minimap.Visible = app.Save.Settings.ShowMinimap;
        _flow.SettingsChanged += OnSettings;
        AttachAll(World);
        StartActAudio();
        _lastPhase = World.Room?.Phase ?? RoomPhase.Enter;

        if (!_continued && app.Options.HeroClass is null && World.HeroClass is { } hero) app.Save.SetLastHero(hero.Id);
        if (_policy is { Visit: AutoplayVisit.Rooms } && World.InnerRooms.Rooms.Count == 0)
            app.Log.Warn("--autoplay-visit rooms: 이 방 맵에 방 기준점(room_*)이 없다 — 보통 자동 조작");
    }

    public void Exit() => _flow.SettingsChanged -= OnSettings;

    /// <summary>판 중 설정에서 미니맵을 켜고 끔(Tab 과 같은 값).</summary>
    private void OnSettings(GameSettings s) => _hud.Minimap.Visible = s.ShowMinimap;

    public void FixedTick(in AppTick t, InputFrame input)
    {
        _flow.Now = t;
        if (_finished) return;
        if (_transitionAge >= 0)
        {
            TickTransition();
            return;
        }

        if (input.IsPressed(UiButtons.Minimap)) _hud.Minimap.Visible = !_hud.Minimap.Visible;
        if (_policy is null && (input.IsPressed(UiButtons.Pause) || _flow.PauseRequested))
        {
            _flow.PauseRequested = false;
            _stack?.Push(new PauseOverlay(_flow, this));
            return;
        }

        var world = World;
        PlayerCommand command;
        if (_input is not null) command = _input.Next(world);
        else if (_policy is not null) command = _policy.Next(world);
        else
        {
            _keys!.Frame = input;
            command = _keys.Next(world);
        }

        var step = StageStepper.AppTick(world, command);
        if (step == StageStep.Stepped) Consume(world);
        LogPolicy();
        _hud.Tick();

        switch (step)
        {
            case StageStep.Finished:
                Conclude();
                return;
            case StageStep.RoomExit:
                BeginTransition(world);
                return;
        }

        var phase = world.Room?.Phase ?? RoomPhase.Enter;
        if (phase != _lastPhase)
        {
            _lastPhase = phase;
            if (phase == RoomPhase.Doors) _flow.Signal("doors");
        }

        // 선택 대기 — 이번 스텝이 켰든 앞에서 남았든 같은 앱 틱에 올린다(3.3)
        if (world.AwaitingChoice != ChoiceKind.None) OpenChoice(world);
    }

    private void OpenChoice(World world)
    {
        IScene overlay = world.AwaitingChoice switch
        {
            ChoiceKind.Chest => new ChestOverlay(_flow, world),
            ChoiceKind.Reward => new LevelUpOverlay(_flow, world, reward: true),
            _ => new LevelUpOverlay(_flow, world, reward: false),
        };
        _stack?.Push(overlay);
    }

    /// <summary>
    /// 표시 이벤트 소비(7.9): <c>FxSystem.Consume</c> → <c>Audio.Consume</c> → <c>Clear</c>. 그 전에 HUD 가 볼 신호만 읽는다 —
    /// 보스 등장이면 화면 신호 <c>boss</c>, 허리띠 물약이면 허리띠 칸 번쩍임(8.2).
    /// </summary>
    private void Consume(World world)
    {
        var buffer = world.Presentation;
        var boss = false;
        foreach (var cue in buffer.Cues.AsSpan())
        {
            if (cue.Kind == CueKind.BossArrived) boss = true;
            else if (cue.Kind == CueKind.BeltPotion) _hud.OnBeltPotion(cue.Tick);
        }
        if (boss) _flow.Signal("boss");
        _renderer?.Fx.Consume(buffer, world.Tick);
        _flow.App.Audio?.Consume(buffer, world);
        buffer.Clear();
    }

    /// <summary>
    /// 새 월드에 붙이기(7.11 순서): <c>WorldRenderer</c>(WP5) → <c>FxSystem</c> → <c>Hud</c> → 미니맵 → 소리(WP8).
    /// <c>WorldRenderer</c>·<c>FxSystem</c> 은 WP5 가 <see cref="IWorldBound"/> 를 달면 불린다(지금 빈 연결 파일에는 없음 — 13장 WP6 구현 기록).
    /// </summary>
    private void AttachAll(World world)
    {
        if ((object?)_renderer is IWorldBound r) r.Attach(world);
        if ((object?)_renderer?.Fx is IWorldBound fx) fx.Attach(world);
        _hud.Attach(world);
        _hud.Minimap.Attach(world);
        _flow.App.Audio?.Attach(world);
    }

    private void StartActAudio()
    {
        var act = _session.Run.Act.Act;
        if (ReferenceEquals(act, _musicAct)) return;
        _musicAct = act;
        _flow.App.Audio?.PreloadStage(act);
        _flow.App.Audio?.PlayMusic(act.Id);
    }

    // ── 방 이동 ──────────────────────────────────────

    private void BeginTransition(World world)
    {
        var rules = world.Defs.Rules.Rooms ?? throw new InvalidOperationException("rules.rooms 없음");
        var floor = world.Room?.ChosenExitInfo is { NextFloor: not 0 };
        var seconds = floor ? rules.TransitionFloor : rules.Transition;
        _transitionTicks = Math.Max(2, (int)MathF.Round(seconds * World.TicksPerSecond));
        _transitionAge = 0;
        _advanced = false;
        TickTransition();
    }

    private void TickTransition()
    {
        _transitionAge++;
        if (!_advanced && _transitionAge >= _transitionTicks / 2) Advance();
        if (_transitionAge >= _transitionTicks) _transitionAge = -1;
    }

    private void Advance()
    {
        _advanced = true;
        var old = World;
        Consume(old);                    // 옛 월드의 남은 표시 이벤트(나감 틱 것)는 옛 월드 틱으로 소비하고 비운다(7.11)
        LogInnerRooms(old);
        _session.Advance();
        LogRooms();
        var world = World;
        AttachAll(world);
        StartActAudio();
        _lastPhase = world.Room?.Phase ?? RoomPhase.Enter;
    }

    // ── 판 끝 ────────────────────────────────────────

    private void Conclude()
    {
        if (_finished) return;
        _finished = true;
        var world = World;
        LogInnerRooms(world);
        var run = _session.Conclude();
        LogRooms();
        LogPolicy();
        if (_policy is { } p)
            foreach (var s in p.RoomStats) _flow.Log.Info(RunLog.Visits(s));
        _stack?.Replace(new ResultScene(_flow, ResultSummary.From(_flow, run, world)));
    }

    /// <summary>일시정지 메뉴 <c>포기</c> → 패배 처리(8.1). 다음 앱 틱의 <c>StageStepper</c> 가 <c>Finished</c> → <c>Conclude</c>(같은 길).</summary>
    public void Forfeit()
    {
        var world = World;
        if (world.Outcome is RunOutcome.Victory or RunOutcome.Defeat) return;
        world.Outcome = RunOutcome.Defeat;
        world.OutcomeTick = world.Tick;
    }

    /// <summary>
    /// 일시정지 메뉴 <c>저장하고 끝내기</c>(v0.8, 8.3 — <c>CanSuspend</c> 일 때만): <c>Suspend()</c> → <c>SaveRun</c> → 타이틀.
    /// <c>Suspend</c> 가 <see cref="InvalidOperationException"/> 이면 거짓(메뉴가 <c>저장 못 함</c> — 판은 그대로, 그 틱은 되돌리지 않음, v0.8.2).
    /// </summary>
    public bool SuspendRun()
    {
        if (!_session.CanSuspend) return false;
        var world = World;
        RunSnapshot snapshot;
        try { snapshot = _session.Suspend(); }
        catch (InvalidOperationException e)
        {
            _flow.Log.Warn($"계단 저장 못 함: {e.Message}");
            world.Presentation.Clear();
            return false;
        }
        world.Presentation.Clear();       // Suspend 의 한 틱이 낸 렌더·소리 신호는 버린다(8.3)
        LogInnerRooms(world);
        LogRooms();
        _flow.App.Save.SaveRun(snapshot);
        _finished = true;
        _stack?.Reset(new TitleScene(_flow));
        return true;
    }

    // ── 로그 ─────────────────────────────────────────

    private void LogRooms()
    {
        var rooms = _session.Run.Rooms;
        for (; _loggedRooms < rooms.Count; _loggedRooms++) _flow.Log.Info(RunLog.Room(rooms[_loggedRooms]));
    }

    private void LogInnerRooms(World world)
    {
        foreach (var line in RunLog.InnerRooms(world)) _flow.Log.Info(line);
    }

    private void LogPolicy()
    {
        if (_policy is not { } p) return;
        var skips = p.DoorSkips;
        for (; _loggedDoorSkips < skips.Count; _loggedDoorSkips++) _flow.Log.Info(RunLog.DoorSkip(skips[_loggedDoorSkips]));
        var stalls = p.RewardStalls;
        for (; _loggedStalls < stalls.Count; _loggedStalls++) _flow.Log.Info(RunLog.RewardStall(stalls[_loggedStalls]));
        var visits = p.Events;
        for (; _loggedVisits < visits.Count; _loggedVisits++) _flow.Log.Info(RunLog.Visit(visits[_loggedVisits]));
    }

    // ── 그리기 ───────────────────────────────────────

    public void Draw(RenderContext rc, float alpha)
    {
        var world = World;
        // 보간: 캡처는 1, 오버레이에 멈춰 있으면 마지막 값 고정(3.2), 아니면 슬로모션 늘임 값 또는 누적기 값
        var a = rc.Capturing ? 1f : _stack?.Top == this ? StageStepper.Alpha(world, alpha) : _lastAlpha;
        _lastAlpha = a;
        _renderer?.Draw(world, rc, a);
        if (_flow.Canvas is not { } c) return;
        c.Begin(rc);
        _hud.Draw(c, rc, _session.Run);
        if (_transitionAge >= 0 && _transitionTicks > 0)
        {
            // 어두워짐: 가운데(Advance)에서 가장 어둡다
            var half = _transitionTicks / 2f;
            var dark = 1f - MathF.Abs(_transitionAge - half) / half;
            c.Fill(Ui.Screen, Theme.Black, Math.Clamp(dark, 0f, 1f));
        }
        c.End();
    }
}
