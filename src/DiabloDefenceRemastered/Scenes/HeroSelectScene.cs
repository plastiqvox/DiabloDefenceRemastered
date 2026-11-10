using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 직업 고르기 (설계서 8.1 — 직업 단계 WP9, 기획 4.5·12.5). 카드(정의 순서): 아이콘(없으면 칸만)·이름·설명, 꺼진 직업은 어둡게 + 빠진 MPQ 이름.
/// ←→ 와 Enter, 1~4, 클릭으로 고른다. 고른 직업으로 <c>SetLastHero</c> 를 쓰고(<c>PlayScene</c> 이 판 시작 때) 바로 판을 연다. Esc 는 타이틀로.
/// 처음 포커스는 <see cref="GameFlow.ResolveHero"/>(저장의 마지막 직업).
/// </summary>
public sealed class HeroSelectScene(GameFlow flow) : IScene
{
    public const float CardWidth = 200f;
    public const float CardHeight = 260f;

    private SceneStack? _stack;
    private int _focus;

    public bool PausesBelow => false;
    public bool DrawsBelow => false;
    public IReadOnlyList<HeroClassDef> Heroes => flow.Defs.HeroClasses.All;
    public int Focus => _focus;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        var resolved = flow.ResolveHero();
        for (var i = 0; i < Heroes.Count; i++)
            if (ReferenceEquals(Heroes[i], resolved)) _focus = i;
        flow.LogScene("HeroSelect");
    }

    public void Exit() { }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        var n = Heroes.Count;
        if (n == 0) return;
        if (input.IsPressed(UiButtons.Cancel))
        {
            _stack?.Replace(new TitleScene(flow));
            return;
        }
        var before = _focus;
        if (input.IsPressed(UiButtons.Left) || input.IsPressed(UiButtons.Up)) _focus = (_focus + n - 1) % n;
        if (input.IsPressed(UiButtons.Right) || input.IsPressed(UiButtons.Down)) _focus = (_focus + 1) % n;
        var hovered = -1;
        if (flow.MouseVirtual(input) is { } m)
            for (var i = 0; i < n; i++)
            {
                var r = CardRect(i, n);
                if (m.X >= r.X0 && m.X < r.X1 && m.Y >= r.Y0 && m.Y < r.Y1) { hovered = i; _focus = i; }
            }
        if (_focus != before) flow.App.Audio?.PlayUi("menu_move");
        if (input.IsPressed(UiButtons.Choice1) && n >= 1) Choose(0);
        else if (input.IsPressed(UiButtons.Choice2) && n >= 2) Choose(1);
        else if (input.IsPressed(UiButtons.Choice3) && n >= 3) Choose(2);
        else if (input.IsPressed(UiButtons.Confirm)) Choose(_focus);
        else if (input.IsPressed(UiButtons.Click) && hovered >= 0) Choose(hovered);
    }

    public void AutoStep(IAutoplayDriver bot)
    {
        if (bot.Ready(this)) Choose(_focus);
    }

    /// <summary>그 직업으로 판 시작(꺼진 직업은 무시 — 소리만).</summary>
    public void Choose(int index)
    {
        var hero = Heroes[index];
        if (flow.IsDisabled(hero))
        {
            flow.App.Audio?.PlayUi("menu_move");
            return;
        }
        flow.App.Audio?.PlayUi("menu_select");
        flow.App.Save.SetLastHero(hero.Id);
        _stack?.Replace(TitleScene.StartRun(flow, hero));
    }

    private static RectF CardRect(int i, int n)
    {
        var total = n * CardWidth + (n - 1) * 16f;
        var x = Theme.Width / 2 - total / 2 + i * (CardWidth + 16f);
        return new RectF(x, 140, x + CardWidth, 140 + CardHeight);
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        c.Fill(Ui.Screen, Theme.Black, 1f);
        Ui.Centered(c, "직업 고르기", Theme.Width / 2, 70, Theme.TitleText, Theme.Gold);
        var n = Heroes.Count;
        for (var i = 0; i < n; i++)
        {
            var h = Heroes[i];
            var r = CardRect(i, n);
            var off = flow.IsDisabled(h);
            var a = off ? 0.45f : 1f;
            Ui.Panel(c, r);
            if (i == _focus) Ui.Outline(c, r, Theme.Focus);
            Ui.Icon(c, flow.Defs, h.Icon, new RectF(r.X0 + 52, r.Y0 + 14, r.X1 - 52, r.Y0 + 110), a);
            Ui.Centered(c, h.Name.Length > 0 ? h.Name : h.Id, r.Center.X, r.Y0 + 120, Theme.MenuText, Theme.Text, a);
            c.Text(h.Description, new Vector2(r.X0 + 10, r.Y0 + 156), Theme.SmallText, Theme.TextDim, a);
            if (off)
            {
                var missing = flow.Content.MissingFor(h);
                var names = string.Join(", ", missing.Select(ContentBundles.MpqName));
                Ui.Centered(c, $"자원 필요({names})", r.Center.X, r.Y1 - 28, Theme.SmallText, Theme.Red);
            }
        }
        c.End();
    }
}
