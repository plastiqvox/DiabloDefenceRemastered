using DiabloDefenceRemastered.Core.Sim.Movement.Invariants;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>World 조각 (WP2, 설계서 11.2): 방(월드) 수명의 이동 쪽 상태.</summary>
public sealed partial class World
{
    /// <summary>불변식 계측 누적(10.1 — 이 방 하나). 판 합은 시나리오가 방마다 <see cref="InvariantReport.AddTo"/> 로 만든다.</summary>
    public InvariantReport Invariants { get; } = new();
}
