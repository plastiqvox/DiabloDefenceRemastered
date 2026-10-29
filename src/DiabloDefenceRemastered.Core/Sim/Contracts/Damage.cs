namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>진영. 같은 진영 대상 피해 요청은 버린다. Ally 는 E 단계 아군 자리(R14).</summary>
public enum Faction : byte { Hero, Enemy, Ally, Neutral }

/// <summary>
/// 죽음 이유 (설계서 3.5). Damage = 보통 처치(훅·보석·드롭·처치 수·시체),
/// Collapse = 승리 때 무너짐(시체만), Culled = 상한 정리(아무것도 없음, 시체도 없음).
/// <c>Dead</c> 가 참일 때만 뜻이 있다.
/// </summary>
public enum KillCause : byte { Damage, Collapse, Culled }

[Flags]
public enum DamageFlags : byte
{
    None = 0,
    /// <summary>피격 무적을 무시한다.</summary>
    IgnoreInvuln = 1,
    /// <summary>장판 틱(적별 타격 간격·결과 집계 구분).</summary>
    FromHazard = 2,
    /// <summary>피해 없이 밀어내기만(레벨업 밀어내기).</summary>
    NoDamage = 4,
}

/// <summary>
/// 피해 요청 (설계서 5.1, 계약). 근접 타격·투사체·폭발·장판 틱·레벨업 밀어내기가 모두 이것을 큐에 넣고
/// DamageResolve(12) 한 곳이 적용한다. 다른 길은 없다.
/// </summary>
public readonly record struct DamageRequest(
    EntityId Target,          // 적 또는 주인공(EntityId.Hero). E: 아군
    EntityId Attacker,        // 때린 적(적중 수정·흡혈용). 주인공 무기·환경이면 None
    Faction From,             // Hero / Enemy / Ally / Neutral — 같은 진영 대상은 버림
    float Amount,             // 주인공 쪽: 무기 원값(레벨 표 값, damage_pct 없음). 적 쪽: 그 적의 '최종 피해'(5.2, 이미 정수)
    float DamagePct,          // 주인공 쪽: 발사 순간 복사한 damage_pct. 적 쪽 0
    float CritChance,         // 주인공 쪽: 발사 순간 복사. 적 쪽 0
    sbyte WeaponSlot,         // 주인공 무기 칸(결과 화면 합계·적별 타격 간격). 없으면 −1
    Vector2 KnockbackDir, float KnockbackDist,
    DamageFlags Flags);       // IgnoreInvuln, FromHazard, NoDamage(밀어내기만) …

/// <summary>
/// 피해 요청 큐(<c>World.Damage</c>). DamageResolve 가 모두 처리하고 비운다.
/// DamageResolve 뒤 단계(죽음 훅 폭발 등)가 넣은 요청은 다음 틱에 처리된다. 도우미도 큐에 넣기만 한다.
/// </summary>
public sealed class DamageQueue
{
    public EventList<DamageRequest> Requests { get; } = new(256);

    public int Count => Requests.Count;

    public void Add(in DamageRequest request) => Requests.Add(request);

    /// <summary>적 → 주인공. amount 는 그 적의 최종 피해.</summary>
    public void ToHero(EntityId attacker, float amount, Vector2 knockbackDir = default, float knockbackDist = 0f, DamageFlags flags = DamageFlags.None)
        => Requests.Add(new DamageRequest(EntityId.Hero, attacker, Faction.Enemy, amount, 0f, 0f, -1, knockbackDir, knockbackDist, flags));

    /// <summary>주인공 무기 → 적. amount 는 레벨 표 원값, damagePct·critChance 는 발사 순간 복사한 값.</summary>
    public void ToEnemy(EntityId target, float amount, float damagePct, float critChance, sbyte weaponSlot,
                        Vector2 knockbackDir = default, float knockbackDist = 0f, DamageFlags flags = DamageFlags.None)
        => Requests.Add(new DamageRequest(target, EntityId.None, Faction.Hero, amount, damagePct, critChance, weaponSlot, knockbackDir, knockbackDist, flags));

    /// <summary>피해 없는 밀어내기.</summary>
    public void Push(EntityId target, Faction from, Vector2 dir, float dist)
        => Requests.Add(new DamageRequest(target, EntityId.None, from, 0f, 0f, 0f, -1, dir, dist, DamageFlags.NoDamage));

    public void Clear() => Requests.Clear();
}
