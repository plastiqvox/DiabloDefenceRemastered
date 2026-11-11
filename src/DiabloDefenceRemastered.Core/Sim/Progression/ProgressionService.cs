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
        return new LevelUpOffer(S.Level, cards, S.RerollsLeft, pending.Source, pending.Reward);   // v0.9: 창이 건너뛰기 문구·성소 카드 둘째 줄에 씀
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
    /// <paramref name="source"/>(v0.9.1 V8): 두 단계 보상(<c>RewardDef.Drop</c>)의 떨어진 물체(책)면 올릴 무기·금화는 상자를 연 틱에 정한 값(<see cref="Pickup.RewardChosen"/>·<see cref="Pickup.Value"/>) —
    /// 다시 뽑지 않는다. 그 사이 그 칸이 만렙·진화가 되었으면 <c>단련</c>.
    /// </summary>
    public void CollectReward(RewardDef reward, Pickup? source = null)
    {
        switch (reward.Kind)
        {
            case RewardKind.WeaponCards:
                if (RewardPool(reward).Count > 0) S.Rewards.Add(new PendingReward(reward, OfferSource.Tome));
                else Build.Temper(world);
                break;
            case RewardKind.WeaponBundle:
            {
                var bundle = reward.Bundle ?? new RewardBundleDef();
                var (slot, gold) = source is { RewardStage: RewardPickupStage.Dropped } dropped ? (dropped.RewardChosen, dropped.Value) : RollBundle(reward);
                if (slot >= 0 && Build.Upgradable(world.Hero.Weapons[slot])) Build.AddWeaponLevels(world, slot, bundle.WeaponLevels);
                else Build.Temper(world);
                if (bundle.Potions > 0) HeroPotions.Add(world, bundle.Potions);   // v0.9.1: 물약 칸(가득이면 빠짐)
                Wallet.Earn(world, gold);                                         // v0.9: 지갑과 얻은 금화 둘 다
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

    /// <summary>책이 떨어지는 자리: 상자에서 이 거리(px — 기획 11.4 "상자 바로 앞 30px").</summary>
    public const float DropDistance = 30f;

    /// <summary>
    /// 두 단계 보상 물체(v0.9.1 V8 — <c>RewardDef.Drop</c>, 보물 상자 → 책)에 처음 닿은 틱: 상자 <see cref="Pickup.RewardStage"/> = Dropped(열린 상자로 남고 문을 막지 않음),
    /// 올릴 무기 칸·금화를 <b>이 틱에</b> 방 난수 <c>Drop</c> 으로 정해(한 번에 받던 때와 같은 차례·같은 수 — <see cref="RollBundle"/>) 상자와 책의 <see cref="Pickup.RewardChosen"/>·
    /// <see cref="Pickup.Value"/>(금화)에 두고, 책을 상자 너머 <see cref="DropDistance"/> 에 <c>SpawnPickup</c>(보상 줄 → <c>GatesExit</c>) + <c>CueKind.ChestDrop</c>(대상 = 그 <c>RewardDef</c>, 자리 = 책).
    /// 책 자리는 주인공 → 상자 방향(없으면 바라보는 방향)으로 30px, 맵 밖·<c>clearance</c> 미달이면 <c>RewardSpot.Row</c> 의 당김 규칙으로 상자 쪽으로 당긴다.
    /// </summary>
    public void DropReward(Pickup chest)
    {
        ArgumentNullException.ThrowIfNull(chest);
        if (chest.Reward is not { Drop: { } drop } reward) throw new ArgumentException("두 단계 보상 물체가 아니다(RewardDef.Drop 없음)", nameof(chest));
        var (slot, gold) = reward.Kind == RewardKind.WeaponBundle ? RollBundle(reward) : (-1, 0);
        chest.RewardStage = RewardPickupStage.Dropped;
        chest.RewardChosen = slot;
        chest.Value = gold;
        chest.GatesExit = false;

        var hero = world.Hero;
        var away = chest.Pos - hero.Pos;
        var dir = away.LengthSquared() > 1e-6f ? Vector2.Normalize(away)
            : hero.Facing.LengthSquared() > 1e-6f ? Vector2.Normalize(hero.Facing) : new Vector2(0f, 1f);
        var pos = DropSpot(world, chest.Pos, dir);
        var def = drop.IsBound ? drop.Def : world.Defs.Pickups.Get(drop.Id);
        var id = world.Spawner.SpawnPickup(new PickupSpawn(def, pos, Reward: reward));
        if (world.Pickups.Get(id) is { } book)
        {
            book.RewardStage = RewardPickupStage.Dropped;
            book.RewardChosen = slot;
            book.Value = gold;
        }
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.ChestDrop, pos, reward));
    }

    /// <summary>
    /// <paramref name="from"/> 에서 <paramref name="dir"/> 로 <see cref="DropDistance"/> 인 자리. <c>RewardSpot.Row</c> 에 두 자리 줄(간격 2 × 30, 옆 방향 = <paramref name="dir"/>)을 세우면
    /// 둘째 자리가 그 자리이고, 맵 밖·<c>clearance</c> 미달이면 같은 규칙으로 <paramref name="from"/> 쪽으로 당겨진다(보상 자리와 한 규칙 — 6.19). <c>rules.rooms</c> 가 없으면 당기지 않는다.
    /// </summary>
    private static Vector2 DropSpot(World world, Vector2 from, Vector2 dir)
    {
        if (world.Defs.Rules.Rooms?.RewardSpot is not { } spot) return from + dir * DropDistance;
        var facing = new Vector2(dir.Y, -dir.X);   // Row 의 옆 방향 = (−facing.Y, facing.X) = dir
        return RewardSpot.Row(world.Nav, from, facing, 2, spot with { Spacing = 2f * DropDistance })[1];
    }

    /// <summary>
    /// 보물 상자 꾸러미의 뽑기(방 난수 <c>Drop</c> — 차례 고정): 올릴 무기 = 만렙·진화 아닌 가진 무기 중 균등(없으면 −1 = <c>단련</c>, 난수 안 씀),
    /// 금화 = <c>gold</c> [최소, 최대] × 액트 <c>gold_scale</c>(없으면 0, 난수 안 씀). 한 번에 받는 상자는 줍는 틱에, 두 단계 상자는 여는 틱에 부른다.
    /// </summary>
    private (int Slot, int Gold) RollBundle(RewardDef reward)
    {
        var bundle = reward.Bundle ?? new RewardBundleDef();
        var hero = world.Hero;
        Span<int> slots = stackalloc int[WeaponSlots.SlotCount];
        var n = 0;
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
            if (Build.Upgradable(hero.Weapons[i])) slots[n++] = i;
        var slot = n > 0 ? slots[world.Rng.Drop.Index(n)] : -1;
        var gold = 0;
        if (bundle.Gold is [var lo, var hi])
        {
            var scale = world.Run?.Act.Act.Rewards.GoldScale ?? world.Stage?.Rewards.GoldScale ?? 1f;
            gold = (int)MathF.Round(world.Rng.Drop.RangeInclusive(lo, hi) * scale, MidpointRounding.AwayFromZero);
        }
        return (slot, gold);
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
