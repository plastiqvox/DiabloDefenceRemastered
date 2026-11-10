namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>방 하나의 이벤트 일정: 방 시계 순 이벤트와 지금 바로 적용할 상태형 이벤트(<c>light_change</c>, 시간 순).</summary>
public sealed record RoomSchedule(IReadOnlyList<ScheduledEvent> Events, IReadOnlyList<PartDef> Replay);

/// <summary>방 시계 틱에 일어날 이벤트 하나. 같은 틱이면 정의 순서.</summary>
public readonly record struct ScheduledEvent(int ClockTick, PartDef Event);

/// <summary>
/// 액트 이벤트를 방 시계로 옮기는 순수 함수 (v0.7 → v0.8 WP1f, 설계서 3.6 결정 5 — 부르는 곳 감독 WP4·<c>World</c> 생성자, 검사기 등록 WP4).
/// <list type="bullet">
/// <item>이벤트 시각 <c>PartDef.At</c> 은 t 다. 방 [t 시작, t 끝)(보스 방은 t 시작 이상)에 드는 이벤트만 그 방 것이고, 방 시계 = max(t − t 시작, <c>grace</c>)(기획 13.4).</item>
/// <item>t &lt; 방 t 시작인 <c>light_change</c> 는 <see cref="RoomSchedule.Replay"/>(시간 순). 그 밖의 지난 이벤트는 버린다.</item>
/// <item><c>startClockTicks</c>(<c>--start-at</c>)보다 이른 방 이벤트는 <c>light_change</c> 면 Replay 뒤에 붙이고, <b>입장 이벤트(<c>miniboss</c>·<c>boss</c>)는 방 시계 <c>startClockTicks</c> 로 옮겨</b>
/// 바로 내보낸다(v0.8, 기획 v0.8.1 15절 — 정예·보스 방을 <c>--start-at</c> 으로 시작해도 생존이 끝날 수 있게). 나머지는 버린다.</item>
/// <item>방 범위 뒤의 이벤트는 버린다. <c>At</c> 이 없는 이벤트(스테이지 이벤트가 아님)는 건너뛴다.</item>
/// </list>
/// 생존이 끝난 뒤 남은 이벤트를 버리는 것은 감독 상태다(이 함수 밖). <c>entry:</c> 바꾸기는 이벤트 부품이 <c>RoomState.RemapEntry</c>/<c>RemapMarch</c> 로 한다.
/// </summary>
public static class RoomEvents
{
    /// <summary>상태형 이벤트(지나간 것도 다시 적용하는 것)의 부품 이름.</summary>
    public const string LightChange = "light_change";

    /// <summary>입장 이벤트 부품 이름 — <c>--start-at</c> 보다 이르면 버리지 않고 그 시각으로 옮긴다(v0.8).</summary>
    public static IReadOnlyList<string> EntranceKinds { get; } = ["miniboss", "boss"];

    /// <summary>강적 이벤트(v0.8, 13.5-23 ⑥ — 기획 "강적(정예 무리·해골 대장)·포위 고리"). 방 시계 <see cref="StrongEarliest"/> 전이면 오류.</summary>
    public static IReadOnlyList<string> StrongKinds { get; } = ["elite_pack", "escort", "ring"];

    /// <summary>절정 이벤트(v0.8, 13.5-23 ⑥ — 행진·포위·정예 무리·호위). 2번 방부터의 일반 방 생존 끝 <see cref="ClimaxWindow"/> 안에 하나도 없으면 경고.</summary>
    public static IReadOnlyList<string> ClimaxKinds { get; } = ["march", "ring", "elite_pack", "escort"];

    /// <summary>강적이 나올 수 있는 가장 이른 방 시계(0:20 — 기획 13.5-23 의 수, 데이터 아님).</summary>
    public static readonly ClockTime StrongEarliest = new(20f);

    /// <summary>일반 방 생존 끝에서 절정이 있어야 하는 창(0:45 — 기획 13.5-23 의 수).</summary>
    public static readonly ClockTime ClimaxWindow = new(45f);

    public static RoomSchedule Schedule(StageDef act, RoomPlan room, RoomRulesDef rules, int startClockTicks)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(rules);
        var from = room.Def.From.Seconds;
        float? to = room.Kind == RoomKind.Boss ? null : room.Def.To?.Seconds;
        var graceTicks = MathX.SecondsToTicks(rules.Grace, World.Dt);

        var past = new List<(float At, int Order, PartDef Event)>();
        var early = new List<(int Clock, int Order, PartDef Event)>();
        var events = new List<(int Clock, int Order, PartDef Event)>();
        for (var i = 0; i < act.Events.Count; i++)
        {
            var e = act.Events[i];
            if (e.At is not { } at) continue;
            var t = at.Seconds;
            var isState = string.Equals(e.Type, LightChange, StringComparison.Ordinal);
            if (t < from)
            {
                if (isState) past.Add((t, i, e));
                continue;
            }
            if (to is { } end && t >= end) continue;
            var clock = Math.Max(MathX.SecondsToTicks(t - from, World.Dt), graceTicks);
            if (clock < startClockTicks)
            {
                if (isState) early.Add((clock, i, e));
                else if (Contains(EntranceKinds, e.Type)) events.Add((startClockTicks, i, e));
                continue;
            }
            events.Add((clock, i, e));
        }

        past.Sort(static (a, b) => a.At != b.At ? a.At.CompareTo(b.At) : a.Order.CompareTo(b.Order));
        early.Sort(static (a, b) => a.Clock != b.Clock ? a.Clock.CompareTo(b.Clock) : a.Order.CompareTo(b.Order));
        events.Sort(static (a, b) => a.Clock != b.Clock ? a.Clock.CompareTo(b.Clock) : a.Order.CompareTo(b.Order));

        var replay = new List<PartDef>(past.Count + early.Count);
        foreach (var p in past) replay.Add(p.Event);
        foreach (var p in early) replay.Add(p.Event);
        var scheduled = new List<ScheduledEvent>(events.Count);
        foreach (var e in events) scheduled.Add(new ScheduledEvent(e.Clock, e.Event));
        return new RoomSchedule(scheduled, replay);
    }

    /// <summary>
    /// 방 이벤트 자리 검사(v0.8, 13.5-23 ⑥ — 계약·구현 WP1f, 검사기 등록 WP4). 이벤트를 <see cref="Schedule"/> 과 같은 규칙(시작 시각 0)으로 방 시계에 놓은 뒤
    /// ① <see cref="StrongKinds"/> 가 방 시계 <see cref="StrongEarliest"/> 전이면 <b>오류</b>(입장 이벤트 <c>miniboss</c>·<c>boss</c> 는 강적 종류가 아니라 통과),
    /// ② 2번 방부터의 일반 방에 생존 끝 <see cref="ClimaxWindow"/> 안의 <see cref="ClimaxKinds"/> 가 없으면 <b>경고</b>.
    /// 종류는 <c>PartDef.Type</c> 문자열로만 본다(매개변수 형을 몰라도 됨). 오류 위치: 파일은 비움(검사기가 액트 파일로 채움), 경로 <c>$.events[k]</c>·<c>$.rooms[i]</c>.
    /// </summary>
    public static IReadOnlyList<DataError> CheckPlacement(StageDef act, RoomRulesDef rules)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(rules);
        var graceTicks = MathX.SecondsToTicks(rules.Grace, World.Dt);
        var strongTicks = MathX.SecondsToTicks(StrongEarliest.Seconds, World.Dt);
        var windowTicks = MathX.SecondsToTicks(ClimaxWindow.Seconds, World.Dt);
        var errors = new List<DataError>();

        for (var r = 0; r < act.Rooms.Count; r++)
        {
            var room = act.Rooms[r];
            var from = room.From.Seconds;
            float? to = room.Kind == RoomKind.Boss ? null : room.To?.Seconds;
            var surviveTicks = room.Kind == RoomKind.Normal && to is { } end0 ? MathX.SecondsToTicks(end0 - from, World.Dt) : 0;
            var climax = false;
            for (var k = 0; k < act.Events.Count; k++)
            {
                var e = act.Events[k];
                if (e.At is not { } at) continue;
                var t = at.Seconds;
                if (t < from || (to is { } end && t >= end)) continue;
                var clock = Math.Max(MathX.SecondsToTicks(t - from, World.Dt), graceTicks);
                if (Contains(StrongKinds, e.Type) && clock < strongTicks)
                    errors.Add(new DataError("", $"$.events[{k}]",
                        $"액트 '{act.Id}' 방 {room.Number}: 강적 이벤트 {e.Type}(t {at}) 가 방 시계 {Clock(clock)} — {StrongEarliest} 전에는 둘 수 없다(13.5-23 ⑥)"));
                if (surviveTicks > 0 && Contains(ClimaxKinds, e.Type) && clock >= surviveTicks - windowTicks && clock < surviveTicks) climax = true;
            }
            if (room.Kind == RoomKind.Normal && room.Number >= 2 && surviveTicks > 0 && !climax)
                errors.Add(new DataError("", $"$.rooms[{r}]",
                    $"액트 '{act.Id}' 방 {room.Number}: 생존 끝 {ClimaxWindow} 안에 절정 이벤트({string.Join("·", ClimaxKinds)})가 없다(13.5-23 ⑥)", DataSeverity.Warning));
        }
        return errors;
    }

    private static bool Contains(IReadOnlyList<string> list, string type)
    {
        foreach (var s in list)
            if (string.Equals(s, type, StringComparison.Ordinal)) return true;
        return false;
    }

    private static string Clock(int ticks) => new ClockTime(ticks * World.Dt).ToString();
}
