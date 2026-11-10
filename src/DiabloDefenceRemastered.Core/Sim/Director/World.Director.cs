using DiabloDefenceRemastered.Core.Sim.Director;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>월드 — WP4 감독 조각 (설계서 11.2 "감독 상태"). 방(월드) 수명이라 방이 바뀌면 새로 시작한다.</summary>
public sealed partial class World
{
    /// <summary>감독 상태(누적기·다음 무리·정예 판정·방 이벤트 위치·계측).</summary>
    public DirectorState DirectorState { get; } = new();
}
