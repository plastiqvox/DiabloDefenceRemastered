using Microsoft.Xna.Framework;

namespace DiabloDefenceRemastered;

/// <summary>
/// 합성 뿌리 (소유 WP7, 설계서 2.1). GameModules·Scenes/SceneFactory·Rendering/RenderServices 만 부른다. WP1 이 만든 최소 본문.
/// </summary>
public sealed class GameApp : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public GameApp()
    {
        _graphics = new GraphicsDeviceManager(this);
    }
}
