namespace DiabloDefenceRemastered.Core.Common;

/// <summary>
/// 종류별 개체 저장소. 밀집 배열(생성 순서)과 칸 표(핸들 → 밀집 인덱스)를 가진다.
/// - 새 개체는 밀집 배열 끝에 붙는다 → 밀집 순서 = 생성 순서(결정적, "가장 오래된 것부터 지우기" 가 앞에서부터).
/// - <see cref="Remove(EntityId)"/> 는 표시만 한다. <see cref="Compact"/>(EndTick)가 순서를 지키며 한꺼번에 뺀다.
/// - 밀집 인덱스는 다음 압축 전까지만 유효하다. 틱을 넘어 들고 있을 것은 <see cref="EntityId"/> 다.
/// </summary>
public sealed class EntityStore<T> where T : class, IEntity
{
    private readonly Pool<T> _pool;
    private T[] _dense;
    private int _count;
    private int[] _slotToDense;   // -1 = 빈 칸
    private int[] _slotGen;
    private readonly Stack<int> _freeSlots = new();
    private int _slotCount;
    private int _pendingRemovals;

    public EntityStore(Func<T> create, int capacity = 64)
    {
        _pool = new Pool<T>(create);
        capacity = Math.Max(capacity, 4);
        _dense = new T[capacity];
        _slotToDense = new int[capacity];
        _slotGen = new int[capacity];
        _pool.Prewarm(capacity);
    }

    /// <summary>압축 전 개체 수(지우기 표시된 것 포함).</summary>
    public int Count => _count;

    /// <summary>지우기 표시가 안 된 개체 수.</summary>
    public int LiveCount => _count - _pendingRemovals;

    public T this[int denseIndex] => _dense[denseIndex];

    /// <summary>밀집 배열(생성 순서). 압축 전까지만 유효하다.</summary>
    public ReadOnlySpan<T> Items => new(_dense, 0, _count);

    /// <summary>새 개체를 끝에 붙인다. 개체는 풀에서 꺼내 <see cref="IEntity.Reset"/> 한 상태다.</summary>
    public T Add()
    {
        int slot;
        if (_freeSlots.Count > 0) slot = _freeSlots.Pop();
        else
        {
            slot = _slotCount++;
            if (slot == _slotToDense.Length)
            {
                Array.Resize(ref _slotToDense, slot * 2);
                Array.Resize(ref _slotGen, slot * 2);
            }
        }
        if (_count == _dense.Length) Array.Resize(ref _dense, _count * 2);

        var gen = ++_slotGen[slot];
        var item = _pool.Get();
        item.Reset();
        item.Id = new EntityId(slot, gen);
        item.Removed = false;
        _dense[_count] = item;
        _slotToDense[slot] = _count;
        _count++;
        return item;
    }

    /// <summary>핸들이 지금 저장소에 있는 개체를 가리키면 그 밀집 인덱스. 아니면 -1(낡은 핸들·지운 개체 포함).</summary>
    public int IndexOf(EntityId id)
    {
        if (id.Gen <= 0 || (uint)id.Slot >= (uint)_slotCount || _slotGen[id.Slot] != id.Gen) return -1;
        var dense = _slotToDense[id.Slot];
        if (dense < 0) return -1;
        return _dense[dense].Removed ? -1 : dense;
    }

    public bool Contains(EntityId id) => IndexOf(id) >= 0;

    public bool TryGet(EntityId id, out T item)
    {
        var dense = IndexOf(id);
        if (dense < 0) { item = null!; return false; }
        item = _dense[dense];
        return true;
    }

    public T? Get(EntityId id) => TryGet(id, out var item) ? item : null;

    /// <summary>지우기 표시. 이번 틱 동안 밀집 배열에는 남고 <see cref="IndexOf"/>·<see cref="TryGet"/> 로는 안 보인다.</summary>
    public bool Remove(EntityId id)
    {
        var dense = IndexOf(id);
        if (dense < 0) return false;
        RemoveAt(dense);
        return true;
    }

    public void RemoveAt(int denseIndex)
    {
        var item = _dense[denseIndex];
        if (item.Removed) return;
        item.Removed = true;
        _pendingRemovals++;
    }

    /// <summary>표시된 개체를 빼고 남은 것을 순서대로 당긴다. 뺀 개체는 풀로, 칸은 다음 생성에 다시 쓴다.</summary>
    public void Compact()
    {
        if (_pendingRemovals == 0) return;
        var write = 0;
        for (var read = 0; read < _count; read++)
        {
            var item = _dense[read];
            if (item.Removed)
            {
                var slot = item.Id.Slot;
                _slotToDense[slot] = -1;
                _freeSlots.Push(slot);
                item.Reset();
                _pool.Return(item);
                continue;
            }
            if (write != read)
            {
                _dense[write] = item;
                _slotToDense[item.Id.Slot] = write;
            }
            write++;
        }
        Array.Clear(_dense, write, _count - write);
        _count = write;
        _pendingRemovals = 0;
    }

    /// <summary>모두 지운다(스테이지 다시 시작). 칸 세대는 유지해 예전 핸들이 계속 무효다.</summary>
    public void Clear()
    {
        for (var i = 0; i < _count; i++) RemoveAt(i);
        Compact();
    }
}
