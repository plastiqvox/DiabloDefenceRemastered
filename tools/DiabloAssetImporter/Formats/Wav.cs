using System.Buffers.Binary;

namespace DiabloAssetImporter.Formats;

/// <summary>
/// 소리 변환 (1.4 변환 규칙 "소리", 부록 A.6): RIFF PCM 만. 8비트(부호 없음) → 16비트 <c>(b − 128) &lt;&lt; 8</c>, 16비트는 그대로.
/// 채널·표본율 그대로, 표준 44바이트 머리로 다시 쓴다(다른 덩어리는 버림). 형식 ≠ PCM, 길이 0 이면 실패.
/// </summary>
internal static class Wav
{
    public sealed record Converted(byte[] Bytes, long Samples, int Rate, int Channels);

    public static Converted ToPcm16(ReadOnlySpan<byte> src)
    {
        if (src.Length < 12 || !src[..4].SequenceEqual("RIFF"u8) || !src[8..12].SequenceEqual("WAVE"u8))
            throw new InvalidDataException("RIFF WAVE 가 아니다");
        int? format = null, channels = null, rate = null, bits = null;
        ReadOnlySpan<byte> data = default;
        var haveData = false;
        var at = 12;
        while (at + 8 <= src.Length)
        {
            var id = src.Slice(at, 4);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(src[(at + 4)..]);
            var body = at + 8;
            var len = Math.Min(size, src.Length - body);
            if (id.SequenceEqual("fmt "u8) && len >= 16)
            {
                format = BinaryPrimitives.ReadUInt16LittleEndian(src[body..]);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(src[(body + 2)..]);
                rate = (int)BinaryPrimitives.ReadUInt32LittleEndian(src[(body + 4)..]);
                bits = BinaryPrimitives.ReadUInt16LittleEndian(src[(body + 14)..]);
            }
            else if (id.SequenceEqual("data"u8))
            {
                data = src.Slice(body, len);
                haveData = true;
            }
            at = body + size + (size & 1);
        }
        if (format is null || !haveData) throw new InvalidDataException("fmt·data 덩어리가 없다");
        if (format != 1) throw new InvalidDataException($"형식 {format} 은 PCM(1)이 아니다");
        if (channels is not (>= 1 and <= 8) || rate <= 0) throw new InvalidDataException("채널·표본율이 틀렸다");
        if (data.Length == 0) throw new InvalidDataException("소리 길이가 0 이다");

        byte[] pcm;
        if (bits == 8)
        {
            pcm = new byte[data.Length * 2];
            for (var i = 0; i < data.Length; i++)
                BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(i * 2), (short)((data[i] - 128) << 8));
        }
        else if (bits == 16) pcm = data[..(data.Length & ~1)].ToArray();
        else throw new InvalidDataException($"표본 비트 {bits} 는 8·16 이 아니다");

        var ch = channels!.Value;
        var r = rate!.Value;
        var outBytes = new byte[44 + pcm.Length];
        var o = outBytes.AsSpan();
        "RIFF"u8.CopyTo(o);
        BinaryPrimitives.WriteUInt32LittleEndian(o[4..], (uint)(36 + pcm.Length));
        "WAVE"u8.CopyTo(o[8..]);
        "fmt "u8.CopyTo(o[12..]);
        BinaryPrimitives.WriteUInt32LittleEndian(o[16..], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(o[20..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(o[22..], (ushort)ch);
        BinaryPrimitives.WriteUInt32LittleEndian(o[24..], (uint)r);
        BinaryPrimitives.WriteUInt32LittleEndian(o[28..], (uint)(r * ch * 2));
        BinaryPrimitives.WriteUInt16LittleEndian(o[32..], (ushort)(ch * 2));
        BinaryPrimitives.WriteUInt16LittleEndian(o[34..], 16);
        "data"u8.CopyTo(o[36..]);
        BinaryPrimitives.WriteUInt32LittleEndian(o[40..], (uint)pcm.Length);
        pcm.CopyTo(o[44..]);
        return new Converted(outBytes, pcm.Length / (ch * 2), r, ch);
    }
}
