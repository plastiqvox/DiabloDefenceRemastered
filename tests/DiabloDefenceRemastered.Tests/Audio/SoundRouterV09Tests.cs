using DiabloDefenceRemastered.Core.Audio;
using DiabloDefenceRemastered.Tests.Shops;
using static DiabloDefenceRemastered.Tests.Audio.AudioTestDefs;

namespace DiabloDefenceRemastered.Tests.Audio;

/// <summary>
/// v0.9 새 신호의 소리 키(wp4 — WP8 단계 2, 설계 7.9·6.15): 못 마심 <c>ui.potion_denied</c>·회복 문 <c>ui.heal_on_enter</c>·상인 인사 <c>rewards.&lt;상인 보상&gt;.open</c>·
/// 사기 <c>ui.shop_buy</c> + 장비 끼우기 <c>ui.equip_&lt;칸&gt;</c>(없으면 <c>ui.equip</c>, 페핀 카드는 끼우기 없음)·못 삼 <c>ui.shop_denied</c>·상자 → 책 <c>ui.chest_drop</c>,
/// 상자를 열 때 같은 틱의 책 <c>PickupDropped</c> 는 소리를 내지 않음(겹치지 않게). 대상 형이 약속과 다르면 소리 없음.
/// </summary>
public sealed class SoundRouterV09Tests
{
    private static readonly SoundDef Gold = Sound("snd_gold_buy", "ui"), Denied = Sound("snd_no", "ui"), Heal = Sound("snd_heal", "ui"), Flip = Sound("snd_flip", "ui"),
        Grab = Sound("snd_grab", "ui"), Cap = Sound("snd_cap", "ui"), Light = Sound("snd_larm", "ui"), Heavy = Sound("snd_harm", "ui"), Ring = Sound("snd_ring", "ui"),
        Greet = Sound("snd_greet", "ui"), BookDrop = Sound("snd_book_drop", "pickup");

    private static readonly GameDefs D = Build();

    private static GameDefs Build()
    {
        var s = ShopKit.Defs();
        DefRef<SoundDef> R(SoundDef d) => DefRef<SoundDef>.To(d);
        return new GameDefs
        {
            Rules = new RulesDef { Audio = Rules() },
            Rewards = s.Rewards, Merchants = s.Merchants, ItemBases = s.ItemBases, Pickups = s.Pickups,
            Sounds = new DefTable<SoundDef>([Gold, Denied, Heal, Flip, Grab, Cap, Light, Heavy, Ring, Greet, BookDrop]),
            SoundBindings = new SoundBindingsDef
            {
                Ui = new Dictionary<string, DefRef<SoundDef>>
                {
                    [UiKeys.ShopBuy] = R(Gold), [UiKeys.ShopDenied] = R(Denied), [UiKeys.PotionDenied] = R(Denied), [UiKeys.HealOnEnter] = R(Heal),
                    [UiKeys.ChestDrop] = R(Flip), [UiKeys.EquipOther] = R(Grab), [UiKeys.EquipHead] = R(Cap), [UiKeys.EquipBody] = R(Light),
                    [UiKeys.EquipBodyHeavy] = R(Heavy), [UiKeys.EquipRing] = R(Ring),
                },
                Rewards = new Dictionary<string, RewardSoundsDef> { [TestDefs.RewardSmithId] = new() { Open = R(Greet) } },
                Pickups = new Dictionary<string, PickupSoundsDef> { [ShopKit.BookId] = new() { Drop = R(BookDrop) } },
            },
        };
    }

    private static World W() => new TestWorldBuilder().WithDefs(TestDefs.Basic()).Build().World;

    private static string? Id(CueKind kind, IDef? subject)
    {
        var router = new SoundRouter(D);
        return router.TryCue(new CueEvent(3, kind, new Vector2(5, 5), subject), W(), out var r) ? r.Def.Id : null;
    }

    private static string? EquipId(IDef? subject) =>
        new SoundRouter(D).TryEquip(new CueEvent(3, CueKind.ShopBuy, default, subject), out var r) ? r.Def.Id : null;

    [Fact]
    public void Potion_and_heal_cues_use_ui_keys()
    {
        Assert.Equal("snd_no", Id(CueKind.PotionDenied, null));
        Assert.Equal("snd_heal", Id(CueKind.HealOnEnter, D.Rewards.Get(TestDefs.RewardPotionId)));
    }

    [Fact]
    public void Shop_open_plays_greeting_of_reward_pointing_at_merchant()
    {
        Assert.Equal("snd_greet", Id(CueKind.ShopOpen, D.Merchants.Get(TestDefs.SmithId)));
        Assert.Null(Id(CueKind.ShopOpen, D.Merchants.Get(TestDefs.WitchId)));           // 연결 없음 = 소리 없음
        Assert.Null(Id(CueKind.ShopOpen, D.ItemBases.Get("t_cap")));                    // 대상 형이 약속과 다름
    }

    [Fact]
    public void Shop_buy_gold_plus_equip_sound_by_slot_and_weight()
    {
        Assert.Equal("snd_gold_buy", Id(CueKind.ShopBuy, D.ItemBases.Get("t_cap")));
        Assert.Equal("snd_gold_buy", Id(CueKind.ShopBuy, D.Merchants.Get(TestDefs.HealerId)));   // 페핀 카드
        Assert.Equal("snd_cap", EquipId(D.ItemBases.Get("t_helm")));
        Assert.Equal("snd_larm", EquipId(D.ItemBases.Get("t_ring_mail")));                     // 경·중 갑옷
        Assert.Equal("snd_harm", EquipId(D.ItemBases.Get("t_breast_plate")));                  // 판금
        Assert.Equal("snd_ring", EquipId(D.ItemBases.Get("t_ring")));
        Assert.Equal("snd_grab", EquipId(D.ItemBases.Get("t_staff")));                         // 지팡이 키가 없으면 equip
        Assert.Null(EquipId(D.Merchants.Get(TestDefs.HealerId)));                              // 페핀 카드는 끼우지 않음
        Assert.Equal("snd_no", Id(CueKind.ShopDenied, D.Merchants.Get(TestDefs.SmithId)));
    }

    [Fact]
    public void Chest_drop_sounds_once_book_dropped_cue_is_silent()
    {
        var chest = D.Rewards.Get(ShopKit.ChestBookId);
        var book = D.Pickups.Get(ShopKit.BookId);
        Assert.Equal("snd_flip", Id(CueKind.ChestDrop, chest));
        Assert.Null(Id(CueKind.PickupDropped, book));                                          // 같은 틱의 책 떨어짐은 ChestDrop 하나로

        // 한 버퍼에 두 신호가 같이 오면 요청은 하나(+ 장비 끼우기는 ShopBuy 에만)
        var buffer = new PresentationBuffer { Enabled = true };
        buffer.Cue(new CueEvent(3, CueKind.PickupDropped, Vector2.Zero, book));
        buffer.Cue(new CueEvent(3, CueKind.ChestDrop, Vector2.Zero, chest));
        buffer.Cue(new CueEvent(3, CueKind.ShopBuy, Vector2.Zero, D.ItemBases.Get("t_breast_plate")));
        var mixer = Mixer();
        new SoundRouter(D).Route(buffer, W(), mixer);
        Assert.Equal(3, mixer.Counts.Requested);                                               // 책 떨어짐 0 + 상자 1 + 금화 1 + 끼우기 1
    }
}
