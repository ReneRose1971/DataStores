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
/// Unit tests for ParentChildRelationService with dynamic tracking.
/// Tests follow TDD approach: Arrange/Act/Assert pattern.
/// </summary>
[Trait("Category", "Unit")]
public class ParentChildRelationService_Tests
{
    #region Test 1: Childs_Is_ReadOnlyObservableCollection

    [Fact]
    public void Childs_Is_ReadOnlyObservableCollection()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = Guid.NewGuid(), Name = "Group1" };

        // Act
        var relation = service.GetRelation(group);

        // Assert
        Assert.NotNull(relation.Childs);
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyObservableCollection<Member>>(relation.Childs);
        
        // Verify it's truly read-only (no Add/Remove/Clear methods)
        var childsType = relation.Childs.GetType();
        Assert.Null(childsType.GetMethod("Add"));
        Assert.Null(childsType.GetMethod("Remove"));
        Assert.Null(childsType.GetMethod("Clear"));
    }

    #endregion

    #region Test 2: AddChild_To_GlobalChildStore_AddsToChilds_WhenKeyMatches

    [Fact]
    public void AddChild_To_GlobalChildStore_AddsToChilds_WhenKeyMatches()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Act
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);

        // Assert
        Assert.Single(relation.Childs);
        Assert.Contains(member, relation.Childs);
    }

    #endregion

    #region Test 3: RemoveChild_From_GlobalChildStore_RemovesFromChilds

    [Fact]
    public void RemoveChild_From_GlobalChildStore_RemovesFromChilds()
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
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Verify initial state
        Assert.Single(relation.Childs);

        // Act
        childStore.Remove(member);

        // Assert
        Assert.Empty(relation.Childs);
    }

    #endregion

    #region Test 4: ClearChildStore_ClearsAllChilds

    [Fact]
    public void ClearChildStore_ClearsAllChilds()
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
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member2" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member3" }
        });
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Verify initial state
        Assert.Equal(3, relation.Childs.Count);

        // Act
        childStore.Clear();

        // Assert
        Assert.Empty(relation.Childs);
    }

    #endregion

    #region Test 5: PropertyChanged_ChangesKey_RemovesFromOldAndAddsToNew

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
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group1 = new Group { Id = group1Id, Name = "Group1" };
        var group2 = new Group { Id = group2Id, Name = "Group2" };
        
        var relation1 = service.GetRelation(group1);
        var relation2 = service.GetRelation(group2);

        // Verify initial state
        Assert.Single(relation1.Childs);
        Assert.Empty(relation2.Childs);

        // Act - Change GroupId (triggers PropertyChanged)
        member.GroupId = group2Id;

        // Assert
        Assert.Empty(relation1.Childs);
        Assert.Single(relation2.Childs);
        Assert.Contains(member, relation2.Childs);
    }

    #endregion

    #region Test 6: PropertyChanged_MakesMatchTrue_Adds

    [Fact]
    public void PropertyChanged_MakesMatchTrue_Adds()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        
        var member = new Member { Id = Guid.NewGuid(), GroupId = otherGroupId, Name = "Member1" };
        childStore.Add(member);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Verify initial state - member doesn't match
        Assert.Empty(relation.Childs);

        // Act - Change GroupId to match
        member.GroupId = groupId;

        // Assert
        Assert.Single(relation.Childs);
        Assert.Contains(member, relation.Childs);
    }

    #endregion

    #region Test 7: PropertyChanged_MakesMatchFalse_Removes

    [Fact]
    public void PropertyChanged_MakesMatchFalse_Removes()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var groupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Verify initial state - member matches
        Assert.Single(relation.Childs);

        // Act - Change GroupId to not match
        member.GroupId = otherGroupId;

        // Assert
        Assert.Empty(relation.Childs);
    }

    #endregion

    #region Test 8: Service_DoesNotDuplicateSubscriptions

    [Fact]
    public void Service_DoesNotDuplicateSubscriptions()
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
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Act - Remove and re-add the same member
        childStore.Remove(member);
        childStore.Add(member);

        var changeCount = 0;
        member.PropertyChanged += (s, e) => changeCount++;

        // Change property multiple times
        member.Name = "Changed1";
        member.Name = "Changed2";

        // Assert - Should only fire once per change (not duplicated)
        Assert.Equal(2, changeCount);
    }

    #endregion

    #region Test 9: Dispose_Unsubscribes_NoFurtherUpdates

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
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Verify initial state
        Assert.Single(relation.Childs);

        // Act - Dispose service
        service.Dispose();

        // Add new member after dispose
        var newMember = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member2" };
        childStore.Add(newMember);

        // Change existing member's key after dispose
        var otherGroupId = Guid.NewGuid();
        member.GroupId = otherGroupId;

        // Assert - Childs should remain unchanged (service unsubscribed)
        Assert.Single(relation.Childs);
        Assert.Contains(member, relation.Childs); // Still contains old member
        Assert.DoesNotContain(newMember, relation.Childs); // New member not added
    }

    #endregion

    #region Test 10: BulkAdd_AddsAllChildren

    [Fact]
    public void BulkAdd_AddsAllChildren()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Act - Bulk add
        var members = new[]
        {
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member2" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member3" }
        };
        childStore.AddRange(members);

        // Assert
        Assert.Equal(3, relation.Childs.Count);
        Assert.All(members, m => Assert.Contains(m, relation.Childs));
    }

    #endregion

    #region Test 11: GetRelation_CachesViewsPerParent

    [Fact]
    public void GetRelation_CachesViewsPerParent()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = Guid.NewGuid(), Name = "Group1" };

        // Act
        var relation1 = service.GetRelation(group);
        var relation2 = service.GetRelation(group);

        // Assert - Should return same instance
        Assert.Same(relation1, relation2);
    }

    #endregion

    #region Test 12: GetChildren_ReturnsCorrectCollection

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
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };

        // Act
        var children = service.GetChildren(group);

        // Assert
        Assert.Single(children);
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyObservableCollection<Member>>(children);
    }

    #endregion

    #region Test 13: InitializeExistingChildren_LoadsChildrenOnConstruction

    [Fact]
    public void InitializeExistingChildren_LoadsChildrenOnConstruction()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        
        var groupId = Guid.NewGuid();
        childStore.AddRange(new[]
        {
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member2" }
        });

        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        // Act - Service created AFTER children exist
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        // Assert - Existing children should be loaded
        Assert.Equal(2, relation.Childs.Count);
    }

    #endregion

    #region Test 14: CollectionChanged_FiresWhenChildrenChange

    [Fact]
    public void CollectionChanged_FiresWhenChildrenChange()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetRelation(group);

        var collectionChangedFired = false;
        var collectionChangedAction = NotifyCollectionChangedAction.Reset;
        
        ((INotifyCollectionChanged)relation.Childs).CollectionChanged += (s, e) =>
        {
            collectionChangedFired = true;
            collectionChangedAction = e.Action;
        };

        // Act
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);

        // Assert
        Assert.True(collectionChangedFired);
        Assert.Equal(NotifyCollectionChangedAction.Add, collectionChangedAction);
    }

    #endregion

    #region Test 15: MultipleParents_IndependentChildCollections

    [Fact]
    public void MultipleParents_IndependentChildCollections()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);

        var group1Id = Guid.NewGuid();
        var group2Id = Guid.NewGuid();
        
        childStore.AddRange(new[]
        {
            new Member { Id = Guid.NewGuid(), GroupId = group1Id, Name = "Group1Member1" },
            new Member { Id = Guid.NewGuid(), GroupId = group1Id, Name = "Group1Member2" },
            new Member { Id = Guid.NewGuid(), GroupId = group2Id, Name = "Group2Member1" }
        });
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group1 = new Group { Id = group1Id, Name = "Group1" };
        var group2 = new Group { Id = group2Id, Name = "Group2" };

        // Act
        var relation1 = service.GetRelation(group1);
        var relation2 = service.GetRelation(group2);

        // Assert
        Assert.Equal(2, relation1.Childs.Count);
        Assert.Single(relation2.Childs);
        Assert.All(relation1.Childs, m => Assert.Equal(group1Id, m.GroupId));
        Assert.All(relation2.Childs, m => Assert.Equal(group2Id, m.GroupId));
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
            new ParentChildRelationService<Group, Member, Guid>(null!, childStore, definition));
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
            new ParentChildRelationService<Group, Member, Guid>(parentStore, null!, definition));
    }

    [Fact]
    public void Constructor_WithNullDefinition_Throws()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ParentChildRelationService<Group, Member, Guid>(parentStore, childStore, null!));
    }

    [Fact]
    public void GetRelation_WithNullParent_Throws()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId);
        
        var service = new ParentChildRelationService<Group, Member, Guid>(
            parentStore, childStore, definition);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.GetRelation(null!));
    }

    #endregion
}
