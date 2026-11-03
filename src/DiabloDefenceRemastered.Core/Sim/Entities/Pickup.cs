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
    /// <summary>
    /// 방 보급이면 그 방 기준점 ID(v0.6.3, <c>room_supply</c> 이벤트 — 기획 10.1), 아니면 null. 쓰기 WP3(<see cref="PickupSpawn.SupplyRoom"/> 를 받아 채움, 넘기는 쪽 WP4).
    /// 읽기: WP4 "안 주운 보급" 판정(같은 기준점에 살아 있는 보급이 있으면 새 보급 생략), WP3(수명·상한 정리에서 빼고 주우면 <see cref="SupplyCollected"/>),
    /// WP6 미니맵 흰 점(가 본 곳이 아니어도), WP7 <c>StateHasher</c>.
    /// </summary>
    public string? SupplyRoom;

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
        SupplyRoom = null;
        ResetCombat();
    }

    partial void ResetCombat();
}
