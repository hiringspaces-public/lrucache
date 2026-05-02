using LRUCache;
using Xunit;

namespace LRUCache.Tests;

[Trait("Phase", "2")]
public class EvictionOrderTests
{
    [Fact(DisplayName = "Oldest untouched item is evicted first")]
    public void Eviction_OldestUntouched_EvictedFirst()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);
        cache.Put(4, 4);

        Assert.Equal(-1, cache.TryGet(1, out var val1) ? val1 : -1);
        Assert.Equal(2,  cache.TryGet(2, out var val2) ? val2 : -1);
        Assert.Equal(3,  cache.TryGet(3, out var val3) ? val3 : -1);
        Assert.Equal(4,  cache.TryGet(4, out var val4) ? val4 : -1);
    }

    [Fact(DisplayName = "Get promotes a key — it survives the next eviction")]
    public void Get_PromotesKey_SurvivesEviction()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        cache.TryGet(1, out _);

        cache.Put(4, 4);

        Assert.Equal(1,  cache.TryGet(1, out var val1) ? val1 : -1);
        Assert.Equal(-1, cache.TryGet(2, out var val2) ? val2 : -1);
        Assert.Equal(3,  cache.TryGet(3, out var val3) ? val3 : -1);
        Assert.Equal(4,  cache.TryGet(4, out var val4) ? val4 : -1);
    }

    [Fact(DisplayName = "Get on LRU saves it through two subsequent evictions")]
    public void Get_OnLRU_SavesItThroughTwoEvictions()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        cache.TryGet(1, out _);

        cache.Put(4, 4);
        cache.Put(5, 5);

        Assert.True(cache.TryGet(1, out var v) && v == 1);
        Assert.False(cache.TryGet(2, out _));
        Assert.False(cache.TryGet(3, out _));
        Assert.True(cache.TryGet(4, out _));
        Assert.True(cache.TryGet(5, out _));
    }

    [Fact(DisplayName = "Repeated Get on same key does not corrupt the list")]
    public void Get_RepeatedAccess_DoesNotCorruptList()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        cache.TryGet(1, out _);
        cache.TryGet(1, out _);
        cache.TryGet(1, out _);

        cache.Put(4, 4);
        cache.Put(5, 5);

        Assert.Equal(3, cache.Count);
        Assert.True(cache.TryGet(1, out var v1) && v1 == 1);
        Assert.True(cache.TryGet(4, out _));
        Assert.True(cache.TryGet(5, out _));
        Assert.False(cache.TryGet(2, out _), "Key 2 should be evicted");
        Assert.False(cache.TryGet(3, out _), "Key 3 should be evicted");
    }

    [Fact(DisplayName = "Put on existing key promotes it — it survives the next eviction")]
    public void Put_ExistingKey_PromotesKey_SurvivesEviction()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        cache.Put(1, 99);

        cache.Put(4, 4);

        Assert.Equal(99, cache.TryGet(1, out var val1) ? val1 : -1);
        Assert.Equal(-1, cache.TryGet(2, out var val2) ? val2 : -1);
    }

    [Fact(DisplayName = "Multiple Gets update recency correctly")]
    public void MultipleGets_UpdateRecencyCorrectly()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        cache.TryGet(1, out _);
        cache.TryGet(2, out _);

        cache.Put(4, 4);

        Assert.Equal(-1, cache.TryGet(3, out var val3) ? val3 : -1);
        Assert.Equal(1,  cache.TryGet(1, out var val4) ? val4 : -1);
        Assert.Equal(2,  cache.TryGet(2, out var val5) ? val5 : -1);
        Assert.Equal(4,  cache.TryGet(4, out var val6) ? val6 : -1);
    }

    [Fact(DisplayName = "Put on existing key makes it the new MRU")]
    public void Put_ExistingKey_MustBecomeNewMRU()
    {
        var cache = new LRUCache<int, int>(2);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(1, 99);
        cache.Put(3, 3);

        Assert.False(cache.TryGet(2, out _), "Key 2 should be evicted — it became LRU after Put(1,99)");
        Assert.True(cache.TryGet(1, out var v) && v == 99, "Key 1 should survive with updated value");
        Assert.True(cache.TryGet(3, out _), "Key 3 should be present");
    }

    [Fact(DisplayName = "Frequent Get on same key does not prevent correct eviction of others")]
    public void FrequentGet_SameKey_DoesNotAffectOthers()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        for (int i = 0; i < 100; i++)
            cache.TryGet(1, out _);

        cache.Put(4, 4);

        Assert.Equal(-1, cache.TryGet(2, out var val1) ? val1 : -1);
        Assert.Equal(1,  cache.TryGet(1, out var val2) ? val2 : -1);
        Assert.Equal(3,  cache.TryGet(3, out var val3) ? val3 : -1);
        Assert.Equal(4,  cache.TryGet(4, out var val4) ? val4 : -1);
    }

    [Fact(DisplayName = "Full eviction sequence matches LRU contract")]
    public void FullEvictionSequence_MatchesLRUContract()
    {
        var cache = new LRUCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);
        cache.Put(4, 4);
        cache.Put(5, 5);
        cache.Put(6, 6);

        Assert.Equal(-1, cache.TryGet(1, out var val1) ? val1 : -1);
        Assert.Equal(-1, cache.TryGet(2, out var val2) ? val2 : -1);
        Assert.Equal(-1, cache.TryGet(3, out var val3) ? val3 : -1);
        Assert.Equal(4,  cache.TryGet(4, out var val4) ? val4 : -1);
        Assert.Equal(5,  cache.TryGet(5, out var val5) ? val5 : -1);
        Assert.Equal(6,  cache.TryGet(6, out var val6) ? val6 : -1);
    }
}
