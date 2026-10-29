using System.Numerics;

namespace DiabloDefenceRemastered.Core.Common;

/// <summary>난수 흐름의 용도. 한 용도에서 더 뽑아도 다른 용도의 결과는 바뀌지 않는다 (설계서 3.4).</summary>
public enum RngStream : byte { Spawn, Drop, LevelUp, Ai, Weapon, Rank }

/// <summary>시뮬레이션 난수. 씨앗 하나에서 용도별 xoshiro128** 흐름을 나눈다(씨앗 펼치기는 SplitMix64).</summary>
public sealed class SimRandom
{
    public const int StreamCount = 6;

    private readonly RandomStream[] _streams = new RandomStream[StreamCount];

    public SimRandom(ulong seed)
    {
        Seed = seed;
        for (var i = 0; i < StreamCount; i++)
            _streams[i] = new RandomStream(seed ^ (0xA24BAED4963EE407UL * (ulong)(i + 1)));
    }

    public ulong Seed { get; }

    public RandomStream this[RngStream stream] => _streams[(int)stream];

    public RandomStream Spawn => _streams[(int)RngStream.Spawn];
    public RandomStream Drop => _streams[(int)RngStream.Drop];
    public RandomStream LevelUp => _streams[(int)RngStream.LevelUp];
    public RandomStream Ai => _streams[(int)RngStream.Ai];
    public RandomStream Weapon => _streams[(int)RngStream.Weapon];
    public RandomStream Rank => _streams[(int)RngStream.Rank];
}

/// <summary>xoshiro128** 흐름 하나. 상태는 <see cref="GetState"/> 로 읽어 상태 해시에 넣는다.</summary>
public sealed class RandomStream
{
    private uint _s0, _s1, _s2, _s3;

    public RandomStream(ulong seed)
    {
        var sm = seed;
        var a = SplitMix64(ref sm);
        var b = SplitMix64(ref sm);
        _s0 = (uint)a; _s1 = (uint)(a >> 32);
        _s2 = (uint)b; _s3 = (uint)(b >> 32);
        if ((_s0 | _s1 | _s2 | _s3) == 0) _s0 = 1;   // 모두 0 이면 흐름이 멈춘다
    }

    public uint NextUInt()
    {
        var result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 9;
        _s2 ^= _s0; _s3 ^= _s1; _s1 ^= _s2; _s0 ^= _s3;
        _s2 ^= t;
        _s3 = BitOperations.RotateLeft(_s3, 11);
        return result;
    }

    /// <summary>[0, 1)</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / (1 << 24));

    /// <summary>[min, max)</summary>
    public float Range(float min, float max) => min + (max - min) * NextFloat();

    /// <summary>[min, max] 정수(양 끝 포함).</summary>
    public int RangeInclusive(int min, int max)
    {
        if (max <= min) return min;
        var span = (ulong)(max - min + 1);
        return min + (int)(NextUInt() * span >> 32);
    }

    /// <summary>[0, count)</summary>
    public int Index(int count) => count <= 1 ? 0 : (int)((ulong)NextUInt() * (uint)count >> 32);

    public bool Chance(float p) => p >= 1f || (p > 0f && NextFloat() < p);

    /// <summary>각도 [0, 2π)</summary>
    public float Angle() => NextFloat() * MathF.Tau;

    public (uint S0, uint S1, uint S2, uint S3) GetState() => (_s0, _s1, _s2, _s3);

    private static ulong SplitMix64(ref ulong x)
    {
        var z = x += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
