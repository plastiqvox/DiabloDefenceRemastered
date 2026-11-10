using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Nav.Validation;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary>
/// 실제 <c>Data/maps/</c> 의 작은 방 맵(액트 1 대성당 일반·정예 방, 설계서 6.11·11.1 WP5)을 모두 원판·거울판으로 — 자원 없이 돈다.
/// 13.5-4 배치 쪽·13.5-19(<c>ValidateRooms</c>), 길 검사 13.5-4~6·13.5-23 ③(<see cref="NavPathValidator"/>), 10.1 <c>Nav</c> 전수 (a)~(c),
/// 문 넷 진입점(뒤 문 4, 앞 문 N − 6)·시작 = <c>center</c>. 맵을 더하면 목록이 저절로 늘어난다(보스 방 <c>cathedral_arena</c> 는 <see cref="RealMapNavTests"/>).
/// </summary>
public class RealRoomMapTests
{
    /// <summary>작은 방(N ≤ <see cref="RoomMapRules.SmallRoomMaxN"/>) 맵 ID 와 거울판(<c>@m</c>).</summary>
    public static TheoryData<string> MapIds
    {
        get
        {
            var ids = new TheoryData<string>();
            foreach (var m in SmallRooms())
            {
                ids.Add(m.Id);
                if (m.Mirrored is not null) ids.Add(m.Id + "@m");
            }
            return ids;
        }
    }

    private static IEnumerable<MapDef> SmallRooms() =>
        RealData.Maps.Maps.Where(m => m.Size * 2 <= RoomMapRules.SmallRoomMaxN).OrderBy(m => m.Id, StringComparer.Ordinal);

    private static MapDef Get(string id)
    {
        var mirror = id.EndsWith("@m", StringComparison.Ordinal);
        var map = RealData.Maps.Maps.Single(m => m.Id == (mirror ? id[..^2] : id));
        return mirror ? map.Mirrored ?? throw new InvalidOperationException("거울판 없음") : map;
    }

    private static (MapDef Map, NavWorld Nav, Vector2 Hero) Load(string id)
    {
        var map = Get(id);
        return (map, new NavWorld(ArenaLayout.Expand(map), default), ResolvedMap.Resolve(map).HeroStart);
    }

    [Fact]
    public void ActOne_PoolHasAtLeastFiveSmallRooms_AllMirrored_BossArenaKept()
    {
        Assert.True(RealData.Maps.Errors.Count == 0, string.Join("\n", RealData.Maps.Errors));
        var rooms = SmallRooms().Where(m => m.Id.StartsWith("cathedral_room_", StringComparison.Ordinal)).ToList();
        Assert.True(rooms.Count >= 5, $"대성당 작은 방 {rooms.Count}장");
        foreach (var m in rooms) Assert.True(m.Mirrored is not null, $"{m.Id} 거울판 없음");

        // 액트 1 일반 방 풀 = 작은 방 전부(기획 v0.9 V2), 보스 방 맵은 cathedral_arena 그대로
        var ctx = GameModules.CreateContext();
        var data = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(data.HasErrors, string.Join("\n", data.Errors));
        var pool = data.Defs.Stages.Get("stage_cathedral").RoomMaps;
        Assert.Equal(rooms.Select(m => m.Id), pool.Normal.Select(m => m.Id).Order(StringComparer.Ordinal));
        Assert.Equal("cathedral_arena", pool.Boss?.Id);
    }

    [Theory, MemberData(nameof(MapIds))]
    public void Expand_LayoutAndRoomChecks_NoErrors(string id)
    {
        var map = Get(id);
        var errors = new List<LayoutError>();
        var grid = ArenaLayout.TryExpand(map, map.Tileset.Def, errors);
        Assert.NotNull(grid);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        ArenaLayout.ValidateRooms(map, grid, errors);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Equal(32, grid.Size);
    }

    [Theory, MemberData(nameof(MapIds))]
    public void Entries_BackFour_FrontNMinusSix_HeroAtCenter(string id)
    {
        var map = Get(id);
        var n = ArenaLayout.Expand(map).Size;
        var mid = n / 2f - 0.5f;
        var e = map.Entries.ToDictionary(x => x.Id, x => x.InsideTile);
        Assert.Equal(["gate_ne", "gate_nw", "gate_se", "gate_sw"], e.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(new TileCoord(4, mid), e["gate_nw"]);
        Assert.Equal(new TileCoord(mid, 4), e["gate_ne"]);
        Assert.Equal(new TileCoord(n - 6, mid), e["gate_se"]);
        Assert.Equal(new TileCoord(mid, n - 6), e["gate_sw"]);
        Assert.Equal(new TileCoord(mid, mid), map.HeroStart.Tile);
        Assert.Equal(new TileCoord(mid, mid), map.AnchorsTile[RoomMapRules.CenterAnchor]);
    }

    [Theory, MemberData(nameof(MapIds))]
    public void D_PathChecks_NoErrors(string id)
    {
        var errors = NavPathValidator.Check(Get(id));
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Theory, MemberData(nameof(MapIds))]
    public void A_FollowDir_ReachesStartWithoutLoops(string id)
    {
        var (_, nav, hero) = Load(id);
        NavExhaustive.FollowDirReachesStart(nav, hero);
    }

    [Theory, MemberData(nameof(MapIds))]
    public void B_PointUnits_ReachHero(string id)
    {
        var (_, nav, hero) = Load(id);
        var failed = NavExhaustive.PointUnitsFailing(nav, hero);
        Assert.True(failed.Count == 0, $"{failed.Count}칸이 시간 한계 안에 못 옴(첫 칸 중심 {(failed.Count > 0 ? nav.Grid.Center(failed[0]) : default)})");
    }

    [Theory, MemberData(nameof(MapIds))]
    public void C_HeroAtCornersPillarsAndRooms_AllPassableReached(string id)
    {
        var (map, nav, hero) = Load(id);
        NavExhaustive.AllSpotsReachSameCells(nav, hero, NavExhaustive.HeroSpots(map));
    }
}
