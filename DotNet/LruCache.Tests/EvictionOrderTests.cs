using LRUCache;
using Xunit;

namespace LRUCache.Tests;

/// <summary>
/// Phase 2 — Eviction order and LRU coherence.
///
/// These tests verify that Get() and Put() both correctly update recency,
/// and that the least-recently-used item is always the one evicted.
/// </summary>
[Trait("Phase", "2")]
public class EvictionOrderTests
{
    [Fact(DisplayName = "Oldest untouched item is evicted first")]
    public void Eviction_OldestUntouched_EvictedFirst()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1); // inserted first → LRU if never touched
        cache.Put(2, 2);
        cache.Put(3, 3);
        cache.Put(4, 4); // triggers eviction — key 1 should go

        Assert.Equal(-1, cache.TryGet(1, out var val1) ? val1 : -1); // evicted
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

        cache.TryGet(1, out var val); // promote key 1 → key 2 is now LRU

        cache.Put(4, 4); // key 2 should be evicted

        Assert.Equal(1,  cache.TryGet(1, out var val1) ? val1 : -1); // survived — was promoted
        Assert.Equal(-1, cache.TryGet(1, out var val2) ? val2 : -1); // evicted
        Assert.Equal(3,  cache.TryGet(1, out var val3) ? val3 : -1);
        Assert.Equal(4,  cache.TryGet(1, out var val4) ? val1 : -1);
    }

    [Fact(DisplayName = "Put on existing key promotes it — it survives the next eviction")]
    public void Put_ExistingKey_PromotesKey_SurvivesEviction()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        cache.Put(1, 99); // update + promote key 1 → key 2 is now LRU

        cache.Put(4, 4); // key 2 should be evicted

        Assert.Equal(99, cache.TryGet(1, out var val1) ? val1 : -1); // survived and updated
        Assert.Equal(-1, cache.TryGet(2, out var val2) ? val1 : -1); // evicted
    }

    [Fact(DisplayName = "Multiple Gets update recency correctly")]
    public void MultipleGets_UpdateRecencyCorrectly()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        cache.TryGet(1, out int val1); // order now: 3 2 1  (1 = MRU)
        cache.TryGet(2, out int val2); // order now: 3 1 2  wait — 2 promoted again → MRU
                      // actually:   1→3  2→MRU  so 3 is LRU

        cache.Put(4, 4); // key 3 should be evicted

        Assert.Equal(-1, cache.TryGet(3, out var val3) ? val3 : -1); // evicted (LRU)
        Assert.Equal(1,  cache.TryGet(1, out var val4) ? val4 : -1);
        Assert.Equal(2,  cache.TryGet(2, out var val5) ? val5 : -1);
        Assert.Equal(4,  cache.TryGet(4, out var val6) ? val6 : -1);
    }

    [Fact(DisplayName = "Frequent Get on same key does not prevent correct eviction of others")]
    public void FrequentGet_SameKey_DoesNotAffectOthers()
    {
        var cache = new LRUCache<int, int>(3);
        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        // Get key 1 many times — only recency matters, not frequency
        for (int i = 0; i < 100; i++)
            cache.TryGet(1, out var val); // promote key 1 → key 2 is now LRU

        cache.Put(4, 4); // key 2 should be evicted (key 1 MRU, key 3 next, key 2 LRU)

        // After all the Gets on 1: order is 3, 2, 1 (1=MRU)
        // Next eviction candidate = 2
        Assert.Equal(-1, cache.TryGet(2, out var val1) ? val1 : -1); // evicted
        Assert.Equal(1,  cache.TryGet(1, out var val2) ? val2 : -1);
        Assert.Equal(3,  cache.TryGet(3, out var val3) ? val3: -1);
    }

    [Fact(DisplayName = "Full eviction sequence matches LRU contract")]
    public void FullEvictionSequence_MatchesLRUContract()
    {
        // capacity=3, insert 1..6 sequentially with no Gets.
        // Each new insert evicts the oldest.
        var cache = new LRUCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);
        cache.Put(4, 4); // evicts 1
        cache.Put(5, 5); // evicts 2
        cache.Put(6, 6); // evicts 3

        Assert.Equal(-1, cache.TryGet(1, out var val1) ? val1 : -1);
        Assert.Equal(-1, cache.TryGet(2, out var val2) ? val2 : -1);
        Assert.Equal(-1, cache.TryGet(3, out var val3) ? val3 : -1);
        Assert.Equal(4, cache.TryGet(4, out var val4) ? val4: -1);
        Assert.Equal(5, cache.TryGet(5, out var val5) ? val5 : -1);
        Assert.Equal(6, cache.TryGet(6, out var val6) ? val6 : -1);
    }
}