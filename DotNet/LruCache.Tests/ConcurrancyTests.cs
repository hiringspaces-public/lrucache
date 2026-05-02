using LRUCache;
using Xunit;

namespace LRUCache.Tests;

/// <summary>
/// Phase 4 — Concurrency (stretch goal).
///
/// The boilerplate has no synchronisation. These tests expose races that cause
/// capacity violations, corrupted state, or crashes under concurrent access.
///
/// A candidate who reaches this phase should add locking or switch to
/// lock-free primitives and explain the trade-offs.
/// </summary>
public class ConcurrencyTests
{
    [Fact(DisplayName = "Concurrent Puts never exceed capacity")]
    public async Task ConcurrentPuts_NeverExceedCapacity()
    {
        const int capacity  = 100;
        const int threads   = 20;
        const int opsEach   = 500;

        var cache = new LRUCache<int, int>(capacity);

        var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var rng = new Random(t);
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
        var cache = new LRUCache<int, int>(capacity);

        for (int i = 0; i < capacity; i++)
            cache.Put(i, i * 2);

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            try
            {
                var rng = new Random();
                for (int i = 0; i < 1_000; i++)
                {
                    int key = rng.Next(0, capacity);
                    cache.TryGet(key, out int val);
                    // Value must be either -1 (evicted) or the correct mapping
                    if (val != -1)
                        Assert.Equal(key * 2, val);
                }
            }
            catch (Exception ex) { exceptions.Add(ex); }
        }));

        await Task.WhenAll(tasks);
        Assert.Empty(exceptions);
    }

    [Fact(DisplayName = "Concurrent Put and Get on the same key — no torn reads")]
    public async Task ConcurrentPutAndGet_SameKey_NoTornReads()
    {
        var cache = new LRUCache<int, int>(10);
        cache.Put(1, 100);

        int violations = 0;

        var writer = Task.Run(() =>
        {
            for (int i = 0; i < 10_000; i++)
                cache.Put(1, (i % 2 == 0) ? 100 : 200);
        });

        var reader = Task.Run(() =>
        {
            for (int i = 0; i < 10_000; i++)
            {
                cache.TryGet(1, out int v);
                if (v != -1 && v != 100 && v != 200)
                    Interlocked.Increment(ref violations);
            }
        });

        await Task.WhenAll(writer, reader);
        Assert.Equal(0, violations);
    }

    [Fact(DisplayName = "High-contention stress test — no deadlock within timeout")]
    public async Task StressTest_NoDeadlock()
    {
        const int capacity = 200;
        var cache = new LRUCache<int, int>(capacity);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var tasks = Enumerable.Range(0, 50).Select(t => Task.Run(() =>
        {
            var rng = new Random(t);
            for (int i = 0; i < 2_000 && !cts.Token.IsCancellationRequested; i++)
            {
                if (rng.Next(2) == 0)
                    cache.Put(rng.Next(0, capacity * 2), rng.Next());
                else
                    cache.TryGet(rng.Next(0, capacity * 2), out var value);
            }
        }, cts.Token));

        // If this throws OperationCanceledException the test times out → deadlock
        await Task.WhenAll(tasks);
    }
}