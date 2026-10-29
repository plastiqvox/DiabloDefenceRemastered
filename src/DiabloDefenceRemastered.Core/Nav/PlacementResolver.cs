namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 놓을 자리 찾기 (설계서 4.10, 소유 WP2). 소환·호위·분열 튕김·정예 무리·포위 고리가 같은 규칙을 쓴다.
/// WP1 이 시그니처만 만든 빈 연결 파일 — WP4 가 먼저 부르고 컴파일한다. 본문은 WP2 가 채운다.
/// </summary>
public static class PlacementResolver
{
    /// <summary>
    /// wish 가 맵 안이고 clearance ≥ bodyRadius + 4 면 그대로. 아니면 center 쪽으로 20px 씩 당기며 다시 보고,
    /// center 까지 거리의 절반까지 당겨도 없으면 false(그 개체는 생략. 생략 수는 결정적).
    /// </summary>
    public static bool TryPlace(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, out Vector2 placed)
        => throw new NotImplementedException("WP2 PlacementResolver.TryPlace");

    /// <summary>분열체 튕김: <see cref="INavWorld.HasClearLine"/> 으로 닿는 곳까지만(기획 8.3 "닿으면 거기서 멈춤").</summary>
    public static Vector2 Throw(INavWorld nav, Vector2 from, Vector2 dir, float dist, float bodyRadius)
        => throw new NotImplementedException("WP2 PlacementResolver.Throw");
}
