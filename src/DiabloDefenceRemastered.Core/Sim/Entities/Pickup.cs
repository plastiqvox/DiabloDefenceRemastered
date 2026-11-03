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
    /// <summary>
    /// 문 보상 물체면 그 보상(v0.7, 6.18), 아니면 null. 쓰기 WP3(<c>SpawnPickup</c> 이 <see cref="PickupSpawn.Reward"/> 로). 읽기 WP3(줍기 효과), WP5·WP6(그리기·미니맵 금색), WP7(해시).
    /// </summary>
    public RewardDef? Reward;
    /// <summary>
    /// 출구 문을 막는 획득물(v0.7 — 살아 있는 동안 문이 안 열림, 3.6). 쓰기: WP3 <c>SpawnPickup</c>(<see cref="PickupSpawn.Reward"/> 가 있으면 참 — v0.7.1),
    /// <c>RoomFlow</c>(보상 단계 시작 때 남은 정예 상자 — 이번 틱에 생긴 것 포함). 읽기 <c>RoomFlow</c>(문 열기 조건), WP6·WP7.
    /// </summary>
    public bool GatesExit;
    /// <summary>
    /// 끌려오는 중(v0.7.1 — WP3 조각에 있던 끌어당김 표시를 공통 조각으로 올림). 쓰기 WP3(끌어당김), 읽기 <c>RoomFlow</c>(끌려오는 획득물이 있으면 문이 안 열리고 안 나감).
    /// </summary>
    public bool Magnetized;

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
        Reward = null;
        GatesExit = false;
        Magnetized = false;
        ResetCombat();
    }

    partial void ResetCombat();
}
