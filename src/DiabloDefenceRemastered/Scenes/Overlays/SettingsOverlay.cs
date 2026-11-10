using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes.Overlays;

/// <summary>
/// 설정 (설계서 8.1·8.3, 기획 12.3): 전체 화면, 화면 흔들림, 미니맵, 피해 숫자, 음량 셋(전체·효과음·음악, 0~100% 10% 단위).
/// ←→ 로 값 바꾸기(켬/끔은 Enter 도), 바뀔 때마다 저장(<c>SaveSettings</c>)·음량 적용(<c>Audio.SetVolumes</c>)·<see cref="GameFlow.SettingsChanged"/>(전체 화면은 창 쪽 WP7).
/// 음량이 저장에 없으면 <c>rules.audio.defaults</c> 를 보이고, 처음 바꿀 때 그 값에서 시작한다(8.3 — 기본값은 rules 한 곳).
/// </summary>
public sealed class SettingsOverlay(GameFlow flow) : IScene
{
    private readonly MenuList _menu = new(Theme.Width / 2, 150, 34f, 420f);
    private SceneStack? _stack;
    private GameSettings _settings = new();

    private enum Row { Fullscreen, Shake, Minimap, Damage, Master, Sfx, Music, Back }
    private static readonly Row[] Rows = [Row.Fullscreen, Row.Shake, Row.Minimap, Row.Damage, Row.Master, Row.Sfx, Row.Music, Row.Back];

    public bool PausesBelow => true;
    public bool DrawsBelow => true;
    public GameSettings Settings => _settings;
    public MenuList Menu => _menu;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        _settings = flow.App.Save.Settings;
        Refresh();
        flow.LogScene("Settings");
    }

    public void Exit() { }

    private void Refresh()
    {
        var (m, s, mu) = flow.Volumes(_settings);
        static string OnOff(bool v) => v ? "켬" : "끔";
        _menu.SetItems(Rows.Select(r => new MenuItem(r switch
        {
            Row.Fullscreen => $"전체 화면  {OnOff(_settings.Fullscreen)}",
            Row.Shake => $"화면 흔들림  {OnOff(_settings.ScreenShake)}",
            Row.Minimap => $"미니맵  {OnOff(_settings.ShowMinimap)}",
            Row.Damage => $"피해 숫자  {OnOff(_settings.ShowDamageNumbers)}",
            Row.Master => $"전체 음량  {RunText.Percent(m)}",
            Row.Sfx => $"효과음  {RunText.Percent(s)}",
            Row.Music => $"음악  {RunText.Percent(mu)}",
            _ => "돌아가기",
        })));
    }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        if (input.IsPressed(UiButtons.Cancel) || input.IsPressed(UiButtons.Pause))
        {
            _stack?.Pop();
            return;
        }
        var pick = _menu.Update(input, flow.MouseVirtual(input), out var moved);
        if (moved) flow.App.Audio?.PlayUi("menu_move");
        var row = Rows[_menu.Focus];
        if (input.IsPressed(UiButtons.Left)) Change(row, -1);
        else if (input.IsPressed(UiButtons.Right)) Change(row, +1);
        else if (pick >= 0)
        {
            if (Rows[pick] == Row.Back) _stack?.Pop();
            else Change(Rows[pick], +1);
        }
    }

    /// <summary>한 줄 값 바꾸기(켬/끔은 뒤집기, 음량은 ±10% — 0~100% 안에서 멈춤).</summary>
    public void Change(int rowIndex, int step) => Change(Rows[rowIndex], step);

    private void Change(Row row, int step)
    {
        var (m, s, mu) = flow.Volumes(_settings);
        static float Step(float v, int d) => Math.Clamp(MathF.Round(v * 10f + d) / 10f, 0f, 1f);
        var next = row switch
        {
            Row.Fullscreen => _settings with { Fullscreen = !_settings.Fullscreen },
            Row.Shake => _settings with { ScreenShake = !_settings.ScreenShake },
            Row.Minimap => _settings with { ShowMinimap = !_settings.ShowMinimap },
            Row.Damage => _settings with { ShowDamageNumbers = !_settings.ShowDamageNumbers },
            Row.Master => _settings with { MasterVolume = Step(m, step) },
            Row.Sfx => _settings with { SfxVolume = Step(s, step) },
            Row.Music => _settings with { MusicVolume = Step(mu, step) },
            _ => _settings,
        };
        if (next == _settings) return;
        _settings = next;
        flow.ApplySettings(_settings);
        flow.App.Audio?.PlayUi("menu_select");
        Refresh();
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        var hintY = _menu.Top + Rows.Length * _menu.Spacing + 8f;
        Ui.Backdrop(c, new RectF(Theme.Width / 2 - 240, 76, Theme.Width / 2 + 240, hintY + 28), 0.75f);
        Ui.Centered(c, "설정", Theme.Width / 2, 90, Theme.TitleText, Theme.Gold);
        _menu.Draw(c);
        Ui.Centered(c, "←→ 바꾸기 · Esc 돌아가기", Theme.Width / 2, hintY, Theme.SmallText, Theme.TextDim);
        c.End();
    }
}
