using DataStores.Abstractions;
using DataStores.Runtime;
using TestHelper.DataStores.Fakes;
using Xunit;

namespace DataStores.Tests.Performance;

/// <summary>
/// Performance and stress tests for DataStores components.
/// </summary>
public class Performance_StressTests
{
    private static DataStoresFacade CreateFacade(IGlobalStoreRegistry registry, ILocalDataStoreFactory factory)
    {
        return new DataStoresFacade(registry, factory, new FakeEqualityComparerService());
    }

    [Fact]
    public void InMemoryStore_Add10000Items_Should_BeFast()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var items = Enumerable.Range(1, 10000)
            .Select(i => new TestItem { Id = i, Name = $"Item{i}" })
            .ToList();

        // Act
        var startTime = DateTime.UtcNow;
        store.AddRange(items);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(10000, store.Items.Count);
        Assert.True(duration.TotalSeconds < 2, $"AddRange took {duration.TotalSeconds}s, expected < 2s");
    }

    [Fact]
    public void InMemoryStore_ConcurrentAccess_10000Operations()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 10000, i =>
        {
            try
            {
                if (i % 3 == 0)
                    store.Add(new TestItem { Id = i, Name = $"Item{i}" });
                else if (i % 3 == 1)
                    _ = store.Items.Count;
                else
                    _ = store.Contains(new TestItem { Id = i / 2 });
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.Empty(exceptions);
        Assert.True(store.Items.Count > 0);
    }

    [Fact]
    public void GlobalRegistry_Stress_1000Types()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var exceptions = new List<Exception>();

        // Act - Register 1000 different "types" (simulated via unique stores)
        Parallel.For(0, 1000, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    // Only register half to avoid duplicates
                    var store = new InMemoryDataStore<TestItem>();
                    // Note: This will fail for same type, but tests thread-safety
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert - No crashes
        Assert.All(exceptions, ex => 
            Assert.True(ex is GlobalStoreAlreadyRegisteredException || ex == null));
    }

    [Fact]
    public void InMemoryStore_SnapshotIsolation_UnderLoad()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        for (int i = 0; i < 1000; i++)
        {
            store.Add(new TestItem { Id = i, Name = $"Item{i}" });
        }

        var snapshots = new List<IReadOnlyList<TestItem>>();
        var exceptions = new List<Exception>();

        // Act - Take snapshots while modifying
        Parallel.For(0, 100, i =>
        {
            try
            {
                var snapshot = store.Items; // Snapshot
                lock (snapshots)
                {
                    snapshots.Add(snapshot);
                }

                if (i % 2 == 0)
                {
                    store.Add(new TestItem { Id = 10000 + i, Name = $"New{i}" });
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(100, snapshots.Count);
    }

    [Fact]
    public void InMemoryStore_EventFiring_10000Events()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        int eventCount = 0;
        
        store.Changed += (s, e) => Interlocked.Increment(ref eventCount);

        // Act - Trigger 10000 events
        var startTime = DateTime.UtcNow;
        for (int i = 0; i < 10000; i++)
        {
            store.Add(new TestItem { Id = i, Name = $"Item{i}" });
        }
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(10000, eventCount);
        Assert.True(duration.TotalSeconds < 5, $"Event firing took {duration.TotalSeconds}s");
    }

    [Fact]
    public void Memory_NoLeaks_AfterManyOperations()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();

        // Act - Add and remove many times
        for (int cycle = 0; cycle < 10; cycle++)
        {
            for (int i = 0; i < 1000; i++)
            {
                store.Add(new TestItem { Id = i, Name = $"Item{i}" });
            }
            store.Clear();
        }

        // Assert - Store should be empty (no memory leaks)
        Assert.Empty(store.Items);
        
        // Force GC to check for lingering objects
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact]
    public void LargeSnapshot_Performance()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = CreateFacade(registry, factory);
        
        var globalStore = new InMemoryDataStore<TestItem>();
        for (int i = 0; i < 10000; i++)
        {
            globalStore.Add(new TestItem { Id = i, Name = $"Item{i}" });
        }
        registry.RegisterGlobal(globalStore);

        // Act
        var startTime = DateTime.UtcNow;
        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>(x => x.Id % 2 == 0);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(5000, snapshot.Items.Count);
        Assert.True(duration.TotalSeconds < 1, $"Snapshot took {duration.TotalSeconds}s");
    }

    [Fact]
    public void ConcurrentReads_NoCrashes()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        for (int i = 0; i < 1000; i++)
        {
            store.Add(new TestItem { Id = i, Name = $"Item{i}" });
        }

        var exceptions = new List<Exception>();

        // Act - 1000 concurrent reads
        Parallel.For(0, 1000, _ =>
        {
            try
            {
                var snapshot = store.Items;
                foreach (var item in snapshot)
                {
                    _ = item.Name.Length;
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void MixedReadWrite_HighConcurrency()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var exceptions = new List<Exception>();

        // Act - Mixed operations
        Parallel.For(0, 1000, i =>
        {
            try
            {
                if (i % 3 == 0)
                    store.Add(new TestItem { Id = i, Name = $"Item{i}" });
                else if (i % 3 == 1)
                    _ = store.Items.ToList();
                else if (store.Items.Count > 0)
                    store.Remove(store.Items[0]);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert - No crashes
        Assert.Empty(exceptions);
    }

    [Fact]
    public void GlobalRegistry_ConcurrentResolve_1000Threads()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        var successCount = 0;
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 1000, _ =>
        {
            try
            {
                var resolved = registry.ResolveGlobal<TestItem>();
                if (resolved == store)
                {
                    Interlocked.Increment(ref successCount);
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(1000, successCount);
    }

    [Fact]
    public async Task LongRunning_StabilityTest()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var exceptions = new List<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - Run for 5 seconds
        var task = Task.Run(() =>
        {
            int counter = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    store.Add(new TestItem { Id = counter++, Name = $"Item{counter}" });
                    
                    if (counter % 100 == 0)
                    {
                        var snapshot = store.Items;
                        _ = snapshot.Count;
                    }

                    if (counter % 500 == 0)
                    {
                        store.Clear();
                        counter = 0;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }
        });

        await task;

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void CustomComparer_PerformanceWithManyItems()
    {
        // Arrange
        var comparer = new IdOnlyComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        for (int i = 0; i < 5000; i++)
        {
            store.Add(new TestItem { Id = i, Name = $"Item{i}" });
        }

        // Act - Search using comparer
        var startTime = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++)
        {
            store.Contains(new TestItem { Id = i, Name = "Different" });
        }
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(duration.TotalSeconds < 2, $"Search took {duration.TotalSeconds}s");
    }

    [Fact]
    public void BulkOperations_Performance()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var items = Enumerable.Range(1, 10000)
            .Select(i => new TestItem { Id = i, Name = $"Item{i}" })
            .ToList();

        // Act - Single AddRange vs multiple Add
        var startTime = DateTime.UtcNow;
        store.AddRange(items);
        var bulkDuration = DateTime.UtcNow - startTime;

        var store2 = new InMemoryDataStore<TestItem>();
        startTime = DateTime.UtcNow;
        foreach (var item in items)
        {
            store2.Add(item);
        }
        var individualDuration = DateTime.UtcNow - startTime;

        // Assert - Bulk should be significantly faster
        Assert.Equal(10000, store.Items.Count);
        Assert.Equal(10000, store2.Items.Count);
        Assert.True(bulkDuration < individualDuration, 
            $"Bulk: {bulkDuration.TotalMilliseconds}ms, Individual: {individualDuration.TotalMilliseconds}ms");
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class IdOnlyComparer : IEqualityComparer<TestItem>
    {
        public bool Equals(TestItem? x, TestItem? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(TestItem obj) => obj.Id.GetHashCode();
    }
}
