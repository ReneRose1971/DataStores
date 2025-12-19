using DataStores.Abstractions;
using DataStores.Runtime;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Concurrency and thread-safety tests for GlobalStoreRegistry.
/// </summary>
public class GlobalStoreRegistry_ConcurrencyTests
{
    [Fact]
    public void RegisterGlobal_Should_BeThreadSafe_WithConcurrentCalls()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var exceptions = new List<Exception>();

        // Act - Try to register 100 different types concurrently
        Parallel.For(0, 100, i =>
        {
            try
            {
                var store = new InMemoryDataStore<TestItem>();
                // Use different types by wrapping in generic
                if (i % 2 == 0)
                {
                    registry.RegisterGlobal(store);
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

        // Assert - Should handle concurrent access without crashes
        Assert.Empty(exceptions.Where(e => !(e is GlobalStoreAlreadyRegisteredException)));
    }

    [Fact]
    public void ResolveGlobal_Should_BeThreadSafe_WithConcurrentReads()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        var results = new List<IDataStore<TestItem>>();
        var exceptions = new List<Exception>();

        // Act - 100 concurrent reads
        Parallel.For(0, 100, _ =>
        {
            try
            {
                var resolved = registry.ResolveGlobal<TestItem>();
                lock (results)
                {
                    results.Add(resolved);
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
        Assert.Equal(100, results.Count);
        Assert.All(results, r => Assert.Same(store, r));
    }

    [Fact]
    public void TryResolveGlobal_Should_NotFailUnderLoad()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        var successCount = 0;
        var exceptions = new List<Exception>();

        // Act - 100 concurrent TryResolve calls
        Parallel.For(0, 100, _ =>
        {
            try
            {
                if (registry.TryResolveGlobal<TestItem>(out var resolved))
                {
                    Interlocked.Increment(ref successCount);
                    Assert.Same(store, resolved);
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
        Assert.Equal(100, successCount);
    }

    [Fact]
    public void ConcurrentRegisterAndResolve_Should_NotDeadlock()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store1 = new InMemoryDataStore<TestItem>();
        var store2 = new InMemoryDataStore<OtherTestItem>();
        
        registry.RegisterGlobal(store1);

        var exceptions = new List<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - Mixed operations
        var tasks = new List<Task>();

        tasks.Add(Task.Run(() =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var resolved = registry.ResolveGlobal<TestItem>();
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        }));

        tasks.Add(Task.Run(() =>
        {
            try
            {
                registry.RegisterGlobal(store2);
            }
            catch (Exception ex)
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        }));

        Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(6));

        // Assert - No deadlock occurred (completed within timeout)
        Assert.True(tasks.All(t => t.IsCompleted));
        Assert.Empty(exceptions.Where(e => !(e is GlobalStoreAlreadyRegisteredException)));
    }

    [Fact]
    public void RegisterGlobal_CalledTwiceForSameType_Should_ThrowOnSecond()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store1 = new InMemoryDataStore<TestItem>();
        var store2 = new InMemoryDataStore<TestItem>();

        // Act & Assert
        registry.RegisterGlobal(store1);
        
        var ex = Assert.Throws<GlobalStoreAlreadyRegisteredException>(() =>
            registry.RegisterGlobal(store2));
        
        Assert.Equal(typeof(TestItem), ex.StoreType);
    }

    [Fact]
    public void TryResolveGlobal_ForMissingType_Should_ReturnFalse()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();

        // Act
        var found = registry.TryResolveGlobal<TestItem>(out var store);

        // Assert
        Assert.False(found);
        Assert.Null(store);
    }

    [Fact]
    public void ResolveGlobal_ForMissingType_Should_ThrowWithTypeName()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();

        // Act & Assert
        var ex = Assert.Throws<GlobalStoreNotRegisteredException>(() =>
            registry.ResolveGlobal<TestItem>());

        Assert.Contains(nameof(TestItem), ex.Message);
        Assert.Equal(typeof(TestItem), ex.StoreType);
    }

    [Fact]
    public void ConcurrentMixedOperations_Should_MaintainConsistency()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store1 = new InMemoryDataStore<TestItem>();
        var store2 = new InMemoryDataStore<OtherTestItem>();

        registry.RegisterGlobal(store1);
        registry.RegisterGlobal(store2);

        var resolveCount = 0;
        var tryResolveCount = 0;
        var exceptions = new List<Exception>();

        // Act - Mixed concurrent operations
        Parallel.For(0, 100, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    var resolved = registry.ResolveGlobal<TestItem>();
                    Interlocked.Increment(ref resolveCount);
                }
                else
                {
                    if (registry.TryResolveGlobal<OtherTestItem>(out _))
                    {
                        Interlocked.Increment(ref tryResolveCount);
                    }
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
        Assert.Equal(50, resolveCount);
        Assert.Equal(50, tryResolveCount);
    }

    [Fact]
    public void StressTest_1000ConcurrentOperations_Should_Succeed()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        var successCount = 0;
        var exceptions = new List<Exception>();

        // Act - 1000 concurrent resolve operations
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
    public void MultipleTypes_Should_BeIndependent()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var store1 = new InMemoryDataStore<TestItem>();
        var store2 = new InMemoryDataStore<OtherTestItem>();

        // Act
        registry.RegisterGlobal(store1);
        registry.RegisterGlobal(store2);

        var resolved1 = registry.ResolveGlobal<TestItem>();
        var resolved2 = registry.ResolveGlobal<OtherTestItem>();

        // Assert
        Assert.Same(store1, resolved1);
        Assert.Same(store2, resolved2);
        Assert.NotSame(resolved1, resolved2);
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class OtherTestItem
    {
        public int Id { get; set; }
        public string Value { get; set; } = "";
    }
}
