namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 상인 방의 보상 자리(wp4 — WP3 단계 2 기록이 넘긴 것): 정예 상자가 함께 놓이면 보상 줄이 둘 이상이라 줄 가운데가 <c>RewardSpot.Find</c> 자리가 아니다 —
/// 상인 자리(<c>RoomState.RewardSpot</c> — 그리기·열기·신호가 읽음) = 줄의 첫 자리, 상자 = 둘째 자리. 상자가 없으면 그대로 찾은 자리.
/// </summary>
public sealed class ShopRewardSpotTests
{
    private static TestWorld CleanupShopRoom(bool withChest)
    {
        var defs = TestDefs.Basic();
        var reward = defs.Rewards.Get(TestDefs.RewardSmithId);
        var t = new TestWorldBuilder().WithDefs(defs).WithRoom(RoomPhase.Cleanup, reward: reward).Build();
        t.World.Room!.Shop = new ShopState { MerchantId = TestDefs.SmithId };
        if (withChest)
        {
            var c = t.World.Pickups.Add();
            c.Def = defs.Pickups.Get(TestDefs.EliteChestId);
            c.Pos = c.PrevPos = t.Hero.Pos + new Vector2(300f, 0f);
        }
        return t;
    }

    [Fact]
    public void Merchant_stands_on_first_row_spot_when_chest_shares_row()
    {
        var t = CleanupShopRoom(withChest: true);
        t.Step();
        var room = t.World.Room!;
        Assert.Equal(RoomPhase.Reward, room.Phase);
        var chest = Assert.Single(t.World.Pickups.Items.ToArray(), p => !p.Removed);
        Assert.True(chest.GatesExit);
        var spacing = t.World.Defs.Rules.Rooms!.RewardSpot.Spacing;
        Assert.Equal(spacing, Vector2.Distance(room.RewardSpot, chest.Pos), 2);      // 상인·상자가 한 줄에 spacing 간격
        var found = RewardSpot.Find(t.World.Nav, t.Hero.Pos, t.Hero.Facing, t.Hero.Radius, t.World.Defs.Rules.Rooms!.RewardSpot);
        Assert.Equal(found.X, ((room.RewardSpot + chest.Pos) * 0.5f).X, 2);           // 줄 가운데 = 찾은 자리
        Assert.Equal(found.Y, ((room.RewardSpot + chest.Pos) * 0.5f).Y, 2);
    }

    [Fact]
    public void Merchant_stands_on_found_spot_without_chest()
    {
        var t = CleanupShopRoom(withChest: false);
        t.Step();
        var room = t.World.Room!;
        Assert.Equal(RoomPhase.Reward, room.Phase);
        var found = RewardSpot.Find(t.World.Nav, t.Hero.Pos, t.Hero.Facing, t.Hero.Radius, t.World.Defs.Rules.Rooms!.RewardSpot);
        Assert.Equal(found, room.RewardSpot);
    }
}
