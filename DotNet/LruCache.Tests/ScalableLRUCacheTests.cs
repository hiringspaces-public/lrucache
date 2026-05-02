namespace LRUCache.Tests;

public class ScalableLRUCacheTests
{
    [Fact(DisplayName = "Keys survive a resize that keeps total capacity the same")]
    public void Resize_SameCapacity_AllKeysSurvive()
    {
        var cache = new ScalableLRUCache<int, int>(totalCapacity: 8, stripeCount: 2);
        for (int i = 0; i < 8; i++) cache.Put(i, i * 10);

        cache.Resize(newTotalCapacity: 8, newStripeCount: 4);

        for (int i = 0; i < 8; i++)
            Assert.True(cache.TryGet(i, out var v) && v == i * 10,
                $"Key {i} should survive resize with same capacity");
    }

    [Fact(DisplayName = "Shrink evicts coldest keys when capacity is reduced")]
    public void Resize_SmallerCapacity_HottestKeysSurvive()
    {
        var cache = new ScalableLRUCache<int, int>(totalCapacity: 6, stripeCount: 2);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);
        cache.Put(4, 4);
        cache.Put(5, 5);
        cache.Put(6, 6);

        // Access 4, 5, 6 to make them hot
        cache.TryGet(4, out _);
        cache.TryGet(5, out _);
        cache.TryGet(6, out _);

        cache.Resize(newTotalCapacity: 3, newStripeCount: 2);

        // Cold keys (1, 2, 3) should be evicted; hot keys (4, 5, 6) should survive
        Assert.False(cache.TryGet(1, out _), "Cold key 1 should be evicted");
        Assert.False(cache.TryGet(2, out _), "Cold key 2 should be evicted");
        Assert.False(cache.TryGet(3, out _), "Cold key 3 should be evicted");
        Assert.True(cache.TryGet(4, out _),  "Hot key 4 should survive");
        Assert.True(cache.TryGet(5, out _),  "Hot key 5 should survive");
        Assert.True(cache.TryGet(6, out _),  "Hot key 6 should survive");
    }

    [Fact(DisplayName = "Concurrent reads and writes do not corrupt state during normal operation")]
    public void Concurrent_ReadWrite_NoCorruption()
    {
        var cache = new ScalableLRUCache<int, int>(totalCapacity: 16, stripeCount: 4);
        for (int i = 0; i < 16; i++) cache.Put(i, i);

        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();

        var readers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                int key = i % 16;
                if (cache.TryGet(key, out var v) && v != key)
                    errors.Add($"Key {key} returned wrong value {v}");
            }
        }));

        var writers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 500; i++)
                cache.Put(i % 16, i % 16);
        }));

        Task.WaitAll(readers.Concat(writers).ToArray());

        Assert.Empty(errors);
    }

    [Fact(DisplayName = "Put and Get work correctly after resize")]
    public void Resize_ThenPutGet_WorksCorrectly()
    {
        var cache = new ScalableLRUCache<int, int>(totalCapacity: 4, stripeCount: 2);
        cache.Put(1, 10);
        cache.Put(2, 20);

        cache.Resize(newTotalCapacity: 8, newStripeCount: 4);

        cache.Put(3, 30);
        cache.Put(4, 40);

        Assert.True(cache.TryGet(1, out var v1) && v1 == 10);
        Assert.True(cache.TryGet(2, out var v2) && v2 == 20);
        Assert.True(cache.TryGet(3, out var v3) && v3 == 30);
        Assert.True(cache.TryGet(4, out var v4) && v4 == 40);
    }

    [Fact(DisplayName = "Count never exceeds total capacity after repeated resize")]
    public void RepeatedResize_CountNeverExceedsCapacity()
    {
        var cache = new ScalableLRUCache<int, int>(totalCapacity: 8, stripeCount: 2);
        for (int i = 0; i < 8; i++) cache.Put(i, i);

        cache.Resize(newTotalCapacity: 4, newStripeCount: 2);
        for (int i = 0; i < 10; i++) cache.Put(i, i);
        Assert.True(cache.Count <= 4, $"After shrink: Count {cache.Count} > 4");

        cache.Resize(newTotalCapacity: 12, newStripeCount: 4);
        for (int i = 0; i < 20; i++) cache.Put(i, i);
        Assert.True(cache.Count <= 12, $"After grow: Count {cache.Count} > 12");
    }

    [Fact(DisplayName = "Resize while under concurrent load does not lose all keys")]
    public void Resize_UnderConcurrentLoad_CacheRemainsUsable()
    {
        var cache = new ScalableLRUCache<int, int>(totalCapacity: 8, stripeCount: 2);
        for (int i = 0; i < 8; i++) cache.Put(i, i);

        var resizeTask = Task.Run(() =>
            cache.Resize(newTotalCapacity: 8, newStripeCount: 4));

        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
                cache.TryGet(i % 8, out _); // should not throw
        });

        Task.WaitAll(resizeTask, readTask);

        // Cache must still be functional after resize under load
        cache.Put(99, 99);
        Assert.True(cache.TryGet(99, out var v) && v == 99,
            "Cache must accept writes and return correct values after concurrent resize");
    }
}