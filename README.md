# LRU Cache — Candidate Brief

You have been given a generic LRU Cache implementation. It compiles. It mostly works. It has bugs.

Your job: find them, explain them, fix them, and prove the fix with tests.

AI tools are allowed and encouraged. Use inbuilt whiteboard for discussions

---

## Files

| File | Purpose |
|---|---|
| `ICache.*` | Shared interface — do not modify |
| `LRUCache.*` | Core implementation — start here |
| `LFUCache.*` | Skeleton — mid level and above |
| `ScalableLRUCache.*` | Skeleton — senior and above |
| `LRUCache.Tests/` | Test project — add your tests here |

---

## Your Level

**Junior**

Read `LRUCache.*`. Understand the structure what are the sentinel head and tail nodes doing and why do they exist? Trace a `Put` followed by a `Get` and describe the list state at each step. Find the bug, explain what invariant it violates, fix it, and write a test that fails before your fix and passes after.

**Mid**

Find both bugs. Explain each in terms of observable behaviour not just where the bad line is. Fix both with targeted tests. Explain how cache interact at higher loads and make thread safe

**Senior**

Fix both bugs and fix concurrancy issues and implement the LFU cache as above.

**Staff**

Complete everything above. Then implement `TryGet`, `Put`, and `Resize` in `ScalableLRUCache.cs`  a striped LRU where the keyspace is divided into N stripes, each with its own lock and LRU list.

---

## What is being assessed

- Can you read and understand unfamiliar code
- Can you identify not just where a bug is but why it is a bug
- Can you reason about correctness under concurrency and system design tradeoffs
- Can you use AI as a tool while owning the reasoning yourself

---

## Environment

The workspace may take a few seconds to fully initialise. If you see unresolved imports or build errors, run the commands below.

```bash
# Java
mvn install -f Java/pom.xml

# C#
dotnet restore DotNet/LRUCache.sln
dotnet build DotNet/LRUCache.sln
```

**Running tests**

```bash
# Java
mvn test -f Java/pom.xml

# C#
dotnet test DotNet/LRUCache.sln
```

To run a single test file — right click the file in the Explorer and select **Run Tests**.

**Draw.io** may take up to 30 seconds to load on first open.
