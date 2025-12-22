using Common.Bootstrap;
using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Tests.Bootstrap;

/// <summary>
/// Tests für das DataStoresServiceModule.
/// </summary>
[Trait("Category", "Unit")]
public class DataStoresServiceModuleTests
{
    [Fact]
    public void Register_Should_RegisterIGlobalStoreRegistry()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();

        module.Register(services);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetService<IGlobalStoreRegistry>();
        
        Assert.NotNull(registry);
        Assert.IsType<GlobalStoreRegistry>(registry);
    }

    [Fact]
    public void Register_Should_RegisterILocalDataStoreFactory()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();

        module.Register(services);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<ILocalDataStoreFactory>();
        
        Assert.NotNull(factory);
        Assert.IsType<LocalDataStoreFactory>(factory);
    }

    [Fact]
    public void Register_Should_RegisterIDataStores()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();

        module.Register(services);

        var provider = services.BuildServiceProvider();
        var stores = provider.GetService<IDataStores>();
        
        Assert.NotNull(stores);
        Assert.IsType<DataStoresFacade>(stores);
    }

    [Fact]
    public void Register_Should_RegisterAsSingleton()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();

        module.Register(services);

        var provider = services.BuildServiceProvider();
        var stores1 = provider.GetService<IDataStores>();
        var stores2 = provider.GetService<IDataStores>();
        
        Assert.Same(stores1, stores2);
    }

    [Fact]
    public void Register_Should_RegisterAllServicesAtOnce()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();

        module.Register(services);

        var provider = services.BuildServiceProvider();
        
        Assert.NotNull(provider.GetService<IGlobalStoreRegistry>());
        Assert.NotNull(provider.GetService<ILocalDataStoreFactory>());
        Assert.NotNull(provider.GetService<IDataStores>());
    }

    [Fact]
    public void Module_Should_ImplementIServiceModule()
    {
        var module = new DataStoresServiceModule();
        
        Assert.IsAssignableFrom<IServiceModule>(module);
    }

    [Fact]
    public void Register_Should_BeIdempotent()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();

        module.Register(services);
        module.Register(services);

        var provider = services.BuildServiceProvider();
        var stores = provider.GetService<IDataStores>();
        
        Assert.NotNull(stores);
    }

    [Fact]
    public void Register_Should_WorkWithAddModulesFromAssemblies()
    {
        var services = new ServiceCollection();
        
        services.AddModulesFromAssemblies(typeof(DataStoresServiceModule).Assembly);

        var provider = services.BuildServiceProvider();
        
        Assert.NotNull(provider.GetService<IGlobalStoreRegistry>());
        Assert.NotNull(provider.GetService<ILocalDataStoreFactory>());
        Assert.NotNull(provider.GetService<IDataStores>());
    }
}
