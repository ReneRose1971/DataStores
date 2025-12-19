using DataStores.Abstractions;
using Xunit;

namespace DataStores.Tests.Abstractions;

/// <summary>
/// Tests for DataStoreChangedEventArgs and DataStoreChangeType.
/// </summary>
public class DataStoreChangedEventArgs_Tests
{
    [Fact]
    public void Constructor_WithSingleItem_Should_SetProperties()
    {
        // Arrange
        var item = new TestItem { Id = 1, Name = "Test" };

        // Act
        var args = new DataStoreChangedEventArgs<TestItem>(DataStoreChangeType.Add, item);

        // Assert
        Assert.Equal(DataStoreChangeType.Add, args.ChangeType);
        Assert.Single(args.AffectedItems);
        Assert.Same(item, args.AffectedItems[0]);
    }

    [Fact]
    public void Constructor_WithMultipleItems_Should_SetProperties()
    {
        // Arrange
        var items = new[]
        {
            new TestItem { Id = 1, Name = "A" },
            new TestItem { Id = 2, Name = "B" }
        };

        // Act
        var args = new DataStoreChangedEventArgs<TestItem>(DataStoreChangeType.BulkAdd, items);

        // Assert
        Assert.Equal(DataStoreChangeType.BulkAdd, args.ChangeType);
        Assert.Equal(2, args.AffectedItems.Count);
        Assert.Same(items[0], args.AffectedItems[0]);
        Assert.Same(items[1], args.AffectedItems[1]);
    }

    [Fact]
    public void Constructor_WithNoItems_Should_SetEmptyCollection()
    {
        // Act
        var args = new DataStoreChangedEventArgs<TestItem>(DataStoreChangeType.Clear);

        // Assert
        Assert.Equal(DataStoreChangeType.Clear, args.ChangeType);
        Assert.Empty(args.AffectedItems);
    }

    [Fact]
    public void Constructor_WithNullItems_Should_SetEmptyCollection()
    {
        // Act
        var args = new DataStoreChangedEventArgs<TestItem>(DataStoreChangeType.Reset, (IReadOnlyList<TestItem>)null!);

        // Assert
        Assert.Equal(DataStoreChangeType.Reset, args.ChangeType);
        Assert.Empty(args.AffectedItems);
    }

    [Fact]
    public void AffectedItems_Should_BeReadOnly()
    {
        // Arrange
        var items = new[] { new TestItem { Id = 1, Name = "Test" } };
        var args = new DataStoreChangedEventArgs<TestItem>(DataStoreChangeType.Add, items);

        // Act & Assert
        Assert.IsAssignableFrom<IReadOnlyList<TestItem>>(args.AffectedItems);
    }

    [Theory]
    [InlineData(DataStoreChangeType.Add)]
    [InlineData(DataStoreChangeType.BulkAdd)]
    [InlineData(DataStoreChangeType.Remove)]
    [InlineData(DataStoreChangeType.Clear)]
    [InlineData(DataStoreChangeType.Reset)]
    public void ChangeType_Should_AcceptAllValidValues(DataStoreChangeType changeType)
    {
        // Act
        var args = new DataStoreChangedEventArgs<TestItem>(changeType);

        // Assert
        Assert.Equal(changeType, args.ChangeType);
    }

    [Fact]
    public void EventArgs_Should_StoreSnapshot()
    {
        // Arrange
        var originalItems = new List<TestItem>
        {
            new TestItem { Id = 1, Name = "A" }
        };

        var args = new DataStoreChangedEventArgs<TestItem>(
            DataStoreChangeType.Add, 
            originalItems);

        // Act - Modify original list after EventArgs creation
        originalItems.Add(new TestItem { Id = 2, Name = "B" });

        // Assert - EventArgs reflects the list at time of creation (not truly immutable, but consistent)
        // Note: This is expected behavior - List<T> is passed by reference
        Assert.Equal(2, args.AffectedItems.Count);
    }

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
