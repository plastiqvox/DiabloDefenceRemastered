namespace DiabloDefenceRemastered.Core.Common;

/// <summary>단순 객체 풀. 꺼낼 때 새로 만들지 않도록 미리 채울 수 있다.</summary>
public sealed class Pool<T>(Func<T> create) where T : class
{
    private readonly Stack<T> _free = new();

    public int FreeCount => _free.Count;

    public void Prewarm(int count)
    {
        for (var i = 0; i < count; i++) _free.Push(create());
    }

    public T Get() => _free.TryPop(out var item) ? item : create();

    public void Return(T item) => _free.Push(item);
}
