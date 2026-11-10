using DiabloAssetImporter.Imaging;

namespace DiabloAssetImporter.Bake;

/// <summary>동작 하나의 프레임들(방향 순 → 프레임 순, 칸 크기 그림). 변종마다 같은 자리에 색만 다른 그림 목록.</summary>
internal sealed record PackAction(string Name, Int2 Cell, int Directions, int Frames, IReadOnlyList<IReadOnlyList<RgbaImage>> Variants);

/// <summary>묶은 결과: 변종마다 페이지 그림, 메타 하나(변종 공통).</summary>
internal sealed record PackedSheet(SpriteMeta Meta, IReadOnlyList<IReadOnlyList<RgbaImage>> VariantPages);

/// <summary>
/// 잘라 묶기 (1.4 변환 규칙): 프레임마다 알파 &gt; 0 사각형으로 잘라 (동작 이름 서수, 방향, 프레임) 순으로 줄 쌓기. 페이지 ≤ 4096,
/// 넘치면 동작 단위로 다음 페이지. 사각형은 변종 0(원색)의 알파로 정한다 — TRN 은 투명을 바꾸지 않으므로 변종끼리 같다.
/// </summary>
internal static class SheetPacker
{
    public const int MaxPage = 4096;
    private const int Gap = 1;

    /// <summary>
    /// 둘째 페이지부터의 파일 이름인지(<c>zombie_p2.png</c>·<c>zombie_p2@bluered.png</c> — <see cref="SpriteMeta.PageFile"/>).
    /// 이름에 "_p" 가 든 것으로 보면 <c>breast_plate.png</c> 같은 첫 페이지를 잘못 센다(v0.9 인벤토리 그림 이름).
    /// </summary>
    public static bool IsExtraPage(string path) =>
        System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(path), @"_p[0-9]+(@[a-z0-9_]+)?\.png$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly int[] Widths = [64, 128, 256, 512, 1024, 2048, 4096];

    public static PackedSheet Pack(IReadOnlyList<PackAction> actionsIn, int scale = 1, int maxPage = MaxPage)
    {
        var actions = actionsIn.OrderBy(a => a.Name, StringComparer.Ordinal).ToList();
        // 사각형 크기
        var bounds = actions.Select(a => a.Variants[0].Select(f => f.AlphaBounds()).ToArray()).ToList();
        var maxW = bounds.SelectMany(b => b).Max(b => b?.W ?? 0);
        if (maxW + Gap > maxPage) throw new InvalidDataException($"프레임 폭 {maxW} 가 페이지 {maxPage} 보다 넓다");

        var width = maxPage;
        foreach (var w in Widths)
        {
            if (w > maxPage || w < maxW) continue;
            var (pages, h) = Layout(bounds, w, int.MaxValue);
            if (pages == 1 && h <= w) { width = w; break; }
        }
        var placement = new List<(int Page, int X, int Y)[]>();
        var pageHeights = new List<int> { 0 };
        {
            int page = 0, x = 0, y = 0, shelf = 0;
            for (var ai = 0; ai < actions.Count; ai++)
            {
                // 동작 단위로 다음 페이지: 이 동작이 남은 자리에 안 들어가고 새 페이지면 들어가면 넘긴다
                var need = SimHeight(bounds[ai], width, x, y, shelf);
                if (need > maxPage && (x > 0 || y > 0))
                {
                    var fresh = SimHeight(bounds[ai], width, 0, 0, 0);
                    if (fresh <= maxPage) { pageHeights[page] = Math.Max(pageHeights[page], y + shelf); page++; pageHeights.Add(0); x = 0; y = 0; shelf = 0; }
                }
                var arr = new (int, int, int)[bounds[ai].Length];
                for (var fi = 0; fi < bounds[ai].Length; fi++)
                {
                    if (bounds[ai][fi] is not { } b) { arr[fi] = (-1, 0, 0); continue; }
                    if (x + b.W > width) { y += shelf + Gap; x = 0; shelf = 0; }
                    if (y + b.H > maxPage) { pageHeights[page] = Math.Max(pageHeights[page], y); page++; pageHeights.Add(0); x = 0; y = 0; shelf = 0; }
                    arr[fi] = (page, x, y);
                    x += b.W + Gap;
                    shelf = Math.Max(shelf, b.H);
                    pageHeights[page] = Math.Max(pageHeights[page], y + shelf);
                }
                placement.Add(arr);
            }
        }

        var pageSizes = pageHeights.Select(h => new Int2(width, Math.Max(1, h))).ToList();
        var metaActions = new Dictionary<string, SpriteMetaAction>(StringComparer.Ordinal);
        for (var ai = 0; ai < actions.Count; ai++)
        {
            var a = actions[ai];
            var rects = new SpriteRect[bounds[ai].Length];
            for (var fi = 0; fi < rects.Length; fi++)
            {
                var (p, x, y) = placement[ai][fi];
                rects[fi] = bounds[ai][fi] is { } b && p >= 0 ? new SpriteRect(p, x, y, b.W, b.H, b.X, b.Y) : default;
            }
            metaActions[a.Name] = new SpriteMetaAction { Cell = a.Cell, Directions = a.Directions, Frames = a.Frames, Rects = rects };
        }
        var meta = new SpriteMeta { Format = SpriteMeta.CurrentFormat, Scale = scale, Pages = pageSizes, Actions = metaActions };

        var variantCount = actions[0].Variants.Count;
        var variantPages = new List<IReadOnlyList<RgbaImage>>(variantCount);
        for (var v = 0; v < variantCount; v++)
        {
            var pages = pageSizes.Select(s => new RgbaImage(s.X, s.Y)).ToList();
            for (var ai = 0; ai < actions.Count; ai++)
                for (var fi = 0; fi < bounds[ai].Length; fi++)
                {
                    if (bounds[ai][fi] is not { } b) continue;
                    var (p, x, y) = placement[ai][fi];
                    pages[p].Blit(actions[ai].Variants[v][fi], b.X, b.Y, b.W, b.H, x, y);
                }
            variantPages.Add(pages);
        }
        return new PackedSheet(meta, variantPages);
    }

    private static (int Pages, int Height) Layout(List<(int X, int Y, int W, int H)?[]> bounds, int width, int maxH)
    {
        int x = 0, y = 0, shelf = 0;
        foreach (var arr in bounds)
            foreach (var b0 in arr)
            {
                if (b0 is not { } b) continue;
                if (x + b.W > width) { y += shelf + Gap; x = 0; shelf = 0; }
                x += b.W + Gap;
                shelf = Math.Max(shelf, b.H);
            }
        var h = y + shelf;
        return (h <= maxH ? 1 : 2, h);
    }

    private static int SimHeight((int X, int Y, int W, int H)?[] arr, int width, int x, int y, int shelf)
    {
        foreach (var b0 in arr)
        {
            if (b0 is not { } b) continue;
            if (x + b.W > width) { y += shelf + Gap; x = 0; shelf = 0; }
            x += b.W + Gap;
            shelf = Math.Max(shelf, b.H);
        }
        return y + shelf;
    }
}
