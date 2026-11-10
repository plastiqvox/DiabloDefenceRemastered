using DiabloDefenceRemastered.Loading;
using Microsoft.Xna.Framework.Graphics;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 텍스처·글꼴·커서·렌더러 만들기 (소유 WP5). GameApp(WP7)은 이 파일만 부른다.
/// 그래픽 장치가 생긴 뒤: <c>app.Art = CreateArt(device, app)</c>, <c>app.Render = CreateContext(device, app)</c>.
/// 프레임마다 GameApp 이 <see cref="GameRenderContext.BeginFrame"/> → 씬 스택 Draw → <see cref="GameRenderContext.EndFrame"/> 를 부른다.
/// </summary>
public static class RenderServices
{
    public static IArtServices CreateArt(GraphicsDevice device, AppServices app) => new ArtServices(device, app);

    public static RenderContext CreateContext(GraphicsDevice device, AppServices app)
    {
        var rc = new GameRenderContext(device, new SpriteBatch(device), new Primitives(device));
        if (app.Options.Window is { } window) rc.PresentSize = window;
        return rc;
    }
}
