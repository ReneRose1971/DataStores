using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DataStores.Tests;

public class ServiceCollectionExtensionsTests
{
    private class TestRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
        }
    }

    private class AnotherTestRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
        }
    }

    private class ParameterizedRegistrar : IDataStoreRegistrar
    {
        public string Parameter { get; }

        public ParameterizedRegistrar(string parameter)
        {
            Parameter = parameter;
        }

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

    [Fact]
    public void AddDataStoreRegistrarsFromAssembly_Should_RegisterAllRegistrars()
    {
        var services = new ServiceCollection();

        services.AddDataStoreRegistrarsFromAssembly(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IDataStoreRegistrar>().ToList();

        Assert.Contains(registrars, r => r is TestRegistrar);
        Assert.Contains(registrars, r => r is AnotherTestRegistrar);
    }

    [Fact]
    public void AddDataStoreRegistrarsFromAssembly_Should_NotRegisterParameterizedRegistrars()
    {
        var services = new ServiceCollection();

        services.AddDataStoreRegistrarsFromAssembly(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IDataStoreRegistrar>().ToList();

        Assert.DoesNotContain(registrars, r => r is ParameterizedRegistrar);
    }

    [Fact]
    public void AddDataStoreRegistrarsFromAssembly_WithNullServices_Should_Throw()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddDataStoreRegistrarsFromAssembly(null!, Assembly.GetExecutingAssembly()));
    }

    [Fact]
    public void AddDataStoreRegistrarsFromAssembly_WithNullAssembly_Should_Throw()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddDataStoreRegistrarsFromAssembly(null!));
    }

    [Fact]
    public void AddDataStoreRegistrarsFromAssemblies_Should_RegisterFromMultipleAssemblies()
    {
        var services = new ServiceCollection();

        services.AddDataStoreRegistrarsFromAssemblies(
            Assembly.GetExecutingAssembly(),
            typeof(DataStoresServiceModule).Assembly);

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IDataStoreRegistrar>().ToList();

        Assert.NotEmpty(registrars);
    }

    [Fact]
    public void AddDataStoreRegistrarsFromAssemblies_WithNullServices_Should_Throw()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddDataStoreRegistrarsFromAssemblies(null!, Assembly.GetExecutingAssembly()));
    }

    [Fact]
    public void AddDataStoreRegistrarsFromAssemblies_WithNullAssemblies_Should_Throw()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddDataStoreRegistrarsFromAssemblies(null!));
    }

    [Fact]
    public void AddDataStoreRegistrarsFromAssemblies_Should_SkipNullAssemblies()
    {
        var services = new ServiceCollection();

        services.AddDataStoreRegistrarsFromAssemblies(
            Assembly.GetExecutingAssembly(),
            null!,
            typeof(DataStoresServiceModule).Assembly);

        var provider = services.BuildServiceProvider();
        var registrars = provider.GetServices<IDataStoreRegistrar>().ToList();

        Assert.NotEmpty(registrars);
    }
}
