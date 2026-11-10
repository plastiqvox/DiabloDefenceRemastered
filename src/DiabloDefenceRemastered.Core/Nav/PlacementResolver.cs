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
/// 놓을 자리 찾기 (설계서 4.10, 소유 WP2). 소환·호위·분열 튕김·정예 무리·포위 고리가 같은 규칙을 쓴다.
/// 후보 = wish 에서 center 쪽으로 20px 씩 당긴 자리(당긴 거리 ≤ |wish − center| / 2). 순서·간격이 고정이라 생략 수가 결정적이다.
/// </summary>
public static class PlacementResolver
{
    /// <summary>당김 간격(px)·자리 여유(px)·튕김 걸음(px, 길 트임 간격과 같음).</summary>
    public const float PullStep = 20f, Margin = 4f, ThrowStep = 4f;

    /// <summary>
    /// wish 가 맵 안이고 clearance ≥ bodyRadius + 4 면 그대로. 아니면 center 쪽으로 20px 씩 당기며 다시 보고,
    /// center 까지 거리의 절반까지 당겨도 없으면 false(그 개체는 생략. 생략 수는 결정적).
    /// </summary>
    public static bool TryPlace(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, out Vector2 placed)
        => Place(nav, center, wish, bodyRadius, null, out placed);

    /// <summary>
    /// 위 <see cref="TryPlace(INavWorld, Vector2, Vector2, float, out Vector2)"/> 의 각 후보에 조건 하나를 더한다(v0.6, 4.10): 길 길이가 유한해야 함.
    /// 아니면 다음 후보(20px 당김), 끝까지 없으면 false(생략). 쓰는 곳: 포위 고리·summon·예고 지점·정예 무리·호위 대형.
    /// </summary>
    public static bool TryPlace(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, PlaceReach reach, out Vector2 placed)
        => Place(nav, center, wish, bodyRadius, reach, out placed);

    /// <summary>분열체 튕김: <see cref="INavWorld.HasClearLine"/> 으로 닿는 곳까지만(기획 8.3 "닿으면 거기서 멈춤"). 처음부터 막혔으면 제자리.</summary>
    public static Vector2 Throw(INavWorld nav, Vector2 from, Vector2 dir, float dist, float bodyRadius)
    {
        var d = MathX.NormalizeOr(dir, Vector2.Zero);
        if (d == Vector2.Zero || dist <= 0f) return from;
        var steps = (int)MathF.Ceiling(dist / ThrowStep);
        var last = from;
        for (var i = 1; i <= steps; i++)
        {
            var p = from + d * MathF.Min(i * ThrowStep, dist);
            if (nav.Clearance(p) < bodyRadius) break;
            last = p;
        }
        return last;
    }

    private static bool Place(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, PlaceReach? reach, out Vector2 placed)
    {
        var toCenter = center - wish;
        var total = toCenter.Length();
        var dir = total > 1e-6f ? toCenter / total : Vector2.Zero;
        var limit = total * 0.5f;
        for (var k = 0; k * PullStep <= limit + 1e-3f; k++)
        {
            var c = wish + dir * (k * PullStep);
            if (Fits(nav, c, bodyRadius, reach))
            {
                placed = c;
                return true;
            }
            if (dir == Vector2.Zero) break;
        }
        placed = wish;
        return false;
    }

    private static bool Fits(INavWorld nav, Vector2 c, float bodyRadius, PlaceReach? reach)
    {
        if (!nav.IsInsideMap(c) || nav.Clearance(c) < bodyRadius + Margin) return false;
        if (reach is not { } r) return true;
        // 길 길이 ≤ |후보 − 기준| × Detour (FromHero 면 필드 거리, 아니면 자른 A*)
        var max = Vector2.Distance(c, r.From) * r.Detour;
        var len = r.FromHero ? nav.PathLengthToHero(c, bodyRadius, max) : nav.PathLength(c, r.From, bodyRadius, max);
        return float.IsFinite(len);
    }
}
