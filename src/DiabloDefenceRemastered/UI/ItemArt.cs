using DiabloDefenceRemastered.Loading;
using DiabloDefenceRemastered.Rendering;

namespace DiabloDefenceRemastered.UI;

/// <summary>
/// 장비·물약·금화·조작판 그림 그리기 (기획 11.6 V4·12.1 V12·12.8 — 단계 2 wp6). 원작 인벤토리 그림(<c>ItemBaseDef.Icon</c> — <c>inv_*</c>)을 칸에 비율대로 맞추고,
/// 그림이 없으면(자원 꺼짐·가져오기에서 빠짐) 칸 모양 실루엣 코드 그림(투구·갑옷·방패·지팡이·반지 — 일반 모양, 상표·남의 그림을 본뜨지 않음)으로 대신한다(6.3 대체 규칙).
/// 조작판 그림(<c>ui_panel8</c>·<c>ui_p8bulbs</c>)은 <see cref="Crop"/> 로 <b>칸 좌표</b>(가져오기 표 주석의 자리) 사각형을 잘라 그린다 — 아틀라스는 빈 가장자리를 잘라 넣으므로
/// 칸 좌표를 메타의 사각형·<c>ox,oy</c>·배율로 페이지 픽셀로 바꾼다.
/// </summary>
public static class ItemArt
{
    /// <summary>체력 물약 인벤토리 그림(페핀 카드·HUD 물약 칸).</summary>
    public const string PotionSprite = "inv_potion_healing";

    private static readonly Rgb Shade = new(40, 34, 28);

    /// <summary>기본 아이템 그림(없으면 칸 모양 실루엣 <paramref name="fallback"/> 색).</summary>
    public static void Item(UiCanvas c, AppServices app, ItemBaseDef b, RectF box, float alpha, Rgb fallback)
    {
        if (b.Icon is { } icon && Fit(c, app, icon, box, alpha)) return;
        Silhouette(c, b.Kind, box, fallback, alpha);
    }

    /// <summary>장비 하나(정의에 없으면 칸 종류를 모르므로 아무것도 그리지 않음).</summary>
    public static void Item(UiCanvas c, AppServices app, EquipItem item, RectF box, float alpha, Rgb fallback)
    {
        if (app.Defs.ItemBases.TryGet(item.BaseId, out var b)) Item(c, app, b, box, alpha, fallback);
    }

    /// <summary>체력 물약 그림(없으면 코드 물약 — 둥근 병 + 목).</summary>
    public static void Potion(UiCanvas c, AppServices app, RectF box, float alpha)
    {
        if (FitSprite(c, app, PotionSprite, 1, box, Rgb.White, alpha)) return;
        var center = box.Center;
        var r = MathF.Min(box.Width, box.Height) * 0.3f;
        c.Fill(new RectF(center.X - r * 0.3f, center.Y - r * 1.6f, center.X + r * 0.3f, center.Y - r * 0.6f), new Rgb(160, 140, 120), alpha);
        c.Disc(center + new Vector2(0f, r * 0.3f), r, Theme.Red, alpha);
    }

    /// <summary>금화 아이콘(원작 exe <c>GoldIcon</c>, 없으면 금색 동그라미).</summary>
    public static void Gold(UiCanvas c, AppServices app, RectF box, float alpha = 1f)
    {
        if (FitSprite(c, app, "GoldIcon", 1, box, Rgb.White, alpha)) return;
        var r = MathF.Min(box.Width, box.Height) * 0.4f;
        c.Disc(box.Center, r, Theme.Gold, alpha);
        c.Disc(box.Center, r * 0.6f, new Rgb(180, 140, 50), alpha);
    }

    /// <summary>
    /// 칸 모양 실루엣(기획 11.6 "확인 전 대체" — 흰·파랑 테는 부르는 쪽). 일반 모양만: 투구 = 둥근 머리 + 챙, 갑옷 = 어깨 넓은 몸통, 방패 = 아래가 좁아지는 판,
    /// 지팡이 = 세로 막대 + 위 구슬, 반지 = 고리.
    /// </summary>
    public static void Silhouette(UiCanvas c, EquipKind kind, RectF box, Rgb color, float alpha)
    {
        var cx = box.Center.X;
        var cy = box.Center.Y;
        var s = MathF.Min(box.Width, box.Height) * 0.5f;
        switch (kind)
        {
            case EquipKind.Head:
                c.Disc(new Vector2(cx, cy + s * 0.1f), s * 0.62f, color, alpha, 0.55f);
                c.Fill(new RectF(cx - s * 0.75f, cy + s * 0.05f, cx + s * 0.75f, cy + s * 0.3f), color, alpha);
                break;
            case EquipKind.Body:
                c.Fill(new RectF(cx - s * 0.8f, cy - s * 0.7f, cx + s * 0.8f, cy - s * 0.35f), color, alpha);
                c.Fill(new RectF(cx - s * 0.5f, cy - s * 0.4f, cx + s * 0.5f, cy + s * 0.75f), color, alpha);
                break;
            case EquipKind.Shield:
                c.Fill(new RectF(cx - s * 0.6f, cy - s * 0.75f, cx + s * 0.6f, cy + s * 0.15f), color, alpha);
                c.Fill(new RectF(cx - s * 0.42f, cy + s * 0.15f, cx + s * 0.42f, cy + s * 0.45f), color, alpha);
                c.Fill(new RectF(cx - s * 0.2f, cy + s * 0.45f, cx + s * 0.2f, cy + s * 0.75f), color, alpha);
                break;
            case EquipKind.Staff:
                c.Fill(new RectF(cx - s * 0.1f, cy - s * 0.5f, cx + s * 0.1f, cy + s * 0.85f), color, alpha);
                c.Disc(new Vector2(cx, cy - s * 0.6f), s * 0.25f, color, alpha);
                break;
            default:
                c.Disc(box.Center, s * 0.55f, color, alpha);
                c.Disc(box.Center, s * 0.32f, Shade, alpha);
                break;
        }
    }

    /// <summary>칸 번호 → 칸 종류(반지 둘은 <see cref="EquipKind.Ring"/>).</summary>
    public static EquipKind KindOf(EquipSlot slot) => slot switch
    {
        EquipSlot.Head => EquipKind.Head,
        EquipSlot.Body => EquipKind.Body,
        EquipSlot.Shield => EquipKind.Shield,
        EquipSlot.Staff => EquipKind.Staff,
        _ => EquipKind.Ring,
    };

    /// <summary><see cref="IconDef"/> 하나를 칸에 비율대로(묶인 그림이 아니면 ID 로 찾음).</summary>
    public static bool Fit(UiCanvas c, AppServices app, IconDef icon, RectF box, float alpha)
    {
        var id = icon.Sprite.IsBound ? icon.Sprite.Def.Id : icon.Sprite.Id;
        return FitSprite(c, app, id, icon.Frame ?? 1, box, icon.Tint ?? Rgb.White, alpha);
    }

    /// <summary>그림 <paramref name="spriteId"/> 의 한 프레임을 칸에 비율대로(<see cref="RewardIcons.Fit(UiCanvas, ArtServices, SpriteDef, int, int, RectF, Rgb, float)"/>). 그림이 없으면 거짓.</summary>
    public static bool FitSprite(UiCanvas c, AppServices app, string spriteId, int frame1, RectF box, Rgb tint, float alpha)
    {
        if (app.Art is not ArtServices art || !app.Defs.Sprites.TryGet(spriteId, out var sprite)) return false;
        return RewardIcons.Fit(c, art, sprite, 0, frame1, box, tint, alpha);
    }

    /// <summary>그림이 그려질 수 있나(정의·자원 파일 있음).</summary>
    public static bool Has(AppServices app, string spriteId) =>
        app.Art is ArtServices art && app.Defs.Sprites.TryGet(spriteId, out var sprite) && art.TryGetSprite(sprite, "", out _);

    /// <summary>
    /// 아틀라스 그림 한 프레임의 <b>칸 좌표</b> 사각형 <paramref name="cell"/>(원본 픽셀)을 <paramref name="dest"/> 에 늘여 그린다. 잘려 나간 빈 가장자리는 그리지 않는다(자리만 맞춤).
    /// 그림이 없거나 grid 그림·첫 페이지가 아니면 거짓.
    /// </summary>
    public static bool Crop(UiCanvas c, AppServices app, string spriteId, int frame1, RectI cell, RectF dest, Rgb tint, float alpha)
    {
        if (cell.Width <= 0 || cell.Height <= 0) return true;
        if (app.Art is not ArtServices art || !app.Defs.Sprites.TryGet(spriteId, out var sprite)) return false;
        if (!art.TryGetSprite(sprite, "", out var h) || h.Action is not { } action || h.Meta is not { } meta) return false;
        var r = action.Rect(0, Math.Clamp(frame1, 1, action.Frames));
        if (r.IsEmpty || r.Page != 0) return false;
        var ms = Math.Max(1, meta.Scale);
        // 칸 좌표 → 페이지 배율 좌표(칸 왼쪽 위 기준) → 잘린 사각형(ox, oy 부터 W×H)과 겹치는 곳
        int sx0 = cell.X0 * ms, sy0 = cell.Y0 * ms, sx1 = cell.X1 * ms, sy1 = cell.Y1 * ms;
        int ix0 = Math.Max(sx0, r.Ox), iy0 = Math.Max(sy0, r.Oy), ix1 = Math.Min(sx1, r.Ox + r.W), iy1 = Math.Min(sy1, r.Oy + r.H);
        if (ix1 <= ix0 || iy1 <= iy0) return true;
        var kx = dest.Width / (sx1 - sx0);
        var ky = dest.Height / (sy1 - sy0);
        var d = new RectF(dest.X0 + (ix0 - sx0) * kx, dest.Y0 + (iy0 - sy0) * ky, dest.X0 + (ix1 - sx0) * kx, dest.Y0 + (iy1 - sy0) * ky);
        var src = new RectI(r.X + ix0 - r.Ox, r.Y + iy0 - r.Oy, r.X + ix1 - r.Ox, r.Y + iy1 - r.Oy);
        c.Sprite(sprite.Id, src, d, tint, alpha);
        return true;
    }
}
