using System.Buffers.Binary;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace DiabloDefenceRemastered.Loading;

/// <summary>
/// 커서 (설계서 7.8, 소유 WP5): 원작 exe 의 <c>DIInop.cur</c> 를 읽는다 — ICO/CUR 머리글 → 첫 항목, PNG 면 그대로, BMP 면 1·4·8 비트 팔레트·24·32 비트 + AND 마스크를 풀어
/// <c>MouseCursor.FromTexture2D(tex, hotX, hotY)</c>. 실패하면 null(기본 화살표) — 커서 하나 때문에 게임이 서지 않게.
/// </summary>
public static class CursorLoader
{
    public const string FileName = "DIInop.cur";

    public static MouseCursor? Load(GraphicsDevice device, string path, IAppLog? log = null)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var bytes = File.ReadAllBytes(path);
            if (!TryDecode(bytes, out var w, out var h, out var rgba, out var hot)) { log?.Warn($"CURSOR {path}: 읽을 수 없는 형식 — 기본 커서"); return null; }
            var tex = new Texture2D(device, w, h);
            tex.SetData(rgba);
            return MouseCursor.FromTexture2D(tex, hot.X, hot.Y);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            log?.Warn($"CURSOR {path}: {e.Message} — 기본 커서");
            return null;
        }
    }

    /// <summary>ICO/CUR 바이트 → 첫 그림의 RGBA(premultiplied)와 핫스팟. PNG 항목은 지원하지 않는다(해독기 없이 — 원작 커서는 BMP).</summary>
    public static bool TryDecode(ReadOnlySpan<byte> b, out int w, out int h, out Color[] pixels, out Int2 hotspot)
    {
        w = h = 0;
        pixels = [];
        hotspot = Int2.Zero;
        if (b.Length < 22 || BinaryPrimitives.ReadUInt16LittleEndian(b) != 0) return false;
        var type = BinaryPrimitives.ReadUInt16LittleEndian(b[2..]);
        if (type is not (1 or 2) || BinaryPrimitives.ReadUInt16LittleEndian(b[4..]) == 0) return false;
        var entry = b[6..22];
        if (type == 2) hotspot = new Int2(BinaryPrimitives.ReadUInt16LittleEndian(entry[4..]), BinaryPrimitives.ReadUInt16LittleEndian(entry[6..]));
        var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
        var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
        if (offset < 0 || size <= 40 || offset + size > b.Length) return false;
        var img = b.Slice(offset, size);
        if (img[0] == 0x89) return false;   // PNG 항목

        var headerSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(img);
        w = BinaryPrimitives.ReadInt32LittleEndian(img[4..]);
        h = BinaryPrimitives.ReadInt32LittleEndian(img[8..]) / 2;   // XOR + AND 두 장 높이
        var bits = BinaryPrimitives.ReadUInt16LittleEndian(img[14..]);
        var used = (int)BinaryPrimitives.ReadUInt32LittleEndian(img[32..]);
        if (w <= 0 || h <= 0 || w > 256 || h > 256) return false;
        var paletteCount = bits <= 8 ? (used > 0 ? used : 1 << bits) : 0;
        var palette = img[headerSize..];
        var xor = palette[(paletteCount * 4)..];
        var xorStride = (w * bits + 31) / 32 * 4;
        var and = xor[(xorStride * h)..];
        var andStride = (w + 31) / 32 * 4;
        if (and.Length < andStride * h) return false;

        pixels = new Color[w * h];
        var anyAlpha = false;
        for (var y = 0; y < h; y++)
        {
            var row = xor.Slice((h - 1 - y) * xorStride, xorStride);
            for (var x = 0; x < w; x++)
            {
                byte r, g, bl, a = 255;
                switch (bits)
                {
                    case 32: bl = row[x * 4]; g = row[x * 4 + 1]; r = row[x * 4 + 2]; a = row[x * 4 + 3]; if (a != 0) anyAlpha = true; break;
                    case 24: bl = row[x * 3]; g = row[x * 3 + 1]; r = row[x * 3 + 2]; break;
                    case 8 or 4 or 1:
                    {
                        var bitPos = x * bits;
                        var idx = (row[bitPos >> 3] >> (8 - bits - (bitPos & 7))) & ((1 << bits) - 1);
                        var p = palette.Slice(idx * 4, 4);
                        bl = p[0]; g = p[1]; r = p[2];
                        break;
                    }
                    default: return false;
                }
                pixels[y * w + x] = new Color(r, g, bl, a);
            }
        }
        // AND 마스크(1 = 투명). 32비트에 알파가 있으면 알파를 그대로 쓴다
        for (var y = 0; y < h; y++)
        {
            var row = and.Slice((h - 1 - y) * andStride, andStride);
            for (var x = 0; x < w; x++)
            {
                var transparent = ((row[x >> 3] >> (7 - (x & 7))) & 1) != 0;
                ref var c = ref pixels[y * w + x];
                if (!anyAlpha && transparent) c = Color.Transparent;
                else if (anyAlpha) c = Color.FromNonPremultiplied(c.R, c.G, c.B, c.A);
            }
        }
        return true;
    }
}
