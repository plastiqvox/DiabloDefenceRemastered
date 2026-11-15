namespace DiabloDefenceRemastered.Core.Art;

/// <summary>바닥 핏웅덩이 하나: 바닥 자리(월드 px), 가로 반경(세로는 절반), 모양 씨앗 0..1.</summary>
public readonly record struct BloodPool(Vector2 Pos, float Radius, float Seed);

/// <summary>
/// 바닥 핏웅덩이 자리 고르기(2026-11-15 사용자 결정 "기존의 피타일은 제거하고 핏웅덩이 몇군데", "방마다 미리 몇군데"). 표시 전용 — 시뮬레이션 난수를 쓰지 않는다.
/// <list type="bullet">
/// <item>후보 = 걸을 수 있고 둘레 8칸도 걸을 수 있는 칸(웅덩이가 벽 밑·맵 밖으로 번지지 않게).</item>
/// <item>문(포탈) 칸에서 <see cref="BloodPoolsDef.GateMargin"/> 안은 뺀다 — 포탈 빛과 겹치지 않게.</item>
/// <item>후보를 <c>StableHash(key, 칸)</c> 순으로 보며 다른 웅덩이와 <see cref="BloodPoolsDef.MinGap"/> 이상 떨어진 것만 고른다. 수는 key 로 Min~Max.</item>
/// </list>
/// key 는 방 씨앗과 맵 ID 로 만든다 — 같은 방은 늘 같은 자리, 방마다 다른 자리.
/// </summary>
public static class BloodPools
{
    public static uint Key(ulong roomSeed, string mapId) => StableHash.Of(StableHash.Of(roomSeed), StableHash.Of(mapId));

    public static IReadOnlyList<BloodPool> Place(ArenaGrid grid, BloodPoolsDef def, uint key, IEnumerable<Vector2> avoid)
    {
        var iso = grid.Iso;
        var avoidList = avoid.ToList();
        var candidates = new List<(uint Hash, Vector2 Pos)>();
        for (var ty = 1; ty < grid.Size - 1; ty++)
            for (var tx = 1; tx < grid.Size - 1; tx++)
            {
                if (!OpenAround(grid, tx, ty)) continue;
                var pos = iso.CellCenter(tx, ty);
                if (avoidList.Any(a => Vector2.Distance(a, pos) < def.GateMargin)) continue;
                candidates.Add((StableHash.Of(key, (uint)tx, (uint)ty), pos));
            }
        candidates.Sort((a, b) => a.Hash.CompareTo(b.Hash));

        var min = Math.Max(0, def.Min);
        var max = Math.Max(min, def.Max);
        var count = min + (int)(StableHash.Of(key, 0xB100Du) % (uint)(max - min + 1));
        var pools = new List<BloodPool>(count);
        foreach (var (hash, pos) in candidates)
        {
            if (pools.Count >= count) break;
            if (pools.Any(p => Vector2.Distance(p.Pos, pos) < def.MinGap)) continue;
            var radius = def.RadiusMin + (def.RadiusMax - def.RadiusMin) * StableHash.ToUnit(StableHash.Of(hash, 1u));
            pools.Add(new BloodPool(pos, radius, StableHash.ToUnit(StableHash.Of(hash, 2u))));
        }
        return pools;
    }

    private static bool OpenAround(ArenaGrid grid, int tx, int ty)
    {
        for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
                if (!grid.IsWalkable(tx + dx, ty + dy)) return false;
        return true;
    }
}

/// <summary>핏웅덩이 물결 하나: 웅덩이 안 자리(-1..1 — 웅덩이 사각형 기준, 아이소 타원이 원), 생긴 때(스테이지 초).</summary>
public readonly record struct BloodRipple(Vector2 Local, float Start);

/// <summary>
/// 핏웅덩이 밟기 물결(2026-11-15 사용자 요청 "물웅덩이 밟는 효과", "픽셀셰이더라"). 표시 전용 상태 — 월드가 바뀌면 <see cref="Reset"/>.
/// 움직이는 주인공·적의 발이 웅덩이 안(<see cref="Inside"/>)에 있으면 웅덩이마다 <see cref="Interval"/> 초에 하나씩 물결을 만든다.
/// 웅덩이마다 최근 <see cref="PerPool"/> 개만 둔다(가장 오래된 것을 덮음). 셰이더(<c>BloodPool.fx</c>)가 나이로 퍼지는 고리를 그린다.
/// </summary>
public sealed class BloodRipples
{
    public const int PerPool = 4;
    /// <summary>웅덩이 하나에 물결이 생기는 최소 간격(초).</summary>
    public const float Interval = 0.22f;
    /// <summary>물결 수명(초) — 셰이더 <c>RippleLife</c> 와 같다.</summary>
    public const float Life = 1.2f;
    /// <summary>발이 이 거리(-1..1 공간) 안이어야 밟은 것.</summary>
    public const float Inside = 0.7f;
    /// <summary>이만큼(px) 이상 움직였을 때만(서 있으면 물결 없음).</summary>
    public const float MinMove = 0.05f;

    private BloodRipple[][] _ripples = [];
    private int[] _next = [];
    private float[] _last = [];

    public void Reset(int pools)
    {
        _ripples = new BloodRipple[pools][];
        for (var i = 0; i < pools; i++)
        {
            _ripples[i] = new BloodRipple[PerPool];
            for (var k = 0; k < PerPool; k++) _ripples[i][k] = new BloodRipple(Vector2.Zero, float.NegativeInfinity);
        }
        _next = new int[pools];
        _last = Enumerable.Repeat(float.NegativeInfinity, pools).ToArray();
    }

    /// <summary>웅덩이 사각형 기준 자리(-1..1). 세로는 반경의 절반(아이소 바닥).</summary>
    public static Vector2 Local(BloodPool pool, Vector2 world) => new((world.X - pool.Pos.X) / pool.Radius, (world.Y - pool.Pos.Y) / (pool.Radius * 0.5f));

    /// <summary>걷는 것 하나(이번 틱 자리·지난 틱 자리)를 본다.</summary>
    public void Observe(IReadOnlyList<BloodPool> pools, float time, Vector2 pos, Vector2 prev)
    {
        if (pools.Count != _ripples.Length) Reset(pools.Count);
        if (Vector2.DistanceSquared(pos, prev) < MinMove * MinMove) return;
        for (var i = 0; i < pools.Count; i++)
        {
            if (time - _last[i] < Interval) continue;
            var local = Local(pools[i], pos);
            if (local.Length() > Inside) continue;
            _ripples[i][_next[i]] = new BloodRipple(local, time);
            _next[i] = (_next[i] + 1) % PerPool;
            _last[i] = time;
        }
    }

    /// <summary>웅덩이 <paramref name="pool"/> 의 물결(아직 안 생긴 칸은 Start = -∞).</summary>
    public ReadOnlySpan<BloodRipple> Of(int pool) => pool < _ripples.Length ? _ripples[pool] : [];
}
