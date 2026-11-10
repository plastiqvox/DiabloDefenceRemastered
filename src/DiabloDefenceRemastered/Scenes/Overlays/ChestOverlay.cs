using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes.Overlays;

/// <summary>
/// 상자 열기 (설계서 8.1 — 오버레이, 멈춤). 들어올 때 <c>IProgression.OpenChest()</c> 로 결과를 받고, 1.2s 연출 뒤 결과 칸을 보인 채
/// Enter/Space·클릭(자동 조작이면 메뉴 대기 뒤)으로 닫는다. 연출이 끝난 앱 틱에 화면 신호 <c>chest</c>.
/// </summary>
public sealed class ChestOverlay(GameFlow flow, World world) : IScene
{
    /// <summary>상자 열기 연출 1.2s(8.1).</summary>
    public const int OpenTicks = 72;

    private SceneStack? _stack;
    private ChestReward _reward = new([]);
    private long _openTick;
    private bool _signaled;
    private bool _done;

    public bool PausesBelow => true;
    public bool DrawsBelow => true;
    public ChestReward Reward => _reward;

    private long Age => flow.Now.Value - _openTick;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        var progression = world.Progression ?? throw new InvalidOperationException("성장 모듈(IProgression)이 없는 월드에서 상자 창");
        _reward = progression.OpenChest();
        _openTick = flow.Now.Value;
        flow.LogScene("Chest");
        flow.App.Audio?.PlayUi("chest_open");
    }

    public void Exit() { }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        if (_done || Age < OpenTicks) return;
        if (!_signaled)
        {
            _signaled = true;
            flow.Signal("chest");
        }
        if (input.IsPressed(UiButtons.Confirm) || input.IsPressed(UiButtons.Click) || input.IsPressed(UiButtons.Cancel)) Close();
    }

    public void AutoStep(IAutoplayDriver bot)
    {
        if (!_done && Age >= OpenTicks && bot.Ready(this)) Close();
    }

    private void Close()
    {
        _done = true;
        flow.App.Audio?.PlayUi("menu_select");
        _stack?.Pop();
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        Ui.Dim(c, 0.45f);
        Ui.Centered(c, "상자", Theme.Width / 2, 120, Theme.TitleText, Theme.Gold);
        var t = Math.Clamp(Age / (float)OpenTicks, 0f, 1f);
        var items = _reward.Items;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var appear = Math.Clamp(t * items.Count - i, 0f, 1f);
            var y = 190 + i * 64;
            var r = new RectF(Theme.Width / 2 - 180, y, Theme.Width / 2 + 180, y + 56);
            Ui.Panel(c, r, appear);
            Ui.Icon(c, world.Defs, item.Icon, new RectF(r.X0 + 6, r.Y0 + 4, r.X0 + 54, r.Y0 + 52), appear);
            var line = item.Kind switch
            {
                ChestItemKind.Evolution => $"{item.Name} — 진화",
                ChestItemKind.Temper => item.Name,
                _ => item.Level > 0 ? $"{item.Name} Lv {item.Level}" : item.Name,
            };
            c.Text(line, new Vector2(r.X0 + 64, r.Y0 + 18), Theme.BodyText, item.Kind == ChestItemKind.Evolution ? Theme.Gold : Theme.Text, appear);
        }
        if (t >= 1f) Ui.Centered(c, "Enter — 계속", Theme.Width / 2, Theme.Height - 60, Theme.BodyText, Theme.TextDim);
        c.End();
    }
}
