using System.Buffers.Binary;

namespace DiabloAssetImporter.Formats;

/// <summary>색인 그림 한 장: 색인 + 투명 여부(투명은 RLE 로만 정해진다, 부록 A.2). 줄 0 = 위.</summary>
internal sealed class IndexedImage(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Index { get; } = new byte[width * height];
    public bool[] Opaque { get; } = new bool[width * height];
}

/// <summary>
/// 스프라이트 CL2 해석기 (부록 A.2). 묶음 머리(8그룹 / 평면)는 종류별로 정해 부른다 — 그룹 머리가 평면 표로도 읽히는 함정 때문.
/// </summary>
internal static class Cl2
{
    /// <summary>프레임 바이트 조각(머리 포함)들. 8그룹이면 그룹마다 목록.</summary>
    public static IReadOnlyList<IReadOnlyList<ReadOnlyMemory<byte>>> ReadGroups(ReadOnlyMemory<byte> file, int groups)
    {
        var span = file.Span;
        var result = new List<IReadOnlyList<ReadOnlyMemory<byte>>>(groups);
        for (var g = 0; g < groups; g++)
        {
            // 그룹 안 오프셋은 그 그룹 머리 기준이다. 그룹 끝은 다음 그룹 머리로 자르지 않는다(프레임 범위 검사는 파일 끝까지)
            var at = (int)U32(span, g * 4);
            if (at < 0 || at > file.Length) throw new InvalidDataException($"CL2 그룹 {g} 오프셋 {at} 이 파일 밖이다");
            result.Add(ReadFlat(file[at..]));
        }
        return result;
    }

    /// <summary>평면: <c>u32 프레임 수, u32 오프셋[n + 1]</c>(이 조각 기준).</summary>
    public static IReadOnlyList<ReadOnlyMemory<byte>> ReadFlat(ReadOnlyMemory<byte> block)
    {
        var span = block.Span;
        if (span.Length < 4) throw new InvalidDataException("CL2 머리가 짧다");
        var n = (int)U32(span, 0);
        if (n < 0 || 4 + (n + 1) * 4 > span.Length) throw new InvalidDataException($"CL2 프레임 수 {n} 이 파일 크기와 맞지 않는다");
        var frames = new List<ReadOnlyMemory<byte>>(n);
        for (var i = 0; i < n; i++)
        {
            var a = (int)U32(span, 4 + i * 4);
            var b = (int)U32(span, 8 + i * 4);
            if (a < 0 || b < a || b > span.Length) throw new InvalidDataException($"CL2 프레임 {i + 1} 범위 {a}..{b} 가 틀렸다");
            frames.Add(block[a..b]);
        }
        return frames;
    }

    /// <summary>
    /// 그룹 머리 판별(시험·probe 용): 8그룹으로 읽어 마지막 그룹의 마지막 프레임 끝이 파일 길이와 같으면 그룹이다.
    /// </summary>
    public static bool LooksGrouped(ReadOnlySpan<byte> file)
    {
        if (file.Length < 32) return false;
        var last = (int)U32(file, 28);
        if (last <= 0 || last + 4 > file.Length) return false;
        var n = (int)U32(file, last);
        if (n < 0 || last + 4 + (n + 1) * 4 > file.Length) return false;
        return last + (int)U32(file, last + 4 + n * 4) == file.Length;
    }

    /// <summary>
    /// 프레임 폭 추정: 10바이트 머리(<c>0x000A</c> + 32줄 건너뛰기 위치 4개)가 있으면 첫 위치까지(32줄)의 픽셀 수 ÷ 32. 추정할 수 없으면 null.
    /// </summary>
    public static int? EstimateWidth(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 10 || BinaryPrimitives.ReadUInt16LittleEndian(frame) != 0x0A) return null;
        var skip32 = BinaryPrimitives.ReadUInt16LittleEndian(frame[2..]);
        if (skip32 == 0 || skip32 <= 10 || skip32 > frame.Length) return null;
        var px = CountPixels(frame[10..skip32]);
        return px % 32 == 0 && px > 0 ? px / 32 : null;
    }

    /// <summary>데이터 부분(머리 뒤)의 픽셀 수.</summary>
    public static int CountPixels(ReadOnlySpan<byte> data)
    {
        var px = 0;
        var i = 0;
        while (i < data.Length)
        {
            var v = (sbyte)data[i++];
            if (v >= 0) px += v;                       // 투명 v칸
            else if (v >= -65) { px += -v; i += -v; }  // 0xBF..0xFF: (−v)바이트 그대로
            else { px += -v - 65; i++; }               // 0x80..0xBE: 한 바이트 (−v−65)칸 반복
        }
        return px;
    }

    /// <summary>프레임 하나를 푼다. 폭은 <see cref="EstimateWidth"/> 또는 <paramref name="fallbackWidth"/>. 아래 줄부터 위로, 칸이 줄 끝을 넘으면 다음 줄로.</summary>
    public static IndexedImage Decode(ReadOnlySpan<byte> frame, int width)
    {
        var data = frame.Length >= 10 && BinaryPrimitives.ReadUInt16LittleEndian(frame) == 0x0A ? frame[10..] : frame;
        var total = CountPixels(data);
        if (width <= 0) throw new InvalidDataException("CL2 폭이 0 이다");
        var height = (total + width - 1) / width;
        var img = new IndexedImage(width, height);
        var p = 0;   // 아래 줄부터 센 픽셀 번호
        var i = 0;
        void Put(byte c, bool opaque)
        {
            var x = p % width;
            var yFromBottom = p / width;
            var y = height - 1 - yFromBottom;
            if (y >= 0)
            {
                img.Index[y * width + x] = c;
                img.Opaque[y * width + x] = opaque;
            }
            p++;
        }
        while (i < data.Length)
        {
            var v = (sbyte)data[i++];
            if (v >= 0) p += v;
            else if (v >= -65)
            {
                var n = -v;
                for (var k = 0; k < n && i < data.Length; k++) Put(data[i++], true);
            }
            else
            {
                var n = -v - 65;
                var c = i < data.Length ? data[i++] : (byte)0;
                for (var k = 0; k < n; k++) Put(c, true);
            }
        }
        return img;
    }

    internal static uint U32(ReadOnlySpan<byte> s, int at) =>
        at + 4 <= s.Length && at >= 0 ? BinaryPrimitives.ReadUInt32LittleEndian(s[at..]) : throw new InvalidDataException($"오프셋 {at} 이 파일 밖이다");
}
