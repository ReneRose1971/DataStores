using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Tests;

public class DataStoresFacadeTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void GetGlobal_Should_ReturnSameInstanceAsRegistry()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        var result = facade.GetGlobal<TestItem>();

        Assert.Same(store, result);
    }

    [Fact]
    public void GetGlobal_Should_ThrowWhenNotRegistered()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);

        Assert.Throws<GlobalStoreNotRegisteredException>(() => facade.GetGlobal<TestItem>());
    }

    [Fact]
    public void CreateLocal_Should_ReturnNewInstanceEachTime()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);

        var local1 = facade.CreateLocal<TestItem>();
        var local2 = facade.CreateLocal<TestItem>();

        Assert.NotSame(local1, local2);
    }

    [Fact]
    public void CreateLocal_Should_ReturnEmptyStore()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);

        var local = facade.CreateLocal<TestItem>();

        Assert.Empty(local.Items);
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_Should_CopyItems()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.Add(new TestItem { Id = 1, Name = "Item1" });
        globalStore.Add(new TestItem { Id = 2, Name = "Item2" });
        registry.RegisterGlobal(globalStore);

        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>();

        Assert.Equal(2, snapshot.Items.Count);
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_Should_NotShareInstances()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.Add(new TestItem { Id = 1, Name = "Item1" });
        registry.RegisterGlobal(globalStore);

        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>();
        globalStore.Add(new TestItem { Id = 2, Name = "Item2" });

        Assert.Single(snapshot.Items);
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_Should_ApplyPredicate()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.Add(new TestItem { Id = 1, Name = "Item1" });
        globalStore.Add(new TestItem { Id = 2, Name = "Item2" });
        globalStore.Add(new TestItem { Id = 3, Name = "Item3" });
        registry.RegisterGlobal(globalStore);

        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>(x => x.Id > 1);

        Assert.Equal(2, snapshot.Items.Count);
    }

    [Fact]
    public void CreateLocalSnapshotFromGlobal_Should_FilterCorrectly()
    {
        var registry = new GlobalStoreRegistry();
        var factory = new LocalDataStoreFactory();
        var facade = new DataStoresFacade(registry, factory);
        var globalStore = new InMemoryDataStore<TestItem>();
        globalStore.Add(new TestItem { Id = 1, Name = "Item1" });
        globalStore.Add(new TestItem { Id = 2, Name = "Item2" });
        registry.RegisterGlobal(globalStore);

        var snapshot = facade.CreateLocalSnapshotFromGlobal<TestItem>(x => x.Id == 1);

        Assert.Equal(1, snapshot.Items[0].Id);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenRegistryIsNull()
    {
        var factory = new LocalDataStoreFactory();

        Assert.Throws<ArgumentNullException>(() => new DataStoresFacade(null!, factory));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenFactoryIsNull()
    {
        var registry = new GlobalStoreRegistry();

        Assert.Throws<ArgumentNullException>(() => new DataStoresFacade(registry, null!));
    }
}
