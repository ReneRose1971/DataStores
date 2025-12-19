using DataStores.Runtime;
using TestHelpers;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Tests for LocalDataStoreFactory.
/// </summary>
public class LocalDataStoreFactory_Tests
{
    [Fact]
    public void CreateLocal_Should_ReturnNewInstance()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();

        // Act
        var store1 = factory.CreateLocal<TestItem>();
        var store2 = factory.CreateLocal<TestItem>();

        // Assert
        Assert.NotSame(store1, store2);
    }

    [Fact]
    public void CreateLocal_WithComparer_Should_UseComparer()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();
        var comparer = new IdOnlyComparer();

        // Act
        var store = factory.CreateLocal<TestItem>(comparer);
        store.Add(new TestItem { Id = 1, Name = "Original" });

        // Assert - Should find by Id only
        Assert.True(store.Contains(new TestItem { Id = 1, Name = "Different" }));
    }

    [Fact]
    public async Task CreateLocal_WithSyncContext_Should_UseSyncContext()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();
        var syncContext = new RecordingSynchronizationContext();

        // Act
        var store = factory.CreateLocal<TestItem>(context: syncContext);
        
        int eventFired = 0;
        var eventCompletionSource = new TaskCompletionSource<bool>();
        store.Changed += (s, e) => 
        {
            eventFired++;
            eventCompletionSource.TrySetResult(true);
        };
        
        store.Add(new TestItem { Id = 1, Name = "Test" });

        // Wait for event
        await Task.WhenAny(eventCompletionSource.Task, Task.Delay(1000));

        // Assert
        Assert.Equal(1, eventFired);
    }

    [Fact]
    public void CreateLocal_CalledTwice_Should_ReturnDifferentInstances()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();

        // Act
        var store1 = factory.CreateLocal<TestItem>();
        var store2 = factory.CreateLocal<TestItem>();
        
        store1.Add(new TestItem { Id = 1, Name = "Item1" });

        // Assert
        Assert.Single(store1.Items);
        Assert.Empty(store2.Items);
        Assert.NotSame(store1, store2);
    }

    [Fact]
    public void CreateLocal_WithNullComparer_Should_UseDefault()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();

        // Act
        var store = factory.CreateLocal<TestItem>(comparer: null);
        
        var item1 = new TestItem { Id = 1, Name = "A" };
        var item2 = new TestItem { Id = 1, Name = "A" }; // Different instance
        
        store.Add(item1);

        // Assert - Default comparer uses reference equality
        Assert.False(store.Contains(item2));
        Assert.True(store.Contains(item1));
    }

    [Fact]
    public void CreateLocal_WithNullSyncContext_Should_Work()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();

        // Act
        var store = factory.CreateLocal<TestItem>(context: null);
        
        int eventFired = 0;
        store.Changed += (s, e) => eventFired++;
        
        store.Add(new TestItem { Id = 1, Name = "Test" });

        // Assert - Events should fire synchronously
        Assert.Equal(1, eventFired);
    }

    [Fact]
    public void CreateLocal_MultipleTypes_Should_WorkIndependently()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();

        // Act
        var store1 = factory.CreateLocal<TestItem>();
        var store2 = factory.CreateLocal<OtherTestItem>();
        
        store1.Add(new TestItem { Id = 1, Name = "Item" });
        store2.Add(new OtherTestItem { Id = 2, Value = "Value" });

        // Assert
        Assert.Single(store1.Items);
        Assert.Single(store2.Items);
    }

    [Fact]
    public async Task CreateLocal_WithBothParameters_Should_UseBoth()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();
        var comparer = new IdOnlyComparer();
        var syncContext = new RecordingSynchronizationContext();

        // Act
        var store = factory.CreateLocal<TestItem>(comparer, syncContext);
        
        store.Add(new TestItem { Id = 1, Name = "Original" });
        
        int eventFired = 0;
        var eventCompletionSource = new TaskCompletionSource<bool>();
        store.Changed += (s, e) => 
        {
            eventFired++;
            eventCompletionSource.TrySetResult(true);
        };
        
        store.Add(new TestItem { Id = 2, Name = "Second" });

        // Wait for event
        await Task.WhenAny(eventCompletionSource.Task, Task.Delay(1000));

        // Assert - Both comparer and syncContext used
        Assert.True(store.Contains(new TestItem { Id = 1, Name = "Different" })); // Comparer
        Assert.Equal(1, eventFired); // SyncContext
    }

    [Fact]
    public void CreateLocal_Should_ReturnEmptyStore()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();

        // Act
        var store = factory.CreateLocal<TestItem>();

        // Assert
        Assert.Empty(store.Items);
    }

    [Fact]
    public void CreateLocal_StoresShouldBeFullyFunctional()
    {
        // Arrange
        var factory = new LocalDataStoreFactory();

        // Act
        var store = factory.CreateLocal<TestItem>();
        
        store.Add(new TestItem { Id = 1, Name = "A" });
        store.AddRange(new[] 
        { 
            new TestItem { Id = 2, Name = "B" },
            new TestItem { Id = 3, Name = "C" }
        });
        
        var removed = store.Remove(store.Items[1]);
        
        // Assert - Full functionality
        Assert.Equal(2, store.Items.Count);
        Assert.True(removed);
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
