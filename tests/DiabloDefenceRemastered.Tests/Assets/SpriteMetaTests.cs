using DiabloDefenceRemastered.Core.Assets;

namespace DiabloDefenceRemastered.Tests.Assets;

public class SpriteMetaTests
{
    // 설계서 6.3 모양: 페이지 둘, 동작 둘(walk 2방향 × 2프레임, die 1 × 1)
    private const string Json = """
        { "format": 2, "pages": [[256, 128], [64, 64]],
          "actions": {
            "walk": { "cell": [96, 80], "directions": 2, "frames": 2,
                      "rects": [[0, 0, 0, 40, 70, 28, 10], [0, 40, 0, 42, 70, 27, 10], [0, 82, 0, 0, 0, 0, 0], [0, 82, 0, 50, 72, 20, 8]] },
            "die":  { "cell": [96, 80], "directions": 1, "frames": 1, "rects": [[1, 0, 0, 60, 30, 18, 50]] } } }
        """;

    [Fact]
    public void Parse_Valid_AndRectLookupIsOneBased()
    {
        var meta = SpriteMeta.Parse(Json);
        Assert.Empty(meta.Validate());
        Assert.Equal(2, meta.Pages.Count);
        Assert.True(meta.TryGetAction("walk", out var walk));
        Assert.Equal(new Int2(96, 80), walk.Cell);
        Assert.Equal(new SpriteRect(0, 40, 0, 42, 70, 27, 10), walk.Rect(0, 2));   // 방향 0, 프레임 2(1부터)
        Assert.True(walk.Rect(1, 1).IsEmpty);                                        // 빈 프레임
        Assert.Equal(new SpriteRect(0, 82, 0, 50, 72, 20, 8), walk.Rect(1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => walk.Rect(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => walk.Rect(2, 1));
        Assert.False(meta.TryGetAction(null, out _));                                 // 동작이 둘이면 이름이 있어야
        Assert.False(meta.TryGetAction("attack", out _));
    }

    [Fact]
    public void SingleAction_NameMayBeOmitted()
    {
        var meta = SpriteMeta.Parse("""
            { "format": 2, "pages": [[96, 96]], "actions": { "fireba": { "cell": [96, 96], "directions": 1, "frames": 1, "rects": [[0, 0, 0, 96, 96, 0, 0]] } } }
            """);
        Assert.True(meta.TryGetAction(null, out var a));
        Assert.Equal(1, a.Frames);
    }

    [Fact]
    public void Validate_RectOutsidePage_WrongCount_MissingPage_CropOutsideCell()
    {
        var bad = Json
            .Replace("[0, 40, 0, 42, 70, 27, 10]", "[0, 240, 0, 42, 70, 27, 10]")     // 페이지 밖(240 + 42 > 256)
            .Replace("[1, 0, 0, 60, 30, 18, 50]", "[2, 0, 0, 60, 30, 18, 50]")       // 없는 페이지
            .Replace("[0, 82, 0, 50, 72, 20, 8]", "[0, 82, 0, 50, 72, 60, 8]")       // 칸 밖(60 + 50 > 96)
            .Replace("\"format\": 2", "\"format\": 1");
        var errors = SpriteMeta.Parse(bad).Validate();
        Assert.Contains(errors, e => e.Contains("walk rects[1]") && e.Contains("페이지 0"));
        Assert.Contains(errors, e => e.Contains("die rects[0]") && e.Contains("없는 페이지"));
        Assert.Contains(errors, e => e.Contains("walk rects[3]") && e.Contains("칸"));
        Assert.Contains(errors, e => e.Contains("형식"));

        var count = SpriteMeta.Parse(Json.Replace("\"directions\": 2, \"frames\": 2", "\"directions\": 2, \"frames\": 3")).Validate();
        Assert.Contains(count, e => e.Contains("사각형 4개 ≠ 방향 2 × 프레임 3"));
    }

    [Fact]
    public void Strict_UnknownPropertyAndShortRect_Throw()
    {
        Assert.Throws<System.Text.Json.JsonException>(() => SpriteMeta.Parse(Json.Replace("\"cell\"", "\"cel\"")));
        Assert.Throws<System.Text.Json.JsonException>(() => SpriteMeta.Parse(Json.Replace("[1, 0, 0, 60, 30, 18, 50]", "[1, 0, 0, 60, 30, 18]")));
    }

    [Fact]
    public void Serialize_IsDeterministic_AndRoundTrips()
    {
        var meta = SpriteMeta.Parse(Json);
        var a = AssetJson.Serialize(meta);
        var b = AssetJson.Serialize(SpriteMeta.Parse(System.Text.Encoding.UTF8.GetString(a)));
        Assert.Equal(a, b);
        var text = System.Text.Encoding.UTF8.GetString(a);
        Assert.DoesNotContain('\r', text);
        Assert.Contains("[0,40,0,42,70,27,10]", text);   // 사각형은 한 줄 배열
        Assert.EndsWith("}\n", text);
    }

    [Fact]
    public void Serialize_DictionaryInsertionOrder_DoesNotMatter()
    {
        // 병렬 굽기 결과를 순서 없이 넣어도 같은 바이트(사전 키 서수 순, 1.4 결정론)
        var meta = SpriteMeta.Parse(Json);
        var forward = new Dictionary<string, SpriteMetaAction> { ["die"] = meta.Actions["die"], ["walk"] = meta.Actions["walk"] };
        var backward = new Dictionary<string, SpriteMetaAction> { ["walk"] = meta.Actions["walk"], ["die"] = meta.Actions["die"] };
        var a = AssetJson.Serialize(meta with { Actions = forward });
        var b = AssetJson.Serialize(meta with { Actions = backward });
        Assert.Equal(a, b);
        var text = System.Text.Encoding.UTF8.GetString(a);
        Assert.True(text.IndexOf("\"die\"", StringComparison.Ordinal) < text.IndexOf("\"walk\"", StringComparison.Ordinal));
        Assert.Equal(a, AssetJson.Serialize(SpriteMeta.Parse(text)));
    }

    [Theory]
    [InlineData("d1/monsters/zombie.png", 0, "", "d1/monsters/zombie.png")]
    [InlineData("d1/monsters/zombie.png", 0, "bluered", "d1/monsters/zombie@bluered.png")]
    [InlineData("d1/monsters/zombie.png", 1, "", "d1/monsters/zombie_p2.png")]
    [InlineData("d1/monsters/zombie.png", 1, "bluered", "d1/monsters/zombie_p2@bluered.png")]
    public void PageFile_Names(string baseFile, int page, string variant, string expected)
    {
        Assert.Equal(expected, SpriteMeta.PageFile(baseFile, page, variant));
    }

    [Fact]
    public void MetaFile_Name() => Assert.Equal("d1/monsters/zombie.json", SpriteMeta.MetaFile("d1/monsters/zombie.png"));
}
