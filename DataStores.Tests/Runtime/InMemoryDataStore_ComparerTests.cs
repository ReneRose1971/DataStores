using DataStores.Runtime;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Tests for InMemoryDataStore with custom IEqualityComparer.
/// </summary>
public class InMemoryDataStore_ComparerTests
{
    [Fact]
    public void Constructor_WithNullComparer_Should_UseDefault()
    {
        // Arrange & Act
        var store = new InMemoryDataStore<TestItem>(comparer: null);
        
        var item1 = new TestItem { Id = 1, Name = "A" };
        var item2 = new TestItem { Id = 1, Name = "A" };
        
        store.Add(item1);
        
        // Assert - Default comparer uses reference equality
        Assert.False(store.Contains(item2)); // Different references
    }

    [Fact]
    public void Remove_Should_UseCustomComparer()
    {
        // Arrange
        var comparer = new IdOnlyComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        store.Add(new TestItem { Id = 1, Name = "Original" });
        
        // Act - Remove with different Name but same Id
        var removed = store.Remove(new TestItem { Id = 1, Name = "Different" });
        
        // Assert - Should find by Id only
        Assert.True(removed);
        Assert.Empty(store.Items);
    }

    [Fact]
    public void Contains_Should_UseCustomComparer()
    {
        // Arrange
        var comparer = new NameOnlyComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        store.Add(new TestItem { Id = 1, Name = "Test" });
        
        // Act - Contains with different Id but same Name
        var contains = store.Contains(new TestItem { Id = 999, Name = "Test" });
        
        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void Add_WithDuplicatesByComparer_Should_AddBoth()
    {
        // Arrange
        var comparer = new IdOnlyComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        // Act - Both have Id=1 according to comparer
        store.Add(new TestItem { Id = 1, Name = "A" });
        store.Add(new TestItem { Id = 1, Name = "B" }); // Comparer sees as duplicate
        
        // Assert - InMemoryDataStore adds both (doesn't enforce uniqueness)
        Assert.Equal(2, store.Items.Count);
    }

    [Fact]
    public void Remove_Should_RemoveFirstMatchByComparer()
    {
        // Arrange
        var comparer = new IdOnlyComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        store.Add(new TestItem { Id = 1, Name = "A" });
        store.Add(new TestItem { Id = 1, Name = "B" });
        store.Add(new TestItem { Id = 1, Name = "C" });
        
        // Act - Remove by Id=1
        var removed = store.Remove(new TestItem { Id = 1, Name = "Anything" });
        
        // Assert - Only first match removed
        Assert.True(removed);
        Assert.Equal(2, store.Items.Count);
    }

    [Fact]
    public void Comparer_GetHashCode_Should_NotAffectInternalStorage()
    {
        // Arrange - Comparer with bad GetHashCode (always returns 0)
        var comparer = new BadHashCodeComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        // Act - Add multiple items
        store.Add(new TestItem { Id = 1, Name = "A" });
        store.Add(new TestItem { Id = 2, Name = "B" });
        store.Add(new TestItem { Id = 3, Name = "C" });
        
        // Assert - Should still work (uses List<T>, not HashSet)
        Assert.Equal(3, store.Items.Count);
    }

    [Fact]
    public void Comparer_Should_HandleNullGracefully()
    {
        // Arrange - Using notnull attribute to satisfy constraint
        var comparer = new NullSafeComparer();
#pragma warning disable CS8634 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'class' constraint.
        var store = new InMemoryDataStore<TestItem?>(comparer);
#pragma warning restore CS8634
        
        store.Add(null);
        store.Add(new TestItem { Id = 1, Name = "A" });
        
        // Act
        var containsNull = store.Contains(null);
        var removed = store.Remove(null);
        
        // Assert
        Assert.True(containsNull);
        Assert.True(removed);
        Assert.Single(store.Items); // Only non-null item remains
    }

    [Fact]
    public void Comparer_Throws_Should_PropagateException()
    {
        // Arrange
        var comparer = new ThrowingComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        store.Add(new TestItem { Id = 1, Name = "A" });
        
        // Act & Assert - Comparer throws during Contains/Remove
        Assert.Throws<InvalidOperationException>(() => 
            store.Contains(new TestItem { Id = 1 }));
        
        Assert.Throws<InvalidOperationException>(() => 
            store.Remove(new TestItem { Id = 1 }));
    }

    [Fact]
    public void CaseInsensitiveComparer_Should_FindMatches()
    {
        // Arrange
        var comparer = new CaseInsensitiveNameComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        store.Add(new TestItem { Id = 1, Name = "Test" });
        
        // Act
        var containsLower = store.Contains(new TestItem { Id = 99, Name = "test" });
        var containsUpper = store.Contains(new TestItem { Id = 99, Name = "TEST" });
        
        // Assert
        Assert.True(containsLower);
        Assert.True(containsUpper);
    }

    [Fact]
    public void Comparer_Should_BeConsistentAcrossOperations()
    {
        // Arrange
        var comparer = new IdOnlyComparer();
        var store = new InMemoryDataStore<TestItem>(comparer);
        
        var item = new TestItem { Id = 1, Name = "Original" };
        store.Add(item);
        
        // Act & Assert - All operations use same comparer
        Assert.True(store.Contains(new TestItem { Id = 1, Name = "Different1" }));
        Assert.True(store.Remove(new TestItem { Id = 1, Name = "Different2" }));
        
        store.Add(new TestItem { Id = 2, Name = "A" });
        Assert.False(store.Contains(new TestItem { Id = 1, Name = "Anything" }));
    }

    // Helper Classes

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
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

    private class NameOnlyComparer : IEqualityComparer<TestItem>
    {
        public bool Equals(TestItem? x, TestItem? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return string.Equals(x.Name, y.Name, StringComparison.Ordinal);
        }

        public int GetHashCode(TestItem obj) => obj.Name?.GetHashCode() ?? 0;
    }

    private class BadHashCodeComparer : IEqualityComparer<TestItem>
    {
        public bool Equals(TestItem? x, TestItem? y) => x?.Id == y?.Id;
        public int GetHashCode(TestItem obj) => 0; // Bad hash!
    }

    private class NullSafeComparer : IEqualityComparer<TestItem?>
    {
        public bool Equals(TestItem? x, TestItem? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(TestItem? obj) => obj?.Id.GetHashCode() ?? 0;
    }

    private class ThrowingComparer : IEqualityComparer<TestItem>
    {
        public bool Equals(TestItem? x, TestItem? y) => 
            throw new InvalidOperationException("Comparer error");
        
        public int GetHashCode(TestItem obj) => obj.Id.GetHashCode();
    }

    private class CaseInsensitiveNameComparer : IEqualityComparer<TestItem>
    {
        public bool Equals(TestItem? x, TestItem? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(TestItem obj) => 
            obj.Name?.ToLowerInvariant().GetHashCode() ?? 0;
    }
}
