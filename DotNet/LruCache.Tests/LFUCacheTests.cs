using LRUCache;
using Xunit;

namespace LRUCache.Tests;

[Trait("Phase", "3")]
public class LFUCacheTests
{
    [Fact(DisplayName = "Put then Get returns the stored value")]
    public void Put_ThenGet_ReturnsValue()
    {
        var cache = new LFUCache<int, int>(2);
        cache.Put(1, 10);

        Assert.True(cache.TryGet(1, out var v) && v == 10);
    }

    [Fact(DisplayName = "Least-frequent key is evicted, not least-recent")]
    public void Eviction_LeastFrequent_NotLeastRecent()
    {
        var cache = new LFUCache<int, int>(2);
        cache.Put(1, 1);
        cache.Put(2, 2);

        cache.TryGet(1, out _);

        cache.Put(3, 3);

        Assert.True(cache.TryGet(1, out _));
        Assert.False(cache.TryGet(2, out _));
        Assert.True(cache.TryGet(3, out _));
    }

    [Fact(DisplayName = "Key accessed many times survives multiple evictions")]
    public void FrequentlyAccessedKey_SurvivesMultipleEvictions()
    {
        var cache = new LFUCache<int, int>(2);
        cache.Put(1, 1);
        cache.Put(2, 2);

        cache.TryGet(1, out _);
        cache.TryGet(1, out _);
        cache.TryGet(1, out _);

        cache.Put(3, 3);
        cache.Put(4, 4);

        Assert.True(cache.TryGet(1, out var v) && v == 1);
        Assert.False(cache.TryGet(2, out _));
        Assert.False(cache.TryGet(3, out _));
        Assert.True(cache.TryGet(4, out _));
    }

    [Fact(DisplayName = "Frequency tie is broken by LRU order")]
    public void Eviction_SameFrequency_EvictsLeastRecentlyUsed()
    {
        var cache = new LFUCache<int, int>(2);
        cache.Put(1, 1);
        cache.Put(2, 2);

        cache.TryGet(1, out _);
        cache.TryGet(2, out _);

        cache.Put(3, 3);

        Assert.False(cache.TryGet(1, out _));
        Assert.True(cache.TryGet(2, out _));
        Assert.True(cache.TryGet(3, out _));
    }

    [Fact(DisplayName = "Put on existing key updates value and increments its frequency")]
    public void Put_ExistingKey_UpdatesValueAndFrequency()
    {
        var cache = new LFUCache<int, int>(2);
        cache.Put(1, 1);
        cache.Put(2, 2);

        cache.Put(1, 99);

        cache.Put(3, 3);

        Assert.True(cache.TryGet(1, out var v) && v == 99);
        Assert.False(cache.TryGet(2, out _));
        Assert.True(cache.TryGet(3, out _));
    }

    [Fact(DisplayName = "New insert resets minFreq to 1")]
    public void NewInsert_ResetsMinFreqToOne()
    {
        var cache = new LFUCache<int, int>(2);
        cache.Put(1, 1);
        cache.Put(2, 2);

        cache.TryGet(1, out _);
        cache.TryGet(1, out _);
        cache.TryGet(2, out _);

        cache.Put(3, 3);
        cache.Put(4, 4);

        Assert.True(cache.TryGet(1, out var v) && v == 1);
        Assert.False(cache.TryGet(3, out _));
        Assert.True(cache.TryGet(4, out _));
    }

    [Fact(DisplayName = "Count reflects the number of stored entries")]
    public void Count_ReflectsStoredEntries()
    {
        var cache = new LFUCache<int, int>(3);

        Assert.Equal(0, cache.Count);
        cache.Put(1, 1);
        Assert.Equal(1, cache.Count);
        cache.Put(2, 2);
        cache.Put(3, 3);
        Assert.Equal(3, cache.Count);

        cache.Put(4, 4);
        Assert.Equal(3, cache.Count);
    }

    [Fact(DisplayName = "Get on missing key returns false")]
    public void Get_MissingKey_ReturnsFalse()
    {
        var cache = new LFUCache<int, int>(2);
        cache.Put(1, 1);

        Assert.False(cache.TryGet(99, out _));
    }


    [Fact(DisplayName = "Eviction order is approximately maintained under concurrent access")]
    public void ConcurrentEviction_HotKeysSurvive()
    {
        var cache = new LFUCache<int, int>(10);
        for (int i = 0; i < 10; i++) cache.Put(i, i);

        // Continuously access keys 0-4 (hot) while flooding with new keys (cold)
        var hotReader = Task.Run(() =>
        {
            for (int i = 0; i < 5000; i++)
                cache.TryGet(i % 5, out _);
        });

        var coldWriter = Task.Run(() =>
        {
            for (int i = 10; i < 2000; i++)
                cache.Put(i, i);
        });

        Task.WaitAll(hotReader, coldWriter);

        // After flooding, at least some hot keys should survive
        int survived = Enumerable.Range(0, 5).Count(k => cache.TryGet(k, out _));
        Assert.True(survived > 0,
            "Expected at least some hot keys to survive eviction flood");
    }

}
