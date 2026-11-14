using System.Runtime.CompilerServices;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>투사체 궤적(설계서 5.4) — 뜨거운 반복이라 가상 호출 대신 열거형 + switch. 새 궤적 = 값 하나 + 함수 하나.</summary>
public enum MotionKind : byte { Straight, Homing, Orbit, Ricochet }

/// <summary>맞은 적 목록 16칸(관통·튕김이 같은 적을 두 번 때리지 않게, 넘치면 가장 오래된 것부터 덮음 — 5.4).</summary>
[InlineArray(Capacity)]
public struct HitList
{
    public const int Capacity = 16;
    private EntityId _e0;
}

/// <summary>투사체 — WP3 조각(설계서 5.4·11.2): 궤적·피해·관통·맞은 목록·폭발.</summary>
public sealed partial class Projectile
{
    public MotionKind Motion;
    /// <summary>주인공 쪽: 무기 원값(damage_pct 없음). 적 쪽: 최종 피해(정수).</summary>
    public float Damage;
    /// <summary>발사 순간 복사한 damage_pct·치명 확률(주인공 쪽, 5.2 발사 시점 고정).</summary>
    public float DamagePct;
    public float CritChance;
    public float Knockback;
    /// <summary>적 탄: 쏜 적의 <c>Enemy.HitFlags</c>(생성 때 복사 — 냉기 강화 탄이 쏜 적이 죽은 뒤에도 둔화를 건다).</summary>
    public DamageFlags HitFlags;
    /// <summary>남은 관통 수(맞을 때마다 1 줄고 0 이면 끝). 궤도 구체는 무한.</summary>
    public int PierceLeft;
    /// <summary>유도·튕김 대상.</summary>
    public EntityId Target;
    /// <summary>유도 회전율(라디안/초).</summary>
    public float TurnRate;
    /// <summary>사거리로 끝나는 투사체의 남은 거리(px, +∞ = 수명만).</summary>
    public float RangeLeft;
    public float Speed;
    public HitList Hits;
    public int HitCount;
    /// <summary>폭발 반경(0 = 폭발 없음)·폭발 피해 원값.</summary>
    public float ExplodeRadius;
    public float ExplodeDamage;
    /// <summary>궤도: 주인공 기준 각(라디안)·반경·각속도(라디안/초)·적별 타격 간격(초).</summary>
    public float OrbitAngle;
    public float OrbitRadius;
    public float AngularSpeed;
    public float HitInterval;
    /// <summary>튕김 남은 수·튕김 찾는 반경.</summary>
    public int BouncesLeft;
    public float BounceRange;
    /// <summary>쏜 무기(진화 추가 효과). 적 투사체는 null.</summary>
    public WeaponDef? Weapon;
    /// <summary>시야 조건 발사(적 쪽 — 공정성 수 세기에 쓴 값, 5.1).</summary>
    public bool SightGated;
    /// <summary>적 쪽 <c>on_end</c> 장판의 틱 피해(쏜 적의 배율로 발사 순간에 굳힌 최종 값, 5.2).</summary>
    public float OnEndDamage;

    /// <summary>이 적을 이미 맞혔나.</summary>
    public bool HasHit(EntityId id)
    {
        var n = Math.Min(HitCount, HitList.Capacity);
        for (var i = 0; i < n; i++)
            if (Hits[i] == id) return true;
        return false;
    }

    public void AddHit(EntityId id)
    {
        Hits[HitCount % HitList.Capacity] = id;
        HitCount++;
    }

    partial void ResetCombat()
    {
        Motion = MotionKind.Straight;
        Damage = DamagePct = CritChance = Knockback = 0f;
        HitFlags = DamageFlags.None;
        PierceLeft = 0;
        Target = EntityId.None;
        TurnRate = 0f;
        RangeLeft = float.PositiveInfinity;
        Speed = 0f;
        Hits = default;
        HitCount = 0;
        ExplodeRadius = ExplodeDamage = 0f;
        OrbitAngle = OrbitRadius = AngularSpeed = HitInterval = 0f;
        BouncesLeft = 0;
        BounceRange = 0f;
        Weapon = null;
        SightGated = false;
        OnEndDamage = 0f;
    }
}
