using System;
using DataStores.Relations;
using DataStores.Runtime;
using DataStores.Tests.TestEntities;
using Xunit;

namespace DataStores.Tests.Unit.Relations;

/// <summary>
/// Unit tests for OneToOneRelationView wrapper.
/// </summary>
[Trait("Category", "Unit")]
public class OneToOneRelationView_Tests
{
    #region Basic API Tests

    [Fact]
    public void Parent_ReturnsCorrectParent()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Assert
        Assert.Same(group, oneToOne.Parent);
    }

    [Fact]
    public void HasChild_WhenNoChild_ReturnsFalse()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Assert
        Assert.False(oneToOne.HasChild);
    }

    [Fact]
    public void HasChild_WhenOneChild_ReturnsTrue()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Assert
        Assert.True(oneToOne.HasChild);
    }

    [Fact]
    public void ChildOrNull_WhenNoChild_ReturnsNull()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Assert
        Assert.Null(oneToOne.ChildOrNull);
    }

    [Fact]
    public void ChildOrNull_WhenOneChild_ReturnsChild()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Assert
        Assert.Same(member, oneToOne.ChildOrNull);
    }

    #endregion

    #region Policy Tests - ThrowIfMultiple

    [Fact]
    public void ChildOrNull_WithMultipleChildren_ThrowIfMultiple_Throws()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(
            relationView, 
            MultipleChildrenPolicy.ThrowIfMultiple);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => oneToOne.ChildOrNull);
        Assert.Contains("Expected at most one child", ex.Message);
        Assert.Contains("found 2", ex.Message);
    }

    [Fact]
    public void HasChild_WithMultipleChildren_ThrowIfMultiple_Throws()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(
            relationView, 
            MultipleChildrenPolicy.ThrowIfMultiple);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => oneToOne.HasChild);
    }

    #endregion

    #region Policy Tests - TakeFirst

    [Fact]
    public void ChildOrNull_WithMultipleChildren_TakeFirst_ReturnsFirst()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(
            relationView, 
            MultipleChildrenPolicy.TakeFirst);

        // Assert
        Assert.Same(member1, oneToOne.ChildOrNull);
    }

    [Fact]
    public void HasChild_WithMultipleChildren_TakeFirst_ReturnsTrue()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(
            relationView, 
            MultipleChildrenPolicy.TakeFirst);

        // Assert
        Assert.True(oneToOne.HasChild);
    }

    #endregion

    #region TryGetChild Tests

    [Fact]
    public void TryGetChild_WhenNoChild_ReturnsFalse()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Act
        var result = oneToOne.TryGetChild(out var child);

        // Assert
        Assert.False(result);
        Assert.Null(child);
    }

    [Fact]
    public void TryGetChild_WhenOneChild_ReturnsTrueAndChild()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Act
        var result = oneToOne.TryGetChild(out var child);

        // Assert
        Assert.True(result);
        Assert.Same(member, child);
    }

    [Fact]
    public void TryGetChild_WithMultipleChildren_ThrowIfMultiple_ReturnsFalse()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(
            relationView, 
            MultipleChildrenPolicy.ThrowIfMultiple);

        // Act
        var result = oneToOne.TryGetChild(out var child);

        // Assert
        Assert.False(result);
        Assert.Null(child);
    }

    [Fact]
    public void TryGetChild_WithMultipleChildren_TakeFirst_ReturnsTrueAndFirst()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(
            relationView, 
            MultipleChildrenPolicy.TakeFirst);

        // Act
        var result = oneToOne.TryGetChild(out var child);

        // Assert
        Assert.True(result);
        Assert.Same(member1, child);
    }

    #endregion

    #region Dynamic Update Tests

    [Fact]
    public void ChildOrNull_UpdatesAutomatically_WhenChildAdded()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Verify initially no child
        Assert.Null(oneToOne.ChildOrNull);
        Assert.False(oneToOne.HasChild);

        // Act - Add child
        var member = new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Member1" };
        childStore.Add(member);

        // Assert - Should update automatically
        Assert.Same(member, oneToOne.ChildOrNull);
        Assert.True(oneToOne.HasChild);
    }

    [Fact]
    public void ChildOrNull_UpdatesAutomatically_WhenChildRemoved()
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
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Verify initially has child
        Assert.Same(member, oneToOne.ChildOrNull);

        // Act - Remove child
        childStore.Remove(member);

        // Assert - Should update automatically
        Assert.Null(oneToOne.ChildOrNull);
        Assert.False(oneToOne.HasChild);
    }

    [Fact]
    public void ChildOrNull_UpdatesAutomatically_WhenChildKeyChanges()
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
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relationView = service.GetOneToManyRelation(group);
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Verify initially has child
        Assert.Same(member, oneToOne.ChildOrNull);

        // Act - Change child's GroupId
        member.GroupId = otherGroupId;

        // Assert - Should update automatically (no longer matches)
        Assert.Null(oneToOne.ChildOrNull);
        Assert.False(oneToOne.HasChild);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRelationView_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new OneToOneRelationView<Group, Member>(null!));
    }

    [Fact]
    public void Constructor_DefaultPolicy_IsThrowIfMultiple()
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
        var relationView = service.GetOneToManyRelation(group);
        
        // Act - Use default constructor (no policy specified)
        var oneToOne = new OneToOneRelationView<Group, Member>(relationView);

        // Assert - Should use ThrowIfMultiple by default
        Assert.Throws<InvalidOperationException>(() => oneToOne.ChildOrNull);
    }

    #endregion
}
