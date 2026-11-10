using System.Buffers.Binary;
using DiabloAssetImporter.Bake;
using DiabloAssetImporter.Formats;
using DiabloAssetImporter.Imaging;
using DiabloAssetImporter.Tests.TestKit;

namespace DiabloAssetImporter.Tests;

/// <summary>설계서 10.1 <c>Formats</c>: 시험 인코더로 만든 바이트를 해석기가 되돌린다(부록 A).</summary>
public class FormatsTests
{
    private static void AssertSame(IndexedImage a, IndexedImage b)
    {
        Assert.Equal((a.Width, a.Height), (b.Width, b.Height));
        for (var i = 0; i < a.Index.Length; i++)
        {
            Assert.Equal(a.Opaque[i], b.Opaque[i]);
            if (a.Opaque[i]) Assert.Equal(a.Index[i], b.Index[i]);
        }
    }

    [Fact]
    public void Cl2_RoundTrip_ThreeRunKinds_RowWrappingRuns_WidthFromSkipTable()
    {
        var img = Encoders.Figure(48, 70, 150, 3);
        // 줄을 넘는 투명 런·반복 런·그대로 런이 다 생기게: 몸 가운데 한 줄을 같은 색으로
        for (var x = 10; x < 40; x++) { img.Index[30 * 48 + x] = 200; img.Opaque[30 * 48 + x] = true; }
        var frame = Encoders.Cl2Frame(img);
        Assert.Equal(48, Cl2.EstimateWidth(frame));
        AssertSame(img, Cl2.Decode(frame, 48));

        // 32줄보다 낮은 프레임은 폭을 추정하지 못한다(표 width 를 씀)
        var low = Encoders.Cl2Frame(Encoders.Figure(40, 20, 150, 0));
        Assert.Null(Cl2.EstimateWidth(low));
        AssertSame(Encoders.Figure(40, 20, 150, 0), Cl2.Decode(low, 40));
    }

    [Fact]
    public void Cl2_RunKinds_DecodeExactBytes()
    {
        // 아래 줄부터: 투명 2, 반복(0x80 = 63칸이 아니라 −v−65) — 0xBD(−67) → 2칸 색 9, 그대로 0xFE(−2) → 색 5,6
        byte[] data = [2, 0xBD, 9, 0xFE, 5, 6];
        var img = Cl2.Decode(data, 6);
        Assert.Equal(1, img.Height);
        Assert.False(img.Opaque[0]);
        Assert.False(img.Opaque[1]);
        Assert.Equal(new byte[] { 9, 9, 5, 6 }, img.Index[2..6]);
        Assert.True(img.Opaque[2..6].All(o => o));
    }

    [Fact]
    public void Cl2_GroupHeaderTrap_GroupedReadsAsFlatToo_DecideByKind()
    {
        var frames = Enumerable.Range(0, 3).Select(f => Encoders.Cl2Frame(Encoders.Figure(64, 40, 140, f))).ToList();
        var grouped = Encoders.Grouped([.. Enumerable.Range(0, 8).Select(_ => (IReadOnlyList<byte[]>)frames)]);
        // 그룹 머리 [0x20, …] 는 "프레임 32개" 인 평면 머리로도 읽힐 수 있다 — 그래서 종류별로 정해 부른다
        Assert.Equal(0x20u, BinaryPrimitives.ReadUInt32LittleEndian(grouped));
        Assert.True(Cl2.LooksGrouped(grouped));
        var groups = Cl2.ReadGroups(grouped, 8);
        Assert.Equal(8, groups.Count);
        Assert.All(groups, g => Assert.Equal(3, g.Count));
        var flat = Encoders.Flat(frames);
        Assert.False(Cl2.LooksGrouped(flat));
        Assert.Equal(3, Cl2.ReadFlat(flat).Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void LevelCel_SixKinds_RoundTrip_TrianglePadding(int kind)
    {
        var img = new IndexedImage(32, 32);
        for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++) { img.Index[y * 32 + x] = (byte)(x + y * 3); img.Opaque[y * 32 + x] = kind != 1 || (x / 5 + y) % 2 == 0; }
        var bytes = Encoders.LevelPiece(img, kind);
        if (kind is 2 or 3) Assert.Equal(544, bytes.Length);
        if (kind is 4 or 5) Assert.Equal(800, bytes.Length);
        if (kind == 0) Assert.Equal(1024, bytes.Length);
        var d = LevelCel.Decode(bytes, kind);
        for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++)
            {
                var r = 31 - y;   // 아래부터 센 줄
                bool expect = kind switch
                {
                    0 => true,
                    1 => img.Opaque[y * 32 + x],
                    2 => r < 31 && x >= 32 - LevelCel.TriangleWidth(r),
                    3 => r < 31 && x < LevelCel.TriangleWidth(r),
                    4 => r >= 16 || x >= 32 - LevelCel.TriangleWidth(r),
                    _ => r >= 16 || x < LevelCel.TriangleWidth(r),
                };
                Assert.Equal(expect, d.Opaque[y * 32 + x]);
                if (expect) Assert.Equal(img.Index[y * 32 + x], d.Index[y * 32 + x]);
            }
        // 삼각형 맨 위 1줄은 빈다(바닥 마름모 31줄 — 7.6)
        if (kind is 2 or 3) Assert.All(Enumerable.Range(0, 32), x => Assert.False(d.Opaque[x]));
    }

    [Fact]
    public void Min_TopTierFirst_KindAndFrameBits_Til_ZeroBased()
    {
        var min = MinFile.Read(Encoders.U16s([(3 << 12) | 7, 0, 0, 0, 0, 0, 0, 0, (2 << 12) | 5, (3 << 12) | 6]), 10);
        Assert.Equal(1, min.Count);
        Assert.Equal(3, MinFile.Kind(min.Piece(0, 0)));
        Assert.Equal(7, MinFile.Frame(min.Piece(0, 0)));
        Assert.Equal(5, MinFile.Frame(min.Piece(0, 8)));   // 바닥 단 = 마지막 둘
        var til = TilFile.Read(Encoders.U16s([0, 1, 2, 3, 4, 5, 6, 7]));
        Assert.Equal(2, til.Count);
        Assert.Equal(4, til.Subtile(2, 0));   // 메가 2(1 기반)의 위-왼 = 하위 4(0 기반)
        Assert.Throws<InvalidDataException>(() => til.Subtile(3, 0));
    }

    [Theory]
    [InlineData(0, '.')]
    [InlineData(13, 'x')]
    [InlineData(15, 'X')]
    [InlineData(159, 'X')]
    [InlineData(175, 'X')]
    [InlineData(191, 'X')]
    [InlineData(128 + 1, 'w')]
    [InlineData(2, 'L')]
    [InlineData(41, 'w')]
    public void Sol_ThreeBits_OtherBitsAndTrapIgnored(int sol, char expected) => Assert.Equal(expected, Sol.PropChar((byte)sol));

    [Fact]
    public void Palette_TrnBeforePal_IndexZeroShadowOrBlack()
    {
        var pal = Palette.Read(Encoders.Pal());
        var trn = Encoders.Trn(10);
        var img = new IndexedImage(3, 1);
        img.Index[0] = 0; img.Opaque[0] = true;
        img.Index[1] = 100; img.Opaque[1] = true;
        var shadow = RgbaImage.FromIndexed(img, pal, trn, 0.5f);
        Assert.Equal(new byte[] { 0, 0, 0, 128 }, shadow.Pixels[0..4]);
        Assert.Equal(new byte[] { 110, 145, 55, 255 }, shadow.Pixels[4..8]);   // TRN 100 → 110 → PAL
        Assert.Equal(0, shadow.Pixels[11]);   // RLE 투명 → 알파 0
        var noShadow = RgbaImage.FromIndexed(img, pal, null, null);
        Assert.Equal(new byte[] { 0, 255, 0, 255 }, noShadow.Pixels[0..4]);   // 그림자 값이 없으면 색인 0 을 그대로 색으로
    }

    [Fact]
    public void Wav_8BitTo16Bit_StandardHeader_DropsOtherChunks()
    {
        var c = Wav.ToPcm16(Encoders.Wav8([128, 255, 0, 64, 200], 11025));
        Assert.Equal(44 + 10, c.Bytes.Length);
        Assert.Equal((5L, 11025, 1), (c.Samples, c.Rate, c.Channels));
        var s = Enumerable.Range(0, 5).Select(i => BinaryPrimitives.ReadInt16LittleEndian(c.Bytes.AsSpan(44 + i * 2))).ToArray();
        Assert.Equal(new short[] { 0, 127 << 8, -128 << 8, -64 << 8, 72 << 8 }, s);
        Assert.Equal(16, BinaryPrimitives.ReadUInt16LittleEndian(c.Bytes.AsSpan(34)));
        Assert.Throws<InvalidDataException>(() => Wav.ToPcm16(Encoders.Wav8([])));
        var adpcm = Encoders.Wav8([1, 2]);
        adpcm[20] = 2;
        Assert.Throws<InvalidDataException>(() => Wav.ToPcm16(adpcm));
    }

    [Fact]
    public void Png_Deterministic_RoundTrips()
    {
        var img = new RgbaImage(5, 3);
        for (var i = 0; i < img.Pixels.Length; i++) img.Pixels[i] = (byte)(i * 7);
        var a = Png.Encode(img);
        Assert.Equal(a, Png.Encode(img));
        var back = Png.Decode(a);
        Assert.Equal(img.Pixels, back.Pixels);
        Assert.Equal((5, 3), Png.ReadSize(a));
    }

    [Fact]
    public void Dun_ReadsHeaderAndCells()
    {
        var d = DunFile.Read(Encoders.U16s([2, 2, 1, 0, 13, 4, 99]));
        Assert.Equal((2, 2), (d.Width, d.Height));
        Assert.Equal(13, d.At(0, 1));
        Assert.Throws<InvalidDataException>(() => DunFile.Read(Encoders.U16s([5, 5, 1])));
    }

    [Fact]
    public void UnitWidth_FileDiffersFromTable_IsCheckFailure()
    {
        var cl2 = SynthSources.UnitCl2(96, 2, 8);
        var a = new ResolvedImportAction("cast_fire", "x.cl2", 128, null, 0.5f);
        var ex = Assert.Throws<ImportCheckException>(() => UnitBaker.DecodeUnitAction("hero/slt", a, cl2));
        Assert.Contains("hero/slt cast_fire: 파일 폭 96, 표 128", ex.Message);
    }

    [Fact]
    public void Cel_HeaderOrNot_SameImage_WidthFromSkipTableOnlyWithHeader()
    {
        // 원작 아이템·물체·마을 사람 CEL 은 프레임 머리가 있고, 조작판 그림(spelicon·panel8)은 없다(v0.9 원본 확인)
        var img = Encoders.Figure(96, 70, 150, 2);
        var withHead = Encoders.CelFrame(img);
        var bare = Encoders.CelFrame(img, header: false);
        Assert.True(Cel.HasHeader(withHead));
        Assert.False(Cel.HasHeader(bare));
        Assert.Equal(96, Cel.EstimateWidth(withHead));
        Assert.Null(Cel.EstimateWidth(bare));
        AssertSame(img, Cel.Decode(withHead, 96));
        AssertSame(img, Cel.Decode(bare, 96));

        // 32줄보다 낮은 머리 있는 프레임(인벤토리 1칸 28×28)은 위치가 모두 0 — 폭은 표에서
        var small = Encoders.Figure(28, 28, 140, 0);
        var smallFrame = Encoders.CelFrame(small);
        Assert.True(Cel.HasHeader(smallFrame));
        Assert.Null(Cel.EstimateWidth(smallFrame));
        AssertSame(small, Cel.Decode(smallFrame, 28));
    }

    [Fact]
    public void Cel_HeaderlessFrameStartingLikeHeader_IsNotHeader()
    {
        // 머리 없는 프레임의 아래 줄이 "그대로 10칸, 색인 0 아홉 + …" 이면 첫 u16 이 0x000A, 위치 넷이 0 으로 읽힌다 — 머리로 보면 안 된다
        var img = new IndexedImage(10, 2);
        for (var i = 0; i < 20; i++) { img.Opaque[i] = true; img.Index[i] = (byte)(i < 10 ? 200 : 0); }
        var bare = Encoders.CelFrame(img, header: false);
        Assert.Equal(0x0A, bare[0]);
        Assert.Equal(0, bare[1]);
        Assert.False(Cel.HasHeader(bare));
        AssertSame(img, Cel.Decode(bare, 10));
    }

    [Fact]
    public void CelSprite_InMissileEntry_WidthCheck_FrameRange_TableWidthForLowFrames()
    {
        // missiles.json 항목의 원본이 .cel 이면 CEL 로 푼다(v0.9 — 바닥 아이템·물체·마을 사람·인벤토리 그림)
        var frames = new[] { Encoders.Figure(56, 56, 150, 0), Encoders.Figure(28, 28, 160, 1), Encoders.Figure(56, 84, 170, 2) };
        var file = Encoders.Flat([.. frames.Select(f => Encoders.CelFrame(f))]);
        var e = new ImportMissileEntry { Out = "d1/inv/x", Cl2 = @"data\inv\objcurs.cel", Width = 56, Frames = [3, 3] };
        var d = UnitBaker.DecodeMissile("inv/x", e, [file], null);
        Assert.Equal((56, 84, 1, 1), (d.Cell.X, d.Cell.Y, d.Directions, d.Frames));
        AssertSame(frames[2], d.Images[0]);

        var ring = UnitBaker.DecodeMissile("inv/ring", e with { Width = 28, Frames = [2, 2] }, [file], null);
        AssertSame(frames[1], ring.Images[0]);

        // 파일 폭(머리)과 표 폭이 다르면 대조 실패, 머리로 폭을 못 정하는 프레임에서 표 폭이 틀리면 픽셀 수로 잡는다
        var wide = Assert.Throws<ImportCheckException>(() => UnitBaker.DecodeMissile("inv/x", e with { Width = 28 }, [file], null));
        Assert.Contains("파일 폭 56, 표 28", wide.Message);
        var low = Assert.Throws<ImportCheckException>(() => UnitBaker.DecodeMissile("inv/ring", e with { Width = 27, Frames = [2, 2] }, [file], null));
        Assert.Contains("배수", low.Message);
    }
}
