using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Scenes.Overlays;
using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 타이틀 (설계서 8.1, 기획 12.5). 메뉴: (계단 저장이 있으면 맨 위) <c>이어 하기 (지하묘지)</c> / <c>새 게임</c> / <c>설정</c> / <c>종료</c>.
/// 메뉴 아래: 고른 직업의 최고 기록 한 줄(<c>최고: 지하묘지 6 / 11</c>), 꺼진 내용이 있으면 그 한 줄(6.17), 이어 하기를 못 읽었으면 그 한 줄.
/// <list type="bullet">
/// <item>새 게임 — 저장이 있으면 "이어 할 판이 지워진다" 를 한 번 묻고 <c>DiscardRun()</c>(8.3). 직업 단계면 HeroSelect → Play, 아니면 바로 Play.</item>
/// <item>이어 하기 — <c>TakeRun()</c>(읽는 순간 파일이 지워짐) → <c>RunSession</c> 이어 하기 생성자. 없거나 <see cref="RunSnapshotMismatchException"/> 이면 로그 한 줄 + <c>이어 할 판을 읽지 못했다</c>.</item>
/// </list>
/// 자동 조작은 메뉴 대기 뒤 새 게임(9절 — 이어 하기는 고르지 않음, 개발 옵션 실행은 저장을 읽지 않는다).
/// </summary>
public sealed class TitleScene(GameFlow flow) : IScene
{
    private readonly MenuList _menu = new(Theme.Width / 2, 300);
    private SceneStack? _stack;
    private RunSnapshotInfo? _saved;
    private bool _confirming;
    private string? _notice;
    private long _enterTick;

    private enum Item { Continue, NewGame, Settings, Quit, ConfirmNew, ConfirmBack }
    private readonly List<Item> _items = [];

    public bool PausesBelow => false;
    public bool DrawsBelow => false;
    public MenuList Menu => _menu;
    /// <summary>계단 저장 한 줄(없으면 null).</summary>
    public RunSnapshotInfo? Saved => _saved;
    public bool Confirming => _confirming;
    /// <summary>메뉴 아래 안내 한 줄(이어 하기 실패).</summary>
    public string? Notice => _notice;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        _enterTick = flow.Now.Value;
        _saved = flow.App.Save.PeekRun();
        BuildMenu();
        flow.LogScene("Title");
        flow.App.Audio?.PlayMusic("title");
    }

    public void Exit() { }

    private void BuildMenu()
    {
        _items.Clear();
        var labels = new List<MenuItem>();
        void Add(Item item, string label)
        {
            _items.Add(item);
            labels.Add(new MenuItem(label));
        }
        if (_confirming)
        {
            Add(Item.ConfirmNew, "새 게임 시작");
            Add(Item.ConfirmBack, "돌아가기");
        }
        else
        {
            if (_saved is { } s) Add(Item.Continue, $"이어 하기 ({ActName(s.NextActId)})");
            Add(Item.NewGame, "새 게임");
            Add(Item.Settings, "설정");
            Add(Item.Quit, "종료");
        }
        _menu.SetItems(labels);
        _menu.SetFocus(0);
    }

    private string ActName(string actId) => flow.Defs.Stages.TryGet(actId, out var a) ? RunText.ActName(a) : actId;

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        if (_confirming && input.IsPressed(UiButtons.Cancel))
        {
            _confirming = false;
            BuildMenu();
            return;
        }
        var pick = _menu.Update(input, flow.MouseVirtual(input), out var moved);
        if (moved) flow.App.Audio?.PlayUi("menu_move");
        if (pick >= 0) Choose(_items[pick]);
    }

    public void AutoStep(IAutoplayDriver bot)
    {
        if (bot.Ready(this)) Choose(_confirming ? Item.ConfirmNew : Item.NewGame);
    }

    private void Choose(Item item)
    {
        flow.App.Audio?.PlayUi("menu_select");
        switch (item)
        {
            case Item.Continue:
                Continue();
                break;
            case Item.NewGame when _saved is not null:
                _confirming = true;
                BuildMenu();
                break;
            case Item.NewGame:
                NewGame();
                break;
            case Item.ConfirmNew:
                flow.App.Save.DiscardRun();
                _saved = null;
                _confirming = false;
                NewGame();
                break;
            case Item.ConfirmBack:
                _confirming = false;
                BuildMenu();
                break;
            case Item.Settings:
                _stack?.Push(new SettingsOverlay(flow));
                break;
            case Item.Quit:
                flow.Quit();
                break;
        }
    }

    /// <summary>새 게임: 직업 단계면 HeroSelect, 아니면 고른 직업으로 바로 판.</summary>
    public void NewGame()
    {
        if (flow.NeedsHeroSelect()) _stack?.Replace(new HeroSelectScene(flow));
        else _stack?.Replace(StartRun(flow, flow.ResolveHero()));
    }

    /// <summary>판 하나 열기(새 게임·직업 고르기·다시 하기가 같이 씀): 씨앗 = <see cref="GameFlow.NextSeed"/>, 다음 판은 새 씨앗.</summary>
    public static PlayScene StartRun(GameFlow flow, HeroClassDef? hero)
    {
        var seed = flow.NextSeed;
        flow.NextSeed = GameFlow.Reseed(seed);
        var session = new RunSession(flow.Defs, flow.App.Modules, flow.CreateRunStart(hero, seed));
        return new PlayScene(flow, session);
    }

    private void Continue()
    {
        _saved = null;
        var snapshot = flow.App.Save.TakeRun();
        if (snapshot is null)
        {
            Fail("계단 저장을 읽지 못했다(없거나 깨진 파일)");
            return;
        }
        RunSession session;
        try { session = new RunSession(flow.Defs, flow.App.Modules, snapshot, flow.App.Options.Run); }
        catch (RunSnapshotMismatchException e)
        {
            Fail(e.Message);
            return;
        }
        _stack?.Replace(new PlayScene(flow, session, continued: true));
    }

    private void Fail(string why)
    {
        flow.Log.Warn($"이어 하기 못 함: {why}");
        _notice = "이어 할 판을 읽지 못했다";
        BuildMenu();
    }

    /// <summary>
    /// 꺼진 내용 한 줄(기획 12.5, 설계서 1.4 끝 — <c>ContentAvailability.Disabled(defs).Any</c> 일 때만). 앞말은 건너뛴 이유로:
    /// <c>없음</c>·<c>--skip-bundles</c> → <c>헬파이어 자원 없음</c>, 그 밖(열 수 없음·판 다름) → <c>헬파이어 자원을 읽지 못함 — &lt;MPQ&gt;</c>.
    /// </summary>
    public static string? DisabledLine(ContentAvailability content, GameDefs defs)
    {
        var d = content.Disabled(defs);
        if (!d.Any) return null;
        var names = new List<string>();
        foreach (var h in d.Heroes) names.Add(h.Name.Length > 0 ? h.Name : h.Id);
        foreach (var s in d.Stages) names.Add(RunText.ActName(s));
        var unreadable = new List<string>();
        foreach (var b in d.Bundles)
            if (content.Skipped(b) is { } skip && skip.Reason is not (ManifestSkippedBundle.ReasonMissing or ManifestSkippedBundle.ReasonSkipped))
                unreadable.Add(skip.Mpq);
        var head = unreadable.Count > 0 ? $"헬파이어 자원을 읽지 못함({string.Join(", ", unreadable)})" : "헬파이어 자원 없음";
        return $"{head} — {string.Join("·", names)} 꺼짐";
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        c.Fill(Ui.Screen, Theme.Black, 1f);
        // 배경 TITLE(640×480)을 비율 유지로 채움, 위에 LOGO 15프레임 80ms 반복(앱 틱)
        if (flow.Defs.Sprites.TryGet(Theme.TitleSprite, out var title) && title.Sheet.Y > 0)
        {
            var scale = MathF.Min(Theme.Width / title.Sheet.X, Theme.Height / title.Sheet.Y);
            var w = title.Sheet.X * scale;
            var h = title.Sheet.Y * scale;
            c.Sprite(title.Id, new RectI(0, 0, title.Sheet.X, title.Sheet.Y), new RectF((Theme.Width - w) / 2, (Theme.Height - h) / 2, (Theme.Width + w) / 2, (Theme.Height + h) / 2), Rgb.White);
        }
        if (flow.Defs.Sprites.TryGet(Theme.LogoSprite, out var logo) && logo.Frame.Y > 0)
        {
            var frame = (int)((flow.Now.Value - _enterTick) * 1000 / World.TicksPerSecond / 80 % Math.Max(1, logo.Frames));
            var w = logo.Frame.X * 0.9f;
            var h = logo.Frame.Y * 0.9f;
            c.Sprite(logo.Id, new RectI(0, frame * logo.Frame.Y, logo.Frame.X, (frame + 1) * logo.Frame.Y), new RectF(Theme.Width / 2 - w / 2, 40, Theme.Width / 2 + w / 2, 40 + h), Rgb.White);
        }

        if (_confirming) Ui.Centered(c, "이어 할 판이 지워진다. 새 게임을 시작할까?", Theme.Width / 2, 262, Theme.BodyText, Theme.Text);
        _menu.Draw(c);

        var y = 300 + _menu.Items.Count * _menu.Spacing + 10;
        if (RunText.BestLine(flow.Defs, flow.ResolveHero() is { } hero && RunPlanner.Root(flow.Defs) is { } root ? flow.App.Save.BestRecord(root.Id, hero.Id) : null) is { } best)
        {
            Ui.Centered(c, best, Theme.Width / 2, y, Theme.BodyText, Theme.Gold);
            y += 22;
        }
        if (DisabledLine(flow.Content, flow.Defs) is { } off)
        {
            Ui.Centered(c, off, Theme.Width / 2, y, Theme.SmallText, Theme.TextDim);
            y += 18;
        }
        if (_notice is { } n) Ui.Centered(c, n, Theme.Width / 2, y, Theme.SmallText, Theme.Red);
        c.End();
    }
}
