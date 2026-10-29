namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 장판(독 웅덩이·지옥불·연옥불 …) — 공통 조각 (WP1, 설계서 5.5·11.2). 틱 피해·붙이는 버프·상한 그룹은 WP3 조각(<see cref="ResetCombat"/>).
/// 반복 이펙트는 장판이 살아 있는 동안 렌더가 직접 그린다.
/// </summary>
public sealed partial class Hazard : IEntity
{
    public EntityId Id { get; set; }
    public bool Removed { get; set; }

    public EffectDef? Effect;
    public Rgb Tint;
    public float EffectScale;
    public Faction Faction;
    public Vector2 Pos;
    public Vector2 PrevPos;
    public float Radius;
    public float Age;
    public float TimeLeft;
    public float Duration;
    /// <summary>만든 적. 주인공 무기면 None.</summary>
    public EntityId Owner;
    public int SpawnTick;

    public float Progress => Duration > 0f ? Math.Clamp(1f - TimeLeft / Duration, 0f, 1f) : 0f;

    public void Reset()
    {
        Id = EntityId.None;
        Removed = false;
        Effect = null;
        Tint = Rgb.White;
        EffectScale = 1f;
        Faction = Faction.Hero;
        Pos = PrevPos = Vector2.Zero;
        Radius = 0f;
        Age = TimeLeft = Duration = 0f;
        Owner = EntityId.None;
        SpawnTick = 0;
        ResetCombat();
    }

    partial void ResetCombat();
}
