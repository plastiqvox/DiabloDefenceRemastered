using System.Buffers.Binary;
using System.IO.Compression;

namespace FakeUpscaler;

/// <summary>
/// 가짜 확대 프로그램. DDR_FAKE_MODE: (없음) 정상, fail = 종료 코드 1, fail-unless-t128 = -t 128 이 없으면 1, sleep = 결과 없이 오래 잠, drop = 결과 하나 빼기,
/// size = 결과 크기 틀림(3배), dllnotfound = 0xC0000135. DDR_FAKE_LOG 가 있으면 인자를 한 줄로 덧붙여 쓴다.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var mode = Environment.GetEnvironmentVariable("DDR_FAKE_MODE") ?? "";
        if (Environment.GetEnvironmentVariable("DDR_FAKE_LOG") is { Length: > 0 } log)
            File.AppendAllText(log, string.Join("\u001f", args) + "\n");
        Console.Error.WriteLine("[0 Fake Vulkan GPU]  queueC=0[1]");
        if (mode == "dllnotfound") return unchecked((int)0xC0000135);
        if (mode == "fail") return 1;
        if (mode == "fail-unless-t128" && !args.Contains("128")) return 1;
        if (mode == "sleep") { Thread.Sleep(TimeSpan.FromMinutes(5)); return 0; }
        string input = Arg(args, "-i"), output = Arg(args, "-o");
        var scale = mode == "size" ? 3 : 4;
        var files = Directory.GetFiles(input, "*.png").Order(StringComparer.Ordinal).ToList();
        if (mode == "drop" && files.Count > 0) files.RemoveAt(files.Count - 1);
        foreach (var f in files)
        {
            var (w, h, px) = Decode(File.ReadAllBytes(f));
            var o = new byte[w * scale * h * scale * 4];
            for (var y = 0; y < h * scale; y++)
                for (var x = 0; x < w * scale; x++)
                    Array.Copy(px, ((y / scale) * w + x / scale) * 4, o, (y * w * scale + x) * 4, 4);
            File.WriteAllBytes(Path.Combine(output, Path.GetFileName(f)), Encode(w * scale, h * scale, o));
        }
        return 0;
    }

    private static string Arg(string[] a, string name)
    {
        var i = Array.IndexOf(a, name);
        return i >= 0 && i + 1 < a.Length ? a[i + 1] : throw new ArgumentException(name);
    }

    // 가져오기 도구가 쓰는 PNG 만 읽는다(RGBA 8비트, 줄 필터 0)
    private static (int W, int H, byte[] Px) Decode(byte[] png)
    {
        int w = 0, h = 0;
        var idat = new MemoryStream();
        for (var at = 8; at + 8 <= png.Length;)
        {
            var len = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at));
            var type = System.Text.Encoding.ASCII.GetString(png, at + 4, 4);
            if (type == "IHDR") { w = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8)); h = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 12)); }
            if (type == "IDAT") idat.Write(png, at + 8, len);
            at += 12 + len;
        }
        idat.Position = 0;
        using var z = new ZLibStream(idat, CompressionMode.Decompress);
        var raw = new MemoryStream();
        z.CopyTo(raw);
        var r = raw.ToArray();
        var px = new byte[w * h * 4];
        for (var y = 0; y < h; y++) Array.Copy(r, y * (w * 4 + 1) + 1, px, y * w * 4, w * 4);
        return (w, h, px);
    }

    private static byte[] Encode(int w, int h, byte[] px)
    {
        var raw = new byte[(w * 4 + 1) * h];
        for (var y = 0; y < h; y++) Array.Copy(px, y * w * 4, raw, y * (w * 4 + 1) + 1, w * 4);
        var zs = new MemoryStream();
        using (var z = new ZLibStream(zs, CompressionLevel.Fastest, true)) z.Write(raw);
        var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)w);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)h);
        ihdr[8] = 8; ihdr[9] = 6;
        Chunk(ms, "IHDR", ihdr);
        Chunk(ms, "IDAT", zs.ToArray());
        Chunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)data.Length);
        s.Write(len);
        var t = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(t);
        s.Write(data);
        var crc = 0xFFFFFFFFu;
        foreach (var b in t.Concat(data))
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }
        BinaryPrimitives.WriteUInt32BigEndian(len, crc ^ 0xFFFFFFFFu);
        s.Write(len);
    }
}
