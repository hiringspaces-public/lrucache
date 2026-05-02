namespace LRUCache;

public class LFUCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, (TValue value, int freq)> _map;
    private readonly Dictionary<int, LinkedList<TKey>> _buckets;
    private readonly Dictionary<TKey, LinkedListNode<TKey>> _nodes;
    private int _minFreq;

    public LFUCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        _capacity = capacity;
        _map      = new Dictionary<TKey, (TValue, int)>(capacity);
        _buckets  = new Dictionary<int, LinkedList<TKey>>();
        _nodes    = new Dictionary<TKey, LinkedListNode<TKey>>(capacity);
    }

    public int Count => throw new NotImplementedException();

    public bool TryGet(TKey key, out TValue? value)
    {
        throw new NotImplementedException();
    }

    public void Put(TKey key, TValue value)
    {
        throw new NotImplementedException();
    }
}
