namespace LRUCache;

/// <summary>
/// Staff-level discussion: a striped LRU cache that supports live resizing.
/// </summary>
public class ScalableLRUCache<TKey, TValue> where TKey : notnull
{
    private volatile Stripe[] _stripes;
    private readonly object _resizeLock = new();
    private int _capacityPerStripe;

    public int Count => throw new NotImplementedException();

    public ScalableLRUCache(int totalCapacity, int stripeCount = 4)
    {
        throw new NotImplementedException();
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        throw new NotImplementedException();
    }

    public void Put(TKey key, TValue value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Grow or shrink stripe count and/or total capacity live.
    /// </summary>
    public void Resize(int newTotalCapacity, int newStripeCount)
    {
        throw new NotImplementedException();
    }

    private int StripeIndex(TKey key, int stripeCount)
    {
        throw new NotImplementedException();
    }

    private sealed class Stripe
    {
        internal readonly object Lock = new();
        private readonly int _capacity;
        private readonly LRUCache<TKey, TValue> _inner;

        internal Stripe(int capacity)
        {
            _capacity = capacity;
            _inner = new LRUCache<TKey, TValue>(capacity);
        }

        internal bool TryGet(TKey key, out TValue? value) => _inner.TryGet(key, out value);
        internal void Put(TKey key, TValue value) => _inner.Put(key, value);
        internal int Count => _inner.Count;

        internal IEnumerable<(TKey, TValue)> DrainLRUOrder()
        {
            throw new NotImplementedException();
        }
    }
}