using System.Globalization;

namespace DiabloDefenceRemastered.Core.Nav.Validation;

/// <summary>
/// 플로우 필드 등급 밖 몸 경고 (설계서 4.3 표 끝, 소유 WP2): <c>movement: flow</c> 적의 생성 때 몸 반경(등급 배율·<c>rules.ranks.max_scale</c> 로 자른 최종 크기, 6.5)이
/// <c>NavConst.FieldM</c>(26)을 넘으면 경고 — M 필드 통과 칸보다 큰 몸이라 필드가 가리키는 좁은 틈에서 비빈다(R3). <c>astar</c> 로 바꾸거나 몸을 줄인다.
/// 파일 이름은 로더(WP7)만 알므로 종류 이름(<c>enemies</c>)과 ID 경로로 낸다.
/// </summary>
public sealed class FlowRadiusValidator : IDataValidator
{
    public string Name => "nav.flow_radius";

    public void Validate(GameDefs defs, ValidationContext ctx)
    {
        var ranks = defs.Rules.Ranks;
        foreach (var e in defs.Enemies.All)
        {
            if (e.Movement != MovementKind.Flow) continue;
            var worst = BodyRadius(e, 1f, ranks);
            if (e.EliteEligible && ranks is not null)
                worst = MathF.Max(worst, MathF.Max(BodyRadius(e, ranks.Elite.Scale, ranks), BodyRadius(e, ranks.Unique.Scale, ranks)));
            if (worst > NavConst.FieldM)
                ctx.Add("enemies", $"$.enemies[{e.Id}].radius",
                    string.Create(CultureInfo.InvariantCulture, $"flow 적의 몸 반경 {worst:0} 이 M 필드 문턱 26 을 넘는다(등급 포함) — movement: astar 나 더 작은 몸"),
                    DataSeverity.Warning);
        }
    }

    /// <summary>생성 때 몸 반경(6.5): 최종 크기 = min(정의 scale × 등급 배율, max_scale), 반경 = round(base_radius × 최종) 또는 round(radius × 최종 / scale).</summary>
    public static float BodyRadius(EnemyDef e, float rankScale, RanksDef? ranks)
    {
        var final = MathF.Min(e.Scale * rankScale, ranks?.MaxScale ?? float.MaxValue);
        return e.Radius is { } r
            ? MathF.Round(r * final / (e.Scale > 0f ? e.Scale : 1f))
            : MathF.Round(e.Sprites.Def.BaseRadius * final);
    }
}
