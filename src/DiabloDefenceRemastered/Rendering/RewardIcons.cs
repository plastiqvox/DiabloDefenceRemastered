using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Loading;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 보상 아이콘 그리기 (설계서 7.11·6.19, 기획 11.4 — 소유 WP5, 단계 2 wp5): 문 위 아이콘(48px 칸에 비율대로 맞춤)과 보상 물체 둘레 아이콘(<see cref="RewardOsd"/>, 24px).
/// 9층 월드 UI(<see cref="UiCanvas"/>, 가상 좌표)에서 그린다. <c>WorldRenderer</c> 와 <c>--preview sprites</c> 가 같이 쓴다.
/// </summary>
public static class RewardIcons
{
    private static readonly Rgb CardBody = new(48, 38, 30);
    private static readonly Rgb CardBand = new(235, 140, 40);
    private static readonly Rgb Frame = new(30, 26, 22);
    private static readonly Rgb Gold = new(230, 190, 80);

    /// <summary>
    /// 그림 한 프레임을 <paramref name="box"/> 안에 비율을 지켜 맞춰 가운데에 그린다(원작 그림 크기가 제각각 — 물약 13×14, 상인 38×65). 그림이 없으면 거짓.
    /// </summary>
    public static bool Fit(UiCanvas ui, ArtServices art, SpriteDef sprite, int dir, int frame1, RectF box, Rgb tint, float alpha)
    {
        if (!art.TryGetSprite(sprite, "", out var h)) return false;
        if (!art.Source(sprite, h, dir, frame1, out _, out var src, out _, Vector2.Zero, 1f) || src.Width <= 0 || src.Height <= 0) return false;
        var k = MathF.Min(box.Width / src.Width, box.Height / src.Height);
        var w = src.Width * k;
        var hh = src.Height * k;
        var c = box.Center;
        ui.Sprite(sprite.Id, src, new RectF(c.X - w * 0.5f, c.Y - hh * 0.5f, c.X + w * 0.5f, c.Y + hh * 0.5f), tint, alpha);
        return true;
    }

    /// <summary><see cref="IconDef"/> 하나(프레임이 없으면 1)를 맞춰 그림.</summary>
    public static bool Fit(UiCanvas ui, ArtServices art, IconDef icon, RectF box, float alpha) =>
        icon.Sprite.IsBound && Fit(ui, art, icon.Sprite.Def, icon.Dir ?? 0, icon.Frame ?? 1, box, icon.Tint ?? Rgb.White, alpha);

    /// <summary>
    /// 문 위 보상 아이콘(7.11): 상인 문 = 상인 그림 첫 프레임, 그 밖 = <c>RewardDef.Icon</c>. 원작 그림을 못 쓰면 방 안 물체의 대체 그림(<c>draw</c>),
    /// 회복 문은 물약 칸 획득물의 대체 그림. 그릴 것이 없으면 거짓.
    /// </summary>
    public static bool Door(UiCanvas ui, ArtServices art, GameDefs defs, RewardDef reward, RectF box)
    {
        if (reward.Merchant?.Def.Sprite?.Def.Clips.Idle?.Sprite.Def is { } towner && Fit(ui, art, towner, 0, 1, box, Rgb.White, 1f)) return true;
        if (reward.Icon is { } icon && Fit(ui, art, icon, box, 1f)) return true;
        var draw = reward.Object?.Def.Draw ?? (reward.Kind == RewardKind.HealNextRoom ? defs.Pickups.All.FirstOrDefault(p => p.Kind == PickupKind.PotionStack)?.Draw : null);
        if (draw?.Sprite?.Def is { } sprite)
        {
            // 대체 반복 그림(exe PNG 96×160 금화·물약)은 마지막 프레임 = 놓인 모습
            var frame = draw.Frame ?? sprite.Frames;
            return Fit(ui, art, sprite, 0, frame, box, draw.Tint ?? Rgb.White, 1f);
        }
        return false;
    }

    /// <summary>
    /// 보상 물체 둘레 아이콘(기획 11.4): 호 가운데 <paramref name="anchor"/>(가상 좌표 — 물체 발 자리 위 <see cref="RewardOsd.ArcLift"/>), 아이콘마다 <see cref="RewardOsd.Offset"/> + 흔들림.
    /// <paramref name="labels"/> 면 아이콘 아래 짧은 글자(주인공 300px 안). 원작 그림이 없거나 코드 그림 종류(카드·?·단련)는 일반 모양으로 그린다.
    /// </summary>
    public static void Osd(UiCanvas ui, ArtServices art, IReadOnlyList<OsdIcon> icons, Vector2 anchor, int stageTick, bool labels, float alpha)
    {
        if (icons.Count == 0 || alpha <= 0f) return;
        var bob = RewardOsd.Bob(stageTick);
        var half = RewardOsd.IconSize * 0.5f;
        for (var i = 0; i < icons.Count; i++)
        {
            var icon = icons[i];
            var c = anchor + RewardOsd.Offset(icons.Count, i) + new Vector2(0f, bob);
            var box = new RectF(c.X - half, c.Y - half, c.X + half, c.Y + half);
            var a = alpha * (icon.Dim ? 0.4f : 1f);
            ui.Fill(Grow(box, 1f), Frame, 0.55f * a);
            if (icon.Kind == OsdIconKind.Evolution) Border(ui, Grow(box, 2f), Gold, a);
            var drawn = icon.Icon is { } def && Fit(ui, art, def, Grow(box, -1f), a);
            if (!drawn) Code(ui, icon.Kind, box, a);
            if (!labels || icon.Label.Length == 0) continue;
            // 글자는 호 바깥쪽으로(위 아이콘은 위, 옆 아이콘은 옆) — 아이콘 아래에 두면 이웃 아이콘·글자와 겹쳤다
            var m = ui.MeasureText(icon.Label, 10f);
            var dir = RewardOsd.Offset(icons.Count, i);
            var color = icon.Dim ? new Rgb(170, 170, 170) : Rgb.White;
            Vector2 at;
            if (MathF.Abs(dir.X) < RewardOsd.ArcRadius * 0.3f) at = new Vector2(c.X - m.X * 0.5f, box.Y0 - m.Y - 1f);
            else if (dir.X < 0f) at = new Vector2(box.X0 - m.X - 3f, c.Y - m.Y * 0.5f);
            else at = new Vector2(box.X1 + 3f, c.Y - m.Y * 0.5f);
            ui.Fill(new RectF(at.X - 2f, at.Y, at.X + m.X + 2f, at.Y + m.Y), Frame, 0.5f * a);
            ui.Text(icon.Label, at, 10f, color, a);
        }
    }

    /// <summary>코드 그림(원작 그림이 없는 종류 — 상표·남의 그림을 본뜨지 않는 일반 모양).</summary>
    private static void Code(UiCanvas ui, OsdIconKind kind, RectF box, float a)
    {
        var c = box.Center;
        switch (kind)
        {
            case OsdIconKind.Card:
                // 무기 카드 실루엣: 세로 카드 + 위 주황 띠
                var card = new RectF(c.X - 7f, c.Y - 10f, c.X + 7f, c.Y + 10f);
                ui.Fill(card, CardBody, a);
                ui.Fill(new RectF(card.X0, card.Y0, card.X1, card.Y0 + 5f), CardBand, a);
                Border(ui, card, new Rgb(150, 120, 90), a);
                break;
            case OsdIconKind.Temper:
                // 단련: 둥근 바탕 + 위로 향한 화살
                ui.Disc(c, 9f, new Rgb(90, 80, 70), a);
                ui.Fill(new RectF(c.X - 1.5f, c.Y - 5f, c.X + 1.5f, c.Y + 6f), Gold, a);
                ui.Fill(new RectF(c.X - 5f, c.Y - 3f, c.X + 5f, c.Y - 1f), Gold, a);
                break;
            case OsdIconKind.Potion:
                ui.Disc(c + new Vector2(0f, 2f), 7f, new Rgb(200, 30, 30), a);
                ui.Fill(new RectF(c.X - 2f, c.Y - 9f, c.X + 2f, c.Y - 4f), new Rgb(160, 140, 120), a);
                break;
            case OsdIconKind.Gold:
                ui.Disc(c, 8f, Gold, a);
                ui.Disc(c, 5f, new Rgb(180, 140, 50), a);
                break;
            default:
                // ? (아직 정하지 않음·정예 상자)
                ui.Disc(c, 9f, new Rgb(70, 60, 50), a);
                var m = ui.MeasureText("?", 15f);
                ui.Text("?", new Vector2(c.X - m.X * 0.5f, c.Y - m.Y * 0.55f), 15f, Gold, a);
                break;
        }
    }

    private static void Border(UiCanvas ui, RectF r, Rgb color, float a)
    {
        ui.Fill(new RectF(r.X0, r.Y0, r.X1, r.Y0 + 1f), color, a);
        ui.Fill(new RectF(r.X0, r.Y1 - 1f, r.X1, r.Y1), color, a);
        ui.Fill(new RectF(r.X0, r.Y0, r.X0 + 1f, r.Y1), color, a);
        ui.Fill(new RectF(r.X1 - 1f, r.Y0, r.X1, r.Y1), color, a);
    }

    private static RectF Grow(RectF r, float d) => new(r.X0 - d, r.Y0 - d, r.X1 + d, r.Y1 + d);
}
