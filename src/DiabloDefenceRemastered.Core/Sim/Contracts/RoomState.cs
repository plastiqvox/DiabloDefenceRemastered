namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 방 흐름 단계 (v0.7, 사용자 결정 R — 설계서 3.6 결정 3). 들어옴 → 생존 → 정리 → 보상 → 문 → 나감. 전이는 <c>RoomFlowSystem</c>(16단계) 한 곳.
/// 보스 방은 생존 → 보상(정리 없음), 판의 마지막 방은 정리 끝(또는 보스 처치)이 승리라 보상 이후로 가지 않는다.
/// </summary>
public enum RoomPhase : byte { Enter, Survive, Cleanup, Reward, Doors, Exit }

/// <summary>
/// 열린 출구 문 하나(3.6 "문 열기"). 다음 방이 일반 방이면 <paramref name="Reward"/> 가 있고, 정예·보스 방이면 보상 없이 <paramref name="NextKind"/>·<paramref name="NextBoss"/>
/// (문 위 <c>정예</c>·보스 이름), 액트 보스 방이면 <paramref name="Stairs"/>·<paramref name="NextActName"/>(<c>지하묘지로</c>).
/// </summary>
public readonly record struct RoomExit(string GateId, RewardDef? Reward, RoomKind NextKind, EnemyDef? NextBoss, bool Stairs, string? NextActName);

/// <summary>
/// 방 월드 하나를 만드는 입력(3.6 결정 4). <c>World(defs, RoomStart)</c> 가 씨앗(<c>Plan.RoomSeed</c>)·액트(<c>Run.Act.Act</c>)·직업(<c>Run.HeroClass</c>)을 여기서 읽는다.
/// </summary>
/// <param name="Reward">이 방을 정리하면 받는 보상(액트 첫 방 = <c>first_room</c>, 정예·보스 방 = null, 그 밖 = 앞 방에서 고른 문의 보상).</param>
/// <param name="AreaScale">넓이 배율(감독이 동시 최대에 곱함, 3.6).</param>
/// <param name="IsLastInRun">판의 마지막 방(정리 끝·보스 처치가 승리).</param>
/// <param name="StartClockTicks">방 시계 시작(<c>--start-at</c>, 판의 첫 방만).</param>
public sealed record RoomStart(RunState Run, RoomPlan Plan, RewardDef? Reward, float AreaScale, bool IsLastInRun, int StartClockTicks = 0);

/// <summary>
/// 방 하나의 실행 상태 (v0.7, 설계서 3.6 — <c>World.Room</c>). 쓰기는 생성자와 <c>RoomFlowSystem</c> 뿐이다(setter 는 internal — 시험 도우미 <c>WithRoom</c> 이 단계를 손으로 정할 때도 같은 길).
/// 읽기: WP3(<c>RoomCleared</c>·보상), WP4(감독 — <see cref="T"/>·<see cref="SpawningOpen"/>·<see cref="RemapEntry"/>·<see cref="ClockTicks"/>, <c>AutoplayPolicy</c>),
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
    }

    public RoomPlan Plan { get; }
    /// <summary>이 방을 정리하면 받는 보상(정예·보스 방은 null — 그 방 정예 상자가 보상).</summary>
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

    /// <summary>정리 단계 길이(틱).</summary>
    public int CleanupTicks { get; internal set; }
    /// <summary>정리 안전장치가 돌았음(남은 적을 무너뜨림).</summary>
    public bool Collapsed { get; internal set; }
    /// <summary>정리가 끝나 보상 단계에 들어온 <c>World.Tick</c>(로그 <c>ROOM clear_tick</c>). 아직이면 −1.</summary>
    public int ClearTick { get; internal set; } = -1;

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
    /// 이벤트의 <c>entry:</c> 바꾸기(v0.7.1 — 서로 바꾸기): 들어온 문 → 맞은편 문(<c>GateZones.Opposite</c>), 맞은편 → 들어온 문, 나머지 그대로.
    /// 이벤트 부품이 시작 문(<c>at_entry</c>)과 목표 문(<c>toward_entry</c>)에 둘 다 쓴다 — 한쪽만 바꾸면 가로지르는 행진이 "같은 문에서 같은 문으로" 가 된다.
    /// </summary>
    public string RemapEntry(string entryId)
    {
        if (_entryOpposite is null) return entryId;
        if (string.Equals(entryId, Plan.GateIn, StringComparison.Ordinal)) return _entryOpposite;
        if (string.Equals(entryId, _entryOpposite, StringComparison.Ordinal)) return Plan.GateIn;
        return entryId;
    }

    internal void EnterPhase(RoomPhase phase, int tick)
    {
        Phase = phase;
        PhaseTick = tick;
    }
}
