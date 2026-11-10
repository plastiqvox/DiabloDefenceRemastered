namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 방 흐름 단계 (v0.7, 사용자 결정 R — 설계서 3.6 결정 3). 들어옴 → 생존 → 정리 → 보상 → 문 → 나감. 전이는 <c>RoomFlowSystem</c>(16단계) 한 곳.
/// 보스 방은 생존 → 보상(정리 없음), 판의 마지막 방은 정리 끝(또는 보스 처치)이 승리라 보상 이후로 가지 않는다.
/// </summary>
public enum RoomPhase : byte { Enter, Survive, Cleanup, Reward, Doors, Exit }

/// <summary>
/// 열린 출구 문 하나(3.6 "문 열기"). 다음 방이 액트 안에 있으면 종류와 상관없이 <paramref name="Reward"/> 가 있고(v0.8 — 정예·보스 방 앞 문은 덤 보상),
/// 다음 방이 정예·보스 방이면 <paramref name="NextKind"/>·<paramref name="NextBoss"/>(문 위 <c>정예</c>·보스 이름과 덤 아이콘), 액트 보스 방이면 보상 없이 <paramref name="Stairs"/>·<paramref name="NextActName"/>(<c>지하묘지로</c>).
/// </summary>
/// <param name="NextFloor">v0.8: 다음 방 층(<c>RoomDef.Floor</c>)이 이 방과 다르면 그 층(문 위 <c>▼ 2층</c>·넘김 어둠 <c>transition_floor</c> — 계단 문도), 같거나 층을 안 적었으면 0.</param>
public readonly record struct RoomExit(string GateId, RewardDef? Reward, RoomKind NextKind, EnemyDef? NextBoss, bool Stairs, string? NextActName,
                                       int NextFloor = 0);

/// <summary>
/// 방 월드 하나를 만드는 입력(3.6 결정 4). <c>World(defs, RoomStart)</c> 가 씨앗(<c>Plan.RoomSeed</c>)·액트(<c>Run.Act.Act</c>)·직업(<c>Run.HeroClass</c>)을 여기서 읽는다.
/// </summary>
/// <param name="Reward">이 방을 정리하면 받는 보상(액트 첫 방 = <c>first_room</c>, 그 밖 = 앞 방에서 고른 문의 보상 — v0.8: 정예·보스 방도 덤 보상으로 같음).</param>
/// <param name="AreaScale">넓이 배율(감독이 동시 최대에 곱함, 3.6).</param>
/// <param name="IsLastInRun">판의 마지막 방(정리 끝·보스 처치가 승리).</param>
/// <param name="StartClockTicks">방 시계 시작(<c>--start-at</c>, 판의 첫 방만 — 상한 <c>RoomStartLimits</c>).</param>
public sealed record RoomStart(RunState Run, RoomPlan Plan, RewardDef? Reward, float AreaScale, bool IsLastInRun, int StartClockTicks = 0);

/// <summary>
/// 방 하나의 실행 상태 (v0.7, 설계서 3.6 — <c>World.Room</c>). 쓰기는 생성자와 <c>RoomFlowSystem</c> 뿐이다(setter 는 internal — 시험 도우미 <c>WithRoom</c> 이 단계를 손으로 정할 때도 같은 길).
/// 읽기: WP3(<c>RoomCleared</c>·보상·<see cref="TimedEffectsRun"/>), WP4(감독 — <see cref="T"/>·<see cref="SpawningOpen"/>·<see cref="RemapEntry"/>·<see cref="RemapMarch"/>·<see cref="ClockTicks"/>, <c>AutoplayPolicy</c>),
/// WP5(봉인·아이콘), WP6(HUD·<c>StageStep</c>), WP7(해시·<c>ROOM</c> 로그).
/// </summary>
public sealed class RoomState
{
    private readonly int _graceTicks;
    private readonly int _eliteCapTicks;
    private readonly string? _entryOpposite;

    internal RoomState(RoomStart start, RoomRulesDef rules)
    {
        Plan = start.Plan;
        Reward = start.Reward;
        IsLastInRun = start.IsLastInRun;
        AreaScale = start.AreaScale;
        ClockTicks = Math.Max(0, start.StartClockTicks);
        _graceTicks = MathX.SecondsToTicks(rules.Grace, World.Dt);
        _eliteCapTicks = MathX.SecondsToTicks(rules.EliteSpawnCap.Seconds, World.Dt);
        CleanupLimitTicks = MathX.SecondsToTicks(rules.CleanupLimit, World.Dt);
        var def = Plan.Def;
        SurviveTicks = def.Kind == RoomKind.Normal && def.To is { } to ? MathX.SecondsToTicks(to.Seconds - def.From.Seconds, World.Dt) : 0;
        _entryOpposite = GateZones.Opposite(Plan.Map, Plan.GateIn);
        HealOnEnter = Reward?.Kind == RewardKind.HealNextRoom;
    }

    public RoomPlan Plan { get; }
    /// <summary>이 방을 정리하면 받는 보상. v0.8: 정예·보스 방은 덤 보상(그 방으로 들어온 문의 보상) — 정예 상자와 함께 보상 자리 줄에 놓인다.</summary>
    public RewardDef? Reward { get; }
    public bool IsLastInRun { get; }
    public float AreaScale { get; }

    /// <summary>지금 단계.</summary>
    public RoomPhase Phase { get; internal set; }
    /// <summary>이 단계에 들어온 <c>World.Tick</c>.</summary>
    public int PhaseTick { get; internal set; }
    /// <summary>방 시계(틱). 들어옴·생존·정리 동안 <c>RoomFlowSystem</c> 이 틱 끝에 1 늘린다 — 단계들은 "이번 틱의 방 시계" 를 읽는다.</summary>
    public int ClockTicks { get; internal set; }
    /// <summary>일반 방 생존 길이(t 끝 − t 시작, 틱). 정예·보스 방은 0(처치까지).</summary>
    public int SurviveTicks { get; }
    /// <summary>정리 안전장치 길이(틱, <c>rules.rooms.cleanup_limit</c>).</summary>
    public int CleanupLimitTicks { get; }

    /// <summary>
    /// 난이도 분 t(3.1). t 시작 + 방 시계 — 일반·정예 방은 t 끝 − 1틱에서 멈춘다(정리 단계는 그 값. 구간 [from, to) 를 끝까지 쓰고 다음 방 구간은 안 씀 —
    /// 정예 방 v0.7.1, 일반 방 기획 v0.8.1 2.5), 보스 방은 안 멈춘다.
    /// </summary>
    public float T
    {
        get
        {
            var def = Plan.Def;
            var seconds = def.From.Seconds + ClockTicks * World.Dt;
            if (def.To is { } to && def.Kind != RoomKind.Boss) seconds = MathF.Min(seconds, to.Seconds - World.Dt);
            return seconds / 60f;
        }
    }

    /// <summary>감독이 생성·이벤트를 하는가: 생존 단계이고 유예가 지났고, 정예 방이면 방 시계 &lt; <c>elite_spawn_cap</c>.</summary>
    public bool SpawningOpen =>
        Phase == RoomPhase.Survive && ClockTicks >= _graceTicks && (Plan.Kind != RoomKind.Elite || ClockTicks < _eliteCapTicks);

    /// <summary>
    /// 주인공 시간 효과(버프·마력 폭주 — WP3)가 흐르는가(v0.8, 3.6 결정 1 "시간 효과 시계"): 생존·정리 단계에서만. 보상·문 단계와 들어옴(유예)은 멈춤.
    /// <c>World.TimedEffectsRun</c> 이 감싼다(방 없는 월드는 늘 참).
    /// </summary>
    public bool TimedEffectsRun => Phase is RoomPhase.Survive or RoomPhase.Cleanup;

    /// <summary>정리 단계 길이(틱).</summary>
    public int CleanupTicks { get; internal set; }
    /// <summary>정리 안전장치가 돌았음(남은 적을 무너뜨림).</summary>
    public bool Collapsed { get; internal set; }
    /// <summary>정리가 끝나 보상 단계에 들어온 <c>World.Tick</c>(로그 <c>ROOM clear_tick</c>). 아직이면 −1.</summary>
    public int ClearTick { get; internal set; } = -1;
    /// <summary>
    /// 정예·보스 방에서 중간 보스·보스를 잡은 방 시계(v0.8 — 로그 <c>ROOM boss_s</c>, <c>RoomRecord.BossClockTicks</c>). 못 잡았거나 일반 방이면 −1.
    /// </summary>
    public int BossClockTicks { get; internal set; } = -1;
    /// <summary>
    /// 보상 자리(v0.8, 월드 px — 3.6 "보상 자리"). 보상 단계에 들어간 순간 <c>RewardSpot.Find</c> 로 정한다(그 전엔 주인공 시작 자리).
    /// <c>RoomCleared</c>·<c>DoorsOpened</c> 신호 자리, 보상 물체·정예 상자 줄의 가운데.
    /// </summary>
    public Vector2 RewardSpot { get; internal set; }

    /// <summary>
    /// 회복 문(v0.9 V1, 6.19 "회복 문")을 지나 들어온 방: 이 방의 보상(<see cref="Reward"/>, 덤 포함)이 <c>heal_next_room</c> 이면 참으로 시작하고,
    /// 첫 시뮬레이션 틱(넘김 어둠이 걷힌 뒤 — 들어옴 단계 첫 틱)에 <c>RoomFlow</c> 가 체력을 최대로 채우고 거짓으로 바꾼다(<c>CueKind.HealOnEnter</c>).
    /// 정리 뒤 보상 자리에는 아무것도 놓이지 않는다(물체 없음).
    /// </summary>
    public bool HealOnEnter { get; internal set; }

    /// <summary>
    /// 상인 방 상태(v0.9 V3·V11, 6.19 — 방 수명). 이 방의 보상이 <c>merchant</c> 면 들어오는 틱에 <c>RunSession</c> 이 <c>ShopStock.Create</c> 로 만든다(정리 전), 아니면 null.
    /// <c>RoomFlow</c>: <see cref="ShopState.Visited"/> 전에는 보상 단계가 끝나지 않고(문이 안 열림), <see cref="ShopState.Open"/> 이면 나가지 못한다.
    /// </summary>
    public ShopState? Shop { get; internal set; }

    /// <summary>열린 출구 문(<see cref="RoomPhase.Doors"/> 부터 — 그 전엔 빈 목록, 문은 모두 봉인). 순서 = <c>Plan.ExitGates</c>.</summary>
    public IReadOnlyList<RoomExit> Exits { get; internal set; } = [];
    /// <summary>고른 출구 문 ID(<see cref="RoomPhase.Exit"/> 에서).</summary>
    public string? ChosenExit { get; internal set; }
    /// <summary>
    /// 문 단계에서 주인공 발 칸이 열린 출구 문의 나가는 자리 밖에 한 번 섰음(v0.7.1). 보상을 집다 문 틈에 서 있던 주인공이 문 위 보상을 보기도 전에 나가지 않게(기획 "들어서면").
    /// </summary>
    public bool ExitArmed { get; internal set; }

    /// <summary>고른 출구(<see cref="ChosenExit"/>)의 문. 아직이면 null.</summary>
    public RoomExit? ChosenExitInfo
    {
        get
        {
            if (ChosenExit is null) return null;
            foreach (var e in Exits)
                if (string.Equals(e.GateId, ChosenExit, StringComparison.Ordinal)) return e;
            return null;
        }
    }

    /// <summary>
    /// 문 하나뿐인 이벤트(<c>miniboss</c>·<c>boss</c>·<c>at_entry</c> 만 있는 생성)의 <c>entry:</c> 바꾸기 — v0.8.1 <b>시작 문 기준 바꾸기</b>(3.6 결정 5 끝, 기획 v0.8.3 2.4):
    /// 거울판 방이면 먼저 짝 이름(<see cref="GateZones.MirrorGate"/>), 그다음 그 문이 <b>들어온 문이면</b> 맞은편 문(<see cref="GateZones.Opposite"/>), 아니면 그대로.
    /// 맞은편 문이 없는 맵이면 그대로(13.5-23 ⑤ 가 오류로 잡는다). v0.7.1 의 서로 바꾸기(맞은편 → 들어온 문)는 없앴다 — 맞은편 문의 보스가 주인공 등 뒤 문으로 옮겨졌다.
    /// </summary>
    public string RemapEntry(string entryId)
    {
        var id = Plan.Mirrored ? GateZones.MirrorGate(entryId) : entryId;
        return _entryOpposite is not null && string.Equals(id, Plan.GateIn, StringComparison.Ordinal) ? _entryOpposite : id;
    }

    /// <summary>
    /// 행진(시작 문 <paramref name="atEntry"/> → 목표 문 <paramref name="towardEntry"/>)의 바꾸기(v0.8.1): 거울판이면 둘 다 짝 이름을 먼저, 그다음
    /// 시작 문이 <b>들어온 문이면</b> 시작 → 맞은편, 목표는 들어온 문 ↔ 맞은편만 맞바꾼다(나머지 두 문 그대로 — 시작한 문으로 다시 가지 않게).
    /// 시작 문이 들어온 문이 아니면 시작·목표 모두 그대로(목표가 들어온 문이어도 — 주인공이 들어온 쪽으로 가로지를 뿐 등 뒤에서 쏟아지지 않는다).
    /// </summary>
    public (string At, string Toward) RemapMarch(string atEntry, string towardEntry)
    {
        var at = Plan.Mirrored ? GateZones.MirrorGate(atEntry) : atEntry;
        var toward = Plan.Mirrored ? GateZones.MirrorGate(towardEntry) : towardEntry;
        if (_entryOpposite is null || !string.Equals(at, Plan.GateIn, StringComparison.Ordinal)) return (at, toward);
        if (string.Equals(toward, Plan.GateIn, StringComparison.Ordinal)) toward = _entryOpposite;
        else if (string.Equals(toward, _entryOpposite, StringComparison.Ordinal)) toward = Plan.GateIn;
        return (_entryOpposite, toward);
    }

    internal void EnterPhase(RoomPhase phase, int tick)
    {
        Phase = phase;
        PhaseTick = tick;
    }
}
