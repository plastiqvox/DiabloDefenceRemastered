namespace DiabloAssetImporter.Tests;

/// <summary>WP1b 빈 프로젝트의 연결 확인. 도구 시험(Formats·Bake·Pipeline·RealMpq)은 WP0 이 채운다.</summary>
public class ToolSmokeTests
{
    [Fact]
    public void Tool_IsReferenced_AndSharesCore()
    {
        Assert.Equal("DiabloAssetImporter", typeof(Program).Assembly.GetName().Name);
        // 2: v0.6(WP1d) manifest 모양 바뀜 — bundle·sources·skipped_bundles. 3: v0.8(WP1f) upscale·파일 scale·메타 형식 3·arena.json 형식 2
        Assert.Equal(3, DiabloDefenceRemastered.Core.Assets.AssetFormat.Version);
    }
}
