namespace DiabloDefenceRemastered.Core.Sim.Run;

/// <summary>
/// 판 씨앗 → 방마다 씨앗 (v0.7, 설계서 3.6 결정 2 — 계약·구현 WP1e). SplitMix64 섞기 한 곳이다.
/// <b>씨앗 이름(소금)·섞는 순서를 바꾸면 모든 판이 바뀐다</b> — 같은 씨앗의 기록·<c>--room</c> 같음이 깨지므로 계약이다(11.3, 메인 세션).
/// 방마다 따로라 "앞 방 플레이와 상관없이 같은 순번 방의 생성이 같다"(기획 2.5)가 구조로 선다.
/// </summary>
public static class RunSeeds
{
    private const ulong FlowSalt = 0x464C4F575F524F4Ful;   // "FLOW_ROO"
    private const ulong RoomSalt = 0x524F4F4D5F574F52ul;   // "ROOM_WOR"

    /// <summary>판 흐름(방 맵·들어오는 문·출구 문 수·보상 후보 — <see cref="RunPlanner"/>). <paramref name="number"/> = <c>RoomDef.Number</c>(보이는 번호, v0.7.1).</summary>
    public static ulong Flow(ulong runSeed, int act, int number) => Mix(runSeed, FlowSalt, act, number);

    /// <summary>방 <c>World</c> 의 <c>SimRandom</c> 씨앗.</summary>
    public static ulong Room(ulong runSeed, int act, int number) => Mix(runSeed, RoomSalt, act, number);

    private static ulong Mix(ulong seed, ulong salt, int act, int number)
    {
        var x = SplitMix64(seed ^ salt);
        x = SplitMix64(x ^ (uint)act);
        return SplitMix64(x ^ ((ulong)(uint)number << 32));
    }

    /// <summary>SplitMix64 한 걸음(Steele·Lea·Flood 2014 의 섞기 함수).</summary>
    public static ulong SplitMix64(ulong z)
    {
        unchecked
        {
            z += 0x9E3779B97F4A7C15ul;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9ul;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBul;
            return z ^ (z >> 31);
        }
    }
}
