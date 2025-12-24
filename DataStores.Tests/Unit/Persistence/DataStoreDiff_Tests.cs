using DataStores.Abstractions;
using DataStores.Persistence;
using Xunit;

namespace DataStores.Tests.Unit.Persistence;

/// <summary>
/// Unit-Tests für DataStoreDiff Record.
/// </summary>
[Trait("Category", "Unit")]
public class DataStoreDiff_Tests
{
    private class TestEntity : EntityBase
    {
        public string Name { get; set; } = "";

        public override string ToString() => $"TestEntity #{Id}: {Name}";
        public override bool Equals(object? obj) => obj is TestEntity other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }

    [Fact]
    public void Constructor_WithEmptyLists_CreatesValidDiff()
    {
        // Arrange
        var toInsert = Array.Empty<TestEntity>();
        var toDelete = Array.Empty<TestEntity>();

        // Act
        var diff = new DataStoreDiff<TestEntity>(toInsert, toDelete);

        // Assert
        Assert.NotNull(diff);
        Assert.Empty(diff.ToInsert);
        Assert.Empty(diff.ToDelete);
    }

    [Fact]
    public void Constructor_WithItems_StoresCorrectly()
    {
        // Arrange
        var toInsert = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New1" },
            new TestEntity { Id = 0, Name = "New2" }
        }.AsReadOnly();

        var toDelete = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Delete1" }
        }.AsReadOnly();

        // Act
        var diff = new DataStoreDiff<TestEntity>(toInsert, toDelete);

        // Assert
        Assert.Equal(2, diff.ToInsert.Count);
        Assert.Single(diff.ToDelete);
        Assert.Equal("New1", diff.ToInsert[0].Name);
        Assert.Equal("Delete1", diff.ToDelete[0].Name);
    }

    [Fact]
    public void HasChanges_WhenEmpty_ReturnsFalse()
    {
        // Arrange
        var diff = new DataStoreDiff<TestEntity>(
            Array.Empty<TestEntity>(),
            Array.Empty<TestEntity>());

        // Act & Assert
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void HasChanges_WithInserts_ReturnsTrue()
    {
        // Arrange
        var toInsert = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New" }
        }.AsReadOnly();

        var diff = new DataStoreDiff<TestEntity>(toInsert, Array.Empty<TestEntity>());

        // Act & Assert
        Assert.True(diff.HasChanges);
    }

    [Fact]
    public void HasChanges_WithDeletes_ReturnsTrue()
    {
        // Arrange
        var toDelete = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Delete" }
        }.AsReadOnly();

        var diff = new DataStoreDiff<TestEntity>(Array.Empty<TestEntity>(), toDelete);

        // Act & Assert
        Assert.True(diff.HasChanges);
    }

    [Fact]
    public void HasChanges_WithBoth_ReturnsTrue()
    {
        // Arrange
        var toInsert = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New" }
        }.AsReadOnly();

        var toDelete = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Delete" }
        }.AsReadOnly();

        var diff = new DataStoreDiff<TestEntity>(toInsert, toDelete);

        // Act & Assert
        Assert.True(diff.HasChanges);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var toInsert = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New1" },
            new TestEntity { Id = 0, Name = "New2" }
        }.AsReadOnly();

        var toDelete = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Delete1" }
        }.AsReadOnly();

        var diff = new DataStoreDiff<TestEntity>(toInsert, toDelete);

        // Act
        var result = diff.ToString();

        // Assert
        Assert.Contains("2 to insert", result);
        Assert.Contains("1 to delete", result);
    }

    [Fact]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var toInsert = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New" }
        }.AsReadOnly();

        var toDelete = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Delete" }
        }.AsReadOnly();

        var diff1 = new DataStoreDiff<TestEntity>(toInsert, toDelete);
        var diff2 = new DataStoreDiff<TestEntity>(toInsert, toDelete);

        // Act & Assert
        Assert.Equal(diff1, diff2);
    }

    [Fact]
    public void ToInsert_IsReadOnly()
    {
        // Arrange
        var toInsert = new List<TestEntity>
        {
            new TestEntity { Id = 0, Name = "New" }
        }.AsReadOnly();

        var diff = new DataStoreDiff<TestEntity>(toInsert, Array.Empty<TestEntity>());

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<TestEntity>>(diff.ToInsert);
    }

    [Fact]
    public void ToDelete_IsReadOnly()
    {
        // Arrange
        var toDelete = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Delete" }
        }.AsReadOnly();

        var diff = new DataStoreDiff<TestEntity>(Array.Empty<TestEntity>(), toDelete);

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<TestEntity>>(diff.ToDelete);
    }
}
