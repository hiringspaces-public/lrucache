ScalableLRUCache
/// Key questions to ask:
/// - Why stripe at all? (reduce lock contention)
/// - Why acquire locks in index order during resize? (deadlock prevention)
/// - What happens to a reader mid-resize? (volatile _stripes reference)
/// - When two stripes collapse into one during shrink, which keys survive?

    /// <summary>
    /// Grow or shrink stripe count and/or total capacity live.
    ///
    /// Steps to walk candidate through:
    ///   1. Acquire _resizeLock (prevent concurrent resize)
    ///   2. Acquire all current stripe locks IN ASCENDING INDEX ORDER
    ///   3. Build new stripe array, redistribute keys in LRU order
    ///      (LRU-to-MRU so hottest keys survive if new capacity is smaller)
    ///   4. volatile write _stripes = newStripes
    ///   5. Release locks
    ///
    /// Ask: what breaks if you acquire locks in random order?
    /// Ask: what does a concurrent reader see between steps 3 and 4?
    /// Ask: if newStripeCount < oldStripeCount, two stripes collapse — 
    ///       combined keys may exceed new stripe capacity. Which survive?
    /// </summary>

            // TODO: acquire _resizeLock
        // TODO: acquire all _stripes[i].Lock in ascending order i=0..n-1
        // TODO: build fresh Stripe[] of length newStripeCount
        // TODO: for each old stripe, drain keys in LRU order, re-insert into new stripes
        // TODO: _stripes = newStripes  (volatile write)
        // TODO: release locks


            private int StripeIndex(TKey key, int stripeCount)
    {
        // TODO: return (uint)(key.GetHashCode() * 2654435769u) >> (32 - Log2(stripeCount))
        // Discussion: why not just GetHashCode() % stripeCount?
        //   - GetHashCode() can be negative
        //   - modulo gives poor distribution for sequential integer keys
        //   - Fibonacci hashing spreads bits more uniformly
        // Discussion: GetHashCode() is NOT stable across .NET process restarts
        //   — does that matter here? (yes if keys are persisted; no for in-memory cache)
        throw new NotImplementedException();
    }