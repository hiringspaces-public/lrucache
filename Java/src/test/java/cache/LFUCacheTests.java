package cache;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import java.util.concurrent.CompletableFuture;
import java.util.stream.IntStream;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Phase 3 — LFU cache contract.
 *
 * Verifies that eviction targets the least-frequently used key, with
 * LRU order as the tiebreaker within the same frequency.
 */
@Tag("Phase3")
class LFUCacheTests {

    @Test
    @DisplayName("put then get returns the stored value")
    void put_ThenGet_ReturnsValue() {
        var cache = new LFUCache<Integer, Integer>(2);
        cache.put(1, 10);
        assertEquals(10, cache.get(1));
    }

    @Test
    @DisplayName("least-frequent key is evicted, not least-recent")
    void eviction_LeastFrequent_NotLeastRecent() {
        var cache = new LFUCache<Integer, Integer>(2);
        cache.put(1, 1);
        cache.put(2, 2);

        cache.get(1); // key 1 freq=2, key 2 freq=1

        cache.put(3, 3); // evicts key 2 (lowest freq), not key 1 (less recent)

        assertNotNull(cache.get(1));
        assertNull(cache.get(2));
        assertNotNull(cache.get(3));
    }

    @Test
    @DisplayName("key accessed many times survives multiple evictions")
    void frequentlyAccessedKey_SurvivesMultipleEvictions() {
        var cache = new LFUCache<Integer, Integer>(2);
        cache.put(1, 1);
        cache.put(2, 2);

        cache.get(1);
        cache.get(1);
        cache.get(1); // key 1 freq=4

        cache.put(3, 3); // evicts key 2 (freq=1)
        cache.put(4, 4); // evicts key 3 (freq=1)

        assertEquals(1, cache.get(1));
        assertNull(cache.get(2));
        assertNull(cache.get(3));
        assertNotNull(cache.get(4));
    }

    @Test
    @DisplayName("frequency tie is broken by LRU order")
    void eviction_SameFrequency_EvictsLeastRecentlyUsed() {
        var cache = new LFUCache<Integer, Integer>(2);
        cache.put(1, 1);
        cache.put(2, 2);

        cache.get(1); // both now freq=2
        cache.get(2); // key 1 accessed first → LRU among freq=2

        cache.put(3, 3); // evicts key 1 (same freq, older access)

        assertNull(cache.get(1));
        assertNotNull(cache.get(2));
        assertNotNull(cache.get(3));
    }

    @Test
    @DisplayName("put on existing key updates value and increments its frequency")
    void put_ExistingKey_UpdatesValueAndFrequency() {
        var cache = new LFUCache<Integer, Integer>(2);
        cache.put(1, 1);
        cache.put(2, 2);

        cache.put(1, 99); // update + promote freq of key 1

        cache.put(3, 3); // evicts key 2 (lower freq)

        assertEquals(99, cache.get(1));
        assertNull(cache.get(2));
        assertNotNull(cache.get(3));
    }

    @Test
    @DisplayName("new insert resets minFreq to 1")
    void newInsert_ResetsMinFreqToOne() {
        var cache = new LFUCache<Integer, Integer>(2);
        cache.put(1, 1);
        cache.put(2, 2);

        cache.get(1);
        cache.get(1);
        cache.get(2); // key 1 freq=3, key 2 freq=2

        cache.put(3, 3); // evicts key 2 (minFreq=2), new key 3 starts at freq=1 → minFreq=1
        cache.put(4, 4); // evicts key 3 (minFreq=1)

        assertEquals(1, cache.get(1));
        assertNull(cache.get(3));
        assertNotNull(cache.get(4));
    }

    @Test
    @DisplayName("size reflects the number of stored entries")
    void size_ReflectsStoredEntries() {
        var cache = new LFUCache<Integer, Integer>(3);
        assertEquals(0, cache.size());
        cache.put(1, 1);
        assertEquals(1, cache.size());
        cache.put(2, 2);
        cache.put(3, 3);
        assertEquals(3, cache.size());
        cache.put(4, 4); // evicts one
        assertEquals(3, cache.size());
    }

    @Test
    @DisplayName("get on missing key returns null")
    void get_MissingKey_ReturnsNull() {
        var cache = new LFUCache<Integer, Integer>(2);
        cache.put(1, 1);
        assertNull(cache.get(99));
    }

    @Test
    @DisplayName("eviction order is approximately maintained under concurrent access")
    void concurrentEviction_HotKeysSurvive() throws Exception {
        var cache = new LFUCache<Integer, Integer>(10);
        for (int i = 0; i < 10; i++) cache.put(i, i);

        var hotReader = CompletableFuture.runAsync(() -> {
            for (int i = 0; i < 5000; i++) cache.get(i % 5);
        });

        var coldWriter = CompletableFuture.runAsync(() -> {
            for (int i = 10; i < 2000; i++) cache.put(i, i);
        });

        CompletableFuture.allOf(hotReader, coldWriter).join();

        long survived = IntStream.range(0, 5)
                .filter(k -> cache.get(k) != null)
                .count();
        assertTrue(survived > 0, "Expected at least some hot keys to survive eviction flood");
    }
}
