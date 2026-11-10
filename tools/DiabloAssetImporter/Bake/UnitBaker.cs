using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Upscale;

namespace DiabloAssetImporter.Bake;

/// <summary>대조 실패(종료 코드 4): 칸 폭·프레임 범위·형식·성질 표.</summary>
internal sealed class ImportCheckException(string message) : Exception(message);

/// <summary>원본 항목 바이트(주 스레드가 차례로 읽어 넘김). 키는 원작 경로(대소문자 무관).</summary>
internal sealed class SourceBag(IReadOnlyDictionary<string, byte[]> bytes)
{
    public byte[] Get(string path) =>
        bytes.TryGetValue(path, out var b) ? b : throw new InvalidOperationException($"원본 '{path}' 를 읽지 않았다(작업 명세 버그)");
}

/// <summary>
/// 몬스터·주인공·마법 그림 묶음 굽기 (1.4 변환 규칙): CL2 → 칸(폭 × 그 동작 최대 높이)에 바닥 맞춤 → 변종마다 색 → 잘라 묶기.
/// </summary>
internal static class UnitBaker
{
    /// <summary>풀어 놓은 동작 하나: 방향 × 프레임 색인 그림(칸 크기, 바닥 맞춤).</summary>
    internal sealed record DecodedAction(string Name, Int2 Cell, int Directions, int Frames, IReadOnlyList<IndexedImage> Images, float? ShadowAlpha);

    /// <summary>몬스터·주인공 동작 하나(8그룹 CL2).</summary>
    public static DecodedAction DecodeUnitAction(string owner, ResolvedImportAction a, byte[] cl2)
    {
        var groups = Cl2.ReadGroups(cl2, 8);
        var frames = groups.Select(g => Select(owner, a.Name, g, a.Frames)).ToList();
        var n = frames[0].Count;
        if (frames.Any(f => f.Count != n)) throw new ImportCheckException($"{owner} {a.Name}: 방향마다 프레임 수가 다르다");
        return Place(owner, a.Name, frames, a.Width, a.ShadowAlpha);
    }

    /// <summary>마법: 방향 파일들(평면) 또는 프레임 = 방향 또는 방향 없음.</summary>
    public static DecodedAction DecodeMissile(string owner, ImportMissileEntry e, IReadOnlyList<byte[]> files, float? headShadow)
    {
        List<IReadOnlyList<ReadOnlyMemory<byte>>> frames;
        if (e.FramesAreDirs is { } nd)
        {
            var all = Select(owner, "", Cl2.ReadFlat(files[0]), e.Frames);
            if (all.Count < nd) throw new ImportCheckException($"{owner}: 프레임 {all.Count}개 < 방향 {nd}");
            frames = [.. Enumerable.Range(0, nd).Select(i => (IReadOnlyList<ReadOnlyMemory<byte>>)[all[i]])];
        }
        else frames = [.. files.Select(f => Select(owner, "", Cl2.ReadFlat(f), e.Frames))];
        var n = frames[0].Count;
        if (frames.Any(f => f.Count != n)) throw new ImportCheckException($"{owner}: 방향 파일마다 프레임 수가 다르다");
        return Place(owner, "default", frames, e.Width, e.ShadowAlpha ?? headShadow);
    }

    private static IReadOnlyList<ReadOnlyMemory<byte>> Select(string owner, string action, IReadOnlyList<ReadOnlyMemory<byte>> all, int[]? range)
    {
        if (range is null) return all;
        if (range[1] > all.Count) throw new ImportCheckException($"{owner} {action}: 프레임 범위 [{range[0]}, {range[1]}] 가 파일 프레임 {all.Count}개 밖이다");
        return [.. all.Skip(range[0] - 1).Take(range[1] - range[0] + 1)];
    }

    private static DecodedAction Place(string owner, string action, List<IReadOnlyList<ReadOnlyMemory<byte>>> frames, int tableWidth, float? shadow)
    {
        int? width = null;
        foreach (var dir in frames)
            foreach (var f in dir)
                if (Cl2.EstimateWidth(f.Span) is { } w) { width = w; break; }
        if (width is { } fw && fw != tableWidth)
            throw new ImportCheckException($"{owner} {action}: 파일 폭 {fw}, 표 {tableWidth}".Replace("  ", " "));
        var cellW = width ?? tableWidth;
        var decoded = frames.SelectMany(d => d.Select(f => Cl2.Decode(f.Span, cellW))).ToList();
        var cellH = Math.Max(1, decoded.Max(d => d.Height));
        var placed = new List<IndexedImage>(decoded.Count);
        foreach (var d in decoded)
        {
            var img = new IndexedImage(cellW, cellH);
            var dy = cellH - d.Height;
            for (var y = 0; y < d.Height; y++)
            {
                Array.Copy(d.Index, y * cellW, img.Index, (y + dy) * cellW, cellW);
                Array.Copy(d.Opaque, y * cellW, img.Opaque, (y + dy) * cellW, cellW);
            }
            placed.Add(img);
        }
        return new DecodedAction(action, new Int2(cellW, cellH), frames.Count, frames[0].Count, placed, shadow);
    }

    /// <summary>
    /// 동작들 → 변종마다 RGBA → (확대면 ①~⑥) → 잘라 묶기. <paramref name="upscaler"/> 가 null 이면 원본 픽셀(배율 1).
    /// </summary>
    public static PackedSheet Bake(IReadOnlyList<DecodedAction> actions, Palette pal, IReadOnlyList<byte[]?> variantTrns, IPlaneUpscaler? upscaler, string job)
    {
        var packs = new List<PackAction>();
        foreach (var a in actions.OrderBy(a => a.Name, StringComparer.Ordinal))
        {
            var variants = new List<IReadOnlyList<RgbaImage>>();
            if (upscaler is null)
            {
                foreach (var trn in variantTrns)
                    variants.Add([.. a.Images.Select(i => RgbaImage.FromIndexed(i, pal, trn, a.ShadowAlpha))]);
                packs.Add(new PackAction(a.Name, a.Cell, a.Directions, a.Frames, variants));
                continue;
            }
            // ① 몸과 그림자 마스크를 떼고 ④ 알파 판은 프레임마다 한 번(변종 공통)
            var perVariant = variantTrns.Select(_ => new List<RgbaImage>()).ToList();
            for (var fi = 0; fi < a.Images.Count; fi++)
            {
                var src = a.Images[fi];
                var body = new IndexedImage(src.Width, src.Height);
                var shadowMask = new byte[src.Width * src.Height];
                var hasShadow = false;
                for (var i = 0; i < src.Index.Length; i++)
                {
                    if (!src.Opaque[i]) continue;
                    if (src.Index[i] == 0 && a.ShadowAlpha is not null) { shadowMask[i] = 255; hasShadow = true; continue; }
                    body.Index[i] = src.Index[i];
                    body.Opaque[i] = true;
                }
                var baseBody = RgbaImage.FromIndexed(body, pal, variantTrns[0], null);
                var alpha4 = upscaler.Up4($"{a.Name}/{fi:D4}/a", UpscaleMath.AlphaPlane(baseBody));
                var alpha2 = UpscaleMath.CropBox2(alpha4, src.Width, src.Height, UpscaleMath.Pad);
                var shadow2 = hasShadow ? UpscaleMath.SmoothShadow2x(shadowMask, src.Width, src.Height) : null;
                var sa = a.ShadowAlpha ?? 0f;
                for (var v = 0; v < variantTrns.Count; v++)
                {
                    var color = v == 0 ? baseBody : RgbaImage.FromIndexed(body, pal, variantTrns[v], null);
                    var col4 = upscaler.Up4($"{a.Name}/{fi:D4}/c{v}", UpscaleMath.ColorPlane(color));
                    var col2 = UpscaleMath.CropBox2(col4, src.Width, src.Height, UpscaleMath.Pad);
                    var outImg = new RgbaImage(col2.Width, col2.Height);
                    for (var i = 0; i < col2.Width * col2.Height; i++)
                    {
                        if (alpha2.Pixels[i * 4] >= 128)   // ⑤ 0.5 에서 잘라 있음·없음
                        {
                            Array.Copy(col2.Pixels, i * 4, outImg.Pixels, i * 4, 3);
                            outImg.Pixels[i * 4 + 3] = 255;
                        }
                        else if (shadow2 is not null && shadow2[i] > 0)   // ⑥ 그림자를 몸 아래에
                            outImg.Pixels[i * 4 + 3] = (byte)Math.Clamp((shadow2[i] * (int)MathF.Round(sa * 255f) + 127) / 255, 0, 255);
                    }
                    perVariant[v].Add(outImg);
                }
            }
            packs.Add(new PackAction(a.Name, new Int2(a.Cell.X * 2, a.Cell.Y * 2), a.Directions, a.Frames, [.. perVariant]));
        }
        return SheetPacker.Pack(packs, upscaler is null ? 1 : 2);
    }
}
