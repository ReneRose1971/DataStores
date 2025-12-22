using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Tests;

public class ServiceCollectionExtensionsTests
{
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

    [Fact]
    public void AddDataStoreRegistrar_WithInstance_Should_RegisterRegistrar()
    {
        var services = new ServiceCollection();
        var registrar = new TestRegistrar();

        services.AddDataStoreRegistrar(registrar);

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IDataStoreRegistrar>();
        Assert.Contains(registrars, r => r == registrar);
    }

    [Fact]
    public void AddDataStoreRegistrar_WithNullInstance_Should_ThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddDataStoreRegistrar(null!));
    }
}
