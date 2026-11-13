using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Upscale;

namespace DiabloAssetImporter.Bake;

/// <summary>타일셋 원본 묶음(주 스레드가 읽은 바이트를 푼 것). 맵 굽기·성질 대조·audit 가 같이 쓴다.</summary>
internal sealed class TilesetSource(ImportTilesetEntry entry, TilFile til, MinFile min, byte[] sol, byte[] cel, byte[]? overlayCel)
{
    public ImportTilesetEntry Entry { get; } = entry;
    public TilFile Til { get; } = til;
    public MinFile Min { get; } = min;
    public byte[] Sol { get; } = sol;
    private readonly IReadOnlyList<ReadOnlyMemory<byte>> _cel = LevelCel.Frames(cel);
    private readonly IReadOnlyList<ReadOnlyMemory<byte>>? _overlay = overlayCel is null ? null : LevelCel.Frames(overlayCel);

    public static IReadOnlyList<string> SourcePaths(ImportTilesetEntry e)
    {
        var list = new List<string> { e.SourcePath(e.Til), e.SourcePath(e.Min), e.SourcePath(e.Sol), e.SourcePath(e.Cel) };
        if (e.Overlay is { } o) list.Add(e.SourcePath(o.Cel));
        return list;
    }

    public static TilesetSource From(ImportTilesetEntry e, SourceBag bag) => new(
        e, TilFile.Read(bag.Get(e.SourcePath(e.Til))), MinFile.Read(bag.Get(e.SourcePath(e.Min)), e.PiecesPerSubtile),
        bag.Get(e.SourcePath(e.Sol)), bag.Get(e.SourcePath(e.Cel)), e.Overlay is { } o ? bag.Get(e.SourcePath(o.Cel)) : null);

    public IndexedImage Piece(ushort v)
    {
        var frame = MinFile.Frame(v);
        if (frame < 1 || frame > _cel.Count) throw new ImportCheckException($"타일셋 {Entry.Id}: CEL 프레임 {frame} 이 1..{_cel.Count} 밖이다");
        return LevelCel.Decode(_cel[frame - 1].Span, MinFile.Kind(v));
    }

    public IndexedImage? OverlayFrame(int frame) =>
        _overlay is null || frame < 1 || frame > _overlay.Count ? null : LevelCel.DecodeOverlay(_overlay[frame - 1].Span);

    /// <summary>하위 타일 기둥(64 × 조각 수/2·32). <paramref name="upperOnly"/> 면 바닥 단을 비운다.</summary>
    public IndexedImage Column(int subtile, bool upperOnly, bool bottomOnly = false)
    {
        var n = Min.PiecesPerSubtile;
        var tiers = n / 2;
        var img = new IndexedImage(64, tiers * 32);
        for (var k = 0; k < n; k++)
        {
            var tier = k / 2;
            var isBottom = tier == tiers - 1;
            if (upperOnly && isBottom) continue;
            if (bottomOnly && !isBottom) continue;
            var v = Min.Piece(subtile, k);
            if (v == 0) continue;
            var p = Piece(v);
            var x0 = (k % 2) * 32;
            var y0 = tier * 32;
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    var s = y * 32 + x;
                    if (!p.Opaque[s]) continue;
                    var t = (y0 + y) * 64 + x0 + x;
                    img.Index[t] = p.Index[s];
                    img.Opaque[t] = true;
                }
        }
        return img;
    }
}

/// <summary>
/// 아레나 굽기 (1.4 "아레나", 7.6): 펼친 격자 → TIL → MIN → 레벨 CEL. 바닥 칸(성질 <c>.</c>·<c>L</c>)의 바닥 단 → 512×512 조각,
/// 그 밖 → 하위 타일 기둥 아틀라스(같은 그림은 한 프레임), 아치 덧그림, <c>arena.json</c>, 미니맵(외곽 상자 1/16).
/// 기둥 그림 왼쪽 위 = <c>(C.x − 32, C.y + 16 − Hc)</c>, 바닥 단은 <c>(C.x − 32, C.y − 16)</c> 에 같은 식으로 그린다 — 1px 틈·겹침 없음.
/// </summary>
internal static class ArenaBaker
{
    public const string ColumnsAction = "default";

    public static List<(string Path, byte[] Bytes, int Scale)> Bake(ArenaGrid grid, TilesetSource ts, Palette pal, IPlaneUpscaler? up)
    {
        var art = grid.Map.Art.TrimEnd('/');
        var iso = grid.Iso;
        var hc = iso.ColumnHeight;
        var floorRgba = new Dictionary<(int, int), RgbaImage>();   // 칸 → 바닥 단 64×32
        var cells = new List<ArenaCell>();
        var frames = new List<RgbaImage>();
        var frameIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        int AddFrame(RgbaImage img)
        {
            var key = UpscalePlane.HashOf(img);
            if (frameIndex.TryGetValue(key, out var f)) return f;
            frames.Add(img);
            return frameIndex[key] = frames.Count;   // 1부터
        }

        for (var ty = 0; ty < grid.Size; ty++)
            for (var tx = 0; tx < grid.Size; tx++)
            {
                var mega = grid.MegaTile(tx / 2, ty / 2);
                var sub = ts.Til.Subtile(mega, (ty % 2) * 2 + tx % 2);
                var floor = (grid.Flags(tx, ty) & (CellFlags.BlocksWalk | CellFlags.BlocksProjectiles)) == 0;
                if (floor)
                {
                    var bottom = ts.Column(sub, upperOnly: false, bottomOnly: true);
                    var full = RgbaImage.FromIndexed(bottom, pal, null, null);
                    floorRgba[(tx, ty)] = full.Crop(0, hc - 32, 64, 32);
                }
                var col = ts.Column(sub, upperOnly: floor);
                var colImg = Fit(RgbaImage.FromIndexed(col, pal, null, null), hc);
                if (colImg.AlphaBounds() is not null)
                    cells.Add(new ArenaCell(tx, ty, floor ? ArenaCellKind.FloorPiece : ArenaCellKind.Wall, AddFrame(colImg)));
                if (ts.Entry.Overlay is { } o)
                    foreach (var (ofr, list) in o.Frames.OrderBy(p => p.Key))
                        if (list.Contains(sub + 1) && ts.OverlayFrame(ofr) is { } oimg)
                        {
                            cells.Add(new ArenaCell(tx, ty, ArenaCellKind.Overlay, AddFrame(Fit(RgbaImage.FromIndexed(oimg, pal, null, null), hc))));
                            break;
                        }
            }
        cells.Sort((a, b) => a.Ty != b.Ty ? a.Ty.CompareTo(b.Ty) : a.Tx != b.Tx ? a.Tx.CompareTo(b.Tx) : a.Kind.CompareTo(b.Kind));

        // 문 봉인 나무 문(2026-11-13, 타일셋 doors): 닫힌·열린 하위 칸 기둥을 같은 아틀라스에 덤으로 — 열린 문 하위 칸은 어떤 메가타일에도 없다
        var doors = new List<ArenaDoorCell>();
        foreach (var (side, list) in grid.Tileset.Doors.OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (var d in list)
            {
                int Frame(int sub) => AddFrame(Fit(RgbaImage.FromIndexed(ts.Column(sub - 1, upperOnly: false), pal, null, null), hc));
                // 열린 문 위 아치 덧그림(원작 L1S 프레임 — 문을 열면 원작이 그 칸에 얹는다)
                var overlay = d.OpenOverlay is { } of && ts.OverlayFrame(of) is { } oimg ? AddFrame(Fit(RgbaImage.FromIndexed(oimg, pal, null, null), hc)) : 0;
                doors.Add(new ArenaDoorCell { Side = side, Dx = d.At.X, Dy = d.At.Y, Closed = Frame(d.Closed), Open = Frame(d.Open), OpenOverlay = overlay });
            }

        var scale = up is null ? 1 : 2;
        var outputs = new List<(string, byte[], int)>();
        var chunks = ArenaArt.FloorChunks(grid);
        foreach (var chunk in chunks)
        {
            var pos = ArenaArt.FloorChunkPos(chunk);
            var img = FloorRegion(grid, floorRgba, pos.X, pos.Y, ArenaArt.ChunkSize, ArenaArt.ChunkSize);
            if (up is not null)
            {
                // ③ 바닥: 이웃 바닥을 32px 더 구운 판 → 확대 → 가운데. 알파는 원본 2배 점 확대
                const int extra = 32;
                var wide = FloorRegion(grid, floorRgba, pos.X - extra, pos.Y - extra, ArenaArt.ChunkSize + extra * 2, ArenaArt.ChunkSize + extra * 2);
                var plane = UpscaleMath.FillTransparent(wide, extra * 2);
                var up4 = up.Up4($"floor_{chunk.X}_{chunk.Y}", plane);
                var col2 = UpscaleMath.CropBox2(up4, ArenaArt.ChunkSize, ArenaArt.ChunkSize, extra);
                img = UpscaleMath.WithNearestAlpha(col2, img);
            }
            outputs.Add(($"{art}/{ArenaArt.FloorChunkFile(chunk)}", Png.Encode(img), scale));
        }

        var colFrames = frames;
        if (up is not null)
            colFrames = [.. frames.Select((f, i) =>
            {
                var up4 = up.Up4($"col/{i + 1:D5}", UpscaleMath.ColorPlane(f));
                return UpscaleMath.WithNearestAlpha(UpscaleMath.CropBox2(up4, f.Width, f.Height, UpscaleMath.Pad), f);
            })];
        if (colFrames.Count == 0) colFrames = [new RgbaImage(64 * scale, hc * scale)];
        var sheet = SheetPacker.Pack([new PackAction(ColumnsAction, new Int2(64 * scale, hc * scale), 1, colFrames.Count, [colFrames])], scale);
        // 빈 아틀라스(칸 그림이 하나도 없는 맵)도 페이지 하나를 둔다
        outputs.Add(($"{art}/columns.png", Png.Encode(sheet.VariantPages[0][0]), scale));
        for (var p = 1; p < sheet.VariantPages[0].Count; p++)
            outputs.Add(($"{art}/{SpriteMeta.PageFile("columns.png", p)}", Png.Encode(sheet.VariantPages[0][p]), scale));
        outputs.Add(($"{art}/columns.json", AssetJson.Serialize(sheet.Meta), scale));

        var box = iso.BoxSize;
        var arena = new ArenaArt
        {
            Format = ArenaArt.CurrentFormat, Map = grid.Map.Id, LayoutHash = grid.LayoutHash, Box = new Int2((int)box.X, (int)box.Y), Scale = scale,
            Floor = [.. chunks.Select(c => new ArenaFloorChunk { File = ArenaArt.FloorChunkFile(c), Pos = ArenaArt.FloorChunkPos(c) })],
            Columns = "columns", Cells = cells, Doors = doors,
        };
        outputs.Add(($"{art}/{ArenaArt.FileName}", AssetJson.Serialize(arena), scale));
        outputs.Add(($"{art}/{ArenaArt.MinimapFile}", Png.Encode(Minimap(grid)), 1));
        outputs.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return outputs;
    }

    /// <summary>기둥 그림을 64×Hc 칸에 바닥 맞춤(덧그림이 더 낮거나 높아도 같은 칸).</summary>
    private static RgbaImage Fit(RgbaImage img, int hc)
    {
        if (img.Width == 64 && img.Height == hc) return img;
        var r = new RgbaImage(64, hc);
        var h = Math.Min(hc, img.Height);
        r.Blit(img, 0, img.Height - h, Math.Min(64, img.Width), h, 0, hc - h);
        return r;
    }

    /// <summary>월드 px 사각형에 바닥 칸의 바닥 단을 그린다.</summary>
    public static RgbaImage FloorRegion(ArenaGrid grid, Dictionary<(int, int), RgbaImage> floors, int x0, int y0, int w, int h)
    {
        var img = new RgbaImage(w, h);
        foreach (var ((tx, ty), tile) in floors.OrderBy(p => p.Key.Item2).ThenBy(p => p.Key.Item1))
        {
            var c = grid.Iso.CellCenter(tx, ty);
            int wx = (int)c.X - 32 - x0, wy = (int)c.Y - 16 - y0;
            if (wx >= w || wy >= h || wx + 64 <= 0 || wy + 32 <= 0) continue;
            img.DrawOver(tile, wx, wy);
        }
        return img;
    }

    /// <summary>미니맵: 외곽 상자 1/16, 바닥 밝게·막힌 칸 어둡게, 마름모 밖 투명.</summary>
    public static RgbaImage Minimap(ArenaGrid grid)
    {
        var box = grid.Iso.BoxSize;
        var mm = new RgbaImage(Math.Max(1, (int)box.X / 16), Math.Max(1, (int)box.Y / 16));
        for (var y = 0; y < mm.Height; y++)
            for (var x = 0; x < mm.Width; x++)
            {
                var cell = grid.Iso.CellOf(new Vector2(x * 16 + 8, y * 16 + 8));
                if (!grid.IsInside(cell.X, cell.Y)) continue;
                var blocked = grid.BlocksWalk(cell.X, cell.Y) || grid.BlocksProjectiles(cell.X, cell.Y);
                if (blocked) mm.Set(x, y, 58, 52, 46, 255);
                else mm.Set(x, y, 170, 158, 136, 255);
            }
        return mm;
    }

    /// <summary>
    /// 성질 대조 (1.4 "성질 대조"): 성질 표를 쓰는 메가타일마다 원본 SOL 의 <c>sol &amp; 7</c> 글자와 표 글자를 견준다.
    /// 다르면 그 칸이 <c>sol_known_diffs</c>·<c>design_diffs</c> 에 그 원본 글자로 있어야 한다. 문제 문구 목록(비면 통과).
    /// </summary>
    public static List<string> CheckProps(TilesetDef def, TilesetSource ts)
    {
        var problems = new List<string>();
        foreach (var (mega, props) in def.Props.OrderBy(p => p.Key))
        {
            for (var k = 0; k < 4 && k < props.Length; k++)
            {
                int sub;
                try { sub = ts.Til.Subtile(mega, k); }
                catch (InvalidDataException ex) { problems.Add($"타일셋 {def.Id} 메가타일 {mega}: {ex.Message}"); break; }
                if (sub >= ts.Sol.Length) { problems.Add($"타일셋 {def.Id} 메가타일 {mega} 하위 칸 {k}: 하위 타일 {sub} 이 SOL 밖이다"); continue; }
                var solChar = Sol.PropChar(ts.Sol[sub]);
                var tableChar = props[k];
                if (solChar == tableChar) continue;
                var known = def.SolKnownDiffs.Concat(def.DesignDiffs).Any(d =>
                    d.Tile == mega && d.Sub == k && d.Sol == solChar.ToString() && d.Use == tableChar.ToString());
                if (!known)
                    problems.Add($"타일셋 {def.Id} 메가타일 {mega} 하위 칸 {k}: 표 '{tableChar}', SOL '{solChar}'({Sol.Describe(tableChar, solChar)})");
            }
        }
        return problems;
    }
}
