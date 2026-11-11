using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Progression;

namespace DiabloDefenceRemastered.Core.Sim.Shops;

/// <summary>
/// 상인 창 본문(v0.9 V3·V5·V11, 설계서 6.19 "상인 방 상태와 구매 명령" — WP3 <see cref="IShop"/> 구현, <c>World.ShopService</c>). 상태는 방 수명 <c>Room.Shop</c>(<see cref="ShopState"/>)과
/// <c>World.Combat</c>(입력 잠금·다시 열기 조건)에만 있고 이 객체는 월드마다 만든다(<see cref="ProgressionService"/> 와 같은 모양).
/// <list type="bullet">
/// <item>열기(<see cref="TryOpen"/> — <c>Pickups</c> 단계 끝): 방 단계 보상·문, <c>Room.Shop</c> 있음·닫힘, 선택 대기 없음, 주인공이 <c>Room.RewardSpot</c>(상인이 서는 자리)에서
/// 주인공 반경 + <see cref="OpenReach"/> 안 → <c>Open</c> + <c>CueKind.ShopOpen</c>(대상 = <c>MerchantDef</c>) + 입력 잠금. 닫은 뒤에는 그 거리 밖으로 나갔다 다시 들어와야 연다.</item>
/// <item>열린 동안: 판 틱이 돌지 않고(<c>StageStepper</c>) 명령 비트가 <see cref="ShopInput"/> 을 거쳐 여기로 온다 — 앱 쪽 창은 이것을 직접 부르지 않는다(사람·자동 조작·재생이 같은 명령 길).</item>
/// <item><see cref="Buy"/>: 지갑 ≥ 값이면 지갑 −값·<c>GoldSpent</c> +값. 장비는 그 칸에 끼움(반지는 빈 칸 먼저, 둘 다 차면 <c>RingPending</c> — 값은 바꿀 반지를 고를 때),
/// 페핀 카드는 주머니·빠른 물약 단계 +1·물약 채우기 뒤 세 카드를 <c>ShopStock.PotionCard</c> 로 다시. <c>CueKind.ShopBuy</c>(대상 = 산 장비의 <c>ItemBaseDef</c>, 페핀 카드면 <c>MerchantDef</c>).
/// 모자라거나 살 수 없는 카드면 아무 일 없음 + <c>CueKind.ShopDenied</c>(대상 = <c>MerchantDef</c>).</item>
/// <item><see cref="Reroll"/>: <c>ShopStock.RerollPrice</c>(30·60·90…)를 지갑에서 → <c>ShopStock.Reroll</c>(산 카드는 자리를 지킴). 페핀(<c>rerollable: false</c>)은 아무 일 없음.</item>
/// <item><see cref="Leave"/>: <c>Open = false; Visited = true</c>(<c>RoomFlow</c> 가 <c>Visited</c> 를 보고 문을 연다).</item>
/// </list>
/// 반지 바꾸기 중(<c>RingPending ≥ 0</c>)에는 <see cref="Buy"/>·<see cref="Reroll"/> 을 받지 않는다 — <see cref="ReplaceRing"/> 이나 <see cref="CancelRing"/>(건너뛰기 = 사지 않음, 기획 12.8)만.
/// </summary>
public sealed class ShopService(World world) : IShop
{
    /// <summary>상인 여는 거리 = 주인공 반경 + 이 값(px, <c>Room.RewardSpot</c> 기준).</summary>
    public const float OpenReach = 20f;

    private CombatState S => world.Combat;

    public ShopState? Shop => world.Room?.Shop;

    /// <summary>이 방 상인 정의(상인 방이 아니면 null).</summary>
    public MerchantDef? Merchant => Shop is { } shop && world.Defs.Merchants.TryGet(shop.MerchantId, out var m) ? m : null;

    /// <summary>
    /// 열기 판정(<c>Pickups</c> 단계 끝 — 이번 틱 이동이 끝난 자리). 닫혀 있고 보상·문 단계이고 대기가 없을 때, 여는 거리 안이면 연다. 여는 거리 밖에 서면 다음에 다시 열 수 있다.
    /// </summary>
    public void TryOpen()
    {
        if (world.Room is not { Phase: RoomPhase.Reward or RoomPhase.Doors, Shop: { Open: false } shop } room) return;
        var hero = world.Hero;
        if (hero.Dead || world.Outcome != RunOutcome.Running) return;
        var reach = hero.Radius + OpenReach;
        if (Vector2.DistanceSquared(hero.Pos, room.RewardSpot) > reach * reach)
        {
            S.ShopAway = true;
            return;
        }
        if (!S.ShopAway || world.AwaitingChoice != ChoiceKind.None) return;
        if (world.Progression is { } p && (p.PendingLevelUps > 0 || p.PendingChests > 0 || p.PendingRewards > 0)) return;
        shop.Open = true;
        shop.RingPending = -1;
        S.ShopAway = false;
        S.ShopLockTicks = (int)MathF.Round((world.Defs.Rules.LevelUp?.InputLock ?? 0f) * World.TicksPerSecond, MidpointRounding.AwayFromZero);
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.ShopOpen, room.RewardSpot, Merchant));
    }

    public void Buy(int card)
    {
        if (Shop is not { Open: true, RingPending: < 0 } shop || (uint)card >= (uint)shop.Cards.Count) return;
        var c = shop.Cards[card];
        var hero = world.Hero;
        if (c.Bought || hero.Gold < c.Price || c.Kind == ShopCardKind.Equip && c.Item is null)
        {
            Denied();
            return;
        }
        if (c.Kind == ShopCardKind.Potion)
        {
            Wallet.TrySpend(world, c.Price);
            BuyPotion(shop, c.Upgrade);
            world.Presentation.Cue(new CueEvent(world.Tick, CueKind.ShopBuy, hero.Pos, Merchant));
            return;
        }

        var item = c.Item!;
        var b = world.Defs.ItemBases.Get(item.BaseId);
        var slot = HeroEquipment.SlotOf(b.Kind);
        if (b.Kind == EquipKind.Ring)
        {
            if (hero.Equipment[(int)EquipSlot.Ring1] is null) slot = EquipSlot.Ring1;
            else if (hero.Equipment[(int)EquipSlot.Ring2] is null) slot = EquipSlot.Ring2;
            else
            {
                shop.RingPending = card;   // 창이 "바꿀 반지" 둘로 — 고르면 산다(기획 12.8)
                return;
            }
        }
        EquipCard(shop, card, slot);
    }

    public void ReplaceRing(int ring)
    {
        if (Shop is not { Open: true, RingPending: >= 0 } shop || ring is < 0 or > 1) return;
        var card = shop.RingPending;
        shop.RingPending = -1;
        if ((uint)card >= (uint)shop.Cards.Count || shop.Cards[card] is not { Bought: false, Item: not null } c || world.Hero.Gold < c.Price)
        {
            Denied();
            return;
        }
        EquipCard(shop, card, ring == 0 ? EquipSlot.Ring1 : EquipSlot.Ring2);
    }

    /// <summary>반지 바꾸기를 그만둔다(건너뛰기 = 사지 않음 — 창은 열린 채). <see cref="IShop"/> 밖 — <see cref="ShopInput"/> 의 <c>Skip</c>.</summary>
    public void CancelRing()
    {
        if (Shop is { Open: true } shop) shop.RingPending = -1;
    }

    public void Reroll()
    {
        if (Shop is not { Open: true, RingPending: < 0 } shop || Merchant is not { } m) return;
        if (!m.Rerollable || m.Stock != MerchantStock.Equipment || world.Run is not { } run || world.Room is not { } room) return;
        var rules = world.Defs.Rules.Equipment ?? throw new InvalidOperationException($"장비 상인 '{m.Id}' — rules.equipment 가 없다(6.19)");
        if (!Wallet.TrySpend(world, ShopStock.RerollPrice(rules, shop.Rerolls)))
        {
            Denied();
            return;
        }
        ShopStock.Reroll(shop, m, room.Plan.Act, room.Plan.Number, run.Seed, world.Defs);
    }

    public void Leave()
    {
        if (Shop is not { } shop) return;
        shop.Open = false;
        shop.Visited = true;
        shop.RingPending = -1;
        S.ShopLockTicks = 0;
        S.ShopAway = false;   // 그 자리에 머물면 다시 열지 않는다 — 밖으로 나갔다 와야
    }

    private void EquipCard(ShopState shop, int card, EquipSlot slot)
    {
        var c = shop.Cards[card];
        var item = c.Item!;
        Wallet.TrySpend(world, c.Price);
        HeroEquipment.Equip(world, slot, item);
        shop.Cards[card] = c with { Bought = true };
        world.Presentation.Cue(new CueEvent(world.Tick, CueKind.ShopBuy, world.Hero.Pos, world.Defs.ItemBases.Get(item.BaseId)));
    }

    /// <summary>페핀 카드 하나: 주머니 +1·빠른 물약 +1·채우기(칸 가득), 그 뒤 세 카드의 값·다 올림을 지금 물약 칸 상태로 다시.</summary>
    private void BuyPotion(ShopState shop, PotionUpgrade upgrade)
    {
        var hero = world.Hero;
        switch (upgrade)
        {
            case PotionUpgrade.Slots: hero.PotionSlotsLevel++; break;
            case PotionUpgrade.Speed: hero.PotionSpeedLevel++; break;
            default: hero.Potions = HeroPotions.Capacity(world); break;
        }
        if (Merchant is not { } m || world.Defs.Rules.Hero?.Potions is not { } rules) return;
        var state = new ShopPotions(hero.Potions, hero.PotionSlotsLevel, hero.PotionSpeedLevel);
        for (var i = 0; i < shop.Cards.Count && i < m.Upgrades.Count; i++) shop.Cards[i] = ShopStock.PotionCard(m.Upgrades[i], rules, state);
    }

    private void Denied() => world.Presentation.Cue(new CueEvent(world.Tick, CueKind.ShopDenied, world.Hero.Pos, Merchant));
}

/// <summary>
/// 상인 창이 열린 동안의 앱 틱(v0.9, 6.19 — <c>StageStepper.AppTick</c> 이 판 틱 대신 부른다). 판 틱은 돌지 않는다(시간 멈춤 — <c>World.Tick</c> 그대로).
/// 열린 뒤 <c>rules.level_up.input_lock</c> 초(앱 틱으로 셈)는 고르기 버튼을 무시한다. 그 뒤 버튼 하나(이 차례로 첫 것): <c>Choice1~3</c> = 반지 바꾸기 중이면
/// <see cref="ShopService.ReplaceRing"/>(1·2 만), 아니면 <see cref="ShopService.Buy"/> / <c>Reroll</c> = <see cref="ShopService.Reroll"/> /
/// <c>Skip</c> = 반지 바꾸기 중이면 <see cref="ShopService.CancelRing"/>(사지 않음), 아니면 <see cref="ShopService.Leave"/>.
/// <c>Drink</c> 는 잠금과 상관없이 <c>CueKind.PotionDenied</c>(창이 열린 동안은 못 마심).
/// </summary>
public static class ShopInput
{
    /// <summary>창이 열려 있으면 명령을 처리하고 참(판 틱을 돌리지 않음), 아니면 거짓.</summary>
    public static bool AppTick(World world, PlayerCommand command)
    {
        if (world.Room?.Shop is not { Open: true } shop) return false;
        var buttons = command.Buttons;
        if ((buttons & CommandButtons.Drink) != 0) HeroPotions.Denied(world);
        var s = world.Combat;
        if (s.ShopLockTicks > 0)
        {
            s.ShopLockTicks--;
            return true;
        }
        if (world.ShopService is not { } service) return true;
        var choice = (buttons & CommandButtons.Choice1) != 0 ? 0
            : (buttons & CommandButtons.Choice2) != 0 ? 1
            : (buttons & CommandButtons.Choice3) != 0 ? 2 : -1;
        if (choice >= 0)
        {
            if (shop.RingPending < 0) service.Buy(choice);
            else if (choice < 2) service.ReplaceRing(choice);
        }
        else if ((buttons & CommandButtons.Reroll) != 0) service.Reroll();
        else if ((buttons & CommandButtons.Skip) != 0)
        {
            if (shop.RingPending >= 0) shop.RingPending = -1;   // 건너뛰기 = 사지 않음(창은 열린 채 — ShopService.CancelRing 과 같음)
            else service.Leave();
        }
        return true;
    }
}
