using System.Buffers.Binary;

namespace DiabloAssetImporter.Formats;

/// <summary>
/// 스프라이트 CEL 해석기(원작 UI·아이템·물체·마을 사람 그림). 파일 머리는 CL2 평면과 같은 <c>u32 프레임 수, u32 오프셋[n + 1]</c>(<see cref="Cl2.ReadFlat"/>).
/// 칸 부호가 CL2 와 반대다: 0x00..0x7F = 그 수만큼 색인 바이트 그대로, 0x80..0xFF = (256−값)칸 투명. 줄은 아래부터 위로, 칸이 줄 끝을 넘으면 다음 줄로(CL2 와 같음).
/// <list type="bullet">
/// <item>프레임 머리는 있을 수도 없을 수도 있다(v0.9 원본 확인): <c>ctrlpan\spelicon.cel</c>·<c>panel8.cel</c> 같은 조작판 그림은 없고,
/// <c>items\*</c>·<c>objects\*</c>·<c>towners\*</c>·<c>data\inv\objcurs.cel</c> 은 CL2 와 같은 10바이트 머리(<c>0x000A</c> + 32줄마다 데이터 위치 4개, 없는 자리 0)가 있다.
/// 파일·프레임마다 <see cref="HasHeader"/> 로 가린다 — 머리 모양이고 위치가 모두 칸 경계에 떨어질 때만 머리로 본다.</item>
/// <item>폭은 머리가 있고 32줄 이상이면 첫 32줄의 픽셀 수 ÷ 32(<see cref="EstimateWidth"/>), 아니면 표의 폭.</item>
/// </list>
/// 지형 CEL 은 <see cref="LevelCel"/>.
/// </summary>
internal static class Cel
{
    public const int HeaderSize = 10;

    public static IReadOnlyList<ReadOnlyMemory<byte>> Frames(ReadOnlyMemory<byte> file) => Cl2.ReadFlat(file);

    /// <summary>
    /// 프레임 머리가 있는지: 첫 u16 이 10 이고, 뒤 위치 넷이 (0 이 아닌 것끼리) 늘어나며 프레임 안이고 0 뒤에 0 아닌 값이 없고,
    /// 머리 뒤 데이터를 칸 단위로 걸을 때 위치마다 칸 경계에 떨어지고 데이터가 프레임 끝에서 정확히 끝난다.
    /// 머리 없는 프레임이 우연히 <c>0A 00</c> 으로 시작해도(10바이트 그대로 + 색인 0) 이 조건을 다 맞추기는 어렵다 — 머리 뒤 첫 바이트가 그 칸의 색인이라
    /// 길이 0 칸(원작 인코더가 쓰지 않음)이나 프레임 끝을 넘는 칸이 되기 쉽다.
    /// </summary>
    public static bool HasHeader(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < HeaderSize || BinaryPrimitives.ReadUInt16LittleEndian(frame) != HeaderSize) return false;
        Span<int> marks = stackalloc int[4];
        var prev = HeaderSize;
        var zero = false;
        for (var k = 0; k < 4; k++)
        {
            int v = BinaryPrimitives.ReadUInt16LittleEndian(frame[(2 + k * 2)..]);
            marks[k] = v;
            if (v == 0) { zero = true; continue; }
            if (zero || v < prev || v > frame.Length) return false;
            prev = v;
        }
        // 칸 경계 걷기
        var next = 0;
        var i = HeaderSize;
        while (i < frame.Length)
        {
            if (next < 4 && marks[next] != 0 && marks[next] < i) return false;   // 칸 가운데에 떨어짐
            while (next < 4 && marks[next] == i) next++;
            var v = (sbyte)frame[i++];
            if (v == 0) return false;   // 길이 0 칸은 원작 인코더가 쓰지 않는다 — 머리로 잘못 본 색인 바이트
            if (v > 0) i += v;
        }
        if (i != frame.Length) return false;
        for (; next < 4; next++)
            if (marks[next] != 0 && marks[next] != frame.Length) return false;
        return true;
    }

    /// <summary>머리 뒤(또는 머리 없는 프레임 전체) 데이터.</summary>
    public static ReadOnlySpan<byte> Data(ReadOnlySpan<byte> frame) => HasHeader(frame) ? frame[HeaderSize..] : frame;

    /// <summary>데이터의 픽셀 수(투명 포함). 칸 길이가 데이터 밖으로 나가면 형식 오류.</summary>
    public static int CountPixels(ReadOnlySpan<byte> data)
    {
        var total = 0;
        for (var i = 0; i < data.Length;)
        {
            var v = (sbyte)data[i++];
            if (v >= 0)
            {
                if (i + v > data.Length) throw new InvalidDataException($"CEL 칸 {v}바이트가 프레임 끝을 넘는다");
                total += v;
                i += v;
            }
            else total += -v;
        }
        return total;
    }

    /// <summary>폭 추정: 머리가 있고 첫 32줄 위치가 있으면 그 구간 픽셀 수 ÷ 32. 추정할 수 없으면 null(32줄보다 낮은 프레임, 머리 없는 프레임).</summary>
    public static int? EstimateWidth(ReadOnlySpan<byte> frame)
    {
        if (!HasHeader(frame)) return null;
        int skip32 = BinaryPrimitives.ReadUInt16LittleEndian(frame[2..]);
        if (skip32 <= HeaderSize) return null;
        var px = CountPixels(frame[HeaderSize..skip32]);
        return px > 0 && px % 32 == 0 ? px / 32 : null;
    }

    /// <summary>프레임 하나를 폭 <paramref name="width"/> 로 푼다(머리가 있으면 건너뜀). 높이 = 픽셀 수 ÷ 폭(올림).</summary>
    public static IndexedImage Decode(ReadOnlySpan<byte> frame, int width)
    {
        if (width <= 0) throw new InvalidDataException("CEL 폭이 0 이다");
        var data = Data(frame);
        var total = CountPixels(data);
        var height = (total + width - 1) / width;
        var img = new IndexedImage(width, height);
        var p = 0;   // 아래 줄부터 센 픽셀 번호
        for (var i = 0; i < data.Length;)
        {
            var v = (sbyte)data[i++];
            if (v < 0) { p += -v; continue; }
            for (var k = 0; k < v && i < data.Length; k++, p++)
            {
                var y = height - 1 - p / width;
                if (y < 0) continue;
                img.Index[y * width + p % width] = data[i++];
                img.Opaque[y * width + p % width] = true;
            }
        }
        return img;
    }
}
