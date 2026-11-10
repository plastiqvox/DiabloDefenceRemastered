namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// 문이 열리는 틱의 판·주인공 상태(v0.9 — <see cref="RewardDraw.Pick"/> 의 입력). <paramref name="HpRatio"/> = <c>Hero.Hp / Hero.MaxHp</c>(최대 체력이 0 이면 −1 = 모름 — 체력 조건을 보지 않음).
/// 나머지는 <see cref="RunState"/>(방 없는 월드·판 없는 시험은 기본값).
/// </summary>
public readonly record struct RewardDrawState(float HpRatio, int Potions, IReadOnlyList<string> MerchantsMet, int MerchantRoomsThisAct,
                                              bool MerchantShownThisAct, bool LastRoomWasMerchant)
{
    public bool HpKnown => HpRatio >= 0f;

    /// <summary>월드에서 읽는다(문 열기 — <c>RoomFlow</c>).</summary>
    public static RewardDrawState Of(World world)
    {
        var hero = world.Hero;
        var ratio = hero.MaxHp > 0f ? hero.Hp / hero.MaxHp : -1f;
        var run = world.Run;
        return new RewardDrawState(ratio, hero.Potions, run?.MerchantsMet ?? [], run?.MerchantRoomsThisAct ?? 0,
                                   run?.MerchantShownThisAct ?? false, run?.LastRoomWasMerchant ?? false);
    }
}

/// <summary>
/// 문 보상 뽑기(v0.9 → v0.9.1, 설계서 6.19 "회복 문"·3.6 "문 열기", 기획 11.4 "뽑기"·11.7 — 계약·구현 WP1g, 그 뒤 WP4. 차례와 난수 쓰임은 판 결과라 계약).
/// <b>문이 열리는 틱 상태의 함수</b>이고 난수는 계획(<see cref="RoomPlan.RewardDraws"/> — <c>Flow(n)</c> ④ 가 뽑아 둔 값)만 쓴다 — 늘 같은 수를 쓰므로 보장이 있어도 다음 방들이 밀리지 않는다.
/// <list type="number">
/// <item><b>가중 바꿈</b>: 회복 문 = 체력 &lt; <c>heal_low_hp</c> 면 <c>heal_low_hp_weight</c>, 상인 = 이 판에 다른 상인을 만났으면 × <c>merchant_met_other</c>,
/// 물약 상인 = 물약 0 ∧ 체력 &lt; <c>healer_boost_below</c> 면 × <c>healer_boost</c>. 바꾼 가중으로 액트 <c>rewards.weights</c> 를 같은 난수(④)로 다시 줄 세운다 —
/// 바뀐 가중이 없으면 <see cref="RoomPlan.RewardOrder"/> 와 같다.</item>
/// <item><b>보장</b>: 체력 &lt; <c>heal_guarantee_below</c> 면 첫 출구 문(거울판이면 짝 — <c>ExitGates[0]</c>)이 회복 문. 상인 보장(이 액트에 상인 문이 안 보였고 다음 방 번호 = <c>merchant_guarantee_room[액트]</c>, 상인 조건 통과)이면
/// 둘째 문이 상인(누구인지는 거른 상인 중 균등 — 계획의 마지막 난수). 겹치면 회복이 첫 문·상인이 둘째 문.</item>
/// <item><b>거르기</b>로 나머지 문을 줄 순서대로 채운다: <c>IProgression.RewardAvailable</c>, 회복 문 = 체력 ≥ <c>heal_skip_at_least</c> 면 빠짐,
/// 상인 = 다음 방이 일반 방 ∧ (액트 1 이면 다음 방 번호 ≥ <c>merchant_first_room</c>) ∧ 바로 앞 방(이 방)이 상인 방 아님 ∧ 이 액트 상인 방 &lt; <c>merchant_per_act</c> ∧ 이 문 단계에 상인 문이 아직 없음.
/// 같은 ID 는 두 문에 두지 않고, 모자라면 <c>fallback_reward</c>(같은 종류가 두 문에 올 수 있음).</item>
/// </list>
/// 체력 조건은 최대 체력이 0 인 월드(성장 모듈이 없는 시험)에서는 보지 않는다.
/// </summary>
public static class RewardDraw
{
    /// <summary>출구 문 <paramref name="gates"/> 개의 보상(문 순서 = <c>ExitGates</c>). <paramref name="next"/> = 다음 방 계획(액트 안).</summary>
    public static RewardDef[] Pick(StageDef act, RoomPlan plan, RoomPlan next, int gates, in RewardDrawState state, RoomRulesDef rules, Func<RewardDef, bool> available)
    {
        ArgumentNullException.ThrowIfNull(act);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(available);
        var rr = rules.Rewards;
        var st = state;

        // ① 가중 바꿈 → 줄 순서
        var entries = new List<WeightEntry<RewardDef>>();
        foreach (var w in act.Rewards.Weights)
            if (w.Weight > 0f) entries.Add(w);
        var weights = new float[entries.Count];
        var changed = false;
        for (var i = 0; i < entries.Count; i++)
        {
            var r = entries[i].Ref.Def;
            var wt = entries[i].Weight;
            if (r.Kind == RewardKind.HealNextRoom && st.HpKnown && st.HpRatio < rr.HealLowHp) wt = rr.HealLowHpWeight;
            if (r.Kind == RewardKind.Merchant && r.Merchant is { } m)
            {
                if (st.MerchantsMet.Count > 0 && !st.MerchantsMet.Contains(m.Id)) wt *= rr.MerchantMetOther;
                if (m.IsBound && m.Def.Stock == MerchantStock.Potions && st.Potions <= 0 && st.HpKnown && st.HpRatio < rr.HealerBoostBelow) wt *= rr.HealerBoost;
            }
            weights[i] = wt;
            if (wt != entries[i].Weight) changed = true;
        }
        var draws = plan.RewardDraws;
        IReadOnlyList<RewardDef> order = changed && draws is not null && draws.Count >= entries.Count
            ? Permute(entries, weights, draws)
            : plan.RewardOrder;
        var guaranteeDraw = draws is not null && draws.Count > entries.Count ? draws[entries.Count] : 0f;

        // ② 거르기 조건
        var merchantOpen = next.Kind == RoomKind.Normal
                           && !(plan.Act == 1 && next.Number < rr.MerchantFirstRoom)
                           && !st.LastRoomWasMerchant
                           && st.MerchantRoomsThisAct < rr.MerchantPerAct;
        bool Ok(RewardDef r)
        {
            if (!available(r)) return false;
            return r.Kind switch
            {
                RewardKind.HealNextRoom => !(st.HpKnown && st.HpRatio >= rr.HealSkipAtLeast),
                RewardKind.Merchant => merchantOpen,
                _ => true,
            };
        }

        var picks = new RewardDef?[gates];
        var merchantPicked = false;

        // ③ 보장 — 회복 첫 문, 상인 둘째 문
        if (gates > 0 && st.HpKnown && st.HpRatio < rr.HealGuaranteeBelow)
        {
            foreach (var e in entries)
                if (e.Ref.Def.Kind == RewardKind.HealNextRoom) { picks[0] = e.Ref.Def; break; }
        }
        if (gates > 1 && merchantOpen && !st.MerchantShownThisAct && GuaranteeRoom(rr, plan.Act) == next.Number)
        {
            var merchants = new List<RewardDef>();
            foreach (var e in entries)
                if (e.Ref.Def.Kind == RewardKind.Merchant && available(e.Ref.Def)) merchants.Add(e.Ref.Def);
            if (merchants.Count > 0)
            {
                picks[1] = merchants[Math.Min(merchants.Count - 1, (int)(guaranteeDraw * merchants.Count))];
                merchantPicked = true;
            }
        }

        // ④ 나머지 문 — 줄 순서대로, 같은 ID 두 번 없음
        var slot = 0;
        foreach (var r in order)
        {
            while (slot < gates && picks[slot] is not null) slot++;
            if (slot >= gates) break;
            if (Array.Exists(picks, p => p is not null && string.Equals(p.Id, r.Id, StringComparison.Ordinal))) continue;
            if (!Ok(r)) continue;
            if (r.Kind == RewardKind.Merchant)
            {
                if (merchantPicked) continue;
                merchantPicked = true;
            }
            picks[slot++] = r;
        }
        var result = new RewardDef[gates];
        for (var i = 0; i < gates; i++) result[i] = picks[i] ?? rules.FallbackReward.Def;
        return result;
    }

    /// <summary>액트 <paramref name="act"/>(1부터)의 상인 보장 방 번호. 표가 짧으면 0(보장 없음).</summary>
    public static int GuaranteeRoom(RewardRulesDef rules, int act) =>
        act >= 1 && act <= rules.MerchantGuaranteeRoom.Count ? rules.MerchantGuaranteeRoom[act - 1] : 0;

    /// <summary>
    /// 가중 비복원 줄 세우기 — <c>RunPlanner</c> ④ 와 같은 식·같은 난수(<paramref name="draws"/>[k] = k 번째 뽑기). 가중이 같으면 <c>RewardOrder</c> 와 같다.
    /// </summary>
    public static List<RewardDef> Permute(IReadOnlyList<WeightEntry<RewardDef>> entries, IReadOnlyList<float> weights, IReadOnlyList<float> draws)
    {
        var left = new List<int>(entries.Count);
        for (var i = 0; i < entries.Count; i++) left.Add(i);
        var order = new List<RewardDef>(entries.Count);
        var d = 0;
        while (left.Count > 0)
        {
            var total = 0f;
            foreach (var i in left) total += weights[i];
            var u = draws[d++] * total;
            var k = 0;
            for (; k < left.Count - 1; k++)
            {
                u -= weights[left[k]];
                if (u < 0f) break;
            }
            order.Add(entries[left[k]].Ref.Def);
            left.RemoveAt(k);
        }
        return order;
    }
}
