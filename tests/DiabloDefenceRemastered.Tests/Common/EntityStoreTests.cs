namespace DiabloDefenceRemastered.Tests.Common;

public class EntityStoreTests
{
    private static EntityStore<Enemy> NewStore(int capacity = 4) => new(() => new Enemy(), capacity);

    [Fact]
    public void Add_AppendsInCreationOrder_WithValidHandles()
    {
        var store = NewStore();
        var a = store.Add();
        var b = store.Add();
        var c = store.Add();
        Assert.Equal(3, store.Count);
        Assert.Same(a, store[0]);
        Assert.Same(c, store[2]);
        Assert.Equal(1, store.IndexOf(b.Id));
        Assert.True(store.TryGet(c.Id, out var got) && ReferenceEquals(got, c));
        Assert.False(a.Id.IsNone);
    }

    [Fact]
    public void Remove_MarksOnly_UntilCompact()
    {
        var store = NewStore();
        var a = store.Add();
        var b = store.Add();
        var c = store.Add();

        Assert.True(store.Remove(b.Id));
        Assert.Equal(3, store.Count);              // 압축 전에는 밀집 배열에 남는다
        Assert.Equal(2, store.LiveCount);
        Assert.Equal(-1, store.IndexOf(b.Id));     // 하지만 핸들로는 안 보인다
        Assert.True(b.Removed);

        store.Compact();
        Assert.Equal(2, store.Count);
        Assert.Same(a, store[0]);
        Assert.Same(c, store[1]);                  // 순서를 지키며 당긴다
        Assert.Equal(1, store.IndexOf(c.Id));
    }

    [Fact]
    public void StaleHandle_IsInvalid_AfterSlotReuse()
    {
        var store = NewStore();
        var a = store.Add();
        var oldId = a.Id;
        store.Remove(oldId);
        store.Compact();

        var d = store.Add();
        Assert.Equal(oldId.Slot, d.Id.Slot);       // 칸은 다시 쓰고
        Assert.NotEqual(oldId.Gen, d.Id.Gen);      // 세대가 바뀐다
        Assert.False(store.Contains(oldId));
        Assert.Null(store.Get(oldId));
        Assert.True(store.Contains(d.Id));
        Assert.False(store.Remove(oldId));
    }

    [Fact]
    public void Pool_ReusesObject_AndResetsIt()
    {
        var store = NewStore();
        var a = store.Add();
        a.Hp = 55f;
        a.Dead = true;
        store.Remove(a.Id);
        store.Compact();

        var b = store.Add();
        Assert.Same(a, b);
        Assert.Equal(0f, b.Hp);
        Assert.False(b.Dead);
        Assert.False(b.Removed);
        Assert.Equal(-1, b.AffixIdx[0]);
    }

    [Fact]
    public void Compact_ManyRemovals_KeepsOrderAndHandles()
    {
        var store = NewStore(2);
        var items = new List<Enemy>();
        for (var i = 0; i < 100; i++)
        {
            var e = store.Add();
            e.Hp = i;
            items.Add(e);
        }
        for (var i = 0; i < 100; i += 3) store.Remove(items[i].Id);
        store.Compact();

        var expected = items.Where((_, i) => i % 3 != 0).ToList();
        Assert.Equal(expected.Count, store.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Same(expected[i], store[i]);
            Assert.Equal(i, store.IndexOf(expected[i].Id));
        }
    }

    [Fact]
    public void Clear_InvalidatesEverything()
    {
        var store = NewStore();
        var a = store.Add();
        store.Add();
        store.Clear();
        Assert.Equal(0, store.Count);
        Assert.False(store.Contains(a.Id));
    }

    [Fact]
    public void EntityId_SpecialValues()
    {
        Assert.True(EntityId.None.IsNone);
        Assert.True(default(EntityId).IsNone);
        Assert.True(EntityId.Hero.IsHero);
        Assert.False(EntityId.Hero.IsNone);
    }

    [Fact]
    public void Pool_GetReturn()
    {
        var created = 0;
        var pool = new Pool<object>(() => { created++; return new object(); });
        var o = pool.Get();
        pool.Return(o);
        Assert.Same(o, pool.Get());
        Assert.Equal(1, created);
    }
}
