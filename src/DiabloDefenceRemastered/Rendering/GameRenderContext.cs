using DiabloDefenceRemastered.Core.Art;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// 그리기 맥락 구현 (설계서 7.1·7.2, 계약 <see cref="RenderContext"/>, 소유 WP5). 결과는 <see cref="Present"/>(<c>PresentRT</c>)에 모으고 백버퍼에 한 번 늘여 복사한다.
/// 프레임 순서(GameApp — WP7): <see cref="BeginFrame"/> → 씬 스택 Draw → <see cref="EndFrame"/>. 캡처는 <see cref="Present"/> 를 저장한다.
/// <see cref="ScreenToWorld"/> 는 마지막으로 그린 카메라(<see cref="SetCamera"/> — <c>WorldRenderer</c> 가 넣음, 흔들림 제외) 기준.
/// </summary>
public sealed class GameRenderContext(GraphicsDevice device, SpriteBatch batch, Primitives primitives) : RenderContext(device, batch), IDisposable
{
    private Vector2 _camera;

    public Primitives Primitives { get; } = primitives;

    /// <summary><c>PresentRT</c>. <see cref="BeginFrame"/> 가 <see cref="RenderContext.PresentSize"/> 에 맞춰 만든다.</summary>
    public RenderTarget2D? Present { get; private set; }

    /// <summary>마지막으로 그린 월드 카메라 왼쪽 위(월드 px).</summary>
    public Vector2 Camera => _camera;

    /// <summary><c>WorldRenderer</c> 가 그린 뒤 넣는다.</summary>
    public void SetCamera(Vector2 topLeft) => _camera = topLeft;

    /// <summary>창 좌표 → 월드(창 좌표 → 레터박스 → 가상 960×540 → 월드). <see cref="RenderContext.PresentSize"/> 가 창 크기와 다르면 창 크기 비율로 먼저 옮긴다.</summary>
    public override Vector2 ScreenToWorld(Point screen)
    {
        var present = new Int2(PresentSize.X, PresentSize.Y);
        var back = Device.PresentationParameters;
        var p = new Vector2(screen.X, screen.Y);
        if (back.BackBufferWidth > 0 && back.BackBufferHeight > 0 && (back.BackBufferWidth != present.X || back.BackBufferHeight != present.Y))
            p = new Vector2(p.X * present.X / back.BackBufferWidth, p.Y * present.Y / back.BackBufferHeight);
        return PresentMath.ScreenToWorld(p, present, _camera);
    }

    /// <summary><c>PresentRT</c> 를 대상으로 잡고 검정으로 지운다.</summary>
    public void BeginFrame()
    {
        if (Present is null || Present.Width != PresentSize.X || Present.Height != PresentSize.Y)
        {
            Present?.Dispose();
            Present = new RenderTarget2D(Device, Math.Max(1, PresentSize.X), Math.Max(1, PresentSize.Y), false, SurfaceFormat.Color,
                                         DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
        Device.SetRenderTarget(Present);
        Device.Clear(Color.Black);
    }

    /// <summary>씬이 자기 타깃을 쓴 뒤 <c>PresentRT</c> 로 되돌린다.</summary>
    public void RestorePresent() => Device.SetRenderTarget(Present);

    /// <summary><c>PresentRT</c> 를 백버퍼에 늘여 복사(선형).</summary>
    public void EndFrame()
    {
        Device.SetRenderTarget(null);
        Device.Clear(Color.Black);
        if (Present is null) return;
        var back = Device.PresentationParameters;
        Batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
        Batch.Draw(Present, new Rectangle(0, 0, back.BackBufferWidth, back.BackBufferHeight), Color.White);
        Batch.End();
    }

    /// <summary>레터박스 사각형(<c>PresentRT</c> 픽셀).</summary>
    public Rectangle Letterbox
    {
        get
        {
            var r = PresentMath.Letterbox(new Int2(PresentSize.X, PresentSize.Y));
            return new Rectangle(r.X0, r.Y0, r.Width, r.Height);
        }
    }

    public void Dispose()
    {
        Present?.Dispose();
        Primitives.Dispose();
        Batch.Dispose();
    }
}
