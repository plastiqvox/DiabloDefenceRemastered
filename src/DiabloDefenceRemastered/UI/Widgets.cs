using DiabloDefenceRemastered.Rendering;

namespace DiabloDefenceRemastered.UI;

/// <summary>메뉴 한 줄. <see cref="Enabled"/> 가 거짓이면 어둡게 그리고 고를 수 없다.</summary>
public sealed record MenuItem(string Label, bool Enabled = true);

/// <summary>
/// 세로 메뉴 (설계서 8.2 "위젯" — 마우스 올림·방향키 선택·Enter 를 같은 "포커스" 로). 즉시 모드에 가깝다:
/// 씬이 앱 틱마다 <see cref="Update"/> 를 불러 고른 줄을 받고, 그릴 때 <see cref="Draw"/> 를 부른다.
/// 판정(포커스·고르기)은 그림과 무관해 그래픽 장치 없이 돈다.
/// </summary>
public sealed class MenuList
{
    private readonly List<MenuItem> _items = [];

    /// <summary>가운데 x, 첫 줄 위 y, 줄 간격(가상 좌표).</summary>
    public MenuList(float centerX, float top, float spacing = 34f, float width = 300f)
    {
        CenterX = centerX;
        Top = top;
        Spacing = spacing;
        ItemWidth = width;
    }

    public float CenterX { get; set; }
    public float Top { get; set; }
    public float Spacing { get; }
    public float ItemWidth { get; }
    public float TextSize { get; set; } = Theme.MenuText;
    public IReadOnlyList<MenuItem> Items => _items;
    public int Focus { get; private set; }

    /// <summary>줄을 바꾼다(포커스는 범위 안 + 고를 수 있는 줄로).</summary>
    public void SetItems(IEnumerable<MenuItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        Focus = Math.Clamp(Focus, 0, Math.Max(0, _items.Count - 1));
        if (_items.Count > 0 && !_items[Focus].Enabled) Move(1);
    }

    public void SetFocus(int index)
    {
        if ((uint)index < (uint)_items.Count && _items[index].Enabled) Focus = index;
    }

    /// <summary>이 줄의 자리(마우스 판정·그리기).</summary>
    public RectF ItemRect(int index)
    {
        var y = Top + index * Spacing;
        return new RectF(CenterX - ItemWidth / 2, y, CenterX + ItemWidth / 2, y + Spacing - 4f);
    }

    /// <summary>
    /// 앱 틱 하나의 입력. 위·아래로 포커스, 마우스가 줄 위면 포커스, Enter/Space·줄 위 클릭이면 그 줄을 돌려준다(아니면 −1).
    /// <paramref name="mouse"/> = 가상 좌표의 마우스(창 밖·그림 없음이면 null). <paramref name="moved"/> = 포커스가 바뀜(메뉴 소리).
    /// </summary>
    public int Update(InputFrame input, Vector2? mouse, out bool moved)
    {
        moved = false;
        if (_items.Count == 0) return -1;
        var before = Focus;
        if (input.IsPressed(UiButtons.Up)) Move(-1);
        if (input.IsPressed(UiButtons.Down)) Move(1);

        var hovered = -1;
        if (mouse is { } m)
            for (var i = 0; i < _items.Count; i++)
            {
                var r = ItemRect(i);
                if (m.X >= r.X0 && m.X < r.X1 && m.Y >= r.Y0 && m.Y < r.Y1 && _items[i].Enabled)
                {
                    hovered = i;
                    Focus = i;
                    break;
                }
            }

        moved = Focus != before;
        if (input.IsPressed(UiButtons.Confirm) && _items[Focus].Enabled) return Focus;
        if (input.IsPressed(UiButtons.Click) && hovered >= 0) return hovered;
        return -1;
    }

    private void Move(int step)
    {
        for (var k = 0; k < _items.Count; k++)
        {
            Focus = (Focus + step + _items.Count) % _items.Count;
            if (_items[Focus].Enabled) return;
        }
    }

    public void Draw(UiCanvas c, float alpha = 1f)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var r = ItemRect(i);
            var size = c.MeasureText(item.Label, TextSize);
            var pos = new Vector2(CenterX - size.X / 2, r.Y0);
            var color = !item.Enabled ? Theme.Grey : i == Focus ? Theme.Focus : Theme.Text;
            if (i == Focus && item.Enabled) c.Fill(new RectF(r.X0, r.Y0, r.X1, r.Y1), Theme.DarkRed, 0.35f * alpha);
            c.Text(item.Label, pos, TextSize, color, alpha);
        }
    }
}

/// <summary>여러 씬이 같이 쓰는 그리기 도우미(가상 좌표).</summary>
public static class Ui
{
    public static readonly RectF Screen = new(0, 0, Theme.Width, Theme.Height);

    /// <summary>화면 전체를 어둡게(오버레이 배경).</summary>
    public static void Dim(UiCanvas c, float alpha = Theme.DimAlpha) => c.Fill(Screen, Theme.Black, alpha);

    /// <summary>
    /// 메뉴 오버레이 바탕: 화면을 어둡게 + 내용 자리에 거의 불투명한 판(<see cref="OverlayPanelAlpha"/>). 어둡게만 하면 아래 씬의 글자(타이틀 메뉴·최고 기록,
    /// 일시정지 메뉴·빌드 목록)가 25~40% 밝기로 남아 같은 크기의 메뉴 글자와 겹쳐 읽혔다(플레이 관찰). 카드·줄마다 판이 있는 레벨업·상자 창은 쓰지 않는다.
    /// </summary>
    public static void Backdrop(UiCanvas c, RectF content, float dim = Theme.DimAlpha)
    {
        Dim(c, dim);
        Panel(c, content, OverlayPanelAlpha);
    }

    public const float OverlayPanelAlpha = 0.96f;

    /// <summary>테두리 있는 판.</summary>
    public static void Panel(UiCanvas c, RectF r, float alpha = 0.92f)
    {
        c.Fill(new RectF(r.X0 - 2, r.Y0 - 2, r.X1 + 2, r.Y1 + 2), Theme.PanelEdge, alpha);
        c.Fill(r, Theme.Panel, alpha);
    }

    /// <summary>가운데 맞춘 글자(<paramref name="y"/> = 글자 위).</summary>
    public static void Centered(UiCanvas c, string text, float centerX, float y, float size, Rgb color, float alpha = 1f)
    {
        var s = c.MeasureText(text, size);
        c.Text(text, new Vector2(centerX - s.X / 2, y), size, color, alpha);
    }

    /// <summary>오른쪽 맞춘 글자.</summary>
    public static void Right(UiCanvas c, string text, float rightX, float y, float size, Rgb color, float alpha = 1f)
    {
        var s = c.MeasureText(text, size);
        c.Text(text, new Vector2(rightX - s.X, y), size, color, alpha);
    }

    /// <summary>막대(배경 + 비율만큼 채움).</summary>
    public static void Bar(UiCanvas c, RectF r, float ratio, Rgb fill, Rgb back, float alpha = 1f)
    {
        c.Fill(r, back, alpha);
        var t = Math.Clamp(ratio, 0f, 1f);
        if (t > 0f) c.Fill(new RectF(r.X0, r.Y0, r.X0 + r.Width * t, r.Y1), fill, alpha);
    }

    /// <summary>
    /// 아이콘(<see cref="IconDef"/>, 6.8) 한 칸. 그림 정의가 없거나 그림 서비스가 없으면 칸만 그린다.
    /// 그림 정의의 프레임 하나를 <paramref name="box"/> 에 맞춰 그린다 — 겹침(<c>layers</c>)·회전은 WP5 아이콘 그리기가 생기면 그쪽으로 옮긴다.
    /// </summary>
    public static void Icon(UiCanvas c, GameDefs defs, IconDef? icon, RectF box, float alpha = 1f)
    {
        if (icon is null) return;
        var sprite = icon.Sprite.IsBound ? icon.Sprite.Def : defs.Sprites.TryGet(icon.Sprite.Id, out var s) ? s : null;
        if (sprite is null) return;
        var frame = Math.Max(1, icon.Frame ?? 1) - 1;
        var dir = Math.Max(0, icon.Dir ?? 0);
        var fw = sprite.Frame.X > 0 ? sprite.Frame.X : sprite.Sheet.X;
        var fh = sprite.Frame.Y > 0 ? sprite.Frame.Y : sprite.Sheet.Y;
        var src = new RectI(frame * fw, dir * fh, frame * fw + fw, dir * fh + fh);
        c.Sprite(sprite.Id, src, box, icon.Tint ?? Rgb.White, alpha);
        if (icon.FrameBorder == "gold") Outline(c, box, Theme.Gold, alpha);
    }

    /// <summary>사각형 테(1px).</summary>
    public static void Outline(UiCanvas c, RectF r, Rgb color, float alpha = 1f, float w = 1f)
    {
        c.Fill(new RectF(r.X0, r.Y0, r.X1, r.Y0 + w), color, alpha);
        c.Fill(new RectF(r.X0, r.Y1 - w, r.X1, r.Y1), color, alpha);
        c.Fill(new RectF(r.X0, r.Y0, r.X0 + w, r.Y1), color, alpha);
        c.Fill(new RectF(r.X1 - w, r.Y0, r.X1, r.Y1), color, alpha);
    }
}
