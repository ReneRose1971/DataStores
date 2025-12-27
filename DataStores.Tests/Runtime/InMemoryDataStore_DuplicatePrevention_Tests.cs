using DataStores.Abstractions;
using DataStores.Runtime;
using TestHelper.DataStores.Comparers;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Runtime;

/// <summary>
/// Detailed tests for duplicate prevention in InMemoryDataStore.
/// </summary>
[Trait("Category", "Unit")]
public class InMemoryDataStore_DuplicatePrevention_Tests
{
    #region Add() Duplicate Prevention

    [Fact]
    public void Add_WithDuplicate_SameReference_Should_ThrowException()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);
        store.Add(item);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => store.Add(item));
        Assert.Contains("Duplicate", ex.Message);
        Assert.Contains("Use AddOrReplace()", ex.Message);
    }

    [Fact]
    public void Add_WithDuplicate_DifferentReference_DefaultComparer_Should_AllowBoth()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>(); // Default comparer = reference equality
        var item1 = new TestDto("Test", 25);
        var item2 = new TestDto("Test", 25); // Different reference

        // Act
        store.Add(item1);
        store.Add(item2); // Should succeed - different references

        // Assert
        Assert.Equal(2, store.Items.Count);
    }

    [Fact]
    public void Add_WithDuplicate_CustomComparer_Should_ThrowException()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        store.Add(new TestDto("Test", 25));

        // Act & Assert - Same Name = duplicate
        var ex = Assert.Throws<InvalidOperationException>(() => 
            store.Add(new TestDto("Test", 30)));
        
        Assert.Contains("KeySelectorEqualityComparer", ex.Message);
    }

    [Fact]
    public void Add_WithEntityBase_ShouldUse_IdComparer()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestEntity, int>(x => x.Id);
        var store = new InMemoryDataStore<TestEntity>(comparer);
        
        var entity1 = new TestEntity { Id = 1, Name = "Original" };
        store.Add(entity1);

        // Act & Assert - Same Id = duplicate
        var entity2 = new TestEntity { Id = 1, Name = "Different" };
        Assert.Throws<InvalidOperationException>(() => store.Add(entity2));
    }

    #endregion

    #region AddRange() Duplicate Prevention

    [Fact]
    public void AddRange_WithDuplicates_InBatch_Should_ThrowException()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, Guid>(x => x.Id);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        var sharedId = Guid.NewGuid();
        var items = new[]
        {
            new TestDto("A", 20) { Id = sharedId },
            new TestDto("B", 30) { Id = sharedId }, // Duplicate in batch
            new TestDto("C", 40) { Id = sharedId }  // Another duplicate
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => store.AddRange(items));
        Assert.Contains("Duplicate", ex.Message);
        Assert.Empty(store.Items); // Transaction rolled back
    }

    [Fact]
    public void AddRange_WithDuplicates_AgainstExisting_Should_ThrowException()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        store.Add(new TestDto("Existing", 25));

        var items = new[]
        {
            new TestDto("New1", 30),
            new TestDto("Existing", 35), // Duplicate with existing
            new TestDto("New2", 40)
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => store.AddRange(items));
        Assert.Contains("existing identities", ex.Message);
        Assert.Single(store.Items); // Only original item remains
    }

    [Fact]
    public void AddRange_WithoutDuplicates_Should_AddAll()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>();
        var items = new[]
        {
            new TestDto("A", 20),
            new TestDto("B", 30),
            new TestDto("C", 40)
        };

        // Act
        store.AddRange(items);

        // Assert
        Assert.Equal(3, store.Items.Count);
    }

    [Fact]
    public void AddRange_EmptyList_Should_NotThrow()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>();

        // Act & Assert
        store.AddRange(Array.Empty<TestDto>());
        Assert.Empty(store.Items);
    }

    #endregion

    #region AddOrReplace() Behavior

    [Fact]
    public void AddOrReplace_WithNewItem_Should_Add()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("New", 25);

        // Act
        store.AddOrReplace(item);

        // Assert
        Assert.Single(store.Items);
        Assert.Same(item, store.Items[0]);
    }

    [Fact]
    public void AddOrReplace_WithExistingItem_Should_Replace()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        var original = new TestDto("Test", 25);
        store.Add(original);

        // Act
        var updated = new TestDto("Test", 99); // Same Name, different Age
        store.AddOrReplace(updated);

        // Assert
        Assert.Single(store.Items);
        Assert.Equal(99, store.Items[0].Age); // Updated
        Assert.NotSame(original, store.Items[0]); // Replaced
    }

    [Fact]
    public void AddOrReplace_Should_RaiseCorrectEvent_OnAdd()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>();
        DataStoreChangeType? capturedType = null;
        store.Changed += (s, e) => capturedType = e.ChangeType;

        // Act
        store.AddOrReplace(new TestDto("New", 25));

        // Assert
        Assert.Equal(DataStoreChangeType.Add, capturedType);
    }

    [Fact]
    public void AddOrReplace_Should_RaiseCorrectEvent_OnUpdate()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var store = new InMemoryDataStore<TestDto>(comparer);
        store.Add(new TestDto("Test", 25));

        DataStoreChangeType? capturedType = null;
        store.Changed += (s, e) => capturedType = e.ChangeType;

        // Act
        store.AddOrReplace(new TestDto("Test", 99)); // Same Name = update

        // Assert
        Assert.Equal(DataStoreChangeType.Update, capturedType);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Add_AfterClear_Should_AllowPreviouslyExisting()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);
        store.Add(item);
        store.Clear();

        // Act - Should succeed after clear
        store.Add(item);

        // Assert
        Assert.Single(store.Items);
    }

    [Fact]
    public void Add_AfterRemove_Should_AllowReAdding()
    {
        // Arrange
        var store = new InMemoryDataStore<TestDto>();
        var item = new TestDto("Test", 25);
        store.Add(item);
        store.Remove(item);

        // Act - Should succeed after remove
        store.Add(item);

        // Assert
        Assert.Single(store.Items);
    }

    [Fact]
    public void Comparer_UsedConsistently_InAllOperations()
    {
        // Arrange
        var comparer = new KeySelectorEqualityComparer<TestDto, string>(x => x.Name);
        var store = new InMemoryDataStore<TestDto>(comparer);
        
        var item1 = new TestDto("Test", 25);
        store.Add(item1);

        // Act & Assert - Contains uses same comparer
        Assert.True(store.Contains(new TestDto("Test", 99)));

        // Remove uses same comparer
        Assert.True(store.Remove(new TestDto("Test", 100)));
        Assert.Empty(store.Items);

        // Add after remove should work
        store.Add(new TestDto("Test", 50));
        Assert.Single(store.Items);
    }

    #endregion
}
