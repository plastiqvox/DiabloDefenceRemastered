using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Tests.Assets;

public class ManifestAndSoundIndexTests
{
    private const string ManifestJson = """
        { "format": 1, "tool": "abc123", "runtime": "9.0.10",
          "sources": [ { "name": "diabdat.mpq", "size": 517000000, "sha256": "00ff" } ],
          "table_hash": "AB12",
          "files": [ { "path": "d1/monsters/zombie.png", "size": 1234, "sha256": "11" },
                     { "path": "d1/sound/index.json", "size": 99, "sha256": "22" } ] }
        """;

    [Fact]
    public void Manifest_Parse_AndLookup()
    {
        var m = AssetJson.Deserialize<AssetManifest>(ManifestJson);
        Assert.Equal(AssetFormat.Version, m.Format);
        Assert.Equal(517000000, m.Sources[0].Size);
        Assert.True(m.TryGetFile("d1/monsters/zombie.png", out var f));
        Assert.Equal(1234, f.Size);
        Assert.False(m.TryGetFile("d1/monsters/zombie@grey.png", out _));
    }

    [Fact]
    public void Manifest_Staleness_FormatAndTableHash()
    {
        var m = AssetJson.Deserialize<AssetManifest>(ManifestJson);
        Assert.Empty(m.Staleness("ab12"));                                     // 대소문자 무시
        var stale = (m with { Format = 0 }).Staleness("ffff");
        Assert.Equal(2, stale.Count);
        Assert.Contains(stale, s => s.Contains($"판 0 → {AssetFormat.Version}"));
        Assert.Contains(stale, s => s.Contains("가져오기 표가 바뀌었다"));
    }

    [Fact]
    public void Manifest_Strict_AndDeterministicBytes()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => AssetJson.Deserialize<AssetManifest>(ManifestJson.Replace("\"runtime\"", "\"runtim\"")));
        var m = AssetJson.Deserialize<AssetManifest>(ManifestJson);
        var a = AssetJson.Serialize(m);
        Assert.Equal(a, AssetJson.Serialize(AssetJson.Deserialize<AssetManifest>(System.Text.Encoding.UTF8.GetString(a))));
        var text = System.Text.Encoding.UTF8.GetString(a);
        Assert.DoesNotContain('\r', text);
        Assert.True(text.IndexOf("\"format\"", StringComparison.Ordinal) < text.IndexOf("\"files\"", StringComparison.Ordinal));   // 정해진 키 순서
        Assert.Contains("\"table_hash\"", text);
    }

    [Fact]
    public void Sha256_IsLowerHex()
    {
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", AssetManifest.Sha256Of("abc"u8));
    }

    [Fact]
    public void SoundIndex_DurationTicks_RoundUp_WithPitch()
    {
        var idx = SoundIndex.Parse("""
            { "format": 1, "files": [
                { "path": "d1/sound/music/dlvla.wav", "samples": 44100, "rate": 22050, "channels": 2 },
                { "path": "d1/sound/sfx/misc/cbolt.wav", "samples": 22050, "rate": 22050, "channels": 1 } ] }
            """);
        Assert.Empty(idx.Validate());
        Assert.True(idx.TryGet("d1/sound/sfx/misc/cbolt.wav", out var cbolt));
        Assert.Equal(1.0, cbolt.Seconds);
        Assert.Equal(60, cbolt.DurationTicks());                     // 정확히 1초 = 60틱(올림 경계에서 한 틱 넘지 않음)
        Assert.Equal(58, cbolt.DurationTicks(pitch: 1.05f));         // 57.14 → 58
        Assert.Equal(64, cbolt.DurationTicks(pitch: 0.95f));         // 63.16 → 64
        Assert.Equal(61, (cbolt with { Samples = 22051 }).DurationTicks());
        Assert.False(idx.TryGet("d1/sound/sfx/misc/nope.wav", out _));
    }

    [Fact]
    public void SoundIndex_Validate_OrderAndValues()
    {
        var idx = SoundIndex.Parse("""
            { "format": 1, "files": [
                { "path": "d1/sound/b.wav", "samples": 1, "rate": 22050, "channels": 1 },
                { "path": "d1/sound/a.wav", "samples": 1, "rate": 0, "channels": 1 },
                { "path": "d1\\sound\\c.wav", "samples": 1, "rate": 22050, "channels": 1 } ] }
            """);
        var errors = idx.Validate();
        Assert.Contains(errors, e => e.Contains("files[1]") && e.Contains("서수"));
        Assert.Contains(errors, e => e.Contains("files[1]") && e.Contains("표본"));
        Assert.Contains(errors, e => e.Contains("files[2]") && e.Contains("경로"));
    }

    [Theory]
    [InlineData("sfx\\misc\\CBolt.wav", "d1/sound/sfx/misc/cbolt.wav")]
    [InlineData("music\\dlvlb.wav", "d1/sound/music/dlvlb.wav")]
    public void SoundOutput_LowercaseForwardSlash(string source, string expected)
    {
        Assert.Equal(expected, AssetFormat.SoundOutput(source));
    }

    [Theory]
    [InlineData("d1/monsters/zombie.png", true)]
    [InlineData("TITLE.png", true)]
    [InlineData("d1\\monsters\\zombie.png", false)]
    [InlineData("/d1/x.png", false)]
    [InlineData("d1/../x.png", false)]
    [InlineData("C:/x.png", false)]
    [InlineData("", false)]
    public void RelativePathRule(string path, bool ok) => Assert.Equal(ok, AssetFormat.IsValidRelativePath(path));
}
