package cache;

import java.util.List;
import java.util.Map;

/**
 * Stripe-based LRU cache that partitions the keyspace across N independent
 * stripes, each with its own LRU list. Threads on different stripes never
 * contend — throughput scales with stripe count.\
 */
public class ScalableLRUCache<K, V> {

    private List<Stripe> stripes;

    public ScalableLRUCache(int totalCapacity, int stripeCount) {
        if (totalCapacity <= 0)
            throw new IllegalArgumentException("totalCapacity must be positive.");
        if (stripeCount <= 0)
            throw new IllegalArgumentException("stripeCount must be positive.");
        if (totalCapacity < stripeCount)
            throw new IllegalArgumentException("totalCapacity must be >= stripeCount.");

        stripes = buildStripes(stripeCount, totalCapacity / stripeCount);
    }

    public ScalableLRUCache(int totalCapacity) {
        this(totalCapacity, 4);
    }

    public int size() {
        throw new UnsupportedOperationException();
    }

    public V get(K key) {
        throw new UnsupportedOperationException();
    }

    public void put(K key, V value) {
        throw new UnsupportedOperationException();
    }

    public boolean remove(K key) {
        throw new UnsupportedOperationException();
    }

    /**
     * Resize stripe count and/or total capacity.
     */
    public void resize(int newTotalCapacity, int newStripeCount) {
        throw new UnsupportedOperationException();

    }

    // ── Private helpers ────────────────────────────────────────────────────

    private Stripe stripeFor(K key) {
        throw new UnsupportedOperationException();
    }

    private static int stripeIndex(Object key, int stripeCount) {
        throw new UnsupportedOperationException();
    }


    private List<Stripe> buildStripes(int count, int capacityEach) {
        throw new UnsupportedOperationException();
    }

    // ── Stripe ─────────────────────────────────────────────────────────────

    public class Stripe {
        private final LRUCache<K, V> inner;

        Stripe(int capacity) {
            inner = new LRUCache<>(capacity);
        }

        V get(K key)                { return inner.get(key); }
        void put(K key, V value)    { inner.put(key, value); }
        boolean remove(K key)       { return inner.remove(key); }
        int size()                  { return inner.size(); }

        /**
         * Yields all entries LRU (coldest) to MRU (hottest).
         * Used during resize so that when the new stripe hits capacity
         * mid-drain, the last-inserted (hottest) keys survive.
         */
        List<Map.Entry<K, V>> drainLRUOrder() {
            return inner.enumerateLRUOrder();
        }
    }
}
