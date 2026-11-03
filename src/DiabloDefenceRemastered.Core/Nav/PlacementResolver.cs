namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 자리 고르기의 길 거리 조건(v0.6, 4.10 — 안쪽 방이 생겨 "가깝지만 벽 너머" 자리가 생긴다).
/// </summary>
/// <param name="From">기준(주인공 또는 부른 적).</param>
/// <param name="Detour"><c>rules.spawn.path_detour</c>(1.5). 후보마다 maxLength = |후보 − From| × Detour.</param>
/// <param name="FromHero">기준이 주인공(v0.6.1) — 참이면 <see cref="INavWorld.PathLengthToHero"/>, 아니면 <see cref="INavWorld.PathLength"/>. 부르는 쪽이 정한다.</param>
public readonly record struct PlaceReach(Vector2 From, float Detour, bool FromHero = false);

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

    /// <summary>
    /// 위 <see cref="TryPlace(INavWorld, Vector2, Vector2, float, out Vector2)"/> 의 각 후보에 조건 하나를 더한다(v0.6, 4.10): 길 길이가 유한해야 함.
    /// 아니면 다음 후보(20px 당김), 끝까지 없으면 false(생략). 쓰는 곳: 포위 고리·summon·예고 지점·정예 무리·호위 대형.
    /// </summary>
    public static bool TryPlace(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, PlaceReach reach, out Vector2 placed)
        => throw new NotImplementedException("WP2 PlacementResolver.TryPlace(PlaceReach)");

    /// <summary>분열체 튕김: <see cref="INavWorld.HasClearLine"/> 으로 닿는 곳까지만(기획 8.3 "닿으면 거기서 멈춤").</summary>
    public static Vector2 Throw(INavWorld nav, Vector2 from, Vector2 dir, float dist, float bodyRadius)
        => throw new NotImplementedException("WP2 PlacementResolver.Throw");
}
