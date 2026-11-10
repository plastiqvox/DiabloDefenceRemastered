namespace DiabloDefenceRemastered.Core.Sim.Items;

/// <summary>
/// 장비 능력치(v0.9 → v0.9.1, 설계서 6.19 "능력치 합산" — 계약·구현 WP1g, 그 뒤 WP3). 4.1 능력치 키 합산의 한 출처(<see cref="StatSources.Equipment"/>):
/// 절댓값 키는 더하고 <c>_pct</c> 키는 같은 키끼리 더한다(곱하기는 쓰는 쪽 — 4.1 그대로).
/// <list type="bullet">
/// <item><see cref="Sum"/>: 끼운 장비마다 기본 능력치 + 굴린 접사 + 방어구 무게 이동(<c>rules.equipment.weight_move[칸 종류][무게]</c> → <c>move_pct</c>). 자르지 않는다.</item>
/// <item><see cref="Clamp"/>: <b>모든 출처를 합친 뒤</b> 방어 ≤ <c>armor_cap</c>(15), <c>move_pct</c> ≥ <c>weight_move_floor</c>(−0.15).
/// 그래서 <c>of Swiftness</c> + 판금은 "하한 뒤에 더하기" 가 아니라 합친 값을 자른다(−18 + 5 = −13). 쿨다운 상한 60% 는 여기서 자르지 않는다 —
/// <c>DamageMath.FinalCooldown</c> 이 마력 폭주(상한 80%)와 함께 쓰는 곳에서 자르므로 두 번 자르면 폭주 때 값이 달라진다(13장 WP1g 구현 기록).</item>
/// <item><see cref="Total"/> = 다른 출처 + <see cref="Sum"/> → <see cref="Clamp"/>.</item>
/// <item><see cref="RebaseHp"/>: 최대 체력이 바뀐 순간 지금 체력 — 오르면 오른 만큼 더하고, 내려가면 새 최대에서 자른다(기획 11.6).</item>
/// </list>
/// 칸끼리 막지 않는다(v0.9.1 — 지팡이 + 방패를 함께 끼우면 둘 다 합산, 원작 양손 규칙 없음).
/// </summary>
public static class EquipStats
{
    /// <summary>장비만의 능력치(자르지 않음). <paramref name="equipment"/> = 칸 번호 순(빈 칸 null). 정의에 없는 ID 는 건너뛴다(이어 하기 대조가 먼저 막음).</summary>
    public static StatBlock Sum(IReadOnlyList<EquipItem?> equipment, GameDefs defs, EquipmentRulesDef rules)
    {
        var s = new StatBlock();
        Add(s, equipment, defs, rules);
        return s;
    }

    /// <summary><see cref="Sum"/> 을 <paramref name="into"/> 에 더한다.</summary>
    public static void Add(StatBlock into, IReadOnlyList<EquipItem?> equipment, GameDefs defs, EquipmentRulesDef rules)
    {
        ArgumentNullException.ThrowIfNull(into);
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(defs);
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var item in equipment)
        {
            if (item is null || !defs.ItemBases.TryGet(item.BaseId, out var b)) continue;
            foreach (var (k, v) in b.Stats) into[k] += v;
            AddAffix(into, item.Prefix, defs);
            AddAffix(into, item.Suffix, defs);
            if (b.Weight != ArmorWeight.None && rules.WeightMove.TryGetValue(b.Kind, out var byWeight) && byWeight.TryGetValue(b.Weight, out var move))
                into[StatKey.MovePct] += move;
        }
    }

    /// <summary>모든 출처를 합친 값을 자른다: 방어 ≤ <c>armor_cap</c>, <c>move_pct</c> ≥ <c>weight_move_floor</c>.</summary>
    public static void Clamp(StatBlock total, EquipmentRulesDef rules)
    {
        ArgumentNullException.ThrowIfNull(total);
        ArgumentNullException.ThrowIfNull(rules);
        total[StatKey.Armor] = MathF.Min(total[StatKey.Armor], rules.ArmorCap);
        total[StatKey.MovePct] = MathF.Max(total[StatKey.MovePct], rules.WeightMoveFloor);
    }

    /// <summary>다른 출처(<paramref name="others"/> — 바꾸지 않음) + 장비 → 자름.</summary>
    public static StatBlock Total(StatBlock others, IReadOnlyList<EquipItem?> equipment, GameDefs defs, EquipmentRulesDef rules)
    {
        ArgumentNullException.ThrowIfNull(others);
        var s = new StatBlock();
        s.CopyFrom(others);
        Add(s, equipment, defs, rules);
        Clamp(s, rules);
        return s;
    }

    /// <summary>
    /// 최대 체력이 <paramref name="oldMax"/> → <paramref name="newMax"/> 로 바뀐 순간의 지금 체력: 오르면 <c>hp + (new − old)</c>, 내려가면 <c>hp</c> 그대로,
    /// 그다음 새 최대에서 자르고 1 아래로 내리지 않는다(살아 있는 주인공이 장비를 바꿔 죽지 않게 — 기획 11.6). <paramref name="hp"/> ≤ 0(죽음)이면 그대로.
    /// </summary>
    public static float RebaseHp(float hp, float oldMax, float newMax)
    {
        if (hp <= 0f) return hp;
        var raised = newMax > oldMax ? hp + (newMax - oldMax) : hp;
        return MathF.Max(1f, MathF.Min(raised, newMax));
    }

    private static void AddAffix(StatBlock into, RolledAffix? rolled, GameDefs defs)
    {
        if (rolled is { } r && defs.ItemAffixes.TryGet(r.Id, out var a)) into[a.Stat] += r.Value;
    }
}
