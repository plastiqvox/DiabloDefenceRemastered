using DiabloDefenceRemastered.Core.Nav;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 놓을 자리 (설계서 4.10 규칙 그대로). 정본은 WP2 <c>Nav/PlacementResolver</c> 인데 본문이 아직 없어(NotImplemented) 같은 규칙을 여기 둔다.
/// 합칠 때 본문을 <c>PlacementResolver.TryPlace</c>·<c>Throw</c> 호출로 바꾼다(WP4 구현 기록). 소환·호위·분열·정예 무리·포위 고리가 이것 하나를 부른다.
/// </summary>
public static class EnemyPlacement
{
    /// <summary>당김 간격(px).</summary>
    public const float Step = 20f;

    /// <summary>
    /// wish 가 맵 안이고 clearance ≥ bodyRadius + 4(그리고 <paramref name="reach"/> 가 있으면 길 거리 ≤ 직선 × detour)면 그대로.
    /// 아니면 center 쪽으로 20px 씩 당기며 다시 보고, center 까지 거리의 절반까지 당겨도 없으면 false.
    /// </summary>
    public static bool TryPlace(INavWorld nav, Vector2 center, Vector2 wish, float bodyRadius, PlaceReach? reach, out Vector2 placed)
    {
        var toCenter = center - wish;
        var dist = toCenter.Length();
        var dir = dist > 1e-4f ? toCenter / dist : Vector2.Zero;
        var maxPull = dist * 0.5f;
        for (var pull = 0f; pull <= maxPull + 1e-3f; pull += Step)
        {
            var p = wish + dir * pull;
            if (Ok(nav, p, bodyRadius, reach))
            {
                placed = p;
                return true;
            }
            if (dist <= 1e-4f) break;
        }
        placed = wish;
        return false;
    }

    private static bool Ok(INavWorld nav, Vector2 p, float bodyRadius, PlaceReach? reach)
    {
        if (!nav.IsInsideMap(p) || nav.Clearance(p) < bodyRadius + 4f) return false;
        if (reach is not { } r) return true;
        var max = Vector2.Distance(p, r.From) * r.Detour + 1f;
        var len = r.FromHero ? nav.PathLengthToHero(p, bodyRadius, max) : nav.PathLength(p, r.From, bodyRadius, max);
        return float.IsFinite(len);
    }

    /// <summary>분열체 튕김: 길이 트인 데까지만(8px 씩 줄임). 하나도 안 되면 from.</summary>
    public static Vector2 Throw(INavWorld nav, Vector2 from, Vector2 dir, float dist, float bodyRadius)
    {
        for (var d = dist; d > 0f; d -= 8f)
        {
            var p = from + dir * d;
            if (nav.IsInsideMap(p) && nav.HasClearLine(from, p, bodyRadius)) return p;
        }
        return from;
    }
}
