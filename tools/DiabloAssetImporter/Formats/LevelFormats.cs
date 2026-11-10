using System.Buffers.Binary;

namespace DiabloAssetImporter.Formats;

/// <summary>
/// 레벨 CEL 조각 (부록 A.3): 머리 <c>u32 프레임 수, u32 오프셋[n + 1]</c>, 프레임 1부터. 조각 종류는 CEL 이 아니라 MIN 값 비트 12~14.
/// 결과는 32×32 색인 그림(줄 0 = 위). 삼각형(31줄)은 아래에 붙는다 — 맨 위 1줄은 빈다.
/// </summary>
internal static class LevelCel
{
    public const int Size = 32;

    public static IReadOnlyList<ReadOnlyMemory<byte>> Frames(ReadOnlyMemory<byte> file) => Cl2.ReadFlat(file);

    /// <summary>삼각형 행 i(아래부터 0..30)의 폭: 2,4,…,32,30,…,2.</summary>
    public static int TriangleWidth(int i) => i < 16 ? 2 * (i + 1) : 2 * (31 - i);

    /// <summary>조각 하나를 32×32 에 푼다(종류 0..5).</summary>
    public static IndexedImage Decode(ReadOnlySpan<byte> data, int kind)
    {
        var img = new IndexedImage(Size, Size);
        void Put(int x, int rowFromBottom, byte c)
        {
            var y = Size - 1 - rowFromBottom;
            if ((uint)x >= Size || (uint)y >= Size) return;
            img.Index[y * Size + x] = c;
            img.Opaque[y * Size + x] = true;
        }
        var i = 0;
        byte Next(ReadOnlySpan<byte> d) => i < d.Length ? d[i++] : throw new InvalidDataException($"레벨 CEL 조각(종류 {kind})이 짧다");
        switch (kind)
        {
            case 0:   // 평면 사각형 32×32
                for (var r = 0; r < Size; r++)
                    for (var x = 0; x < Size; x++) Put(x, r, Next(data));
                break;
            case 1:   // 투명 사각형: 행마다 i8 v — 양수 v 개 색, 음수 −v 개 투명
                for (var r = 0; r < Size; r++)
                {
                    var x = 0;
                    while (x < Size)
                    {
                        var v = (sbyte)Next(data);
                        if (v >= 0) { for (var k = 0; k < v; k++) Put(x++, r, Next(data)); }
                        else x += -v;
                    }
                }
                break;
            case 2: case 3: case 4: case 5:
            {
                var left = kind is 2 or 4;
                var rows = kind is 2 or 3 ? 31 : 16;
                for (var r = 0; r < rows; r++)
                {
                    var w = TriangleWidth(r);
                    var pad = r % 2 == 0;
                    if (pad && left) { Next(data); Next(data); }
                    var x0 = left ? Size - w : 0;
                    for (var k = 0; k < w; k++) Put(x0 + k, r, Next(data));
                    if (pad && !left) { Next(data); Next(data); }
                }
                if (kind is 4 or 5)
                    for (var r = 16; r < Size; r++)
                        for (var x = 0; x < Size; x++) Put(x, r, Next(data));
                break;
            }
            default:
                throw new InvalidDataException($"레벨 CEL 조각 종류 {kind} 는 0..5 다");
        }
        return img;
    }

    /// <summary>
    /// 아치 덧그림 CEL(L1S, 부록 A.3): 폭 64, 아래 행부터, <c>b &lt; 0x80</c> 이면 b 개 색 복사, 아니면 256 − b 개 투명. 프레임 머리말 없음.
    /// </summary>
    public static IndexedImage DecodeOverlay(ReadOnlySpan<byte> data, int width = 64)
    {
        var total = 0;
        for (var i = 0; i < data.Length;)
        {
            var b = data[i++];
            if (b < 0x80) { total += b; i += b; } else total += 256 - b;
        }
        var height = Math.Max(1, (total + width - 1) / width);
        var img = new IndexedImage(width, height);
        var p = 0;
        for (var i = 0; i < data.Length;)
        {
            var b = data[i++];
            if (b < 0x80)
            {
                for (var k = 0; k < b && i < data.Length; k++, p++)
                {
                    var y = height - 1 - p / width;
                    if (y < 0) continue;
                    img.Index[y * width + p % width] = data[i++];
                    img.Opaque[y * width + p % width] = true;
                }
            }
            else p += 256 - b;
        }
        return img;
    }
}

/// <summary>MIN (부록 A.3): 하위 타일마다 <c>u16 × 조각 수</c>, 파일은 위 단부터. 값 = 종류(비트 12~14) | CEL 프레임(비트 0~11, 1 기반), 0 = 빈 칸.</summary>
internal sealed class MinFile(ushort[] values, int piecesPerSubtile)
{
    public int PiecesPerSubtile { get; } = piecesPerSubtile;
    public int Count => values.Length / PiecesPerSubtile;

    public static MinFile Read(ReadOnlySpan<byte> bytes, int piecesPerSubtile)
    {
        if (bytes.Length % (2 * piecesPerSubtile) != 0) throw new InvalidDataException($"MIN 크기 {bytes.Length} 가 조각 수 {piecesPerSubtile} 와 맞지 않는다");
        var v = new ushort[bytes.Length / 2];
        for (var i = 0; i < v.Length; i++) v[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(i * 2)..]);
        return new MinFile(v, piecesPerSubtile);
    }

    /// <summary>하위 타일 <paramref name="subtile"/>(0 기반)의 조각 k(0 = 위 단 왼쪽 … 마지막 둘 = 바닥 단). 짝수 = 왼쪽, 홀수 = 오른쪽.</summary>
    public ushort Piece(int subtile, int k) => values[subtile * PiecesPerSubtile + k];

    public static int Kind(ushort v) => (v >> 12) & 7;
    public static int Frame(ushort v) => v & 0x0FFF;
}

/// <summary>TIL (부록 A.3): 메가타일 하나 = <c>u16 × 4</c> = 하위 타일 번호(0 기반) 위-왼, 위-오른, 아래-왼, 아래-오른.</summary>
internal sealed class TilFile(ushort[] values)
{
    public int Count => values.Length / 4;

    public static TilFile Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length % 8 != 0) throw new InvalidDataException($"TIL 크기 {bytes.Length} 가 8 의 배수가 아니다");
        var v = new ushort[bytes.Length / 2];
        for (var i = 0; i < v.Length; i++) v[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(i * 2)..]);
        return new TilFile(v);
    }

    /// <summary>메가타일 번호(1 기반, 타일셋 정의와 같은 체계)의 하위 칸 k(0..3)의 하위 타일 번호(0 기반).</summary>
    public int Subtile(int mega, int k)
    {
        if (mega < 1 || mega > Count) throw new InvalidDataException($"메가타일 {mega} 가 TIL(1..{Count}) 밖이다");
        return values[(mega - 1) * 4 + k];
    }
}

/// <summary>SOL (부록 A.4): 하위 타일마다 1바이트. 성질 대조는 <c>sol &amp; 7</c>(bit0 걷기, bit1 시야, bit2 투사체)만 본다.</summary>
internal static class Sol
{
    public static char PropChar(byte sol) => ArenaLayout.PropChars[sol & 7];

    /// <summary>글자의 뜻(실패 문구용).</summary>
    public static string Describe(char table, char sol)
    {
        var a = ArenaLayout.PropChars.IndexOf(table);
        var b = ArenaLayout.PropChars.IndexOf(sol);
        if (a < 0 || b < 0) return "알 수 없는 글자";
        var diff = a ^ b;
        var parts = new List<string>();
        if ((diff & 1) != 0) parts.Add("걷기");
        if ((diff & 2) != 0) parts.Add("시야");
        if ((diff & 4) != 0) parts.Add("투사체");
        return string.Join("·", parts) + " 막힘 다름";
    }
}

/// <summary>PAL(768바이트, 256 × RGB 8비트)·TRN(256바이트 색인 → 색인, 팔레트 전에 적용).</summary>
internal sealed class Palette(byte[] rgb)
{
    public static Palette Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 768) throw new InvalidDataException($"PAL 크기 {bytes.Length} 는 768 이상");
        return new Palette(bytes[..768].ToArray());
    }

    public (byte R, byte G, byte B) this[int i] => (rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]);

    public static byte[] ReadTrn(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 256) throw new InvalidDataException($"TRN 크기 {bytes.Length} 는 256 이상");
        return bytes[..256].ToArray();
    }
}

/// <summary>DUN(부록 A.3 끝 — audit 대조에만): <c>u16 폭, u16 높이</c> 뒤 칸마다 <c>u16</c>(줄 = y). 0 = 비움. 내용은 메모리에서만 쓴다.</summary>
internal sealed class DunFile(int width, int height, ushort[] cells)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int At(int x, int y) => cells[y * Width + x];

    public static DunFile Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4) throw new InvalidDataException("DUN 머리가 짧다");
        int w = BinaryPrimitives.ReadUInt16LittleEndian(bytes), h = BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]);
        if (4 + w * h * 2 > bytes.Length) throw new InvalidDataException($"DUN {w}×{h} 가 파일보다 크다");
        var c = new ushort[w * h];
        for (var i = 0; i < c.Length; i++) c[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(4 + i * 2)..]);
        return new DunFile(w, h, c);
    }
}
