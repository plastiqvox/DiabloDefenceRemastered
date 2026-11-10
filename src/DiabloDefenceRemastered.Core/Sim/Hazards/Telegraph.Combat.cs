namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>예고 — WP3 조각(설계서 5.5·11.2): 끝나면 실행할 폭발.</summary>
public sealed partial class Telegraph
{
    public BlastSpec Blast;

    partial void ResetCombat() => Blast = default;
}
