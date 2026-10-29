using Microsoft.Xna.Framework.Graphics;
using Point = Microsoft.Xna.Framework.Point;

namespace DiabloDefenceRemastered.App.Contracts;

/// <summary>
/// 씬이 그릴 때 받는 것 (설계서 7·8). 가상 해상도 960×540, 결과는 PresentRT 에 모은다.
/// 화면 → 월드 변환(창 좌표 → 레터박스 → 가상 960×540 → 월드)은 <see cref="ScreenToWorld"/> 하나다 — 구현은 WP5, 사용은 WP6(마우스 이동).
/// </summary>
public abstract class RenderContext(GraphicsDevice device, SpriteBatch batch)
{
    public static readonly Point VirtualSize = new(960, 540);

    public GraphicsDevice Device { get; } = device;
    public SpriteBatch Batch { get; } = batch;

    /// <summary>PresentRT 크기(--window 가 있으면 그 크기로 고정, 7.1).</summary>
    public Point PresentSize { get; set; } = VirtualSize;

    public AppTick AppTick { get; set; }

    /// <summary>캡처 프레임(alpha = 1, 커서 그리지 않음).</summary>
    public bool Capturing { get; set; }

    /// <summary>창 좌표 → 월드 좌표(마지막으로 그린 카메라 기준).</summary>
    public abstract Vector2 ScreenToWorld(Point screen);
}
