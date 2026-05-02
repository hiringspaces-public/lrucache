using LRUCache;
using Xunit;

namespace LRUCache.Tests;

/// <summary>
/// Phase 3 — Resizing and hashing integrity.
///
/// These tests insert enough entries to expose problems with the internal
/// hash-table bucket management. A naive fixed-bucket implementation degrades
/// to O(n) lookup under load; a correct one resizes dynamically.
/// </summary>
public class ResizingAndHashingTests
{
    [Fact(DisplayName = "All keys retrievable after many insertions (triggers resize path)")]
    public void AllKeys_RetrievableAfterManyInsertions()
    {
        const int capacity = 5_000;
        var cache = new LRUCache<int, int>(capacity);

        for (int i = 0; i < capacity; i++)
            cache.Put(i, i * 10);

        for (int i = 0; i < capacity; i++)
            Assert.Equal(i * 10, cache.Get(i));
    }

    [Fact(DisplayName = "Count stays accurate across multiple resize cycles")]
    public void Count_AccurateAcrossResizeCycles()
    {
        const int capacity = 2_000;
        var cache = new LRUCache<int, int>(capacity);

        for (int i = 0; i < capacity; i++)
            cache.Put(i, i);

        Assert.Equal(capacity, cache.Count);
    }

    [Fact(DisplayName = "No phantom entries after resize — Count matches unique keys")]
    public void NoPhantomEntries_AfterResize()
    {
        const int capacity = 1_000;
        var cache = new LRUCache<int, int>(capacity);

        // Insert same key repeatedly — count must not grow
        for (int round = 0; round < 5; round++)
            for (int i = 0; i < capacity; i++)
                cache.Put(i, i + round);

        Assert.Equal(capacity, cache.Count);
    }

    [Fact(DisplayName = "Keys that collided pre-resize are still distinct post-resize")]
    public void CollidingKeys_RemainDistinct_PostResize()
    {
        // Keys 0, 16, 32, 48 ... all land in bucket 0 with 16-bucket table.
        // After resize they must spread to separate buckets.
        const int capacity = 3_000;
        var cache = new LRUCache<int, int>(capacity);

        for (int i = 0; i < capacity; i += 16)
            cache.Put(i, i);

        for (int i = 0; i < capacity; i += 16)
            Assert.Equal(i, cache.Get(i));
    }

    [Fact(DisplayName = "Eviction still works correctly after a resize")]
    public void Eviction_StillCorrect_AfterResize()
    {
        const int capacity = 500;
        var cache = new LRUCache<int, int>(capacity);

        // Fill to capacity
        for (int i = 0; i < capacity; i++)
            cache.Put(i, i);

        // Promote key 0 to MRU
        cache.Get(0);

        // Add one more — should evict key 1 (the new LRU after key 0 was promoted)
        cache.Put(capacity, capacity);

        Assert.Equal(0,        cache.Get(0));       // survived
        Assert.Equal(-1,       cache.Get(1));        // evicted
        Assert.Equal(capacity, cache.Get(capacity)); // newly inserted
    }
}