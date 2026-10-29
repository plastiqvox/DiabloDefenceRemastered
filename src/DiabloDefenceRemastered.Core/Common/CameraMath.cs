namespace DiabloDefenceRemastered.Core.Common;

/// <summary>
/// 카메라 시야. 시뮬레이션의 <c>World.ViewRect</c>(생성 띠)와 렌더 카메라가 이 함수 하나를 쓴다 (설계서 7.2).
/// 따라가기 지연 없이 주인공 중심, 맵 경계에서 자른다. 맵이 시야보다 작으면 가운데 둔다.
/// </summary>
public static class CameraMath
{
    public static readonly Vector2 DefaultViewSize = new(960, 540);

    public static RectF ViewRect(Vector2 heroPos, Vector2 mapSize, Vector2 viewSize)
    {
        var x0 = Axis(heroPos.X, mapSize.X, viewSize.X);
        var y0 = Axis(heroPos.Y, mapSize.Y, viewSize.Y);
        return new RectF(x0, y0, x0 + viewSize.X, y0 + viewSize.Y);
    }

    private static float Axis(float hero, float map, float view)
    {
        if (map <= view) return (map - view) * 0.5f;
        return Math.Clamp(hero - view * 0.5f, 0f, map - view);
    }
}
