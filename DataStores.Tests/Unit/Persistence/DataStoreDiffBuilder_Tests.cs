using DataStores.Abstractions;
using DataStores.Runtime;
using TestHelper.DataStores.TestSetup;
using Xunit;

namespace DataStores.Tests.Unit.Persistence;

/// <summary>
/// Unit-Tests für IDataStoreDiffService.
/// Testet die Diff-Berechnung zwischen DataStore und Datenbank.
/// </summary>
[Trait("Category", "Unit")]
public class DataStoreDiffService_Tests
{
    private readonly IDataStoreDiffService _diffService = TestDiffServiceFactory.Create();

    private class TestEntity : EntityBase
    {
        public string Name { get; set; } = "";

        public override string ToString() => $"TestEntity #{Id}: {Name}";
        public override bool Equals(object? obj) => obj is TestEntity other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }

    #region ComputeDiff - Basic Scenarios

    [Fact]
    public void ComputeDiff_WithBothEmpty_ReturnsEmptyDiff()
    {
        // Arrange
        var dataStoreItems = Array.Empty<TestEntity>();
        var databaseItems = Array.Empty<TestEntity>();

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Empty(diff.ToInsert);
        Assert.Empty(diff.ToDelete);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void ComputeDiff_WithNullDataStoreItems_ThrowsArgumentNullException()
    {
        // Arrange
        var databaseItems = Array.Empty<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _diffService.ComputeDiff<TestEntity>(null!, databaseItems));
    }

    [Fact]
    public void ComputeDiff_WithNullDatabaseItems_ThrowsArgumentNullException()
    {
        // Arrange
        var dataStoreItems = Array.Empty<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _diffService.ComputeDiff(dataStoreItems, null!));
    }

    #endregion

    #region INSERT Detection

    [Fact]
    public void ComputeDiff_WithNewItems_DetectsInserts()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New1" },
            new TestEntity { Id = 0, Name = "New2" },
            new TestEntity { Id = 0, Name = "New3" }
        };

        var databaseItems = Array.Empty<TestEntity>();

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Equal(3, diff.ToInsert.Count);
        Assert.Empty(diff.ToDelete);
        Assert.True(diff.HasChanges);
        Assert.All(diff.ToInsert, item => Assert.Equal(0, item.Id));
    }

    #endregion

    #region DELETE Detection

    [Fact]
    public void ComputeDiff_WithRemovedItems_DetectsDeletes()
    {
        // Arrange
        var dataStoreItems = Array.Empty<TestEntity>();

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Deleted1" },
            new TestEntity { Id = 2, Name = "Deleted2" }
        };

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Empty(diff.ToInsert);
        Assert.Equal(2, diff.ToDelete.Count);
        Assert.True(diff.HasChanges);
        Assert.Contains(diff.ToDelete, item => item.Id == 1);
        Assert.Contains(diff.ToDelete, item => item.Id == 2);
    }

    [Fact]
    public void ComputeDiff_WhenItemRemovedFromDataStore_DetectsDelete()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Kept" }
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Kept" },
            new TestEntity { Id = 2, Name = "Removed" }
        };

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Empty(diff.ToInsert);
        Assert.Single(diff.ToDelete);
        Assert.Equal(2, diff.ToDelete[0].Id);
    }

    #endregion

    #region Mixed Scenarios

    [Fact]
    public void ComputeDiff_WithMixedChanges_DetectsBoth()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Existing" },
            new TestEntity { Id = 0, Name = "New" }
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Existing" },
            new TestEntity { Id = 2, Name = "ToDelete" }
        };

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Single(diff.ToInsert);
        Assert.Single(diff.ToDelete);
        Assert.Equal(0, diff.ToInsert[0].Id);
        Assert.Equal(2, diff.ToDelete[0].Id);
    }

    #endregion

    #region No Changes

    [Fact]
    public void ComputeDiff_WhenIdentical_ReturnsNoDiff()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item1" },
            new TestEntity { Id = 2, Name = "Item2" }
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item1" },
            new TestEntity { Id = 2, Name = "Item2" }
        };

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Empty(diff.ToInsert);
        Assert.Empty(diff.ToDelete);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void ComputeDiff_WhenOnlyExistingItems_ReturnsNoDiff()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Existing1" },
            new TestEntity { Id = 2, Name = "Existing2" },
            new TestEntity { Id = 3, Name = "Existing3" }
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Existing1" },
            new TestEntity { Id = 2, Name = "Existing2" },
            new TestEntity { Id = 3, Name = "Existing3" }
        };

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Empty(diff.ToInsert);
        Assert.Empty(diff.ToDelete);
        Assert.False(diff.HasChanges);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ComputeDiff_WithOnlyId0Items_DetectsAllAsInserts()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New1" },
            new TestEntity { Id = 0, Name = "New2" },
            new TestEntity { Id = 0, Name = "New3" }
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Existing" }
        };

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Equal(3, diff.ToInsert.Count);
        Assert.Single(diff.ToDelete);
    }

    [Fact]
    public void ComputeDiff_PreservesOriginalItems()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New" }
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Delete" }
        };

        // Act
        var diff = _diffService.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Same(dataStoreItems[0], diff.ToInsert[0]);
        Assert.Same(databaseItems[0], diff.ToDelete[0]);
    }

    #endregion
}
