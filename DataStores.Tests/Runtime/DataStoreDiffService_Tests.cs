using DataStores.Abstractions;
using DataStores.Persistence;
using DataStores.Runtime;
using TestHelper.DataStores.Comparers;
using TestHelper.DataStores.Fakes;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Tests for DataStoreDiffService with automatic comparer resolution.
/// </summary>
[Trait("Category", "Unit")]
public class DataStoreDiffService_Tests
{
    [Fact]
    public void Constructor_WithNullComparerService_Should_Throw()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DataStoreDiffService(null!));
    }

    [Fact]
    public void ComputeDiff_WithNullSourceItems_Should_Throw()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        var targetItems = Array.Empty<TestDto>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            service.ComputeDiff<TestDto>(null!, targetItems));
    }

    [Fact]
    public void ComputeDiff_WithNullTargetItems_Should_Throw()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        var sourceItems = Array.Empty<TestDto>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            service.ComputeDiff(sourceItems, null!));
    }

    [Fact]
    public void ComputeDiff_BothEmpty_Should_ReturnEmptyDiff()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        var source = Array.Empty<TestDto>();
        var target = Array.Empty<TestDto>();

        // Act
        var diff = service.ComputeDiff(source, target);

        // Assert
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.ToInsert);
        Assert.Empty(diff.ToDelete);
    }

    [Fact]
    public void ComputeDiff_WithInserts_Should_DetectNewItems()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        
        // Use same references for "Existing" to ensure comparer recognizes them as equal
        var existingItem = new TestDto("Existing", 35);
        var source = new[]
        {
            new TestDto("New1", 25),
            new TestDto("New2", 30),
            existingItem
        };
        var target = new[]
        {
            existingItem // Same reference
        };

        // Act
        var diff = service.ComputeDiff(source, target);

        // Assert
        Assert.True(diff.HasChanges);
        Assert.Equal(2, diff.ToInsert.Count);
        Assert.Empty(diff.ToDelete);
    }

    [Fact]
    public void ComputeDiff_WithDeletes_Should_DetectRemovedItems()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        
        // Use same reference for "Kept"
        var keptItem = new TestDto("Kept", 25);
        var source = new[]
        {
            keptItem
        };
        var target = new[]
        {
            keptItem, // Same reference
            new TestDto("Deleted1", 30),
            new TestDto("Deleted2", 35)
        };

        // Act
        var diff = service.ComputeDiff(source, target);

        // Assert
        Assert.True(diff.HasChanges);
        Assert.Empty(diff.ToInsert);
        Assert.Equal(2, diff.ToDelete.Count);
    }

    [Fact]
    public void ComputeDiff_WithMixedChanges_Should_DetectBoth()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        
        // Use same reference for "Kept"
        var keptItem = new TestDto("Kept", 25);
        var source = new[]
        {
            keptItem,
            new TestDto("New", 30)
        };
        var target = new[]
        {
            keptItem, // Same reference
            new TestDto("Deleted", 35)
        };

        // Act
        var diff = service.ComputeDiff(source, target);

        // Assert
        Assert.True(diff.HasChanges);
        Assert.Single(diff.ToInsert);
        Assert.Single(diff.ToDelete);
    }

    [Fact]
    public void ComputeDiff_Identical_Should_ReturnNoDiff()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        var items = new[]
        {
            new TestDto("A", 25),
            new TestDto("B", 30),
            new TestDto("C", 35)
        };

        // Act
        var diff = service.ComputeDiff(items, items);

        // Assert
        Assert.False(diff.HasChanges);
        Assert.Empty(diff.ToInsert);
        Assert.Empty(diff.ToDelete);
    }

    [Fact]
    public void ComputeDiff_WithCustomComparer_Should_UseProvidedComparer()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);

        var source = new[]
        {
            new TestDto("Same", 25), // Same Name, different Age
            new TestDto("New", 30)
        };
        var target = new[]
        {
            new TestDto("Same", 99), // Same Name, different Age
            new TestDto("Deleted", 40)
        };

        // Act
        var diff = service.ComputeDiff(source, target, comparer);

        // Assert
        Assert.True(diff.HasChanges);
        Assert.Single(diff.ToInsert);  // "New"
        Assert.Single(diff.ToDelete);  // "Deleted"
        // "Same" is in both (according to Name comparer)
    }

    [Fact]
    public void ComputeDiff_WithEntityBase_Should_UseIdComparison()
    {
        // Arrange
        var comparerService = new FakeEqualityComparerService();
        var service = new DataStoreDiffService(comparerService);

        var source = new[]
        {
            new TestEntity { Id = 1, Name = "Kept" },
            new TestEntity { Id = 0, Name = "New" } // Id=0 = new
        };
        var target = new[]
        {
            new TestEntity { Id = 1, Name = "Kept" },
            new TestEntity { Id = 2, Name = "Deleted" }
        };

        // Act
        var diff = service.ComputeDiff(source, target);

        // Assert
        Assert.True(diff.HasChanges);
        Assert.Single(diff.ToInsert);  // Id=0
        Assert.Single(diff.ToDelete);  // Id=2
    }

    [Fact]
    public void ComputeDiff_Performance_LargeDatasets()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        
        // Create items that can be compared by reference
        var sourceItems = Enumerable.Range(1, 10000)
            .Select(i => new TestDto($"Item{i}", i))
            .ToArray();
        
        var targetItems = sourceItems.Take(9000).ToArray(); // Share first 9000 references

        // Act
        var startTime = DateTime.UtcNow;
        var diff = service.ComputeDiff(sourceItems, targetItems);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(diff.HasChanges);
        Assert.Equal(1000, diff.ToInsert.Count); // Items 9001-10000
        Assert.Empty(diff.ToDelete);
        Assert.True(duration.TotalSeconds < 1, $"Diff took {duration.TotalSeconds}s");
    }

    [Fact]
    public void ComputeDiff_PreservesOriginalReferences()
    {
        // Arrange
        var service = new DataStoreDiffService(new FakeEqualityComparerService());
        var sourceItem = new TestDto("New", 25);
        var targetItem = new TestDto("Delete", 30);

        var source = new[] { sourceItem };
        var target = new[] { targetItem };

        // Act
        var diff = service.ComputeDiff(source, target);

        // Assert
        Assert.Same(sourceItem, diff.ToInsert[0]);
        Assert.Same(targetItem, diff.ToDelete[0]);
    }
}
