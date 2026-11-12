using System.Globalization;
using DiabloDefenceRemastered.Loading;
using DiabloDefenceRemastered.Rendering;

namespace DiabloDefenceRemastered.UI;

/// <summary>상인 창이 그리는 것(<c>Room.Shop</c> + 그 상인 정의 + 주인공 — 실제 판과 <c>--preview ui</c> 합성 상태가 같은 모양).</summary>
public readonly record struct ShopView(ShopState Shop, MerchantDef? Merchant, Hero Hero, GameDefs Defs);

/// <summary>
/// 상인 창 (기획 12.8 — 레벨업 창 모양 재사용, 설계서 6.19 "상인 방 상태와 구매 명령" — 단계 2 wp6). <b>판을 멈추는 오버레이가 아니다</b>:
/// <c>PlayScene</c> 이 앱 틱마다 명령을 계속 <c>StageStepper</c> 로 넘기고(시간 멈춤·사기는 Core <c>ShopInput</c>), 이 창은 사람 입력을 명령 비트로 바꾸고(<see cref="Buttons"/>) 그리기만 한다.
/// 자동 조작은 Core 정책이 명령을 내고 이 창은 그림만 그린다.
/// <list type="bullet">
/// <item>입력: <c>1/2/3</c>·카드 클릭·←→/↑↓ + Enter/Space → <c>Choice1~3</c>, <c>R</c>·다시 굴리기 단추 → <c>Reroll</c>, <c>X</c>·<c>Esc</c>·떠나기 단추 → <c>Skip</c>
/// (반지 바꾸기 중이면 <c>Skip</c> = 사지 않음). 선택 입력 잠금 0.4s 는 Core(<c>ShopLockTicks</c>)가 버린다.</item>
/// <item>모양: 위에 상인 그림 한 프레임 + 이름 + 지갑 금화, 가운데 카드 셋(200×170 — 인벤토리 그림·이름 등급 색·무게 줄·효과 줄·차이 줄·값), 아래 다시 굴리기·떠나기.
/// 모자란 카드는 어둡고 값이 붉다, 산 카드는 <c>산 것</c> 으로 자리를 지킨다. <c>RingPending ≥ 0</c> 이면 "바꿀 반지" 카드 둘(지금 반지 둘).
/// 페핀은 카드 셋(물약 주머니·빠른 물약·물약 채우기 — 다 올리면 <c>다 올림</c>), 다시 굴리기 자리는 어둡다.</item>
/// </list>
/// </summary>
public sealed class ShopWindow(AppServices app)
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public const float CardWidth = 200f;
    public const float CardHeight = 170f;
    public const float CardGap = 20f;
    public const float CardTop = 178f;
    /// <summary>아래 단추 줄 위(카드 아래 30).</summary>
    public const float ButtonTop = CardTop + CardHeight + 26f;

    /// <summary>마법 등급 이름 색(원작 파란 이름 — 어두운 판 위에서 읽히게 밝게).</summary>
    public static readonly Rgb MagicText = new(120, 140, 255);
    private static readonly Rgb MagicBand = new(70, 90, 230);
    private static readonly Rgb NormalBand = new(170, 165, 150);

    private bool _wasOpen;
    private int _wasPending = -1;

    /// <summary>고른 카드(키보드·마우스 올림).</summary>
    public int Focus { get; private set; }

    /// <summary>
    /// 앱 틱마다(그리기 전에) 창 상태를 맞춘다: 새로 열리거나 반지 바꾸기로 바뀌면 고른 카드를 첫 장으로. 새로 열렸으면 참(<c>SCENE Shop</c> 로그).
    /// </summary>
    public bool Sync(ShopState? shop)
    {
        var open = shop is { Open: true };
        var opened = open && !_wasOpen;
        if (opened || open && shop!.RingPending != _wasPending) Focus = 0;
        _wasOpen = open;
        _wasPending = open ? shop!.RingPending : -1;
        return opened;
    }

    /// <summary>지금 보이는 카드 수(반지 바꾸기 = 2).</summary>
    public static int CardCount(ShopState shop) => shop.RingPending >= 0 ? 2 : Math.Min(3, shop.Cards.Count);

    public static RectF CardRect(int i, int n)
    {
        var total = n * CardWidth + (n - 1) * CardGap;
        var x = Theme.Width / 2 - total / 2 + i * (CardWidth + CardGap);
        return new RectF(x, CardTop, x + CardWidth, CardTop + CardHeight);
    }

    public static RectF RerollRect => new(Theme.Width / 2 - 330, ButtonTop, Theme.Width / 2 - 30, ButtonTop + 28);
    public static RectF LeaveRect => new(Theme.Width / 2 + 30, ButtonTop, Theme.Width / 2 + 330, ButtonTop + 28);

    private static bool Inside(Vector2 m, RectF r) => m.X >= r.X0 && m.X < r.X1 && m.Y >= r.Y0 && m.Y < r.Y1;

    /// <summary>
    /// 사람 입력 한 앱 틱 → 상인 창 명령 비트(<c>KeyboardMouseSource</c> 가 창이 열린 동안 부른다). 첫으로 맞는 것 하나: 숫자 → 카드, Enter/Space → 고른 카드,
    /// 클릭(카드·단추), <c>R</c> → 다시 굴리기, <c>X</c>·<c>Esc</c> → 떠나기(반지 바꾸기 중이면 사지 않음). 방향키·마우스 올림은 고른 카드만 바꾼다.
    /// <paramref name="mouse"/> = 가상 좌표 마우스(창 밖이면 null).
    /// </summary>
    public CommandButtons Buttons(InputFrame input, Vector2? mouse, ShopState shop)
    {
        var n = CardCount(shop);
        var before = Focus;
        if (n > 0)
        {
            if (input.IsPressed(UiButtons.Left) || input.IsPressed(UiButtons.Up)) Focus = (Focus + n - 1) % n;
            if (input.IsPressed(UiButtons.Right) || input.IsPressed(UiButtons.Down)) Focus = (Focus + 1) % n;
        }
        var hovered = -1;
        if (mouse is { } m)
            for (var i = 0; i < n; i++)
                if (Inside(m, CardRect(i, n))) { hovered = i; Focus = i; break; }
        Focus = n > 0 ? Math.Clamp(Focus, 0, n - 1) : 0;
        if (Focus != before) app.Audio?.PlayUi("menu_move");

        if (input.IsPressed(UiButtons.Choice1) && n >= 1) return CommandButtons.Choice1;
        if (input.IsPressed(UiButtons.Choice2) && n >= 2) return CommandButtons.Choice2;
        if (input.IsPressed(UiButtons.Choice3) && n >= 3) return CommandButtons.Choice3;
        if (input.IsPressed(UiButtons.Confirm) && n > 0) return Choice(Focus);
        if (input.IsPressed(UiButtons.Click) && mouse is { } click)
        {
            if (hovered >= 0) return Choice(hovered);
            if (Inside(click, RerollRect)) return CommandButtons.Reroll;
            if (Inside(click, LeaveRect)) return CommandButtons.Skip;
        }
        if (input.IsPressed(UiButtons.Reroll)) return CommandButtons.Reroll;
        if (input.IsPressed(UiButtons.Skip) || input.IsPressed(UiButtons.Cancel)) return CommandButtons.Skip;
        return CommandButtons.None;
    }

    private static CommandButtons Choice(int i) => i switch
    {
        0 => CommandButtons.Choice1,
        1 => CommandButtons.Choice2,
        _ => CommandButtons.Choice3,
    };

    // ── 그리기 ───────────────────────────────────────

    public void Draw(UiCanvas c, ShopView v)
    {
        var shop = v.Shop;
        var hero = v.Hero;
        Ui.Dim(c);
        DrawHeader(c, v);

        var pending = shop.RingPending >= 0 && shop.RingPending < shop.Cards.Count ? shop.Cards[shop.RingPending] : null;
        var n = CardCount(shop);
        if (pending is { Item: { } buying })
        {
            Ui.Centered(c, "바꿀 반지를 고르라", Theme.Width / 2, CardTop - 30, Theme.BodyText, Theme.Focus);
            for (var i = 0; i < n; i++)
            {
                var ring = hero.Equipment[(int)(i == 0 ? EquipSlot.Ring1 : EquipSlot.Ring2)];
                DrawRingCard(c, v, CardRect(i, n), i, ring, buying);
            }
            var line = $"살 것: {ItemText.Name(buying, v.Defs)}";
            Ui.Centered(c, line, Theme.Width / 2, ButtonTop - 20, Theme.SmallText, MagicText);
        }
        else
            for (var i = 0; i < n; i++)
            {
                var card = shop.Cards[i];
                var r = CardRect(i, n);
                if (card.Kind == ShopCardKind.Potion) DrawPotionCard(c, v, r, i, card);
                else DrawEquipCard(c, v, r, i, card);
            }

        // 아래 단추 — 다시 굴리기(페핀·반지 바꾸기 중은 어둡게) · 떠나기
        var merchant = v.Merchant;
        var rerollable = merchant is { Rerollable: true, Stock: MerchantStock.Equipment } && pending is null;
        string reroll;
        var rerollOk = false;
        if (rerollable && v.Defs.Rules.Equipment is { } rules)
        {
            var price = ShopStock.RerollPrice(rules, shop.Rerolls);
            rerollOk = hero.Gold >= price;
            reroll = string.Create(Inv, $"[R] 다시 굴리기 (금화 {RunText.Number(price)})");
        }
        else reroll = merchant is { Rerollable: false } ? "[R] 다시 굴리기 없음" : "[R] 다시 굴리기";
        DrawButton(c, RerollRect, reroll, rerollOk ? Theme.Text : Theme.Grey);
        DrawButton(c, LeaveRect, pending is null ? "[X] 떠나기" : "[X] 사지 않음", Theme.Text);
    }

    private void DrawHeader(UiCanvas c, ShopView v)
    {
        var box = new RectF(Theme.Width / 2 - 170, 40, Theme.Width / 2 - 90, 140);
        var drawn = false;
        if (v.Merchant?.Sprite?.Def.Clips.Idle?.Sprite.Def is { } towner && app.Art is ArtServices art)
            drawn = RewardIcons.Fit(c, art, towner, 0, 1, box, Rgb.White, 1f);
        if (!drawn)
        {
            // 코드 그림(일반 사람 실루엣 — 머리 + 망토)
            var cx = box.Center.X;
            c.Disc(new Vector2(cx, box.Y0 + 26), 14f, Theme.PanelEdge, 0.9f);
            c.Fill(new RectF(cx - 22, box.Y0 + 42, cx + 22, box.Y1 - 6), Theme.PanelEdge, 0.9f);
        }
        var name = v.Merchant?.Name is { Length: > 0 } nm ? nm : v.Shop.MerchantId;
        c.Text(name, new Vector2(Theme.Width / 2 - 70, 58), Theme.TitleText, Theme.Gold);
        ItemArt.Gold(c, app, new RectF(Theme.Width / 2 - 70, 104, Theme.Width / 2 - 48, 126));
        c.Text($"금화 {RunText.Number(v.Hero.Gold)}", new Vector2(Theme.Width / 2 - 42, 104), Theme.MenuText, Theme.Text);
    }

    private static void DrawButton(UiCanvas c, RectF r, string text, Rgb color)
    {
        c.Fill(r, Theme.Panel, 0.85f);
        Ui.Outline(c, r, Theme.PanelEdge);
        var m = c.MeasureText(text, Theme.BodyText);
        c.Text(text, new Vector2(r.Center.X - m.X / 2, r.Y0 + (r.Height - m.Y) / 2), Theme.BodyText, color);
    }

    /// <summary>카드 바탕(판 + 위 띠 + 고른 카드 테 + 번호).</summary>
    private void CardFrame(UiCanvas c, RectF r, int i, Rgb band)
    {
        Ui.Panel(c, r);
        c.Fill(new RectF(r.X0, r.Y0, r.X1, r.Y0 + 5), band, 0.95f);
        if (i == Focus) Ui.Outline(c, r, Theme.Focus, 1f, 2f);
        Ui.Right(c, string.Create(Inv, $"[{i + 1}]"), r.X1 - 6, r.Y0 + 8, Theme.SmallText, Theme.TextDim);
    }

    private void DrawEquipCard(UiCanvas c, ShopView v, RectF r, int i, ShopCard card)
    {
        var defs = v.Defs;
        var rules = defs.Rules.Equipment;
        if (card.Item is not { } item || !defs.ItemBases.TryGet(item.BaseId, out var b))
        {
            CardFrame(c, r, i, NormalBand);
            Ui.Centered(c, "물건 없음", r.Center.X, r.Center.Y - 8, Theme.BodyText, Theme.Grey);
            return;
        }
        var magic = item.Magic;
        CardFrame(c, r, i, magic ? MagicBand : NormalBand);
        ItemArt.Item(c, app, b, new RectF(r.X0 + 8, r.Y0 + 12, r.X0 + 50, r.Y0 + 70), 1f, magic ? MagicBand : NormalBand);

        // 오른쪽: 이름(등급 색, 줄바꿈) + 무게 줄
        var y = NameLines(c, ItemText.Name(item, defs), r.X0 + 56, r.Y0 + 12, r.X1 - 30, magic ? MagicText : Theme.White);
        if (ItemText.WeightLine(b, rules) is { } weight)
            c.Text(weight, new Vector2(r.X0 + 56, y + 2), Theme.SmallText, Theme.TextDim);

        // 효과 줄(접사 둘이면 한 단계 작게)
        var lines = ItemText.EffectLines(item, defs);
        var two = item.Prefix is not null && item.Suffix is not null;
        var size = two ? 11f : Theme.SmallText;
        var ly = r.Y0 + 74;
        foreach (var line in lines)
        {
            FitText(c, line, new Vector2(r.X0 + 10, ly), r.Width - 20, size, Theme.Text);
            ly += size + 3;
        }

        // 차이 줄 + 값
        var slot = DiffSlot(v.Hero, b.Kind);
        var diff = slot is { } s ? ItemText.DiffLine(v.Hero.Equipment[(int)s], item, defs, rules) : "반지 둘 — 고르면 바꿀 것을 묻는다";
        FitText(c, diff, new Vector2(r.X0 + 10, r.Y1 - 44), r.Width - 20, 11f, Theme.TextDim);
        PriceRow(c, r, card.Price, v.Hero.Gold >= card.Price);

        if (card.Bought) Done(c, r, "산 것");
        else if (v.Hero.Gold < card.Price) c.Fill(r, Theme.Black, 0.35f);
    }

    private void DrawRingCard(UiCanvas c, ShopView v, RectF r, int i, EquipItem? ring, EquipItem buying)
    {
        var defs = v.Defs;
        CardFrame(c, r, i, MagicBand);
        if (ring is null || !defs.ItemBases.TryGet(ring.BaseId, out var b))
        {
            Ui.Centered(c, "빈 칸", r.Center.X, r.Center.Y - 8, Theme.BodyText, Theme.Grey);
            return;
        }
        ItemArt.Item(c, app, b, new RectF(r.X0 + 8, r.Y0 + 12, r.X0 + 50, r.Y0 + 70), 1f, MagicBand);
        NameLines(c, ItemText.Name(ring, defs), r.X0 + 56, r.Y0 + 12, r.X1 - 30, ring.Magic ? MagicText : Theme.White);
        var ly = r.Y0 + 74;
        foreach (var line in ItemText.EffectLines(ring, defs))
        {
            FitText(c, line, new Vector2(r.X0 + 10, ly), r.Width - 20, Theme.SmallText, Theme.Text);
            ly += Theme.SmallText + 3;
        }
        FitText(c, ItemText.DiffLine(ring, buying, defs, defs.Rules.Equipment), new Vector2(r.X0 + 10, r.Y1 - 44), r.Width - 20, 11f, Theme.TextDim);
        Ui.Centered(c, "이 반지를 바꾼다", r.Center.X, r.Y1 - 24, Theme.SmallText, Theme.Focus);
    }

    private void DrawPotionCard(UiCanvas c, ShopView v, RectF r, int i, ShopCard card)
    {
        var hero = v.Hero;
        CardFrame(c, r, i, Theme.Red);
        ItemArt.Potion(c, app, new RectF(r.X0 + 8, r.Y0 + 12, r.X0 + 50, r.Y0 + 70), 1f);
        c.Text(ItemText.PotionName(card.Upgrade), new Vector2(r.X0 + 56, r.Y0 + 16), Theme.BodyText, Theme.White);
        if (v.Defs.Rules.Hero?.Potions is { } rules)
        {
            var effect = ItemText.PotionEffect(card.Upgrade, rules, hero.Potions, hero.PotionSlotsLevel, hero.PotionSpeedLevel, card.Bought);
            FitText(c, effect, new Vector2(r.X0 + 10, r.Y0 + 78), r.Width - 20, Theme.BodyText, Theme.Text);
            if (card.Upgrade == PotionUpgrade.Refill)
                c.Text(string.Create(Inv, $"물약 {hero.Potions} / {rules.Capacity(hero.PotionSlotsLevel)}"), new Vector2(r.X0 + 10, r.Y0 + 102), Theme.SmallText, Theme.TextDim);
        }
        if (card.Bought)
        {
            Done(c, r, ItemText.PotionDone(card.Upgrade));
            return;
        }
        PriceRow(c, r, card.Price, hero.Gold >= card.Price);
        if (hero.Gold < card.Price) c.Fill(r, Theme.Black, 0.35f);
    }

    /// <summary>값 줄(금화 아이콘 + 숫자, 모자라면 붉게).</summary>
    private void PriceRow(UiCanvas c, RectF r, int price, bool affordable)
    {
        ItemArt.Gold(c, app, new RectF(r.X0 + 8, r.Y1 - 26, r.X0 + 28, r.Y1 - 6));
        c.Text(RunText.Number(price), new Vector2(r.X0 + 32, r.Y1 - 26), Theme.BodyText, affordable ? Theme.Gold : Theme.Red);
    }

    /// <summary>살 수 없는 카드 덮개(<c>산 것</c>·<c>다 올림</c>·<c>칸 가득</c> — 자리는 지킨다).</summary>
    private static void Done(UiCanvas c, RectF r, string text)
    {
        c.Fill(r, Theme.Black, 0.6f);
        // 글자 뒤 띠 — 어둡게 남은 효과·차이 줄과 겹쳐 읽히지 않게
        c.Fill(new RectF(r.X0 + 2, r.Y0 + 98, r.X1 - 2, r.Y0 + 130), Theme.Panel, 0.95f);
        Ui.Centered(c, text, r.Center.X, r.Y0 + 101, Theme.MenuText, Theme.TextDim);
    }

    /// <summary>
    /// 이 장비를 견줄 칸: 반지가 아니면 그 칸, 반지는 빈 칸 먼저(둘 다 차면 null — 사는 순간 어느 반지를 바꿀지 묻는다, 기획 12.8).
    /// </summary>
    public static EquipSlot? DiffSlot(Hero hero, EquipKind kind)
    {
        if (kind != EquipKind.Ring) return Core.Sim.Heroes.HeroEquipment.SlotOf(kind);
        if (hero.Equipment[(int)EquipSlot.Ring1] is null) return EquipSlot.Ring1;
        if (hero.Equipment[(int)EquipSlot.Ring2] is null) return EquipSlot.Ring2;
        return null;
    }

    /// <summary>이름을 폭 안에서 단어 단위로 줄바꿈(14px, 세 줄을 넘으면 12px). 다음 줄 y 를 돌려준다.</summary>
    private static float NameLines(UiCanvas c, string name, float x, float y, float right, Rgb color)
    {
        var width = right - x;
        var size = 14f;
        var lines = Wrap(c, name, width, size);
        if (lines.Count > 2)
        {
            size = 12f;
            lines = Wrap(c, name, width, size);
        }
        foreach (var line in lines)
        {
            c.Text(line, new Vector2(x, y), size, color);
            y += size + 3;
        }
        return y;
    }

    private static List<string> Wrap(UiCanvas c, string text, float width, float size)
    {
        var lines = new List<string>();
        var current = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var next = current.Length == 0 ? word : current + " " + word;
            if (current.Length > 0 && c.MeasureText(next, size).X > width)
            {
                lines.Add(current);
                current = word;
            }
            else current = next;
        }
        if (current.Length > 0) lines.Add(current);
        return lines;
    }

    /// <summary>폭 안에 들게 글자를 한 단계씩 줄이고(최소 9px), 그래도 넘으면 끝을 <c>…</c> 로 자른다.</summary>
    public static void FitText(UiCanvas c, string text, Vector2 pos, float width, float size, Rgb color, float alpha = 1f)
    {
        var s = size;
        while (s > 9f && c.MeasureText(text, s).X > width) s -= 1f;
        var t = text;
        while (t.Length > 1 && c.MeasureText(t, s).X > width) t = t[..^2] + "…";
        c.Text(t, pos, s, color, alpha);
    }
}
