using DiabloDefenceRemastered.Core.Sim.Shops;
using DiabloDefenceRemastered.Tests.Combat;

namespace DiabloDefenceRemastered.Tests.Shops;

/// <summary>
/// WP3 단계 2 시험 도우미(장비·상인·물약 칸·상자 → 책): <see cref="CombatKit"/> 합성 정의 + <see cref="TestDefs"/> 의 장비·접사·상인 + 두 단계 보물 상자(<see cref="ChestBookId"/> — 책 <see cref="BookId"/>).
/// 실제 <c>Data/</c> 에 기대지 않는다(<c>rewards.json</c>·<c>pickups.json</c> 의 <c>drop</c>·<c>pk_book</c> 은 wp5 몫).
/// </summary>
public static class ShopKit
{
    public const string BookId = "t_book";
    public const string ChestBookId = "t_reward_chest_book";
    public const string ChestObjId = "obj_t_chest_book";

    /// <summary>합성 정의 한 벌: 전투(CombatKit) + 장비·접사·상인(보상 표의 상인 보상이 가리키는 같은 인스턴스) + 두 단계 보물 상자.</summary>
    public static GameDefs Defs()
    {
        var c = CombatKit.Defs();
        var basic = TestDefs.Basic();
        var book = new PickupDef { Id = BookId, Kind = PickupKind.Reward, Draw = new PickupDrawDef { Procedural = "gem", Size = new Int2(16, 16) } };
        var chestObj = TestDefs.RewardObject(ChestObjId);
        var chest = c.Rewards.Get(TestDefs.RewardChestId) with
        {
            Id = ChestBookId, Object = DefRef<PickupDef>.To(chestObj), Drop = DefRef<PickupDef>.To(book),
        };
        return new GameDefs
        {
            Rules = c.Rules, Sprites = c.Sprites, SpriteSets = c.SpriteSets, Projectiles = c.Projectiles, Weapons = c.Weapons, Passives = c.Passives,
            HeroClasses = c.HeroClasses, Tilesets = c.Tilesets, Maps = c.Maps, Stages = c.Stages, Enemies = c.Enemies,
            Rewards = new DefTable<RewardDef>([.. c.Rewards.All, chest]),
            Pickups = new DefTable<PickupDef>([.. c.Pickups.All, book, chestObj]),
            ItemBases = basic.ItemBases, ItemAffixes = basic.ItemAffixes,
            Merchants = new DefTable<MerchantDef>(TestDefs.MerchantsOf(c.Rewards.All)),
        };
    }

    /// <summary>
    /// 합성 방 월드(<c>WithRoom</c>, 실제 전투 모듈, 표시 이벤트 켬) 단계 <paramref name="phase"/>. <paramref name="merchantId"/> 를 주면 그 상인 방 —
    /// <c>RunSession.Enter</c> 처럼 <c>ShopStock.Create</c> 로 물건을 정하고, 상인 자리(<c>RewardSpot</c>)를 주인공 오른쪽 100px 에 둔다.
    /// </summary>
    public static TestWorld Room(RoomPhase phase = RoomPhase.Reward, string? merchantId = null, GameDefs? defs = null, ulong seed = 1)
    {
        defs ??= Defs();
        var reward = merchantId is null ? null : defs.Rewards.All.First(r => r.Merchant?.Id == merchantId);
        var t = new TestWorldBuilder().WithSeed(seed).WithDefs(defs).WithRoom(phase, reward: reward, fakeSpawner: false)
            .WithModules(CombatKit.Modules()).WithSystem(SimPhase.DeathHooks, new FakeDeathHooks()).WithPresentation().Build();
        var w = t.World;
        if (merchantId is not null)
        {
            var hero = w.Hero;
            w.Room!.Shop = ShopStock.Create(defs.Merchants.Get(merchantId), w.Room.Plan.Act, w.Room.Plan.Number, w.Run!.Seed, defs,
                                            new ShopPotions(hero.Potions, hero.PotionSlotsLevel, hero.PotionSpeedLevel));
            w.Room.RewardSpot = hero.Pos + new Vector2(100f, 0f);
        }
        return t;
    }

    /// <summary>주인공을 상인 자리에 세우고 한 틱(닿음 → 창이 열림). 입력 잠금은 남는다.</summary>
    public static void Touch(TestWorld t)
    {
        t.PlaceHero(t.World.Room!.RewardSpot);
        t.AppTick();
    }

    /// <summary>열린 창의 입력 잠금을 빈 명령으로 흘려보낸다.</summary>
    public static void PassLock(TestWorld t)
    {
        while (t.World.Combat.ShopLockTicks > 0) t.AppTick();
    }

    /// <summary>닿아서 열고 잠금까지 넘긴다.</summary>
    public static ShopState OpenReady(TestWorld t)
    {
        Touch(t);
        Assert.True(t.World.Room!.Shop!.Open);
        PassLock(t);
        return t.World.Room.Shop;
    }

    public static PlayerCommand Press(CommandButtons buttons) => new(Vector2.Zero, buttons);

    /// <summary>표시 신호 중 <paramref name="kind"/> 들(쌓인 순서).</summary>
    public static List<CueEvent> Cues(World w, CueKind kind)
    {
        var list = new List<CueEvent>();
        foreach (var c in w.Presentation.Cues.AsSpan())
            if (c.Kind == kind) list.Add(c);
        return list;
    }

    /// <summary>장비 하나(굴리지 않은 — 시험이 값을 정함).</summary>
    public static EquipItem Item(string baseId, RolledAffix? prefix = null, RolledAffix? suffix = null, int price = 100) => new(baseId, prefix, suffix, price);
}
