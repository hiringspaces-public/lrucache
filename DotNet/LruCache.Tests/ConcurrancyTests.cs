using System.Collections.Concurrent;
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
        const int threads = 20;
        const int opsEach = 500;

        var cache = new LRUCache<int, int>(capacity);
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
        const int threads = 10;

        var cache = new LRUCache<int, int>(capacity);
        for (int i = 0; i < capacity; i++)
            cache.Put(i, i * 2);

        var barrier = new Barrier(threads);
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

        var barrier = new Barrier(2);
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
    [Fact(DisplayName = "Count is always consistent under concurrent Put and Remove")]
    public void ConcurrentPutRemove_CountNeverExceedsCapacity()
    {
        var cache = new LRUCache<int, int>(30);
        var snapshots = new ConcurrentBag<int>();

        var putters = Enumerable.Range(0, 5).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < 300; i++)
            {
                cache.Put(i % 30, i);
                snapshots.Add(cache.Count); // sample Count after every mutation
            }
        }));

        var removers = Enumerable.Range(0, 3).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                cache.Remove(i % 30);
                snapshots.Add(cache.Count);
            }
        }));

        Task.WaitAll(putters.Concat(removers).ToArray());

        Assert.True(snapshots.All(c => c >= 0 && c <= 30),
            $"Saw illegal Count. Min={snapshots.Min()} Max={snapshots.Max()}");
    }

    [Fact(DisplayName = "No deadlock under mixed load — completes within timeout")]
    public void MixedLoad_NoDeadlock()
    {
        var cache = new LRUCache<int, int>(20);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var tasks = Enumerable.Range(0, 12).Select(t => Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                int key = Random.Shared.Next(30);
                if (t % 3 == 0)
                    cache.Put(key, key);
                else if (t % 3 == 1)
                    cache.TryGet(key, out _);
                else
                    cache.Remove(key);
            }
        }));

        cts.Cancel();

        // If deadlocked, WaitAll hangs — test runner timeout kills it
        // Give 2 extra seconds for tasks to observe cancellation
        Assert.True(Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(2)),
            "Tasks did not complete — possible deadlock");
    }
    [Fact(DisplayName = "Concurrent Gets never return wrong value")]
    public void ConcurrentGets_NeverReturnWrongValue()
    {
        var cache = new LRUCache<int, int>(10);
        for (int i = 0; i < 10; i++) cache.Put(i, i * 10);

        var errors = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                int key = i % 10;
                if (cache.TryGet(key, out var v) && v != key * 10)
                    errors.Add($"key {key} returned {v}, expected {key * 10}");
            }
        }));

        Task.WaitAll(tasks.ToArray());
        Assert.Empty(errors);
    }

    [Fact(DisplayName = "Concurrent Puts never exceed capacity")]
    public void ConcurrentPuts_NeverExceedCapacityLimit()
    {
        var cache = new LRUCache<int, int>(50);

        var tasks = Enumerable.Range(0, 10).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
                cache.Put(t * 200 + i, i);
        }));

        Task.WaitAll(tasks.ToArray());
        Assert.True(cache.Count <= 50, $"Count {cache.Count} exceeded capacity 50");
    }

    [Fact(DisplayName = "Concurrent readers and writers do not corrupt list")]
    public void ConcurrentReadWrite_NoCorruption()
    {
        var cache = new LRUCache<int, int>(20);
        for (int i = 0; i < 20; i++) cache.Put(i, i);

        var errors = new ConcurrentBag<string>();

        var readers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 2000; i++)
            {
                int key = i % 20;
                if (cache.TryGet(key, out var v) && v != key)
                    errors.Add($"key {key} returned {v}");
            }
        }));

        var writers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 500; i++)
                cache.Put(i % 20, i % 20);
        }));

        Task.WaitAll(readers.Concat(writers).ToArray());
        Assert.Empty(errors);
        Assert.True(cache.Count <= 20);
    }
}
