namespace DiabloDefenceRemastered.Core.Common;

public static class MathX
{
    public const float DegToRad = MathF.PI / 180f;
    public const float RadToDeg = 180f / MathF.PI;

    /// <summary>
    /// 겹친 두 점(d &lt; 1e-4)을 벌릴 방향 각도(라디안). 두 ID 의 해시로 정하고, 상대 쪽은 정반대(+π)다 (설계서 4.6).
    /// 같은 입구로 겹쳐 들어온 무리·분열체·겹친 소환이 이 값으로 결정적으로 벌어진다.
    /// </summary>
    public static float TieAngle(EntityId a, EntityId b) => TieAngle(a.Key, b.Key);

    /// <summary>개체와 도형 번호처럼 ID 가 아닌 짝(원 도형 중심에 놓인 유닛, 설계서 4.7).</summary>
    public static float TieAngle(EntityId a, int shapeIndex) => TieAngle(a.Key, 0x80000000u | (uint)shapeIndex);

    public static float TieAngle(uint a, uint b)
    {
        if (a == b) return StableHash.ToUnit(StableHash.Of(a)) * MathF.Tau;
        var lo = Math.Min(a, b);
        var hi = Math.Max(a, b);
        var angle = StableHash.ToUnit(StableHash.Of(lo, hi)) * MathF.Tau;
        return a == lo ? angle : WrapAngle(angle + MathF.PI);
    }

    /// <summary><see cref="TieAngle(EntityId, EntityId)"/> 방향의 단위 벡터(a 를 b 에서 떼는 쪽).</summary>
    public static Vector2 TieDir(EntityId a, EntityId b) => FromAngle(TieAngle(a, b));

    public static Vector2 FromAngle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

    /// <summary>[0, 2π)</summary>
    public static float WrapAngle(float radians)
    {
        var r = radians % MathF.Tau;
        return r < 0 ? r + MathF.Tau : r;
    }

    /// <summary>길이가 0 에 가까우면 fallback.</summary>
    public static Vector2 NormalizeOr(Vector2 v, Vector2 fallback)
    {
        var len2 = v.LengthSquared();
        return len2 > 1e-12f ? v / MathF.Sqrt(len2) : fallback;
    }

    public static bool IsFinite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>초 → 틱 수(올림, 부동소수 오차 허용). 0 이하면 0.</summary>
    public static int SecondsToTicks(float seconds, float dt) => seconds <= 0 ? 0 : (int)MathF.Ceiling(seconds / dt - 1e-3f);

    /// <summary>점에서 선분 ab 까지 거리의 제곱.</summary>
    public static float DistanceSqToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var len2 = ab.LengthSquared();
        var t = len2 > 1e-12f ? Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f) : 0f;
        return Vector2.DistanceSquared(p, a + ab * t);
    }
}
