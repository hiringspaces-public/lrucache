package cache;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Phase 1 — Basic correctness.
 *
 * These are the simplest tests. A correct implementation should pass all of them.
 * If any Phase 1 test fails the implementation has a fundamental defect.
 */
@Tag("Phase1")
class BasicCorrectnessTests {

    @Test
    @DisplayName("get on empty cache returns null")
    void get_EmptyCache_ReturnsNull() {
        var cache = new LRUCache<Integer, Integer>(2);
        assertNull(cache.get(1));
    }

    @Test
    @DisplayName("put then get returns the stored value")
    void put_ThenGet_ReturnsValue() {
        var cache = new LRUCache<Integer, Integer>(2);
        cache.put(1, 10);
        assertEquals(10, cache.get(1));
    }

    @Test
    @DisplayName("get on a missing key returns null")
    void get_MissingKey_ReturnsNull() {
        var cache = new LRUCache<Integer, Integer>(2);
        cache.put(1, 10);
        assertNull(cache.get(99));
    }

    @Test
    @DisplayName("put updates an existing key's value")
    void put_ExistingKey_UpdatesValue() {
        var cache = new LRUCache<Integer, Integer>(2);
        cache.put(1, 10);
        cache.put(1, 20);
        assertEquals(20, cache.get(1));
    }

    @Test
    @DisplayName("size reflects the number of stored entries")
    void size_ReflectsStoredEntries() {
        var cache = new LRUCache<Integer, Integer>(3);
        assertEquals(0, cache.size());
        cache.put(1, 1);
        assertEquals(1, cache.size());
        cache.put(2, 2);
        assertEquals(2, cache.size());
    }

    @Test
    @DisplayName("inserting capacity+1 items evicts one entry")
    void put_OverCapacity_EvictsOneEntry() {
        var cache = new LRUCache<Integer, Integer>(2);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.put(3, 3); // should evict key 1 (LRU)

        assertEquals(2, cache.size());
    }

    @Test
    @DisplayName("cache with capacity 1 holds exactly one item")
    void capacity1_HoldsOneItem() {
        var cache = new LRUCache<Integer, Integer>(1);
        cache.put(1, 10);
        cache.put(2, 20);

        assertNull(cache.get(1));
        assertEquals(20, cache.get(2));
    }

    @Test
    @DisplayName("remove returns true and entry is gone")
    void remove_ExistingKey_ReturnsTrueAndRemovesEntry() {
        var cache = new LRUCache<Integer, Integer>(2);
        cache.put(1, 10);
        assertTrue(cache.remove(1));
        assertNull(cache.get(1));
        assertEquals(0, cache.size());
    }

    @Test
    @DisplayName("remove on missing key returns false")
    void remove_MissingKey_ReturnsFalse() {
        var cache = new LRUCache<Integer, Integer>(2);
        assertFalse(cache.remove(99));
    }

    @Test
    @DisplayName("clear removes all entries")
    void clear_RemovesAllEntries() {
        var cache = new LRUCache<Integer, Integer>(3);
        cache.put(1, 1);
        cache.put(2, 2);
        cache.clear();
        assertEquals(0, cache.size());
        assertNull(cache.get(1));
        assertNull(cache.get(2));
    }
}
