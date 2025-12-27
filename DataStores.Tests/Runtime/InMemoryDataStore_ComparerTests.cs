using DataStores.Runtime;
using TestHelper.DataStores.Comparers;
using TestHelper.DataStores.Models;
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
        var store = new InMemoryDataStore<TestDto>(comparer: null);
        
        var item1 = new TestDto("A", 25);
        var item2 = new TestDto("A", 25);
        
        store.Add(item1);
        
        // Assert - Default comparer uses reference equality
        Assert.False(store.Contains(item2)); // Different references
    }

    [Fact]
    public void Remove_Should_UseCustomComparer()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, Guid>(x => x.Id);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        var item = new TestDto("Original", 25);
        store.Add(item);
        
        // Act - Remove with different Name but same Id
        var itemToRemove = new TestDto("Different", 30) { Id = item.Id };
        var removed = store.Remove(itemToRemove);
        
        // Assert - Should find by Id only
        Assert.True(removed);
        Assert.Empty(store.Items);
    }

    [Fact]
    public void Contains_Should_UseCustomComparer()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        store.Add(new TestDto("Test", 25));
        
        // Act - Contains with different Age but same Name
        var contains = store.Contains(new TestDto("Test", 99));
        
        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void Add_WithDuplicatesByComparer_Should_ThrowException()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, Guid>(x => x.Id);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        var sharedId = Guid.NewGuid();
        
        // Act - Both have same Id according to comparer
        store.Add(new TestDto("A", 20) { Id = sharedId });
        
        // Assert - NEW BEHAVIOR: Duplicate prevention
        Assert.Throws<InvalidOperationException>(() => 
            store.Add(new TestDto("B", 30) { Id = sharedId }));
        Assert.Single(store.Items); // Only first one added
    }

    [Fact]
    public void Remove_Should_RemoveFirstMatchByComparer()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        // Use AddOrReplace to avoid duplicate exceptions
        store.Add(new TestDto("Test", 20));
        store.AddOrReplace(new TestDto("Test2", 30)); // Different name
        store.AddOrReplace(new TestDto("Test3", 40)); // Different name
        
        // Act - Remove by Name="Test"
        var removed = store.Remove(new TestDto("Test", 99));
        
        // Assert - Match removed
        Assert.True(removed);
        Assert.Equal(2, store.Items.Count);
    }

    [Fact]
    public void Comparer_GetHashCode_Should_NotAffectInternalStorage()
    {
        // Arrange - Comparer with bad GetHashCode (always returns 0)
        var comparer = new BadHashCodeComparer();
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        // Act - Add multiple items
        store.Add(new TestDto("A", 20));
        store.Add(new TestDto("B", 30));
        store.Add(new TestDto("C", 40));
        
        // Assert - Should still work (uses List<T>, not HashSet)
        Assert.Equal(3, store.Items.Count);
    }

    [Fact]
    public void Comparer_Should_HandleNullGracefully()
    {
        // Arrange - Using notnull attribute to satisfy constraint
        var comparer = new NullSafeComparer();
#pragma warning disable CS8634
        var store = new InMemoryDataStore<TestDto?>(comparer);
#pragma warning restore CS8634
        
        store.Add(null);
        store.Add(new TestDto("A", 20));
        
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
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        store.Add(new TestDto("A", 20));
        
        // Act & Assert - Comparer throws during Contains/Remove
        Assert.Throws<InvalidOperationException>(() => 
            store.Contains(new TestDto("A", 20)));
        
        Assert.Throws<InvalidOperationException>(() => 
            store.Remove(new TestDto("A", 20)));
    }

    [Fact]
    public void CaseInsensitiveComparer_Should_FindMatches()
    {
        // Arrange
        var comparer = new CaseInsensitiveNameComparer();
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        store.Add(new TestDto("Test", 25));
        
        // Act
        var containsLower = store.Contains(new TestDto("test", 99));
        var containsUpper = store.Contains(new TestDto("TEST", 99));
        
        // Assert
        Assert.True(containsLower);
        Assert.True(containsUpper);
    }

    [Fact]
    public void Comparer_Should_BeConsistentAcrossOperations()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, Guid>(x => x.Id);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        var item = new TestDto("Original", 25);
        store.Add(item);
        
        // Act & Assert - All operations use same comparer
        Assert.True(store.Contains(new TestDto("Different1", 30) { Id = item.Id }));
        Assert.True(store.Remove(new TestDto("Different2", 40) { Id = item.Id }));
        
        store.Add(new TestDto("A", 20));
        Assert.False(store.Contains(new TestDto("Anything", 50) { Id = item.Id }));
    }

    // Edge-Case Comparers (bleiben lokal - testen spezifische Comparer-Verhaltensweisen)

    private class BadHashCodeComparer : IEqualityComparer<TestDto>
    {
        public bool Equals(TestDto? x, TestDto? y) => x?.Id == y?.Id;
        public int GetHashCode(TestDto obj) => 0; // Bad hash!
    }

    private class NullSafeComparer : IEqualityComparer<TestDto?>
    {
        public bool Equals(TestDto? x, TestDto? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(TestDto? obj) => obj?.Id.GetHashCode() ?? 0;
    }

    private class ThrowingComparer : IEqualityComparer<TestDto>
    {
        public bool Equals(TestDto? x, TestDto? y) => 
            throw new InvalidOperationException("Comparer error");
        
        public int GetHashCode(TestDto obj) => obj.Id.GetHashCode();
    }

    private class CaseInsensitiveNameComparer : IEqualityComparer<TestDto>
    {
        public bool Equals(TestDto? x, TestDto? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(TestDto obj) => 
            obj.Name?.ToLowerInvariant().GetHashCode() ?? 0;
    }
}
