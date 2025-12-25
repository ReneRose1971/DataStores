using DataStores.Registration;
using DataStores.Runtime;

namespace DataStores.Tests.Registration;

[Trait("Category", "Unit")]
public class InMemoryDataStoreBuilderTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestRegistrar : DataStoreRegistrarBase
    {
        private readonly InMemoryDataStoreBuilder<TestItem> _builder;

        public TestRegistrar(InMemoryDataStoreBuilder<TestItem> builder)
        {
            _builder = builder;
            AddStore(_builder);
        }
    }

    [Fact]
    public void Register_Should_CreateInMemoryStore()
    {
        var builder = new InMemoryDataStoreBuilder<TestItem>();
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();

        registrar.Register(registry, null!);

        var store = registry.ResolveGlobal<TestItem>();
        Assert.NotNull(store);
        Assert.IsType<InMemoryDataStore<TestItem>>(store);
    }

    [Fact]
    public void Register_Should_CreateStoreWithComparer()
    {
        var comparer = EqualityComparer<TestItem>.Default;
        var builder = new InMemoryDataStoreBuilder<TestItem>(comparer: comparer);
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();

        registrar.Register(registry, null!);

        var store = registry.ResolveGlobal<TestItem>();
        Assert.NotNull(store);
    }

    [Fact]
    public void Register_Should_CreateStoreWithSynchronizationContext()
    {
        var syncContext = new SynchronizationContext();
        var builder = new InMemoryDataStoreBuilder<TestItem>(
            synchronizationContext: syncContext);
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();

        registrar.Register(registry, null!);

        var store = registry.ResolveGlobal<TestItem>();
        Assert.NotNull(store);
    }

    [Fact]
    public void Constructor_Should_AcceptNullParameters()
    {
        var builder = new InMemoryDataStoreBuilder<TestItem>(
            comparer: null,
            synchronizationContext: null);

        Assert.NotNull(builder);
    }
}
