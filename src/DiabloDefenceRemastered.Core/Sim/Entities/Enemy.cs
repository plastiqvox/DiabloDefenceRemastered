using System.Runtime.CompilerServices;

namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>접두사 번호 두 칸(정의 목록 번호, -1 = 없음).</summary>
[InlineArray(2)]
public struct AffixPair
{
    private short _e0;
}

/// <summary>
/// 적 — 공통 조각 (WP1, 설계서 11.2 표가 정본). 풀에서 다시 쓰는 클래스다.
/// 한 묶음만 쓰는 상태는 같은 namespace 의 조각 파일에 둔다:
/// <c>Sim/Movement/Enemy.Nav.cs</c>(WP2, <see cref="ResetNav"/>), <c>Sim/Combat/Enemy.Combat.cs</c>(WP3, <see cref="ResetCombat"/>),
/// <c>Sim/Enemies/Enemy.Behavior.cs</c>(WP4, <see cref="ResetBehavior"/>). 옆 사전으로 상태를 붙이지 않는다.
/// </summary>
public sealed partial class Enemy : IEntity
{
    public EntityId Id { get; set; }
    public bool Removed { get; set; }

    // 쓰기 WP4 / 읽기 모두
    public EnemyDef Def = null!;
    /// <summary><c>Defs.Enemies</c> 안 번호(틱 이벤트 DefIndex).</summary>
    public int DefIndex;
    public Faction Faction;
    public EnemyRank Rank;
    public AffixPair AffixIdx;
    public int SpawnTick;
    public LifeState Life;

    // 쓰기 WP2(이동)·WP4(생성·순간 이동)
    public Vector2 Pos;
    public Vector2 PrevPos;
    public Vector2 Vel;
    public float Radius;

    // 쓰기 WP3(피해)·WP4(생성·Collapse·Culled)
    public float Hp;
    public float MaxHp;
    public bool Dead;
    public KillCause KillCause;
    /// <summary>마지막으로 때린 적(EnemyKilled.Attacker). 주인공 무기면 None.</summary>
    public EntityId KilledBy;

    // 쓰기 WP4(생성·버프) / 읽기 WP3
    public float Armor;
    public float KnockbackResist;
    public Immunity Immunities;
    /// <summary>생성 때 굳힌 피해 배율 = 시간 × 등급 × (1 + 접두사 damage_pct). 버프%·광폭화는 낼 때마다.</summary>
    public float DamageMul;

    // 쓰기 WP4 / 읽기 WP2
    public MoveIntent Intent;
    /// <summary>정의 × 등급 × (1 + 접두사·버프 speed_pct 합) × 광폭화. 매 틱 WP4 가 쓴다.</summary>
    public float MoveSpeed;
    public bool PassThroughObstacles;

    // 쓰기 WP2 / 읽기 WP4
    /// <summary>희망 방향으로 실제 이동이 막힌 시간(초).</summary>
    public float MoveBlockedTime;

    // 쓰기 WP3 / 읽기 WP2·WP5
    public Vector2 KnockbackVel;
    public float KnockbackTime;
    public float HitFlashTime;

    // 쓰기 WP4 / 읽기 WP5
    /// <summary>바라보는 방향(단위 벡터). 시트 열은 <see cref="FacingMath.ToColumn"/>.</summary>
    public Vector2 Facing;
    public AnimClock Anim;
    public EnemyVisual Visual;

    // 쓰기 WP4(생성 때 한 번) / 읽기 WP4(SeenClearly)·FogMath (v0.5.1, 설계서 4.11·6.6)
    /// <summary>몸 높이 px = 그림 묶음 <c>body_height</c> × 최종 크기. "또렷이 보임" 의 몸 위 끝(발 y − 이 값).</summary>
    public float BodyTop;
    /// <summary>닿은 거리 여분 = max(<c>rules.sight.touch_margin</c>, 단일 대상 근접 사거리 − 두 반경 + 10). 두 반경 + 이 값 안이면 안개와 상관없이 그린다.</summary>
    public float TouchReach;

    // 쓰기 WP3(장판 둔화)·WP4(오라·함성) — BuffSlots 도우미로만
    public BuffSlots Buffs;

    /// <summary>살아 있고 저장소에서 빠지지 않았다.</summary>
    public bool IsAlive => !Dead && !Removed;

    /// <summary>조준·맞음·분리 대상(입장·등장 중은 아니다, 4.8).</summary>
    public bool IsTargetable => IsAlive && Life is LifeState.Active or LifeState.Stunned;

    public void Reset()
    {
        Id = EntityId.None;
        Removed = false;
        Def = null!;
        DefIndex = -1;
        Faction = Faction.Enemy;
        Rank = EnemyRank.Normal;
        AffixIdx[0] = -1;
        AffixIdx[1] = -1;
        SpawnTick = 0;
        Life = LifeState.Entering;
        Pos = PrevPos = Vel = Vector2.Zero;
        Radius = 0f;
        Hp = MaxHp = 0f;
        Dead = false;
        KillCause = KillCause.Damage;
        KilledBy = EntityId.None;
        Armor = KnockbackResist = 0f;
        Immunities = Immunity.None;
        DamageMul = 1f;
        Intent = MoveIntent.Hold;
        MoveSpeed = 0f;
        PassThroughObstacles = false;
        MoveBlockedTime = 0f;
        KnockbackVel = Vector2.Zero;
        KnockbackTime = 0f;
        HitFlashTime = 0f;
        Facing = new Vector2(0, 1);
        Anim = default;
        Visual = new EnemyVisual { AlphaMul = 1f };
        BodyTop = TouchReach = 0f;
        Buffs.Clear();
        ResetNav();
        ResetCombat();
        ResetBehavior();
    }

    /// <summary>WP2 조각(Enemy.Nav.cs)의 상태 지우기.</summary>
    partial void ResetNav();

    /// <summary>WP3 조각(Enemy.Combat.cs)의 상태 지우기.</summary>
    partial void ResetCombat();

    /// <summary>WP4 조각(Enemy.Behavior.cs)의 상태 지우기.</summary>
    partial void ResetBehavior();
}
