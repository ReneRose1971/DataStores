using DataStores.Abstractions;
using DataStores.Relations;
using DataStores.Runtime;

namespace DataStores.Tests;

public class ParentChildRelationshipTests
{
    private class Parent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Child
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void UseGlobalDataSource_Should_SetDataSourceToGlobal()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id);

        relationship.UseGlobalDataSource();

        Assert.Same(globalStore, relationship.DataSource);
    }

    [Fact]
    public void UseSnapshotFromGlobal_Should_CreateLocalSnapshot()
    {
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
            (p, c) => c.ParentId == p.Id);

        relationship.UseSnapshotFromGlobal();

        Assert.NotSame(globalStore, relationship.DataSource);
    }

    [Fact]
    public void UseSnapshotFromGlobal_Should_CopyItems()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        globalStore.Add(new Child { Id = 1, ParentId = 1, Name = "Child1" });
        globalStore.Add(new Child { Id = 2, ParentId = 2, Name = "Child2" });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id);

        relationship.UseSnapshotFromGlobal();

        Assert.Equal(2, relationship.DataSource.Items.Count);
    }

    [Fact]
    public void Refresh_Should_CopyFilteredItemsIntoChilds()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        globalStore.Add(new Child { Id = 1, ParentId = 1, Name = "Child1" });
        globalStore.Add(new Child { Id = 2, ParentId = 2, Name = "Child2" });
        globalStore.Add(new Child { Id = 3, ParentId = 1, Name = "Child3" });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id);
        relationship.UseGlobalDataSource();

        relationship.Refresh();

        Assert.Equal(2, relationship.Childs.Items.Count);
    }

    [Fact]
    public void Refresh_Should_FilterCorrectly()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        globalStore.Add(new Child { Id = 1, ParentId = 1, Name = "Child1" });
        globalStore.Add(new Child { Id = 2, ParentId = 2, Name = "Child2" });
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id);
        relationship.UseGlobalDataSource();

        relationship.Refresh();

        Assert.All(relationship.Childs.Items, child => Assert.Equal(1, child.ParentId));
    }

    [Fact]
    public void Refresh_Should_ClearPreviousChilds()
    {
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
            (p, c) => c.ParentId == p.Id);
        relationship.UseGlobalDataSource();
        relationship.Refresh();
        globalStore.Clear();

        relationship.Refresh();

        Assert.Empty(relationship.Childs.Items);
    }

    [Fact]
    public void Childs_Should_BeLocalInMemoryStore()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<Child>();
        registry.RegisterGlobal(globalStore);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id);

        Assert.IsType<InMemoryDataStore<Child>>(relationship.Childs);
    }

    [Fact]
    public void Childs_Should_BeIndependentOfDataSource()
    {
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
            (p, c) => c.ParentId == p.Id);
        relationship.UseGlobalDataSource();
        relationship.Refresh();
        globalStore.Add(new Child { Id = 2, ParentId = 1, Name = "Child2" });

        Assert.Single(relationship.Childs.Items);
    }

    [Fact]
    public void Refresh_Should_ThrowWhenDataSourceNotSet()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id);

        Assert.Throws<InvalidOperationException>(() => relationship.Refresh());
    }

    [Fact]
    public void DataSource_Should_ThrowWhenNotSet()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);

        var parent = new Parent { Id = 1, Name = "Parent1" };
        var relationship = new ParentChildRelationship<Parent, Child>(
            stores,
            parent,
            (p, c) => c.ParentId == p.Id);

        Assert.Throws<InvalidOperationException>(() => _ = relationship.DataSource);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenStoresIsNull()
    {
        var parent = new Parent { Id = 1 };

        Assert.Throws<ArgumentNullException>(() =>
            new ParentChildRelationship<Parent, Child>(null!, parent, (p, c) => true));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenParentIsNull()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);

        Assert.Throws<ArgumentNullException>(() =>
            new ParentChildRelationship<Parent, Child>(stores, null!, (p, c) => true));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenFilterIsNull()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var stores = new DataStoresFacade(registry, factory);
        var parent = new Parent { Id = 1 };

        Assert.Throws<ArgumentNullException>(() =>
            new ParentChildRelationship<Parent, Child>(stores, parent, null!));
    }
}
