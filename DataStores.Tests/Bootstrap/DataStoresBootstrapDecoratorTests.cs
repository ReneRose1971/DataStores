using Common.Bootstrap;
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DataStores.Tests.Bootstrap;

[Trait("Category", "Unit")]
public class DataStoresBootstrapDecoratorTests
{
    [Fact]
    public void Constructor_WithNullInnerWrapper_Should_Throw()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DataStoresBootstrapDecorator(null!));

        Assert.Equal("innerWrapper", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidInnerWrapper_Should_Succeed()
    {
        // Arrange
        var innerWrapper = new FakeBootstrapWrapper();

        // Act
        var decorator = new DataStoresBootstrapDecorator(innerWrapper);

        // Assert
        Assert.NotNull(decorator);
    }

    [Fact]
    public void RegisterServices_Should_CallInnerWrapper()
    {
        // Arrange
        var innerWrapper = new FakeBootstrapWrapper();
        var decorator = new DataStoresBootstrapDecorator(innerWrapper);
        var services = new ServiceCollection();
        var assembly = typeof(DataStoresBootstrapDecoratorTests).Assembly;

        // Act
        decorator.RegisterServices(services, assembly);

        // Assert
        Assert.True(innerWrapper.RegisterServicesCalled);
        Assert.Same(services, innerWrapper.ReceivedServices);
        Assert.Contains(assembly, innerWrapper.ReceivedAssemblies);
    }

    [Fact]
    public void RegisterServices_Should_PassMultipleAssemblies()
    {
        // Arrange
        var innerWrapper = new FakeBootstrapWrapper();
        var decorator = new DataStoresBootstrapDecorator(innerWrapper);
        var services = new ServiceCollection();
        var assembly1 = typeof(DataStoresBootstrapDecoratorTests).Assembly;
        var assembly2 = typeof(DataStoresServiceModule).Assembly;

        // Act
        decorator.RegisterServices(services, assembly1, assembly2);

        // Assert
        Assert.True(innerWrapper.RegisterServicesCalled);
        Assert.Equal(2, innerWrapper.ReceivedAssemblies.Length);
        Assert.Contains(assembly1, innerWrapper.ReceivedAssemblies);
        Assert.Contains(assembly2, innerWrapper.ReceivedAssemblies);
    }

    [Fact]
    public void RegisterServices_Should_NotThrow_WithEmptyAssemblies()
    {
        // Arrange
        var innerWrapper = new FakeBootstrapWrapper();
        var decorator = new DataStoresBootstrapDecorator(innerWrapper);
        var services = new ServiceCollection();

        // Act
        decorator.RegisterServices(services);

        // Assert
        Assert.True(innerWrapper.RegisterServicesCalled);
        Assert.Empty(innerWrapper.ReceivedAssemblies);
    }

    // Fake-Implementation für Tests
    private class FakeBootstrapWrapper : IBootstrapWrapper
    {
        public bool RegisterServicesCalled { get; private set; }
        public IServiceCollection? ReceivedServices { get; private set; }
        public Assembly[] ReceivedAssemblies { get; private set; } = Array.Empty<Assembly>();

        public void RegisterServices(IServiceCollection services, params Assembly[] assemblies)
        {
            RegisterServicesCalled = true;
            ReceivedServices = services;
            ReceivedAssemblies = assemblies;
        }
    }
}
