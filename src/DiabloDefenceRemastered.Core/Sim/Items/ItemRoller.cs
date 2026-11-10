namespace DiabloDefenceRemastered.Core.Sim.Items;

/// <summary>
/// 장비 굴림(v0.9 → v0.9.1 V7, 설계서 6.19 — 계약·구현 WP1g, 그 뒤 WP3. <b>난수 순서는 계약</b> — 바꾸면 같은 씨앗의 상인 물건·저장이 바뀐다, 11.3).
/// 차례: 마법 여부(액트별 <c>magic_chance</c>, 반지 등 <c>always_magic</c> 은 늘) → 접사 수(하나 1 − <c>two_affix_chance</c> / 둘 = 접두사 + 접미사) →
/// 하나면 접두사·접미사 반반 → 접사(그 칸 종류·그 액트 단계 중 정의 표 순서로 균등) → 값(범위를 <c>step</c> 으로 나눈 값 중 균등) →
/// 값 = 기본 값 + Σ 접사 값 × (<c>price_roll[0]</c> + <c>price_roll[1]</c> × 굴림 위치), <c>price_round</c> 단위 반올림(.5 는 위로).
/// <para><b>난수 수 고정</b>: 한 번 굴림에 늘 <see cref="DrawsPerRoll"/> 개를 뽑는다(쓰지 않는 갈래도 뽑아 버림 — 3.6 "뽑기 단계마다 난수 개수 고정" 과 같은 규칙).
/// 그래서 같은 흐름에서 뒤에 오는 굴림이 앞 굴림의 갈래에 밀리지 않는다. 카드마다 흐름을 따로 내는 것은 부르는 쪽(<c>ShopStock</c>)이다 — 정의 표에 접사를 더해도 다른 칸 카드가 그대로.</para>
/// 같은 쪽 접사 둘은 붙지 않는다(구조로). 고를 접사가 한쪽에만 있으면 그쪽, 양쪽 다 없으면 보통 등급.
/// </summary>
public static class ItemRoller
{
    /// <summary>굴림 한 번이 뽑는 난수 수(마법·접사 수·쪽·접두사·접미사·접두사 값·접미사 값).</summary>
    public const int DrawsPerRoll = 7;

    /// <summary>굴림 위치가 없는 고정 접사(<c>min == max</c>)의 위치 — 범위 가운데(값 배율 1.0).</summary>
    public const float FixedPosition = 0.5f;

    /// <summary>보통·마법을 굴린다(반지처럼 <c>always_magic</c> 이면 늘 마법).</summary>
    public static EquipItem Roll(ItemBaseDef b, int act, EquipmentRulesDef rules, GameDefs defs, RandomStream rng) =>
        RollCore(b, act, rules, defs, rng, forceMagic: false, avoidAffixes: null);

    /// <summary>늘 마법으로 굴린다. <paramref name="avoidAffixes"/> 의 접사는 고르지 않는다(아드리아 반지 둘 = 서로 다른 접사).</summary>
    public static EquipItem RollMagic(ItemBaseDef b, int act, EquipmentRulesDef rules, GameDefs defs, RandomStream rng, IReadOnlySet<string> avoidAffixes) =>
        RollCore(b, act, rules, defs, rng, forceMagic: true, avoidAffixes);

    /// <summary>접사 하나가 그 칸 종류·그 액트에 붙을 수 있는가(<c>stage</c> 1 = 두 액트, 2 = 액트 2 부터).</summary>
    public static bool Allowed(ItemAffixDef affix, EquipKind kind, int act) =>
        (affix.Stage <= 1 || act >= affix.Stage) && affix.Kinds.Contains(kind);

    /// <summary>범위 칸 수(<c>min == max</c> 면 1).</summary>
    public static int Steps(ItemAffixDef affix) =>
        affix.Max <= affix.Min || affix.Step <= 0f ? 1 : (int)MathF.Floor((affix.Max - affix.Min) / affix.Step + 1e-4f) + 1;

    /// <summary>칸 번호 <paramref name="index"/> 의 값(소수 넷째 자리 반올림 — 0.03 + 2 × 0.01 이 0.05 로).</summary>
    public static float ValueAt(ItemAffixDef affix, int index) => MathF.Round(affix.Min + index * affix.Step, 4);

    /// <summary>굴린 값의 범위 안 위치(0..1). 고정 접사는 <see cref="FixedPosition"/>.</summary>
    public static float Position(ItemAffixDef affix, float value)
    {
        var n = Steps(affix);
        if (n <= 1) return FixedPosition;
        var index = (int)MathF.Round((value - affix.Min) / affix.Step);
        return Math.Clamp(index, 0, n - 1) / (float)(n - 1);
    }

    /// <summary>값 = 기본 값 + Σ 접사 값 × (price_roll[0] + price_roll[1] × 위치), <c>price_round</c> 단위 반올림.</summary>
    public static int PriceOf(ItemBaseDef b, RolledAffix? prefix, RolledAffix? suffix, EquipmentRulesDef rules, GameDefs defs)
    {
        var price = (double)b.Price;
        foreach (var r in (ReadOnlySpan<RolledAffix?>)[prefix, suffix])
        {
            if (r is not { } a || !defs.ItemAffixes.TryGet(a.Id, out var def)) continue;
            var lo = rules.PriceRoll.Count > 0 ? rules.PriceRoll[0] : 1f;
            var span = rules.PriceRoll.Count > 1 ? rules.PriceRoll[1] : 0f;
            price += def.Price * (double)(lo + span * Position(def, a.Value));
        }
        var unit = Math.Max(1, rules.PriceRound);
        return (int)(Math.Round(price / unit, MidpointRounding.AwayFromZero) * unit);
    }

    private static EquipItem RollCore(ItemBaseDef b, int act, EquipmentRulesDef rules, GameDefs defs, RandomStream rng, bool forceMagic, IReadOnlySet<string>? avoidAffixes)
    {
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(defs);
        ArgumentNullException.ThrowIfNull(rng);

        // 난수 일곱 — 늘 같은 수(쓰지 않는 갈래도 뽑음)
        var uMagic = rng.NextFloat();
        var uTwo = rng.NextFloat();
        var uSide = rng.NextFloat();
        var pickPrefix = rng.NextUInt();
        var pickSuffix = rng.NextUInt();
        var uPrefixValue = rng.NextFloat();
        var uSuffixValue = rng.NextFloat();

        var magic = forceMagic || b.AlwaysMagic || uMagic < rules.MagicChanceAt(act);
        if (!magic) return new EquipItem(b.Id, null, null, PriceOf(b, null, null, rules, defs));

        var prefixes = Candidates(defs, b.Kind, act, prefix: true, avoidAffixes);
        var suffixes = Candidates(defs, b.Kind, act, prefix: false, avoidAffixes);
        bool usePrefix, useSuffix;
        if (uTwo < rules.TwoAffixChance && prefixes.Count > 0 && suffixes.Count > 0) usePrefix = useSuffix = true;
        else
        {
            var wantPrefix = uSide < 0.5f;
            usePrefix = wantPrefix ? prefixes.Count > 0 : suffixes.Count == 0 && prefixes.Count > 0;
            useSuffix = !usePrefix && suffixes.Count > 0;
        }

        RolledAffix? pre = usePrefix ? Pick(prefixes, pickPrefix, uPrefixValue) : null;
        RolledAffix? suf = useSuffix ? Pick(suffixes, pickSuffix, uSuffixValue) : null;
        return new EquipItem(b.Id, pre, suf, PriceOf(b, pre, suf, rules, defs));
    }

    private static RolledAffix Pick(List<ItemAffixDef> candidates, uint pick, float uValue)
    {
        var affix = candidates[candidates.Count <= 1 ? 0 : (int)((ulong)pick * (uint)candidates.Count >> 32)];
        var n = Steps(affix);
        var index = Math.Min(n - 1, (int)(uValue * n));
        return new RolledAffix(affix.Id, ValueAt(affix, index));
    }

    private static List<ItemAffixDef> Candidates(GameDefs defs, EquipKind kind, int act, bool prefix, IReadOnlySet<string>? avoid)
    {
        var list = new List<ItemAffixDef>();
        foreach (var a in defs.ItemAffixes.All)
            if (a.Prefix == prefix && Allowed(a, kind, act) && (avoid is null || !avoid.Contains(a.Id))) list.Add(a);
        return list;
    }
}
