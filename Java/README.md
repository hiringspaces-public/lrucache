# LRU Cache — Candidate Brief (Java)

## What you are working with

You have been given a generic LRU (Least Recently Used) cache implementation
in Java. The cache combines a `HashMap` for O(1) key lookup with a
doubly-linked list to track access order.

The code compiles and mostly works. It also contains bugs. Your job is to find
them, explain them, fix them, and prove the fix with tests.

AI tools are allowed and encouraged. How you use them is part of what is
being assessed.

## Files

| File | Purpose |
|---|---|
| `ICache.java` | Shared interface — do not modify |
| `LRUCache.java` | Core implementation — start here |
| `Node.java` | Doubly-linked list node |
| `LFUCache.java` | LFU implementation — mid level and above |
| `ScalableLRUCache.java` | Striped implementation — senior and above |
| `BasicCorrectnessTests.java` | Phase 1 — basic get/put/size/eviction |
| `EvictionOrderTests.java` | Phase 2 — LRU ordering correctness |
| `LFUCacheTests.java` | Phase 3 — LFU contract |
| `ConcurrencyTests.java` | Phase 4 — thread safety |
| `ScalableLRUCacheTests.java` | Phase 5 — resize and coherence |

---

## Junior

Read `LRUCache.java` and understand the structure. Be ready to explain what
the sentinel head and tail nodes are doing and why they exist. Trace a `put`
followed by a `get` and describe the list state at each step.

There is at least one bug. Find it, explain what invariant it violates, fix
it, and write a test that fails before your fix and passes after.

---

## Mid level

Find all bugs by reading the code before running any tests. Explain why each
is a bug in terms of observable behaviour. Fix both with targeted tests.

Then open `LFUCache.java`. The data structures are already in place.
Understand how `minFreq` works and be ready to explain `_minFreq` maintenance
through a sequence of gets and puts.

---

## Senior

Fix both bugs and understand the LFU implementation.

Then look at the concurrency model in `LRUCache.java`. Is every public method
safe? Why is `synchronized(this)` an antipattern? Why does
`ReadWriteLock` not help here?

---

## Staff

Complete everything above.

Then open `ScalableLRUCache.java`. This is a striped LRU cache where the
keyspace is divided into N stripes, each with its own lock. Walk through
`resize`. Identify every failure mode: lock ordering, volatile correctness,
and capacity incoherence.

---

## What is being assessed

- Can you read and understand unfamiliar code?
- Can you identify not just where a bug is but why it is a bug?
- Can you reason about correctness under concurrency?
- Can you use AI as a tool while owning the reasoning yourself?

If you use AI to produce a fix, expect to be asked to break the code in a
different way and write a test that catches it.
