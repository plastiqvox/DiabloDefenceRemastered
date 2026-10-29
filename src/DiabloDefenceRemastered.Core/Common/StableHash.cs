namespace DiabloDefenceRemastered.Core.Common;

/// <summary>
/// 실행·PC 와 무관하게 같은 값을 내는 해시. 표시용 흔들림·깜빡임·장식처럼 "틱과 ID 로 계산하는" 값에 쓴다 (설계서 3.4).
/// string.GetHashCode 는 실행마다 달라지므로 문자열은 <see cref="Of(string)"/> 를 쓴다.
/// </summary>
public static class StableHash
{
    public static uint Mix(uint x)
    {
        x ^= x >> 16; x *= 0x7FEB352Du;
        x ^= x >> 15; x *= 0x846CA68Bu;
        x ^= x >> 16;
        return x;
    }

    public static uint Of(uint a) => Mix(a + 0x9E3779B9u);
    public static uint Of(uint a, uint b) => Mix(Of(a) ^ (b + 0x85EBCA6Bu + (a << 6) + (a >> 2)));
    public static uint Of(uint a, uint b, uint c) => Of(Of(a, b), c);
    public static uint Of(uint a, uint b, uint c, uint d) => Of(Of(a, b, c), d);

    public static uint Of(ulong a) => Of((uint)a, (uint)(a >> 32));

    /// <summary>FNV-1a 32비트 + 섞기.</summary>
    public static uint Of(string s)
    {
        var h = 2166136261u;
        foreach (var ch in s) { h ^= ch; h *= 16777619u; }
        return Mix(h);
    }

    /// <summary>[0, 1)</summary>
    public static float ToUnit(uint h) => (h >> 8) * (1f / (1 << 24));

    /// <summary>[-1, 1)</summary>
    public static float ToSigned(uint h) => ToUnit(h) * 2f - 1f;
}
