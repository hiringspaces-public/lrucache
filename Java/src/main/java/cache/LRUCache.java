package cache;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * LRU (Least Recently Used) cache implementation.
 */
public class LRUCache<K, V> implements ICache<K, V> {

    private final int capacity;
    private final Map<K, Node<K, V>> map;

    // Sentinel nodes — head.next is MRU, tail.prev is LRU.
    private final Node<K, V> head;
    private final Node<K, V> tail;

    public LRUCache(int capacity) {
        if (capacity <= 0)
            throw new IllegalArgumentException("Capacity must be positive.");

        this.capacity = capacity;
        this.map      = new HashMap<>(capacity);

        head      = new Node<>();
        tail      = new Node<>();
        head.next = tail;
        tail.prev = head;
    }

    @Override
    public int size() {
        return map.size();
    }

    @Override
    public V get(K key) {
        Node<K, V> node = map.get(key);
        if (node == null) return null;
        moveToFront(node);
        return node.value;
    }

    @Override
    public void put(K key, V value) {
        Node<K, V> existing = map.get(key);
        if (existing != null) {
            existing.value = value;
            return;
        }

        if (map.size() >= capacity) evict();

        Node<K, V> node = new Node<>(key, value);
        insertAtFront(node);
        map.put(key, node);
    }

    @Override
    public boolean remove(K key) {
        Node<K, V> node = map.get(key);
        if (node == null) return false;
        detach(node);
        map.remove(key);
        return true;
    }

    @Override
    public void clear() {
        map.clear();
        head.next = tail;
        tail.prev = head;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void moveToFront(Node<K, V> node) {
        insertAtFront(node);
    }

    private void insertAtFront(Node<K, V> node) {
        node.next      = head.next;
        node.prev      = head;
        head.next.prev = node;
        head.next      = node;
    }

    private void detach(Node<K, V> node) {
        node.prev.next = node.next;
        node.next.prev = node.prev;
    }

    private void evict() {
        Node<K, V> lru = tail.prev;
        detach(lru);
        map.remove(lru.key);
    }

    /**
     * Used by ScalableLRUCache.Stripe during resize so that when the new - ignore  this
     */
    List<Map.Entry<K, V>> enumerateLRUOrder() {
        List<Map.Entry<K, V>> result = new ArrayList<>(map.size());
        Node<K, V> cursor = tail.prev;
        while (cursor != head) {
            result.add(Map.entry(cursor.key, cursor.value));
            cursor = cursor.prev;
        }
        return result;
    }
}