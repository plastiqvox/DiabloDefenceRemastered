using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Nav.Validation;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary>
/// 작은 합성 방 맵(N 32)에서 10.1 <c>Nav</c> 전수 시험 (a)~(d) (설계서 11.1 WP2 v0.7 완료 기준 더함).
/// 맵: 합성 액트의 작은 방(<see cref="RoomActs.RoomA"/> — 기둥 넷)과 그 거울판, 안쪽 방 하나(문 하나)와 기둥 하나를 둔 N 32 맵.
/// 스테이지 2 실제 맵이 생기면(단계 3) <see cref="RealMapNavTests"/> 를 그 맵으로 한 번 더.
/// </summary>
public class SmallRoomNavTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, MapDef>> Maps = new(Build);

    public static TheoryData<string> MapIds => [RoomActs.RoomA, RoomActs.RoomA + "@m", InnerRoomId];

    private const string InnerRoomId = "test_small_inner_room";

    private static IReadOnlyDictionary<string, MapDef> Build()
    {
        var a = RoomActs.Defs(mirror: true).Maps.Get(RoomActs.RoomA);
        var mirrored = a.Mirrored ?? throw new InvalidOperationException("거울판 없음");
        return new Dictionary<string, MapDef>
        {
            [a.Id] = a,
            [RoomActs.RoomA + "@m"] = mirrored,
            [InnerRoomId] = InnerRoom(),
        };
    }

    /// <summary>
    /// N 32 안쪽 방 맵: 메가 (2..5, 2..5) 벽(꺾이는 자리 모서리 조각), 아래 벽 가운데 두 메가 (3..4, 5)가 문, 기준점 <c>room_a</c> (7, 7).
    /// 둘레와 하위 3칸 띄움(13.5-4 80px), 입구 진입점과 5칸 넘게 띄움(13.5-6). 기둥 하나 (11, 11).
    /// </summary>
    private static MapDef InnerRoom() => TestDefs.RoomMap(
    [
        new()
        {
            Id = "room_a", Origin = new(2, 2),
            Grid =
            [
                "wall_corner wall_h wall_h wall_corner",
                "wall_v      .      .      wall_v",
                "wall_v      .      .      wall_v",
                "wall_corner .      .      wall_corner",
            ],
        },
        new() { Piece = "pillar", At = [new(11, 11)] },
    ], id: InnerRoomId, megaTiles: 16,
    anchors: new Dictionary<string, TileCoord> { ["room_a"] = new(7, 7) },
    roomAreas: new Dictionary<string, RectI> { ["room_a"] = new(2, 2, 5, 5) });

    private static (MapDef Map, NavWorld Nav, Vector2 Hero) Load(string id)
    {
        var map = Maps.Value[id];
        Assert.Equal(32, ResolvedMap.Resolve(map).Iso.N);
        return (map, new NavWorld(ArenaLayout.Expand(map), default), ResolvedMap.Resolve(map).HeroStart);
    }

    [Theory, MemberData(nameof(MapIds))]
    public void D_PathChecks_NoErrors(string id)
    {
        var errors = NavPathValidator.Check(Maps.Value[id]);
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
        var spots = NavExhaustive.HeroSpots(map);
        Assert.True(spots.Count > 4);   // 기둥 또는 방 기준점이 들어갔다
        NavExhaustive.AllSpotsReachSameCells(nav, hero, spots);
    }
}
