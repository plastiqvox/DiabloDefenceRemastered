using System.Globalization;
using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes.Overlays;

/// <summary>
/// 일시정지 (설계서 8.1, 기획 12.3). Esc/P 로 열고 닫는다. 메뉴: <c>이어하기</c> / <c>설정</c> / (계단 저장을 할 수 있을 때만) <c>저장하고 끝내기</c> / <c>포기</c>.
/// 오른쪽에 지금 빌드(무기·패시브·축복과 레벨, 능력치 요약). 들어오면 <c>Audio.SetPaused(true)</c>, 나가면 false(7.9).
/// <c>저장하고 끝내기</c> 가 <c>Suspend</c> 예외로 실패하면 <c>저장 못 함</c> 한 줄을 보이고 판을 이어 간다(v0.8.2).
/// </summary>
public sealed class PauseOverlay(GameFlow flow, PlayScene play) : IScene
{
    private readonly MenuList _menu = new(300, 200);
    private readonly List<Item> _items = [];
    private SceneStack? _stack;
    private string? _notice;

    private enum Item { Resume, Settings, SaveQuit, Forfeit }

    public bool PausesBelow => true;
    public bool DrawsBelow => true;
    public MenuList Menu => _menu;
    /// <summary>메뉴에 <c>저장하고 끝내기</c> 가 있나.</summary>
    public bool CanSaveQuit => _items.Contains(Item.SaveQuit);
    public string? Notice => _notice;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        BuildMenu();
        flow.LogScene("Pause");
        flow.App.Audio?.SetPaused(true);
    }

    public void Exit() => flow.App.Audio?.SetPaused(false);

    private void BuildMenu()
    {
        _items.Clear();
        _items.Add(Item.Resume);
        _items.Add(Item.Settings);
        if (play.Session.CanSuspend) _items.Add(Item.SaveQuit);
        _items.Add(Item.Forfeit);
        _menu.SetItems(_items.Select(i => new MenuItem(i switch
        {
            Item.Resume => "이어하기",
            Item.Settings => "설정",
            Item.SaveQuit => "저장하고 끝내기",
            _ => "포기",
        })));
    }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        if (input.IsPressed(UiButtons.Pause) || input.IsPressed(UiButtons.Cancel))
        {
            _stack?.Pop();
            return;
        }
        var pick = _menu.Update(input, flow.MouseVirtual(input), out var moved);
        if (moved) flow.App.Audio?.PlayUi("menu_move");
        if (pick >= 0) Choose(pick);
    }

    /// <summary>메뉴 줄 고르기(시험·자동 확인용으로 공개).</summary>
    public void Choose(int index)
    {
        flow.App.Audio?.PlayUi("menu_select");
        switch (_items[index])
        {
            case Item.Resume:
                _stack?.Pop();
                break;
            case Item.Settings:
                _stack?.Push(new SettingsOverlay(flow));
                break;
            case Item.SaveQuit:
                if (!play.SuspendRun())
                {
                    _notice = "저장 못 함";
                    BuildMenu();
                }
                break;
            case Item.Forfeit:
                play.Forfeit();
                _stack?.Pop();
                break;
        }
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        c.Begin(rc);
        // 바탕 판: 왼쪽 메뉴(알림 줄 포함)와 오른쪽 빌드 목록을 한 판에 — 아래는 둘 중 긴 쪽
        var world = play.World;
        var hero = world.Hero;
        var named = 0;
        for (var i = 0; i < WeaponSlots.SlotCount; i++) if (hero.Weapons[i].Def is not null) named++;
        for (var i = 0; i < PassiveSlots.SlotCount; i++) if (hero.Passives[i].Def is not null) named++;
        for (var i = 0; i < BlessingSlots.SlotCount; i++) if (hero.Blessings[i].Def is not null) named++;
        var buildBottom = 120f + 32f + named * 20f + 10f + StatLines(hero.MaxHp, hero.Stats).Count() * 16f;
        var menuBottom = 200f + (_items.Count + 1) * _menu.Spacing + 10f;
        Ui.Backdrop(c, new RectF(130, 110, 800, MathF.Max(buildBottom, menuBottom) + 16f));
        Ui.Centered(c, "일시정지", 300, 130, Theme.TitleText, Theme.Gold);
        _menu.Draw(c);
        if (_notice is { } n) Ui.Centered(c, n, 300, 200 + _items.Count * _menu.Spacing + 10, Theme.BodyText, Theme.Red);

        // 오른쪽 빌드 목록
        var x = 560f;
        var y = 120f;
        c.Text("빌드", new Vector2(x, y), Theme.MenuText, Theme.Gold);
        y += 32;
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
            if (hero.Weapons[i].Def is { } w)
            {
                c.Text($"{w.Name} Lv {hero.Weapons[i].Level}{(hero.Weapons[i].Evolved ? " (진화)" : "")}", new Vector2(x, y), Theme.BodyText, Theme.WeaponBand);
                y += 20;
            }
        for (var i = 0; i < PassiveSlots.SlotCount; i++)
            if (hero.Passives[i].Def is { } p)
            {
                c.Text($"{p.Name} Lv {hero.Passives[i].Level}", new Vector2(x, y), Theme.BodyText, Theme.PassiveBand);
                y += 20;
            }
        for (var i = 0; i < BlessingSlots.SlotCount; i++)
            if (hero.Blessings[i].Def is { } b)
            {
                c.Text(b.Name, new Vector2(x, y), Theme.BodyText, Theme.Text);
                y += 20;
            }
        y += 10;
        var s = hero.Stats;
        foreach (var line in StatLines(hero.MaxHp, s))
        {
            c.Text(line, new Vector2(x, y), Theme.SmallText, Theme.TextDim);
            y += 16;
        }
        c.End();
    }

    /// <summary>능력치 요약(기획 12.3): 최대 체력, 이동, 피해 %, 쿨 %, 치명 %, 방어, 획득 반경.</summary>
    public static IEnumerable<string> StatLines(float maxHp, StatBlock s)
    {
        var inv = CultureInfo.InvariantCulture;
        yield return string.Create(inv, $"최대 체력 {maxHp:0}");
        yield return string.Create(inv, $"이동 {s[StatKey.MoveSpeed]:0}");
        yield return string.Create(inv, $"피해 {s[StatKey.DamagePct] * 100:+0;-0;0}%");
        yield return string.Create(inv, $"쿨 {s[StatKey.CooldownPct] * 100:+0;-0;0}%");
        yield return string.Create(inv, $"치명 {s[StatKey.CritChance] * 100:0}%");
        yield return string.Create(inv, $"방어 {s[StatKey.Armor]:0}");
        yield return string.Create(inv, $"획득 반경 {s[StatKey.PickupRadius]:0}");
    }
}
