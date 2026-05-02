package cache;

/**
 * Contract for a generic LRU (Least Recently Used) cache.
 *
 * @param <K> the type of cache keys
 * @param <V> the type of cache values
 */
public interface ICache<K, V> {

    /** Returns the number of entries currently in the cache. */
    int size();

    /**
     * Retrieves the value associated with {@code key}, or {@code null} if absent.
     * Accessing an existing key promotes it to most-recently used.
     */
    V get(K key);

    /**
     * Inserts or updates the value for {@code key}.
     * If inserting causes the cache to exceed capacity the least-recently used
     * entry is evicted before the new entry is added.
     */
    void put(K key, V value);

    /**
     * Removes the entry for {@code key} if it exists.
     *
     * @return {@code true} if the key was found and removed; otherwise {@code false}
     */
    boolean remove(K key);

    /** Removes all entries from the cache. */
    void clear();
}
