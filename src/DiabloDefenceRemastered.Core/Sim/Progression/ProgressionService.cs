using DiabloDefenceRemastered.Core.Sim.Heroes;

namespace DiabloDefenceRemastered.Core.Sim.Progression;

/// <summary>
/// 성장 (WP1 계약 <see cref="IProgression"/>, WP3 구현 — 기획 4.3·4.4·7·11.1·11.4, 설계서 6.10·6.18). 상태는 <see cref="World.Combat"/>(방 수명)에 있고 이 객체는 월드마다 만든다.
/// 선택 창은 시뮬레이션이 멈춘 사이에 오버레이가 부른다. 고른 뒤 대기 순서(레벨업 → 상자 → 보상)대로 <c>World.AwaitingChoice</c> 를 바꾸고, 다 비면 None.
/// 카드 뽑기 난수 = <c>LevelUp</c> 흐름, 보물 상자·성소의 무작위 무기 = 방 난수 <c>Drop</c>.
/// </summary>
public sealed class ProgressionService(World world) : IProgression
{
    private CombatState S => world.Combat;

    public int Level => S.Level;
    public int Xp => S.Xp;
    public int XpToNext => XpCurve.Need(world.Defs.Rules.XpCurve, S.Level);
    public int PendingLevelUps => S.PendingLevelUps;
    public int PendingChests => S.Chests.Count;
    public int PendingRewards => S.Rewards.Count;
    public int PicksLeft => Build.PicksLeft(world);

    /// <summary>경험치를 더한다(보석). 레벨이 오르면 레벨마다 대기 +1·<c>LevelUpReady</c>.</summary>
    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        S.Xp += amount;
        world.RunStats.XpCollected += amount;
        world.Events.Current.XpGained.Add(new XpGained(amount, world.RunStats.XpCollected));
        for (var need = XpToNext; S.Xp >= need; need = XpToNext)
        {
            S.Xp -= need;
            S.Level++;
            S.PendingLevelUps++;
            world.Events.Current.LevelUpReady.Add(new LevelUpReady(S.Level));
        }
    }

    // ── 레벨업 ───────────────────────────────────────────────────

    public LevelUpOffer Offer()
    {
        if (S.PendingLevelUps <= 0) throw new InvalidOperationException("대기 중인 레벨업이 없다");
        return S.LevelOffer ??= MakeLevelOffer();
    }

    private LevelUpOffer MakeLevelOffer()
    {
        var rules = world.Defs.Rules.LevelUp ?? new LevelUpRulesDef();
        var forLevel = S.Level - S.PendingLevelUps + 1;
        var pool = Build.LevelUpCandidates(world, rules.Weights);
        var rng = world.Rng.LevelUp;
        var picked = Build.Draw(rng, pool, rules.Cards);
        // 초반 보장(기획 4.4): 그 레벨의 창에 새 무기 카드 하나 이상(무기 칸이 남아 있으면)
        if (rules.GuaranteeNewWeaponAt.Contains(forLevel) && !picked.Exists(c => c.Kind == CardKind.NewWeapon))
        {
            var fresh = pool.FindAll(c => c.Kind == CardKind.NewWeapon);
            if (fresh.Count > 0)
            {
                var one = Build.Draw(rng, fresh, 1)[0];
                if (picked.Count >= rules.Cards && picked.Count > 0) picked[^1] = one;
                else picked.Add(one);
            }
        }
        var cards = new List<LevelUpCard>(rules.Cards);
        foreach (var c in picked) cards.Add(Build.Card(world, c));
        foreach (var f in rules.Fillers)
        {
            if (cards.Count >= rules.Cards) break;
            cards.Add(Build.Card(world, new Candidate(CardKind.Filler, f.Id, 0f)));
        }
        return new LevelUpOffer(forLevel, cards, S.RerollsLeft);
    }

    public void Choose(int index)
    {
        var offer = Offer();
        if ((uint)index >= (uint)offer.Cards.Count) throw new ArgumentOutOfRangeException(nameof(index));
        Build.Apply(world, offer.Cards[index]);
        EndLevelUp();
    }

    public void Reroll()
    {
        Offer();
        if (S.RerollsLeft <= 0) return;
        S.RerollsLeft--;
        S.LevelOffer = MakeLevelOffer();
    }

    public void Skip()
    {
        Offer();
        Heal(world.Defs.Rules.LevelUp?.SkipHeal ?? 0f);
        EndLevelUp();
    }

    private void EndLevelUp()
    {
        S.PendingLevelUps--;
        S.LevelOffer = null;
        Closed();
    }

    // ── 상자 ─────────────────────────────────────────────────────

    /// <summary>
    /// 대기 상자 하나를 연다(기획 7·11.1): 진화 조건이 맞으면 첫 번은 진화, 나머지는 가진 무기·패시브 중 무작위 강화(만렙 제외), 강화할 것이 없으면 <c>단련</c>.
    /// </summary>
    public ChestReward OpenChest()
    {
        if (S.Chests.Count == 0) throw new InvalidOperationException("대기 중인 상자가 없다");
        var uses = Math.Max(1, S.Chests[0]);
        S.Chests.RemoveAt(0);
        var items = new List<ChestItem>(uses);
        var hero = world.Hero;
        for (var u = 0; u < uses; u++)
        {
            if (u == 0 && Build.FindEvolution(world) is { } evo)
            {
                Build.Evolve(world, evo.Slot, evo.Evo);
                var r = evo.Evo.Result.Def;
                items.Add(new ChestItem(ChestItemKind.Evolution, r.Id, r.Name, r.Icon, hero.Weapons[evo.Slot].Level));
                continue;
            }
            var pool = new List<Candidate>();
            for (var i = 0; i < WeaponSlots.SlotCount; i++)
                if (Build.Upgradable(hero.Weapons[i])) pool.Add(new Candidate(CardKind.WeaponUpgrade, hero.Weapons[i].Def!.Id, 1f));
            for (var i = 0; i < PassiveSlots.SlotCount; i++)
                if (hero.Passives[i].Def is { } p && hero.Passives[i].Level < p.MaxLevel) pool.Add(new Candidate(CardKind.PassiveUpgrade, p.Id, 1f));
            if (pool.Count == 0)
            {
                var t = Build.Temper(world);
                items.Add(new ChestItem(ChestItemKind.Temper, t?.Id ?? "", t?.Name ?? "", t?.Icon, 0));
                continue;
            }
            var pick = Build.Draw(world.Rng.LevelUp, pool, 1)[0];
            var card = Build.Card(world, pick);
            Build.Apply(world, card);
            items.Add(new ChestItem(pick.Kind == CardKind.WeaponUpgrade ? ChestItemKind.WeaponUpgrade : ChestItemKind.PassiveUpgrade,
                                    card.Id, card.Name, card.Icon, card.ToLevel));
        }
        HeroStats.Recompute(world);
        Closed();
        return new ChestReward(items);
    }

    // ── 보상 창(마법서·매혹의 성소) ─────────────────────────────────

    public LevelUpOffer RewardOffer()
    {
        if (S.Rewards.Count == 0) throw new InvalidOperationException("대기 중인 보상 창이 없다");
        return S.RewardOffer ??= MakeRewardOffer(S.Rewards[0]);
    }

    private LevelUpOffer MakeRewardOffer(PendingReward pending)
    {
        var count = world.Defs.Rules.LevelUp?.Cards ?? 3;
        var pool = RewardPool(pending.Reward);
        var picked = Build.Draw(world.Rng.LevelUp, pool, count);
        var levels = pending.Reward.Shrine?.PickWeapon is > 0 and var n ? n : 1;
        var cards = new List<LevelUpCard>(picked.Count);
        foreach (var c in picked) cards.Add(Build.Card(world, c, levels));
        return new LevelUpOffer(S.Level, cards, S.RerollsLeft, pending.Source);
    }

    /// <summary>보상 창 후보: 마법서 = 무기 카드(<c>cards</c> 가중), 매혹의 성소 = <c>pick_max_level</c> 이하 진화 아닌 가진 무기.</summary>
    private List<Candidate> RewardPool(RewardDef reward)
    {
        var pool = new List<Candidate>();
        if (reward.Kind == RewardKind.Shrine && reward.Shrine is { PickWeapon: > 0 } shrine)
        {
            var hero = world.Hero;
            for (var i = 0; i < WeaponSlots.SlotCount; i++)
                if (Build.NonEvolved(hero.Weapons[i]) && hero.Weapons[i].Level <= shrine.PickMaxLevel)
                    pool.Add(new Candidate(CardKind.WeaponUpgrade, hero.Weapons[i].Def!.Id, 1f));
            return pool;
        }
        var cards = reward.Cards ?? new RewardCardsDef { OwnedWeapon = 100, NewWeapon = 100 };
        Build.WeaponCandidates(world, pool, cards.OwnedWeapon, cards.NewWeapon);
        return pool;
    }

    public void ChooseReward(int index)
    {
        var offer = RewardOffer();
        if ((uint)index >= (uint)offer.Cards.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var pending = S.Rewards[0];
        var levels = pending.Reward.Shrine?.PickWeapon is > 0 and var n ? n : 1;
        Build.Apply(world, offer.Cards[index], levels);
        EndReward();
    }

    public void RerollReward()
    {
        RewardOffer();
        if (S.RerollsLeft <= 0) return;
        S.RerollsLeft--;
        S.RewardOffer = MakeRewardOffer(S.Rewards[0]);
    }

    public void SkipReward()
    {
        RewardOffer();
        Heal(S.Rewards[0].Reward.Skip?.Heal ?? 0f);
        EndReward();
    }

    private void EndReward()
    {
        S.Rewards.RemoveAt(0);
        S.RewardOffer = null;
        Closed();
    }

    /// <summary>문 열기 거르기(6.18 "거르기", 문을 여는 순간 한 번). v0.9: 회복 문·상인은 늘 참 — 체력·상인 조건은 <c>RewardDraw</c>(6.19).</summary>
    public bool RewardAvailable(RewardDef reward)
    {
        switch (reward.Kind)
        {
            case RewardKind.WeaponCards:
                return RewardPool(reward).Count > 0;
            case RewardKind.Shrine:
            {
                if (S.Shrines.Contains(reward.Id) || reward.Shrine is not { } shrine) return false;
                var hero = world.Hero;
                var nonEvolved = 0;
                var pickable = 0;
                for (var i = 0; i < WeaponSlots.SlotCount; i++)
                {
                    if (!Build.NonEvolved(hero.Weapons[i])) continue;
                    nonEvolved++;
                    if (hero.Weapons[i].Level <= shrine.PickMaxLevel) pickable++;
                }
                if (shrine.RandomWeapon != 0 && nonEvolved < 2) return false;
                if (shrine.PickWeapon != 0 && pickable < 1) return false;
                return true;
            }
            default:
                return true;
        }
    }

    // ── 보상 물체 줍기(6.18 "받는 길") ─────────────────────────────

    /// <summary>
    /// 문 보상 물체를 주운 순간. 마법서 = 보상 대기(줄 카드가 0장이면 <c>단련</c>), 보물 상자 = 무작위 무기 +n(없으면 <c>단련</c>)·물약 칸(v0.9.1)·금화 × <c>gold_scale</c>,
    /// 성소 = 무기 레벨·<c>Mods</c>·<c>Shrines</c>(매혹은 창 — 후보 0 이면 <c>단련</c>).
    /// </summary>
    public void CollectReward(RewardDef reward)
    {
        var hero = world.Hero;
        switch (reward.Kind)
        {
            case RewardKind.WeaponCards:
                if (RewardPool(reward).Count > 0) S.Rewards.Add(new PendingReward(reward, OfferSource.Tome));
                else Build.Temper(world);
                break;
            case RewardKind.WeaponBundle:
            {
                var bundle = reward.Bundle ?? new RewardBundleDef();
                Span<int> slots = stackalloc int[WeaponSlots.SlotCount];
                var n = 0;
                for (var i = 0; i < WeaponSlots.SlotCount; i++)
                    if (Build.Upgradable(hero.Weapons[i])) slots[n++] = i;
                if (n > 0) Build.AddWeaponLevels(world, slots[world.Rng.Drop.Index(n)], bundle.WeaponLevels);
                else Build.Temper(world);
                if (bundle.Potions > 0) HeroPotions.Add(world, bundle.Potions);   // v0.9.1: 물약 칸(가득이면 빠짐)
                if (bundle.Gold is [var lo, var hi])
                {
                    var scale = world.Run?.Act.Act.Rewards.GoldScale ?? world.Stage?.Rewards.GoldScale ?? 1f;
                    world.RunStats.Gold += (int)MathF.Round(world.Rng.Drop.RangeInclusive(lo, hi) * scale, MidpointRounding.AwayFromZero);
                }
                break;
            }
            case RewardKind.Shrine:
                CollectShrine(reward);
                break;
        }
        S.StatsDirty = true;
        HeroStats.Recompute(world);
        UpdateAwaiting();
    }

    private void CollectShrine(RewardDef reward)
    {
        var shrine = reward.Shrine ?? new ShrineDef();
        var hero = world.Hero;
        if (!S.Shrines.Contains(reward.Id)) S.Shrines.Add(reward.Id);
        if (shrine.AllWeapons != 0 || shrine.RandomWeapon != 0)
        {
            Span<int> slots = stackalloc int[WeaponSlots.SlotCount];
            var n = 0;
            for (var i = 0; i < WeaponSlots.SlotCount; i++)
                if (Build.NonEvolved(hero.Weapons[i]) && (shrine.RandomWeapon >= 0 || hero.Weapons[i].Level > 1)) slots[n++] = i;
            var odd = shrine.RandomWeapon != 0 && n > 0 ? slots[world.Rng.Drop.Index(n)] : -1;
            for (var i = 0; i < WeaponSlots.SlotCount; i++)
            {
                if (!Build.NonEvolved(hero.Weapons[i])) continue;
                Build.AddWeaponLevels(world, i, i == odd ? shrine.RandomWeapon : shrine.AllWeapons);
            }
        }
        foreach (var (k, v) in shrine.Stats) S.Mods[(int)k] += v;
        if (shrine.PickWeapon > 0)
        {
            if (RewardPool(reward).Count > 0) S.Rewards.Add(new PendingReward(reward, OfferSource.Shrine));
            else Build.Temper(world);
        }
    }

    // ── 공통 ─────────────────────────────────────────────────────

    private void Heal(float amount)
    {
        var hero = world.Hero;
        hero.Hp = MathF.Min(hero.MaxHp, hero.Hp + amount);
    }

    /// <summary>창이 닫힘: 다음 틱 밀어내기, 대기 순서대로 <c>AwaitingChoice</c>.</summary>
    private void Closed()
    {
        S.PushPending = true;
        HeroStats.Recompute(world);
        UpdateAwaiting();
    }

    /// <summary>대기 순서(레벨업 → 상자 → 보상)대로 <c>World.AwaitingChoice</c>. 판이 끝나 가면 바꾸지 않는다(EndTick 과 같은 조건).</summary>
    internal void UpdateAwaiting()
    {
        if (world.AwaitingChoice == ChoiceKind.None) return;   // 창이 열려 있지 않으면 EndTick 이 켠다
        world.AwaitingChoice = S.PendingLevelUps > 0 ? ChoiceKind.LevelUp
            : S.Chests.Count > 0 ? ChoiceKind.Chest
            : S.Rewards.Count > 0 ? ChoiceKind.Reward
            : ChoiceKind.None;
    }
}
