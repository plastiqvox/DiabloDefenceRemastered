namespace DiabloDefenceRemastered.Core.Art;

/// <summary>이펙트 한 순간의 그림(설계서 6.4). <see cref="Intro"/> 면 <c>intro</c> 그림의 프레임이다.</summary>
public readonly record struct EffectFrame(bool Intro, int Frame1, bool Done);

/// <summary>
/// 표시 이펙트 재생 (설계서 5.6·6.4, 소유 WP5). 나이(초) → 프레임. <c>intro</c> 를 한 번 재생한 뒤 본 그림 <c>frames</c> 를 반복(<c>loop</c>)하거나 한 번 재생한다.
/// 나이는 늘 "지금 스테이지 틱 − 생긴 틱" 으로 계산해 넘긴다(진행량을 누적하지 않음 — 3.4·5.6).
/// </summary>
public static class EffectPlayback
{
    /// <summary>[처음, 끝](1부터, 끝 포함). 없으면 그림 전체 1..frames.</summary>
    public static (int First, int Last) Range(int[]? frames, SpriteDef? sprite)
    {
        if (frames is { Length: 2 } f && f[0] >= 1 && f[1] >= f[0]) return (f[0], f[1]);
        return (1, Math.Max(1, sprite?.Frames ?? 1));
    }

    /// <summary>intro 길이(초).</summary>
    public static float IntroLength(EffectDef def)
    {
        if (def.Intro is not { } intro) return 0f;
        var (a, b) = Range(intro.Frames, intro.Sprite.Def);
        return (b - a + 1) * def.FrameTime;
    }

    /// <summary>본 그림 한 바퀴 길이(초).</summary>
    public static float CycleLength(EffectDef def)
    {
        var (a, b) = Range(def.Frames, def.Sprite?.Def);
        return (b - a + 1) * def.FrameTime;
    }

    /// <summary>
    /// <paramref name="age"/> 초의 프레임. <paramref name="startFrame"/> = 반복 구간의 시작 어긋남(흩뿌린 조각마다 다르게, 0부터).
    /// 반복이 아니면 마지막 프레임을 지난 뒤 <see cref="EffectFrame.Done"/>.
    /// </summary>
    public static EffectFrame Sample(EffectDef def, float age, int startFrame = 0)
    {
        var ft = def.FrameTime > 0f ? def.FrameTime : 0.06f;
        if (age < 0f) age = 0f;
        if (def.Intro is { } intro)
        {
            var (ia, ib) = Range(intro.Frames, intro.Sprite.Def);
            var introLen = (ib - ia + 1) * ft;
            if (age < introLen) return new EffectFrame(true, ia + Math.Min(ib - ia, (int)(age / ft)), false);
            age -= introLen;
        }
        var (a, b) = Range(def.Frames, def.Sprite?.Def);
        var n = b - a + 1;
        var idx = (int)(age / ft) + startFrame;
        if (def.Loop) return new EffectFrame(false, a + idx % n, false);
        if (idx >= n) return new EffectFrame(false, b, true);
        return new EffectFrame(false, a + idx, false);
    }

    /// <summary>
    /// 크기 배율(설계서 3.x <c>FxEvent</c>): <paramref name="radius"/> &gt; 0 이고 <c>fit_radius</c> 가 있으면 radius / fit_radius, 아니면 정의 <c>scale</c>(없으면 1). 거기에 <paramref name="mul"/>.
    /// </summary>
    public static float Scale(EffectDef def, float radius, float mul)
    {
        var baseScale = radius > 0f && def.FitRadius is > 0f ? radius / def.FitRadius.Value : def.Scale ?? 1f;
        return baseScale * (mul > 0f ? mul : 1f);
    }
}

/// <summary>
/// 장판 조각 흩뿌리기 (설계서 6.4 <c>scatter</c>, 소유 WP5) — 표시만, 시뮬레이션과 무관. 자리·시작 프레임 = <c>StableHash(장판 키, i)</c>.
/// 개수 = max(min, round(반경 / per_radius)) + (center 면 가운데 1). 가운데 조각은 0번.
/// </summary>
public static class ScatterLayout
{
    public static int Count(ScatterDef scatter, float radius)
    {
        var ring = scatter.PerRadius > 0f ? (int)MathF.Round(radius / scatter.PerRadius, MidpointRounding.AwayFromZero) : 0;
        return Math.Max(scatter.Min, ring) + (scatter.Center ? 1 : 0);
    }

    /// <summary>조각 <paramref name="i"/> 의 자리(중심 기준 오프셋, 반경 안 고른 분포 — 원 넓이 기준).</summary>
    public static Vector2 Offset(ScatterDef scatter, uint key, int i, float radius)
    {
        if (scatter.Center && i == 0) return Vector2.Zero;
        var angle = StableHash.ToUnit(StableHash.Of(key, (uint)i, 1u)) * MathF.Tau;
        var dist = MathF.Sqrt(StableHash.ToUnit(StableHash.Of(key, (uint)i, 2u))) * radius;
        return new Vector2(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);
    }

    /// <summary>조각마다 반복 시작 프레임 어긋남(0..frames−1) — 같은 순간에 같은 그림이 겹치지 않게.</summary>
    public static int StartFrame(uint key, int i, int frames) => frames <= 1 ? 0 : (int)(StableHash.Of(key, (uint)i, 3u) % (uint)frames);
}

/// <summary>
/// 화면 흔들림 (설계서 7.2, 소유 WP5): 지금 남은 흔들림 중 가장 센 것 → ±px 정수 오프셋 = <c>StableHash(seed, stageTick)</c>(프레임마다 뽑는 난수 아님).
/// </summary>
public static class ShakeMath
{
    /// <summary>남은 것 중 가장 센 px(없으면 0). 남음 = 지금 틱 − 생긴 틱 &lt; 지속 × 60.</summary>
    public static float Strength(ReadOnlySpan<ShakeEvent> shakes, int stageTick)
    {
        var best = 0f;
        foreach (var s in shakes)
        {
            var age = stageTick - s.Tick;
            if (age >= 0 && age < s.Duration * World.TicksPerSecond && s.Px > best) best = s.Px;
        }
        return best;
    }

    /// <summary>오프셋(정수 px). 세기 0 이면 0.</summary>
    public static Int2 Offset(ulong seed, int stageTick, float px)
    {
        if (px <= 0f) return Int2.Zero;
        var h = StableHash.Of(seed);
        var x = (int)MathF.Round(StableHash.ToSigned(StableHash.Of(h, (uint)stageTick, 1u)) * px);
        var y = (int)MathF.Round(StableHash.ToSigned(StableHash.Of(h, (uint)stageTick, 2u)) * px);
        return new Int2(x, y);
    }
}

/// <summary>피해 숫자 한 개(설계서 7.8).</summary>
public struct DamageNumberEntry
{
    public bool Active;
    public EntityId Target;
    public Vector2 Pos;
    public int Value;
    public bool Crit;
    public bool ToHero;
    /// <summary>처음 생긴 스테이지 틱(합쳐도 그대로 — 올라가는 길이 끊기지 않게).</summary>
    public int StartTick;
    /// <summary>마지막으로 합친 틱(0.1s 합치기 창의 기준).</summary>
    public int LastTick;
}

/// <summary>
/// 피해 숫자 풀 (설계서 7.8, 소유 WP5): 120개. 같은 대상(ID 가 있는 것)이 0.1s 안이면 합침. 0.6s 동안 24px 위로 + 알파 감소. 할당은 생성 때만.
/// 풀이 차면 가장 오래된 것을 바꾼다.
/// </summary>
public sealed class DamageNumberPool(int capacity = DamageNumberPool.DefaultCapacity)
{
    public const int DefaultCapacity = 120;
    public const int MergeTicks = 6;        // 0.1s
    public const int LifeTicks = 36;        // 0.6s
    public const float Rise = 24f;

    private readonly DamageNumberEntry[] _items = new DamageNumberEntry[capacity];

    public ReadOnlySpan<DamageNumberEntry> Items => _items;

    public int ActiveCount
    {
        get
        {
            var n = 0;
            foreach (ref readonly var e in _items.AsSpan()) if (e.Active) n++;
            return n;
        }
    }

    public void Add(in DamageNumber d)
    {
        var oldest = 0;
        var free = -1;
        for (var i = 0; i < _items.Length; i++)
        {
            ref var e = ref _items[i];
            if (!e.Active) { if (free < 0) free = i; continue; }
            if (d.Target != default && e.Target == d.Target && e.ToHero == d.ToHero && d.Tick - e.LastTick <= MergeTicks && d.Tick >= e.LastTick)
            {
                e.Value += d.Value;
                e.Crit |= d.Crit;
                e.LastTick = d.Tick;
                return;
            }
            if (_items[oldest].Active && e.StartTick < _items[oldest].StartTick) oldest = i;
        }
        var slot = free >= 0 ? free : oldest;
        _items[slot] = new DamageNumberEntry
        {
            Active = true, Target = d.Target, Pos = d.Pos, Value = d.Value, Crit = d.Crit, ToHero = d.ToHero, StartTick = d.Tick, LastTick = d.Tick,
        };
    }

    /// <summary>수명이 끝난 것을 끈다(스테이지 틱 기준).</summary>
    public void Expire(int stageTick)
    {
        for (var i = 0; i < _items.Length; i++)
            if (_items[i].Active && (stageTick - _items[i].StartTick >= LifeTicks || stageTick < _items[i].StartTick)) _items[i].Active = false;
    }

    public void Clear() => Array.Clear(_items);

    /// <summary>진행 비율 0..1(<paramref name="age"/> = 틱, 보간 포함 실수).</summary>
    public static float Progress(float age) => Math.Clamp(age / LifeTicks, 0f, 1f);

    /// <summary>그릴 자리(생긴 자리에서 위로 24px × 진행).</summary>
    public static Vector2 DrawPos(in DamageNumberEntry e, float age) => e.Pos - new Vector2(0f, Rise * Progress(age));

    /// <summary>알파 = 1 − 진행.</summary>
    public static float Alpha(float age) => 1f - Progress(age);
}
