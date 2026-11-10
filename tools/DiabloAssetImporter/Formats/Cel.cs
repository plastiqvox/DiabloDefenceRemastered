namespace DiabloAssetImporter.Formats;

/// <summary>
/// 스프라이트 CEL 해석기(원작 UI 그림 — 마법 아이콘 <c>ctrlpan\spelicon.cel</c> 등). 파일 머리는 CL2 평면과 같은 <c>u32 프레임 수, u32 오프셋[n + 1]</c>
/// (<see cref="Cl2.ReadFlat"/>)이고, 프레임에는 10바이트 머리가 없다. 칸 부호가 CL2 와 반대다: 0x00..0x7F = 그 수만큼 색인 바이트 그대로, 0x80..0xFF = (256−값)칸 투명.
/// 줄은 아래부터 위로, 칸이 줄 끝을 넘으면 다음 줄로(CL2 와 같음). 지형 CEL 은 <see cref="LevelCel"/>.
/// </summary>
internal static class Cel
{
    public static IReadOnlyList<ReadOnlyMemory<byte>> Frames(ReadOnlyMemory<byte> file) => Cl2.ReadFlat(file);

    /// <summary>프레임 하나를 폭 <paramref name="width"/> 로 푼다. 높이 = 픽셀 수 ÷ 폭(올림).</summary>
    public static IndexedImage Decode(ReadOnlySpan<byte> data, int width)
    {
        if (width <= 0) throw new InvalidDataException("CEL 폭이 0 이다");
        var total = 0;
        for (var i = 0; i < data.Length;)
        {
            var v = (sbyte)data[i++];
            if (v >= 0) { total += v; i += v; }
            else total += -v;
        }
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
