namespace DiabloDefenceRemastered.Rendering;

/// <summary>
/// UI 그리기 (설계서 7.3 9~10층, 소유 WP5). 좌표는 가상 960×540 이고 창 배율로 바꿔 그린다. 글꼴은 크기 × 배율로 요청해 늘 선명하다.
/// HUD·메뉴·오버레이(WP6)가 쓴다. Begin/End 사이에서만 그린다.
/// WP1 이 시그니처만 만든 빈 연결 파일 — WP6 이 먼저 부르고 컴파일한다. 본문은 WP5 가 채운다.
/// </summary>
public sealed class UiCanvas
{
    public UiCanvas(AppServices app) => throw new NotImplementedException("WP5 UiCanvas");

    /// <summary>가상 좌표 1 = 창 픽셀 몇 개.</summary>
    public float Scale => throw new NotImplementedException("WP5 UiCanvas.Scale");

    public void Begin(RenderContext rc) => throw new NotImplementedException("WP5 UiCanvas.Begin");
    public void End() => throw new NotImplementedException("WP5 UiCanvas.End");

    /// <summary>가상 좌표 → 창 좌표.</summary>
    public Vector2 ToScreen(Vector2 virtualPos) => throw new NotImplementedException("WP5 UiCanvas.ToScreen");
    /// <summary>창 좌표 → 가상 좌표(버튼 누르기 판정).</summary>
    public Vector2 ToVirtual(Vector2 screenPos) => throw new NotImplementedException("WP5 UiCanvas.ToVirtual");

    /// <summary><paramref name="size"/> = 가상 좌표 기준 글자 크기(px).</summary>
    public void Text(string text, Vector2 pos, float size, Rgb color, float alpha = 1f) => throw new NotImplementedException("WP5 UiCanvas.Text");
    public Vector2 MeasureText(string text, float size) => throw new NotImplementedException("WP5 UiCanvas.MeasureText");

    public void Fill(RectF rect, Rgb color, float alpha = 1f) => throw new NotImplementedException("WP5 UiCanvas.Fill");

    /// <summary>그림 정의 <paramref name="spriteId"/> 의 <paramref name="source"/> 영역을 그린다.</summary>
    public void Sprite(string spriteId, RectI source, RectF dest, Rgb tint, float alpha = 1f) => throw new NotImplementedException("WP5 UiCanvas.Sprite");
}
