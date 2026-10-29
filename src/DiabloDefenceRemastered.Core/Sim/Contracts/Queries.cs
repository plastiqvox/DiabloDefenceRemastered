namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 내비 질의 (WP1 계약, WP2 구현 <c>NavWorld.Build(MapDef, StageDef)</c>, 시험용 <c>TestKit/StraightNav</c>).
/// </summary>
public interface INavWorld
{
    /// <summary>장애물·맵 경계까지 부호 있는 여유 거리(px, 이중선형, 도형 안이면 음수). "반경 r 유닛이 설 수 있다" = Clearance ≥ r.</summary>
    float Clearance(Vector2 p);

    /// <summary>선분 a→b 를 따라 반경 radius 유닛이 지나갈 수 있나(4.5).</summary>
    bool HasClearLine(Vector2 a, Vector2 b, float radius);

    /// <summary>투사체가 a→b 를 지나갈 수 있나(정확한 도형, blocks_projectiles 만). 원거리 적의 "쏠 수 있나" 도 이것.</summary>
    bool ProjectileLineClear(Vector2 a, Vector2 b);

    /// <summary>맵 안인가.</summary>
    bool IsInsideMap(Vector2 p);
}

/// <summary>
/// 적 공간 질의 (설계서 4.8, WP1 계약, WP2 구현 SpatialHash, 시험용 <c>TestKit/BruteForceSpatial</c>).
/// 들어 있는 것: Life == Active 또는 Stunned 인 적만(입장·등장 중은 위치와 상관없이 빠진다).
/// <b>결과는 거리로 거른 정확한 집합이다</b>: 구현은 칸으로 고른 후보를 다시 거리로 걸러 아래 조건을 만족하는 적만 돌려준다
/// (칸 단위로 더 잡힌 적은 넣지 않는다 → 맞음 판정·장판·폭발이 반경 밖 적을 맞히지 않는다). 적 반경은 부르는 쪽이 radius 에 더한다.
/// 결과는 적 저장소의 밀집 인덱스이고 그 틱 EndTick 압축 전까지만 유효하다. 결과 순서는 결정적이다.
/// 결과 칸이 모자라면 앞에서부터 채우고 실제 개수(채운 수보다 클 수 있음)를 돌려준다.
/// </summary>
public interface ISpatialQuery
{
    /// <summary>적 중심과 <paramref name="center"/> 의 거리 ≤ <paramref name="radius"/> 인 적.</summary>
    int Query(Vector2 center, float radius, Span<int> denseIdx);
    /// <summary>적 중심에서 선분 a→b 까지 거리 ≤ <paramref name="radius"/> 인 적(투사체 이동 선분 둘레).</summary>
    int QueryCapsule(Vector2 a, Vector2 b, float radius, Span<int> denseIdx);
    /// <summary>칸(32px) 하나에 든 적 수(조준 densest). 이것만 칸 단위다.</summary>
    int CellCount(int cx, int cy);
}

/// <summary>
/// 적대 대상 질의 (설계서 5.1, WP1 계약, WP3 구현). 적 부품·적 투사체는 대상을 주인공으로 박아 두지 않고 이것에 묻는다.
/// MVP 구현은 주인공 하나만 돌려준다.
/// </summary>
public interface IHostileQuery
{
    bool TryNearestHostile(Faction self, Vector2 from, float range, out EntityId target, out Vector2 pos);
}

public enum TelegraphShape : byte { Circle, Line }

/// <summary>적 투사체 하나. Damage 는 생성 순간 그 적의 배율·버프로 계산한 최종 피해(5.2).</summary>
/// <param name="Speed">0 이면 정의 speed.</param>
public readonly record struct EnemyProjectileSpawn(ProjectileDef Def, Vector2 Pos, Vector2 Dir, int Damage, EntityId Owner,
                                                   EntityId Target = default, float Speed = 0f);

/// <summary>장판 하나(독 웅덩이·지옥불 …). CapGroup 별 동시 상한을 넘으면 오래된 것부터 지운다.</summary>
public readonly record struct HazardSpawn(EffectRef? Effect, Vector2 Pos, float Radius, float Duration, float TickInterval, int TickDamage,
                                          Faction From, EntityId Owner, BuffDef? Buff = null, float BuffDuration = 0f,
                                          string? CapGroup = null, int Cap = 0);

/// <summary>예고가 끝나면 터지는 폭발. Radius 0 이면 폭발 없음(돌진 예고처럼 그림만).</summary>
public readonly record struct BlastSpec(float Radius, int Damage, EffectRef? Effect = null, float Knockback = 0f, ShakeDef? Shake = null);

/// <summary>예고 하나. 원이면 Pos·Size(반경), 선이면 Pos→End·Size(폭).</summary>
public readonly record struct TelegraphSpawn(EffectRef? Effect, TelegraphShape Shape, Vector2 Pos, Vector2 End, float Size, float Time,
                                             Faction From, EntityId Owner, BlastSpec Blast = default);

/// <summary>
/// 적 쪽 투사체·장판·예고 생성 (설계서 5.5, WP1 계약, WP3 구현). WP4 는 이것으로 만들기만 하고 저장소·진행·맞음 판정은 WP3 몫.
/// </summary>
public interface ICombatSpawner
{
    EntityId SpawnProjectile(in EnemyProjectileSpawn spawn);
    EntityId SpawnHazard(in HazardSpawn spawn);
    EntityId SpawnTelegraph(in TelegraphSpawn spawn);
}
