namespace LRUCache;
 
/// <summary>
/// Contract for a generic LRU (Least Recently Used) cache.
/// </summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TValue">The type of cache values.</typeparam>
public interface ICache<TKey, TValue>
{
    /// <summary>
    /// Returns the number of entries currently in the cache.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Tries to retrieve the value associated with <paramref name="key"/>.
    /// Accessing an existing key promotes it to most-recently used.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The cached value if found; otherwise the default.</param>
    /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
    bool TryGet(TKey key, out TValue? value);

    /// <summary>
    /// Inserts or updates the value for <paramref name="key"/>.
    /// If inserting causes the cache to exceed capacity, the least-recently
    /// used entry is evicted before the new entry is added.
    /// </summary>
    void Put(TKey key, TValue value);

}
