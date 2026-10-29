namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 획득물(보석·금화·물약·룬·상자) — 공통 조각 (WP1, 설계서 11.2). 끌어당김 상태는 WP3 조각(<see cref="ResetCombat"/>).
/// </summary>
public sealed partial class Pickup : IEntity
{
    public EntityId Id { get; set; }
    public bool Removed { get; set; }

    public PickupDef Def = null!;
    public Vector2 Pos;
    public Vector2 PrevPos;
    public float Radius;
    public float Age;
    /// <summary>값(경험치·금화·강화 횟수).</summary>
    public int Value;
    public int SpawnTick;

    public void Reset()
    {
        Id = EntityId.None;
        Removed = false;
        Def = null!;
        Pos = PrevPos = Vector2.Zero;
        Radius = 0f;
        Age = 0f;
        Value = 0;
        SpawnTick = 0;
        ResetCombat();
    }

    partial void ResetCombat();
}
