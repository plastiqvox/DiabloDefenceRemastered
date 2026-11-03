using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Visibility;

/// <summary>
/// 실제 <c>cathedral_arena</c>(WP0a 첫 판)로 만든 <see cref="VisibilityField"/> (설계서 11.1 WP0a 완료 기준 v0.5·v0.6, 기획 10.3 "시야").
/// 빛 반경 420(스테이지 1), <c>rules.sight</c> 는 실제 <c>rules/sight.json</c>. 하위 칸 좌표, 주인공 시작 = (19.5,19.5) → 칸 (20,20).
/// </summary>
public class RealMapVisibilityTests
{
    private static (TestWorld T, VisibilityField F, ArenaGrid A) At(int? tx = null, int? ty = null)
    {
        var t = RealData.CathedralWorld();
        if (tx is { } x && ty is { } y) t.PlaceHero(t.CellCenter(x, y));
        t.Step();
        return (t, t.World.Visibility, t.World.Arena!);
    }

    /// <summary>칸 사이 화면 거리.</summary>
    private static float CellDist(int dx, int dy) => new Vector2(32 * (dx - dy), 16 * (dx + dy)).Length();

    [Fact]
    public void Start_SightCoversOpenFloor_NothingOutsideMap_PillarsDoNotShadow()
    {
        var (t, f, a) = At();
        Assert.True(f.Enabled);
        Assert.Equal(new Int2(20, 20), f.CellOf(t.Hero.Pos));

        // 시작 자리에서 빛 반경 안 열린 바닥(open_center 메가 [6..12] = 하위 12..25)은 모두 보인다 — 가운데에는 기둥(x, 시야 통과)뿐
        for (var ty = 12; ty < 26; ty++)
            for (var tx = 12; tx < 26; tx++)
                if (CellDist(tx - 20, ty - 20) <= 420f) Assert.True(f.InSight(tx, ty), $"({tx},{ty})");
        // 기둥 칸은 걷기만 막고 그 뒤도 보인다(기획 열린 질문 22 기본 — 원본대로)
        Assert.True(a.BlocksWalk(24, 18));
        Assert.False(a.BlocksSight(24, 18));
        Assert.True(f.InSight(28, 16));
        Assert.True(f.InSight(10, 17));    // 기둥 (14,18) 너머

        // 맵 가운데에서는 둘레 너머(맵 밖)까지 반경이 닿지 않는다 — 보이는 칸은 모두 격자 안
        for (var ty = -VisibilityField.Pad; ty < 40 + VisibilityField.Pad; ty++)
            for (var tx = -VisibilityField.Pad; tx < 40 + VisibilityField.Pad; tx++)
                if (f.InSight(tx, ty)) Assert.True(a.IsInside(tx, ty), $"({tx},{ty})");
    }

    [Fact]
    public void Start_BloodRoomAndGrateRoomInsidesHidden()
    {
        var (_, f, a) = At();
        var map = RealData.Cathedral;
        var floors = RoomFloors.Compute(map, a);

        // 피 방: 문 틈 [5,3..4] 으로 들어오는 좁은 쐐기만 보이고 나머지는 가려진다(기준점 포함)
        Assert.True(floors.TryGet("room_blood", out var blood));
        var bloodSeen = blood.Cells.Count(c => f.InSight(c.X, c.Y));
        Assert.InRange(bloodSeen, 1, 8);    // 81칸 중 문 쐐기
        Assert.False(f.InSight(5, 5));
        Assert.False(f.InSight(3, 8));
        Assert.True(f.InSight(10, 7));      // 문 틈 칸 자체

        // 창살 방: 가운데에서는 안이 하나도 안 보인다(문 틈은 반대쪽, 창살은 반경 밖)
        Assert.True(floors.TryGet("room_grate", out var grate));
        Assert.DoesNotContain(grate.Cells, c => f.InSight(c.X, c.Y));
        Assert.False(f.InSight(32, 7));
    }

    [Fact]
    public void InFrontOfGrate_SeesThroughGrateCellsOnly()
    {
        // 창살 방 아래 벽(ty 12) 바로 앞 (32,16). 창살 36 = "Xw.." → (30,12)·(32,12) 기둥 X, (31,12)·(33,12) 걷기만 w
        var (_, f, a) = At(32, 16);
        Assert.True(a.BlocksWalk(31, 12));
        Assert.False(a.BlocksSight(31, 12));
        Assert.False(a.BlocksProjectiles(31, 12));
        Assert.True(a.BlocksSight(32, 12));

        Assert.True(f.InSight(31, 12));
        Assert.True(f.InSight(33, 12));
        Assert.True(f.InSight(31, 11));     // 창살 칸 너머
        Assert.True(f.InSight(33, 11));
        Assert.False(f.InSight(32, 11));    // 기둥 칸 뒤
        Assert.False(f.InSight(32, 7));     // 방 기준점은 쐐기 사이
        Assert.False(f.InSight(27, 8));
        Assert.False(f.InSight(36, 4));

        var floors = RoomFloors.Compute(RealData.Cathedral, a);
        Assert.True(floors.TryGet("room_grate", out var grate));
        var seen = grate.Cells.Count(c => f.InSight(c.X, c.Y));
        Assert.InRange(seen, 4, grate.Cells.Count / 3);   // 창 너머 쐐기만
    }

    [Fact]
    public void AtGateNw_OutsideMapSeenOnlyThroughGateGap()
    {
        // 뒤 문 gate_nw 진입점 (4, 19.5) → 칸 (4, 20). 둘레 벽(x 0 열) 너머 맵 밖(바로 뒤 6칸)은 문 틈(하위 18..21)으로 난 쐐기만 보인다
        var (t, f, a) = At(4, 20);
        Assert.Equal(new Int2(4, 20), f.CellOf(t.Hero.Pos));
        var outside = 0;
        for (var ty = -VisibilityField.Pad; ty < 40 + VisibilityField.Pad; ty++)
            for (var tx = -6; tx < 0; tx++)
            {
                if (!f.InSight(tx, ty)) continue;
                outside++;
                Assert.InRange(ty, 13, 27);   // 문 틈(18..21)에서 퍼지는 쐐기
            }
        Assert.True(outside > 0);
        Assert.True(f.InSight(-3, 19));
        Assert.False(f.InSight(-3, 12));    // 벽 뒤
        Assert.False(f.InSight(-3, 28));
        Assert.True(a.BlocksSight(0, 17));  // 문 틈 양옆 벽
        Assert.True(a.BlocksSight(0, 22));
    }
}
