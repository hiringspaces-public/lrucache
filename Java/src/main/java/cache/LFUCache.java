package cache;

import java.util.HashMap;
import java.util.Map;

/*
 * LFU Cache
 *
 * Eviction policy:
 * - Remove least frequently used key
 * - If multiple keys share same frequency, evict least recently used
 *
 * Target:
 * O(1) get / put
 */
public class LFUCache<K, V> implements ICache<K, V> {

    private final int capacity;

    // key -> value
    private final Map<K, V> values;

    // key -> frequency
    private final Map<K, Integer> freqs;

    // frequency -> keys ordered by recency
    private final Map<Integer, FreqList<K>> buckets;

    // key -> node in frequency bucket
    private final Map<K, FreqNode<K>> nodes;

    private int minFreq;

    public LFUCache(int capacity) {
        if (capacity <= 0)
            throw new IllegalArgumentException("Capacity must be positive.");

        this.capacity = capacity;
        this.values   = new HashMap<>(capacity);
        this.freqs    = new HashMap<>(capacity);
        this.buckets  = new HashMap<>();
        this.nodes    = new HashMap<>(capacity);
    }

    @Override
    public int size() {
        throw new UnsupportedOperationException();
    }

    @Override
    public V get(K key) {
        throw new UnsupportedOperationException();
    }

    @Override
    public void put(K key, V value) {
        throw new UnsupportedOperationException();
    }

    @Override
    public boolean remove(K key) {
        throw new UnsupportedOperationException();
    }

    @Override
    public void clear() {
        throw new UnsupportedOperationException();
    }

    // Promote key to next frequency bucket
    private void promote(K key, int freq) {
        throw new UnsupportedOperationException();
    }

    // Evict LFU/LRU key
    private void evict() {
        throw new UnsupportedOperationException();
    }

    private void removeFromBucket(int freq, K key) {
        throw new UnsupportedOperationException();
    }

    private FreqList<K> bucketFor(int freq) {
        throw new UnsupportedOperationException();
    }

    // ── Frequency bucket node ─────────────────────────────

    static class FreqNode<K> {
        K key;
        FreqNode<K> prev;
        FreqNode<K> next;

        FreqNode() {}

        FreqNode(K key) {
            this.key = key;
        }
    }

    // Doubly-linked list for keys sharing same frequency
    static class FreqList<K> {

        FreqNode<K> head;
        FreqNode<K> tail;

        FreqList() {
            throw new UnsupportedOperationException();
        }

        FreqNode<K> addFirst(K key) {
            throw new UnsupportedOperationException();
        }

        void remove(FreqNode<K> node) {
            throw new UnsupportedOperationException();
        }

        K removeLast() {
            throw new UnsupportedOperationException();
        }

        boolean isEmpty() {
            throw new UnsupportedOperationException();
        }
    }
}
