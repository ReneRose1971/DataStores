using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDataStoresCore_Should_RegisterIGlobalStoreRegistry()
    {
        var services = new ServiceCollection();

        services.AddDataStoresCore();

        var provider = services.BuildServiceProvider();
        var registry = provider.GetService<IGlobalStoreRegistry>();
        Assert.NotNull(registry);
    }

    [Fact]
    public void AddDataStoresCore_Should_RegisterILocalDataStoreFactory()
    {
        var services = new ServiceCollection();

        services.AddDataStoresCore();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<ILocalDataStoreFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void AddDataStoresCore_Should_RegisterIDataStores()
    {
        var services = new ServiceCollection();

        services.AddDataStoresCore();

        var provider = services.BuildServiceProvider();
        var stores = provider.GetService<IDataStores>();
        Assert.NotNull(stores);
    }

    [Fact]
    public void AddDataStoresCore_Should_RegisterAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddDataStoresCore();

        var provider = services.BuildServiceProvider();
        var stores1 = provider.GetService<IDataStores>();
        var stores2 = provider.GetService<IDataStores>();
        Assert.Same(stores1, stores2);
    }

    private class TestRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
        }
    }

    [Fact]
    public void AddDataStoreRegistrar_Should_RegisterRegistrar()
    {
        var services = new ServiceCollection();

        services.AddDataStoreRegistrar<TestRegistrar>();

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IDataStoreRegistrar>();
        Assert.Contains(registrars, r => r is TestRegistrar);
    }
}
