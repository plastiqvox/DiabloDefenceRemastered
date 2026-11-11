using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>
/// 보상 물체 둘레 아이콘 <see cref="RewardOsd"/>(v0.9.1 V8, 설계서 6.19, 기획 11.4 표 — 단계 2 wp5): 보물 상자(닫힘 = 아직 안 정함, 연 뒤 책 = 정한 무기·금화, 물약 칸이 가득이면 흐림),
/// 마법서 실루엣 셋, 정예 상자 진화/<c>?</c>, 아이콘 상한·자리·흔들림. 합성 정의(<see cref="ShopKit"/> — 두 단계 보물 상자).
/// </summary>
public class RewardOsdTests
{
    private static (TestWorld T, RewardDef Reward) World(GameDefs? defs = null)
    {
        defs ??= ShopKit.Defs();
        var t = CombatKit.World(defs, 1, b => b.WithPresentation());
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 2);
        CombatKit.SetWeapon(w, 2, CombatKit.Fb, 1);
        w.Hero.Facing = new Vector2(0f, 1f);
        return (t, defs.Rewards.TryGet(ShopKit.ChestBookId, out var r) ? r : defs.Rewards.Get(TestDefs.RewardChestId));
    }

    private static Pickup Book(World w) => w.Pickups.Items.ToArray().Single(p => !p.Removed && p.Def.Id == ShopKit.BookId);

    [Fact]
    public void TreasureChest_ClosedUnknown_BookShowsChosenWeaponPotionGold_OpenedChestEmpty()
    {
        var (t, reward) = World();
        var w = t.World;
        var chest = CombatKit.Drop(w, ShopKit.ChestObjId, w.Hero.Pos + new Vector2(200f, 0f), reward: reward);

        var closed = RewardOsd.Icons(chest, w);
        Assert.Equal([OsdIconKind.Unknown, OsdIconKind.Potion, OsdIconKind.Gold], closed.Select(i => i.Kind));
        Assert.Equal("무기 +1", closed[0].Label);
        Assert.Equal("금화 30~60", closed[2].Label);                                     // 아직 안 굴림 — 범위 × gold_scale(1)
        Assert.False(closed[1].Dim);

        t.PlaceHero(chest.Pos);
        t.Step();                                                                        // 상자를 엶 → 책
        Assert.Equal(RewardPickupStage.Dropped, chest.RewardStage);
        Assert.Empty(RewardOsd.Icons(chest, w));                                         // 열린 상자는 비었다(받을 것은 책에)

        var book = Book(w);
        var icons = RewardOsd.Icons(book, w);
        Assert.Equal([OsdIconKind.Weapon, OsdIconKind.Potion, OsdIconKind.Gold], icons.Select(i => i.Kind));
        var weapon = w.Hero.Weapons[book.RewardChosen].Def!;                              // 상자를 연 틱에 정한 무기(RewardChosen)
        Assert.Same(weapon.Icon, icons[0].Icon);
        Assert.Equal($"{weapon.Name} +1", icons[0].Label);
        Assert.Equal($"금화 {book.Value}", icons[2].Label);
        Assert.Equal("물약", icons[1].Label);

        // 물약 칸이 가득이면 흐리게 + 가득
        w.Hero.Potions = HeroPotions.Capacity(w);
        var full = RewardOsd.Icons(book, w);
        Assert.True(full[1].Dim);
        Assert.Equal("가득", full[1].Label);

        // 정한 무기가 없음(−1) → 단련
        book.RewardChosen = -1;
        Assert.Equal(OsdIconKind.Temper, RewardOsd.Icons(book, w)[0].Kind);
    }

    [Fact]
    public void Tome_ThreeCardSilhouettes_NonRewardPickupsNone()
    {
        var (t, _) = World();
        var w = t.World;
        var tome = w.Defs.Rewards.Get(TestDefs.RewardTomeId);
        var p = CombatKit.Drop(w, tome.Object!.Id, w.Hero.Pos + new Vector2(200f, 0f), reward: tome);
        var icons = RewardOsd.Icons(p, w);
        Assert.Equal(3, icons.Count);
        Assert.All(icons, i => Assert.Equal(OsdIconKind.Card, i.Kind));

        Assert.Empty(RewardOsd.Icons(CombatKit.Drop(w, "t_gold", w.Hero.Pos + new Vector2(300f, 0f), 5), w));    // 적이 떨군 금화·물약: 그림으로 충분
        Assert.Empty(RewardOsd.Icons(CombatKit.Drop(w, "t_potion", w.Hero.Pos + new Vector2(300f, 40f)), w));
    }

    [Fact]
    public void EliteChest_EvolutionIconIfReady_ElseThreeQuestionMarks()
    {
        var mm = CombatKit.MagicMissile();
        var power = CombatKit.Passive(CombatKit.Power, StatKey.DamagePct, 0.1f);
        var storm = new WeaponDef { Id = "t_storm", Name = "비전 폭풍", BaseWeapon = DefRef<WeaponDef>.To(mm), Icon = new IconDef { Sprite = new DefRef<SpriteDef>("icon_storm") } };
        var basic = ShopKit.Defs();
        var defs = new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Projectiles = basic.Projectiles,
            Weapons = new DefTable<WeaponDef>([.. basic.Weapons.All, storm]), Passives = basic.Passives, HeroClasses = basic.HeroClasses,
            Tilesets = basic.Tilesets, Maps = basic.Maps, Stages = basic.Stages, Enemies = basic.Enemies, Rewards = basic.Rewards, Pickups = basic.Pickups,
            ItemBases = basic.ItemBases, ItemAffixes = basic.ItemAffixes, Merchants = basic.Merchants,
            Evolutions = new DefTable<EvolutionDef>([new EvolutionDef { Weapon = DefRef<WeaponDef>.To(mm), Passive = DefRef<PassiveDef>.To(power), Result = DefRef<WeaponDef>.To(storm) }]),
        };
        var (t, _) = World(defs);
        var w = t.World;
        var elite = w.Defs.Pickups.All.First(p => p.Kind == PickupKind.Chest);
        var chest = CombatKit.Drop(w, elite.Id, w.Hero.Pos + new Vector2(200f, 0f), 3);

        var plain = RewardOsd.Icons(chest, w);
        Assert.Equal(3, plain.Count);
        Assert.All(plain, i => Assert.Equal(OsdIconKind.Unknown, i.Kind));

        CombatKit.SetWeapon(w, 0, CombatKit.Mm, w.Defs.Weapons.Get(CombatKit.Mm).MaxLevel);   // 만렙 + 짝 패시브 → 진화 가능
        w.Hero.Passives[0] = new PassiveSlot { Def = w.Defs.Passives.Get(CombatKit.Power), Level = 1 };
        var evo = Assert.Single(RewardOsd.Icons(chest, w));
        Assert.Equal(OsdIconKind.Evolution, evo.Kind);
        Assert.Same(storm.Icon, evo.Icon);
        Assert.Equal("비전 폭풍", evo.Label);
    }

    [Fact]
    public void AtMostFour_ArcAboveObject_OneSecondBob()
    {
        var (t, reward) = World();
        var w = t.World;
        // 꾸러미에 칸을 늘려도(무기·물약·금화 = 3) 4 를 넘지 않는다 — 상한은 목록 끝을 자른다
        var big = reward with { Bundle = reward.Bundle! with { Potions = 3 } };
        Assert.True(RewardOsd.Icons(big, w, decided: true, chosen: 0, gold: 40).Count <= RewardOsd.MaxIcons);
        Assert.Equal("물약 3", RewardOsd.Icons(big, w, decided: true, chosen: 0, gold: 40)[1].Label);

        // 호: 하나면 바로 위, 셋이면 왼쪽(180°)·위(90°)·오른쪽(0°), 모두 위쪽 반원 반지름 28
        var one = RewardOsd.Offset(1, 0);
        Assert.Equal(0f, one.X, 3);
        Assert.Equal(-RewardOsd.ArcRadius, one.Y, 3);
        var three = Enumerable.Range(0, 3).Select(i => RewardOsd.Offset(3, i)).ToArray();
        Assert.True(three[0].X < three[1].X && three[1].X < three[2].X);
        Assert.All(three, o => { Assert.True(o.Y <= 1e-3f); Assert.Equal(RewardOsd.ArcRadius, o.Length(), 3); });
        Assert.Equal(-three[0].X, three[2].X, 3);
        Assert.Equal(-RewardOsd.ArcRadius, three[1].Y, 3);
        // 24px 아이콘이 서로 겹치지 않는다(이웃 가운데 거리 ≥ 24) — 넷까지
        for (var n = 2; n <= RewardOsd.MaxIcons; n++)
            for (var i = 1; i < n; i++)
                Assert.True(Vector2.Distance(RewardOsd.Offset(n, i - 1), RewardOsd.Offset(n, i)) >= RewardOsd.IconSize, $"{n}개 중 {i}");

        // 흔들림: 1초(60틱) 주기, ±2px
        Assert.Equal(RewardOsd.Bob(0), RewardOsd.Bob(60), 3);
        Assert.InRange(Enumerable.Range(0, 60).Max(RewardOsd.Bob), 1.9f, 2.0001f);
        Assert.InRange(Enumerable.Range(0, 60).Min(RewardOsd.Bob), -2.0001f, -1.9f);
    }
}
