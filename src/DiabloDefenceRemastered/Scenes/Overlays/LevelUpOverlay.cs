using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes.Overlays;

/// <summary>
/// 레벨업 선택 창 (설계서 8.1, 기획 12.2) — v0.7: 마법서·매혹의 성소 보상 창도 이 창이다(<paramref name="reward"/>, <c>IProgression.RewardOffer</c>).
/// 시뮬레이션이 멈춘 사이(<see cref="PausesBelow"/>)에 <c>IProgression</c> 을 직접 부른다(결정성 영향 없음, 8.1).
/// 입력: 1/2/3, ←→/↑↓ + Enter/Space, 카드 클릭, R 재추첨, X 건너뛰기. 열린 뒤 <c>rules.level_up.input_lock</c>(0.4s) 동안 고르기 입력을 버린다(기획 3절).
/// 카드는 0.2s 동안 아래에서 올라오고, 다 올라온 앱 틱에 화면 신호 <c>levelup</c>·<c>reward</c>(<c>--capture-on</c>).
/// </summary>
public sealed class LevelUpOverlay(GameFlow flow, World world, bool reward) : IScene
{
    /// <summary>카드 올라옴 0.2s(기획 12.2).</summary>
    public const int RiseTicks = 12;
    public const float CardWidth = 220f;
    public const float CardHeight = 150f;
    public const float CardGap = 20f;

    private SceneStack? _stack;
    private IProgression _progression = null!;
    private LevelUpOffer _offer = null!;
    private long _openTick;
    private int _focus;
    private bool _signaled;
    private bool _done;

    public bool PausesBelow => true;
    public bool DrawsBelow => true;
    public bool IsReward => reward;
    public LevelUpOffer Offer => _offer;
    public int Focus => _focus;

    private int LockTicks => (int)MathF.Round((world.Defs.Rules.LevelUp?.InputLock ?? 0.4f) * World.TicksPerSecond);

    private long Age => flow.Now.Value - _openTick;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        _progression = world.Progression ?? throw new InvalidOperationException("성장 모듈(IProgression)이 없는 월드에서 선택 창");
        _offer = reward ? _progression.RewardOffer() : _progression.Offer();
        _openTick = flow.Now.Value;
        flow.LogScene(reward ? "Reward" : "LevelUp");
        if (!reward) flow.App.Audio?.PlayUi("level_up");
    }

    public void Exit() { }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        if (_done) return;
        if (!_signaled && Age >= RiseTicks)
        {
            _signaled = true;
            flow.Signal(reward ? "reward" : "levelup");
        }
        if (Age < LockTicks) return;

        var n = _offer.Cards.Count;
        if (n > 0)
        {
            var before = _focus;
            if (input.IsPressed(UiButtons.Left) || input.IsPressed(UiButtons.Up)) _focus = (_focus + n - 1) % n;
            if (input.IsPressed(UiButtons.Right) || input.IsPressed(UiButtons.Down)) _focus = (_focus + 1) % n;
            var hovered = flow.MouseVirtual(input) is { } m ? CardAt(m, n) : -1;
            if (hovered >= 0) _focus = hovered;
            if (_focus != before) flow.App.Audio?.PlayUi("menu_move");

            if (input.IsPressed(UiButtons.Choice1) && n >= 1) { Choose(0); return; }
            if (input.IsPressed(UiButtons.Choice2) && n >= 2) { Choose(1); return; }
            if (input.IsPressed(UiButtons.Choice3) && n >= 3) { Choose(2); return; }
            if (input.IsPressed(UiButtons.Confirm)) { Choose(_focus); return; }
            if (input.IsPressed(UiButtons.Click) && hovered >= 0) { Choose(hovered); return; }
        }
        if (input.IsPressed(UiButtons.Reroll) && _offer.RerollsLeft > 0) { Reroll(); return; }
        if (input.IsPressed(UiButtons.Skip)) Skip();
    }

    public void AutoStep(IAutoplayDriver bot)
    {
        if (_done || Age < LockTicks || !bot.Ready(this)) return;
        var pick = bot.PickCard(_offer);
        if (pick >= 0 && pick < _offer.Cards.Count) Choose(pick);
        else Skip();
    }

    /// <summary>카드 하나 고르기(창을 닫는다 — 대기가 더 남았으면 <c>PlayScene</c> 이 다음 앱 틱에 다시 연다).</summary>
    public void Choose(int index)
    {
        if (_done) return;
        if (reward) _progression.ChooseReward(index);
        else _progression.Choose(index);
        Close();
    }

    public void Reroll()
    {
        if (reward) _progression.RerollReward();
        else _progression.Reroll();
        _offer = reward ? _progression.RewardOffer() : _progression.Offer();
        _focus = Math.Clamp(_focus, 0, Math.Max(0, _offer.Cards.Count - 1));
        flow.App.Audio?.PlayUi("menu_select");
    }

    public void Skip()
    {
        if (_done) return;
        if (reward) _progression.SkipReward();
        else _progression.Skip();
        Close();
    }

    private void Close()
    {
        _done = true;
        flow.App.Audio?.PlayUi("menu_select");
        _stack?.Pop();
    }

    /// <summary>
    /// 건너뛰기 회복량: 레벨업 = <c>rules.level_up.skip_heal</c>, 보상 = 창이 받은 보상 정의(<c>LevelUpOffer.Reward</c> — v0.9, 단계 2 wp6)의 <c>skip.heal</c>,
    /// 그것이 없으면 그 방 보상의 것(6.18).
    /// </summary>
    private float SkipHeal => reward ? (_offer.Reward ?? world.Room?.Reward)?.Skip?.Heal ?? 0f : world.Defs.Rules.LevelUp?.SkipHeal ?? 0f;

    private static RectF CardRect(int i, int n)
    {
        var total = n * CardWidth + (n - 1) * CardGap;
        var x = Theme.Width / 2 - total / 2 + i * (CardWidth + CardGap);
        var y = Theme.Height / 2 - CardHeight / 2;
        return new RectF(x, y, x + CardWidth, y + CardHeight);
    }

    private static int CardAt(Vector2 m, int n)
    {
        for (var i = 0; i < n; i++)
        {
            var r = CardRect(i, n);
            if (m.X >= r.X0 && m.X < r.X1 && m.Y >= r.Y0 && m.Y < r.Y1) return i;
        }
        return -1;
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        Ui.Dim(c);
        Ui.Centered(c, RunText.OfferTitle(_offer.Source), Theme.Width / 2, 110, Theme.TitleText, Theme.Gold);
        var rise = Math.Clamp(Age / (float)RiseTicks, 0f, 1f);
        var n = _offer.Cards.Count;
        for (var i = 0; i < n; i++)
        {
            var card = _offer.Cards[i];
            var r = CardRect(i, n);
            var lift = (1f - rise) * 60f + (i == _focus ? -4f : 0f);
            r = new RectF(r.X0, r.Y0 + lift, r.X1, r.Y1 + lift);
            Ui.Panel(c, r);
            var weapon = card.Kind is CardKind.NewWeapon or CardKind.WeaponUpgrade || _offer.Source != OfferSource.LevelUp;
            c.Fill(new RectF(r.X0, r.Y0, r.X1, r.Y0 + 6), weapon ? Theme.WeaponBand : Theme.PassiveBand, rise);
            if (i == _focus) Ui.Outline(c, r, Theme.Focus, rise);
            Ui.Icon(c, world.Defs, card.Icon, new RectF(r.X0 + 10, r.Y0 + 14, r.X0 + 58, r.Y0 + 62), rise);
            c.Text($"[{i + 1}] {card.Name}", new Vector2(r.X0 + 66, r.Y0 + 16), Theme.BodyText, Theme.Text, rise);
            var level = RunText.CardLevel(card);
            if (_offer.Source == OfferSource.Shrine && (_offer.Reward ?? world.Room?.Reward)?.Shrine is { } shrine) level = RunText.ShrineLine(shrine);
            c.Text(level, new Vector2(r.X0 + 66, r.Y0 + 38), Theme.SmallText, Theme.TextDim, rise);
            var y = r.Y0 + 70;
            foreach (var line in card.Lines)
            {
                c.Text(line, new Vector2(r.X0 + 12, y), Theme.SmallText, Theme.Text, rise);
                y += 16;
            }
        }
        var bottom = Theme.Height / 2 + CardHeight / 2 + 30;
        Ui.Centered(c, $"[R] 재추첨 ({_offer.RerollsLeft})", Theme.Width / 2 - 140, bottom, Theme.BodyText, _offer.RerollsLeft > 0 ? Theme.Text : Theme.Grey);
        Ui.Centered(c, RunText.SkipLine(SkipHeal), Theme.Width / 2 + 140, bottom, Theme.BodyText, Theme.Text);
        c.End();
    }
}
