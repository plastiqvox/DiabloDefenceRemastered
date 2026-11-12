using System.Globalization;

namespace DiabloDefenceRemastered.UI;

/// <summary>
/// 장비·페핀 카드 문구 (기획 11.6·11.7·12.3·12.8 — 단계 2 wp6). 상인 창 카드·일시정지 장비 목록이 같은 모양을 쓴다. 그림과 무관한 순수 함수.
/// <list type="bullet">
/// <item>이름: 원작 영어 그대로 <c>접두사 + 기본 이름 + 접미사</c>(<c>Fine Skull Cap of the Fox</c> — 기획 11.6 "이름").</item>
/// <item>효과 줄: 기본 능력치 한 줄(<c>최대 체력 +15 · 방어 +1</c>) + 굴린 접사마다 한 줄(실제 값 — 기본 능력치와 같은 키면 기본 줄에 합침). 무게 줄 <c>중갑 · 이동 −5%</c>.</item>
/// <item>차이 줄: 그 칸 지금 장비와 <c>EquipStats.Sum</c> 을 견줌(<c>최대 체력 +10 → +25</c>), 빈 칸이면 <c>빈 칸</c>.</item>
/// </list>
/// 음수 부호는 ASCII <c>-</c>(기획 표기는 U+2212 <c>−</c> 지만 화면 글꼴 굽기에서 그 글자가 빈칸으로 찍혔다 — <c>--preview ui</c> 캡처).
/// </summary>
public static class ItemText
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>차이 줄·효과 줄의 키 차례(장비가 쓰는 키 — 기획 11.6 "능력치").</summary>
    public static readonly StatKey[] Order =
    [
        StatKey.MaxHp, StatKey.Armor, StatKey.Regen, StatKey.DamagePct, StatKey.CooldownPct,
        StatKey.MovePct, StatKey.PickupPct, StatKey.KnockbackTakenPct, StatKey.GoldPct,
    ];

    /// <summary>차이 줄에 보이는 키 수(카드 폭 200 안 — 넘으면 <c>…</c>).</summary>
    public const int DiffParts = 2;

    /// <summary>장비 이름(접두사 + 기본 + 접미사). 정의에 없는 ID 는 ID 그대로.</summary>
    public static string Name(EquipItem item, GameDefs defs)
    {
        var name = defs.ItemBases.TryGet(item.BaseId, out var b) && b.Name.Length > 0 ? b.Name : item.BaseId;
        if (item.Prefix is { } p && defs.ItemAffixes.TryGet(p.Id, out var pre)) name = $"{pre.Name} {name}";
        if (item.Suffix is { } s && defs.ItemAffixes.TryGet(s.Id, out var suf)) name = $"{name} {suf.Name}";
        return name;
    }

    /// <summary>능력치 키의 한국어 이름(기획 11.6 접사 표의 말).</summary>
    public static string StatName(StatKey key) => key switch
    {
        StatKey.MaxHp => "최대 체력",
        StatKey.Armor => "방어",
        StatKey.Regen => "체력 재생",
        StatKey.DamagePct => "피해",
        StatKey.CooldownPct => "쿨다운 감소",
        StatKey.MovePct or StatKey.MoveSpeed => "이동",
        StatKey.PickupPct or StatKey.PickupRadius => "획득 반경",
        StatKey.KnockbackTakenPct => "받는 넉백",
        StatKey.GoldPct => "금화",
        _ => key.ToString(),
    };

    /// <summary>비율 키(값 0.03 = 3%).</summary>
    public static bool IsPct(StatKey key) => key is StatKey.DamagePct or StatKey.CooldownPct or StatKey.MovePct or StatKey.PickupPct
        or StatKey.KnockbackTakenPct or StatKey.GoldPct or StatKey.MaxHpPct or StatKey.AreaPct or StatKey.DurationPct;

    /// <summary>값만(<c>+15</c>·<c>+3%</c>·<c>+0.5/s</c>·<c>−5%</c>·<c>0</c>).</summary>
    public static string Value(StatKey key, float v)
    {
        var amount = IsPct(key) ? v * 100f : v;
        if (MathF.Abs(amount) < 0.05f) return "0";
        var sign = amount < 0 ? "-" : "+";
        var unit = IsPct(key) ? "%" : key == StatKey.Regen ? "/s" : "";
        return string.Create(Inv, $"{sign}{MathF.Abs(amount):0.#}{unit}");
    }

    /// <summary><c>최대 체력 +15</c>.</summary>
    public static string Stat(StatKey key, float v) => $"{StatName(key)} {Value(key, v)}";

    /// <summary>기본 능력치(<see cref="Order"/> 차례, 그 밖의 키는 뒤에). 이동(무게)은 무게 줄로 따로.</summary>
    private static List<(StatKey Key, float Value)> BaseStats(ItemBaseDef b)
    {
        var stats = new List<(StatKey Key, float Value)>();
        foreach (var k in Order)
            if (b.Stats.TryGetValue(k, out var v) && v != 0f) stats.Add((k, v));
        foreach (var (k, v) in b.Stats)
            if (Array.IndexOf(Order, k) < 0 && v != 0f) stats.Add((k, v));
        return stats;
    }

    /// <summary>기본 능력치 한 줄(<c>최대 체력 +15 · 방어 +1</c>). 없으면(반지) null. 이동(무게)은 무게 줄로 따로.</summary>
    public static string? BaseLine(ItemBaseDef b) => Join(BaseStats(b));

    /// <summary>굴린 접사 한 줄(실제 값 — <c>최대 체력 +11</c>). 정의에 없으면 null.</summary>
    public static string? AffixLine(RolledAffix? rolled, GameDefs defs) =>
        Affix(rolled, defs) is { } a ? Stat(a.Key, a.Value) : null;

    private static (StatKey Key, float Value)? Affix(RolledAffix? rolled, GameDefs defs) =>
        rolled is { } r && defs.ItemAffixes.TryGet(r.Id, out var a) ? (a.Stat, r.Value) : null;

    /// <summary>같은 키가 이미 있으면 값을 더하고 참.</summary>
    private static bool AddTo(List<(StatKey Key, float Value)> stats, StatKey key, float value)
    {
        var i = stats.FindIndex(s => s.Key == key);
        if (i < 0) return false;
        stats[i] = (key, stats[i].Value + value);
        return true;
    }

    private static string? Join(List<(StatKey Key, float Value)> stats) =>
        stats.Count == 0 ? null : string.Join(" · ", stats.Select(s => Stat(s.Key, s.Value)));

    /// <summary>
    /// 효과 줄: 기본 능력치(있으면) + 접두사 + 접미사 차례. 같은 키는 한 줄로 합친다 — 접사 키가 기본 능력치에 있으면 기본 줄의 그 값에 더하고
    /// (<c>Fine Buckler</c> 기본 <c>방어 +1</c> + 접사 <c>방어 +1</c> → <c>방어 +2</c>), 접두사·접미사가 같은 키면 한 줄로.
    /// </summary>
    public static IReadOnlyList<string> EffectLines(EquipItem item, GameDefs defs)
    {
        var groups = EffectStats(item, defs);
        var lines = new List<string>(groups.Count);
        foreach (var g in groups)
            if (Join(g) is { } line) lines.Add(line);
        return lines;
    }

    /// <summary><see cref="EffectLines"/> 의 줄마다 (키, 합친 값) — 첫 묶음 = 기본 능력치(비어 있을 수 있음), 그 뒤 = 기본에 없는 접사 키 하나씩.</summary>
    private static List<List<(StatKey Key, float Value)>> EffectStats(EquipItem item, GameDefs defs)
    {
        var baseStats = defs.ItemBases.TryGet(item.BaseId, out var b) ? BaseStats(b) : [];
        List<List<(StatKey Key, float Value)>> groups = [baseStats];
        foreach (var rolled in (RolledAffix?[])[item.Prefix, item.Suffix])
        {
            if (Affix(rolled, defs) is not { } a) continue;
            if (AddTo(baseStats, a.Key, a.Value)) continue;
            var merged = false;
            for (var i = 1; i < groups.Count && !merged; i++) merged = AddTo(groups[i], a.Key, a.Value);
            if (!merged) groups.Add([(a.Key, a.Value)]);
        }
        return groups;
    }

    /// <summary>효과를 한 줄로(일시정지 장비 목록 — 기획 12.3 "효과 한 줄씩"). 무게 이동도 붙인다(이동 접사가 있으면 합친 값 하나로). 같은 키는 한 번만.</summary>
    public static string OneLine(EquipItem item, GameDefs defs, EquipmentRulesDef? rules)
    {
        var stats = EffectStats(item, defs).SelectMany(g => g).ToList();
        if (defs.ItemBases.TryGet(item.BaseId, out var b) && WeightMove(b, rules) is { } move && move != 0f && !AddTo(stats, StatKey.MovePct, move))
            stats.Add((StatKey.MovePct, move));
        return Join(stats.Where(s => MathF.Abs(s.Value) > 1e-6f).ToList()) ?? "";
    }

    /// <summary>무게 이름(경갑·중갑·판금, 무게 없음 null — 기획 11.6 V6).</summary>
    public static string? WeightName(ArmorWeight w) => w switch
    {
        ArmorWeight.Light => "경갑",
        ArmorWeight.Medium => "중갑",
        ArmorWeight.Heavy => "판금",
        _ => null,
    };

    /// <summary>그 기본 아이템 무게의 이동(<c>rules.equipment.weight_move[칸 종류][무게]</c>), 무게 없음·규칙 없음이면 null.</summary>
    public static float? WeightMove(ItemBaseDef b, EquipmentRulesDef? rules) =>
        b.Weight != ArmorWeight.None && rules is not null && rules.WeightMove.TryGetValue(b.Kind, out var byWeight) && byWeight.TryGetValue(b.Weight, out var move)
            ? move : null;

    /// <summary>무게 줄(<c>중갑 · 이동 −5%</c>, 경갑은 이동이 0 이라 <c>경갑</c>만). 무게 없는 칸(지팡이·반지)은 null.</summary>
    public static string? WeightLine(ItemBaseDef b, EquipmentRulesDef? rules)
    {
        if (WeightName(b.Weight) is not { } name) return null;
        return WeightMove(b, rules) is { } move && move != 0f ? $"{name} · {Stat(StatKey.MovePct, move)}" : name;
    }

    /// <summary>
    /// 지금 칸 장비와의 차이 줄(기획 12.8): <paramref name="now"/> 가 null 이면 <c>빈 칸</c>. 아니면 두 장비의 <c>EquipStats.Sum</c>(무게 이동 포함)을
    /// <see cref="Order"/> 차례로 견주어 다른 키를 <c>최대 체력 +10 → +25</c> 꼴로 <see cref="DiffParts"/> 개까지(넘으면 <c>…</c>), 같으면 <c>차이 없음</c>.
    /// </summary>
    public static string DiffLine(EquipItem? now, EquipItem next, GameDefs defs, EquipmentRulesDef? rules) =>
        string.Join(" · ", DiffLines(now, next, defs, rules));

    /// <summary>
    /// <see cref="DiffLine"/> 을 키마다 한 토막씩(바꿀 반지 카드가 토막마다 한 줄로 그린다 — 한 줄로 이으면 카드 폭 200 에서 잘렸다).
    /// <paramref name="max"/> 개 키까지(기본 <see cref="DiffParts"/>), 넘친 키가 있으면 마지막 토막 끝에 <c> …</c>.
    /// </summary>
    public static IReadOnlyList<string> DiffLines(EquipItem? now, EquipItem next, GameDefs defs, EquipmentRulesDef? rules, int max = DiffParts)
    {
        if (now is null) return ["빈 칸"];
        if (rules is null) return [""];
        var a = EquipStats.Sum([now], defs, rules);
        var b = EquipStats.Sum([next], defs, rules);
        var parts = new List<string>();
        var more = false;
        foreach (var k in Order)
        {
            if (MathF.Abs(a[k] - b[k]) < 1e-4f) continue;
            if (parts.Count >= max) { more = true; break; }
            parts.Add($"{StatName(k)} {Value(k, a[k])} → {Value(k, b[k])}");
        }
        if (parts.Count == 0) return ["차이 없음"];
        if (more) parts[^1] += " …";
        return parts;
    }

    // ── 페핀 카드(기획 11.7 V11·12.8) ─────────────────────────

    /// <summary>페핀 카드 이름.</summary>
    public static string PotionName(PotionUpgrade kind) => kind switch
    {
        PotionUpgrade.Slots => "물약 주머니",
        PotionUpgrade.Speed => "빠른 물약",
        _ => "물약 채우기",
    };

    /// <summary>
    /// 페핀 카드 효과 줄(기획 12.8): <c>물약 칸 3 → 4</c>·<c>한 병 3초 → 2초</c>·<c>2병 채움</c>. 다 올렸으면 지금 값만(<c>물약 칸 5</c>·<c>한 병 1.2초</c>), 칸이 가득이면 <c>칸이 가득</c>.
    /// </summary>
    public static string PotionEffect(PotionUpgrade kind, PotionRulesDef rules, int potions, int slotsLevel, int speedLevel, bool maxed)
    {
        switch (kind)
        {
            case PotionUpgrade.Slots:
            {
                var cap = rules.Capacity(slotsLevel);
                return maxed ? string.Create(Inv, $"물약 칸 {cap}") : string.Create(Inv, $"물약 칸 {cap} → {rules.Capacity(slotsLevel + 1)}");
            }
            case PotionUpgrade.Speed:
            {
                var now = rules.Duration(speedLevel);
                return maxed ? string.Create(Inv, $"한 병 {now:0.#}초") : string.Create(Inv, $"한 병 {now:0.#}초 → {rules.Duration(speedLevel + 1):0.#}초");
            }
            default:
            {
                var empty = Math.Max(0, rules.Capacity(slotsLevel) - potions);
                return empty > 0 ? string.Create(Inv, $"{empty}병 채움") : "칸이 가득";
            }
        }
    }

    /// <summary>살 수 없는 페핀 카드의 표시(<c>다 올림</c> / 채우기는 <c>칸 가득</c>).</summary>
    public static string PotionDone(PotionUpgrade kind) => kind == PotionUpgrade.Refill ? "칸 가득" : "다 올림";
}
