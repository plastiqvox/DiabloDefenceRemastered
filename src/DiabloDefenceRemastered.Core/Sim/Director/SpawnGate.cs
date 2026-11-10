namespace DiabloDefenceRemastered.Core.Sim.Director;

/// <summary>
/// 상한 정리 (설계서 6.6 "상한 정리 순서", 기획 2.3). 출처를 셋으로 나눈다:
/// 일반(<see cref="SpawnSource.Interval"/>) → 상한(300)에서 막힘, 이벤트(정예 무리·고리·행진·호위·중간 보스·보스·기타) → 화면 밖 먼 일반 적을 <see cref="KillCause.Culled"/> 로 지워 자리를 냄,
/// 분열·소환 → 절대 상한(360)까지 같은 방법. <see cref="SpawnSource.Stress"/> 는 검사를 건너뛴다.
/// </summary>
public static class SpawnGate
{
    public enum Tier : byte { Interval, Event, SplitSummon, Unlimited }

    public static Tier TierOf(SpawnSource source) => source switch
    {
        SpawnSource.Interval => Tier.Interval,
        SpawnSource.Split or SpawnSource.Summon => Tier.SplitSummon,
        SpawnSource.Stress => Tier.Unlimited,
        _ => Tier.Event,
    };

    /// <summary>살아 있는 적 수(입장·등장 중 포함).</summary>
    public static int Alive(World world)
    {
        var n = 0;
        foreach (var e in world.Enemies.Items)
            if (e.IsAlive) n++;
        return n;
    }

    /// <summary>
    /// <paramref name="source"/> 출처로 <paramref name="count"/> 마리를 낼 자리를 잡는다. 낼 수 있는 수를 돌려준다(이벤트·분열·소환은 먼 일반 적을 지워 자리를 냄).
    /// </summary>
    public static int Reserve(World world, SpawnSource source, int count)
    {
        if (count <= 0) return 0;
        var tier = TierOf(source);
        if (tier == Tier.Unlimited) return count;
        var caps = world.Defs.Rules.Caps ?? new CapsDef();
        var alive = Alive(world);
        if (tier == Tier.Interval) return Math.Clamp(caps.Enemies - alive, 0, count);
        var cap = tier == Tier.Event ? caps.Enemies : caps.EnemiesAbsolute;
        var over = alive + count - cap;
        if (over > 0) over -= Cull(world, over);
        return over > 0 ? Math.Max(0, count - over) : count;
    }

    /// <summary>화면 밖 일반 출처 적을 주인공에서 먼 것부터 <paramref name="n"/> 마리 지운다(보상·시체 없음). 지운 수.</summary>
    public static int Cull(World world, int n)
    {
        var view = world.ViewRect;
        var hero = world.Hero.Pos;
        var removed = 0;
        while (removed < n)
        {
            Enemy? far = null;
            var best = -1f;
            foreach (var e in world.Enemies.Items)
            {
                if (!e.IsAlive || e.Source != SpawnSource.Interval || e.Def.Rank != DefRank.Normal || view.Contains(e.Pos)) continue;
                var d = Vector2.DistanceSquared(e.Pos, hero);
                if (d > best) { best = d; far = e; }
            }
            if (far is null) break;
            far.Dead = true;
            far.KillCause = KillCause.Culled;
            removed++;
        }
        return removed;
    }
}
