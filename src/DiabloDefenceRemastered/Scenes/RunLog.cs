using System.Globalization;
using DiabloDefenceRemastered.Core.Sim.Autoplay;

namespace DiabloDefenceRemastered.Scenes;

/// <summary><c>ROOM</c> 줄에 <c>RoomRecord</c> 밖에서 더하는 값(단계 2 wp6): 들어올 때 최대 체력(<c>hp_in</c> 비율), 그 방 처치·얻은 금화.</summary>
public readonly record struct RoomLogExtra(float MaxHpIn, int Kills, int Gold);

/// <summary>
/// 판정용 고정 로그 문장 중 화면 쪽(WP6)이 쓰는 것 (설계서 9절): <c>SCENE</c>·<c>RESULT</c>·<c>ROOM</c>·<c>INNER_ROOMS</c>·<c>DOOR_SKIP</c>·<c>REWARD_STALL</c>·<c>VISIT</c>·<c>VISIT_SKIP</c>·<c>VISITS</c>.
/// 문장 모양만 여기서 만든다(순수 함수) — 언제 쓰는지는 <c>PlayScene</c>·<c>ResultScene</c>.
/// 숫자는 고정 문화(점 소수, 쉼표 없음)로 쓴다 — 로그를 읽는 판정 스크립트가 PC 지역 설정에 흔들리지 않게.
/// </summary>
public static class RunLog
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static string Seconds(int ticks) => (ticks / (float)World.TicksPerSecond).ToString("0.##", Inv);

    /// <summary><c>SCENE &lt;이름&gt; app_tick=…</c>.</summary>
    public static string Scene(string name, AppTick tick) => $"SCENE {name} app_tick={tick}";

    /// <summary>
    /// <c>RESULT start=&lt;시작 액트 ID&gt; hero=… outcome=victory|defeat depth=&lt;액트&gt;:&lt;순번&gt; run_time=… level=… kills=… gold_spent=… seed=…</c>(v0.7 — 단계 2 wp6: <c>gold_spent</c> = <c>RunStats.GoldSpent</c>).
    /// </summary>
    public static string Result(RunResult r, ulong seed, int goldSpent = 0) =>
        string.Create(Inv, $"RESULT start={r.StartActId} hero={r.HeroClassId} outcome={(r.Victory ? "victory" : "defeat")} depth={r.Reached} run_time={r.RunTime:0.##} level={r.Level} kills={r.Kills} gold_spent={goldSpent} seed={seed}");

    /// <summary>
    /// <c>ROOM act=&lt;n&gt; room=&lt;n&gt; map=&lt;원판 id&gt; mirror=&lt;0|1&gt; gate_in=&lt;id&gt; reward=&lt;id|-&gt; door=&lt;id|-&gt; clear_tick=&lt;틱&gt; cleanup_s=&lt;초&gt; collapsed=&lt;0|1&gt; boss_s=&lt;초|-&gt; picks_left=&lt;n|-&gt; level=&lt;n&gt;</c>(v0.8)
    /// + 단계 2 wp6(기획 15절 v0.9·v0.9.1): <c>hp_in=&lt;0..1|-&gt; kills=&lt;n&gt; gold=&lt;n&gt; gold_spent=&lt;n&gt; shop=&lt;상인 ID|-&gt; potions=&lt;n&gt; drunk=&lt;n&gt;</c>.
    /// <c>hp_in</c> = <c>RoomRecord.HpIn</c> ÷ 들어올 때 최대 체력(<paramref name="extra"/> — 모르면 <c>-</c>), <c>kills</c>·<c>gold</c> = 그 방 동안 <c>RunStats.Kills</c>·<c>Gold</c>(얻은 금화)의 차이
    /// (<c>RoomRecord</c> 에 없는 두 값 — <c>PlayScene</c> 이 방마다 센다, 13장 WP6 단계 2 기록의 WP1h 요청), 나머지는 <c>RoomRecord</c> 그대로.
    /// </summary>
    public static string Room(in RoomRecord r, in RoomLogExtra extra = default) =>
        string.Create(Inv,
            $"ROOM act={r.Act} room={r.Number} map={r.MapId} mirror={(r.Mirrored ? 1 : 0)} gate_in={r.GateIn} reward={r.RewardId ?? "-"} door={r.Door ?? "-"} " +
            $"clear_tick={r.ClearTick} cleanup_s={Seconds(r.CleanupTicks)} collapsed={(r.Collapsed ? 1 : 0)} " +
            $"boss_s={(r.BossClockTicks >= 0 ? Seconds(r.BossClockTicks) : "-")} picks_left={(r.PicksLeft >= 0 ? r.PicksLeft.ToString(Inv) : "-")} level={r.Level} " +
            $"hp_in={(r.HpIn >= 0f && extra.MaxHpIn > 0f ? Math.Clamp(r.HpIn / extra.MaxHpIn, 0f, 1f).ToString("0.##", Inv) : "-")} kills={extra.Kills} gold={extra.Gold} " +
            $"gold_spent={r.GoldSpent} shop={r.Shop ?? "-"} potions={r.Potions} drunk={r.Drunk}");

    /// <summary>
    /// 방이 끝날 때 안쪽 방 기준점마다 한 줄(v0.7.1): <c>INNER_ROOMS act=&lt;n&gt; room=&lt;n&gt; map=&lt;id&gt; anchor=&lt;기준점 ID&gt; doors=… inside_s=… damage=… supplies=…</c>.
    /// 안쪽 방이 없는 맵이면 빈 목록. <c>World.InnerRoomPresence</c> 를 아직 채우지 않은 월드(배열 길이가 방 수보다 짧음)는 0 으로 쓴다.
    /// </summary>
    public static IEnumerable<string> InnerRooms(World world)
    {
        if (world.Room is not { } room) yield break;
        var rooms = world.InnerRooms.Rooms;
        var p = world.InnerRoomPresence;
        for (var i = 0; i < rooms.Count; i++)
        {
            var r = rooms[i];
            var ticks = i < p.TicksInside.Length ? p.TicksInside[i] : 0;
            var damage = i < p.DamageInside.Length ? p.DamageInside[i] : 0f;
            var supplies = i < p.SuppliesTaken.Length ? p.SuppliesTaken[i] : 0;
            yield return string.Create(Inv,
                $"INNER_ROOMS act={room.Plan.Act} room={room.Plan.Number} map={room.Plan.MapId} anchor={r.Id} doors={r.Doors} inside_s={Seconds(ticks)} damage={damage:0.#} supplies={supplies}");
        }
    }

    /// <summary><c>DOOR_SKIP act=&lt;n&gt; room=&lt;n&gt; stage_tick=&lt;틱&gt;</c>.</summary>
    public static string DoorSkip(in DoorSkip s) => string.Create(Inv, $"DOOR_SKIP act={s.Act} room={s.Room} stage_tick={s.StageTick}");

    /// <summary><c>REWARD_STALL act=&lt;n&gt; room=&lt;n&gt; phase=&lt;단계&gt; stage_tick=&lt;틱&gt;</c>.</summary>
    public static string RewardStall(in RewardStall s) =>
        string.Create(Inv, $"REWARD_STALL act={s.Act} room={s.Room} phase={s.Phase.ToString().ToLowerInvariant()} stage_tick={s.StageTick}");

    /// <summary><c>VISIT map=… room=… stage_tick=…</c> 또는 <c>VISIT_SKIP …</c>(v0.7 — <c>map=</c>).</summary>
    public static string Visit(in RoomVisit v) =>
        string.Create(Inv, $"{(v.Skipped ? "VISIT_SKIP" : "VISIT")} map={v.MapId} room={v.RoomId} stage_tick={v.StageTick}");

    /// <summary>판 끝 <c>VISITS map=… room=… visits=… skips=… inside_s=… damage=…</c>.</summary>
    public static string Visits(in RoomVisitStats s) =>
        string.Create(Inv, $"VISITS map={s.MapId} room={s.RoomId} visits={s.Visits} skips={s.Skips} inside_s={Seconds(s.TicksInside)} damage={s.DamageInside:0.#}");
}
