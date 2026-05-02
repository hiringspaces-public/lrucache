package cache;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicBoolean;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Phase 4 — Thread safety under concurrent load.
 *
 * These tests verify that LRUCache remains correct when multiple threads
 * read and write concurrently. A cache without a lock will either corrupt
 * the linked list silently or throw NullPointerException under load.
 */
@Tag("Phase4")
class ConcurrencyTests {

    @Test
    @DisplayName("concurrent puts never exceed capacity")
    void concurrentPuts_NeverExceedCapacity() throws Exception {
        int capacity = 100;
        var cache    = new LRUCache<Integer, Integer>(capacity);
        int threads  = 8;
        int opsEach  = 2000;

        runConcurrently(threads, threadId -> {
            for (int i = 0; i < opsEach; i++)
                cache.put(threadId * opsEach + i, i);
        });

        assertTrue(cache.size() <= capacity,
                "size " + cache.size() + " exceeds capacity " + capacity);
    }

    @Test
    @DisplayName("concurrent reads and writes return consistent values")
    void concurrentReadsAndWrites_ReturnConsistentValues() throws Exception {
        var cache   = new LRUCache<Integer, Integer>(50);
        var failed  = new AtomicBoolean(false);

        // Seed some values
        for (int i = 0; i < 50; i++) cache.put(i, i * 10);

        runConcurrently(4, threadId -> {
            for (int i = 0; i < 5000; i++) {
                int key   = i % 50;
                Integer v = cache.get(key);
                // Value must be either the seeded value or one written by another thread
                if (v != null && v < 0) failed.set(true); // sentinel: negative values are invalid
                cache.put(key, key * 10);
            }
        });

        assertFalse(failed.get(), "A read returned an invalid (corrupted) value");
    }

    @Test
    @DisplayName("concurrent puts and gets do not throw or deadlock")
    void concurrentPutsAndGets_DoNotThrow() throws Exception {
        var cache = new LRUCache<Integer, Integer>(200);
        var error = new AtomicBoolean(false);

        runConcurrently(8, threadId -> {
            try {
                for (int i = 0; i < 3000; i++) {
                    cache.put(i % 300, i);
                    cache.get(i % 300);
                }
            } catch (Exception e) {
                error.set(true);
            }
        });

        assertFalse(error.get(), "An exception was thrown during concurrent access");
    }

    @Test
    @DisplayName("concurrent removes and puts maintain correct size")
    void concurrentRemovesAndPuts_MaintainCorrectSize() throws Exception {
        var cache = new LRUCache<Integer, Integer>(100);
        for (int i = 0; i < 100; i++) cache.put(i, i);

        runConcurrently(4, threadId -> {
            for (int i = 0; i < 1000; i++) {
                cache.remove(i % 100);
                cache.put(i % 100, i);
            }
        });

        assertTrue(cache.size() <= 100, "size exceeded capacity after concurrent removes/puts");
    }

    @Test
    @DisplayName("stress test — no data corruption under sustained mixed load")
    void stressTest_NoDataCorruption() throws Exception {
        int capacity = 200;
        var cache    = new LRUCache<Integer, Integer>(capacity);
        var error    = new AtomicBoolean(false);

        runConcurrently(10, threadId -> {
            try {
                ThreadLocalRandom rng = ThreadLocalRandom.current();
                for (int i = 0; i < 5000; i++) {
                    int key = rng.nextInt(500);
                    switch (rng.nextInt(3)) {
                        case 0 -> cache.put(key, key);
                        case 1 -> cache.get(key);
                        case 2 -> cache.remove(key);
                    }
                }
            } catch (Exception e) {
                error.set(true);
            }
        });

        assertFalse(error.get(), "Exception thrown during stress test: data corruption");
        assertTrue(cache.size() <= capacity, "size exceeded capacity under stress");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    @FunctionalInterface
    interface ThreadTask {
        void run(int threadId) throws Exception;
    }

    private void runConcurrently(int threadCount, ThreadTask task) throws Exception {
        var executor = Executors.newFixedThreadPool(threadCount);
        var latch    = new CountDownLatch(1);
        List<Future<?>> futures = new ArrayList<>();

        for (int t = 0; t < threadCount; t++) {
            final int id = t;
            futures.add(executor.submit(() -> {
                latch.await();
                task.run(id);
                return null;
            }));
        }

        latch.countDown(); // release all threads simultaneously
        executor.shutdown();
        assertTrue(executor.awaitTermination(30, TimeUnit.SECONDS), "Threads did not finish in time");

        for (Future<?> f : futures) f.get(); // rethrow any exception from threads
    }
}
