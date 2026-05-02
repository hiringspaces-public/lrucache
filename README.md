# LRU Cache — System Design & Testing Guide

## 1. Core Concept

**Least Recently Used (LRU)** evicts the entry that hasn't been accessed for the longest time when the cache reaches capacity.

---

## 2. Single-Node Design

**Data Structures:** `HashMap` + `Doubly Linked List` → O(1) get/put

```
 HashMap<Key, Node*>
        │
        ▼
 Head ↔ [A] ↔ [B] ↔ [C] ↔ [D] ↔ Tail
 (MRU)                          (LRU → evict)
```

**Operations:**

| Op | Action | Time |
|---|---|---|
| `get(key)` | Lookup in map → move node to head → return value | O(1) |
| `put(key,val)` | If exists: update + move to head. If full: evict tail, insert at head | O(1) |

---

## 3. Distributed LRU Cache Design

```
                    ┌──────────────┐
   Clients ──────►  │  Load Balancer │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
        ┌──────────┐ ┌──────────┐ ┌──────────┐
        │ Cache Node│ │ Cache Node│ │ Cache Node│   ← Consistent Hashing
        │  (Shard 1)│ │  (Shard 2)│ │  (Shard 3)│
        └─────┬────┘ └─────┬────┘ └─────┬────┘
              │             │            │
              └─────────────┼────────────┘
                            ▼
                    ┌──────────────┐
                    │   Database    │  ← Source of truth
                    └──────────────┘
```

### Key Components

**Partitioning (Consistent Hashing)**

- Keys distributed across shards via consistent hash ring
- Adding/removing nodes only remaps ~K/N keys
- Virtual nodes ensure even distribution

**Each Cache Node (In-Memory)**

- Local LRU using HashMap + Doubly Linked List
- Fixed memory budget; evicts LRU on capacity hit
- TTL per entry for staleness control

**Read Path**

```
Client → hash(key) → route to shard
  → Cache HIT:  move to MRU, return value
  → Cache MISS: read from DB, insert to cache, evict LRU if full, return
```

**Write Path (Cache-Aside)**

```
Client → write to DB → invalidate cache entry
```

Other strategies:

- **Write-through:** Write to cache + DB synchronously
- **Write-back:** Write to cache, async flush to DB (risk of data loss)

### Handling Failures

| Scenario | Mitigation |
|---|---|
| Node crash | Consistent hashing redirects to next node; cache miss → DB backfill |
| Hot key | Replicate hot keys to multiple shards or use local client-side cache |
| Thundering herd | Request coalescing (single-flight) — one DB fetch, many waiters |
| DB down | Serve stale cache (extend TTL), circuit breaker on DB calls |

### Key Trade-offs

| Decision | Option A | Option B |
|---|---|---|
| Consistency | Strong (write-through) | Eventual (write-back + TTL) |
| Eviction | Pure LRU | LRU + TTL hybrid |
| Partitioning | Consistent hashing | Fixed slot mapping |
| Replication | None (simpler) | Leader-follower per shard (HA) |

### Scaling Estimates

```
Assumption: 10M keys, 1KB avg value
Total data:  ~10 GB
3 shards:    ~3.3 GB RAM each  ✓ fits single node
Read QPS:    100K → ~33K per shard
Latency:     Cache hit < 1ms, miss ~5-10ms (DB)
Hit ratio:   Typical 90-95% with proper sizing
```

---

## 4. Dynamic Resizing & Consistent Hashing — Deep Dive

### Problem: Fixed Buckets Don't Scale

```
16 buckets, 1M keys → ~62,500 keys/bucket
  → Long chains → O(n) lookup within bucket
  → Cache becomes slower than useful
```

### Load Factor Trigger

```
load_factor = num_keys / num_buckets
Resize when load_factor > threshold (typically 0.75)

16 buckets × 0.75 = resize at 12 keys
32 → 24, 64 → 48, ... doubling each time
```

### Naive Rehash (Stop-the-world)

```
Allocate 2× buckets
For each key: new_bucket = hash(key) % new_size
  → ALL keys move at once → O(n) spike
```

### Incremental Rehash (Redis approach)

```
Phase 1: Allocate new table (2×), keep old table alive

 ┌─────────────┐    ┌─────────────────────────┐
 │ Old (16)     │    │ New (32)                 │
 │ [0]→k1→k5   │    │ [0]                      │
 │ [1]→k2      │    │ [1]                      │
 │ ...          │    │ ...                      │
 └─────────────┘    └─────────────────────────┘

Phase 2: On EVERY get/put, migrate 1-2 buckets from old → new
  → get(key): check new first, then old (if not migrated yet)
  → put(key): always write to new table
  → Background: migrate one old bucket per operation

Phase 3: Old table fully drained → free it

Cost: O(1) amortized per operation, zero downtime
```

**Key Insight:**

```
Bucket being migrated:  old_bucket[i] splits into:
  new_bucket[i]          (hash bit = 0)
  new_bucket[i + old_size] (hash bit = 1)

Only need to check ONE extra bit of the hash — no full rehash!
```

### Consistent Hashing — Hash Ring

```
        0 (top)
       / \
     N1    N3          Ring: 0 → 2^32
    /        \         Nodes placed at hash(node_id)
  N2 ──────── N1       Keys walk clockwise to first node
```

**Adding a node:**

```
  Before:     N1 ─────────── N2      (N2 owns full arc)
  After:      N1 ──── N4 ─── N2      (N4 steals part of N2's arc)
  
  Only keys in [N1, N4] range move from N2 → N4
  Rest of cluster: UNCHANGED
  Keys remapped: ~K/N  (optimal!)
```

**Problem with modular hashing:**

```
node = hash(key) % N

N=3:  key "user:42" → hash=107 → 107 % 3 = node 2
N=4:  key "user:42" → hash=107 → 107 % 4 = node 3  ← MOVED!

Adding 1 node remaps ~75% of keys → cache miss storm
```

### Virtual Nodes (Fixing Imbalance)

```
Problem: 3 physical nodes → uneven arcs

  N1 ████████████████░░░░ (60% keys)
  N2 ████░░░░░░░░░░░░░░░░ (15% keys)  ← hot/cold imbalance
  N3 █████░░░░░░░░░░░░░░░ (25% keys)

Solution: Each physical node → 100-200 virtual nodes on ring

  N1_v0, N1_v1, ..., N1_v149  → scattered across ring
  N2_v0, N2_v1, ..., N2_v149
  N3_v0, N3_v1, ..., N3_v149

  ~450 points on ring → nearly uniform distribution
  Each physical node: ~33% ± 2% keys  ✓
```

### Dynamic Distributed LRU — Combined

```
                        Hash Ring (2^32)
                    ┌───────────────────┐
                    │  V1a  V2c  V3b    │
                    │ V3a  V1c  V2a     │  ← Virtual nodes
                    │  V2b  V3c  V1b    │
                    └───────────────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
     ┌────────────┐  ┌────────────┐  ┌────────────┐
     │ Node 1     │  │ Node 2     │  │ Node 3     │
     │ Local LRU  │  │ Local LRU  │  │ Local LRU  │
     │ Incremental│  │ Incremental│  │ Incremental│
     │ Rehash     │  │ Rehash     │  │ Rehash     │
     └────────────┘  └────────────┘  └────────────┘
```

**Scaling out (add node):**

1. New node joins → gets virtual node positions on ring
2. Coordinator identifies affected key ranges
3. Background migration: pull keys from neighbors
4. During migration: reads try new node first, fallback to old owner
5. Migration complete → update ring → old owners drop migrated keys

**Scaling in (remove node):**

1. Node announces departure
2. Its key ranges assigned to clockwise successors
3. Keys migrated out before shutdown
4. Ring updated → node removed

| Layer | Problem | Solution | Cost |
|---|---|---|---|
| **Intra-node** | Bucket chains too long | Incremental rehash (2× buckets) | O(1) amortized |
| **Inter-node** | Adding node remaps all keys | Consistent hash ring | ~K/N keys move |
| **Load imbalance** | Uneven key distribution | Virtual nodes (150+/node) | ~uniform ±2% |
| **Migration** | Keys in transit | Double-read (new then old) | Brief latency bump |

---

## 5. Test Scenario — 16 Buckets → 32 Buckets Fix

### The Problem

```
16 buckets, 100K items → ~6,250 items/bucket (long chains)
Test cases with 6,000+ items → O(n) scan → timeouts/failures
```

### Why Tests Fail with 16 Buckets

| Root Cause | Effect |
|---|---|
| Hash collisions pile up in chains | `get()` degrades from O(1) → O(n) |
| Test expects response within X ms | 6,000 items in one bucket → too slow |
| Memory locality destroyed | Long linked-list chains → CPU cache misses |
| Possible correctness bug | Resize logic off-by-one → keys in wrong bucket → null returns |

### Why Tests Pass After Fix (32 + dynamic resize)

```
16 → 32 → 64 → 128 → ...
100K items at load_factor 0.75 → eventually ~131K buckets
~0.76 items/bucket → true O(1)
```

The real fix is that **resizing now triggers dynamically**, and 32 is just the first step.

---

## 6. Comprehensive LRU Cache Test Suite

### Phase 1: Basic Correctness (Boilerplate LRU)

| Test | What It Verifies |
|---|---|
| `put(k,v)` then `get(k) == v` | Basic store/retrieve |
| `get(missing) == -1` | Cache miss handling |
| `put(k,v1)` then `put(k,v2)` → `get(k) == v2` | Value update |
| Insert `capacity+1` items → oldest evicted | Basic LRU eviction |
| `get(k)` makes it "recently used" → survives eviction | Access refreshes recency |

### Phase 2: Eviction Order & LRU Coherence

| Test | What It Verifies |
|---|---|
| Insert A,B,C (cap=3). Insert D → A evicted | FIFO-like for untouched items |
| Insert A,B,C. `get(A)`. Insert D → **B** evicted (not A) | Read promotes recency |
| Insert A,B,C. `put(A, new_val)`. Insert D → **B** evicted | Write promotes recency |
| Insert A,B,C. `get(A)`, `get(B)`. Insert D → **C** evicted | Multiple promotions tracked |
| Insert A. `get(A)` ×1000. Insert B,C → A survives | Frequency shouldn't matter, only recency |
| Insert 1..N, access in reverse, evict → verify order | Full eviction order matches LRU contract |

**Why this catches bugs:** A broken doubly-linked-list (e.g., not unlinking before moving to head) silently corrupts eviction order.

### Phase 3: Resizing & Hashing Integrity

| Test | What It Verifies |
|---|---|
| Insert keys until resize triggers → all keys still retrievable | No keys lost during rehash |
| Insert, resize, insert more, resize again → all correct | Multiple resize cycles |
| Check `size()` matches actual unique keys after resize | No phantom entries |
| Insert keys that collide pre-resize → distinct post-resize | Collision resolution survives rehash |
| Verify load factor stays within bounds after many ops | Resize triggers at right threshold |
| Insert + delete + resize → deleted keys stay deleted | Tombstones/deletions handled during rehash |

### Phase 4: Cache Coherence (Multi-threaded / Distributed)

**Single-node thread safety:**

| Test | What It Verifies |
|---|---|
| N threads `put()` concurrently → `size() <= capacity` | No over-capacity races |
| Thread A `get(k)` while Thread B evicts → no crash/corrupt | Read-eviction race |
| Thread A `put(k,v1)` + Thread B `put(k,v2)` → `get(k)` returns one of v1,v2 | Last-writer-wins, no torn reads |
| Concurrent resize + reads → all reads return valid data | Resize doesn't lose in-flight reads |
| 100 threads, 10K ops each → final state is consistent | Stress test: no deadlocks, no lost keys |

**Distributed coherence (multi-node):**

| Test | What It Verifies |
|---|---|
| Write to node A, read from node B → eventually consistent | Replication/invalidation works |
| Delete on node A → node B doesn't serve stale value | Invalidation propagation |
| Node A caches key, DB updated externally → TTL forces refresh | Staleness bounded by TTL |
| Two nodes write same key concurrently → no split-brain | Conflict resolution (last-write-wins / vector clock) |

### Phase 5: Edge Cases & Adversarial

| Test | What It Verifies |
|---|---|
| `capacity = 1` → put A, put B → only B exists | Minimum capacity works |
| `capacity = 0` → every put evicts immediately | Degenerate case |
| Same key put 1M times → no memory leak | Node reuse, not new allocation |
| All keys hash to same bucket → still correct (O(n) but correct) | Worst-case collision handling |
| Insert `MAX_INT` keys → graceful OOM or eviction | Memory pressure behavior |
| `null` key / `null` value → defined behavior | Null handling |
| Very large values (1MB+) → eviction frees memory | Memory accounting per entry |
| Rapid put/delete/put/delete same key → consistent state | Lifecycle thrashing |

### Phase 6: Performance / SLA

| Test | What It Verifies |
|---|---|
| 1M `get()` on full cache → p99 < X µs | Read latency SLA |
| `get()` latency doesn't degrade with cache size | O(1) confirmed, not O(n) |
| Eviction cost is constant regardless of cache size | O(1) eviction, not O(n) scan |
| Resize latency: no single op takes > Y ms | Incremental rehash, no stop-the-world |
| Hit ratio > 90% with Zipfian workload | LRU is effective for skewed access patterns |
| Memory usage ≤ expected (entry size × capacity + overhead) | No memory leaks |

### Test Progression Summary

```
Boilerplate LRU:    Phase 1 ✅ Phase 2 ❌ (eviction order bugs)
                    Phase 3 ❌ (no resize)  Phase 4 ❌ (no locking)

Proper LRU:         Phase 1 ✅ Phase 2 ✅ Phase 3 ✅
                    Phase 4 ✅ Phase 5 ✅ Phase 6 ✅
```

---

## 7. Quick Reference — Code Sketch (Python)

```python
class LRUCache:
    def __init__(self, capacity):
        self.cache = OrderedDict()
        self.capacity = capacity

    def get(self, key):
        if key not in self.cache:
            return -1
        self.cache.move_to_end(key)
        return self.cache[key]

    def put(self, key, value):
        if key in self.cache:
            self.cache.move_to_end(key)
        self.cache[key] = value
        if len(self.cache) > self.capacity:
            self.cache.popitem(last=False)  # evict LRU
```

## 8. More problems

```
1. HashMap — The one you asked about

 Surface:    dict[key] = value
 Beneath:    hashing → buckets → collision chains → load factor
             → resize → incremental rehash → consistent hashing (distributed)
             → CPU cache behavior → security (hash flooding attacks → SipHash)

2. Database Index (B+ Tree)

 Surface:    SELECT * WHERE id = 42
 Beneath:    B+ tree with fan-out 100+ → disk page alignment
             → buffer pool → WAL logging → MVCC versioning
             → range scans via leaf-level linked list

3. malloc / Memory Allocator

 Surface:    new Object()
 Beneath:    Free lists → slab allocation → buddy system
             → mmap/brk syscalls → TLB misses → NUMA awareness
             → GC generations → compaction → write barriers

4. ConcurrentDictionary / ConcurrentHashMap

 Surface:    thread-safe dict
 Beneath:    Lock striping → per-bucket locks → atomic CAS
             → memory ordering (volatile/barriers)
             → false sharing avoidance (cache-line padding)
             → lock-free reads with hazard pointers

5. git (Merkle DAG)

 Surface:    git commit / git merge
 Beneath:    Content-addressable SHA tree → DAG of commits
             → 3-way merge algorithm → packfiles with delta compression
             → reflog → garbage collection of unreachable objects

The Pattern

 What we know:     Interface  →  O(1) / O(log n) / "it works"
 What's hidden:    Hashing, balancing, resizing, concurrency,
                   memory layout, CPU cache effects, failure modes
```