namespace DiabloDefenceRemastered.Core.Nav;

/// <summary>
/// 칸 번호 이진 힙 (설계서 4.3·4.4, 소유 WP2). 칸 번호 → 힙 위치 표로 decrease-key 를 한다. 키가 같으면 둘째 키(A* 의 h)가 작은 쪽,
/// 그것도 같으면 칸 번호가 작은 쪽 → 결정적. 배열은 만들 때 한 번 잡고 <see cref="Clear"/> 는 들어 있던 것만 지운다(할당 0).
/// </summary>
internal sealed class IndexedMinHeap
{
    private readonly int[] _heap;
    private readonly int[] _pos;   // 칸 → 힙 위치(−1 = 없음)
    private readonly float[] _key;
    private readonly float[] _tie;
    private int _count;

    public IndexedMinHeap(int capacity)
    {
        _heap = new int[capacity];
        _pos = new int[capacity];
        _key = new float[capacity];
        _tie = new float[capacity];
        Array.Fill(_pos, -1);
    }

    public int Count => _count;

    public void Clear()
    {
        for (var i = 0; i < _count; i++) _pos[_heap[i]] = -1;
        _count = 0;
    }

    public bool Contains(int item) => _pos[item] >= 0;

    /// <summary>넣거나, 이미 있으면 키를 줄인다(더 크면 그대로).</summary>
    public void Push(int item, float key, float tie = 0f)
    {
        var p = _pos[item];
        if (p >= 0)
        {
            if (!Less(key, tie, item, _key[item], _tie[item], item)) return;
            _key[item] = key;
            _tie[item] = tie;
            Up(p);
            return;
        }
        _key[item] = key;
        _tie[item] = tie;
        _heap[_count] = item;
        _pos[item] = _count;
        Up(_count++);
    }

    public int Pop()
    {
        var top = _heap[0];
        _pos[top] = -1;
        _count--;
        if (_count > 0)
        {
            var last = _heap[_count];
            _heap[0] = last;
            _pos[last] = 0;
            Down(0);
        }
        return top;
    }

    private bool Less(int a, int b) => Less(_key[a], _tie[a], a, _key[b], _tie[b], b);

    private static bool Less(float ka, float ta, int a, float kb, float tb, int b) =>
        ka < kb || (ka == kb && (ta < tb || (ta == tb && a < b)));

    private void Up(int i)
    {
        var item = _heap[i];
        while (i > 0)
        {
            var parent = (i - 1) >> 1;
            var p = _heap[parent];
            if (!Less(item, p)) break;
            _heap[i] = p;
            _pos[p] = i;
            i = parent;
        }
        _heap[i] = item;
        _pos[item] = i;
    }

    private void Down(int i)
    {
        var item = _heap[i];
        while (true)
        {
            var l = 2 * i + 1;
            if (l >= _count) break;
            var r = l + 1;
            var c = r < _count && Less(_heap[r], _heap[l]) ? r : l;
            if (!Less(_heap[c], item)) break;
            _heap[i] = _heap[c];
            _pos[_heap[i]] = i;
            i = c;
        }
        _heap[i] = item;
        _pos[item] = i;
    }
}
