using DiabloDefenceRemastered.Core.Geometry;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary><c>ArenaCollision</c>·clearance (설계서 4.2, 10.1 <c>Nav</c> 첫 줄).</summary>
public class CollisionShapeTests
{
    /// <summary>합성 타일셋의 성질 글자를 모두 쓰는 맵: 기둥 x, 걷기만 w, 투사체만 m, W·M·L, X 벽 줄, 두 줄 벽.</summary>
    private static MapDef MixedMap(TilesetDef? tileset = null) => TestDefs.Map(12, "test_mixed", tileset,
    [
        new() { Piece = "pillar", At = [new(3, 3), new(8, 3)] },
        new() { Piece = "low_wall", Line = [new(3, 5), new(5, 5)] },
        new() { Piece = "glass", Line = [new(7, 5), new(8, 5)] },
        new() { Piece = "walk_sight_wall", At = [new(3, 7)] },
        new() { Piece = "sight_missile_wall", At = [new(5, 7)] },
        new() { Piece = "sight_only", At = [new(7, 7)] },
        new() { Piece = "inner_wall", Line = [new(3, 9), new(4, 9)] },
        new() { Piece = "thick_wall", At = [new(8, 8)] },
    ]);

    [Fact]
    public void Shapes_UnionEqualsBlockedCells_PointSamples()
    {
        foreach (var map in new[] { MixedMap(), RealData.Cathedral })
        {
            var grid = ArenaLayout.Expand(map);
            var shapes = ArenaCollision.Build(grid);
            var polys = shapes.Select(s => s.Corners.ToArray()).ToArray();
            var rng = new Random(7);
            var box = grid.Iso.BoxSize;
            var samples = 0;
            while (samples < 10_000)
            {
                var p = new Vector2((float)rng.NextDouble() * box.X, (float)rng.NextDouble() * box.Y);
                var t = grid.Iso.ToTile(p);
                // 칸 경계 위 표본은 버린다(어느 칸인지 부동소수에 달림)
                var fx = t.Tx + 0.5f - MathF.Floor(t.Tx + 0.5f);
                var fy = t.Ty + 0.5f - MathF.Floor(t.Ty + 0.5f);
                if (fx < 0.01f || fx > 0.99f || fy < 0.01f || fy > 0.99f) continue;
                samples++;
                var c = t.Cell;
                if (!grid.IsInside(c.X, c.Y)) continue;
                var walkCell = grid.BlocksWalk(c.X, c.Y);
                var projCell = grid.BlocksProjectiles(c.X, c.Y);
                var walkShape = false;
                var projShape = false;
                for (var i = 0; i < shapes.Count; i++)
                {
                    if (!Geo.Contains(polys[i], p)) continue;
                    walkShape |= shapes[i].BlocksWalk;
                    projShape |= shapes[i].BlocksProjectiles;
                }
                Assert.True(walkCell == walkShape, $"{map.Id} 걷기 {p} 칸 {c}");
                Assert.True(projCell == projShape, $"{map.Id} 투사체 {p} 칸 {c}");
            }
        }
    }

    [Fact]
    public void Shapes_WalkOnlyAndProjectileOnly_AndBoundaryFlag()
    {
        var grid = ArenaLayout.Expand(MixedMap());
        var shapes = ArenaCollision.Build(grid);
        foreach (var s in shapes)
            for (var ty = s.Tiles.Y0; ty < s.Tiles.Y1; ty++)
                for (var tx = s.Tiles.X0; tx < s.Tiles.X1; tx++)
                {
                    Assert.Equal(grid.BlocksWalk(tx, ty), s.BlocksWalk);
                    Assert.Equal(grid.BlocksProjectiles(tx, ty), s.BlocksProjectiles);
                    Assert.Equal(grid.IsBoundary(tx, ty), s.IsBoundary);
                }
        // 걷기만(w): 투사체 선이 지나간다, 투사체만(m): 걷기는 트였다
        var nav = new NavWorld(grid, default);
        var iso = grid.Iso;
        var lowWall = iso.CellCenter(6, 10);   // low_wall (3,5) 위 줄 왼쪽 칸
        Assert.True(grid.BlocksWalk(6, 10) && !grid.BlocksProjectiles(6, 10));
        Assert.True(nav.ProjectileLineClear(lowWall - new Vector2(0, 60), lowWall + new Vector2(0, 60)));
        Assert.True(nav.ExactClearance(lowWall) < 0f);
        var glass = iso.CellCenter(14, 10);
        Assert.True(!grid.BlocksWalk(14, 10) && grid.BlocksProjectiles(14, 10));
        Assert.False(nav.ProjectileLineClear(glass - new Vector2(0, 60), glass + new Vector2(0, 60)));
        Assert.True(nav.ExactClearance(glass) > 0f);
        // 기둥은 둘레가 아니다, 테두리는 둘레
        Assert.Contains(shapes, s => s.IsBoundary);
        Assert.False(shapes.Single(s => s.Tiles == new RectI(6, 6, 7, 7)).IsBoundary);
    }

    [Fact]
    public void Shapes_SightLettersDoNotChangeShapes()
    {
        // 성질 표의 시야 비트만 뒤집은 타일셋(대문자 ↔ 소문자) → 도형 목록이 같다(4.2 끝)
        var ts = TestDefs.Tileset();
        var flipped = ts with
        {
            Props = ts.Props.ToDictionary(kv => kv.Key, kv => new string([.. kv.Value.Select(Flip)])),
        };
        var a = ArenaCollision.Build(ArenaLayout.Expand(MixedMap(ts), ts));
        var b = ArenaCollision.Build(ArenaLayout.Expand(MixedMap(flipped), flipped));
        Assert.Equal(a.Select(s => (s.Tiles, s.BlocksWalk, s.BlocksProjectiles, s.IsBoundary)), b.Select(s => (s.Tiles, s.BlocksWalk, s.BlocksProjectiles, s.IsBoundary)));

        static char Flip(char c) => c switch { 'X' => 'x', 'x' => 'X', 'W' => 'w', 'w' => 'W', 'M' => 'm', 'm' => 'M', 'L' => '.', _ => c };
    }

    [Fact]
    public void Clearance_KnownValues()
    {
        var grid = ArenaLayout.Expand(TestDefs.Map(12));
        var nav = new NavWorld(grid, default);
        var iso = grid.Iso;
        // 기둥 (3,3) = 하위 (6,6) 위-왼 칸 하나: 칸 마름모 아래 꼭짓점에서 10px 아래 → 10
        var p = iso.CellCenter(6, 6) + new Vector2(0, IsoMath.HalfHeight + 10);
        Assert.Equal(10f, nav.ExactClearance(p), 3);
        // 기둥 안은 음수, 맵 밖(바깥 마름모 밖)은 경계까지 음수 거리
        Assert.True(nav.ExactClearance(iso.CellCenter(6, 6)) < 0f);
        var outside = iso.DiamondTop - new Vector2(0, 50);
        Assert.Equal(iso.SignedDistanceToBoundary(outside), nav.ExactClearance(outside), 3);
        Assert.True(nav.Clearance(outside) < 0f);
        // 격자 값 = 칸 중심의 정확한 값, 이중선형은 사이 값
        var g = nav.Grid;
        Assert.Equal(nav.ExactClearance(g.Center(40, 30)), g.At(40, 30), 3);
        var mid = (g.Center(40, 30) + g.Center(41, 30)) * 0.5f;
        Assert.Equal((g.At(40, 30) + g.At(41, 30)) * 0.5f, nav.Clearance(mid), 3);
    }

    [Fact]
    public void Clearance_BoxWorldWithoutMap()
    {
        var nav = new NavWorld(null, new Vector2(320, 160));
        Assert.Empty(nav.Collision);
        Assert.Equal(8f, nav.Grid.At(0, 0), 3);
        Assert.Equal(40f, nav.ExactClearance(new Vector2(40, 80)), 3);
        Assert.True(nav.IsInsideMap(new Vector2(10, 10)));
        Assert.False(nav.IsInsideMap(new Vector2(-1, 10)));
    }
}
