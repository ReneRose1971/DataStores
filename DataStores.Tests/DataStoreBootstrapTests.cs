using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Tests;

public class DataStoreBootstrapTests
{
    private class TestItem
    {
        public int Id { get; set; }
    }

    private class TestRegistrar : IDataStoreRegistrar
    {
        public bool WasCalled { get; private set; }
        public IGlobalStoreRegistry? ReceivedRegistry { get; private set; }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            WasCalled = true;
            ReceivedRegistry = registry;
            registry.RegisterGlobal(new InMemoryDataStore<TestItem>());
        }
    }

    [Fact]
    public void Run_Should_CallAllRegistrars()
    {
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        var registrar = new TestRegistrar();
        services.AddSingleton<IDataStoreRegistrar>(registrar);

        var provider = services.BuildServiceProvider();
        DataStoreBootstrap.Run(provider);

        Assert.True(registrar.WasCalled);
    }

    [Fact]
    public void Run_Should_PassRegistry()
    {
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        var registrar = new TestRegistrar();
        services.AddSingleton<IDataStoreRegistrar>(registrar);

        var provider = services.BuildServiceProvider();
        DataStoreBootstrap.Run(provider);

        Assert.NotNull(registrar.ReceivedRegistry);
    }

    [Fact]
    public void Run_Should_RegisterStores()
    {
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, TestRegistrar>();

        var provider = services.BuildServiceProvider();
        DataStoreBootstrap.Run(provider);

        var stores = provider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestItem>();
        Assert.NotNull(store);
    }

    [Fact]
    public async Task RunAsync_Should_InitializeAsyncInitializables()
    {
        var services = new ServiceCollection();
        services.AddDataStoresCore();

        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: true, autoSaveOnChange: false);
        services.AddSingleton<IAsyncInitializable>(decorator);

        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);

        Assert.Equal(1, strategy.LoadCallCount);
    }

    [Fact]
    public void Run_Should_WorkWithNoRegistrars()
    {
        var services = new ServiceCollection();
        services.AddDataStoresCore();

        var provider = services.BuildServiceProvider();

        DataStoreBootstrap.Run(provider);
    }
}
