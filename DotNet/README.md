# LRU Cache — Candidate Brief

## What you are working with

You have been given a generic LRU (Least Recently Used) cache implementation
in C#. The cache combines a `Dictionary` for O(1) key lookup with a
doubly-linked list to track access order. 

The code compiles and mostly works. It also contains bugs. Your job is to find them, explain them, fix them, and prove the fix with tests.

AI tools are allowed and encouraged. How you use them is part of what is being assessed.

---

## Files you will work with

| File | Purpose |
|---|---|
| `ICache.cs` | Shared interface — do not modify |
| `LRUCache.cs` | Core implementation — start here |
| `LFUCache.cs` | Skeleton — mid level and above |
| `ScalableLRUCache.cs` | Skeleton — senior and above |
| `LRUCache.Tests/` | Test project — add your tests here |

---

## Junior

Read `LRUCache.cs` and understand the structure. Be ready to explain what the sentinel head and tail nodes are doing and why they exist. Trace a `Put`
followed by a `Get` and describe the list state at each step.

There is at least one bug. Find it, explain what invariant it violates, fix it, and write a test that fails before your fix and passes after.

---

## Mid level

Find bugs. Explain why each is a bug in terms of observable behaviour, not just where the bad line is. Fix both with targeted tests.

Then open `LFUCache.cs`. The data structures are already in place. Implement `TryGet` and `Put` in O(1). 

---

## Senior

Fix both bugs and implement the LFU cache as above.

---

## Staff

Complete everything above.

Then open `ScalableLRUCache.cs`. This is a striped LRU cache where the keyspace is divided into N stripes, each with its own lock and its own LRU
list. Implement `TryGet`, `Put`, and `Resize`.

---

## What is being assessed

- Can you read and understand unfamiliar code 
- Can you identify not just where a bug is but why it is a bug
- Can you reason about correctness under concurrency and system design tradeoffs
- Can you use AI as a tool while owning the reasoning yourself

If you use AI to produce a fix, expect to be asked to break the code in a different way and write a test that catches it.