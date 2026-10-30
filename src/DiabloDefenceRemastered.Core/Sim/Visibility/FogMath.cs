namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 안개 알파 (설계서 4.11·7.10, 구현 WP1c, 그 뒤 WP5 소유 — 그리기 규칙). 그리기(WP5)·미니맵 점(WP6)·공정성 계측(WP3·<c>InvariantChecker</c>)이
/// 같은 "보이는가" 를 쓰게 Core 에 둔다. 시그니처는 계약(11.3), 안의 값(보스 늘 보임·닿은 거리·흐림 띠)은 WP5 가 바꾼다.
/// <see cref="VisibilityField.Enabled"/> 가 거짓이면 모든 값이 1 이다(<c>--reveal</c> 은 그리기가 이 함수를 부르지 않는 것으로 한다).
/// 틱 값 오버로드는 시뮬레이션·계측용(<see cref="VisibilityField.HeroPos"/> = 틱 시작 값), 위치를 받는 오버로드는 그리기용(보간된 주인공·대상 위치).
/// </summary>
public static class FogMath
{
    /// <summary>칸 값 0..1: 칸 보임이 바뀐 뒤 F 틱에 걸쳐 바뀐다(바뀜 값 잇기는 <see cref="VisibilityField.ChangedTick"/> 가 이미 했다).</summary>
    public static float CellLevel(VisibilityField f, int tx, int ty, int stageTick)
    {
        if (!f.Enabled) return 1f;
        var on = f.InSight(tx, ty);
        var fade = f.FadeTicks;
        if (fade <= 0) return on ? 1f : 0f;
        var p = Math.Clamp((stageTick - f.ChangedTick(tx, ty)) / (float)fade, 0f, 1f);
        return on ? p : 1f - p;
    }

    /// <summary>자리 알파 = <see cref="CellLevel"/>(pos 의 칸) × clamp((R − |pos − HeroPos|) / edge, 0, 1). 시체·획득물·일반 적.</summary>
    public static float PosAlpha(VisibilityField f, Vector2 pos, int stageTick) => PosAlpha(f, pos, stageTick, f.HeroPos);

    /// <summary>그리기용: 거리는 보간된 주인공 위치 <paramref name="heroPos"/> 로 잰다(칸 값은 그대로).</summary>
    public static float PosAlpha(VisibilityField f, Vector2 pos, int stageTick, Vector2 heroPos)
    {
        if (!f.Enabled) return 1f;
        var c = f.CellOf(pos);
        var level = CellLevel(f, c.X, c.Y, stageTick);
        if (level <= 0f) return 0f;
        var d = Vector2.Distance(pos, heroPos);
        var edge = f.Rules!.Edge;
        var ramp = edge > 0f ? Math.Clamp((f.Radius - d) / edge, 0f, 1f) : (d <= f.Radius ? 1f : 0f);
        return level * ramp;
    }

    /// <summary>
    /// 적 알파: 1 = 정의 등급 중간 보스·보스, 주인공에 닿은 적(거리 ≤ 두 반경 + <c>e.TouchReach</c>), <c>e.Visual.ForceShown</c>(자폭 점화 중).
    /// 그 밖은 <see cref="PosAlpha(VisibilityField, Vector2, int)"/>(발). 시뮬레이션·계측용(틱 시작 주인공 위치, 적의 지금 위치).
    /// </summary>
    public static float EnemyAlpha(World w, Enemy e, int stageTick) => EnemyAlpha(w, e, stageTick, w.Visibility.HeroPos, e.Pos);

    /// <summary>그리기용: 보간된 주인공 위치와 적의 그리는 발 위치.</summary>
    public static float EnemyAlpha(World w, Enemy e, int stageTick, Vector2 heroPos, Vector2 drawPos)
    {
        var f = w.Visibility;
        if (!f.Enabled) return 1f;
        if (e.Def is { Rank: DefRank.Miniboss or DefRank.Boss }) return 1f;
        if (e.Visual.ForceShown) return 1f;
        var touch = e.Radius + w.Hero.Radius + e.TouchReach;
        if (Vector2.DistanceSquared(drawPos, heroPos) <= touch * touch) return 1f;
        return PosAlpha(f, drawPos, stageTick, heroPos);
    }
}
