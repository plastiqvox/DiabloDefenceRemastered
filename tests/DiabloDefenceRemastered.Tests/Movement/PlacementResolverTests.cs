using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Tests.Movement;

/// <summary><c>PlacementResolver</c> (설계서 4.10, 10.1 <c>Movement</c>): 마름모 모서리·기둥 옆 소환, 생략 수 결정적, <c>PlaceReach</c>(v0.6).</summary>
public class PlacementResolverTests
{
    private static (NavWorld Nav, IsoMath Iso) Nav(MapDef map)
    {
        var grid = ArenaLayout.Expand(map);
        return (new NavWorld(grid, default), grid.Iso);
    }

    [Fact]
    public void Ring_AtDiamondCornerAndPillar_AllPassableOrSkipped()
    {
        var (nav, iso) = Nav(TestDefs.Map(12));
        foreach (var center in new[] { iso.ToWorld(new TileCoord(3, 3)), iso.ToWorld(new TileCoord(20, 3)), iso.CellCenter(7, 7) })
        {
            var placed = 0;
            var skipped = 0;
            for (var k = 0; k < 16; k++)
            {
                var wish = center + MathX.FromAngle(k * MathF.Tau / 16) * 200f;
                if (PlacementResolver.TryPlace(nav, center, wish, 16f, out var p))
                {
                    placed++;
                    Assert.True(nav.IsInsideMap(p));
                    Assert.True(nav.Clearance(p) >= 20f, $"{p}");
                    Assert.True(Vector2.Distance(p, center) >= 100f - 1e-3f);   // 절반까지만 당김
                }
                else skipped++;
            }
            Assert.True(placed > 0);
            // 같은 입력 → 같은 생략 수
            var again = Enumerable.Range(0, 16).Count(k => !PlacementResolver.TryPlace(nav, center, center + MathX.FromAngle(k * MathF.Tau / 16) * 200f, 16f, out _));
            Assert.Equal(skipped, again);
        }
    }

    [Fact]
    public void TryPlace_WishOk_ReturnsWish_InsidePillar_Pulled()
    {
        var (nav, iso) = Nav(TestDefs.Map(12));
        var center = iso.ToWorld(new TileCoord(11.5f, 11.5f));
        var free = center + new Vector2(60, 0);
        Assert.True(PlacementResolver.TryPlace(nav, center, free, 16f, out var p));
        Assert.Equal(free, p);
        var pillar = iso.CellCenter(16, 16);   // 기둥 (8,8)
        Assert.True(PlacementResolver.TryPlace(nav, center, pillar, 16f, out p));
        Assert.NotEqual(pillar, p);
        Assert.True(nav.Clearance(p) >= 20f);
    }

    [Fact]
    public void Reach_RoomWallBehind_RejectedThenNext_DoorSideAllowed()
    {
        // 방(메가 3..8, 문은 아래)의 위 벽 바깥에 기준이 있으면 벽 바로 너머 방 안 후보는 길이 1.5 배를 넘어 버린다
        var map = TestDefs.RoomMapOneDoor();
        var (nav, iso) = Nav(map);
        var from = iso.ToWorld(new TileCoord(11, 3.5f));
        var insideNearWall = iso.ToWorld(new TileCoord(11, 9));
        Assert.True(PlacementResolver.TryPlace(nav, from, insideNearWall, 14f, out _));   // 길 조건 없으면 놓임
        var reach = new PlaceReach(from, 1.5f);
        if (PlacementResolver.TryPlace(nav, from, insideNearWall, 14f, reach, out var p))
        {
            // 놓였다면 방 밖(기준 쪽)으로 당겨진 자리여야 한다
            Assert.NotEqual(insideNearWall, p);
            Assert.True(float.IsFinite(nav.PathLength(p, from, 14f, Vector2.Distance(p, from) * 1.5f)));
        }
        // 문 쪽(아래)에서 부르면 방 안이 허용
        var below = iso.ToWorld(new TileCoord(11, 22));
        var inside = iso.ToWorld(new TileCoord(11, 13));
        Assert.True(PlacementResolver.TryPlace(nav, below, inside, 14f, new PlaceReach(below, 1.5f), out p));
        Assert.Equal(inside, p);
    }

    [Fact]
    public void Throw_StopsAtObstacle()
    {
        var (nav, iso) = Nav(TestDefs.Map(12));
        var pillar = iso.CellCenter(6, 6);
        var from = pillar + new Vector2(-80, 0);
        var p = PlacementResolver.Throw(nav, from, new Vector2(1, 0), 200f, 12f);
        Assert.True(p.X < pillar.X - 12f);
        Assert.True(nav.Clearance(p) >= 12f);
        var open = PlacementResolver.Throw(nav, from, new Vector2(0, 1), 40f, 12f);
        Assert.Equal(from + new Vector2(0, 40), open);
    }

    [Fact]
    public void PathLengthToHero_UsesFieldOrAStar()
    {
        var t = MovementTests.Build();
        var nav = (NavWorld)t.World.Nav;
        var from = t.Hero.Pos + new Vector2(300, 0);
        var field = nav.PathLengthToHero(from, 12f, 10_000f);
        Assert.True(float.IsFinite(field));
        Assert.True(field >= Vector2.Distance(from, t.Hero.Pos) * 0.9f);
        var big = nav.PathLengthToHero(from, 34f, 10_000f);   // 필드 등급 밖 → 자른 A*
        Assert.True(float.IsFinite(big));
        Assert.True(float.IsPositiveInfinity(nav.PathLengthToHero(from, 12f, 50f)));
    }
}
