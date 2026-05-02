namespace LRUCache;

/// <summary>
/// LRU (Least Recently Used) cache implementation.
///
/// Internally uses a Dictionary for O(1) key lookup combined with a
/// doubly-linked list to track access order (head = MRU, tail = LRU).
///
/// Capacity is fixed at construction time.
/// </summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TValue">The type of cache values.</typeparam>
public class LRUCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, Node<TKey, TValue>> _map;

    // Sentinel nodes — head.Next is MRU, tail.Prev is LRU.
    private readonly Node<TKey, TValue> _head;
    private readonly Node<TKey, TValue> _tail;

    public LRUCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _capacity = capacity;
        _map      = new Dictionary<TKey, Node<TKey, TValue>>(capacity);

        _head = new Node<TKey, TValue>();
        _tail = new Node<TKey, TValue>();
        _head.Next = _tail;
        _tail.Prev = _head;
    }

    /// <inheritdoc/>
    public int Count => _map.Count;

    /// <inheritdoc/>
    public bool TryGet(TKey key, out TValue? value)
    {
        if (!_map.TryGetValue(key, out Node<TKey, TValue>? node))
        {
            value = default;
            return false;
        }

        value = node.Value;
        return true;
    }

    public TValue Get(TKey key)
    {
        if (!TryGet(key, out TValue? value))
            throw new KeyNotFoundException($"Key '{key}' not found in cache.");

        return value!;
    }

    /// <inheritdoc/>
    public void Put(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out Node<TKey, TValue>? existing))
        {
            // Update value and promote to MRU.
            existing.Value = value;
            MoveToFront(existing);
            return;
        }

        if (_map.Count >= _capacity)
            Evict();

        var node = new Node<TKey, TValue>(key, value);
        InsertAtFront(node);
        _map[key] = node;
    }

    public void Remove(TKey key)
    {
        if (!_map.TryGetValue(key, out Node<TKey, TValue>? node))
            return;

        Detach(node);
        _map.Remove(key);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void MoveToFront(Node<TKey, TValue> node)
    {
        InsertAtFront(node);
    }

    private void InsertAtFront(Node<TKey, TValue> node)
    {
        node.Next        = _head.Next;
        node.Prev        = _head;
        _head.Next!.Prev = node;
        _head.Next       = node;
    }

    private static void Detach(Node<TKey, TValue> node)
    {
        node.Prev!.Next = node.Next;
        node.Next!.Prev = node.Prev;
    }

    private void Evict()
    {
        Node<TKey, TValue> lru = _tail.Prev!;
        Detach(lru);
        _map.Remove(lru.Key!);
    }
}

/// <summary>
/// A node in the doubly-linked list.
/// </summary>
public class Node<TKey, TValue>
{
    public TKey?   Key;
    public TValue? Value;
    public Node<TKey, TValue>? Prev;
    public Node<TKey, TValue>? Next;

    public Node() { }

    public Node(TKey key, TValue value)
    {
        Key   = key;
        Value = value;
    }
}