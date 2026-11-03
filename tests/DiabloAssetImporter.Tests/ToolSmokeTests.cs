namespace DiabloAssetImporter.Tests;

/// <summary>WP1b 빈 프로젝트의 연결 확인. 도구 시험(Formats·Bake·Pipeline·RealMpq)은 WP0 이 채운다.</summary>
public class ToolSmokeTests
{
    [Fact]
    public void Tool_IsReferenced_AndSharesCore()
    {
        Assert.Equal("DiabloAssetImporter", typeof(Program).Assembly.GetName().Name);
        // 2: v0.6(WP1d) manifest 모양 바뀜 — bundle·sources·skipped_bundles
        Assert.Equal(2, DiabloDefenceRemastered.Core.Assets.AssetFormat.Version);
    }
}
