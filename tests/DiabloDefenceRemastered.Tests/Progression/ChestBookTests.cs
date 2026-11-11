using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Progression;

/// <summary>
/// 10.1 <c>Progression</c> 행(v0.9.1 V8 WP3 단계 2 — 설계서 6.19 "보상 물체 둘레 아이콘", 기획 11.4): <c>RewardDef.Drop</c> 이 있는 보상 물체(보물 상자 → 책).
/// 상자에 닿으면 열림(<c>RewardStage</c> Dropped·올릴 무기 <c>RewardChosen</c>·금화를 그 틱에) → 책(<c>GatesExit</c>, 상자 너머 30px) + <c>ChestDrop</c>, 책에 닿으면 꾸러미.
/// 합성 정의 <see cref="ShopKit.ChestBookId"/>(보물 상자 꾸러미 +1Lv·물약 1·금화 30~60 + <c>drop: t_book</c>).
/// </summary>
public class ChestBookTests
{
    private static (TestWorld T, RewardDef Reward) World(ulong seed = 1)
    {
        var defs = ShopKit.Defs();
        var t = CombatKit.World(defs, seed, b => b.WithPresentation());
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 2);
        CombatKit.SetWeapon(w, 2, CombatKit.Fb, 1);
        CombatKit.SetWeapon(w, 3, CombatKit.Orbit, 3);
        w.Hero.Facing = new Vector2(0f, 1f);
        return (t, defs.Rewards.Get(ShopKit.ChestBookId));
    }

    private static Pickup? Book(World w)
    {
        foreach (var p in w.Pickups.Items)
            if (!p.Removed && p.Def.Id == ShopKit.BookId) return p;
        return null;
    }

    [Fact]
    public void TouchChest_DropsBook_GatesExit_30pxAhead_OneChestDropCue()
    {
        var (t, reward) = World();
        var w = t.World;
        var chest = CombatKit.Drop(w, ShopKit.ChestObjId, w.Hero.Pos, reward: reward);
        Assert.True(chest.GatesExit);
        t.Step();

        Assert.False(chest.Removed);                                                     // 열린 상자로 남음
        Assert.Equal(RewardPickupStage.Dropped, chest.RewardStage);
        Assert.False(chest.GatesExit);                                                   // 문은 책이 막는다
        var book = Book(w);
        Assert.NotNull(book);
        Assert.Same(reward, book.Reward);
        Assert.True(book.GatesExit);
        Assert.Equal(RewardPickupStage.Dropped, book.RewardStage);
        Assert.Equal(30f, Vector2.Distance(chest.Pos, book.Pos), 1);
        Assert.True(book.Pos.Y > chest.Pos.Y);                                           // 주인공이 상자 위에 서 있으면 바라보는 쪽(남)
        var drop = Assert.Single(ShopKit.Cues(w, CueKind.ChestDrop));
        Assert.Same(reward, drop.Subject);
        Assert.Equal(book.Pos, drop.Pos);
        Assert.Empty(ShopKit.Cues(w, CueKind.RewardCollected));                          // 아직 받지 않음
        Assert.InRange(book.Value, 30, 60);                                              // 금화도 여는 틱에
        Assert.Equal(0, w.Hero.Gold);

        for (var i = 0; i < 10; i++) t.Step();                                           // 열린 상자에 계속 서 있어도 다시 안 열림
        Assert.Single(ShopKit.Cues(w, CueKind.ChestDrop));
        Assert.Equal(1, w.Pickups.Items.ToArray().Count(p => !p.Removed && p.Def.Id == ShopKit.BookId));

        // 상자 쪽에서 다가가면 책은 상자 너머(주인공 → 상자 방향)
        var (u, r2) = World(3);
        var c2 = CombatKit.Drop(u.World, ShopKit.ChestObjId, u.Hero.Pos + new Vector2(20f, 0f), reward: r2);
        u.Step();
        var b2 = Book(u.World)!;
        Assert.Equal(c2.Pos.X + 30f, b2.Pos.X, 1);
        Assert.Equal(c2.Pos.Y, b2.Pos.Y, 1);
    }

    [Fact]
    public void ChosenAtOpen_KeptWhenBookCollected_EvenIfBuildChangedBetween()
    {
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var (t, reward) = World(seed);
            var w = t.World;
            var chest = CombatKit.Drop(w, ShopKit.ChestObjId, w.Hero.Pos, reward: reward);
            t.Step();
            var chosen = chest.RewardChosen;
            Assert.Contains(chosen, new[] { 0, 2, 3 });
            var book = Book(w)!;
            Assert.Equal(chosen, book.RewardChosen);
            var gold = book.Value;
            var bookId = book.Id;
            var levels = Enumerable.Range(0, WeaponSlots.SlotCount).Select(i => w.Hero.Weapons[i].Level).ToArray();

            // 그 사이 빌드가 바뀜(다른 칸 하나가 만렙 — 다시 뽑으면 후보 수가 달라 다른 칸이 나올 수 있다) + 방 난수도 흐름
            var other = new[] { 0, 2, 3 }.First(i => i != chosen);
            CombatKit.SetWeapon(w, other, w.Hero.Weapons[other].Def!.Id, w.Hero.Weapons[other].Def!.MaxLevel);
            levels[other] = w.Hero.Weapons[other].Level;
            w.Rng.Drop.NextUInt();
            t.PlaceHero(book.Pos);
            t.Step();
            Assert.False(w.Pickups.Contains(bookId));                                     // 집힘(저장소에서 빠짐)
            for (var i = 0; i < WeaponSlots.SlotCount; i++) Assert.Equal(levels[i] + (i == chosen ? 1 : 0), w.Hero.Weapons[i].Level);
            Assert.Equal(gold, w.Hero.Gold);
            Assert.Equal(1, w.Hero.Potions);
            Assert.Single(ShopKit.Cues(w, CueKind.RewardCollected));
        }
    }

    [Fact]
    public void FullPotionSlots_OnlyPotionLost()
    {
        var (t, reward) = World(5);
        var w = t.World;
        w.Hero.Potions = HeroPotions.Capacity(w);
        var chest = CombatKit.Drop(w, ShopKit.ChestObjId, w.Hero.Pos, reward: reward);
        t.Step();
        var chosen = chest.RewardChosen;
        var level = w.Hero.Weapons[chosen].Level;
        var book = Book(w)!;
        var gold = book.Value;
        t.PlaceHero(book.Pos);
        t.Step();
        Assert.Equal(2, w.Hero.Potions);                                                 // 칸 그대로(넘친 1 은 빠짐)
        Assert.Equal(level + 1, w.Hero.Weapons[chosen].Level);
        Assert.Equal(gold, w.Hero.Gold);
    }

    [Fact]
    public void MaxedBetween_GivesTemper_NoReroll()
    {
        var (t, reward) = World(2);
        var w = t.World;
        var chest = CombatKit.Drop(w, ShopKit.ChestObjId, w.Hero.Pos, reward: reward);
        t.Step();
        var chosen = chest.RewardChosen;
        var def = w.Hero.Weapons[chosen].Def!;
        CombatKit.SetWeapon(w, chosen, def.Id, def.MaxLevel);                            // 그 칸이 그 사이 만렙
        var others = Enumerable.Range(0, WeaponSlots.SlotCount).Select(i => w.Hero.Weapons[i].Level).ToArray();
        var mods = w.Combat.Mods[(int)StatKey.MaxHp];
        t.PlaceHero(Book(w)!.Pos);
        t.Step();
        Assert.Equal(others, Enumerable.Range(0, WeaponSlots.SlotCount).Select(i => w.Hero.Weapons[i].Level).ToArray());
        Assert.Equal(mods + 5f, w.Combat.Mods[(int)StatKey.MaxHp]);                      // 단련
    }

    [Fact]
    public void NoDrop_OneTouchAsBefore()
    {
        var (t, _) = World();
        var w = t.World;
        var plain = w.Defs.Rewards.Get(TestDefs.RewardChestId);
        Assert.Null(plain.Drop);
        var levels = Enumerable.Range(0, WeaponSlots.SlotCount).Sum(i => w.Hero.Weapons[i].Level);
        var id = CombatKit.Drop(w, plain.Object!.Id, w.Hero.Pos, reward: plain).Id;
        t.Step();
        Assert.False(w.Pickups.Contains(id));
        Assert.Null(Book(w));
        Assert.Empty(ShopKit.Cues(w, CueKind.ChestDrop));
        Assert.Single(ShopKit.Cues(w, CueKind.RewardCollected));
        Assert.Equal(levels + 1, Enumerable.Range(0, WeaponSlots.SlotCount).Sum(i => w.Hero.Weapons[i].Level));
        Assert.Equal((1, w.RunStats.Gold), (w.Hero.Potions, w.Hero.Gold));
        Assert.InRange(w.Hero.Gold, 30, 60);
    }

    [Fact]
    public void SameSeed_SameDropStreamAsOneStep()
    {
        // 두 단계 상자의 뽑기(무기 칸·금화)는 한 번에 받는 상자와 같은 차례·같은 수 — 여는 틱에 뽑을 뿐
        for (ulong seed = 1; seed <= 8; seed++)
        {
            var (a, r) = World(seed);
            var chest = CombatKit.Drop(a.World, ShopKit.ChestObjId, a.Hero.Pos, reward: r);
            a.Step();
            var (b, _) = World(seed);
            var plain = b.World.Defs.Rewards.Get(TestDefs.RewardChestId);
            var before = Enumerable.Range(0, WeaponSlots.SlotCount).Select(i => b.Hero.Weapons[i].Level).ToArray();
            CombatKit.Drop(b.World, plain.Object!.Id, b.Hero.Pos, reward: plain);
            b.Step();
            var raised = Enumerable.Range(0, WeaponSlots.SlotCount).Single(i => b.Hero.Weapons[i].Level != before[i]);
            Assert.Equal(raised, chest.RewardChosen);
            Assert.Equal(b.Hero.Gold, chest.Value);
            Assert.Equal(b.World.Rng.Drop.GetState(), a.World.Rng.Drop.GetState());
        }
    }
}
