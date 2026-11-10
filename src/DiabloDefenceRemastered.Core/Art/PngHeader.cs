using System.Buffers.Binary;

namespace DiabloDefenceRemastered.Core.Art;

/// <summary>
/// PNG 크기 읽기 (설계서 1.3·13.5-7, 소유 WP5). 해독하지 않고 앞 24바이트(서명 8 + IHDR 길이·형 8 + 폭·높이 8)만 본다.
/// 실제 자원 대조(<see cref="ArtAssetValidator"/>)와 시험이 쓴다 — 시험에 PNG 해독 패키지를 넣지 않으려고(13.4 L9).
/// </summary>
public static class PngHeader
{
    /// <summary>읽어야 하는 앞부분 길이.</summary>
    public const int Length = 24;

    private static ReadOnlySpan<byte> Signature => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> Ihdr => "IHDR"u8;

    /// <summary>
    /// 앞 24바이트에서 폭·높이. 서명이 PNG 가 아니거나, 첫 덩이가 길이 13 의 IHDR 가 아니거나, 폭·높이가 0 이거나 2³¹ 이상이면 거짓.
    /// </summary>
    public static bool TryRead(ReadOnlySpan<byte> head, out Int2 size)
    {
        size = default;
        if (head.Length < Length) return false;
        if (!head[..8].SequenceEqual(Signature)) return false;
        if (BinaryPrimitives.ReadUInt32BigEndian(head[8..12]) != 13) return false;
        if (!head[12..16].SequenceEqual(Ihdr)) return false;
        var w = BinaryPrimitives.ReadUInt32BigEndian(head[16..20]);
        var h = BinaryPrimitives.ReadUInt32BigEndian(head[20..24]);
        if (w is 0 or > int.MaxValue || h is 0 or > int.MaxValue) return false;
        size = new Int2((int)w, (int)h);
        return true;
    }

    /// <summary>파일의 폭·높이. PNG 머리글이 아니면 <see cref="InvalidDataException"/>.</summary>
    public static Int2 Read(string path)
    {
        Span<byte> head = stackalloc byte[Length];
        using (var stream = File.OpenRead(path))
        {
            var read = 0;
            while (read < Length)
            {
                var n = stream.Read(head[read..]);
                if (n == 0) break;
                read += n;
            }
            if (read < Length) throw new InvalidDataException($"{path}: PNG 머리글이 짧다({read}바이트)");
        }
        return TryRead(head, out var size) ? size : throw new InvalidDataException($"{path}: PNG 서명·IHDR 머리글이 아니다");
    }
}
