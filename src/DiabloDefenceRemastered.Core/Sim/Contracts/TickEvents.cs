namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>할당 없이 재사용하는 구조체 목록(틱 이벤트·표시 이벤트·요청 큐). 순서 = 넣은 순서.</summary>
public sealed class EventList<T>(int capacity = 32) where T : struct
{
    private T[] _items = new T[Math.Max(capacity, 4)];

    public int Count { get; private set; }

    public ref readonly T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return ref _items[index];
        }
    }

    public ReadOnlySpan<T> AsSpan() => new(_items, 0, Count);

    public void Add(in T item)
    {
        if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
        _items[Count++] = item;
    }

    public void Clear()
    {
        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            Array.Clear(_items, 0, Count);
        Count = 0;
    }
}

// ── 틱 이벤트 형(시뮬레이션 안, 설계서 2.1·3.5). 한 사건에 이름 하나 ─────────────────────────

/// <summary>적이 죽었다(DeathHooks 13 이 낸다). DeathRewards 14 는 Cause == Damage 만 본다.</summary>
public readonly record struct EnemyKilled(EntityId Id, int DefIndex, Vector2 Pos, EnemyRank Rank, KillCause Cause, EntityId Attacker);

/// <summary>피해가 실제로 들어갔다(DamageResolve 12). 흡혈처럼 실제 피해가 필요한 것은 다음 틱 EnemyThink 가 읽는다.</summary>
public readonly record struct HitLanded(EntityId Attacker, EntityId Target, int Applied);

/// <summary>주인공이 피해를 받았다.</summary>
public readonly record struct HeroDamaged(EntityId Attacker, int Applied, int HpAfter);

/// <summary>경험치를 얻었다(보석 줍기).</summary>
public readonly record struct XpGained(int Amount, int Total);

/// <summary>레벨이 올랐다(선택 대기). 여러 레벨이 한 번에 오르면 레벨마다 하나.</summary>
public readonly record struct LevelUpReady(int NewLevel);

/// <summary>보스(rank: boss)가 Damage 로 죽었다.</summary>
public readonly record struct BossDefeated(EntityId Id, int DefIndex, Vector2 Pos);

/// <summary>한 틱 동안 쓴 이벤트 묶음.</summary>
public sealed class TickEventSet
{
    public EventList<EnemyKilled> EnemyKilled { get; } = new(64);
    public EventList<HitLanded> HitLanded { get; } = new(128);
    public EventList<HeroDamaged> HeroDamaged { get; } = new(8);
    public EventList<XpGained> XpGained { get; } = new(32);
    public EventList<LevelUpReady> LevelUpReady { get; } = new(4);
    public EventList<BossDefeated> BossDefeated { get; } = new(2);

    public void Clear()
    {
        EnemyKilled.Clear();
        HitLanded.Clear();
        HeroDamaged.Clear();
        XpGained.Clear();
        LevelUpReady.Clear();
        BossDefeated.Clear();
    }
}

/// <summary>
/// 두 칸 틱 이벤트 큐 (설계서 3.5). 이번 틱에 쓴 것(<see cref="Current"/>)은 같은 틱의 뒤 단계와 다음 틱 전체(<see cref="Previous"/>)가 읽는다.
/// BeginTick 이 바꿔 끼우고 오래된 칸을 비운다.
/// </summary>
public sealed class TickEvents
{
    public TickEventSet Current { get; private set; } = new();
    public TickEventSet Previous { get; private set; } = new();

    public void Swap()
    {
        (Current, Previous) = (Previous, Current);
        Current.Clear();
    }

    public void Clear()
    {
        Current.Clear();
        Previous.Clear();
    }
}
