using System.Text.Json;
using DiabloDefenceRemastered.Core.Data.Json;

namespace DiabloDefenceRemastered.Tests.Common;

public class DefJsonTests
{
    private sealed record Shapes
    {
        public Vector2 P { get; init; }
        public Int2 Size { get; init; }
        public TileCoord T { get; init; }
        public Rgb C { get; init; }
        public RectI R { get; init; }
        public Rgb? Maybe { get; init; }
    }

    [Fact]
    public void CommonConverters_Read_AndRoundTrip()
    {
        const string json = """
            // 주석 허용
            { "p": [1.5, -2], "size": [96, 160], "t": [19.5, -4], "c": [255, 120, 40], "r": [0, 0, 64, 32], "maybe": null, }
            """;
        var s = JsonSerializer.Deserialize<Shapes>(json, DefJson.Options)!;
        Assert.Equal(new Vector2(1.5f, -2), s.P);
        Assert.Equal(new Int2(96, 160), s.Size);
        Assert.Equal(new TileCoord(19.5f, -4), s.T);
        Assert.Equal(new Rgb(255, 120, 40), s.C);
        Assert.Equal(new RectI(0, 0, 64, 32), s.R);
        Assert.Null(s.Maybe);

        var back = JsonSerializer.Deserialize<Shapes>(JsonSerializer.Serialize(s, DefJson.Options), DefJson.Options)!;
        Assert.Equal(s, back);
    }

    [Theory]
    [InlineData("""{ "p": [1] }""")]
    [InlineData("""{ "p": [1, 2, 3] }""")]
    [InlineData("""{ "size": [1.5, 2] }""")]
    [InlineData("""{ "c": [256, 0, 0] }""")]
    [InlineData("""{ "r": [10, 0, 0, 5] }""")]
    [InlineData("""{ "t": "19.5" }""")]
    [InlineData("""{ "pp": [1, 2] }""")]
    [InlineData("""{ "P": [1, 2] }""")]
    public void Strict_BadShapes_Throw(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Shapes>(json, DefJson.Options));
    }

    [Fact]
    public void TileOrAnchor_StringOrTile()
    {
        var a = JsonSerializer.Deserialize<PointLightDef>("""{ "id": "l1", "at": "center", "radius": 100 }""", DefJson.Options)!;
        Assert.True(a.AtTile.IsAnchor);
        Assert.Equal("center", a.AtTile.Anchor);
        var b = JsonSerializer.Deserialize<PointLightDef>("""{ "id": "l2", "at": [4, 19.5] }""", DefJson.Options)!;
        Assert.False(b.AtTile.IsAnchor);
        Assert.Equal(new TileCoord(4, 19.5f), b.AtTile.Tile);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PointLightDef>("""{ "id": "l3" }""", DefJson.Options));   // at 필수
    }

    [Fact]
    public void Lighting_DocShape_Deserializes()
    {
        // 설계서 6.12 의 lighting 블록 + 점 빛 하나
        var l = JsonSerializer.Deserialize<LightingDef>("""
            { "ambient": [200, 200, 215], "darkness": 0.5,
              "hero_light": { "color": [255, 240, 220], "radius": 420 },
              "unit_min_light": 0.6,
              "lights": [ { "id": "brazier", "at": [10, 12], "color": [255, 170, 80], "radius": 140, "flicker": 0.08,
                            "pulse": { "period": 2, "amount": 0.15 } } ] }
            """, DefJson.Options)!;
        Assert.Equal(0.6f, l.UnitMinLight);
        Assert.Equal(420f, l.HeroLight.Radius);
        Assert.Equal(new TileCoord(10, 12), l.Lights[0].AtTile.Tile);
        Assert.Equal(2f, l.Lights[0].Pulse!.Period);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LightingDef>("""{ "kind_lights": {} }""", DefJson.Options));   // v0.2 키는 없다
    }

    [Fact]
    public void WallFade_DefaultLight_AndHeroMode()
    {
        Assert.Equal(WallFadeMode.Light, new WallFadeDef().Mode);
        var w = JsonSerializer.Deserialize<WallFadeDef>("""{ "mode": "hero", "radius": 300 }""", DefJson.Options)!;
        Assert.Equal(WallFadeMode.Hero, w.Mode);
        Assert.Equal(300f, w.Radius);
    }

    [Fact]
    public void JoinPath_ElementPlusInner()
    {
        Assert.Equal("$.maps[0].layout.gates[1]", DefJson.JoinPath("$.maps[0]", "$.layout.gates[1]"));
        Assert.Equal("$.maps[0]", DefJson.JoinPath("$.maps[0]", "$"));
        Assert.Equal("$.maps[0]", DefJson.JoinPath("$.maps[0]", null));
    }

    [Fact]
    public void FindDuplicateKey_FirstDuplicatePath()
    {
        Assert.Null(DefJson.FindDuplicateKey("""{ "a": 1, "b": { "a": 2 } } // 주석"""));
        Assert.Equal("$.a", DefJson.FindDuplicateKey("""{ "a": 1, "a": 2 }"""));
        Assert.Equal("$.list[1].props.1", DefJson.FindDuplicateKey("""{ "list": [ {}, { "props": { "1": "x", "1": "y" } } ] }"""));
        Assert.ThrowsAny<JsonException>(() => DefJson.FindDuplicateKey("{ \"a\": "));
        Assert.Equal("$.x", DefJson.FindDuplicateKey(DefJson.ParseNode("""{ "o": { "x": 1, "x": 2 } }""")!["o"]));
    }

    [Fact]
    public void Core_DoesNotReference_MonoGameOrWar3Net()
    {
        // 1.2·1.3: Core 는 게임·도구 라이브러리를 모른다(WP0 이 도구에 War3Net 을 넣다가 Core 를 고치는 실수 막기)
        var refs = typeof(World).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToList();
        foreach (var banned in new[] { "MonoGame", "War3Net", "FontStashSharp" })
            Assert.DoesNotContain(refs, n => n.Contains(banned, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateOptions_ExtraConverterWins_AndSharedOptionsAreReadOnly()
    {
        var o = DefJson.CreateOptions(new FixedVectorConverter());
        Assert.Equal(new Vector2(7, 7), JsonSerializer.Deserialize<Vector2>("[1, 2]", o));
        _ = JsonSerializer.Deserialize<Vector2>("[1, 2]", DefJson.Options);
        Assert.True(DefJson.Options.IsReadOnly);
    }

    private sealed class FixedVectorConverter : System.Text.Json.Serialization.JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return new Vector2(7, 7);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options) => writer.WriteNullValue();
    }
}
