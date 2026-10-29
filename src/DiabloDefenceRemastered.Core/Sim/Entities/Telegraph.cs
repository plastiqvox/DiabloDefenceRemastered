namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 예고(원·선) — 공통 조각 (WP1, 설계서 5.5·11.2). 피해 시점이 예고 시간에 묶여 있어 시뮬레이션 개체다.
/// 끝나면 실행할 폭발 매개변수는 WP3 조각(<see cref="ResetCombat"/>). 렌더는 남은 시간 비율만큼 안쪽을 채운다.
/// </summary>
public sealed partial class Telegraph : IEntity
{
    public EntityId Id { get; set; }
    public bool Removed { get; set; }

    public EffectDef? Effect;
    public Rgb Tint;
    public TelegraphShape Shape;
    public Faction Faction;
    /// <summary>원의 중심, 선의 시작.</summary>
    public Vector2 Pos;
    public Vector2 PrevPos;
    /// <summary>선의 끝.</summary>
    public Vector2 End;
    /// <summary>원 반경 또는 선 폭.</summary>
    public float Size;
    public float Age;
    public float TimeLeft;
    public float Duration;
    public EntityId Owner;
    public int SpawnTick;

    public float Progress => Duration > 0f ? Math.Clamp(1f - TimeLeft / Duration, 0f, 1f) : 0f;

    public void Reset()
    {
        Id = EntityId.None;
        Removed = false;
        Effect = null;
        Tint = Rgb.White;
        Shape = TelegraphShape.Circle;
        Faction = Faction.Enemy;
        Pos = PrevPos = End = Vector2.Zero;
        Size = 0f;
        Age = TimeLeft = Duration = 0f;
        Owner = EntityId.None;
        SpawnTick = 0;
        ResetCombat();
    }

    partial void ResetCombat();
}
