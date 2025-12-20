using DataStores.Runtime;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Tests for thread-safety of InMemoryDataStore.
/// </summary>
public class InMemoryDataStore_ThreadSafetyTests
{
    [Fact]
    public async Task Add_Should_BeThreadSafe()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var tasks = new List<Task>();

        // Act - 100 concurrent adds
        for (int i = 0; i < 100; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() => store.Add(new TestItem { Id = id, Name = $"Item{id}" })));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, store.Items.Count);
    }

    [Fact]
    public async Task Remove_Should_BeThreadSafe()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var items = new List<TestItem>();
        
        for (int i = 0; i < 100; i++)
        {
            var item = new TestItem { Id = i, Name = $"Item{i}" };
            items.Add(item);
            store.Add(item);
        }

        var tasks = new List<Task>();

        // Act - Concurrent removes
        foreach (var item in items)
        {
            var itemToRemove = item;
            tasks.Add(Task.Run(() => store.Remove(itemToRemove)));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task MixedOperations_Should_BeThreadSafe()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var tasks = new List<Task>();

        // Act - Mixed concurrent operations
        for (int i = 0; i < 50; i++)
        {
            var id = i;
            
            // Add task
            tasks.Add(Task.Run(() => store.Add(new TestItem { Id = id, Name = $"Item{id}" })));
            
            // Contains task
            tasks.Add(Task.Run(() => { var _ = store.Contains(new TestItem { Id = id }); }));
        }

        await Task.WhenAll(tasks);

        // Assert - All adds completed successfully
        Assert.Equal(50, store.Items.Count);
    }

    [Fact]
    public async Task Items_Snapshot_Should_NotThrow_DuringModification()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var exceptions = new List<Exception>();

        // Add initial items
        for (int i = 0; i < 10; i++)
        {
            store.Add(new TestItem { Id = i, Name = $"Item{i}" });
        }

        var tasks = new List<Task>();

        // Act - Read snapshot while modifying
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var snapshot = store.Items;
                    foreach (var item in snapshot)
                    {
                        var _ = item.Name;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));

            // Concurrent modifications
            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(() => store.Add(new TestItem { Id = i + 100, Name = $"New{i}" })));
            }
        }

        await Task.WhenAll(tasks);

        // Assert - No exceptions occurred
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task AddRange_Should_BeThreadSafe()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var tasks = new List<Task>();

        // Act - Concurrent AddRange operations
        for (int i = 0; i < 10; i++)
        {
            var startId = i * 10;
            tasks.Add(Task.Run(() =>
            {
                var items = Enumerable.Range(startId, 10)
                    .Select(id => new TestItem { Id = id, Name = $"Item{id}" })
                    .ToArray();
                store.AddRange(items);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, store.Items.Count);
    }

    [Fact]
    public async Task Clear_Should_BeThreadSafe_WithConcurrentReads()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        
        for (int i = 0; i < 100; i++)
        {
            store.Add(new TestItem { Id = i, Name = $"Item{i}" });
        }

        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Act - Clear while reading
        tasks.Add(Task.Run(() => store.Clear()));

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var snapshot = store.Items;
                    var count = snapshot.Count;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Empty(store.Items);
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
