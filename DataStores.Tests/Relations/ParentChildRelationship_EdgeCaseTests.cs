using DataStores.Relations;
using DataStores.Runtime;
using Xunit;

namespace DataStores.Tests.Relations;

/// <summary>
/// Edge case tests for ParentChildRelationship.
/// </summary>
public class ParentChildRelationship_EdgeCaseTests
{
    [Fact]
    public void Constructor_WithNullStores_Should_Throw()
    {
        // Arrange
        var parent = new Parent { Id = 1, Name = "Parent1" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ParentChildRelationship<Parent, Child>(null!, parent, (p, c) => true));
    }

    [Fact]
    public void Constructor_WithNullParent_Should_Throw()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ParentChildRelationship<Parent, Child>(stores, null!, (p, c) => true));
    }

    [Fact]
    public void Constructor_WithNullFilter_Should_Throw()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var parent = new Parent { Id = 1, Name = "Parent1" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ParentChildRelationship<Parent, Child>(stores, parent, null!));
    }

    [Fact]
    public void Refresh_WithNullDataSource_Should_Throw()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var parent = new Parent { Id = 1, Name = "Parent1" };
        
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);

        // Act & Assert - DataSource not set
        Assert.Throws<InvalidOperationException>(() => relationship.Refresh());
    }

    [Fact]
    public void DataSource_GetBeforeSet_Should_Throw()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var parent = new Parent { Id = 1, Name = "Parent1" };
        
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = relationship.DataSource);
    }

    [Fact]
    public void DataSource_SetToNull_Should_Throw()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var parent = new Parent { Id = 1, Name = "Parent1" };
        
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => relationship.DataSource = null!);
    }

    [Fact]
    public void Filter_ThrowsException_Should_Propagate()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        
        globalStore.Add(new Child { Id = 1, ParentId = 1, Name = "Child1" });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => throw new InvalidOperationException("Filter error"));

        relationship.UseGlobalDataSource();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => relationship.Refresh());
        Assert.Contains("Filter error", ex.Message);
    }

    [Fact]
    public void CascadeUpdate_Should_UpdateChilds()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        
        globalStore.AddRange(new[]
        {
            new Child { Id = 1, ParentId = 1, Name = "Child1" },
            new Child { Id = 2, ParentId = 1, Name = "Child2" },
            new Child { Id = 3, ParentId = 2, Name = "Child3" }
        });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);
        
        relationship.UseGlobalDataSource();
        relationship.Refresh();

        // Act - Add new child to global store and refresh
        globalStore.Add(new Child { Id = 4, ParentId = 1, Name = "Child4" });
        relationship.Refresh();

        // Assert
        Assert.Equal(3, relationship.Childs.Items.Count);
        Assert.All(relationship.Childs.Items, c => Assert.Equal(1, c.ParentId));
    }

    [Fact]
    public void MultipleRefresh_Should_ClearAndReload()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        
        globalStore.Add(new Child { Id = 1, ParentId = 1, Name = "Child1" });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);
        
        relationship.UseGlobalDataSource();

        // Act
        relationship.Refresh();
        Assert.Single(relationship.Childs.Items);

        globalStore.Add(new Child { Id = 2, ParentId = 1, Name = "Child2" });
        relationship.Refresh();

        // Assert - Should have both children after second refresh
        Assert.Equal(2, relationship.Childs.Items.Count);
    }

    [Fact]
    public void UseGlobalDataSource_TwiceInRow_Should_Work()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);

        // Act - Call twice
        relationship.UseGlobalDataSource();
        var dataSource1 = relationship.DataSource;
        
        relationship.UseGlobalDataSource();
        var dataSource2 = relationship.DataSource;

        // Assert - Both should reference the same global store
        Assert.Same(dataSource1, dataSource2);
        Assert.Same(globalStore, dataSource1);
    }

    [Fact]
    public void UseSnapshotFromGlobal_WithNullPredicate_Should_CopyAll()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        
        globalStore.AddRange(new[]
        {
            new Child { Id = 1, ParentId = 1, Name = "Child1" },
            new Child { Id = 2, ParentId = 2, Name = "Child2" },
            new Child { Id = 3, ParentId = 1, Name = "Child3" }
        });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);

        // Act - No predicate = copy all
        relationship.UseSnapshotFromGlobal(predicate: null);

        // Assert - DataSource should have all 3 items
        Assert.Equal(3, relationship.DataSource.Items.Count);
    }

    [Fact]
    public void Childs_Should_BeIndependentOfDataSource()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        
        globalStore.Add(new Child { Id = 1, ParentId = 1, Name = "Child1" });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);
        
        relationship.UseGlobalDataSource();
        relationship.Refresh();

        // Act - Add directly to Childs
        relationship.Childs.Add(new Child { Id = 99, ParentId = 1, Name = "Manual" });

        // Assert - DataSource should be unchanged
        Assert.Single(relationship.DataSource.Items);
        Assert.Equal(2, relationship.Childs.Items.Count);
    }

    [Fact]
    public void FilterWithComplexLogic_Should_Work()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        
        globalStore.AddRange(new[]
        {
            new Child { Id = 1, ParentId = 1, Name = "ActiveChild" },
            new Child { Id = 2, ParentId = 1, Name = "InactiveChild" },
            new Child { Id = 3, ParentId = 2, Name = "ActiveChild" }
        });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        
        // Complex filter: ParentId matches AND Name contains "Active"
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id && c.Name.Contains("Active"));
        
        relationship.UseGlobalDataSource();

        // Act
        relationship.Refresh();

        // Assert - Only one child matches both conditions
        Assert.Single(relationship.Childs.Items);
        Assert.Equal("ActiveChild", relationship.Childs.Items[0].Name);
    }

    [Fact]
    public void Parent_Should_BeAccessible()
    {
        // Arrange
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var parent = new Parent { Id = 1, Name = "Parent1" };
        
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores, parent, (p, c) => c.ParentId == p.Id);

        // Act & Assert
        Assert.NotNull(relationship.Parent);
        Assert.Equal(1, relationship.Parent.Id);
        Assert.Equal("Parent1", relationship.Parent.Name);
    }

    private class Parent
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class Child
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; } = "";
    }
}
