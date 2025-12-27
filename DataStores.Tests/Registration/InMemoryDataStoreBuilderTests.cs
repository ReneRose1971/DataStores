using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Registration;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Fakes;
using TestHelper.DataStores.PathProviders;

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
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(_builder);
        }
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        services.AddSingleton<IEqualityComparerService>(new FakeEqualityComparerService());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Register_Should_CreateInMemoryStore()
    {
        var builder = new InMemoryDataStoreBuilder<TestItem>();
        var registrar = new TestRegistrar(builder);
        var registry = new GlobalStoreRegistry();
        var provider = CreateServiceProvider();

        registrar.Register(registry, provider);

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
        var provider = CreateServiceProvider();

        registrar.Register(registry, provider);

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
        var provider = CreateServiceProvider();

        registrar.Register(registry, provider);

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
