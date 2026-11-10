using System.Buffers.Binary;
using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Core.Assets;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>PNG IHDR 읽기·실제 자원 대조 13.5-7 (WP5, 10.1 <c>Art</c>). 자원은 합성 — 임시 폴더에 머리글만 있는 PNG·메타·manifest 를 쓴다.</summary>
public class ArtAssetValidatorTests
{
    /// <summary>머리글 24바이트 + IHDR 나머지·CRC 자리(해독하지 않으므로 0).</summary>
    public static byte[] PngBytes(int w, int h)
    {
        var b = new byte[33];
        ReadOnlySpan<byte> sig = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        sig.CopyTo(b);
        BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(8), 13);
        "IHDR"u8.CopyTo(b.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(16), (uint)w);
        BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(20), (uint)h);
        b[24] = 8; b[25] = 6;
        return b;
    }

    private static void WritePng(TempDir dir, string rel, int w, int h)
    {
        var full = Path.Combine(dir.Path, rel.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, PngBytes(w, h));
    }

    [Fact]
    public void PngHeader_ReadsSize_RejectsNonPng()
    {
        Assert.True(PngHeader.TryRead(PngBytes(1536, 96), out var size));
        Assert.Equal(new Int2(1536, 96), size);

        var bad = PngBytes(10, 10);
        bad[1] = (byte)'X';
        Assert.False(PngHeader.TryRead(bad, out _));                       // 서명
        var notIhdr = PngBytes(10, 10);
        notIhdr[12] = (byte)'I'; notIhdr[13] = (byte)'D';
        Assert.False(PngHeader.TryRead(notIhdr, out _));                   // 첫 덩이가 IHDR 아님
        Assert.False(PngHeader.TryRead(PngBytes(0, 10), out _));            // 폭 0
        Assert.False(PngHeader.TryRead(PngBytes(10, 10).AsSpan(0, 20), out _));   // 짧음

        using var dir = new TempDir();
        WritePng(dir, "a.png", 64, 160);
        Assert.Equal(new Int2(64, 160), PngHeader.Read(Path.Combine(dir.Path, "a.png")));
        dir.Write("b.png", "not a png at all, longer than 24 bytes");
        Assert.Throws<InvalidDataException>(() => PngHeader.Read(Path.Combine(dir.Path, "b.png")));
    }

    // ── 합성 자원: atlas 묶음 하나(변종 둘, 페이지 둘) + grid 하나 ──────────────

    private static SpriteDef AtlasSprite(string action = "walk", int frames = 2) => new()
    {
        Id = "zombie_" + action, Source = SpriteSource.Atlas, File = "d1/monsters/zombie.png", Action = action,
        Frame = new Int2(96, 80), Directions = 2, Frames = frames, Origin = new Vector2(48, 64),
    };

    private static GameDefs Defs(SpriteDef atlas, SpriteDef? grid = null)
    {
        var set = new SpriteSetDef
        {
            Id = "set_zombie", BodyHeight = 70, Variants = ["", "bluered"],
            Clips = new ClipSetDef { Walk = new ClipDef { Sprite = DefRef<SpriteDef>.To(atlas) } },
        };
        return new GameDefs
        {
            Sprites = new DefTable<SpriteDef>(grid is null ? [atlas] : [atlas, grid]),
            SpriteSets = new DefTable<SpriteSetDef>([set]),
        };
    }

    private const string Meta = """
        { "format": 3, "scale": 1, "pages": [[256, 128], [64, 64]],
          "actions": { "walk": { "cell": [96, 80], "directions": 2, "frames": 2,
                       "rects": [[0, 0, 0, 40, 70, 28, 10], [0, 40, 0, 42, 70, 27, 10], [0, 82, 0, 0, 0, 0, 0], [1, 0, 0, 50, 64, 20, 8]] } } }
        """;

    private static TempDir Assets(string meta = Meta, int format = AssetFormat.Version)
    {
        var dir = new TempDir();
        dir.Write(AssetFormat.ManifestPath, $$"""{ "format": {{format}}, "table_hash": "", "files": [] }""");
        dir.Write("d1/monsters/zombie.json", meta);
        foreach (var v in new[] { "", "@bluered" })
        {
            WritePng(dir, $"d1/monsters/zombie{v}.png", 256, 128);
            WritePng(dir, $"d1/monsters/zombie_p2{v}.png", 64, 64);
        }
        WritePng(dir, "Gold.png", 96, 1600);
        return dir;
    }

    private static (ArtAssetValidator V, ValidationContext Ctx) Run(string root, GameDefs defs)
    {
        var v = new ArtAssetValidator(root);
        var ctx = new ValidationContext { Defs = defs };
        v.Validate(defs, ctx);
        return (v, ctx);
    }

    private static SpriteDef Gold => new()
    {
        Id = "Gold", Source = SpriteSource.Grid, File = "Gold.png", Sheet = new Int2(96, 1600), Frame = new Int2(96, 160), Frames = 10,
        ColorKey = Rgb.Black, Origin = new Vector2(48, 150),
    };

    [Fact]
    public void NoManifest_OrStaleFormat_Skips()
    {
        using var empty = new TempDir();
        var (v, ctx) = Run(empty.Path, Defs(AtlasSprite()));
        Assert.False(v.Ran);
        Assert.Empty(ctx.Errors);

        using var stale = Assets(format: AssetFormat.Version - 1);
        (v, ctx) = Run(stale.Path, Defs(AtlasSprite()));
        Assert.False(v.Ran);                       // 낡음 = 시작 검사(WP7)·시험 건너뜀이 맡는다
        Assert.Empty(ctx.Errors);
    }

    [Fact]
    public void MatchingAssets_Pass()
    {
        using var dir = Assets();
        var (v, ctx) = Run(dir.Path, Defs(AtlasSprite(), Gold));
        Assert.True(v.Ran);
        Assert.Empty(ctx.Errors);
    }

    [Fact]
    public void Mismatches_AreErrors()
    {
        using var dir = Assets();
        // 정의 프레임 수 ≠ 메타, 변종 페이지 크기 틀림, grid 크기 틀림, 없는 동작
        WritePng(dir, "d1/monsters/zombie_p2@bluered.png", 64, 32);
        var gold = Gold with { Sheet = new Int2(96, 1440) };
        var (_, ctx) = Run(dir.Path, Defs(AtlasSprite(frames: 3), gold));
        Assert.Contains(ctx.Errors, e => e.Message.Contains("프레임 2 ≠ 정의 3"));
        Assert.Contains(ctx.Errors, e => e.File == "d1/monsters/zombie_p2@bluered.png" && e.Message.Contains("PNG 크기"));
        Assert.Contains(ctx.Errors, e => e.File == "Gold.png");
        Assert.DoesNotContain(ctx.Errors, e => e.File == "d1/monsters/zombie_p2.png");   // 원색 페이지는 맞음

        (_, ctx) = Run(dir.Path, Defs(AtlasSprite(action: "attack")));
        Assert.Contains(ctx.Errors, e => e.Message.Contains("동작 'attack'"));

        File.Delete(Path.Combine(dir.Path, "d1", "monsters", "zombie@bluered.png"));
        (_, ctx) = Run(dir.Path, Defs(AtlasSprite()));
        Assert.Contains(ctx.Errors, e => e.File == "d1/monsters/zombie@bluered.png" && e.Message.Contains("없다"));
    }

    [Fact]
    public void UpscaledMeta_CellDividedByScale_AndScaleMustFitArtScale()
    {
        // 메타 scale 2(그림 픽셀 = 월드 px × 2): 칸 192×160 ÷ 2 = 정의 96×80. manifest 가 확대를 안 했으면(s = 1) 그릴 수 없다
        var meta2 = Meta.Replace("\"scale\": 1", "\"scale\": 2").Replace("\"cell\": [96, 80]", "\"cell\": [192, 160]");
        using var dir = Assets(meta2);
        var (_, ctx) = Run(dir.Path, Defs(AtlasSprite()));
        Assert.DoesNotContain(ctx.Errors, e => e.Message.Contains("칸"));
        Assert.Contains(ctx.Errors, e => e.Message.Contains("그림 배율 s = 1"));

        dir.Write(AssetFormat.ManifestPath, $$"""{ "format": {{AssetFormat.Version}}, "table_hash": "", "upscale": { "status": "done", "output_scale": 2 }, "files": [] }""");
        (_, ctx) = Run(dir.Path, Defs(AtlasSprite()));
        Assert.Empty(ctx.Errors);
    }

    [Fact]
    public void VariantsBySprite_UnionOfSetsPlusOriginal()
    {
        var atlas = AtlasSprite();
        var map = ArtAssetValidator.VariantsBySprite(Defs(atlas));
        Assert.Equal(["", "bluered"], map[atlas.Id]);
    }

    [Fact]
    public void RealAssets_MatchDefinitions_WhenPresent()
    {
        // 1.3: DDR_ASSETS 또는 저장소 Assets. manifest 가 없으면 건너뜀, 판이 다르면 "낡음" 건너뜀
        var root = Environment.GetEnvironmentVariable("DDR_ASSETS") is { Length: > 0 } env
            ? env : Path.Combine(RealData.RepoRoot(), "src", "DiabloDefenceRemastered", "Assets");
        var manifest = Path.Combine(root, AssetFormat.ManifestPath);
        if (!File.Exists(manifest)) Assert.Skip("자원이 없다(d1/manifest.json) — 13.5-7 실제 쪽 건너뜀");
        if (AssetManifest.Read(manifest).Format != AssetFormat.Version) Assert.Skip("자원이 낡았다 — 가져오기를 다시 돌린다");

        var sprites = new List<SpriteDef>();
        var sets = new List<SpriteSetDef>();
        foreach (var f in new[] { "sprites/monsters.json", "sprites/hero.json" })
        {
            sprites.AddRange(RealData.Kind<SpriteDef>(f, "sprites"));
            sets.AddRange(RealData.Kind<SpriteSetDef>(f, "sprite_sets"));
        }
        // 실제 정의는 로더(WP7) 전이라 클립 참조가 비어 있다 — 그림 ID 로 다시 묶는다
        var byId = sprites.ToDictionary(s => s.Id);
        var linked = sets.Select(set => set with { Clips = Relink(set.Clips, byId) }).ToList();
        var defs = new GameDefs
        {
            Sprites = new DefTable<SpriteDef>(sprites),
            SpriteSets = new DefTable<SpriteSetDef>(linked),
            Maps = new DefTable<MapDef>([RealData.Cathedral]),
        };
        var (v, ctx) = Run(root, defs);
        Assert.True(v.Ran);
        Assert.Empty(ctx.Errors.Select(e => $"{e.File}: {e.Message}"));
    }

    private static ClipSetDef Relink(ClipSetDef clips, IReadOnlyDictionary<string, SpriteDef> byId)
    {
        ClipDef? R(ClipDef? c) => c is null ? null : c with { Sprite = DefRef<SpriteDef>.To(byId[c.Sprite.Id]) };
        return clips with
        {
            Idle = R(clips.Idle), Walk = R(clips.Walk), Attack = R(clips.Attack), Special = R(clips.Special), Die = R(clips.Die),
            CastFire = R(clips.CastFire), CastLightning = R(clips.CastLightning), CastMagic = R(clips.CastMagic),
        };
    }
}
