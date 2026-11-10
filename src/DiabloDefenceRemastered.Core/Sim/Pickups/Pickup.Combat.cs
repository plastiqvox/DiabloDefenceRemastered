namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>획득물 — WP3 조각(설계서 11.2): 끌려오는 속도(기획 11.1 — 처음 200px/s, 초당 +600).</summary>
public sealed partial class Pickup
{
    public float MagnetSpeed;

    partial void ResetCombat() => MagnetSpeed = 0f;
}
