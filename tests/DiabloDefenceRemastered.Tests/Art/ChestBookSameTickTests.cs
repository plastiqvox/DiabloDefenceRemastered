using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Shops;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>
/// 보물 상자 → 책(v0.9.1 V8)을 데이터로 켜기 전 확인(단계 2 wp5 — 13장 WP5 단계 2 기록): 상자를 연 틱에 떨어진 책을 <b>그 틱에</b> 줍지 않는다.
/// <c>DropReward</c> 가 책을 획득물 저장소 끝에 붙이므로 같은 <c>Pickups</c> 반복이 곧 그 책을 만나고, 책 자리가 벽·맵 끝 때문에 상자 쪽으로 당겨지면(<c>RewardSpot.Row</c>)
/// 주인공 몸에 바로 닿아 상자에 닿는 한 번으로 꾸러미를 받던 문제(책이 화면에 한 프레임도 안 보임).
/// </summary>
public class ChestBookSameTickTests
{
    private static Pickup? Book(World w)
    {
        foreach (var p in w.Pickups.Items)
            if (!p.Removed && p.Def.Id == ShopKit.BookId) return p;
        return null;
    }

    [Fact]
    public void BookPulledOntoHero_NotCollectedOnOpenTick_CollectedNextTick()
    {
        var defs = ShopKit.Defs();
        Assert.NotNull(defs.Rules.Rooms?.RewardSpot);                                   // 당김 규칙이 있는 정의(책 자리 = RewardSpot.Row)
        var t = CombatKit.World(defs, 1, b => b.WithPresentation());
        var w = t.World;
        CombatKit.ClearWeapons(w);
        CombatKit.SetWeapon(w, 0, CombatKit.Mm, 2);
        var reward = defs.Rewards.Get(ShopKit.ChestBookId);

        // 맵 오른쪽 끝: 주인공 → 상자 방향(오른쪽)으로 30px 너머는 맵 밖이라 책이 상자 자리까지 당겨진다 — 주인공과 20px(몸 14 + 책 8 안)
        var y = w.MapSize.Y * 0.5f;
        t.PlaceHero(new Vector2(w.MapSize.X - 45f, y));
        var chest = CombatKit.Drop(w, ShopKit.ChestObjId, new Vector2(w.MapSize.X - 25f, y), reward: reward);
        t.Step();

        Assert.Equal(RewardPickupStage.Dropped, chest.RewardStage);
        var book = Book(w);
        Assert.NotNull(book);                                                            // 연 틱에는 책이 남아 있다
        Assert.True(Vector2.Distance(w.Hero.Pos, book.Pos) <= w.Hero.Radius + book.Radius, "시험 전제: 책이 주인공 몸에 닿는 자리로 당겨짐");
        Assert.Empty(ShopKit.Cues(w, CueKind.RewardCollected));
        Assert.Equal(0, w.Hero.Gold);

        t.Step();                                                                        // 다음 틱에는 닿아 있으므로 집힘
        Assert.Null(Book(w));
        Assert.Single(ShopKit.Cues(w, CueKind.RewardCollected));
        Assert.True(w.Hero.Gold > 0);
    }
}
