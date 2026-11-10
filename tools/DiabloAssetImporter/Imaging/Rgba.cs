using System.Buffers.Binary;
using System.IO.Compression;
using DiabloAssetImporter.Formats;

namespace DiabloAssetImporter.Imaging;

/// <summary>RGBA 8비트 그림(줄 0 = 위). 굽기·묶기·PNG 가 쓰는 하나의 형.</summary>
internal sealed class RgbaImage(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Pixels { get; } = new byte[width * height * 4];

    public byte A(int x, int y) => Pixels[(y * Width + x) * 4 + 3];

    public void Set(int x, int y, byte r, byte g, byte b, byte a)
    {
        var i = (y * Width + x) * 4;
        Pixels[i] = r; Pixels[i + 1] = g; Pixels[i + 2] = b; Pixels[i + 3] = a;
    }

    /// <summary>알파가 0 이 아닌 곳만 덮어 그린다(겹침 — 아이콘·조각 굽기).</summary>
    public void DrawOver(RgbaImage src, int dx, int dy)
    {
        for (var y = 0; y < src.Height; y++)
        {
            var ty = dy + y;
            if ((uint)ty >= (uint)Height) continue;
            for (var x = 0; x < src.Width; x++)
            {
                var tx = dx + x;
                if ((uint)tx >= (uint)Width) continue;
                var s = (y * src.Width + x) * 4;
                if (src.Pixels[s + 3] == 0) continue;
                Array.Copy(src.Pixels, s, Pixels, (ty * Width + tx) * 4, 4);
            }
        }
    }

    /// <summary>사각형을 그대로 복사(알파 포함).</summary>
    public void Blit(RgbaImage src, int sx, int sy, int w, int h, int dx, int dy)
    {
        for (var y = 0; y < h; y++)
            Array.Copy(src.Pixels, ((sy + y) * src.Width + sx) * 4, Pixels, ((dy + y) * Width + dx) * 4, w * 4);
    }

    public RgbaImage Crop(int x, int y, int w, int h)
    {
        var r = new RgbaImage(w, h);
        r.Blit(this, x, y, w, h, 0, 0);
        return r;
    }

    /// <summary>알파 &gt; 0 인 픽셀의 사각형(반투명 그림자도 남는다 — 1.4 "잘라 묶기"). 비면 null.</summary>
    public (int X, int Y, int W, int H)? AlphaBounds()
    {
        int x0 = Width, y0 = Height, x1 = -1, y1 = -1;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (Pixels[(y * Width + x) * 4 + 3] != 0)
                {
                    if (x < x0) x0 = x;
                    if (x > x1) x1 = x;
                    if (y < y0) y0 = y;
                    if (y > y1) y1 = y;
                }
        return x1 < 0 ? null : (x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    /// <summary>
    /// 색인 그림 → RGBA: 투명(RLE) → 알파 0, 색인 0 은 <paramref name="shadowAlpha"/> 가 있으면 검정 × 그 알파(그림자), 없으면 불투명 검정 그대로,
    /// 나머지 = TRN(있으면 먼저) → PAL.
    /// </summary>
    public static RgbaImage FromIndexed(IndexedImage src, Palette pal, byte[]? trn, float? shadowAlpha)
    {
        var img = new RgbaImage(src.Width, src.Height);
        var sa = shadowAlpha is { } a ? (byte)Math.Clamp((int)MathF.Round(a * 255f), 0, 255) : (byte)255;
        for (var i = 0; i < src.Index.Length; i++)
        {
            if (!src.Opaque[i]) continue;
            var c = src.Index[i];
            if (c == 0 && shadowAlpha is not null)
            {
                img.Pixels[i * 4 + 3] = sa;
                continue;
            }
            var (r, g, b) = pal[trn is null ? c : trn[c]];
            img.Pixels[i * 4] = r; img.Pixels[i * 4 + 1] = g; img.Pixels[i * 4 + 2] = b; img.Pixels[i * 4 + 3] = 255;
        }
        return img;
    }
}

/// <summary>
/// 결정적 PNG (1.4 결정론): RGBA 8비트, 줄 필터 0 고정, <c>ZLibStream</c> 압축 수준 고정, <c>tIME</c>·글 덩어리 없음.
/// 읽기는 확대 결과(외부 프로그램 PNG — 8비트 회색·RGB·RGBA, 필터 다섯, 인터레이스 없음)만 받는다.
/// </summary>
internal static class Png
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] Encode(RgbaImage img)
    {
        var raw = new byte[(img.Width * 4 + 1) * img.Height];
        for (var y = 0; y < img.Height; y++)
        {
            raw[y * (img.Width * 4 + 1)] = 0;
            Array.Copy(img.Pixels, y * img.Width * 4, raw, y * (img.Width * 4 + 1) + 1, img.Width * 4);
        }
        using var zs = new MemoryStream();
        using (var z = new ZLibStream(zs, CompressionLevel.Optimal, leaveOpen: true)) z.Write(raw);

        using var ms = new MemoryStream();
        ms.Write(Signature);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)img.Width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)img.Height);
        ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
        Chunk(ms, "IHDR", ihdr);
        Chunk(ms, "IDAT", zs.ToArray());
        Chunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void Chunk(Stream s, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        s.Write(len);
        var t = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(t);
        s.Write(data);
        var crc = Crc32(t, data);
        BinaryPrimitives.WriteUInt32BigEndian(len, crc);
        s.Write(len);
    }

    private static readonly uint[] CrcTable = BuildCrc();

    private static uint[] BuildCrc()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    private static uint Crc32(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var c = 0xFFFFFFFFu;
        foreach (var x in a) c = CrcTable[(c ^ x) & 0xFF] ^ (c >> 8);
        foreach (var x in b) c = CrcTable[(c ^ x) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    /// <summary>PNG 머리(IHDR)의 크기만.</summary>
    public static (int W, int H) ReadSize(ReadOnlySpan<byte> png)
    {
        if (png.Length < 24 || !png[..8].SequenceEqual(Signature)) throw new InvalidDataException("PNG 가 아니다");
        return ((int)BinaryPrimitives.ReadUInt32BigEndian(png[16..]), (int)BinaryPrimitives.ReadUInt32BigEndian(png[20..]));
    }

    public static RgbaImage Decode(ReadOnlySpan<byte> png)
    {
        if (png.Length < 8 || !png[..8].SequenceEqual(Signature)) throw new InvalidDataException("PNG 가 아니다");
        int w = 0, h = 0, colorType = 0, depth = 0, interlace = 0;
        using var idat = new MemoryStream();
        var at = 8;
        while (at + 8 <= png.Length)
        {
            var len = (int)BinaryPrimitives.ReadUInt32BigEndian(png[at..]);
            var type = System.Text.Encoding.ASCII.GetString(png.Slice(at + 4, 4));
            var data = png.Slice(at + 8, len);
            if (type == "IHDR")
            {
                w = (int)BinaryPrimitives.ReadUInt32BigEndian(data);
                h = (int)BinaryPrimitives.ReadUInt32BigEndian(data[4..]);
                depth = data[8]; colorType = data[9]; interlace = data[12];
            }
            else if (type == "IDAT") idat.Write(data);
            else if (type == "IEND") break;
            at += 12 + len;
        }
        var channels = colorType switch { 0 => 1, 2 => 3, 4 => 2, 6 => 4, _ => throw new InvalidDataException($"PNG 색 종류 {colorType} 는 읽지 않는다") };
        if (depth != 8 || interlace != 0) throw new InvalidDataException("PNG 는 8비트·인터레이스 없음만 읽는다");
        idat.Position = 0;
        using var z = new ZLibStream(idat, CompressionMode.Decompress);
        using var rawStream = new MemoryStream();
        z.CopyTo(rawStream);
        var raw = rawStream.ToArray();
        var stride = w * channels;
        var cur = new byte[stride];
        var prev = new byte[stride];
        var img = new RgbaImage(w, h);
        for (var y = 0; y < h; y++)
        {
            var off = y * (stride + 1);
            if (off + stride + 1 > raw.Length) throw new InvalidDataException("PNG 데이터가 짧다");
            var filter = raw[off];
            for (var i = 0; i < stride; i++)
            {
                int x = raw[off + 1 + i];
                int a = i >= channels ? cur[i - channels] : 0, b = prev[i], c = i >= channels ? prev[i - channels] : 0;
                x += filter switch
                {
                    0 => 0, 1 => a, 2 => b, 3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => throw new InvalidDataException($"PNG 필터 {filter}"),
                };
                cur[i] = (byte)x;
            }
            for (var x = 0; x < w; x++)
            {
                var s = x * channels;
                byte r, g, bb, al;
                switch (channels)
                {
                    case 1: r = g = bb = cur[s]; al = 255; break;
                    case 2: r = g = bb = cur[s]; al = cur[s + 1]; break;
                    case 3: r = cur[s]; g = cur[s + 1]; bb = cur[s + 2]; al = 255; break;
                    default: r = cur[s]; g = cur[s + 1]; bb = cur[s + 2]; al = cur[s + 3]; break;
                }
                img.Set(x, y, r, g, bb, al);
            }
            (prev, cur) = (cur, prev);
        }
        return img;
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}
