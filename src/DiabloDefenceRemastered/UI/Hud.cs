using System.Globalization;
using DiabloDefenceRemastered.Rendering;

namespace DiabloDefenceRemastered.UI;

/// <summary>보스 막대 한 줄(8.2): 큰 막대(480×10) 또는 작은 막대(320×6).</summary>
public readonly record struct BossBarLine(string Name, float Ratio, bool Big);

/// <summary>방 줄 칸 하나(8.2): 지난 방(보상 아이콘), 지금 방(밝은 테), 앞 방(종류 표시).</summary>
public readonly record struct RoomStripCell(int Number, RoomKind Kind, bool Past, bool Current, string? RewardId);

/// <summary>
/// HUD 가 그리는 값 (설계서 8.2, 기획 12.1). 그림과 나눈 순수 계산이라 그래픽 없이 시험한다.
/// 알림 문구는 <c>World.Banner</c> 가 유일한 출처다(감독 WP4 가 씀).
/// </summary>
public sealed record HudModel
{
    public int Level { get; init; }
    public float XpRatio { get; init; }
    public float Hp { get; init; }
    public float MaxHp { get; init; }
    public int Kills { get; init; }
    /// <summary>지갑(v0.9 — <c>Hero.Gold</c>, 상인에게 쓰는 금화. 얻은 금화 누적 <c>RunStats.Gold</c> 가 아니다 — 기획 12.1).</summary>
    public int Gold { get; init; }
    /// <summary>물약 칸의 물약 수(v0.9.1 — v0.8 의 허리띠 수. 물약 칸 그리기·<c>2/3</c> 는 WP6).</summary>
    public int Potions { get; init; }
    /// <summary>물약 칸 수(<c>rules.hero.potions</c> 의 지금 칸 — 규칙이 없으면 0).</summary>
    public int PotionSlots { get; init; }
    /// <summary>마시는 중이면 회복이 끝났을 때의 체력 비율(구슬 위 차오름 띠 — 기획 12.1), 아니면 −1.</summary>
    public float HealTarget { get; init; } = -1f;
    public int WeaponSlotsOpen { get; init; }
    public int PassiveSlotsOpen { get; init; }
    /// <summary><c>대성당 4 / 9</c>(방 없는 월드는 null).</summary>
    public string? RoomLine { get; init; }
    /// <summary>방 상태 줄(생존 남은 시간·<c>남은 적 n</c>·<c>문을 고르라</c>).</summary>
    public string? RoomStatus { get; init; }
    public IReadOnlyList<RoomStripCell> RoomStrip { get; init; } = [];
    public IReadOnlyList<BossBarLine> BossBars { get; init; } = [];
    /// <summary><c>World.BossBars</c> 가 4줄을 넘음(8.2 — 로그 경고 한 번).</summary>
    public bool BossBarsOverflow { get; init; }
    public string? Banner { get; init; }

    /// <summary>보스 막대 최대 줄 수(중간 보스 3 + 최종 보스, 8.2).</summary>
    public const int MaxBossBars = 4;

    public static HudModel From(World world, RunState? run)
    {
        var hero = world.Hero;
        var prog = world.Progression;
        var live = 0;
        foreach (var e in world.Enemies.Items)
            if (!e.Removed && !e.Dead) live++;

        var bars = BossLines(world, out var overflow);
        var banner = world.Banner.IsActive(world.Tick) ? world.Banner.Text : null;

        string? roomLine = null;
        IReadOnlyList<RoomStripCell> strip = [];
        if (world.Room is { } room && run is not null)
        {
            roomLine = RunText.RoomLine(run.Act.Act, room.Plan.Number);
            strip = Strip(run);
        }

        return new HudModel
        {
            Level = prog?.Level ?? 1,
            XpRatio = prog is { XpToNext: > 0 } p ? p.Xp / (float)p.XpToNext : 0f,
            Hp = hero.Hp,
            MaxHp = hero.MaxHp,
            Kills = world.RunStats.Kills,
            Gold = hero.Gold,
            Potions = hero.Potions,
            PotionSlots = world.Defs.Rules.Hero?.Potions?.Capacity(hero.PotionSlotsLevel) ?? 0,
            HealTarget = hero.Heal.Active && hero.MaxHp > 0f ? Math.Clamp((hero.Hp + hero.Heal.Remaining) / hero.MaxHp, 0f, 1f) : -1f,
            WeaponSlotsOpen = Math.Clamp(hero.WeaponSlotsOpen, 0, WeaponSlots.SlotCount),
            PassiveSlotsOpen = Math.Clamp(hero.PassiveSlotsOpen, 0, PassiveSlots.SlotCount),
            RoomLine = roomLine,
            RoomStatus = RunText.RoomStatus(world, live),
            RoomStrip = strip,
            BossBars = bars,
            BossBarsOverflow = overflow,
            Banner = banner,
        };
    }

    /// <summary>
    /// 보스 막대(8.2 v0.5.3): <c>Final</c> 인 줄이 맨 위 큰 막대, 나머지는 등장 순서로 작은 막대. <c>Final</c> 이 없으면 첫 줄이 큰 막대.
    /// 4줄을 넘으면 <c>Final</c> 과 먼저 나온 중간 보스 3줄만. 체력은 그 적에서 읽는다(없어진 적은 건너뜀).
    /// </summary>
    public static IReadOnlyList<BossBarLine> BossLines(World world, out bool overflow)
    {
        var list = world.BossBars;
        overflow = list.Count > MaxBossBars;
        var lines = new List<BossBarLine>(Math.Min(list.Count, MaxBossBars));
        var finalIndex = -1;
        for (var i = 0; i < list.Count; i++)
            if (list[i].Final) { finalIndex = i; break; }
        var bigIndex = finalIndex >= 0 ? finalIndex : 0;
        if (list.Count > 0) AddLine(bigIndex, true);
        for (var i = 0; i < list.Count && lines.Count < MaxBossBars; i++)
            if (i != bigIndex && !(finalIndex >= 0 && list[i].Final)) AddLine(i, false);
        return lines;

        void AddLine(int i, bool big)
        {
            var bar = list[i];
            if (world.Enemies.Get(bar.Id) is not { } e) return;
            lines.Add(new BossBarLine(bar.Name, e.MaxHp > 0f ? Math.Clamp(e.Hp / e.MaxHp, 0f, 1f) : 0f, big));
        }
    }

    /// <summary>방 줄(8.2): 지금 액트 <c>rooms</c> 원소마다 — 지난 방 = 그 방 기록의 보상, 지금 방 = 밝은 테, 앞 방 = 종류.</summary>
    public static IReadOnlyList<RoomStripCell> Strip(RunState run)
    {
        var act = run.Act;
        var cells = new RoomStripCell[act.Rooms.Count];
        for (var i = 0; i < act.Rooms.Count; i++)
        {
            var plan = act.Rooms[i];
            string? reward = null;
            if (i < run.RoomIndex)
                foreach (var r in run.Rooms)
                    if (r.Act == act.ActNumber && r.Number == plan.Number) reward = r.RewardId;
            cells[i] = new RoomStripCell(plan.Number, plan.Kind, i < run.RoomIndex, i == run.RoomIndex, reward);
        }
        return cells;
    }
}

/// <summary>
/// HUD (설계서 8.2, 기획 12.1). <see cref="IWorldBound"/> — 방마다 그 월드에 다시 붙는다(7.11, <c>PlayScene</c> 이 <c>WorldRenderer → FxSystem → Hud → 미니맵 → 소리</c> 순서로).
/// 그리기 값은 <see cref="HudModel"/>, 그리기는 <c>UiCanvas</c>(WP5).
/// 단계 2 wp6(기획 12.1 V9·V12): 왼쪽 아래 원작 체력 구슬(<c>ui_panel8</c> 의 구슬을 둥글게 오려 청동 테 — 잃은 비율만큼 위에서 어둡게)·물약 칸 하나(<c>2/3</c>·키 <c>Q</c>),
/// 왼쪽 위 장비 줄 여섯 칸, 오른쪽 위 지갑 금화(늘면 0.3s 금색). 그림이 없으면 코드 그림. 물약 칸 오른쪽 <see cref="TeleportReserve"/> 는 다음 묶음 순간이동 충전 칸 자리(지금은 비움).
/// </summary>
public sealed class Hud(AppServices app) : IWorldBound
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private World? _world;
    private bool _warnedBossBars;

    public Minimap Minimap { get; } = new(app);

    /// <summary>물약 칸·금화 번쩍임 길이(스테이지 틱, 0.3s).</summary>
    public const int BeltFlashTicks = 18;

    private int _beltFlashTick = int.MinValue / 2;
    private int _goldFlashTick = int.MinValue / 2;
    private int _lastGold;

    // ── 왼쪽 아래 배치(가상 px) ──────────────────────
    /// <summary>체력 구슬 자리(지름 72 — 기획 12.1 V12, 원본 88×88 을 줄임).</summary>
    public static readonly RectF OrbBox = new(8, 446, 80, 518);
    /// <summary>물약 칸(28×28 — 구슬 오른쪽 아래에 맞춤).</summary>
    public static readonly RectF PotionBox = new(88, 490, 116, 518);
    /// <summary>
    /// 다음 묶음 "보스 탄막·순간이동" 의 순간이동 충전 칸 자리(기획 v0.9.4 12.1 V21 — 물약 칸 옆, 작은 원 2~4개 · 22px 간격). 지금은 아무것도 그리지 않는다.
    /// </summary>
    public static readonly RectF TeleportReserve = new(122, 494, 214, 518);
    /// <summary>원작 조작판 칸 좌표: 체력 구슬 (96,0,88,88), 허리띠 칸 0 (205,21,28,28) — 가져오기 표 주석의 자리.</summary>
    public static readonly RectI OrbCell = new(96, 0, 184, 88);
    public static readonly RectI BeltCell = new(205, 21, 233, 49);
    /// <summary>
    /// 구슬 액체가 차는 줄(칸 좌표 행): 가득 = <see cref="OrbLiquidTop"/>, 빔 = <see cref="OrbLiquidBottom"/>. 원작은 빈 구슬을 위에서 (85 − 80 × 비율) 행까지 덮는다
    /// (DevilutionX 참고 — 조작판 구슬 높이 80). 그 사이를 비율대로 나눈다.
    /// </summary>
    public const int OrbLiquidTop = 5;
    public const int OrbLiquidBottom = 85;
    public void Attach(World world)
    {
        _world = world;
        _warnedBossBars = false;
        _beltFlashTick = int.MinValue / 2;
        _goldFlashTick = int.MinValue / 2;
        _lastGold = world.Hero.Gold;
    }

    /// <summary>물약을 마심(v0.9.1 <c>CueKind.PotionDrink</c> — v0.8 의 허리띠 저절로 마심을 바꿈. <c>PlayScene</c> 이 표시 이벤트를 소비할 때 알림). 그 신호의 스테이지 틱부터 번쩍임.</summary>
    public void OnPotionDrink(int stageTick) => _beltFlashTick = stageTick;

    /// <summary>물약 칸이 번쩍이는 중인가(스테이지 틱 — 멈추면 같이 멈춤).</summary>
    public bool BeltFlashing => _world is { } w && w.Tick - _beltFlashTick < BeltFlashTicks;

    /// <summary>지갑 금화가 늘어 번쩍이는 중인가(기획 12.1 — 0.3s 금색).</summary>
    public bool GoldFlashing => _world is { } w && w.Tick - _goldFlashTick < BeltFlashTicks;

    /// <summary>미리보기(<c>--preview ui</c>)가 금화 번쩍임을 켠다.</summary>
    public void FlashGold() { if (_world is { } w) _goldFlashTick = w.Tick; }

    /// <summary>
    /// 앱 틱마다(<c>PlayScene</c>): 지갑이 늘었으면 금화 번쩍임, 보스 막대가 4줄을 넘으면 로그 경고 한 번(8.2) — 그리지 않는 실행에서도 같게 틱에서 본다.
    /// </summary>
    public void Tick()
    {
        if (_world is not { } world) return;
        var gold = world.Hero.Gold;
        if (gold > _lastGold) _goldFlashTick = world.Tick;
        _lastGold = gold;
        if (_warnedBossBars || world.BossBars.Count <= HudModel.MaxBossBars) return;
        _warnedBossBars = true;
        app.Log.Warn($"HUD 보스 막대 {world.BossBars.Count}줄 — 최대 {HudModel.MaxBossBars}줄만 그림(8.2)");
    }

    public void Draw(UiCanvas c, RenderContext rc, RunState? run)
    {
        if (_world is null) return;
        var m = HudModel.From(_world, run);

        // 맨 위 경험치 막대 + Lv
        Ui.Bar(c, new RectF(0, 0, Theme.Width, 8), m.XpRatio, Theme.Blue, Theme.Panel, 0.9f);
        Ui.Right(c, string.Create(Inv, $"Lv {m.Level}"), Theme.Width - 6, 10, Theme.BodyText, Theme.Text);

        // 왼쪽 위 무기·패시브 칸
        var hero = _world.Hero;
        for (var i = 0; i < m.WeaponSlotsOpen; i++)
        {
            var box = new RectF(8 + i * 36, 14, 40 + i * 36, 46);
            var slot = hero.Weapons[i];
            DrawSlot(c, box, slot.Def?.Icon, slot.IsEmpty ? 0 : slot.Level, slot.Def?.MaxLevel ?? 0, slot.Evolved);
        }
        for (var i = 0; i < m.PassiveSlotsOpen; i++)
        {
            var box = new RectF(8 + i * 36, 50, 40 + i * 36, 82);
            var slot = hero.Passives[i];
            DrawSlot(c, box, slot.Def?.Icon, slot.IsEmpty ? 0 : slot.Level, slot.Def?.MaxLevel ?? 0, false);
        }
        // 그 아래 장비 줄(v0.9 — 칸 여섯 20×20: 투구·갑옷·방패·지팡이·반지·반지, 마법은 파란 테, 빈 칸은 어두운 실루엣. 보기만)
        DrawEquipmentRow(c, app, hero.Equipment, 8, 86, 20, 24);

        // 위 가운데 방 줄·방 상태
        var y = 12f;
        if (m.RoomLine is { } line)
        {
            Ui.Centered(c, line, Theme.Width / 2, y, Theme.BodyText, Theme.Text);
            y += 20;
            var n = m.RoomStrip.Count;
            var x0 = Theme.Width / 2 - n * 8;
            for (var i = 0; i < n; i++)
            {
                var cell = m.RoomStrip[i];
                var r = new RectF(x0 + i * 16 + 2, y, x0 + i * 16 + 14, y + 12);
                if (cell.Past) c.Fill(r, cell.RewardId is { } id && app.Defs.Rewards.TryGet(id, out var rd) ? Minimap.RewardColor(rd) : Theme.Grey, 0.9f);
                else c.Fill(Shrink(r, cell.Kind == RoomKind.Normal ? 3 : 1), cell.Kind == RoomKind.Normal ? Theme.Grey : Theme.Red, 0.9f);
                if (cell.Current) Ui.Outline(c, r, Theme.Focus);
            }
            y += 16;
        }
        if (m.RoomStatus is { } status)
        {
            Ui.Centered(c, status, Theme.Width / 2, y, 20f, Theme.Text);
            y += 26;
        }

        // 보스 막대
        foreach (var bar in m.BossBars)
        {
            var w = bar.Big ? 480f : 320f;
            var h = bar.Big ? 10f : 6f;
            Ui.Centered(c, bar.Name, Theme.Width / 2, y, Theme.SmallText, Theme.Gold);
            y += 14;
            Ui.Bar(c, new RectF(Theme.Width / 2 - w / 2, y, Theme.Width / 2 + w / 2, y + h), bar.Ratio, Theme.Red, Theme.DarkRed);
            y += h + 6;
        }

        // 오른쪽 위 처치·지갑 금화(GoldIcon + 숫자, 늘면 0.3s 금색 — 기획 12.1), 그 아래 미니맵
        Ui.Right(c, $"처치 {RunText.Number(m.Kills)}", Theme.Width - 8, 30, Theme.BodyText, Theme.Text);
        var goldText = RunText.Number(m.Gold);
        var goldWidth = c.MeasureText(goldText, Theme.BodyText).X;
        Ui.Right(c, goldText, Theme.Width - 8, 50, Theme.BodyText, GoldFlashing ? Theme.Gold : Theme.Text);
        ItemArt.Gold(c, app, new RectF(Theme.Width - 30 - goldWidth, 50, Theme.Width - 12 - goldWidth, 68));
        Minimap.Draw(c, rc, new Vector2(Theme.Width - 8, 72));

        // 왼쪽 아래 체력 구슬 + 물약 칸
        var ratio = m.MaxHp > 0 ? Math.Clamp(m.Hp / m.MaxHp, 0f, 1f) : 0f;
        DrawOrb(c, app, ratio, m.HealTarget, _world.Tick);
        Ui.Centered(c, string.Create(Inv, $"{MathF.Ceiling(m.Hp):0} / {MathF.Ceiling(m.MaxHp):0}"), OrbBox.Center.X, OrbBox.Y1 + 2, Theme.SmallText, Theme.Text);
        DrawPotionSlot(c, app, m.Potions, m.PotionSlots, m.HealTarget >= 0f, BeltFlashing, _world.Tick);

        // 체력 30% 아래 붉은 가장자리(1초 주기 — 앱 틱이 아니라 스테이지 틱으로, 멈추면 같이 멈춤)
        if (ratio < 0.3f && m.MaxHp > 0)
        {
            var pulse = 0.25f + 0.15f * MathF.Sin(_world.Tick * MathF.Tau / World.TicksPerSecond);
            const float edge = 18f;
            c.Fill(new RectF(0, 0, Theme.Width, edge), Theme.Red, pulse);
            c.Fill(new RectF(0, Theme.Height - edge, Theme.Width, Theme.Height), Theme.Red, pulse);
            c.Fill(new RectF(0, 0, edge, Theme.Height), Theme.Red, pulse);
            c.Fill(new RectF(Theme.Width - edge, 0, Theme.Width, Theme.Height), Theme.Red, pulse);
        }

        // 알림 문구(가운데 위)
        if (m.Banner is { } banner) Ui.Centered(c, banner, Theme.Width / 2, 150, 26f, Theme.Gold);
    }

    /// <summary>구슬 칸 행(0..88) — 체력 비율 <paramref name="ratio"/> 일 때 빈 구슬이 덮는 아래 끝.</summary>
    public static int OrbLine(float ratio) =>
        (int)MathF.Round(OrbLiquidBottom - (OrbLiquidBottom - OrbLiquidTop) * Math.Clamp(ratio, 0f, 1f));

    /// <summary>
    /// 체력 구슬(기획 12.1 V12): 원작 조작판의 가득 찬 구슬을 원 밖은 투명하게 오린 것(<see cref="OrbArt"/>)에 청동 테를 둘러 판 조각 없이 둥글게 그리고,
    /// <b>위에서</b> 잃은 비율만큼은 같은 구슬을 아주 어둡게(빈 유리) 그린다. 원작 빈 구슬 그림(<c>ui_p8bulbs</c>)은 회색 돌 무늬·판 윗줄·바둑판 머리가 함께 있어
    /// 판에서 떼어 둔 둥근 구슬에서는 어색해(사용자 관찰) 쓰지 않는다.
    /// 마시는 중이면 지금 수위에서 회복이 끝날 수위까지 가득 찬 구슬을 옅게(맥동) 다시 비춰 차오름 띠. 그림이 없으면 코드 구슬.
    /// </summary>
    public static void DrawOrb(UiCanvas c, AppServices app, float ratio, float healTarget, int stageTick)
    {
        var box = OrbBox;
        var k = box.Height / OrbCell.Height;
        if (OrbArt.TryGet(app, OrbCell, out var full, out var ms))
        {
            // 둥글게 오린 구슬(판의 돌 무늬·모서리 없이) 둘레에 그림자 + 청동 테 — 판에서 떼어 HUD 에 단독으로 둔 구슬이라 테로 마감한다
            var center = new Vector2(box.X0 + OrbArt.Center.X * k, box.Y0 + OrbArt.Center.Y * k);
            var r = OrbArt.Radius * k;
            c.Disc(center + new Vector2(0f, 2f), r + 5f, Theme.Black, 0.45f);
            c.Disc(center, r + 3f, OrbRim);
            c.Disc(center, r + 1.5f, OrbRimDark);
            var line = OrbLine(ratio);
            c.Texture(full, new RectI(0, line * ms, OrbCell.Width * ms, OrbCell.Height * ms), new RectF(box.X0, box.Y0 + line * k, box.X1, box.Y1), Rgb.White);
            if (line > 0)
                c.Texture(full, new RectI(0, 0, OrbCell.Width * ms, line * ms), new RectF(box.X0, box.Y0, box.X1, box.Y0 + line * k), OrbEmpty);
            // 수면: 원 안 너비만큼 밝은 붉은 줄 하나(빈 유리와 액체의 경계를 또렷하게)
            var surface = box.Y0 + line * k;
            var dy = surface - center.Y;
            if (ratio is > 0f and < 1f && MathF.Abs(dy) < r - 1f)
            {
                var half = MathF.Sqrt(r * r - dy * dy) - 1f;
                c.Fill(new RectF(center.X - half, surface - 0.5f, center.X + half, surface + 0.5f), Theme.Red, 0.7f);
            }
            if (healTarget >= 0f)
            {
                var target = OrbLine(healTarget);
                if (target < line)
                {
                    var pulse = 0.35f + 0.15f * MathF.Sin(stageTick * MathF.Tau / World.TicksPerSecond * 2f);
                    c.Texture(full, new RectI(0, target * ms, OrbCell.Width * ms, line * ms),
                              new RectF(box.X0, box.Y0 + target * k, box.X1, box.Y0 + line * k), Rgb.White, pulse);
                }
            }
            return;
        }

        // 대체: 코드 구슬(붉은 액체 + 돌 테 — v0.9 의 둥근 구슬)
        var orb = box.Center;
        const float radius = OrbRadius;
        c.Disc(orb, radius, OrbStone);
        c.Disc(orb, radius - 2f, OrbStoneDark);
        c.Disc(orb, radius - 4f, OrbGlass, 0.95f);
        if (healTarget > ratio) c.Disc(orb, radius - 4f, Theme.Red, 0.3f, healTarget);
        c.Disc(orb, radius - 4f, Theme.DarkRed, 1f, ratio);
        var liquid = radius - 4f;
        var core = liquid - 3f;
        c.Disc(orb, core, Theme.Red, 0.9f, (core - liquid + 2f * liquid * ratio) / (2f * core));
        c.Disc(orb + new Vector2(-9f, -11f), 6f, Theme.White, 0.16f);
    }

    /// <summary>
    /// 물약 칸 하나(기획 12.1 V9): 원작 허리띠 칸 그림(없으면 코드 칸) + 물약 그림(비면 흐리게) + 왼쪽 위 키 <c>Q</c> + 오른쪽 아래 <c>2/3</c>(가득이면 금색).
    /// 마시는 중이면 칸 테가 은은히 맥동, 마신 순간 번쩍.
    /// </summary>
    public static void DrawPotionSlot(UiCanvas c, AppServices app, int potions, int slots, bool drinking, bool flash, int stageTick)
    {
        var r = PotionBox;
        if (!ItemArt.Has(app, "ui_panel8") || !ItemArt.Crop(c, app, "ui_panel8", 1, BeltCell, r, Rgb.White, 1f))
        {
            c.Fill(r, Theme.Panel, 0.9f);
            Ui.Outline(c, r, Theme.PanelEdge);
        }
        ItemArt.Potion(c, app, Shrink(r, 3), potions > 0 ? 1f : 0.3f);
        if (drinking)
        {
            var pulse = 0.4f + 0.3f * MathF.Sin(stageTick * MathF.Tau / World.TicksPerSecond * 2f);
            Ui.Outline(c, Shrink(r, -1), Theme.Red, pulse, 2f);
        }
        if (flash) c.Fill(r, Theme.White, 0.5f);
        c.Fill(new RectF(r.X0, r.Y0, r.X0 + 9, r.Y0 + 11), Theme.Black, 0.6f);
        c.Text("Q", new Vector2(r.X0 + 1, r.Y0), 10f, Theme.Text);
        var count = string.Create(Inv, $"{potions}/{slots}");
        var size = c.MeasureText(count, 11f);
        var at = new Vector2(r.X1 - size.X - 1, r.Y1 - size.Y);
        c.Fill(new RectF(at.X - 1, at.Y, r.X1, r.Y1), Theme.Black, 0.6f);
        c.Text(count, at, 11f, slots > 0 && potions >= slots ? Theme.Gold : potions > 0 ? Theme.Text : Theme.TextDim);
    }

    /// <summary>장비 줄(HUD·결과 — 기획 12.1·12.4): 칸 <paramref name="size"/> px, <paramref name="step"/> 간격으로 여섯. 마법 = 파란 테, 빈 칸 = 어두운 실루엣.</summary>
    public static void DrawEquipmentRow(UiCanvas c, AppServices app, IReadOnlyList<EquipItem?> equipment, float x, float y, float size, float step)
    {
        for (var i = 0; i < equipment.Count && i < 6; i++)
        {
            var box = new RectF(x + i * step, y, x + i * step + size, y + size);
            var item = equipment[i];
            c.Fill(box, Theme.Panel, 0.85f);
            if (item is null)
            {
                Ui.Outline(c, box, Theme.PanelEdge, 0.6f);
                ItemArt.Silhouette(c, ItemArt.KindOf((EquipSlot)i), Shrink(box, 2), Theme.PanelEdge, 0.45f);
                continue;
            }
            Ui.Outline(c, box, item.Magic ? ShopWindow.MagicText : Theme.PanelEdge);
            ItemArt.Item(c, app, item, Shrink(box, 1), 1f, item.Magic ? ShopWindow.MagicText : Theme.Text);
        }
    }

    private void DrawSlot(UiCanvas c, RectF box, IconDef? icon, int level, int maxLevel, bool evolved)
    {
        c.Fill(box, Theme.Panel, 0.85f);
        Ui.Outline(c, box, evolved ? Theme.Gold : Theme.PanelEdge);
        if (level <= 0) return;
        Ui.Icon(c, app.Defs, icon, Shrink(box, 2));
        var max = maxLevel > 0 && level >= maxLevel;
        Ui.Right(c, max ? "MAX" : level.ToString(Inv), box.X1 - 1, box.Y1 - 13, Theme.SmallText, max ? Theme.Gold : Theme.Text);
    }

    /// <summary>코드 구슬(대체) 반지름(가상 px — 지름 72, 기획 12.1 V12)과 돌 테·유리 색.</summary>
    public const float OrbRadius = 36f;
    private static readonly Rgb OrbStone = new(104, 94, 80);
    private static readonly Rgb OrbStoneDark = new(46, 40, 34);
    private static readonly Rgb OrbGlass = new(14, 8, 8);
    /// <summary>원작 구슬 둘레 테(청동 + 안쪽 어두운 줄).</summary>
    private static readonly Rgb OrbRim = new(118, 94, 56);
    private static readonly Rgb OrbRimDark = new(28, 20, 12);
    /// <summary>빈 부분 = 같은 구슬에 곱하는 어두운 색(빈 유리).</summary>
    private static readonly Rgb OrbEmpty = new(46, 36, 40);

    private static RectF Shrink(RectF r, float d) => new(r.X0 + d, r.Y0 + d, r.X1 - d, r.Y1 - d);
}
