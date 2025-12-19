using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Tests;

public class GlobalStoreRegistryTests
{
    private class TestItem
    {
        public int Id { get; set; }
    }

    private class AnotherTestItem
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void RegisterGlobal_Should_AllowFirstRegistration()
    {
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();

        registry.RegisterGlobal(store);

        var resolved = registry.ResolveGlobal<TestItem>();
        Assert.Same(store, resolved);
    }

    [Fact]
    public void RegisterGlobal_Should_ThrowOnDuplicateRegistration()
    {
        var registry = new GlobalStoreRegistry();
        var store1 = new InMemoryDataStore<TestItem>();
        var store2 = new InMemoryDataStore<TestItem>();

        registry.RegisterGlobal(store1);

        Assert.Throws<GlobalStoreAlreadyRegisteredException>(() => registry.RegisterGlobal(store2));
    }

    [Fact]
    public void RegisterGlobal_Should_AllowDifferentTypes()
    {
        var registry = new GlobalStoreRegistry();
        var store1 = new InMemoryDataStore<TestItem>();
        var store2 = new InMemoryDataStore<AnotherTestItem>();

        registry.RegisterGlobal(store1);
        registry.RegisterGlobal(store2);

        Assert.Same(store1, registry.ResolveGlobal<TestItem>());
    }

    [Fact]
    public void ResolveGlobal_Should_ReturnRegisteredStore()
    {
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        var resolved = registry.ResolveGlobal<TestItem>();

        Assert.Same(store, resolved);
    }

    [Fact]
    public void ResolveGlobal_Should_ThrowWhenMissing()
    {
        var registry = new GlobalStoreRegistry();

        Assert.Throws<GlobalStoreNotRegisteredException>(() => registry.ResolveGlobal<TestItem>());
    }

    [Fact]
    public void TryResolveGlobal_Should_ReturnTrue_WhenStoreExists()
    {
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        var result = registry.TryResolveGlobal<TestItem>(out var resolved);

        Assert.True(result);
    }

    [Fact]
    public void TryResolveGlobal_Should_ReturnStore_WhenStoreExists()
    {
        var registry = new GlobalStoreRegistry();
        var store = new InMemoryDataStore<TestItem>();
        registry.RegisterGlobal(store);

        registry.TryResolveGlobal<TestItem>(out var resolved);

        Assert.Same(store, resolved);
    }

    [Fact]
    public void TryResolveGlobal_Should_ReturnFalseWhenMissing()
    {
        var registry = new GlobalStoreRegistry();

        var result = registry.TryResolveGlobal<TestItem>(out _);

        Assert.False(result);
    }

    [Fact]
    public void RegisterGlobal_Should_ThrowWhenStoreIsNull()
    {
        var registry = new GlobalStoreRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.RegisterGlobal<TestItem>(null!));
    }
}
