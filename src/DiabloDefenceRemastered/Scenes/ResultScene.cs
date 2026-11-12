using DiabloDefenceRemastered.UI;

namespace DiabloDefenceRemastered.Scenes;

/// <summary>무기 칸 하나의 결과(기획 12.4 무기별 피해 표).</summary>
public readonly record struct WeaponDamageLine(WeaponDef Weapon, float Damage, float PerSecond);

/// <summary>
/// 결과 화면이 읽는 것 (설계서 8.1 — v0.7.1: <c>RunSession.Conclude()</c> 뒤의 <see cref="RunState"/> 만). 저장 결과(<see cref="RunResult"/>)·앞 기록·방 줄·무기별 피해.
/// 단계 2 wp6(기획 12.4 v0.9): <paramref name="GoldSpent"/> = <c>RunStats.GoldSpent</c>(<c>쓴 금화</c>·<c>RESULT gold_spent</c>), <paramref name="Equipment"/> = 판 끝 장비 여섯 칸.
/// </summary>
public sealed record ResultSummary(RunResult Result, ulong Seed, StageDef StartAct, HeroClassDef? Hero, StageDef ReachedAct,
                                   IReadOnlyList<RoomRecord> Rooms, IReadOnlyList<WeaponDamageLine> Weapons, RunRecord? Previous,
                                   int GoldSpent = 0, IReadOnlyList<EquipItem?>? Equipment = null)
{
    /// <summary>
    /// 판 끝 정산 뒤 값으로 결과를 만든다(8.3): <c>HeroClassId = World.HeroClass.Id</c>(없으면 <c>rules.hero.class</c>), 깊이 = <c>RunState.Depth</c>·그 액트,
    /// 걸린 시간 = <c>ElapsedTicks</c> 초, 레벨 = 마지막 방의 성장 모듈(없으면 짐), 처치·금화 = <c>RunState.Stats</c>.
    /// 앞 기록(<c>BestRecord</c>)은 <c>RecordResult</c> 전에 읽는다(<c>새 기록</c> 표시).
    /// </summary>
    public static ResultSummary From(GameFlow flow, RunState run, World lastWorld)
    {
        var hero = lastWorld.HeroClass ?? run.HeroClass ?? flow.DefaultHero();
        var heroId = hero?.Id ?? "";
        var victory = lastWorld.Outcome == RunOutcome.Victory;
        var seconds = run.ElapsedTicks / (float)World.TicksPerSecond;
        var level = lastWorld.Progression?.Level ?? run.Carry?.Level ?? 1;
        var result = new RunResult(run.StartAct.Id, heroId, victory, run.Depth, run.Act.Act.Id, seconds, level, run.Stats.Kills, run.Stats.Gold);

        var weapons = new List<WeaponDamageLine>();
        var damage = run.Stats.DamageByWeapon;
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
        {
            var slot = lastWorld.Hero.Weapons[i];
            if (slot.Def is not { } def) continue;
            var d = i < damage.Length ? damage[i] : 0f;
            weapons.Add(new WeaponDamageLine(def, d, seconds > 0f ? d / seconds : 0f));
        }
        var previous = heroId.Length > 0 ? flow.App.Save.BestRecord(run.StartAct.Id, heroId) : null;
        return new ResultSummary(result, run.Seed, run.StartAct, hero, run.Act.Act, [.. run.Rooms], weapons, previous,
                                 run.Stats.GoldSpent, [.. lastWorld.Hero.Equipment]);
    }

    public bool NewRecord => RunText.IsNewRecord(Previous, Result);
}

/// <summary>
/// 결과 (설계서 8.1, 기획 12.4). 들어올 때 <c>RESULT</c> 로그 한 줄 → <c>RecordResult</c> → 음악 <c>title</c> → 화면 신호 <c>result</c>(<c>--capture-on result</c>·<c>--exit-on-result</c>).
/// 메뉴 <c>다시 하기</c>(같은 시작 액트·직업, 새 씨앗) / <c>타이틀로</c>. 자동 조작은 메뉴 대기 뒤 다시 하기.
/// 보스 묶음 B: 열린 뒤 <c>rules.level_up.input_lock</c>(0.4s) 동안 메뉴 입력을 버린다 — 판 중 순간이동 키 <c>Space</c> 가 <c>Confirm</c> 도 겸해서,
/// 연타하다 죽으면 결과 화면이 열리자마자 <c>다시 하기</c> 로 넘어가 결과를 못 본다(레벨업 창과 같은 잠금).
/// </summary>
public sealed class ResultScene(GameFlow flow, ResultSummary summary) : IScene
{
    private readonly MenuList _menu = new(Theme.Width / 2, 456);
    private SceneStack? _stack;
    private long _openTick;

    public bool PausesBelow => false;
    public bool DrawsBelow => false;
    public ResultSummary Summary => summary;
    public MenuList Menu => _menu;

    private int LockTicks => (int)MathF.Round((flow.Defs.Rules.LevelUp?.InputLock ?? 0.4f) * World.TicksPerSecond);

    /// <summary>열린 뒤 메뉴 입력 잠금이 아직 걸려 있나(앱 틱으로 셈).</summary>
    public bool InputLocked => flow.Now.Value - _openTick < LockTicks;

    public void Enter(SceneStack stack)
    {
        _stack = stack;
        _openTick = flow.Now.Value;
        _menu.SetItems([new MenuItem("다시 하기"), new MenuItem("타이틀로")]);
        flow.LogScene("Result");
        flow.Log.Info(RunLog.Result(summary.Result, summary.Seed, summary.GoldSpent));
        if (summary.Result.HeroClassId.Length > 0) flow.App.Save.RecordResult(summary.Result);
        flow.App.Audio?.PlayMusic("title");
        flow.Signal("result");
    }

    public void Exit() { }

    public void FixedTick(in AppTick t, InputFrame input)
    {
        flow.Now = t;
        if (InputLocked) return;
        var pick = _menu.Update(input, flow.MouseVirtual(input), out var moved);
        if (moved) flow.App.Audio?.PlayUi("menu_move");
        if (pick >= 0) Choose(pick);
    }

    public void AutoStep(IAutoplayDriver bot)
    {
        if (!InputLocked && bot.Ready(this)) Choose(0);
    }

    public void Choose(int index)
    {
        flow.App.Audio?.PlayUi("menu_select");
        if (index == 0) _stack?.Replace(TitleScene.StartRun(flow, summary.Hero));
        else _stack?.Replace(new TitleScene(flow));
    }

    public void Draw(RenderContext rc, float alpha)
    {
        if (flow.Canvas is not { } c) return;
        var r = summary.Result;
        c.Begin(rc);
        c.Fill(Ui.Screen, Theme.Black, 1f);
        Ui.Centered(c, r.Victory ? "승리" : "쓰러졌다", Theme.Width / 2, 40, 44f, r.Victory ? Theme.Gold : Theme.Red);
        if (summary.NewRecord) Ui.Centered(c, "새 기록", Theme.Width / 2, 92, Theme.BodyText, Theme.Gold);

        var y = 120f;
        void Row(string label, string value)
        {
            c.Text(label, new Vector2(240, y), Theme.BodyText, Theme.TextDim);
            c.Text(value, new Vector2(400, y), Theme.BodyText, Theme.Text);
            y += 24;
        }
        Row("도달한 곳", RunText.Reached(summary.ReachedAct, r.Reached, r.Victory));
        Row("걸린 시간", RunText.ClockSeconds(r.RunTime));
        Row("레벨", r.Level.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Row("처치", RunText.Number(r.Kills));
        Row("금화", $"얻은 금화 {RunText.Number(r.Gold)} · 쓴 금화 {RunText.Number(summary.GoldSpent)}");
        // 장비 여섯 칸(기획 12.4 v0.9 — HUD 장비 줄과 같은 그림)
        c.Text("장비", new Vector2(240, y), Theme.BodyText, Theme.TextDim);
        Hud.DrawEquipmentRow(c, flow.App, summary.Equipment ?? new EquipItem?[6], 400, y, 20, 24);
        y += 24;

        // 방 줄 — 방마다 받은 보상 색
        var x = 240f;
        foreach (var room in summary.Rooms)
        {
            var color = room.RewardId is { } id && flow.Defs.Rewards.TryGet(id, out var rd) ? Minimap.RewardColor(rd) : Theme.Grey;
            c.Fill(new RectF(x, y + 4, x + 12, y + 16), color, 0.9f);
            x += 16;
        }
        y += 30;

        // 무기별 피해
        foreach (var w in summary.Weapons)
        {
            Ui.Icon(c, flow.Defs, w.Weapon.Icon, new RectF(240, y, 264, y + 24));
            c.Text(w.Weapon.Name, new Vector2(272, y + 4), Theme.SmallText, Theme.Text);
            Ui.Right(c, RunText.Number((long)w.Damage), 560, y + 4, Theme.SmallText, Theme.Text);
            Ui.Right(c, $"{RunText.Number((long)w.PerSecond)}/초", 680, y + 4, Theme.SmallText, Theme.TextDim);
            y += 25;
        }

        _menu.Draw(c);
        c.End();
    }
}
