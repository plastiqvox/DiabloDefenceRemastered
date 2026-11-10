namespace DiabloDefenceRemastered.Core.Sim.Progression;

/// <summary>경험치 곡선(기획 4.3, <c>rules.xp_curve</c> — 6.10): 레벨 L → L+1 필요 경험치 = base + per × (L − origin), L ∈ [from, to].</summary>
public static class XpCurve
{
    /// <summary>곡선이 없거나 L 이 어느 구간에도 없으면 <see cref="int.MaxValue"/>(레벨이 오르지 않음 — 규칙 없는 시험 월드).</summary>
    public static int Need(IReadOnlyList<XpCurveSegment> curve, int level)
    {
        foreach (var s in curve)
        {
            if (level < s.From || (s.To is { } to && level > to)) continue;
            var origin = s.Origin ?? s.From;
            return Math.Max(1, (int)MathF.Round(s.Base + s.Per * (level - origin), MidpointRounding.AwayFromZero));
        }
        return int.MaxValue;
    }
}
