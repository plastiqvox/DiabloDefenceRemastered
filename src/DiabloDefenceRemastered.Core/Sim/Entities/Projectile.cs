namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 투사체 — 공통 조각 (WP1, 설계서 11.2). 쓰기 WP3(WP4 는 ICombatSpawner 로 생성만), 읽기 WP4(세기)·WP5(그리기).
/// 맞은 목록·궤적 상태·피해 값은 WP3 조각(<c>Sim/Projectiles/…</c>, <see cref="ResetCombat"/>).
/// </summary>
public sealed partial class Projectile : IEntity
{
    public EntityId Id { get; set; }
    public bool Removed { get; set; }

    public ProjectileDef Def = null!;
    public Faction Faction;
    public Vector2 Pos;
    public Vector2 PrevPos;
    /// <summary>속도 벡터(그림 방향도 이것).</summary>
    public Vector2 Vel;
    public float Radius;
    /// <summary>그릴 색조·배율(보통 정의 값, 진화·덮어쓰기가 바꿀 수 있다).</summary>
    public Rgb Tint;
    public float Scale;
    public float Age;
    /// <summary>남은 시간(초). 사거리로 끝나는 투사체는 WP3 가 거리로 환산한다.</summary>
    public float TimeLeft;
    public float Duration;
    /// <summary>쏜 적. 주인공 무기면 None.</summary>
    public EntityId Owner;
    /// <summary>주인공 무기 칸. 적 투사체면 -1.</summary>
    public sbyte WeaponSlot;
    public int SpawnTick;

    /// <summary>진행 비율 [0, 1].</summary>
    public float Progress => Duration > 0f ? Math.Clamp(1f - TimeLeft / Duration, 0f, 1f) : 0f;

    public void Reset()
    {
        Id = EntityId.None;
        Removed = false;
        Def = null!;
        Faction = Faction.Hero;
        Pos = PrevPos = Vel = Vector2.Zero;
        Radius = 0f;
        Tint = Rgb.White;
        Scale = 1f;
        Age = TimeLeft = Duration = 0f;
        Owner = EntityId.None;
        WeaponSlot = -1;
        SpawnTick = 0;
        ResetCombat();
    }

    partial void ResetCombat();
}
