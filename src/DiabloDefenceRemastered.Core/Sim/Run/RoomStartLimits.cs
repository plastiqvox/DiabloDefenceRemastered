namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// <c>--start-at</c> 상한(v0.8, 설계서 3.6 결정 5 끝 — 계약·구현 WP1f, v0.7.1 9절 규칙을 Core 한 곳으로). 옵션 파싱(WP7)이 이것으로 64 를 내고,
/// <c>RunSession</c> 생성자도 같은 함수로 <see cref="ArgumentOutOfRangeException"/>(앱을 거치지 않는 시험·시나리오의 잘못을 조용히 넘기지 않게).
/// </summary>
public static class RoomStartLimits
{
    /// <summary>
    /// 그 방 방 시계 시작의 상한(틱, <b>그 미만만</b> 된다): 일반 방 = 생존 길이(t 끝 − t 시작), 정예 방 = <c>elite_spawn_cap</c>, 보스 방 = null(상한 없음).
    /// </summary>
    public static int? MaxStartClockTicks(RoomDef room, RoomRulesDef rules)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(rules);
        return room.Kind switch
        {
            RoomKind.Normal => room.To is { } to ? MathX.SecondsToTicks(to.Seconds - room.From.Seconds, World.Dt) : null,
            RoomKind.Elite => MathX.SecondsToTicks(rules.EliteSpawnCap.Seconds, World.Dt),
            _ => null,
        };
    }

    /// <summary>방 시계 시작이 상한 안인가(상한 없음이면 0 이상이면 참).</summary>
    public static bool Allows(RoomDef room, RoomRulesDef rules, int startClockTicks) =>
        startClockTicks >= 0 && (MaxStartClockTicks(room, rules) is not { } max || startClockTicks < max);
}
