namespace DiabloAssetImporter.Tests;

/// <summary>WP1b 빈 프로젝트의 연결 확인. 도구 시험(Formats·Bake·Pipeline·RealMpq)은 WP0 이 채운다.</summary>
public class ToolSmokeTests
{
    [Fact]
    public void Tool_IsReferenced_AndSharesCore()
    {
        Assert.Equal("DiabloAssetImporter", typeof(Program).Assembly.GetName().Name);
        Assert.Equal(1, DiabloDefenceRemastered.Core.Assets.AssetFormat.Version);
    }
}
