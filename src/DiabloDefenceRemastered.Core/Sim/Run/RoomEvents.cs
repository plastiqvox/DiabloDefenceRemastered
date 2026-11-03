namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>방 하나의 이벤트 일정: 방 시계 순 이벤트와 지금 바로 적용할 상태형 이벤트(<c>light_change</c>, 시간 순).</summary>
public sealed record RoomSchedule(IReadOnlyList<ScheduledEvent> Events, IReadOnlyList<PartDef> Replay);

/// <summary>방 시계 틱에 일어날 이벤트 하나. 같은 틱이면 정의 순서.</summary>
public readonly record struct ScheduledEvent(int ClockTick, PartDef Event);

/// <summary>
/// 액트 이벤트를 방 시계로 옮기는 순수 함수 (v0.7, 설계서 3.6 결정 5 — 구현·시험 WP1e, 부르는 곳 감독 WP4·<c>World</c> 생성자).
/// <list type="bullet">
/// <item>이벤트 시각 <c>PartDef.At</c> 은 t 다. 방 [t 시작, t 끝)(보스 방은 t 시작 이상)에 드는 이벤트만 그 방 것이고, 방 시계 = max(t − t 시작, <c>grace</c>)(기획 13.4).</item>
/// <item>t &lt; 방 t 시작인 <c>light_change</c> 는 <see cref="RoomSchedule.Replay"/>(시간 순). 그 밖의 지난 이벤트는 버린다.</item>
/// <item><c>startClockTicks</c>(<c>--start-at</c>)보다 이른 방 이벤트는 <c>light_change</c> 면 Replay 뒤에 붙이고 나머지는 버린다.</item>
/// <item>방 범위 뒤의 이벤트는 버린다. <c>At</c> 이 없는 이벤트(스테이지 이벤트가 아님)는 건너뛴다.</item>
/// </list>
/// 생존이 끝난 뒤 남은 이벤트를 버리는 것은 감독 상태다(이 함수 밖).
/// </summary>
public static class RoomEvents
{
    /// <summary>상태형 이벤트(지나간 것도 다시 적용하는 것)의 부품 이름.</summary>
    public const string LightChange = "light_change";

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
}
