namespace DiabloDefenceRemastered.Core.Geometry;

/// <summary>
/// 평면 기하 도우미 (설계서 4.2·4.5·4.7, 소유 WP2). 충돌 도형은 볼록 다각형(평행사변형)이지만 함수는 오목 다각형도 받는다(변 단위).
/// 꼭짓점 순서는 상관없다(안/밖은 짝홀 규칙). 모두 할당 없이 돈다.
/// </summary>
public static class Geo
{
    /// <summary>점 p 에서 선분 ab 의 가장 가까운 점.</summary>
    public static Vector2 ClosestOnSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var len2 = ab.LengthSquared();
        var t = len2 > 1e-12f ? Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f) : 0f;
        return a + ab * t;
    }

    /// <summary>점이 다각형 안인가(짝홀 규칙 — 오목 다각형도 됨). 변 위는 어느 쪽이든 될 수 있다.</summary>
    public static bool Contains(ReadOnlySpan<Vector2> poly, Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            var a = poly[i];
            var b = poly[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    /// <summary>점에서 다각형 둘레의 가장 가까운 점과 그 거리(변마다 최근접점 — 오목 다각형도 됨).</summary>
    public static float ClosestOnBoundary(ReadOnlySpan<Vector2> poly, Vector2 p, out Vector2 closest)
    {
        var best = float.MaxValue;
        closest = p;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            var q = ClosestOnSegment(p, poly[j], poly[i]);
            var d = Vector2.DistanceSquared(p, q);
            if (d < best)
            {
                best = d;
                closest = q;
            }
        }
        return MathF.Sqrt(best);
    }

    /// <summary>다각형까지 부호 있는 거리(밖 +, 안 −).</summary>
    public static float SignedDistance(ReadOnlySpan<Vector2> poly, Vector2 p)
    {
        var d = ClosestOnBoundary(poly, p, out _);
        return Contains(poly, p) ? -d : d;
    }

    /// <summary>
    /// 원(중심 c, 반경 r)을 다각형 밖으로 밀어낼 벡터. 겹치지 않으면 false.
    /// 중심이 둘레 위에 정확히 있어 방향이 없으면(거리 &lt; 1e-4) <paramref name="fallbackDir"/> 쪽으로 민다(4.7-3 — 부르는 쪽이 clearance 기울기·TieAngle 로 정함).
    /// </summary>
    public static bool PushOut(ReadOnlySpan<Vector2> poly, Vector2 c, float r, Vector2 fallbackDir, out Vector2 push)
    {
        var d = ClosestOnBoundary(poly, c, out var q);
        var inside = Contains(poly, c);
        if (!inside && d >= r)
        {
            push = Vector2.Zero;
            return false;
        }
        if (d < 1e-4f)
        {
            push = fallbackDir * r;
            return true;
        }
        var dir = (c - q) / d;
        // 안이면 둘레 점 쪽(바깥)으로 d + r, 밖이면 r − d
        push = inside ? -dir * (d + r) : dir * (r - d);
        return true;
    }

    /// <summary>두 선분이 만나는가(끝점·겹침 포함).</summary>
    public static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        var d1 = Cross(q2 - q1, p1 - q1);
        var d2 = Cross(q2 - q1, p2 - q1);
        var d3 = Cross(p2 - p1, q1 - p1);
        var d4 = Cross(p2 - p1, q2 - p1);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return true;
        return (d1 == 0 && OnSegment(q1, q2, p1)) || (d2 == 0 && OnSegment(q1, q2, p2))
            || (d3 == 0 && OnSegment(p1, p2, q1)) || (d4 == 0 && OnSegment(p1, p2, q2));
    }

    /// <summary>선분 ab 가 다각형(둘레 또는 안)에 닿는가.</summary>
    public static bool SegmentHitsPolygon(ReadOnlySpan<Vector2> poly, Vector2 a, Vector2 b)
    {
        if (Contains(poly, a) || Contains(poly, b)) return true;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            if (SegmentsIntersect(a, b, poly[j], poly[i])) return true;
        return false;
    }

    /// <summary>두 선분 사이 거리.</summary>
    public static float SegmentDistance(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        if (SegmentsIntersect(p1, p2, q1, q2)) return 0f;
        var d = MathF.Min(
            MathF.Min(Vector2.Distance(p1, ClosestOnSegment(p1, q1, q2)), Vector2.Distance(p2, ClosestOnSegment(p2, q1, q2))),
            MathF.Min(Vector2.Distance(q1, ClosestOnSegment(q1, p1, p2)), Vector2.Distance(q2, ClosestOnSegment(q2, p1, p2))));
        return d;
    }

    /// <summary>두 다각형 사이 거리(겹치거나 닿으면 0).</summary>
    public static float PolygonDistance(ReadOnlySpan<Vector2> a, ReadOnlySpan<Vector2> b)
    {
        if (Contains(a, b[0]) || Contains(b, a[0])) return 0f;
        var best = float.MaxValue;
        for (int i = 0, j = a.Length - 1; i < a.Length; j = i++)
            for (int k = 0, l = b.Length - 1; k < b.Length; l = k++)
                best = MathF.Min(best, SegmentDistance(a[j], a[i], b[l], b[k]));
        return best;
    }

    /// <summary>원과 원 사이 부호 있는 거리(겹치면 음수).</summary>
    public static float CircleDistance(Vector2 a, float ra, Vector2 b, float rb) => Vector2.Distance(a, b) - ra - rb;

    /// <summary>축 정렬 사각형(최소·최대)까지 거리(안이면 0).</summary>
    public static float RectDistance(Vector2 min, Vector2 max, Vector2 p)
    {
        var dx = MathF.Max(MathF.Max(min.X - p.X, 0f), p.X - max.X);
        var dy = MathF.Max(MathF.Max(min.Y - p.Y, 0f), p.Y - max.Y);
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>2차원 외적(z 성분).</summary>
    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static bool OnSegment(Vector2 a, Vector2 b, Vector2 p) =>
        p.X >= MathF.Min(a.X, b.X) && p.X <= MathF.Max(a.X, b.X) && p.Y >= MathF.Min(a.Y, b.Y) && p.Y <= MathF.Max(a.Y, b.Y);
}
