using DiabloDefenceRemastered.Core.Sim.Progression;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>월드 — WP3 조각(설계서 11.2 "무기·성장 내부 상태"). 방 수명이다 — 방 사이에는 짐(<see cref="HeroCarry"/>)으로만 넘긴다.</summary>
public sealed partial class World
{
    /// <summary>성장·전투 상태(레벨·경험치·대기·재추첨·짐 덧값·시간 효과·임시 버퍼). 쓰기 WP3 만, 읽기 WP7(해시·로그).</summary>
    public CombatState Combat { get; } = new();
}
