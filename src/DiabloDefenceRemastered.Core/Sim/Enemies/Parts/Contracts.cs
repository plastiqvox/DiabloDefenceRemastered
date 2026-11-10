using System.Runtime.CompilerServices;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

// 행동 부품 계약 (설계서 6.6, 소유 WP4). 부품 객체는 정의당 하나(매개변수만 가진 불변 객체)이고,
// 적마다 다른 값은 슬롯(PartSlot)·행동 상태(ActionState)·이동 지시(MoveOrder)에 둔다.

/// <summary>모든 행동 부품의 표시 인터페이스.</summary>
public interface IEnemyBehavior { }

/// <summary>독점 행동이 진행 중인가.</summary>
public enum ActionStatus : byte { Running, Done }

/// <summary>독점 행동이 끊긴 까닭.</summary>
public enum InterruptReason : byte { Death, Collapse, Culled, Fear, Phase }

/// <summary>능력(독점 행동): 조건 확인 후 시작, 끝날 때까지 그 부품의 <see cref="Tick"/> 만 돈다.</summary>
public interface IAbilityPart : IEnemyBehavior
{
    /// <summary>조건(거리·쿨·길 트임·또렷이 보임·HP 문턱) 확인 후 시작. 시작했으면 참.</summary>
    bool TryBegin(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action);
    ActionStatus Tick(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action);
    void Interrupt(in EnemyCtx ctx, ref PartSlot slot, ref ActionState action, InterruptReason why) { }
}

/// <summary>위치 잡기. false 면 다음 부품·기본 이동.</summary>
public interface IPositioningPart : IEnemyBehavior
{
    bool TryPosition(in EnemyCtx ctx, ref PartSlot slot, out MoveIntent intent);
}

/// <summary>지속 효과(오라·공포). 매 틱 한 번.</summary>
public interface IAuraPart : IEnemyBehavior
{
    void Tick(in EnemyCtx ctx, ref PartSlot slot);
}

/// <summary>생성 직후 한 번(<see cref="SpawnContext"/> 로 출처를 본다).</summary>
public interface ISpawnHookPart : IEnemyBehavior
{
    void OnSpawn(in EnemyCtx ctx, in SpawnContext spawn);
}

/// <summary>죽음 훅. <see cref="KillCause.Damage"/> 일 때만 불린다.</summary>
public interface IDeathHookPart : IEnemyBehavior
{
    void OnDeath(in EnemyCtx ctx);
}

/// <summary>나가는 피해 고치기(heavy_strike 넉백)·맞힌 뒤(흡혈, 다음 틱).</summary>
public interface IHitModifierPart : IEnemyBehavior
{
    void ModifyOutgoing(in EnemyCtx ctx, ref DamageRequest request);
    void OnHitLanded(in EnemyCtx ctx, in HitLanded hit) { }
}

/// <summary>동작(패턴·훅 안쪽). 이벤트 프레임(또는 훅 순간)에 한 번 <see cref="Begin"/>, 여러 틱 동작만 <see cref="Tick"/>.</summary>
public interface IEnemyAction
{
    void Begin(in EnemyCtx ctx, ref ActionState action);
    ActionStatus Tick(in EnemyCtx ctx, ref ActionState action) => ActionStatus.Done;
    /// <summary>여러 틱 걸리는 동작(dash)인가 — <c>actions</c> 의 마지막에만 올 수 있다(검사).</summary>
    bool MultiTick => false;
}

/// <summary><c>trigger: cooldown</c> 으로 감쌀 수 있는 동작(매개변수 <c>cooldown</c>).</summary>
public interface ITriggerCooldown
{
    float Cooldown { get; }
}

/// <summary>예고 검사(13.5-13)가 읽는 동작의 피해·범위 정보.</summary>
public interface IDamageInfo
{
    /// <summary>기본 피해(null = 그 적의 damage).</summary>
    float? BaseDamage { get; }
    /// <summary>범위 피해 반경(null = 단일 대상).</summary>
    float? AreaRadius { get; }
}

/// <summary>부품 슬롯 하나(적마다 12칸).</summary>
public struct PartSlot
{
    public float Cooldown;
    public float Timer;
    public int Counter;
    public bool Fired;
    public EntityId Ref;
}

/// <summary>적마다 부품 슬롯 12칸(정의 부품 + 모든 페이즈 패턴의 합집합 + 접두사 부품).</summary>
[InlineArray(SlotCount)]
public struct PartSlots
{
    public const int SlotCount = 12;
    private PartSlot _e0;

    public void Clear()
    {
        for (var i = 0; i < SlotCount; i++) this[i] = default;
    }
}

/// <summary>진행 중인 독점 행동. <see cref="PartIndex"/> −1 = 없음.</summary>
public struct ActionState
{
    public sbyte PartIndex;
    public sbyte ActionIndex;
    public byte Stage;
    public float T;
    public Vector2 Dir;
    public Vector2 Point;
    public EntityId Target;
    /// <summary>이번 동작이 피해를 한 번 냈음(돌진 닿음).</summary>
    public bool Hit;
    /// <summary>이 행동이 만드는 투사체가 시야 조건을 지켜야 함(<c>shoot_projectile</c>·<c>clear_sight</c> 패턴 — <c>EnemyProjectileSpawn.SightGated</c>).</summary>
    public bool SightGated;

    public readonly bool Active => PartIndex >= 0;

    public static ActionState None => new() { PartIndex = -1, ActionIndex = -1 };
}

/// <summary>이동 지시 종류(6.6). 공포만 능력을 막는다.</summary>
public enum OrderKind : byte { None, March, Follow, Flee }

/// <summary>이동 지시. 위치 잡기만 대신한다.</summary>
public struct MoveOrder
{
    public OrderKind Kind;
    public Vector2 Dir;
    public EntityId Leader;
    public Vector2 Offset;
    public int UntilTick;
    /// <summary>목표 문 행진의 진입점(월드 px) — 48px 안이거나 지나치면 풀림.</summary>
    public Vector2? Target;
}
