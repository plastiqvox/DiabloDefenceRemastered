using System.Globalization;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Loading;
using DiabloDefenceRemastered.Scenes.Overlays;
using DiabloDefenceRemastered.UI;
using Color = Microsoft.Xna.Framework.Color;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>
/// 화면 확인 씬(<c>--preview ui</c>, 단계 2 wp6 — 소유 WP6). 합성 상태로 HUD·상인 창·일시정지 장비 목록·결과 금화 줄을 쪽마다 1초(60 앱 틱)씩 차례로 그린다.
/// 쪽 k(1부터)는 앱 시간 k 초에 캡처하면 늘 같은 그림이다(<c>--capture-at 1,2,…</c>). 상인 물건은 실제 데이터로 <c>ShopStock.Create</c>(상인 전용 흐름 — 고정 씨앗)가 정한다.
/// <list type="number">
/// <item>HUD — 구슬 100%, 물약 0/2, 장비 줄 빈 것, 지갑 0, 순간이동 2/2(가득).</item>
/// <item>HUD — 구슬 40%, 물약 2/2(가득), 장비 줄 찬 것(보통·마법), 지갑 1,240 번쩍, 순간이동 1/2(둘째 칸 반쯤 참).</item>
/// <item>HUD — 구슬 40% 마시는 중(차오름 띠 → 80%), 물약 3/5, 물약 칸 번쩍, 순간이동 0/3(숙련 Lv2 — 첫 칸 30%)·시전 중(쓰는 칸 맥동).</item>
/// <item>그리스월드(액트 1) — 산 것 하나·금화 모자란 카드.</item>
/// <item>아드리아(액트 2) — 접사 둘 카드가 나오는 씨앗.</item>
/// <item>아드리아 — 반지 둘이 찬 채 반지를 고름(<c>RingPending</c> — 바꿀 반지 두 장).</item>
/// <item>페핀 — 주머니 단계 1(3칸)·빠른 물약 다 올림·채우기 2병, 금화가 주머니 값에 모자람.</item>
/// <item>페핀 — 주머니 다 올림·칸 가득.</item>
/// <item>일시정지 빌드·장비 목록, 순간이동 1/2 "못 함"(칸 테 붉음 — 보스 묶음 B).</item>
/// <item>결과 화면(<c>얻은 금화 · 쓴 금화</c>·장비 여섯 칸).</item>
/// </list>
/// 쪽이 바뀔 때 <c>PREVIEW ui page=&lt;k&gt; …</c> 로그 한 줄.
/// </summary>
public sealed class UiPreviewScene(AppServices app) : IScene
{
    public const int PageTicks = 60;
    public const int Pages = 10;

    private readonly GameFlow _flow = GameFlow.Of(app);
    private readonly Hud _hud = new(app);
    private readonly ShopWindow _shopWindow = new(app);
    private World? _world;
    private ShopState? _shop;
    private MerchantDef? _merchant;
    private ResultScene? _result;
    private int _page = -1;
    private long _now;

    public bool PausesBelow => false;
    public bool DrawsBelow => false;

    public void Enter(SceneStack stack)
    {
        (app.Art as ArtServices)?.PreloadAll();
        _world = new World(app.Defs, app.Options.Seed, null, _flow.DefaultHero());
        _hud.Attach(_world);
        _hud.Minimap.Visible = false;
    }

    public void Exit() { }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        _now = t.Value;
        if (_world is not { } w) return;
        var page = (int)Math.Clamp((_now - 1) / PageTicks, 0, Pages - 1);
        w.Tick = (int)_now;
        if (page == 8) w.Hero.BlinkDeniedTick = w.Tick;   // 못 함 붉은 테를 쪽 내내 켬(실제로는 0.2s)
        if (page == _page) return;
        _page = page;
        Setup(w, page);
        _hud.Attach(w);
        if (page == 1) _hud.FlashGold();
        if (page == 2) _hud.OnPotionDrink(w.Tick);
        app.Log.Info(string.Create(CultureInfo.InvariantCulture, $"PREVIEW ui page={page + 1} {PageName(page)}"));
    }

    private static string PageName(int page) => page switch
    {
        0 => "hud_full_empty",
        1 => "hud_40_full_equipment",
        2 => "hud_drinking",
        3 => "shop_smith",
        4 => "shop_witch",
        5 => "shop_ring_pending",
        6 => "shop_healer",
        7 => "shop_healer_maxed",
        8 => "pause_equipment",
        _ => "result",
    };

    // ── 합성 상태 ────────────────────────────────────

    private void Setup(World w, int page)
    {
        var defs = app.Defs;
        var hero = w.Hero;
        hero.MaxHp = 120f;
        hero.Hp = 120f;
        hero.Heal = default;
        hero.Potions = 0;
        hero.PotionSlotsLevel = 0;
        hero.PotionSpeedLevel = 0;
        hero.Gold = 0;
        Array.Clear(hero.Equipment);
        // 순간이동 충전 칸(보스 묶음 B): 기본 가득, 쪽마다 아래에서 바꿈
        hero.Stats.Clear();
        hero.BlinkCharges = HeroBlink.MaxCharges(hero, defs.Rules.Hero?.Blink);
        hero.BlinkRecharge = hero.BlinkCast = 0f;
        hero.BlinkDeniedTick = int.MinValue / 2;
        _shop = null;
        _merchant = null;
        _result = null;
        switch (page)
        {
            case 0:
                break;
            case 1:
                hero.Hp = 48f;
                hero.Potions = 2;
                hero.Gold = 1240;
                FullEquipment(hero);
                Blink(hero, charges: 0, fill: 0.5f);   // 1칸 — 다 써서 충전 중(짙은 덮개)
                break;
            case 2:
                hero.Hp = 48f;
                hero.Heal = HealOverTime.Start(48f, 180);
                hero.Potions = 3;
                hero.PotionSlotsLevel = 3;
                hero.Gold = 380;
                FullEquipment(hero);
                hero.Stats[StatKey.BlinkCharges] = 1;   // 숙련 Lv2 — 2칸
                Blink(hero, charges: 0, fill: 0.3f);
                hero.BlinkCast = 0.1f;
                break;
            case 3:
                hero.Hp = 90f;
                hero.Gold = 120;
                hero.Equipment[(int)EquipSlot.Head] = Item("eq_cap");
                OpenShop("smith", act: 1, room: 4, seed: 7);
                if (_shop is { Cards.Count: > 0 } smith) smith.Cards[0] = smith.Cards[0] with { Bought = true };
                break;
            case 4:
                hero.Gold = 300;
                hero.Equipment[(int)EquipSlot.Staff] = Item("eq_short_staff");
                OpenShop("witch", act: 2, room: 3, seed: TwoAffixSeed("witch", 2, 3));
                break;
            case 5:
            {
                hero.Gold = 500;
                var rings = ShopStock.Create(defs.Merchants.Get("witch"), 1, 4, 11, defs, default);
                hero.Equipment[(int)EquipSlot.Ring1] = rings.Cards[1].Item;
                hero.Equipment[(int)EquipSlot.Ring2] = rings.Cards[2].Item;
                OpenShop("witch", act: 1, room: 6, seed: 23);
                if (_shop is { } witch) witch.RingPending = 1;
                break;
            }
            case 6:
                hero.Gold = 100;
                hero.Potions = 1;
                hero.PotionSlotsLevel = 1;
                hero.PotionSpeedLevel = 2;
                OpenShop("healer", act: 1, room: 5, seed: 1);
                break;
            case 7:
                hero.Gold = 400;
                hero.PotionSlotsLevel = 3;
                hero.Potions = 5;
                hero.PotionSpeedLevel = 1;
                OpenShop("healer", act: 2, room: 5, seed: 1);
                break;
            case 8:
                hero.Hp = 100f;
                FullEquipment(hero);
                hero.Stats[StatKey.BlinkCharges] = 1;   // 2칸 중 1칸 — 다음 칸 충전 중(옅은 덮개)
                Blink(hero, charges: 1, fill: 0.6f);
                break;
            default:
            {
                FullEquipment(hero);
                var act = RunPlanner.Root(defs);
                if (act is null) break;
                var result = new RunResult(act.Id, _flow.DefaultHero()?.Id ?? "", false, new RunDepth(1, 6), act.Id, 612f, 14, 1534, 1240);
                var summary = new ResultSummary(result, app.Options.Seed, act, _flow.DefaultHero(), act, [], [], null, 980, [.. hero.Equipment]);
                _result = new ResultScene(_flow, summary);
                break;
            }
        }
    }

    /// <summary>순간이동 칸 견본: 지금 칸·충전 중인 칸 진행(0..1).</summary>
    private void Blink(Hero hero, int charges, float fill)
    {
        hero.BlinkCharges = charges;
        hero.BlinkRecharge = app.Defs.Rules.Hero?.Blink is { } rules ? HeroBlink.RechargeTime(hero, rules) * fill : 0f;
    }

    private void OpenShop(string merchantId, int act, int room, ulong seed)
    {
        var defs = app.Defs;
        if (!defs.Merchants.TryGet(merchantId, out var m)) return;
        var hero = _world!.Hero;
        _merchant = m;
        _shop = ShopStock.Create(m, act, room, seed, defs, new ShopPotions(hero.Potions, hero.PotionSlotsLevel, hero.PotionSpeedLevel));
        _shop.Open = true;
        _shopWindow.Sync(null);
        _shopWindow.Sync(_shop);
    }

    /// <summary>접사 둘인 카드가 나오는 첫 씨앗(1..500 — 없으면 1).</summary>
    private ulong TwoAffixSeed(string merchantId, int act, int room)
    {
        var defs = app.Defs;
        if (!defs.Merchants.TryGet(merchantId, out var m)) return 1;
        for (ulong s = 1; s <= 500; s++)
        {
            var shop = ShopStock.Create(m, act, room, s, defs, default);
            if (shop.Cards.Exists(c => c.Item is { Prefix: not null, Suffix: not null })) return s;
        }
        return 1;
    }

    private EquipItem? Item(string baseId, (string Id, float Value)? prefix = null, (string Id, float Value)? suffix = null)
    {
        if (!app.Defs.ItemBases.TryGet(baseId, out var b)) return null;
        RolledAffix? p = prefix is { } pp ? new RolledAffix(pp.Id, pp.Value) : null;
        RolledAffix? s = suffix is { } ss ? new RolledAffix(ss.Id, ss.Value) : null;
        return new EquipItem(baseId, p, s, b.Price);
    }

    /// <summary>장비 여섯 칸 견본(보통 둘·마법 넷 — 접사 둘 하나 포함).</summary>
    private void FullEquipment(Hero hero)
    {
        hero.Equipment[(int)EquipSlot.Head] = Item("eq_helm");
        hero.Equipment[(int)EquipSlot.Body] = Item("eq_ring_mail", ("af_fine", 2f), ("af_fox", 11f));
        hero.Equipment[(int)EquipSlot.Shield] = Item("eq_large_shield");
        hero.Equipment[(int)EquipSlot.Staff] = Item("eq_long_staff", suffix: ("af_jaguar", 20f));
        hero.Equipment[(int)EquipSlot.Ring1] = Item("eq_ring", ("af_jagged", 0.04f));
        hero.Equipment[(int)EquipSlot.Ring2] = Item("eq_ring", suffix: ("af_harmony", 0.5f));
    }

    // ── 그리기 ───────────────────────────────────────

    public void Draw(RenderContext rc, float alpha)
    {
        rc.Device.Clear(new Color(34, 30, 28));
        if (_result is not null)
        {
            _result.Draw(rc, 1f);
            return;
        }
        if (_flow.Canvas is not { } c || _world is not { } w) return;
        c.Begin(rc);
        _hud.Draw(c, rc, null);
        if (_shop is { } shop) _shopWindow.Draw(c, new ShopView(shop, _merchant, w.Hero, app.Defs));
        if (_page == 8)
        {
            Ui.Backdrop(c, new RectF(PauseOverlay.PanelRight - 540, 110, PauseOverlay.PanelRight, PauseOverlay.BuildHeight(w.Hero) + 16f));
            PauseOverlay.DrawBuild(c, app, w.Hero);
        }
        c.Text($"--preview ui {_page + 1}/{Pages} — {PageName(_page)}", new Vector2(300, 524), Theme.SmallText, Theme.TextDim);
        c.End();
    }
}
