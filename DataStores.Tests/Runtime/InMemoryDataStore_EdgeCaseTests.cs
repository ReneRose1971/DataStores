using DataStores.Abstractions;
using DataStores.Runtime;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Edge case tests for InMemoryDataStore.
/// </summary>
public class InMemoryDataStore_EdgeCaseTests
{
    [Fact]
    public void Add_Should_AcceptNullName()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var item = new TestItem { Id = 1, Name = null! };

        // Act
        store.Add(item);

        // Assert
        Assert.Single(store.Items);
        Assert.Null(store.Items[0].Name);
    }

    [Fact]
    public void AddRange_WithEmptyCollection_Should_NotRaiseEvent()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        int eventCount = 0;
        
        store.Changed += (s, e) => eventCount++;

        // Act
        store.AddRange(Array.Empty<TestItem>());

        // Assert
        Assert.Equal(0, eventCount);
        Assert.Empty(store.Items);
    }

    [Fact]
    public void Remove_NonExistentItem_Should_ReturnFalse()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var item = new TestItem { Id = 999, Name = "NotAdded" };

        // Act
        var removed = store.Remove(item);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void Contains_Should_WorkWithCustomComparer()
    {
        // Arrange
        var comparer = new IdOnlyComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        store.Add(new TestItem { Id = 1, Name = "Original" });

        // Act
        var contains = store.Contains(new TestItem { Id = 1, Name = "Different" });

        // Assert - Should find by Id only
        Assert.True(contains);
    }

    [Fact]
    public void Clear_OnEmptyStore_Should_NotThrow()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();

        // Act & Assert
        store.Clear();
        Assert.Empty(store.Items);
    }

    [Fact]
    public void Items_Should_ReturnNewSnapshot_EachTime()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        store.Add(new TestItem { Id = 1, Name = "A" });

        // Act
        var snapshot1 = store.Items;
        store.Add(new TestItem { Id = 2, Name = "B" });
        var snapshot2 = store.Items;

        // Assert
        Assert.Single(snapshot1);
        Assert.Equal(2, snapshot2.Count);
        Assert.NotSame(snapshot1, snapshot2);
    }

    [Fact]
    public void Changed_Should_ProvideCorrectAffectedItems_OnAdd()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        DataStoreChangedEventArgs<TestItem>? receivedArgs = null;
        
        store.Changed += (s, e) => receivedArgs = e;

        var item = new TestItem { Id = 1, Name = "Test" };

        // Act
        store.Add(item);

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(DataStoreChangeType.Add, receivedArgs.ChangeType);
        Assert.Single(receivedArgs.AffectedItems);
        Assert.Same(item, receivedArgs.AffectedItems[0]);
    }

    [Fact]
    public void Changed_Should_ProvideCorrectAffectedItems_OnBulkAdd()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        DataStoreChangedEventArgs<TestItem>? receivedArgs = null;
        
        store.Changed += (s, e) => receivedArgs = e;

        var items = new[]
        {
            new TestItem { Id = 1, Name = "A" },
            new TestItem { Id = 2, Name = "B" }
        };        // Act
        store.AddRange(items);

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(DataStoreChangeType.BulkAdd, receivedArgs.ChangeType);
        Assert.Equal(2, receivedArgs.AffectedItems.Count);
    }

    [Fact]
    public void Changed_Should_ProvideEmptyAffectedItems_OnClear()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        store.Add(new TestItem { Id = 1, Name = "A" });
        
        DataStoreChangedEventArgs<TestItem>? receivedArgs = null;
        store.Changed += (s, e) => receivedArgs = e;

        // Act
        store.Clear();

        // Assert
        Assert.NotNull(receivedArgs);
        Assert.Equal(DataStoreChangeType.Clear, receivedArgs.ChangeType);
        Assert.Empty(receivedArgs.AffectedItems);
    }

    [Fact]
    public void Multiple_Subscribers_Should_AllReceiveEvents()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        int subscriber1Count = 0;
        int subscriber2Count = 0;
        int subscriber3Count = 0;

        store.Changed += (s, e) => subscriber1Count++;
        store.Changed += (s, e) => subscriber2Count++;
        store.Changed += (s, e) => subscriber3Count++;

        // Act
        store.Add(new TestItem { Id = 1, Name = "Test" });

        // Assert
        Assert.Equal(1, subscriber1Count);
        Assert.Equal(1, subscriber2Count);
        Assert.Equal(1, subscriber3Count);
    }

    [Fact]
    public void AddRange_WithDuplicates_Should_ThrowException()
    {
        // Arrange
        var store = new InMemoryDataStore<TestItem>();
        var item = new TestItem { Id = 1, Name = "A" };

        // Act & Assert - NEW BEHAVIOR: Duplicate prevention in AddRange
        Assert.Throws<InvalidOperationException>(() => 
            store.AddRange(new[] { item, item, item }));

        // Store should be empty (transaction failed)
        Assert.Empty(store.Items);
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class IdOnlyComparer : IEqualityComparer<TestItem>
    {
        public bool Equals(TestItem? x, TestItem? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Id == y.Id;
        }

        public int GetHashCode(TestItem obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
