using System.Buffers.Binary;
using DiabloAssetImporter.Formats;

namespace DiabloAssetImporter.Tests.TestKit;

/// <summary>
/// 시험 인코더(설계서 10.1 — 바이트 픽스처는 시험 코드가 만든다, 원본에서 잘라 낸 바이트를 넣지 않는다 R24). 부록 A 의 형식을 거꾸로 쓴다.
/// </summary>
internal static class Encoders
{
    /// <summary>색인 그림(줄 0 = 위) → CL2 프레임(10바이트 머리 + RLE). 아래 줄부터, 줄이 넘어가는 런을 그대로 둔다.</summary>
    public static byte[] Cl2Frame(IndexedImage img, bool header = true)
    {
        // 아래 줄부터 픽셀 순서
        var px = new List<(bool Opaque, byte C)>();
        for (var y = img.Height - 1; y >= 0; y--)
            for (var x = 0; x < img.Width; x++)
                px.Add((img.Opaque[y * img.Width + x], img.Index[y * img.Width + x]));
        var data = new List<byte>();
        var skips = new List<int>();
        var i = 0;
        int rowsDone() => i / img.Width;
        while (i < px.Count)
        {
            if (header && rowsDone() >= 32 * (skips.Count + 1) && i % img.Width == 0 && skips.Count < 4) skips.Add(data.Count + 10);
            // 런은 32줄 경계를 넘지 않게(건너뛰기 표가 그 경계에서 끊긴다)
            var limit = header ? (32 * (skips.Count + 1)) * img.Width - i : int.MaxValue;
            if (!px[i].Opaque)
            {
                var n = 0;
                while (i + n < px.Count && !px[i + n].Opaque && n < 127 && n < limit) n++;
                data.Add((byte)n);
                i += n;
                continue;
            }
            // 같은 색 반복이 3 이상이면 반복 런, 아니면 그대로 런
            var rep = 1;
            while (i + rep < px.Count && px[i + rep].Opaque && px[i + rep].C == px[i].C && rep < 63 && rep < limit) rep++;
            if (rep >= 3)
            {
                data.Add((byte)(sbyte)(-(rep + 65)));
                data.Add(px[i].C);
                i += rep;
                continue;
            }
            var lit = 0;
            while (i + lit < px.Count && px[i + lit].Opaque && lit < 65 && lit < limit) lit++;
            data.Add((byte)(sbyte)(-lit));
            for (var k = 0; k < lit; k++) data.Add(px[i + k].C);
            i += lit;
        }
        if (!header) return [.. data];
        while (skips.Count < 4) skips.Add(0);
        var head = new byte[10];
        BinaryPrimitives.WriteUInt16LittleEndian(head, 0x0A);
        for (var k = 0; k < 4; k++) BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(2 + k * 2), (ushort)skips[k]);
        return [.. head, .. data];
    }

    /// <summary>평면 묶음: <c>u32 n, u32 오프셋[n + 1]</c> + 프레임들.</summary>
    public static byte[] Flat(IReadOnlyList<byte[]> frames)
    {
        var headLen = 4 + (frames.Count + 1) * 4;
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        bw.Write(frames.Count);
        var off = headLen;
        foreach (var f in frames) { bw.Write(off); off += f.Length; }
        bw.Write(off);
        foreach (var f in frames) bw.Write(f);
        return ms.ToArray();
    }

    /// <summary>8그룹 묶음: 그룹 오프셋 8개 + 그룹마다 평면 머리(그룹 머리 기준 오프셋).</summary>
    public static byte[] Grouped(IReadOnlyList<IReadOnlyList<byte[]>> groups)
    {
        var blocks = groups.Select(g => Flat(g)).ToList();
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        var off = groups.Count * 4;
        foreach (var b in blocks) { bw.Write(off); off += b.Length; }
        foreach (var b in blocks) bw.Write(b);
        return ms.ToArray();
    }

    /// <summary>시험 그림: 폭 × 높이, 가운데 몸(색 <paramref name="c"/>), 아래 두 줄 그림자(색인 0), 나머지 투명. 프레임마다 조금씩 다르게.</summary>
    public static IndexedImage Figure(int w, int h, byte c, int seed)
    {
        var img = new IndexedImage(w, h);
        int bx0 = w / 4 + seed % 3, bx1 = w * 3 / 4 - seed % 2, by0 = h / 4, by1 = h - 3;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (x >= bx0 && x < bx1 && y >= by0 && y < by1) { img.Opaque[i] = true; img.Index[i] = (byte)(c + (x + y + seed) % 4); }
                else if (y >= h - 3 && y < h - 1 && x >= bx0 - 4 && x < bx1 + 4) { img.Opaque[i] = true; img.Index[i] = 0; }
            }
        return img;
    }

    /// <summary>레벨 CEL 조각 인코더(부록 A.3 표). 그림은 32×32(줄 0 = 위), 삼각형은 아래 31줄 모양만 쓴다.</summary>
    public static byte[] LevelPiece(IndexedImage img, int kind)
    {
        var o = new List<byte>();
        byte At(int x, int rowFromBottom) => img.Index[(31 - rowFromBottom) * 32 + x];
        switch (kind)
        {
            case 0:
                for (var r = 0; r < 32; r++) for (var x = 0; x < 32; x++) o.Add(At(x, r));
                break;
            case 1:
                for (var r = 0; r < 32; r++)
                {
                    var x = 0;
                    while (x < 32)
                    {
                        var op = img.Opaque[(31 - r) * 32 + x];
                        var n = 0;
                        while (x + n < 32 && img.Opaque[(31 - r) * 32 + x + n] == op) n++;
                        if (op) { o.Add((byte)n); for (var k = 0; k < n; k++) o.Add(At(x + k, r)); }
                        else o.Add((byte)(sbyte)(-n));
                        x += n;
                    }
                }
                break;
            default:
            {
                var left = kind is 2 or 4;
                var rows = kind is 2 or 3 ? 31 : 16;
                for (var r = 0; r < rows; r++)
                {
                    var w = LevelCel.TriangleWidth(r);
                    var pad = r % 2 == 0;
                    if (pad && left) { o.Add(0); o.Add(0); }
                    var x0 = left ? 32 - w : 0;
                    for (var k = 0; k < w; k++) o.Add(At(x0 + k, r));
                    if (pad && !left) { o.Add(0); o.Add(0); }
                }
                if (kind is 4 or 5) for (var r = 16; r < 32; r++) for (var x = 0; x < 32; x++) o.Add(At(x, r));
                break;
            }
        }
        return [.. o];
    }

    public static byte[] U16s(IEnumerable<int> values)
    {
        var v = values.ToArray();
        var b = new byte[v.Length * 2];
        for (var i = 0; i < v.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(i * 2), (ushort)v[i]);
        return b;
    }

    /// <summary>PAL: 색인 i → (i, 255 − i, i/2).</summary>
    public static byte[] Pal()
    {
        var p = new byte[768];
        for (var i = 0; i < 256; i++) { p[i * 3] = (byte)i; p[i * 3 + 1] = (byte)(255 - i); p[i * 3 + 2] = (byte)(i / 2); }
        return p;
    }

    /// <summary>TRN: 색인 i → (i + shift) mod 256(0 은 그대로).</summary>
    public static byte[] Trn(int shift)
    {
        var t = new byte[256];
        for (var i = 0; i < 256; i++) t[i] = i == 0 ? (byte)0 : (byte)((i + shift) % 256);
        return t;
    }

    /// <summary>RIFF PCM 8비트 모노(덧붙은 덩어리 하나 포함).</summary>
    public static byte[] Wav8(byte[] samples, int rate = 22050)
    {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        bw.Write("RIFF"u8); bw.Write(0); bw.Write("WAVE"u8);
        bw.Write("fmt "u8); bw.Write(16); bw.Write((short)1); bw.Write((short)1); bw.Write(rate); bw.Write(rate); bw.Write((short)1); bw.Write((short)8);
        bw.Write("LIST"u8); bw.Write(4); bw.Write("INFO"u8);
        bw.Write("data"u8); bw.Write(samples.Length); bw.Write(samples);
        if (samples.Length % 2 == 1) bw.Write((byte)0);
        var b = ms.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4), b.Length - 8);
        return b;
    }
}
