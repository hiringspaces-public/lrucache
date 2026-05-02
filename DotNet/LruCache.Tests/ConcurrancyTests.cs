using LRUCache;
using Xunit;

namespace LRUCache.Tests;

[Trait("Phase", "4")]
public class ConcurrencyTests
{
    [Fact(DisplayName = "Concurrent Puts never exceed capacity")]
    public async Task ConcurrentPuts_NeverExceedCapacity()
    {
        const int capacity = 100;
        const int threads  = 20;
        const int opsEach  = 500;

        var cache   = new LRUCache<int, int>(capacity);
        var barrier = new Barrier(threads);

        var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var rng = new Random(t);
            barrier.SignalAndWait();
            for (int i = 0; i < opsEach; i++)
                cache.Put(rng.Next(0, capacity * 3), rng.Next());
        }));

        await Task.WhenAll(tasks);

        Assert.True(cache.Count <= capacity,
            $"Cache exceeded capacity: Count={cache.Count}, Capacity={capacity}");
    }

    [Fact(DisplayName = "Concurrent Gets do not crash or return corrupt values")]
    public async Task ConcurrentGets_DoNotCrashOrCorrupt()
    {
        const int capacity = 50;
        const int threads  = 10;

        var cache = new LRUCache<int, int>(capacity);
        for (int i = 0; i < capacity; i++)
            cache.Put(i, i * 2);

        var barrier    = new Barrier(threads);
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            try
            {
                var rng = new Random();
                barrier.SignalAndWait();
                for (int i = 0; i < 1_000; i++)
                {
                    int key = rng.Next(0, capacity);
                    if (cache.TryGet(key, out int val))
                        Assert.Equal(key * 2, val);
                }
            }
            catch (Exception ex) { exceptions.Add(ex); }
        }));

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);
    }

    [Fact(DisplayName = "Concurrent Put and Get on the same key — no list corruption")]
    public async Task ConcurrentPutAndGet_SameKey_NoListCorruption()
    {
        var cache = new LRUCache<int, int>(10);
        cache.Put(1, 100);

        var barrier    = new Barrier(2);
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var writer = Task.Run(() =>
        {
            try
            {
                barrier.SignalAndWait();
                for (int i = 0; i < 10_000; i++)
                    cache.Put(1, (i % 2 == 0) ? 100 : 200);
            }
            catch (Exception ex) { exceptions.Add(ex); }
        });

        var reader = Task.Run(() =>
        {
            try
            {
                barrier.SignalAndWait();
                for (int i = 0; i < 10_000; i++)
                    cache.TryGet(1, out _);
            }
            catch (Exception ex) { exceptions.Add(ex); }
        });

        await Task.WhenAll(writer, reader);
        Assert.Empty(exceptions);
    }
    
[Fact(Timeout = 12000, DisplayName = "High-contention stress test — completes within timeout")]
public async Task StressTest_NoDeadlock()
{
    const int capacity = 200;
    const int threads = 32;

    var cache = new LRUCache<int, int>(capacity);
    var barrier = new Barrier(threads);

    var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
    {
        var rng = new Random(t);
        barrier.SignalAndWait(); // use barrier.SignalAndWait(TimeSpan.FromSeconds(2)); if hangs
        for (int i = 0; i < 2000; i++)
        {
            if (rng.Next(2) == 0)
                cache.Put(rng.Next(0, capacity * 2), rng.Next());
            else
                cache.TryGet(rng.Next(0, capacity * 2), out _);
        }
    }));

    var allTasks = Task.WhenAll(tasks);

    var completed = await Task.WhenAny(
        allTasks,
        Task.Delay(TimeSpan.FromSeconds(10)));

    Assert.True(completed == allTasks, "Possible deadlock detected.");
    
    await allTasks;
    Assert.True(cache.Count <= capacity);
}
}
