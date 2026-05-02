package cache;

/** A node in the doubly-linked list used by LRUCache. */
class Node<K, V> {
    K key;
    V value;
    Node<K, V> prev;
    Node<K, V> next;

    Node() {}

    Node(K key, V value) {
        this.key   = key;
        this.value = value;
    }
}
