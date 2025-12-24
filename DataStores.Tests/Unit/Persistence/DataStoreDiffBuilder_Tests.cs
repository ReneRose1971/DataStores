using DataStores.Abstractions;
using DataStores.Persistence;
using Xunit;

namespace DataStores.Tests.Unit.Persistence;

/// <summary>
/// Unit-Tests für DataStoreDiffBuilder.
/// Testet die Diff-Berechnung zwischen DataStore und Datenbank.
/// </summary>
[Trait("Category", "Unit")]
public class DataStoreDiffBuilder_Tests
{
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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

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
            DataStoreDiffBuilder.ComputeDiff<TestEntity>(null!, databaseItems));
    }

    [Fact]
    public void ComputeDiff_WithNullDatabaseItems_ThrowsArgumentNullException()
    {
        // Arrange
        var dataStoreItems = Array.Empty<TestEntity>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DataStoreDiffBuilder.ComputeDiff(dataStoreItems, null!));
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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Equal(3, diff.ToInsert.Count);
        Assert.Empty(diff.ToDelete);
        Assert.True(diff.HasChanges);
        Assert.All(diff.ToInsert, item => Assert.Equal(0, item.Id));
    }

    [Fact]
    public void ComputeDiff_WithMissingIds_DetectsInserts()
    {
        // Arrange - Items mit Id > 0 die nicht in DB sind (Missing IDs Policy)
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 99, Name = "Missing1" },
            new TestEntity { Id = 100, Name = "Missing2" }
        };

        var databaseItems = Array.Empty<TestEntity>();

        // Act
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Equal(2, diff.ToInsert.Count);
        Assert.Empty(diff.ToDelete);
        Assert.Contains(diff.ToInsert, item => item.Id == 99);
        Assert.Contains(diff.ToInsert, item => item.Id == 100);
    }

    [Fact]
    public void ComputeDiff_WithMixedNewAndMissingIds_DetectsBothAsInserts()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New" },
            new TestEntity { Id = 42, Name = "MissingId" }
        };

        var databaseItems = Array.Empty<TestEntity>();

        // Act
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Equal(2, diff.ToInsert.Count);
        Assert.Empty(diff.ToDelete);
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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

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
            new TestEntity { Id = 1, Name = "Existing" },  // In DB, bleibt
            new TestEntity { Id = 0, Name = "New" }        // Neu, INSERT
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Existing" },  // Bleibt
            new TestEntity { Id = 2, Name = "ToDelete" }   // DELETE
        };

        // Act
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Single(diff.ToInsert);
        Assert.Single(diff.ToDelete);
        Assert.Equal(0, diff.ToInsert[0].Id);
        Assert.Equal(2, diff.ToDelete[0].Id);
    }

    [Fact]
    public void ComputeDiff_WithAllOperations_DetectsCorrectly()
    {
        // Arrange
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Keep1" },      // Bleibt
            new TestEntity { Id = 2, Name = "Keep2" },      // Bleibt
            new TestEntity { Id = 0, Name = "New1" },       // INSERT (Id = 0)
            new TestEntity { Id = 0, Name = "New2" },       // INSERT (Id = 0)
            new TestEntity { Id = 99, Name = "MissingId" }  // INSERT (Missing ID)
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Keep1" },      // Bleibt
            new TestEntity { Id = 2, Name = "Keep2" },      // Bleibt
            new TestEntity { Id = 3, Name = "Delete1" },    // DELETE
            new TestEntity { Id = 4, Name = "Delete2" }     // DELETE
        };

        // Act
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Equal(3, diff.ToInsert.Count);  // 2x Id=0 + 1x Id=99
        Assert.Equal(2, diff.ToDelete.Count);  // Id=3, Id=4
        Assert.True(diff.HasChanges);
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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

        // Assert
        Assert.Equal(3, diff.ToInsert.Count);
        Assert.Single(diff.ToDelete);  // Id=1 wird gelöscht
    }

    [Fact]
    public void ComputeDiff_WithDuplicateIds_ThrowsException()
    {
        // Arrange - DB hat doppelte IDs (sollte nicht vorkommen)
        var dataStoreItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item" }
        };

        var databaseItems = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Item1" },
            new TestEntity { Id = 1, Name = "Item2" }  // Duplikat - ungültiger Zustand
        };

        // Act & Assert - Sollte Exception werfen bei ungültigen Daten
        Assert.Throws<ArgumentException>(() =>
            DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems));
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
        var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);

        // Assert - Prüfe dass die Referenzen erhalten bleiben
        Assert.Same(dataStoreItems[0], diff.ToInsert[0]);
        Assert.Same(databaseItems[0], diff.ToDelete[0]);
    }

    #endregion
}
