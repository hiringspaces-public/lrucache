using LRUCache;
using Xunit;

namespace LRUCache.Tests;

/// <summary>
/// Phase 1 — Basic correctness.
///
/// These are the simplest tests. A correct implementation should pass all of them.
/// If any Phase 1 test fails, the implementation has a fundamental defect.
/// </summary>
public class BasicCorrectnessTests
{
    [Fact(DisplayName = "Get on empty cache returns -1")]
    public void Get_EmptyCache_ReturnsMinusOne()
    {
        var cache = new LRUCache<int, int>(2);
        Assert.Equal(-1, cache.TryGet(1, out int value) ? value : -1);
    }

    [Fact(DisplayName = "Put then Get returns the stored value")]
    public void Put_ThenGet_ReturnsValue()
    {
        var cache = new LRUCache<int, int>(2);
        cache.Put(1, 10);
        Assert.Equal(10, cache.TryGet(1, out int value) ? value : -1);
    }

    [Fact(DisplayName = "Get on a missing key returns -1")]
    public void Get_MissingKey_ReturnsMinusOne()
    {
        var cache = new LRUCache<int, int>(2);
        cache.Put(1, 10);
        Assert.Equal(-1, cache.TryGet(99, out int value) ? value : -1);
    }

    [Fact(DisplayName = "Put updates an existing key's value")]
    public void Put_ExistingKey_UpdatesValue()
    {
        var cache = new LRUCache<int, int>(2);
        cache.Put(1, 10);
        cache.Put(1, 20);
        Assert.Equal(20, cache.TryGet(1, out int value) ? value : -1);
    }

    [Fact(DisplayName = "Count reflects the number of stored entries")]
    public void Count_ReflectsStoredEntries()
    {
        var cache = new LRUCache<int, int>(3);
        Assert.Equal(0, cache.Count);
        cache.Put(1, 1);
        Assert.Equal(1, cache.Count);
        cache.Put(2, 2);
        Assert.Equal(2, cache.Count);
    }

    [Fact(DisplayName = "Inserting capacity+1 items evicts one entry")]
    public void Put_OverCapacity_EvictsOneEntry()
    {
        var cache = new LRUCache<int, int>(2);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3); // should evict key 1 (LRU)

        Assert.Equal(2, cache.Count);
    }

    [Fact(DisplayName = "Cache with capacity 1 holds exactly one item")]
    public void Capacity1_HoldsOneItem()
    {
        var cache = new LRUCache<int,int>(1);
        cache.Put(1, 10);
        cache.Put(2, 20);

        Assert.Equal(-1, cache.TryGet(1, out int value) ? value : -1);
        Assert.Equal(20, cache.TryGet(2, out int value1) ? value1 : -1);
    }
}