using DiabloAssetImporter.Bake;
using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Pipeline;
using DiabloAssetImporter.Tests.TestKit;

namespace DiabloAssetImporter.Tests;

/// <summary>설계서 10.1 <c>Bake</c>: 잘라 묶기·마법 방향·아이콘·아레나·성질 대조.</summary>
public class BakeTests
{
    private static RgbaImage Cell(int w, int h, (int X, int Y, int W, int H)? body, byte alpha = 255)
    {
        var img = new RgbaImage(w, h);
        if (body is { } b)
            for (var y = b.Y; y < b.Y + b.H; y++)
                for (var x = b.X; x < b.X + b.W; x++) img.Set(x, y, 200, 100, 50, alpha);
        return img;
    }

    [Fact]
    public void Pack_NoOverlap_InsidePage_OrderFixed_EmptyFrame_ShadowOnlyRowKept()
    {
        var frames = new List<RgbaImage>
        {
            Cell(40, 50, (5, 10, 20, 30)),
            Cell(40, 50, null),                       // 빈 프레임
            Cell(40, 50, (0, 45, 40, 2), alpha: 128), // 그림자만 있는 줄(반투명) — 알파 > 0 이면 사각형에 남는다
        };
        var sheet = SheetPacker.Pack([new PackAction("walk", new Int2(40, 50), 1, 3, [frames]), new PackAction("attack", new Int2(40, 50), 1, 3, [frames])]);
        Assert.Empty(sheet.Meta.Validate());
        Assert.Equal(["attack", "walk"], sheet.Meta.Actions.Keys.Order(StringComparer.Ordinal));
        var walk = sheet.Meta.Actions["walk"];
        Assert.True(walk.Rects[1].IsEmpty);
        Assert.Equal((0, 45, 40, 2), (walk.Rects[2].Ox, walk.Rects[2].Oy, walk.Rects[2].W, walk.Rects[2].H));
        var rects = sheet.Meta.Actions.Values.SelectMany(a => a.Rects).Where(r => !r.IsEmpty).ToList();
        for (var i = 0; i < rects.Count; i++)
            for (var j = i + 1; j < rects.Count; j++)
                Assert.False(rects[i].Page == rects[j].Page && rects[i].X < rects[j].X + rects[j].W && rects[j].X < rects[i].X + rects[i].W
                             && rects[i].Y < rects[j].Y + rects[j].H && rects[j].Y < rects[i].Y + rects[i].H, $"겹침 {rects[i]} {rects[j]}");
        // 순서 고정: (동작 이름 서수, 방향, 프레임) — attack 이 먼저 놓인다
        Assert.True(sheet.Meta.Actions["attack"].Rects[0].Y <= walk.Rects[0].Y);
        var page = sheet.VariantPages[0][0];
        Assert.Equal(128, page.A(walk.Rects[2].X, walk.Rects[2].Y));
    }

    [Fact]
    public void Pack_Overflow_GoesToNextPageByAction_VariantsShareMeta()
    {
        var big = Enumerable.Range(0, 4).Select(_ => Cell(100, 100, (0, 0, 100, 100))).ToList();
        var other = big.Select(f => { var c = Cell(100, 100, (0, 0, 100, 100)); c.Pixels[0] = 1; return c; }).ToList();
        var sheet = SheetPacker.Pack(
            [new PackAction("a", new Int2(100, 100), 1, 4, [big, other]), new PackAction("b", new Int2(100, 100), 1, 4, [big, other])], maxPage: 256);
        Assert.Empty(sheet.Meta.Validate());
        Assert.True(sheet.Meta.Pages.Count >= 2);
        Assert.All(sheet.Meta.Actions["b"].Rects, r => Assert.True(r.Page >= 1));   // 동작 단위로 다음 페이지
        Assert.Equal(2, sheet.VariantPages.Count);
        Assert.Equal(sheet.VariantPages[0].Count, sheet.VariantPages[1].Count);
    }

    [Fact]
    public void Missile_DirFiles_And_FramesAreDirs()
    {
        var files = Enumerable.Range(0, 16).Select(_ => SynthSources.FlatCl2(32, 5)).ToList();
        var e = new ImportMissileEntry { Out = "d1/missiles/x", Cl2 = "missiles\\x{d}.cl2", Dirs = 16, Width = 32 };
        var d = UnitBaker.DecodeMissile("x", e, files, null);
        Assert.Equal((16, 5), (d.Directions, d.Frames));
        var arrows = new ImportMissileEntry { Out = "d1/missiles/arrows", Cl2 = "missiles\\arrows.cl2", FramesAreDirs = 16, Width = 32 };
        var a = UnitBaker.DecodeMissile("arrows", arrows, [SynthSources.FlatCl2(32, 16)], null);
        Assert.Equal((16, 1), (a.Directions, a.Frames));
    }

    [Fact]
    public void Icon_LayersOverlap_FitsDarkSquare()
    {
        var e = new ImportIconEntry
        {
            Out = "d1/icons/multishot", Cl2 = "missiles\\arrows.cl2", FramesAreDirs = 16, Width = 32,
            Layers = [new ImportIconLayer { Dir = 3, Frame = 1, Offset = new Int2(-6, 4) }, new ImportIconLayer { Dir = 4, Frame = 1 }, new ImportIconLayer { Dir = 5, Frame = 1, Rotate = 12 }],
        };
        var asMissile = new ImportMissileEntry { Out = e.Out, Cl2 = e.Cl2, Width = 32, FramesAreDirs = 16 };
        var d = UnitBaker.DecodeMissile("x", asMissile, [SynthSources.FlatCl2(32, 16)], null);
        var img = IconBaker.Bake(e, d, Palette.Read(Encoders.Pal()), 48, new Rgb(16, 12, 10));
        Assert.Equal((48, 48), (img.Width, img.Height));
        Assert.Equal(new byte[] { 16, 12, 10, 255 }, img.Pixels[0..4]);   // 모서리 = 바탕
        Assert.Contains(Enumerable.Range(0, 48 * 48), i => img.Pixels[i * 4] != 16);   // 가운데에 그림
        Assert.All(Enumerable.Range(0, 48 * 48), i => Assert.Equal(255, img.Pixels[i * 4 + 3]));
        // 큰 그림은 정수 상자 평균으로 줄인다
        var bigD = UnitBaker.DecodeMissile("y", new ImportMissileEntry { Out = "d1/missiles/y", Cl2 = "y.cl2", Width = 160 }, [SynthSources.FlatCl2(160, 2, 150)], null);
        var bigIcon = IconBaker.Bake(new ImportIconEntry { Out = "d1/icons/y", Cl2 = "y.cl2", Width = 160, Layers = [new ImportIconLayer { Frame = 1 }] },
                                     bigD, Palette.Read(Encoders.Pal()), 48, new Rgb(0, 0, 0));
        Assert.Equal(48, bigIcon.Width);
    }

    [Fact]
    public void Icon_Cel_DecodesSignedRuns_AndKeepsOpaqueColors()
    {
        // 폭 4, 두 줄(아래 줄부터): 아래 = 색인 200 넷, 위 = 투명 둘 + 색인 7 둘. CEL 은 0x00..0x7F 가 그대로, 0x80.. 가 투명(CL2 와 반대)
        byte[] frame = [0x04, 200, 200, 200, 200, 0xFE, 0x02, 7, 7];
        var file = new byte[12 + frame.Length];
        BitConverter.GetBytes(1).CopyTo(file, 0);
        BitConverter.GetBytes(12).CopyTo(file, 4);
        BitConverter.GetBytes(12 + frame.Length).CopyTo(file, 8);
        frame.CopyTo(file, 12);
        var e = new ImportIconEntry { Out = "d1/icons/z", Cel = @"ctrlpan\spelicon.cel", Width = 4, Layers = [new ImportIconLayer { Frame = 1 }] };
        var d = JobPlanner.DecodeCelIcon(e, file);
        Assert.Equal((4, 2, 1, 1), (d.Cell.X, d.Cell.Y, d.Directions, d.Frames));
        var img = d.Images[0];
        Assert.Equal(new[] { false, false, true, true, true, true, true, true }, img.Opaque);
        Assert.Equal((byte)7, img.Index[3]);
        Assert.Equal((byte)200, img.Index[4]);
        // 1:1 로 옮기면(4×2 그림은 4×4 칸의 가운데 1~2 줄) 불투명 칸은 팔레트 색 그대로다(바탕과 섞여 어두워지지 않는다)
        var pal = Palette.Read(Encoders.Pal());
        var icon = IconBaker.Bake(e, d, pal, 4, new Rgb(16, 12, 10));
        var (r, g, b) = pal[200];
        Assert.Equal(new byte[] { r, g, b, 255 }, icon.Pixels[((2 * 4 + 0) * 4)..((2 * 4 + 0) * 4 + 4)]);
    }

    private static (ArenaGrid Grid, TilesetSource Ts) RealMapSynthTiles()
    {
        var data = MapDataReader.Read(Repo.DataDir);
        var map = data.Maps.First(m => m.Id == "cathedral_arena");
        var grid = ArenaLayout.Expand(map);
        var table = Repo.Table();
        var entry = table.Tilesets.Tilesets.First(t => t.Id == "l1");
        var files = SynthSources.Tileset(entry, grid.Tileset);
        return (grid, TilesetSource.From(entry, new SourceBag(files)));
    }

    [Fact]
    public void Arena_FloorChunksMatchRule_ColumnsDeduped_CellsSorted_FloorMeetsColumnBottom()
    {
        var (grid, ts) = RealMapSynthTiles();
        var outs = ArenaBaker.Bake(grid, ts, Palette.Read(Encoders.Pal()), null);
        var art = ArenaArt.Parse(System.Text.Encoding.UTF8.GetString(outs.First(o => o.Path.EndsWith("arena.json", StringComparison.Ordinal)).Bytes));
        Assert.Empty(art.Validate(grid));
        Assert.Equal(ArenaArt.OutputNames(grid), outs.Select(o => o.Path).Where(p => !p.Contains("_p", StringComparison.Ordinal)).Order(StringComparer.Ordinal));
        var meta = SpriteMeta.Parse(System.Text.Encoding.UTF8.GetString(outs.First(o => o.Path.EndsWith("columns.json", StringComparison.Ordinal)).Bytes));
        Assert.Empty(meta.Validate());
        // 같은 그림은 한 프레임 — 합성 타일셋은 기둥 그림 종류가 적다
        Assert.True(meta.Actions["default"].Frames < art.Cells.Count);

        // 1px 틈·겹침 없음: 바닥 칸의 바닥 단 마름모 맨 아래 줄(C.y+15)과 같은 식으로 놓은 기둥 그림 마지막 줄이 같은 월드 줄이다
        var iso = grid.Iso;
        var c = iso.CellCenter(5, 5);
        var columnTop = c.Y + 16 - iso.ColumnHeight;
        Assert.Equal(c.Y + 15, columnTop + iso.ColumnHeight - 1);
        var floorBottomRow = c.Y - 16 + 31;   // 바닥 단 64×32 를 (C.x−32, C.y−16)에 놓은 마지막 줄
        Assert.Equal(c.Y + 15, floorBottomRow);
        // 바닥 조각 그림에서 실제로 C.y−16 줄은 비고 C.y−15..C.y+15 가 칠해진다
        var floors = new Dictionary<(int, int), RgbaImage>();
        var (fx, fy) = Enumerable.Range(0, grid.Size).SelectMany(y => Enumerable.Range(0, grid.Size).Select(x => (x, y))).First(p => !grid.BlocksWalk(p.x, p.y) && !grid.BlocksProjectiles(p.x, p.y));
        var bottom = RgbaImage.FromIndexed(ts.Column(ts.Til.Subtile(grid.MegaTile(fx / 2, fy / 2), (fy % 2) * 2 + fx % 2), false, true), Palette.Read(Encoders.Pal()), null, null);
        floors[(fx, fy)] = bottom.Crop(0, iso.ColumnHeight - 32, 64, 32);
        var cc = iso.CellCenter(fx, fy);
        var region = ArenaBaker.FloorRegion(grid, floors, (int)cc.X - 32, (int)cc.Y - 16, 64, 32);
        Assert.All(Enumerable.Range(0, 64), x => Assert.Equal(0, region.A(x, 0)));
        Assert.Equal(255, region.A(31, 31));
        Assert.Equal(255, region.A(32, 1));
    }

    [Fact]
    public void PropCheck_TableVsSol_KnownDiffPasses_SightOnlyDiffFails()
    {
        var (grid, ts) = RealMapSynthTiles();
        var def = grid.Tileset;
        Assert.Empty(ArenaBaker.CheckProps(def, ts));
        // 원본 SOL 의 메가 15 하위 0 을 시야 비트만 다르게(x → X 반대로) — 실패
        var sub = ts.Til.Subtile(15, 0);
        var keep = ts.Sol[sub];
        ts.Sol[sub] = (byte)(keep ^ 2);
        var p = ArenaBaker.CheckProps(def, ts);
        Assert.Single(p);
        Assert.Contains("메가타일 15 하위 칸 0", p[0]);
        Assert.Contains("시야 막힘 다름", p[0]);
        // sol_known_diffs 에 그 원본 글자로 있으면 통과
        var withDiff = def with { SolKnownDiffs = [.. def.SolKnownDiffs, new SolDiffDef { Tile = 15, Sub = 0, Sol = Sol.PropChar(ts.Sol[sub]).ToString(), Use = def.Props[15][0].ToString(), Why = "시험" }] };
        Assert.Empty(ArenaBaker.CheckProps(withDiff, ts));
        ts.Sol[sub] = keep;
    }
}
