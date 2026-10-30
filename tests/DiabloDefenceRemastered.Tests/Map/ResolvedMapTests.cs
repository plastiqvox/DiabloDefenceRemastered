namespace DiabloDefenceRemastered.Tests.Map;

public class ResolvedMapTests
{
    [Fact]
    public void Resolve_TileCoordsToWorldPx()
    {
        var map = TestDefs.Map(20);   // N 40 = 스테이지 1 크기
        var r = ResolvedMap.Resolve(map);
        var iso = r.Iso;
        Assert.Equal(40, iso.N);

        // 주인공 시작 = 맵 중심 (19.5, 19.5) → (1280, 128 + 16·39 + 16)
        Assert.Equal(new Vector2(1280, 768), r.HeroStart);
        Assert.Equal(Compass.South, r.HeroFacing);
        Assert.Equal(new Vector2(1280, 768), r.Anchor("center"));
        Assert.True(r.TryGetAnchor("center", out _));
        Assert.False(r.TryGetAnchor("nope", out _));

        Assert.Equal(["gate_nw", "gate_ne", "gate_se", "gate_sw"], r.Entries.Select(e => e.Id));   // 정의 순서
        foreach (var e in r.Entries)
        {
            Assert.False(iso.IsInsideMap(e.Outside), $"{e.Id} 바깥 시작점은 맵 밖");
            Assert.True(iso.IsInsideMap(e.Inside), $"{e.Id} 진입점은 맵 안");
        }
        var nw = r.Entry("gate_nw");
        Assert.Equal(iso.ToWorld(new TileCoord(-4, 19.5f)), nw.Outside);
        Assert.Equal(iso.ToWorld(new TileCoord(4, 19.5f)), nw.Inside);
        Assert.True(r.TryGetEntry("gate_se", out var se));
        Assert.Equal(iso.ToWorld(new TileCoord(34, 19.5f)), se.Inside);   // 앞 문 진입점 N − 6
        Assert.Throws<KeyNotFoundException>(() => r.Entry("gate_x"));
        Assert.Throws<KeyNotFoundException>(() => r.Anchor("nope"));
    }

    [Fact]
    public void ToWorld_TileOrAnchor()
    {
        var r = ResolvedMap.Resolve(TestDefs.Map(12));
        Assert.Equal(r.Anchor("center"), r.ToWorld(new TileOrAnchor("center")));
        Assert.Equal(r.Iso.CellCenter(3, 5), r.ToWorld(new TileOrAnchor(new TileCoord(3, 5))));
    }

    [Fact]
    public void World_HoldsArenaAndResolved_ForStage()
    {
        var defs = TestDefs.Basic(20);
        var world = new World(defs, 7, defs.Stages[0]);
        Assert.NotNull(world.Arena);
        Assert.NotNull(world.Resolved);
        Assert.Equal(new Vector2(2560, 1408), world.MapSize);       // 외곽 상자
        Assert.Equal(world.Resolved!.HeroStart, world.Hero.Pos);
        Assert.Equal(world.Arena!.LayoutHash, ArenaLayout.Expand(defs.Maps[0]).LayoutHash);

        var noMap = new World(defs, 7);
        Assert.Null(noMap.Arena);
        Assert.Null(noMap.Resolved);
        Assert.Equal(CameraMath.DefaultViewSize, noMap.MapSize);
    }
}
