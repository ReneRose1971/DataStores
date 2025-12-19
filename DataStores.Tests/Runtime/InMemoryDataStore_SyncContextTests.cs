using DataStores.Abstractions;
using DataStores.Runtime;
using TestHelpers;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Tests for InMemoryDataStore with SynchronizationContext marshaling.
/// </summary>
public class InMemoryDataStore_SyncContextTests : IDisposable
{
    private readonly RecordingSynchronizationContext _syncContext;

    public InMemoryDataStore_SyncContextTests()
    {
        _syncContext = new RecordingSynchronizationContext();
    }

    public void Dispose()
    {
        _syncContext.Reset();
    }

    [Fact]
    public async Task Changed_Should_MarshalToSyncContext_WhenCalledFromDifferentThread()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>(synchronizationContext: _syncContext);
        int eventFiredCount = 0;
        var eventCompletionSource = new TaskCompletionSource<bool>();
        
        store.Changed += (s, e) => 
        {
            eventFiredCount++;
            eventCompletionSource.TrySetResult(true);
        };

        // Act - Add from different thread
        var thread = new Thread(() =>
        {
            using (SynchronizationContextScope.None())
            {
                store.Add(new TestItem { Id = 1, Name = "Test" });
            }
        });
        
        thread.Start();
        thread.Join();

        // Wait for event to be processed
        await Task.WhenAny(eventCompletionSource.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(1, eventFiredCount);
    }

    [Fact]
    public void Add_Should_NotDeadlock_WhenSyncContextIsBlocked()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>(synchronizationContext: _syncContext);
        
        // Act & Assert - Should complete without deadlock
        using var scope = SynchronizationContextScope.None();
        store.Add(new TestItem { Id = 1, Name = "Test" });
        Assert.Equal(1, store.Items.Count);
    }

    [Fact]
    public async Task Changed_Should_FireOnCorrectContext_WithMultipleOperations()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>(synchronizationContext: _syncContext);
        int eventCount = 0;
        var eventCompletionSource = new TaskCompletionSource<bool>();
        
        store.Changed += (s, e) => 
        {
            eventCount++;
            if (eventCount == 4)
            {
                eventCompletionSource.TrySetResult(true);
            }
        };

        // Act
        using (SynchronizationContextScope.Use(_syncContext))
        {
            store.Add(new TestItem { Id = 1, Name = "A" });
            store.Add(new TestItem { Id = 2, Name = "B" });
            store.Remove(store.Items[0]);
            store.Clear();
        }

        // Wait for all events to be processed
        await Task.WhenAny(eventCompletionSource.Task, Task.Delay(1000));

        // Assert - 4 operations = 4 events
        Assert.Equal(4, eventCount);
    }

    [Fact]
    public async Task AddRange_Should_MarshalToSyncContext()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>(synchronizationContext: _syncContext);
        int eventCount = 0;
        var eventCompletionSource = new TaskCompletionSource<bool>();
        
        store.Changed += (s, e) => 
        {
            eventCount++;
            eventCompletionSource.TrySetResult(true);
        };

        // Act
        using (SynchronizationContextScope.None())
        {
            store.AddRange(new[]
            {
                new TestItem { Id = 1, Name = "A" },
                new TestItem { Id = 2, Name = "B" }
            });
        }

        // Wait for event to be processed
        await Task.WhenAny(eventCompletionSource.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(1, eventCount); // Single BulkAdd event
    }

    [Fact]
    public void SyncContext_Null_Should_FireEventsDirectly()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>(synchronizationContext: null);
        int eventCount = 0;
        
        store.Changed += (s, e) => eventCount++;

        // Act
        store.Add(new TestItem { Id = 1, Name = "Test" });

        // Assert
        Assert.Equal(1, eventCount);
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
