using System.Globalization;

namespace DiabloDefenceRemastered.UI;

/// <summary>
/// 화면 문구 만들기 (기획 12.1·12.4·12.5 — 설계서 8.1·8.2). 숫자·시간·깊이 모양을 한 곳에서 정한다(HUD·결과·타이틀이 같은 모양).
/// 그림과 무관한 순수 함수라 그래픽 없이 돈다.
/// </summary>
public static class RunText
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary><c>1,234</c>.</summary>
    public static string Number(long n) => n.ToString("#,0", Inv);

    /// <summary>
    /// 틱 → <c>m:ss</c>(한 시간 넘으면 <c>h:mm:ss</c>). 남은 시간은 올림(<paramref name="ceil"/>) — 0:00 이 보이는 순간 끝나게.
    /// </summary>
    public static string Clock(int ticks, bool ceil = false)
    {
        var totalSeconds = ceil ? (Math.Max(0, ticks) + World.TicksPerSecond - 1) / World.TicksPerSecond : Math.Max(0, ticks) / World.TicksPerSecond;
        var h = totalSeconds / 3600;
        var m = totalSeconds / 60 % 60;
        var s = totalSeconds % 60;
        return h > 0 ? string.Create(Inv, $"{h}:{m:00}:{s:00}") : string.Create(Inv, $"{m}:{s:00}");
    }

    /// <summary>초 → <c>m:ss</c>(결과·기록 걸린 시간).</summary>
    public static string ClockSeconds(float seconds) => Clock((int)MathF.Floor(Math.Max(0f, seconds) * World.TicksPerSecond));

    /// <summary>그 액트의 마지막 방 번호(HUD 분모, v0.7.1 — <c>slice</c> 판은 8).</summary>
    public static int LastRoomNumber(StageDef act) => act.Rooms.Count > 0 ? act.Rooms[^1].Number : 0;

    /// <summary>액트 이름(없으면 ID).</summary>
    public static string ActName(StageDef act) => act.Name.Length > 0 ? act.Name : act.Id;

    /// <summary><c>대성당 4 / 9</c>(방 줄·결과·타이틀 기록, 8.2).</summary>
    public static string RoomLine(StageDef act, int roomNumber) =>
        string.Create(Inv, $"{ActName(act)} {roomNumber} / {LastRoomNumber(act)}");

    /// <summary>
    /// 도달한 곳(결과·타이틀 기록 — 기획 12.4): 승리면 마지막 방 보스 이름 + <c>처치</c>(보스 방이 없는 단계 1 판은 방 줄 + <c> 정리</c>), 아니면 방 줄.
    /// <paramref name="act"/> 를 못 찾으면(방 표가 바뀐 옛 기록) 숫자만.
    /// </summary>
    public static string Reached(StageDef? act, RunDepth depth, bool victory)
    {
        if (act is null) return depth.ToString();
        if (victory)
        {
            var last = act.Rooms.Count > 0 ? act.Rooms[^1] : null;
            if (last?.Boss is { } boss)
            {
                var name = boss.IsBound ? boss.Def.Name : "";
                if (name.Length > 0) return $"{name} 처치";
            }
            return $"{RoomLine(act, depth.Room)} 정리";
        }
        return RoomLine(act, depth.Room);
    }

    /// <summary>타이틀 한 줄 <c>최고: 지하묘지 6 / 11</c>(8.1). 깊이가 비워진 기록(<c>BestDepth</c> null)은 줄을 띄우지 않는다.</summary>
    public static string? BestLine(GameDefs defs, RunRecord? record)
    {
        if (record?.BestDepth is not { } depth) return null;
        var act = RunPlanner.ActAt(defs, depth.Act);
        return $"최고: {Reached(act, depth, record.Victory)}";
    }

    /// <summary>
    /// 기록 비교 키(8.3 — v0.7.1): (깊이, 승리, −걸린 시간)의 사전순. <paramref name="result"/> 가 <paramref name="old"/> 보다 나으면 참(새 기록).
    /// 깊이가 비워진 옛 기록은 어떤 판보다도 얕다. 저장 구현(WP7)과 같은 순서 — 결과 화면 <c>새 기록</c> 표시만 쓴다.
    /// </summary>
    public static bool IsNewRecord(RunRecord? old, RunResult result)
    {
        if (old is null || old.BestDepth is not { } d) return true;
        var c = result.Reached.CompareTo(d);
        if (c != 0) return c > 0;
        if (result.Victory != old.Victory) return result.Victory;
        return result.Victory && result.RunTime < old.BestTime;
    }

    /// <summary>방 상태 줄(8.2): 생존 = 남은 시간, 정리 = <c>남은 적 n</c>, 문 = <c>문을 고르라</c>, 그 밖은 null.</summary>
    public static string? RoomStatus(World world, int liveEnemies)
    {
        if (world.Room is not { } room) return null;
        return room.Phase switch
        {
            RoomPhase.Survive when room.SurviveTicks > 0 => Clock(room.SurviveTicks - room.ClockTicks, ceil: true),
            RoomPhase.Cleanup => $"남은 적 {Number(liveEnemies)}",
            RoomPhase.Doors => "문을 고르라",
            _ => null,
        };
    }

    /// <summary>문 위 이름(기획 11.4 — 보상 이름, 정예·보스 앞 문은 <c>정예</c>·보스 이름, 계단은 <c>&lt;액트&gt;로</c>, 층이 바뀌면 <c>▼ n층</c>).</summary>
    public static string DoorLabel(RoomExit exit)
    {
        string label;
        if (exit.Stairs) label = exit.NextActName is { Length: > 0 } act ? $"{act}로" : "계단";
        else if (exit.NextKind == RoomKind.Boss && exit.NextBoss is { } boss) label = boss.Name.Length > 0 ? boss.Name : "보스";
        else if (exit.NextKind == RoomKind.Elite) label = "정예";
        else label = exit.Reward?.Name is { Length: > 0 } n ? n : "";
        if (exit.NextFloor != 0) label = label.Length > 0 ? $"{label} ▼ {exit.NextFloor}층" : $"▼ {exit.NextFloor}층";
        return label;
    }

    /// <summary>레벨업 창 제목(8.1 — 출처별).</summary>
    public static string OfferTitle(OfferSource source) => source switch
    {
        OfferSource.Tome => "마법서",
        OfferSource.Shrine => "매혹의 성소",
        _ => "레벨 업!",
    };

    /// <summary>카드 아래 둘째 줄: <c>새로 얻음</c> / <c>Lv 3 → 4</c>.</summary>
    public static string CardLevel(LevelUpCard card) => card.Kind switch
    {
        CardKind.NewWeapon or CardKind.NewPassive => "새로 얻음",
        CardKind.Filler => "",
        _ => string.Create(Inv, $"Lv {card.FromLevel} → {card.ToLevel}"),
    };

    /// <summary>건너뛰기 문구(<c>[X] 건너뛰기 (+10 체력)</c>). 회복이 없으면 괄호를 뺀다.</summary>
    public static string SkipLine(float heal) =>
        heal > 0f ? string.Create(Inv, $"[X] 건너뛰기 (+{heal:0.#} 체력)") : "[X] 건너뛰기";

    /// <summary>
    /// 매혹의 성소 카드에 붙는 줄(기획 12.2 — <c>+2Lv · 최대 체력 −10%</c>): 고른 무기 레벨 더하기 + 성소 대가 능력치. 대가 키 이름은 아는 것만 한국어, 나머지는 키 그대로.
    /// </summary>
    public static string ShrineLine(ShrineDef shrine)
    {
        var parts = new List<string>();
        if (shrine.PickWeapon > 0) parts.Add(string.Create(Inv, $"+{shrine.PickWeapon}Lv"));
        foreach (var (key, value) in shrine.Stats.OrderBy(kv => kv.Key))
        {
            var pct = key is StatKey.MaxHpPct or StatKey.DamagePct or StatKey.CooldownPct or StatKey.MovePct or StatKey.AreaPct;
            var amount = pct ? value * 100f : value;
            var sign = amount < 0 ? "−" : "+";
            var name = key switch
            {
                StatKey.MaxHpPct or StatKey.MaxHp => "최대 체력",
                StatKey.DamagePct => "피해",
                StatKey.CooldownPct => "쿨",
                StatKey.MovePct or StatKey.MoveSpeed => "이동",
                StatKey.Armor => "방어",
                _ => key.ToString(),
            };
            parts.Add(string.Create(Inv, $"{name} {sign}{MathF.Abs(amount):0.#}{(pct ? "%" : "")}"));
        }
        return string.Join(" · ", parts);
    }

    /// <summary>음량 10% 단위(0~100).</summary>
    public static string Percent(float volume) => string.Create(Inv, $"{(int)MathF.Round(Math.Clamp(volume, 0f, 1f) * 100f)}%");
}
