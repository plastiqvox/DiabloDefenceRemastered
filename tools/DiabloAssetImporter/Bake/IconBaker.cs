using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Imaging;

namespace DiabloAssetImporter.Bake;

/// <summary>
/// 아이콘 (1.4 변환 규칙 "아이콘", 기획 5.9): 표 <c>layers</c> 의 (방향, 프레임)을 차례로 겹쳐(<c>rotate</c> = 가장 가까운 픽셀 회전, <c>offset</c> = 정수 이동)
/// size×size 어두운 칸 가운데에 맞춰 줄인다(정수 상자 평균 — 결정적). 결과는 불투명.
/// </summary>
internal static class IconBaker
{
    public static RgbaImage Bake(ImportIconEntry e, UnitBaker.DecodedAction frames, Palette pal, int size, Rgb background)
    {
        var cell = frames.Cell;
        // 회전·이동이 잘리지 않게 넉넉한 판
        var margin = Math.Max(cell.X, cell.Y);
        var canvas = new RgbaImage(cell.X + margin * 2, cell.Y + margin * 2);
        foreach (var l in e.Layers)
        {
            var dirIndex = e.FramesAreDirs is not null ? 0 : l.Dir;
            var frameIndex = e.FramesAreDirs is not null ? l.Dir : l.Frame - 1;
            var perDir = frames.Frames;
            var idx = e.FramesAreDirs is not null ? frameIndex : dirIndex * perDir + frameIndex;
            if (e.FramesAreDirs is null && (frameIndex < 0 || frameIndex >= perDir))
                throw new ImportCheckException($"{e.Out}: 프레임 {l.Frame} 이 1..{perDir} 밖이다");
            if (idx < 0 || idx >= frames.Images.Count) throw new ImportCheckException($"{e.Out}: 방향 {l.Dir} 이 없다");
            var img = RgbaImage.FromIndexed(frames.Images[idx], pal, null, e.ShadowAlpha);
            if (l.Rotate is { } deg && deg != 0f) img = Rotate(img, deg);
            var off = l.Offset ?? default;
            canvas.DrawOver(img, margin + off.X - (img.Width - cell.X) / 2, margin + off.Y - (img.Height - cell.Y) / 2);
        }
        var result = new RgbaImage(size, size);
        for (var i = 0; i < size * size; i++)
        {
            result.Pixels[i * 4] = background.R; result.Pixels[i * 4 + 1] = background.G; result.Pixels[i * 4 + 2] = background.B; result.Pixels[i * 4 + 3] = 255;
        }
        if (canvas.AlphaBounds() is not { } b) return result;
        var k = Math.Max(1, (Math.Max(b.W, b.H) + size - 1) / size);
        int ow = (b.W + k - 1) / k, oh = (b.H + k - 1) / k;
        int ox = (size - ow) / 2, oy = (size - oh) / 2;
        for (var y = 0; y < oh; y++)
            for (var x = 0; x < ow; x++)
            {
                long r = 0, g = 0, bl = 0, a = 0;
                var n = k * k;
                for (var dy = 0; dy < k; dy++)
                    for (var dx = 0; dx < k; dx++)
                    {
                        int sx = b.X + x * k + dx, sy = b.Y + y * k + dy;
                        if (sx >= b.X + b.W || sy >= b.Y + b.H) continue;
                        var i = (sy * canvas.Width + sx) * 4;
                        int pa = canvas.Pixels[i + 3];
                        r += canvas.Pixels[i] * pa; g += canvas.Pixels[i + 1] * pa; bl += canvas.Pixels[i + 2] * pa; a += pa;
                    }
                var t = ((oy + y) * size + ox + x) * 4;
                // 미리 곱한 색 평균을 바탕 위에 덮는다(정수)
                var aa = a / n;
                if (aa == 0) continue;
                long Mix(long sum, byte bg) => (sum / n + bg * (255 - aa)) / 255;
                result.Pixels[t] = (byte)Math.Clamp(Mix(r / 255, background.R), 0, 255);
                result.Pixels[t + 1] = (byte)Math.Clamp(Mix(g / 255, background.G), 0, 255);
                result.Pixels[t + 2] = (byte)Math.Clamp(Mix(bl / 255, background.B), 0, 255);
            }
        return result;
    }

    /// <summary>가장 가까운 픽셀 회전(가운데 기준, 크기 그대로). 각도는 시계 방향 도.</summary>
    public static RgbaImage Rotate(RgbaImage src, float degrees)
    {
        var r = new RgbaImage(src.Width, src.Height);
        var rad = degrees * Math.PI / 180.0;
        double c = Math.Cos(rad), s = Math.Sin(rad);
        double cx = (src.Width - 1) / 2.0, cy = (src.Height - 1) / 2.0;
        for (var y = 0; y < src.Height; y++)
            for (var x = 0; x < src.Width; x++)
            {
                double dx = x - cx, dy = y - cy;
                var sx = (int)Math.Round(cx + dx * c + dy * s);
                var sy = (int)Math.Round(cy - dx * s + dy * c);
                if ((uint)sx >= (uint)src.Width || (uint)sy >= (uint)src.Height) continue;
                Array.Copy(src.Pixels, (sy * src.Width + sx) * 4, r.Pixels, (y * src.Width + x) * 4, 4);
            }
        return r;
    }
}
