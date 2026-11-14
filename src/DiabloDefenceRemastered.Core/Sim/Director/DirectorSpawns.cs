using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Sim.Enemies;

namespace DiabloDefenceRemastered.Core.Sim.Director;

/// <summary>포위 고리 결과(감독 시험·로그 — 가장 가까운 마리 거리·생략 수).</summary>
public readonly record struct RingResult(int Wanted, int Placed, float MinDistance);

/// <summary>
/// 감독의 생성 자리 (설계서 6.12 "일반 생성 자리"·4.10, 기획 2.4·10.3). 생성 띠(시야 사각형 밖 band)·입구 대체·포위 고리·정예 무리.
/// 일반 생성 띠는 길 거리 조건을 쓰지 않는다(4.10 — 띠는 화면 밖이고 방 안 생성은 허용).
/// </summary>
public static class DirectorSpawns
{
    /// <summary>
    /// 생성 띠 위 자리: 시야 사각형을 band 안 거리만큼 넓힌 둘레 위 무작위 점, 맵 안·clearance ≥ 반경 + edge_margin. <c>tries</c> 번 실패면 false.
    /// </summary>
    public static bool TryBandPoint(World world, float radius, out Vector2 pos)
    {
        var rules = world.Defs.Rules.Spawn ?? new SpawnRulesDef();
        var rng = world.Rng.Spawn;
        var view = world.ViewRect;
        var band = rules.Band is { Length: >= 2 } b ? b : [40f, 200f];
        for (var i = 0; i < Math.Max(1, rules.Tries); i++)
        {
            var d = rng.Range(band[0], band[1]);
            var r = view.Inflate(d);
            var per = 2f * (r.Width + r.Height);
            var t = rng.NextFloat() * per;
            Vector2 p;
            if (t < r.Width) p = new Vector2(r.X0 + t, r.Y0);
            else if ((t -= r.Width) < r.Height) p = new Vector2(r.X1, r.Y0 + t);
            else if ((t -= r.Height) < r.Width) p = new Vector2(r.X1 - t, r.Y1);
            else { t -= r.Width; p = new Vector2(r.X0, r.Y1 - t); }
            if (!world.Nav.IsInsideMap(p) || world.Nav.Clearance(p) < radius + rules.EdgeMargin) continue;
            pos = p;
            return true;
        }
        pos = default;
        return false;
    }

    /// <summary>입구 대체(생성 띠 실패): 들어온 문을 뺀 입구 중 주인공에서 가장 먼 것(방 없는 월드는 모든 입구). 없으면 null.</summary>
    public static ResolvedEntry? FarEntry(World world)
    {
        if (world.Resolved is not { } map) return null;
        var gateIn = world.Room?.Plan.GateIn;
        ResolvedEntry? best = null;
        var bestD = -1f;
        foreach (var e in map.Entries)
        {
            if (gateIn is not null && string.Equals(e.Id, gateIn, StringComparison.Ordinal)) continue;
            var d = Vector2.DistanceSquared(e.Inside, world.Hero.Pos);
            if (d > bestD) { bestD = d; best = e; }
        }
        return best;
    }

    /// <summary>입구로 들어오는 적들(맵 밖 시작점에서 진입점까지 입장). 여럿이면 입구 둘레로 조금씩 흩는다.</summary>
    public static List<Enemy> AtEntry(World world, EnemyDef def, EnemyRank rank, ReadOnlySpan<int> affixes, ResolvedEntry entry, int count,
                                      SpawnSource source, int escortCount = -1)
    {
        var list = new List<Enemy>(count);
        count = SpawnGate.Reserve(world, source, count);
        var dir = MathX.NormalizeOr(entry.Inside - entry.Outside, new Vector2(0, 1));
        var side = new Vector2(-dir.Y, dir.X);
        for (var i = 0; i < count; i++)
        {
            var lane = (i % 5 - 2) * 14f;
            var back = (i / 5) * 24f;
            var start = entry.Outside + side * lane - dir * back;
            var e = EnemyFactory.Create(world, def, rank, affixes, start, new SpawnContext(source, EntityId.None, escortCount), LifeState.Entering);
            e.EntryTarget = entry.Inside + side * lane;
            list.Add(e);
        }
        return list;
    }

    /// <summary>
    /// 포위 고리(기획 10.3 v0.8.1): 72 방향을 보고 방향마다 반경 R 에서 20px 씩 줄여(하한 min) 놓을 수 있는 반경을 찾는다(맵 안·clearance·길 거리 ≤ 직선 × path_detour).
    /// 놓을 수 있는 방향들 중에서 count 마리를 같은 간격으로 고른다(못 놓는 호를 건너뛰고 남은 호에 다시 나눔). 놓을 방향이 없으면 생략.
    /// </summary>
    public static RingResult Ring(World world, EnemyDef def, int count, float radius, float minRadius)
    {
        const int Directions = 72;
        var hero = world.Hero.Pos;
        var r = EnemyFactory.BodyRadius(world, def, EnemyRank.Normal);
        var detour = world.Defs.Rules.Spawn?.PathDetour ?? 1.5f;
        var start = world.Rng.Spawn.Angle();
        var valid = new List<(Vector2 Pos, float Dist)>(Directions);
        for (var k = 0; k < Directions; k++)
        {
            var dir = MathX.FromAngle(start + MathF.Tau * k / Directions);
            for (var rr = radius; rr >= minRadius - 1e-3f; rr -= EnemyPlacement.Step)
            {
                var p = hero + dir * rr;
                if (!world.Nav.IsInsideMap(p) || world.Nav.Clearance(p) < r + 4f) continue;
                var len = world.Nav.PathLengthToHero(p, r, rr * detour + 1f);
                if (!float.IsFinite(len)) continue;
                valid.Add((p, rr));
                break;
            }
        }
        var want = SpawnGate.Reserve(world, SpawnSource.Ring, count);
        if (valid.Count == 0 || want == 0) return new RingResult(count, 0, float.PositiveInfinity);
        var placed = 0;
        var minD = float.PositiveInfinity;
        var n = Math.Min(want, valid.Count);
        for (var i = 0; i < n; i++)
        {
            var (pos, _) = valid[(int)((long)i * valid.Count / n)];
            EnemyFactory.Create(world, def, EnemyRank.Normal, [], pos, new SpawnContext(SpawnSource.Ring, EntityId.None));
            minD = MathF.Min(minD, Vector2.Distance(pos, hero));
            placed++;
        }
        return new RingResult(count, placed, minD);
    }

    /// <summary>
    /// 정예 무리(2026-11-14 디아2식, 기획 9.1·9.2): <paramref name="uniqueChance"/>(없으면 <c>pack.unique_chance</c>)로 우두머리 무리 —
    /// 우두머리 하나(고유 이름, 접두사 <c>ranks.unique.affixes</c> 개 — <c>unique</c> 가 받을 수 있는 것, 같은 무리 하나) + 졸개(같은 종류 일반 등급, <c>pack.minions</c>).
    /// 아니면 챔피언 무리 — 정예 n, 모두 같은 접두사 <paramref name="affixCount"/> 개(<c>elite</c> 가 받을 수 있는 것). <paramref name="count"/> 는 대장 포함 전체 수.
    /// 생성 띠 한 자리에 모은다(대장 기준 길 거리 조건). 문구·신호 <c>ElitePack</c>.
    /// </summary>
    public static int ElitePack(World world, EnemyDef def, int? count, int affixCount, float? uniqueChance)
    {
        var ranks = world.Defs.Rules.Ranks;
        var rng = world.Rng.Rank;
        var pack = ranks?.Pack ?? new PackDef();
        var unique = rng.Chance(uniqueChance ?? pack.UniqueChance);
        var n = count ?? (unique ? 1 + RangeOf(rng, pack.Minions) : RangeOf(rng, pack.PackSize));
        n = SpawnGate.Reserve(world, SpawnSource.ElitePack, n);
        if (n <= 0) return 0;
        var leadRank = unique ? EnemyRank.Unique : EnemyRank.Elite;
        var r = EnemyFactory.BodyRadius(world, def, leadRank);
        Vector2 center;
        if (!TryBandPoint(world, r, out center))
        {
            if (FarEntry(world) is not { } entry) return 0;
            center = entry.Inside;
        }
        var detour = world.Defs.Rules.Spawn?.PathDetour ?? 1.5f;
        var groupRadius = MathF.Max(30f, world.Defs.Rules.Spawn?.GroupRadius ?? 40f);
        // 무리 공통 접두사 한 번(챔피언 무리는 모두 같은 형 — 기획 9.1)
        Span<int> affixes = stackalloc int[2];
        var k = unique ? ranks?.Unique.Affixes ?? 2 : affixCount;
        var picked = PickAffixes(world, def, unique ? AffixRankUnique : AffixRankElite, Math.Min(k, 2), affixes);
        var made = 0;
        for (var i = 0; i < n; i++)
        {
            var rank = i == 0 ? leadRank : unique ? EnemyRank.Normal : EnemyRank.Elite;
            var radius = i == 0 ? r : EnemyFactory.BodyRadius(world, def, rank);
            var wish = center + (i == 0 ? Vector2.Zero : MathX.FromAngle(MathF.Tau * i / n) * groupRadius);
            if (!EnemyPlacement.TryPlace(world.Nav, center, wish, radius, i == 0 ? null : new PlaceReach(center, detour), out var pos)) continue;
            var e = EnemyFactory.Create(world, def, rank, rank == EnemyRank.Normal ? [] : affixes[..picked], pos, new SpawnContext(SpawnSource.ElitePack, EntityId.None));
            if (rank == EnemyRank.Unique) e.NameRoll = UniqueNames.Roll(world);
            made++;
        }
        if (made > 0)
        {
            var banner = unique && pack.UniqueBanner.Length > 0 ? pack.UniqueBanner : pack.Banner;
            if (banner.Length > 0) world.Banner.Show(banner, world.Tick, pack.BannerTime > 0f ? pack.BannerTime : 1.5f);
            world.Presentation.Cue(new CueEvent(world.Tick, CueKind.ElitePack, center, null));
        }
        return made;
    }

    private static int RangeOf(RandomStream rng, int[] range) =>
        range.Length == 0 ? 0 : rng.RangeInclusive(range[0], range.Length > 1 ? range[1] : range[0]);

    /// <summary>접두사를 받는 등급 이름(<c>AffixDef.ranks</c> 값).</summary>
    public const string AffixRankElite = "elite", AffixRankUnique = "unique";

    /// <summary>
    /// 접두사 고르기: <paramref name="rank"/> 가 받을 수 있고(<c>ranks</c>) <c>exclude_tags</c>·<c>exclude_parts</c> 에 걸리지 않으며 <c>require_parts</c> 를 채우는 것 중
    /// 서로 다른 k 개(정의 표 순서에서 난수로). 하나를 고르면 같은 <c>group</c>(오라·강화)은 후보에서 뺀다. 보스·중간 보스는 받지 않는다.
    /// </summary>
    public static int PickAffixes(World world, EnemyDef def, string rank, int k, Span<int> into)
    {
        if (k <= 0 || def.Rank != DefRank.Normal) return 0;
        var all = world.Defs.Affixes;
        Span<int> pool = stackalloc int[Math.Max(1, all.Count)];
        var m = 0;
        for (var i = 0; i < all.Count; i++)
            if (all[i].AllowsRank(rank) && Allowed(all[i], def)) pool[m++] = i;
        var got = 0;
        while (got < k && m > 0)
        {
            var j = world.Rng.Rank.Index(m);
            var idx = pool[j];
            into[got++] = idx;
            pool[j] = pool[--m];
            if (all[idx].Group is not { } group) continue;
            for (var q = m - 1; q >= 0; q--)
                if (string.Equals(all[pool[q]].Group, group, StringComparison.Ordinal)) pool[q] = pool[--m];
        }
        return got;
    }

    private static bool Allowed(AffixDef a, EnemyDef def)
    {
        foreach (var t in a.ExcludeTags)
            foreach (var dt in def.Tags)
                if (string.Equals(t, dt, StringComparison.Ordinal)) return false;
        foreach (var part in a.ExcludeParts)
            foreach (var b in def.Behaviors)
                if (string.Equals(part, b.Type, StringComparison.Ordinal)) return false;
        if (a.RequireParts.Count == 0) return true;
        foreach (var part in a.RequireParts)
            foreach (var b in def.Behaviors)
                if (string.Equals(part, b.Type, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>지금 구간 가중치에서 정예 대상(<c>elite_eligible</c>, rank normal)만 가중으로 하나.</summary>
    public static EnemyDef? PickEliteKind(World world)
    {
        if (DirectorSystem.CurrentInterval(world) is not { } interval) return null;
        var sum = 0f;
        foreach (var w in interval.Weights)
            if (w.Ref.IsBound && w.Ref.Def.EliteEligible && w.Ref.Def.Rank == DefRank.Normal) sum += w.Weight;
        if (sum <= 0f) return null;
        var x = world.Rng.Rank.NextFloat() * sum;
        foreach (var w in interval.Weights)
        {
            if (!w.Ref.IsBound || !w.Ref.Def.EliteEligible || w.Ref.Def.Rank != DefRank.Normal) continue;
            x -= w.Weight;
            if (x < 0f) return w.Ref.Def;
        }
        return null;
    }
}
