namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>페핀 카드 값을 정할 물약 칸 상태(v0.9.1 — <c>Hero.Potions</c>·<c>PotionSlotsLevel</c>·<c>PotionSpeedLevel</c>).</summary>
public readonly record struct ShopPotions(int Potions, int SlotsLevel, int SpeedLevel);

/// <summary>
/// 상인 물건 정하기(v0.9 → v0.9.1, 설계서 6.19 "상인 방 상태와 구매 명령" — 계약·구현 WP1g, 그 뒤 WP4. <b>상인 전용 흐름 이름·차례는 계약</b>, 11.3).
/// <list type="bullet">
/// <item>장비 상인: <c>kinds</c> 의 카드마다 흐름 <c>RunSeeds.Shop(씨앗, 액트, 방 번호, 다시 굴린 수, 카드)</c> 하나 — 그 액트에 나오는 그 칸 종류 기본 아이템 중 균등(난수 하나) →
/// <c>ItemRoller.Roll</c>(반지는 다른 반지 카드와 다른 접사 — <c>RollMagic</c>). 방 난수를 하나도 쓰지 않는다(상인 방이든 아니든 적·드롭이 같다).</item>
/// <item>다시 굴리기(<see cref="Reroll"/>): <c>Rerolls</c> 를 올리고 <b>사지 않은 카드만</b> 같은 식으로 새로 굴린다(산 카드는 자리를 지킴). 같은 씨앗·같은 누른 수면 같은 결과.</item>
/// <item>물약 상인(페핀): 굴리지 않는다 — <see cref="PotionCard"/> 가 지금 단계의 값·다 올림·채우기 값(빈 칸 × <c>per_slot</c>)을 낸다. 사고 나면 부르는 쪽(WP3)이 다시 부른다.</item>
/// </list>
/// </summary>
public static class ShopStock
{
    /// <summary>상인 방에 들어오는 틱의 물건(<c>RunSession</c> 이 새 <c>World</c> 직후·정리 전에 부른다).</summary>
    public static ShopState Create(MerchantDef merchant, int act, int number, ulong runSeed, GameDefs defs, ShopPotions potions)
    {
        ArgumentNullException.ThrowIfNull(merchant);
        ArgumentNullException.ThrowIfNull(defs);
        var shop = new ShopState { MerchantId = merchant.Id };
        switch (merchant.Stock)
        {
            case MerchantStock.Equipment:
                for (var c = 0; c < merchant.Kinds.Count; c++) shop.Cards.Add(new ShopCard(ShopCardKind.Equip, null, default, 0, false));
                RollEquip(shop, merchant, act, number, runSeed, defs);
                break;
            case MerchantStock.Potions:
                var rules = defs.Rules.Hero?.Potions ?? throw new InvalidOperationException($"물약 상인 '{merchant.Id}' — rules.hero.potions 가 없다(6.19)");
                foreach (var up in merchant.Upgrades) shop.Cards.Add(PotionCard(up, rules, potions));
                break;
        }
        return shop;
    }

    /// <summary>
    /// 다시 굴리기: <c>Rerolls</c> + 1 뒤 사지 않은 장비 카드를 새로 굴린다. 금화 빼기·<c>rerollable</c> 확인은 부르는 쪽(WP3 <c>IShop.Reroll</c>).
    /// 물약 상인이면 아무것도 바꾸지 않는다.
    /// </summary>
    public static void Reroll(ShopState shop, MerchantDef merchant, int act, int number, ulong runSeed, GameDefs defs)
    {
        ArgumentNullException.ThrowIfNull(shop);
        ArgumentNullException.ThrowIfNull(merchant);
        if (merchant.Stock != MerchantStock.Equipment) return;
        shop.Rerolls++;
        RollEquip(shop, merchant, act, number, runSeed, defs);
    }

    /// <summary>다시 굴리기 값(<c>reroll_price + reroll_step × 이 방에서 누른 수</c> — 30·60·90…).</summary>
    public static int RerollPrice(EquipmentRulesDef rules, int rerolls) => rules.RerollPrice + rules.RerollStep * Math.Max(0, rerolls);

    /// <summary>
    /// 페핀 카드 하나(v0.9.1 V11): 물약 주머니 = <c>prices[주머니 단계]</c>(칸이 <c>slots_max</c> 거나 단계 값이 끝나면 다 올림), 빠른 물약 = <c>prices[빠른 단계]</c>(<c>durations</c> 끝이거나 값이 끝나면 다 올림),
    /// 물약 채우기 = 빈 칸 × <c>per_slot</c>(칸이 가득이면 살 수 없음). 살 수 없으면 <see cref="ShopCard.Bought"/> 참(어둡게 — 기획 12.8).
    /// </summary>
    public static ShopCard PotionCard(PotionUpgradeDef up, PotionRulesDef rules, ShopPotions state)
    {
        ArgumentNullException.ThrowIfNull(up);
        ArgumentNullException.ThrowIfNull(rules);
        switch (up.Kind)
        {
            case PotionUpgrade.Slots:
            {
                var maxed = state.SlotsLevel >= up.Prices.Count || rules.Capacity(state.SlotsLevel) >= rules.SlotsMax;
                return new ShopCard(ShopCardKind.Potion, null, up.Kind, maxed ? 0 : up.Prices[Math.Max(0, state.SlotsLevel)], maxed);
            }
            case PotionUpgrade.Speed:
            {
                var maxed = state.SpeedLevel >= up.Prices.Count || state.SpeedLevel >= rules.Durations.Count - 1;
                return new ShopCard(ShopCardKind.Potion, null, up.Kind, maxed ? 0 : up.Prices[Math.Max(0, state.SpeedLevel)], maxed);
            }
            default:
            {
                var empty = Math.Max(0, rules.Capacity(state.SlotsLevel) - state.Potions);
                return new ShopCard(ShopCardKind.Potion, null, up.Kind, empty * up.PerSlot, empty == 0);
            }
        }
    }

    /// <summary>사지 않은 장비 카드를 지금 <c>Rerolls</c> 의 흐름으로 굴린다. 반지는 이미 정해진 다른 반지 카드(산 것·앞 카드)와 다른 접사.</summary>
    private static void RollEquip(ShopState shop, MerchantDef merchant, int act, int number, ulong runSeed, GameDefs defs)
    {
        var rules = defs.Rules.Equipment ?? throw new InvalidOperationException($"장비 상인 '{merchant.Id}' — rules.equipment 가 없다(6.19)");
        var settled = new bool[shop.Cards.Count];
        for (var c = 0; c < shop.Cards.Count; c++) settled[c] = shop.Cards[c].Bought;
        for (var c = 0; c < shop.Cards.Count && c < merchant.Kinds.Count; c++)
        {
            if (shop.Cards[c].Bought) continue;
            var kind = merchant.Kinds[c];
            var rng = new RandomStream(RunSeeds.Shop(runSeed, act, number, shop.Rerolls, c));
            var bases = new List<ItemBaseDef>();
            foreach (var b in defs.ItemBases.All)
                if (b.Kind == kind && b.Acts.Contains(act)) bases.Add(b);
            var pick = rng.NextUInt();   // 늘 하나(후보가 하나여도)
            if (bases.Count == 0)
            {
                shop.Cards[c] = new ShopCard(ShopCardKind.Equip, null, default, 0, true);   // 그 액트에 나올 기본 아이템이 없다 — 살 수 없음
                settled[c] = true;
                continue;
            }
            var chosen = bases[bases.Count <= 1 ? 0 : (int)((ulong)pick * (uint)bases.Count >> 32)];
            EquipItem item;
            if (kind == EquipKind.Ring)
            {
                var avoid = new HashSet<string>(StringComparer.Ordinal);
                for (var o = 0; o < shop.Cards.Count; o++)
                    if (o != c && settled[o] && shop.Cards[o].Item is { } other)
                    {
                        if (other.Prefix is { } p) avoid.Add(p.Id);
                        if (other.Suffix is { } s) avoid.Add(s.Id);
                    }
                item = ItemRoller.RollMagic(chosen, act, rules, defs, rng, avoid);
            }
            else item = ItemRoller.Roll(chosen, act, rules, defs, rng);
            shop.Cards[c] = new ShopCard(ShopCardKind.Equip, item, default, item.Price, false);
            settled[c] = true;
        }
    }
}
