using System.Globalization;
using DiabloDefenceRemastered.Rendering;
using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes.Overlays;

/// <summary>
/// 일시정지 (설계서 8.1, 기획 12.3). Esc/P 로 열고 닫는다. 메뉴: <c>이어하기</c> / <c>설정</c> / (계단 저장을 할 수 있을 때만) <c>저장하고 끝내기</c> / <c>포기</c>.
/// 가운데에 지금 빌드(무기·패시브·축복과 레벨, 능력치 요약), 오른쪽에 장비 여섯 칸(이름 등급 색 + 효과 한 줄 — v0.9, 단계 2 wp6). 들어오면 <c>Audio.SetPaused(true)</c>, 나가면 false(7.9).
/// <c>저장하고 끝내기</c> 가 <c>Suspend</c> 예외로 실패하면 <c>저장 못 함</c> 한 줄을 보이고 판을 이어 간다(v0.8.2).
/// </summary>
public sealed class PauseOverlay(GameFlow flow, PlayScene play) : IScene
{
    private readonly MenuList _menu = new(MenuCenter, 200, 34f, 260f);
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
        // 바탕 판: 왼쪽 메뉴(알림 줄 포함)·가운데 빌드 목록·오른쪽 장비 여섯 칸을 한 판에 — 아래는 셋 중 긴 쪽
        var hero = play.World.Hero;
        var menuBottom = 200f + (_items.Count + 1) * _menu.Spacing + 10f;
        Ui.Backdrop(c, new RectF(MenuLeft, 110, PanelRight, MathF.Max(BuildHeight(hero), menuBottom) + 16f));
        Ui.Centered(c, "일시정지", MenuCenter, 130, Theme.TitleText, Theme.Gold);
        _menu.Draw(c);
        if (_notice is { } n) Ui.Centered(c, n, MenuCenter, 200 + _items.Count * _menu.Spacing + 10, Theme.BodyText, Theme.Red);
        DrawBuild(c, flow.App, hero);
        c.End();
    }

    // 배치(가상 px, v0.9 단계 2 wp6 — 장비 칸이 들어갈 셋째 열을 위해 판을 넓힘)
    public const float MenuLeft = 70f;
    public const float MenuCenter = 210f;
    public const float BuildX = 380f;
    public const float EquipX = 640f;
    public const float PanelRight = 900f;

    /// <summary>빌드·장비 목록이 차지하는 아래 끝(바탕 판 높이).</summary>
    public static float BuildHeight(Hero hero)
    {
        var named = 0;
        for (var i = 0; i < WeaponSlots.SlotCount; i++) if (hero.Weapons[i].Def is not null) named++;
        for (var i = 0; i < PassiveSlots.SlotCount; i++) if (hero.Passives[i].Def is not null) named++;
        for (var i = 0; i < BlessingSlots.SlotCount; i++) if (hero.Blessings[i].Def is not null) named++;
        var build = 120f + 32f + named * 20f + 10f + StatLines(hero.MaxHp, hero.Stats).Count() * 16f;
        var equip = 120f + 32f + EquipSlots.Count * EquipRow;
        return MathF.Max(build, equip);
    }

    private const float EquipRow = 36f;

    /// <summary>
    /// 지금 빌드(기획 12.3): 가운데 열 = 무기·패시브·축복과 레벨, 능력치 요약 / 오른쪽 열 = 장비 여섯 칸의 이름(등급 색 — 보통 흰·마법 파랑)과 효과 한 줄씩(v0.9 —
    /// 장비를 자세히 보는 곳은 여기와 상인 창뿐). 빈 칸은 칸 이름을 흐리게. <c>--preview ui</c> 도 부른다.
    /// </summary>
    public static void DrawBuild(UiCanvas c, AppServices app, Hero hero)
    {
        var x = BuildX;
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
        foreach (var line in StatLines(hero.MaxHp, hero.Stats))
        {
            c.Text(line, new Vector2(x, y), Theme.SmallText, Theme.TextDim);
            y += 16;
        }

        // 장비 여섯 칸
        var defs = app.Defs;
        var rules = defs.Rules.Equipment;
        var ex = EquipX;
        var ey = 120f;
        c.Text("장비", new Vector2(ex, ey), Theme.MenuText, Theme.Gold);
        ey += 32;
        var width = PanelRight - 10f - (ex + 26f);
        for (var i = 0; i < EquipSlots.Count; i++)
        {
            var item = hero.Equipment[i];
            var icon = new RectF(ex, ey + 2, ex + 20, ey + 22);
            c.Fill(icon, Theme.Panel, 0.85f);
            if (item is null)
            {
                ItemArt.Silhouette(c, ItemArt.KindOf((EquipSlot)i), icon, Theme.PanelEdge, 0.45f);
                c.Text(SlotName((EquipSlot)i), new Vector2(ex + 26, ey), Theme.BodyText, Theme.Grey);
                ey += EquipRow;
                continue;
            }
            Ui.Outline(c, icon, item.Magic ? ShopWindow.MagicText : Theme.PanelEdge);
            ItemArt.Item(c, app, item, icon, 1f, item.Magic ? ShopWindow.MagicText : Theme.Text);
            ShopWindow.FitText(c, ItemText.Name(item, defs), new Vector2(ex + 26, ey), width, 15f, item.Magic ? ShopWindow.MagicText : Theme.White);
            ShopWindow.FitText(c, ItemText.OneLine(item, defs, rules), new Vector2(ex + 26, ey + 18), width, 11f, Theme.TextDim);
            ey += EquipRow;
        }
    }

    /// <summary>빈 칸 이름(투구·갑옷·방패·지팡이·반지).</summary>
    public static string SlotName(EquipSlot slot) => slot switch
    {
        EquipSlot.Head => "투구",
        EquipSlot.Body => "갑옷",
        EquipSlot.Shield => "방패",
        EquipSlot.Staff => "지팡이",
        _ => "반지",
    };

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
