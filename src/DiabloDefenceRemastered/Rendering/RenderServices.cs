using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 텍스처·글꼴·커서·렌더러 만들기 (소유 WP5). GameApp(WP7)은 이 파일만 부른다. WP1 이 만든 빈 연결 파일.
/// </summary>
public static class RenderServices
{
    public static IArtServices CreateArt(GraphicsDevice device, AppServices app) => throw new NotImplementedException("WP5 RenderServices.CreateArt");

    public static RenderContext CreateContext(GraphicsDevice device, AppServices app) => throw new NotImplementedException("WP5 RenderServices.CreateContext");
}
