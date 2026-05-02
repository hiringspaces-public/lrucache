package cache;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Phase 2 — Eviction order and LRU coherence.
 *
 * These tests verify that get and put both update recency correctly.
 * A broken MoveToFront (Bug 1 or Bug 2) will cause failures here.
 */
@Tag("Phase2")
class EvictionOrderTests {

    @Test
    @DisplayName("oldest untouched item is evicted first")
    void eviction_OldestUntouched_EvictedFirst() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);
        cache.put(4, 4); // evicts 1

        assertNull(cache.get(1));
        assertEquals(2, cache.get(2));
        assertEquals(3, cache.get(3));
        assertEquals(4, cache.get(4));
    }

    @Test
    @DisplayName("get promotes a key — it survives the next eviction")
    void get_PromotesKey_SurvivesEviction() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);

        cache.get(1); // promote 1 to MRU

        cache.put(4, 4); // should evict 2 (now LRU)

        assertEquals(1, cache.get(1));
        assertNull(cache.get(2));
        assertEquals(3, cache.get(3));
        assertEquals(4, cache.get(4));
    }

    @Test
    @DisplayName("get on LRU saves it through two subsequent evictions")
    void get_OnLRU_SavesItThroughTwoEvictions() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);

        cache.get(1); // promote 1

        cache.put(4, 4); // evicts 2
        cache.put(5, 5); // evicts 3

        assertEquals(1, cache.get(1));
        assertNull(cache.get(2));
        assertNull(cache.get(3));
        assertNotNull(cache.get(4));
        assertNotNull(cache.get(5));
    }

    @Test
    @DisplayName("repeated get on same key does not corrupt the list")
    void get_RepeatedAccess_DoesNotCorruptList() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);

        cache.get(1);
        cache.get(1);
        cache.get(1);

        cache.put(4, 4);
        cache.put(5, 5);

        assertEquals(3, cache.size());
        assertEquals(1, cache.get(1));
        assertNotNull(cache.get(4));
        assertNotNull(cache.get(5));
        assertNull(cache.get(2));
        assertNull(cache.get(3));
    }

    @Test
    @DisplayName("put on existing key promotes it — it survives the next eviction")
    void put_ExistingKey_PromotesKey_SurvivesEviction() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);

        cache.put(1, 99); // update + promote 1

        cache.put(4, 4); // should evict 2

        assertEquals(99, cache.get(1));
        assertNull(cache.get(2));
    }

    @Test
    @DisplayName("put on existing key makes it the new MRU")
    void put_ExistingKey_MustBecomeNewMRU() {
        var cache = new LRUCache<Integer, Integer>(2);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(1, 99); // update 1 → now MRU; 2 → LRU
        cache.put(3, 3);  // evicts 2

        assertNull(cache.get(2));
        assertEquals(99, cache.get(1));
        assertNotNull(cache.get(3));
    }

    @Test
    @DisplayName("multiple gets update recency correctly")
    void multipleGets_UpdateRecencyCorrectly() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);

        cache.get(1); // promote 1
        cache.get(2); // promote 2 — 3 is now LRU

        cache.put(4, 4); // evicts 3

        assertNull(cache.get(3));
        assertEquals(1, cache.get(1));
        assertEquals(2, cache.get(2));
        assertEquals(4, cache.get(4));
    }

    @Test
    @DisplayName("frequent get on same key does not prevent correct eviction of others")
    void frequentGet_SameKey_DoesNotAffectOthers() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);

        for (int i = 0; i < 100; i++) cache.get(1);

        cache.put(4, 4); // evicts 2 (LRU)

        assertNull(cache.get(2));
        assertEquals(1, cache.get(1));
        assertEquals(3, cache.get(3));
        assertEquals(4, cache.get(4));
    }

    @Test
    @DisplayName("full eviction sequence matches LRU contract")
    void fullEvictionSequence_MatchesLRUContract() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3);
        cache.put(4, 4);
        cache.put(5, 5);
        cache.put(6, 6);

        assertNull(cache.get(1));
        assertNull(cache.get(2));
        assertNull(cache.get(3));
        assertEquals(4, cache.get(4));
        assertEquals(5, cache.get(5));
        assertEquals(6, cache.get(6));
    }
}
