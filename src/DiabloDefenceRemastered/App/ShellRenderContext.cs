using Microsoft.Xna.Framework.Graphics;
using Point = Microsoft.Xna.Framework.Point;

namespace DiabloDefenceRemastered.App;

/// <summary>
/// 앱 셸만으로 그릴 때의 그리기 맥락(WP7). <c>RenderServices.CreateContext</c>(WP5) 본문이 아직 없을 때 <see cref="BlankScene"/> 를 그리는 데만 쓴다.
/// 카메라가 없으므로 화면 좌표를 그대로 돌려준다.
/// </summary>
public sealed class ShellRenderContext(GraphicsDevice device) : RenderContext(device, new SpriteBatch(device))
{
    public override Vector2 ScreenToWorld(Point screen) => new(screen.X, screen.Y);
}
