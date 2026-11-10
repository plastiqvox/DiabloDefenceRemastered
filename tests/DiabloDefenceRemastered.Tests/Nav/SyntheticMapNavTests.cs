using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Nav.Validation;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary>
/// 합성 맵 길 검사(설계서 10.1 <c>Nav</c> v0.6·v0.6.1·v0.7.1 — WP1d 합성 방 맵)와 길 길이·경로 다듬기.
/// 스테이지 2 실제 맵이 없는 동안(단계 3 전) 합성 맵이 같은 검사를 대신한다.
/// </summary>
public class SyntheticMapNavTests
{
    private static List<string> Check(MapDef map)
    {
        var layout = new List<LayoutError>();
        Assert.NotNull(ArenaLayout.TryExpand(map, map.Tileset.Def, layout));   // 배치 풀기가 되는 맵이어야 길 검사가 돈다
        return NavPathValidator.Check(map);
    }

    private static NavWorld NavOf(MapDef map) => new(ArenaLayout.Expand(map), default);

    [Fact]
    public void Strip_TrappedFloorError_OverBorderPasses()
    {
        var strip = Check(TestDefs.RoomMapStrip());
        Assert.Contains(strip, e => e.Contains("갇힌 바닥 칸", StringComparison.Ordinal));
        var over = Check(TestDefs.RoomMapOverBorder());
        Assert.DoesNotContain(over, e => e.Contains("갇힌 바닥 칸", StringComparison.Ordinal));
    }

    [Fact]
    public void Example_And_OneDoorRoom_Pass()
    {
        Assert.Empty(Check(TestDefs.RoomMapExample()));
        Assert.Empty(Check(TestDefs.RoomMapOneDoor()));
        Assert.Empty(Check(TestDefs.Map(12)));
    }

    [Fact]
    public void ClosedRoom_TrappedFloorError()
    {
        var closed = OneDoorRoom("wall_h wall_h", "test_room_closed");
        Assert.Contains(Check(closed), e => e.Contains("갇힌 바닥 칸", StringComparison.Ordinal));
    }

    [Fact]
    public void NarrowDoor_LargeRadiusError()
    {
        // 문을 메가 2 → 1 로 좁힌 방: 34 몸은 room_a 에 못 들어간다(하위 2칸 = 약 57px < 68)
        var narrow = Check(OneDoorRoom(".      wall_h", "test_room_narrow"));
        Assert.Contains(narrow, e => e.Contains("반경 34", StringComparison.Ordinal) && e.Contains("room_a", StringComparison.Ordinal));
        Assert.DoesNotContain(narrow, e => e.Contains("반경 16", StringComparison.Ordinal));
    }

    [Fact]
    public void TJunction_GapError_CornerPieceJoins()
    {
        // 가로 벽 줄 아래에 세로 벽이 붙는 메가에 세로 조각만 → 하위 1칸 틈(28.6px < 80) / 접합 메가를 모서리 조각으로 → 붙음
        static MapDef T(string junction) => TestDefs.RoomMap(
        [
            new() { Piece = "wall_h", Line = [new(5, 6), new(7, 6)] },
            new() { Piece = "wall_h", Line = [new(9, 6), new(12, 6)] },
            new() { Piece = junction, At = [new(8, 6)] },
            new() { Piece = "wall_v", Line = [new(8, 7), new(8, 11)] },
        ], id: "test_t_" + junction);
        Assert.Contains(Check(T("wall_h")), e => e.StartsWith("13.5-4", StringComparison.Ordinal));
        Assert.DoesNotContain(Check(T("wall_corner")), e => e.StartsWith("13.5-4", StringComparison.Ordinal));
    }

    [Fact]
    public void EntryNearInnerWall_60pxError()
    {
        // 북서 입구 진입점 (4, 19.5) 바로 옆 기둥(하위 (4,20))
        var map = TestDefs.Map(20, "test_entry_pillar", TestDefs.RoomTileset(), [new() { Piece = "pillar", At = [new(2, 10)] }]);
        Assert.Contains(Check(map), e => e.Contains("gate_nw", StringComparison.Ordinal) && e.Contains("60px", StringComparison.Ordinal));
    }

    [Fact]
    public void GateReach_BlockedGateError_OpenPasses()
    {
        // 왼쪽 열 바로 안쪽 메가 줄 전체를 세로 벽으로 막으면 북서 문 틈(하위 x 0..1)에 몸이 못 닿는다
        var blocked = TestDefs.RoomMap([new() { Piece = "wall_v", Line = [new(1, 1), new(1, 18)] }], id: "test_gate_blocked")
            with { RoomKinds = [RoomKind.Normal] };
        Assert.Contains("13.5-23 문 틈에 못 닿음 gate_nw", Check(blocked));
        var open = TestDefs.RoomMap([], id: "test_gate_open") with { RoomKinds = [RoomKind.Normal] };
        Assert.DoesNotContain(Check(open), e => e.Contains("문 틈", StringComparison.Ordinal));
    }

    [Fact]
    public void PathLength_StraightDetourMaxAndDeterministic()
    {
        var map = TestDefs.RoomMapOneDoor();
        var nav = NavOf(map);
        var iso = ArenaLayout.Expand(map).Iso;
        // 트인 선 = 직선 거리
        var a = iso.ToWorld(new TileCoord(24, 24));
        var b = iso.ToWorld(new TileCoord(30, 26));
        Assert.Equal(Vector2.Distance(a, b), nav.PathLength(a, b, 14f, 10_000f), 3);
        // 방 위쪽 벽 바깥 → 방 안: 문(아래)을 돌아가는 길
        var inside = iso.ToWorld(new TileCoord(11, 11));
        var behind = iso.ToWorld(new TileCoord(11, 3.5f));
        var straight = Vector2.Distance(inside, behind);
        var detour = nav.PathLength(behind, inside, 14f, 10_000f);
        Assert.True(float.IsFinite(detour));
        Assert.True(detour > straight * 2f, $"돌아가는 길 {detour} / 직선 {straight}");
        Assert.True(float.IsPositiveInfinity(nav.PathLength(behind, inside, 14f, straight * 1.5f)));
        Assert.Equal(detour, NavOf(map).PathLength(behind, inside, 14f, 10_000f));
    }

    [Fact]
    public void FindPath_SmoothedPathAvoidsWalls_AndTryPathStep()
    {
        var map = TestDefs.RoomMapOneDoor();
        var nav = NavOf(map);
        var iso = ArenaLayout.Expand(map).Iso;
        var inside = iso.ToWorld(new TileCoord(11, 11));
        var behind = iso.ToWorld(new TileCoord(11, 3.5f));
        var path = new List<Vector2>();
        Assert.True(nav.FindPath(behind, inside, 14f, path, requireGoal: true));
        Assert.Equal(behind, path[0]);
        Assert.Equal(inside, path[^1]);
        Assert.True(path.Count >= 3);
        for (var i = 1; i < path.Count; i++) Assert.True(nav.HasClearLine(path[i - 1], path[i], 14f), $"구간 {i}");
        Assert.True(nav.TryPathStep(behind, inside, 14f, out var next));
        Assert.Equal(path[1], next);
        Assert.True(nav.TryPathStep(inside, inside + new Vector2(10, 0), 14f, out next));
        Assert.Equal(inside + new Vector2(10, 0), next);   // 트인 선이면 목표
    }

    [Fact]
    public void ProjectileLine_PillarBlocks()
    {
        var map = TestDefs.Map(12);
        var nav = NavOf(map);
        var iso = ArenaLayout.Expand(map).Iso;
        var pillar = iso.CellCenter(6, 6);
        Assert.False(nav.ProjectileLineClear(pillar - new Vector2(80, 0), pillar + new Vector2(80, 0)));
        Assert.True(nav.ProjectileLineClear(pillar + new Vector2(-80, 40), pillar + new Vector2(80, 40)));
        Assert.False(nav.HasClearLine(pillar - new Vector2(80, 0), pillar + new Vector2(80, 0), 1f));
    }

    /// <summary>합성 방 맵 ③ 의 아래 벽 가운데 두 칸만 바꾼 방.</summary>
    private static MapDef OneDoorRoom(string door, string id) => TestDefs.RoomMap(
    [
        new()
        {
            Id = "room_a", Origin = new(3, 3),
            Grid =
            [
                "wall_corner wall_h wall_h wall_h wall_h wall_corner",
                "wall_v .      .      .      .      wall_v",
                "wall_v .      .      .      .      wall_v",
                "wall_v .      .      .      .      wall_v",
                "wall_v .      .      .      .      wall_v",
                $"wall_corner wall_h {door} wall_h wall_corner",
            ],
        },
    ], id: id, anchors: new Dictionary<string, TileCoord> { ["room_a"] = new(11, 11) },
    roomAreas: new Dictionary<string, RectI> { ["room_a"] = new(3, 3, 8, 8) });
}
