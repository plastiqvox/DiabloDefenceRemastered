using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Map;

/// <summary>
/// 변 규칙(2026-11-15 사용자 지적 "끝에 있으면 안 되는 타일이 끝에 있어 잘려 보임", 설계서 6.11 "변 규칙"). 자원 없이 돈다.
/// </summary>
public class TileJoinsTests
{
    [Fact]
    public void Compatible_SameLabel_X_AndAlternatives()
    {
        Assert.True(TileJoins.Compatible("wv", "wv"));
        Assert.True(TileJoins.Compatible("x", "wh"));
        Assert.True(TileJoins.Compatible("wv|f", "f"));
        Assert.False(TileJoins.Compatible("wv", "f"));    // 세로 벽이 바닥에서 그냥 끝남 = 단면이 보임
        Assert.False(TileJoins.Compatible("f", "d"));     // 바닥과 흙이 턱 없이 만남
        var fade = new HashSet<string> { "d", "f" };
        Assert.True(TileJoins.EdgeOk("x", fade));
        Assert.True(TileJoins.EdgeOk("d|ly", fade));
        Assert.False(TileJoins.EdgeOk("wv", fade));
    }

    [Fact]
    public void RealData_EveryPropTileHasKind_AllMapsAndMirrorsPass()
    {
        var errors = new List<LayoutError>();
        TileJoins.ValidateTileset(RealData.L1, errors);
        Assert.Empty(errors);
        foreach (var map in RealData.Maps.Maps)
        {
            TileJoins.Validate(ArenaLayout.Expand(map), errors);
            if (map.Mirrored is { } mirror) TileJoins.Validate(ArenaLayout.Expand(mirror), errors);
        }
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void Validate_WallEndingOnFloor_IsError()
    {
        // 방 A 에 세로 벽 토막 하나(아래 끝을 기둥으로 닫지 않음) — 아래 바닥과 맞지 않는다
        var map = RealData.Maps.Maps.Single(m => m.Id == "cathedral_room_a");
        var bad = map with
        {
            Layout = map.Layout with
            {
                Place = [.. map.Layout.Place, new PiecePlacementDef { Piece = "wall_v", At = [new Int2(7, 3)] }],
            },
        };
        var errors = new List<LayoutError>();
        TileJoins.Validate(ArenaLayout.Expand(bad), errors);
        Assert.Contains(errors, e => e.Message.Contains("(7,3)") && e.Message.Contains("sw 변 'wv'"));
    }
}
