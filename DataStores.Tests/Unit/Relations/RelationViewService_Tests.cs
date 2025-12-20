using System;
using System.Collections.Specialized;
using System.Linq;
using DataStores.Abstractions;
using DataStores.Relations;
using DataStores.Runtime;
using DataStores.Tests.TestEntities;
using Xunit;

namespace DataStores.Tests.Unit.Relations;

/// <summary>
/// Unit tests for RelationViewService with dynamic tracking.
/// Tests follow TDD approach: Arrange/Act/Assert pattern.
/// Tests the new naming: RelationViewService + OneToManyRelationView + IRelationViewService
/// </summary>
[Trait("Category", "Unit")]
public class RelationViewService_Tests
{
    #region Test 1: Children_Is_ReadOnlyObservableCollection

    [Fact]
    public void Children_Is_ReadOnlyObservableCollection()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        IRelationViewService<Group, Member, Guid> service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = Guid.NewGuid(), Name = "Group1" };

        // Act
        var relation = service.GetOneToManyRelation(group);

        // Assert
        Assert.NotNull(relation.Children);
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyObservableCollection<Member>>(relation.Children);
        
        // Verify it's truly read-only (no Add/Remove/Clear methods)
        var childrenType = relation.Children.GetType();
        Assert.Null(childrenType.GetMethod("Add"));
        Assert.Null(childrenType.GetMethod("Remove"));
        Assert.Null(childrenType.GetMethod("Clear"));
    }

    #endregion

    #region Test 2: AddChild_To_GlobalChildStore_AddsToChildren_WhenKeyMatches

    [Fact]
    public void AddChild_To_GlobalChildStore_AddsToChildren_WhenKeyMatches()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetOneToManyRelation(group);

        // Act
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);

        // Assert
        Assert.Single(relation.Children);
        Assert.Contains(member, relation.Children);
    }

    #endregion

    #region Test 3: PropertyChanged_ChangesKey_RemovesFromOldAndAddsToNew

    [Fact]
    public void PropertyChanged_ChangesKey_RemovesFromOldAndAddsToNew()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var group1Id = Guid.NewGuid();
        var group2Id = Guid.NewGuid();
        
        var member = new Member { Id = Guid.NewGuid(), GroupId = group1Id, Name = "Member1" };
        childStore.Add(member);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group1 = new Group { Id = group1Id, Name = "Group1" };
        var group2 = new Group { Id = group2Id, Name = "Group2" };
        
        var relation1 = service.GetOneToManyRelation(group1);
        var relation2 = service.GetOneToManyRelation(group2);

        // Verify initial state
        Assert.Single(relation1.Children);
        Assert.Empty(relation2.Children);

        // Act - Change GroupId (triggers PropertyChanged)
        member.GroupId = group2Id;

        // Assert
        Assert.Empty(relation1.Children);
        Assert.Single(relation2.Children);
        Assert.Contains(member, relation2.Children);
    }

    #endregion

    #region Test 4: Service_DoesNotDuplicateSubscriptions_WithPropertyChangedBinder

    [Fact]
    public void Service_DoesNotDuplicateSubscriptions_WithPropertyChangedBinder()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetOneToManyRelation(group);

        // Act - Remove and re-add the same member (tests idempotent binding)
        childStore.Remove(member);
        childStore.Add(member);

        var changeCount = 0;
        member.PropertyChanged += (s, e) => changeCount++;

        // Change property multiple times
        member.Name = "Changed1";
        member.Name = "Changed2";

        // Assert - Should only fire once per change (PropertyChangedBinder prevents duplicates)
        Assert.Equal(2, changeCount);
    }

    #endregion

    #region Test 5: GetOneToManyRelation_CachesViewsPerParent

    [Fact]
    public void GetOneToManyRelation_CachesViewsPerParent()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = Guid.NewGuid(), Name = "Group1" };

        // Act
        var relation1 = service.GetOneToManyRelation(group);
        var relation2 = service.GetOneToManyRelation(group);

        // Assert - Should return same instance (cached)
        Assert.Same(relation1, relation2);
    }

    #endregion

    #region Test 6: GetChildren_ReturnsCorrectCollection

    [Fact]
    public void GetChildren_ReturnsCorrectCollection()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        childStore.Add(new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" });
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };

        // Act
        var children = service.GetChildren(group);

        // Assert
        Assert.Single(children);
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyObservableCollection<Member>>(children);
    }

    #endregion

    #region Test 7: Dispose_Unsubscribes_NoFurtherUpdates

    [Fact]
    public void Dispose_Unsubscribes_NoFurtherUpdates()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetOneToManyRelation(group);

        // Verify initial state
        Assert.Single(relation.Children);

        // Act - Dispose service (should cleanup PropertyChangedBinder)
        service.Dispose();

        // Add new member after dispose
        var newMember = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member2" };
        childStore.Add(newMember);

        // Change existing member's key after dispose
        var otherGroupId = Guid.NewGuid();
        member.GroupId = otherGroupId;

        // Assert - Children should remain unchanged (service unsubscribed via PropertyChangedBinder)
        Assert.Single(relation.Children);
        Assert.Contains(member, relation.Children);
        Assert.DoesNotContain(newMember, relation.Children);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullParentStore_Throws()
    {
        // Arrange
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelationViewService<Group, Member, Guid>(null!, childStore, definition));
    }

    [Fact]
    public void Constructor_WithNullChildStore_Throws()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelationViewService<Group, Member, Guid>(parentStore, null!, definition));
    }

    [Fact]
    public void Constructor_WithNullDefinition_Throws()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelationViewService<Group, Member, Guid>(parentStore, childStore, null!));
    }

    [Fact]
    public void GetOneToManyRelation_WithNullParent_Throws()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.GetOneToManyRelation(null!));
    }

    #endregion

    #region GetOneToOneRelation Tests

    [Fact]
    public void GetOneToOneRelation_WhenNoChild_ReturnsViewWithNoChild()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        IRelationViewService<Group, Member, Guid> service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = Guid.NewGuid(), Name = "Group1" };

        // Act
        var oneToOne = service.GetOneToOneRelation(group);

        // Assert
        Assert.False(oneToOne.HasChild);
        Assert.Null(oneToOne.ChildOrNull);
    }

    [Fact]
    public void GetOneToOneRelation_WhenOneChild_ReturnsViewWithChild()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };

        // Act
        var oneToOne = service.GetOneToOneRelation(group);

        // Assert
        Assert.True(oneToOne.HasChild);
        Assert.Same(member, oneToOne.ChildOrNull);
    }

    [Fact]
    public void GetOneToOneRelation_WithMultipleChildren_ThrowIfMultiple_Throws()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        childStore.AddRange(new[]
        {
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member2" }
        });
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };

        // Act
        var oneToOne = service.GetOneToOneRelation(group); // Default: ThrowIfMultiple

        // Assert
        Assert.Throws<InvalidOperationException>(() => oneToOne.ChildOrNull);
    }

    [Fact]
    public void GetOneToOneRelation_WithMultipleChildren_TakeFirst_ReturnsFirst()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        var member1 = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        var member2 = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member2" };
        
        childStore.Add(member1);
        childStore.Add(member2);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };

        // Act
        var oneToOne = service.GetOneToOneRelation(group, MultipleChildrenPolicy.TakeFirst);

        // Assert
        Assert.True(oneToOne.HasChild);
        Assert.Same(member1, oneToOne.ChildOrNull);
    }

    [Fact]
    public void GetOneToOneRelation_CalledTwice_ReturnsDifferentInstances()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = Guid.NewGuid(), Name = "Group1" };

        // Act
        var oneToOne1 = service.GetOneToOneRelation(group);
        var oneToOne2 = service.GetOneToOneRelation(group);

        // Assert - NOT cached (different instances)
        Assert.NotSame(oneToOne1, oneToOne2);
    }

    [Fact]
    public void GetOneToOneRelation_UpdatesDynamically_WhenChildAdded()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var oneToOne = service.GetOneToOneRelation(group);

        // Verify initially no child
        Assert.False(oneToOne.HasChild);

        // Act
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);

        // Assert - Should update automatically
        Assert.True(oneToOne.HasChild);
        Assert.Same(member, oneToOne.ChildOrNull);
    }

    [Fact]
    public void GetOneToOneRelation_WithNullParent_Throws()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            service.GetOneToOneRelation(null!));
    }

    #endregion
}
