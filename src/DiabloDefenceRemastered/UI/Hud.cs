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
    public int Gold { get; init; }
    public int Belt { get; init; }
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
            Gold = world.RunStats.Gold,
            Belt = hero.Belt,
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
/// </summary>
public sealed class Hud(AppServices app) : IWorldBound
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private World? _world;
    private bool _warnedBossBars;

    public Minimap Minimap { get; } = new(app);

    public void Attach(World world)
    {
        _world = world;
        _warnedBossBars = false;
    }

    /// <summary>이번 앱 틱의 값(그리지 않고 경고만 — 그래픽 없는 실행에서도 로그 경고가 같게).</summary>
    public HudModel Update(RunState? run)
    {
        if (_world is not { } world) throw new InvalidOperationException("Hud.Attach 전");
        var model = HudModel.From(world, run);
        if (model.BossBarsOverflow && !_warnedBossBars)
        {
            _warnedBossBars = true;
            app.Log.Warn($"HUD 보스 막대 {world.BossBars.Count}줄 — 최대 {HudModel.MaxBossBars}줄만 그림(8.2)");
        }
        return model;
    }

    public void Draw(UiCanvas c, RunState? run)
    {
        if (_world is null) return;
        var m = Update(run);

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

        // 오른쪽 위 처치·금화, 그 아래 미니맵
        Ui.Right(c, $"처치 {RunText.Number(m.Kills)}", Theme.Width - 8, 30, Theme.BodyText, Theme.Text);
        Ui.Right(c, $"금화 {RunText.Number(m.Gold)}", Theme.Width - 8, 50, Theme.BodyText, Theme.Gold);
        Minimap.Draw(c, new Vector2(Theme.Width - 8, 72));

        // 왼쪽 아래 체력 구슬(코드 그림 — 원·돌 테 텍스처는 WP5 UiCanvas 에 원 채우기가 생기면 바꿈) + 허리띠 둘
        var orb = new RectF(12, Theme.Height - 76, 76, Theme.Height - 12);
        c.Fill(orb, Theme.Grey, 0.8f);
        var inner = Shrink(orb, 3);
        var ratio = m.MaxHp > 0 ? Math.Clamp(m.Hp / m.MaxHp, 0f, 1f) : 0f;
        c.Fill(inner, Theme.Black, 0.9f);
        c.Fill(new RectF(inner.X0, inner.Y1 - inner.Height * ratio, inner.X1, inner.Y1), Theme.Red, 0.95f);
        c.Text(string.Create(Inv, $"{MathF.Ceiling(m.Hp):0} / {MathF.Ceiling(m.MaxHp):0}"), new Vector2(84, Theme.Height - 30), Theme.BodyText, Theme.Text);
        var beltSlots = app.Defs.Rules.Hero?.Belt?.Slots ?? 2;
        for (var i = 0; i < beltSlots; i++)
        {
            var r = new RectF(84 + i * 28, Theme.Height - 76, 108 + i * 28, Theme.Height - 52);
            c.Fill(r, Theme.Panel, 0.9f);
            Ui.Outline(c, r, Theme.PanelEdge);
            if (i < m.Belt) c.Fill(Shrink(r, 6), Theme.Red, 0.95f);
        }

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

    private void DrawSlot(UiCanvas c, RectF box, IconDef? icon, int level, int maxLevel, bool evolved)
    {
        c.Fill(box, Theme.Panel, 0.85f);
        Ui.Outline(c, box, evolved ? Theme.Gold : Theme.PanelEdge);
        if (level <= 0) return;
        Ui.Icon(c, app.Defs, icon, Shrink(box, 2));
        var max = maxLevel > 0 && level >= maxLevel;
        Ui.Right(c, max ? "MAX" : level.ToString(Inv), box.X1 - 1, box.Y1 - 13, Theme.SmallText, max ? Theme.Gold : Theme.Text);
    }

    private static RectF Shrink(RectF r, float d) => new(r.X0 + d, r.Y0 + d, r.X1 - d, r.Y1 - d);
}
