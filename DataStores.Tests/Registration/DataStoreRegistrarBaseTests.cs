using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Registration;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Fakes;
using TestHelper.DataStores.PathProviders;

namespace DataStores.Tests.Registration;

[Trait("Category", "Unit")]
public class DataStoreRegistrarBaseTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class SimpleRegistrar : DataStoreRegistrarBase
    {
        public SimpleRegistrar() { }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new InMemoryDataStoreBuilder<TestEntity>());
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
    public void Register_Should_RegisterAllAddedStores()
    {
        var registrar = new SimpleRegistrar();
        var registry = new GlobalStoreRegistry();
        var provider = CreateServiceProvider();

        registrar.Register(registry, provider);

        var store = registry.ResolveGlobal<TestEntity>();
        Assert.NotNull(store);
    }

    [Fact]
    public void Register_Should_RegisterStoresInOrder()
    {
        var registrar = new MultiStoreRegistrar();
        var registry = new GlobalStoreRegistry();
        var provider = CreateServiceProvider();

        registrar.Register(registry, provider);

        Assert.NotNull(registry.ResolveGlobal<Product>());
        Assert.NotNull(registry.ResolveGlobal<Customer>());
    }

    private class Product
    {
        public int Id { get; set; }
    }

    private class Customer
    {
        public int Id { get; set; }
    }

    private class MultiStoreRegistrar : DataStoreRegistrarBase
    {
        public MultiStoreRegistrar() { }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new InMemoryDataStoreBuilder<Product>());
            AddStore(new InMemoryDataStoreBuilder<Customer>());
        }
    }

    [Fact]
    public void Register_Should_WorkWithDependencyInjection()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        services.AddDataStoreRegistrar<SimpleRegistrar>();

        var provider = services.BuildServiceProvider();
        DataStoreBootstrap.Run(provider);

        var stores = provider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();
        Assert.NotNull(store);
    }
}
