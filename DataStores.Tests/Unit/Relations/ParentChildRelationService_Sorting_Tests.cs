using System;
using System.Linq;
using DataStores.Relations;
using DataStores.Runtime;
using DataStores.Tests.TestEntities;
using Xunit;

namespace DataStores.Tests.Unit.Relations;

/// <summary>
/// Tests for sorted children functionality in RelationViewService.
/// </summary>
[Trait("Category", "Unit")]
public class ParentChildRelationService_Sorting_Tests
{
    private class MemberNameComparer : IComparer<Member>
    {
        public int Compare(Member? x, Member? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ChildComparer_SortsChildrenOnAdd()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId,
            childComparer: new MemberNameComparer());
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetOneToManyRelation(group);

        // Act - Add members in random order
        childStore.Add(new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Charlie" });
        childStore.Add(new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Alice" });
        childStore.Add(new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Bob" });

        // Assert - Should be sorted by name
        Assert.Equal(3, relation.Children.Count);
        Assert.Equal("Alice", relation.Children[0].Name);
        Assert.Equal("Bob", relation.Children[1].Name);
        Assert.Equal("Charlie", relation.Children[2].Name);
    }

    [Fact]
    public void ChildComparer_SortsChildrenOnBulkAdd()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId,
            childComparer: new MemberNameComparer());
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetOneToManyRelation(group);

        // Act - Bulk add in random order
        childStore.AddRange(new[]
        {
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Zara" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Anna" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Mike" }
        });

        // Assert - Should be sorted by name
        Assert.Equal(3, relation.Children.Count);
        Assert.Equal("Anna", relation.Children[0].Name);
        Assert.Equal("Mike", relation.Children[1].Name);
        Assert.Equal("Zara", relation.Children[2].Name);
    }

    [Fact]
    public void ChildComparer_MaintainsSortOnPropertyChange()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId,
            childComparer: new MemberNameComparer());

        var group1Id = Guid.NewGuid();
        var group2Id = Guid.NewGuid();
        
        childStore.AddRange(new[]
        {
            new Member { Id = Guid.NewGuid(), GroupId = group2Id, Name = "David" },
            new Member { Id = Guid.NewGuid(), GroupId = group1Id, Name = "Alice" },
            new Member { Id = Guid.NewGuid(), GroupId = group1Id, Name = "Charlie" }
        });
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group1 = new Group { Id = group1Id, Name = "Group1" };
        var relation1 = service.GetOneToManyRelation(group1);

        // Verify initial sorted state for group1
        Assert.Equal(2, relation1.Children.Count);
        Assert.Equal("Alice", relation1.Children[0].Name);
        Assert.Equal("Charlie", relation1.Children[1].Name);

        var davidMember = childStore.Items.First(m => m.Name == "David");

        // Act - Move David from group2 to group1 (should insert sorted)
        davidMember.GroupId = group1Id;

        // Assert - Should maintain sorted order
        Assert.Equal(3, relation1.Children.Count);
        Assert.Equal("Alice", relation1.Children[0].Name);
        Assert.Equal("Charlie", relation1.Children[1].Name);
        Assert.Equal("David", relation1.Children[2].Name);
    }

    [Fact]
    public void NoChildComparer_MaintainsInsertionOrder()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId,
            childComparer: null); // No sorting
        
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var groupId = Guid.NewGuid();
        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetOneToManyRelation(group);

        // Act - Add in specific order
        childStore.Add(new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Zara" });
        childStore.Add(new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Anna" });
        childStore.Add(new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Mike" });

        // Assert - Should maintain insertion order (not sorted)
        Assert.Equal(3, relation.Children.Count);
        Assert.Equal("Zara", relation.Children[0].Name);
        Assert.Equal("Anna", relation.Children[1].Name);
        Assert.Equal("Mike", relation.Children[2].Name);
    }

    [Fact]
    public void ChildComparer_SortsExistingChildrenOnConstruction()
    {
        // Arrange
        var parentStore = new InMemoryDataStore<Group>();
        var childStore = new InMemoryDataStore<Member>();
        
        var groupId = Guid.NewGuid();
        childStore.AddRange(new[]
        {
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Zoe" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Adam" },
            new Member { Id = Guid.NewGuid(), GroupId = groupId, Name = "Molly" }
        });

        var definition = new RelationDefinition<Group, Member, Guid>(
            parent => parent.Id,
            child => child.GroupId,
            childComparer: new MemberNameComparer());

        // Act - Create service AFTER children exist
        var service = new RelationViewService<Group, Member, Guid>(
            parentStore, childStore, definition);

        var group = new Group { Id = groupId, Name = "Group1" };
        var relation = service.GetOneToManyRelation(group);

        // Assert - Existing children should be sorted
        Assert.Equal(3, relation.Children.Count);
        Assert.Equal("Adam", relation.Children[0].Name);
        Assert.Equal("Molly", relation.Children[1].Name);
        Assert.Equal("Zoe", relation.Children[2].Name);
    }
}
