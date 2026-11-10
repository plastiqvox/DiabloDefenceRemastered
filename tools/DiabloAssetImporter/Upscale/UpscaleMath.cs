using DiabloAssetImporter.Imaging;

namespace DiabloAssetImporter.Upscale;

/// <summary>
/// AI 확대 처리 ①~⑥ 의 결정적인 부분(1.4 "AI 확대 — 처리 순서"). 색 바이트만 외부 프로그램에 따라 다르고, 여기 함수는 모두 정수 연산이다.
/// </summary>
internal static class UpscaleMath
{
    /// <summary>③ 의 원본 여백(px). ② 의 채우기는 이것 이상 번진다.</summary>
    public const int Pad = 16;
    /// <summary>모델 배율(실행 파일 <c>-s 4</c>). 첫 판은 4 고정 — 표 <c>scale</c> 도 4 만 쓴다.</summary>
    public const int ModelScale = 4;

    /// <summary>
    /// ② 투명 칸 색 채우기: 알파 0 칸의 색을 가장 가까운(4방향 너비 우선) 불투명 칸 색으로 채운 불투명 판을 만든다.
    /// <paramref name="radius"/> 밖은 검정. 결과 알파는 모두 255(색 판).
    /// </summary>
    public static RgbaImage FillTransparent(RgbaImage src, int radius)
    {
        int w = src.Width, h = src.Height;
        var outImg = new RgbaImage(w, h);
        var dist = new int[w * h];
        Array.Fill(dist, int.MaxValue);
        var q = new Queue<int>();
        for (var i = 0; i < w * h; i++)
            if (src.Pixels[i * 4 + 3] != 0)
            {
                dist[i] = 0;
                Array.Copy(src.Pixels, i * 4, outImg.Pixels, i * 4, 3);
                q.Enqueue(i);
            }
        while (q.Count > 0)
        {
            var i = q.Dequeue();
            if (dist[i] >= radius) continue;
            int x = i % w, y = i / w;
            Span<int> ns = [x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1];
            foreach (var n in ns)
            {
                if (n < 0 || dist[n] != int.MaxValue) continue;
                dist[n] = dist[i] + 1;
                Array.Copy(outImg.Pixels, i * 4, outImg.Pixels, n * 4, 3);
                q.Enqueue(n);
            }
        }
        for (var i = 0; i < w * h; i++) outImg.Pixels[i * 4 + 3] = 255;
        return outImg;
    }

    /// <summary>③ 여백: 그림을 사방 <paramref name="pad"/>px 넓힌 투명 판에 놓는다(색 채우기 전).</summary>
    public static RgbaImage Expand(RgbaImage src, int pad)
    {
        var r = new RgbaImage(src.Width + pad * 2, src.Height + pad * 2);
        r.Blit(src, 0, 0, src.Width, src.Height, pad, pad);
        return r;
    }

    /// <summary>색 판: 여백을 두고 ② 로 채운 불투명 판. 반경을 두지 않는다 — 불투명 칸에서 먼 여백 귀퉁이(4방향 거리 32 넘음)에도 검정이 남지 않게.</summary>
    public static RgbaImage ColorPlane(RgbaImage src) => FillTransparent(Expand(src, Pad), int.MaxValue);

    /// <summary>알파 판(몬스터): 몸 알파를 회색으로(여백 0).</summary>
    public static RgbaImage AlphaPlane(RgbaImage src)
    {
        var e = Expand(src, Pad);
        var r = new RgbaImage(e.Width, e.Height);
        for (var i = 0; i < e.Width * e.Height; i++)
        {
            var a = e.Pixels[i * 4 + 3];
            r.Pixels[i * 4] = a; r.Pixels[i * 4 + 1] = a; r.Pixels[i * 4 + 2] = a; r.Pixels[i * 4 + 3] = 255;
        }
        return r;
    }

    /// <summary>4배 결과에서 여백을 떼고(가운데) ⑤ 2×2 상자 평균으로 2배 크기로.</summary>
    public static RgbaImage CropBox2(RgbaImage up4, int innerW, int innerH, int pad)
    {
        var s = ModelScale;
        var crop = up4.Crop(pad * s, pad * s, innerW * s, innerH * s);
        return Box2(crop);
    }

    /// <summary>⑤ 2×2 상자 평균(정수, 반올림 = +2 &gt;&gt; 2).</summary>
    public static RgbaImage Box2(RgbaImage src)
    {
        var r = new RgbaImage(src.Width / 2, src.Height / 2);
        for (var y = 0; y < r.Height; y++)
            for (var x = 0; x < r.Width; x++)
                for (var c = 0; c < 4; c++)
                {
                    var sum = src.Pixels[((2 * y) * src.Width + 2 * x) * 4 + c] + src.Pixels[((2 * y) * src.Width + 2 * x + 1) * 4 + c]
                            + src.Pixels[((2 * y + 1) * src.Width + 2 * x) * 4 + c] + src.Pixels[((2 * y + 1) * src.Width + 2 * x + 1) * 4 + c];
                    r.Pixels[(y * r.Width + x) * 4 + c] = (byte)((sum + 2) >> 2);
                }
        return r;
    }

    /// <summary>최근접 k배(맵 알파 점 확대·시험용 가짜).</summary>
    public static RgbaImage Nearest(RgbaImage src, int k)
    {
        var r = new RgbaImage(src.Width * k, src.Height * k);
        for (var y = 0; y < r.Height; y++)
            for (var x = 0; x < r.Width; x++)
                Array.Copy(src.Pixels, ((y / k) * src.Width + x / k) * 4, r.Pixels, (y * r.Width + x) * 4, 4);
        return r;
    }

    /// <summary>맵 그림: 색(확대 2배) + 원본 알파 2배 점 확대(④ — 경계 모양이 원본 2배 그대로).</summary>
    public static RgbaImage WithNearestAlpha(RgbaImage color2x, RgbaImage original)
    {
        var r = new RgbaImage(color2x.Width, color2x.Height);
        Array.Copy(color2x.Pixels, r.Pixels, r.Pixels.Length);
        for (var y = 0; y < r.Height; y++)
            for (var x = 0; x < r.Width; x++)
            {
                var a = original.Pixels[((y / 2) * original.Width + x / 2) * 4 + 3];
                var i = (y * r.Width + x) * 4;
                r.Pixels[i + 3] = a;
                if (a == 0) { r.Pixels[i] = 0; r.Pixels[i + 1] = 0; r.Pixels[i + 2] = 0; }
            }
        return r;
    }

    /// <summary>
    /// ⑥ 그림자 마스크(0/255)를 매끈하게 2배: 4배 쌍삼차(Catmull-Rom, 정수 가중치 /1024) → 2×2 상자 평균 → 1px 흐림(3×3 [1 2 1]², /16).
    /// </summary>
    public static byte[] SmoothShadow2x(byte[] mask, int w, int h)
    {
        const int s = 4;
        int W4 = w * s, H4 = h * s;
        // 축마다 분리 가능한 쌍삼차: 출력 i 의 원본 위치 = (i + 0.5)/4 − 0.5
        var horiz = new int[W4 * h];
        var wt = Weights();
        for (var y = 0; y < h; y++)
            for (var x = 0; x < W4; x++)
            {
                var (b, ws) = wt[x % s];
                var sx = x / s + b;
                var acc = 0;
                for (var k = 0; k < 4; k++) acc += ws[k] * mask[y * w + Math.Clamp(sx - 1 + k, 0, w - 1)];
                horiz[y * W4 + x] = acc;   // ×1024
            }
        var up = new int[W4 * H4];
        for (var y = 0; y < H4; y++)
        {
            var (b, ws) = wt[y % s];
            var sy = y / s + b;
            for (var x = 0; x < W4; x++)
            {
                long acc = 0;
                for (var k = 0; k < 4; k++) acc += (long)ws[k] * horiz[Math.Clamp(sy - 1 + k, 0, h - 1) * W4 + x];
                up[y * W4 + x] = (int)Math.Clamp((acc + (1 << 19)) >> 20, 0, 255);
            }
        }
        int W2 = w * 2, H2 = h * 2;
        var box = new int[W2 * H2];
        for (var y = 0; y < H2; y++)
            for (var x = 0; x < W2; x++)
                box[y * W2 + x] = (up[(2 * y) * W4 + 2 * x] + up[(2 * y) * W4 + 2 * x + 1] + up[(2 * y + 1) * W4 + 2 * x] + up[(2 * y + 1) * W4 + 2 * x + 1] + 2) >> 2;
        var outp = new byte[W2 * H2];
        for (var y = 0; y < H2; y++)
            for (var x = 0; x < W2; x++)
            {
                var acc = 0;
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var wgt = (dx == 0 ? 2 : 1) * (dy == 0 ? 2 : 1);
                        acc += wgt * box[Math.Clamp(y + dy, 0, H2 - 1) * W2 + Math.Clamp(x + dx, 0, W2 - 1)];
                    }
                outp[y * W2 + x] = (byte)((acc + 8) >> 4);
            }
        return outp;
    }

    private static (int Base, int[] W)[]? _weights;

    private static (int Base, int[] W)[] Weights()
    {
        if (_weights is not null) return _weights;
        var r = new (int, int[])[4];
        for (var p = 0; p < 4; p++)
        {
            // 출력 위치 4i+p 의 원본 좌표 = i + (p + 0.5)/4 − 0.5 = i + f(p) ; f 는 −0.375, −0.125, 0.125, 0.375
            var f = (p + 0.5) / 4 - 0.5;
            var b = f < 0 ? -1 : 0;
            var t = f - b;
            double[] w =
            [
                ((-0.5 * t + 1.0) * t - 0.5) * t,
                (1.5 * t - 2.5) * t * t + 1.0,
                ((-1.5 * t + 2.0) * t + 0.5) * t,
                (0.5 * t - 0.5) * t * t,
            ];
            var wi = w.Select(v => (int)Math.Round(v * 1024)).ToArray();
            wi[1] += 1024 - wi.Sum();
            r[p] = (b, wi);
        }
        return _weights = r;
    }
}
