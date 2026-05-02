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
 * Phase 5 — Scalable LRU cache: resize and concurrent correctness.
 *
 * These tests verify stripe-based partitioning, resize correctness under
 * quiescence, and safety under concurrent load.
 */
@Tag("Phase5")
class ScalableLRUCacheTests {

    @Test
    @DisplayName("same-capacity resize preserves all keys")
    void resize_SameCapacity_PreservesAllKeys() {
        var cache = new ScalableLRUCache<Integer, Integer>(100, 4);
        for (int i = 0; i < 80; i++) cache.put(i, i * 10);

        cache.resize(100, 8); // same total capacity, more stripes

        for (int i = 0; i < 80; i++)
            assertEquals(i * 10, cache.get(i),
                    "Key " + i + " missing after resize");
    }

    @Test
    @DisplayName("shrink evicts the coldest keys")
    void resize_Shrink_EjectsColdestKeys() {
        var cache = new ScalableLRUCache<Integer, Integer>(100, 4);
        for (int i = 0; i < 100; i++) cache.put(i, i);

        // Access keys 0-9 to make them hot
        for (int i = 0; i < 10; i++) cache.get(i);

        cache.resize(40, 4); // shrink — only 40 slots survive

        assertTrue(cache.size() <= 40, "size exceeds new capacity after shrink");

        // Hot keys should have a higher survival rate than cold ones
        long hotSurvived = 0;
        for (int i = 0; i < 10; i++) if (cache.get(i) != null) hotSurvived++;
        // At least some hot keys should survive
        assertTrue(hotSurvived > 0, "No hot keys survived the shrink");
    }

    @Test
    @DisplayName("concurrent reads and writes do not corrupt the cache")
    void concurrentReadsAndWrites_DoNotCorrupt() throws Exception {
        var cache = new ScalableLRUCache<Integer, Integer>(200, 4);
        for (int i = 0; i < 100; i++) cache.put(i, i);

        var error = new AtomicBoolean(false);
        runConcurrently(8, threadId -> {
            try {
                ThreadLocalRandom rng = ThreadLocalRandom.current();
                for (int i = 0; i < 3000; i++) {
                    int key = rng.nextInt(200);
                    if (rng.nextBoolean()) cache.put(key, key);
                    else cache.get(key);
                }
            } catch (Exception e) {
                error.set(true);
            }
        });

        assertFalse(error.get(), "Exception thrown during concurrent access");
        assertTrue(cache.size() <= 200, "size exceeded capacity");
    }

    @Test
    @DisplayName("resize under concurrent read load leaves cache usable")
    void resize_UnderConcurrentLoad_CacheRemainsUsable() throws Exception {
        var cache  = new ScalableLRUCache<Integer, Integer>(200, 4);
        for (int i = 0; i < 100; i++) cache.put(i, i);

        var error   = new AtomicBoolean(false);
        var latch   = new CountDownLatch(1);
        var executor = Executors.newFixedThreadPool(5);
        List<Future<?>> futures = new ArrayList<>();

        // 4 reader threads
        for (int t = 0; t < 4; t++) {
            futures.add(executor.submit(() -> {
                latch.await();
                try {
                    ThreadLocalRandom rng = ThreadLocalRandom.current();
                    for (int i = 0; i < 2000; i++) cache.get(rng.nextInt(100));
                } catch (Exception e) {
                    error.set(true);
                }
                return null;
            }));
        }

        // 1 resize thread
        futures.add(executor.submit(() -> {
            latch.await();
            try {
                cache.resize(200, 8);
            } catch (Exception e) {
                error.set(true);
            }
            return null;
        }));

        latch.countDown();
        executor.shutdown();
        assertTrue(executor.awaitTermination(30, TimeUnit.SECONDS));
        for (Future<?> f : futures) f.get();

        assertFalse(error.get(), "Exception thrown during resize under concurrent load");

        // Cache must still be functional after resize
        cache.put(999, 999);
        assertEquals(999, cache.get(999));
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

        latch.countDown();
        executor.shutdown();
        assertTrue(executor.awaitTermination(30, TimeUnit.SECONDS), "Threads did not finish in time");
        for (Future<?> f : futures) f.get();
    }
}
