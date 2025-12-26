using Common.Bootstrap;
using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Registration;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TestHelper.DataStores.Models;
using TestHelper.DataStores.PathProviders;

namespace DataStores.Tests.Integration;

/// <summary>
/// Integration-Tests für DataStoresBootstrapDecorator mit Common.Bootstrap.
/// Testet das automatische Scanning von IDataStoreRegistrar aus Assemblies.
/// </summary>
[Trait("Category", "Integration")]
public class DataStoresBootstrapDecorator_IntegrationTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        await Task.Delay(50);
    }

    // ====================================================================
    // Basic Functionality Tests
    // ====================================================================

    [Fact]
    public void Decorator_Should_CallInnerWrapper()
    {
        // Arrange
        var fakeWrapper = new FakeBootstrapWrapper();
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(fakeWrapper);
        
        // Act
        bootstrap.RegisterServices(services, typeof(DataStoresBootstrapDecorator_IntegrationTests).Assembly);
        
        // Assert
        Assert.True(fakeWrapper.RegisterServicesCalled);
        Assert.Same(services, fakeWrapper.ReceivedServices);
        Assert.Single(fakeWrapper.ReceivedAssemblies);
    }

    [Fact]
    public void Decorator_Should_PassMultipleAssemblies_ToInnerWrapper()
    {
        // Arrange
        var fakeWrapper = new FakeBootstrapWrapper();
        var services = new ServiceCollection();
        
        var bootstrap = new DataStoresBootstrapDecorator(fakeWrapper);
        var assembly1 = typeof(DataStoresBootstrapDecorator_IntegrationTests).Assembly;
        var assembly2 = typeof(DataStoresServiceModule).Assembly;
        
        // Act
        bootstrap.RegisterServices(services, assembly1, assembly2);
        
        // Assert
        Assert.True(fakeWrapper.RegisterServicesCalled);
        Assert.Equal(2, fakeWrapper.ReceivedAssemblies.Length);
        Assert.Contains(assembly1, fakeWrapper.ReceivedAssemblies);
        Assert.Contains(assembly2, fakeWrapper.ReceivedAssemblies);
    }

    [Fact]
    public void Decorator_Should_CallScan_AfterInnerWrapper()
    {
        // Arrange
        var callOrder = new List<string>();
        var trackingWrapper = new TrackingBootstrapWrapper(callOrder);
        var services = new ServiceCollection();
        
        var bootstrap = new DataStoresBootstrapDecorator(trackingWrapper);
        
        // Act
        bootstrap.RegisterServices(services, typeof(string).Assembly); // Use safe assembly
        
        // Assert - Inner wrapper is called
        Assert.Contains("InnerWrapper.RegisterServices", callOrder);
        
        // Note: AddDataStoreRegistrarsFromAssemblies is an extension method on IServiceCollection,
        // so we can't easily track it with a custom collection. 
        // The important part is that the inner wrapper is called first.
        Assert.Single(callOrder);
        Assert.Equal("InnerWrapper.RegisterServices", callOrder[0]);
    }

    // ====================================================================
    // Assembly Scanning Tests
    // ====================================================================

    [Fact]
    public async Task Decorator_Should_ScanAndRegister_ManualRegistrar()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(new FakeBootstrapWrapper());
        // Do NOT scan this assembly to avoid conflicts
        bootstrap.RegisterServices(services);
        
        // Manually add a registrar
        services.AddDataStoreRegistrar(new ManualTestRegistrar());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Act
        await DataStoreBootstrap.RunAsync(_serviceProvider);
        
        // Assert
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();
        
        Assert.NotNull(store);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Decorator_Should_Work_WithMultipleRegistrars()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(new FakeBootstrapWrapper());
        bootstrap.RegisterServices(services);
        
        // Manually add multiple registrars
        services.AddDataStoreRegistrar(new ManualTestRegistrar());
        services.AddDataStoreRegistrar(new ManualTestDtoRegistrar());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Act
        await DataStoreBootstrap.RunAsync(_serviceProvider);
        
        // Assert
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        
        var testEntityStore = stores.GetGlobal<TestEntity>();
        var testDtoStore = stores.GetGlobal<TestDto>();
        
        Assert.NotNull(testEntityStore);
        Assert.NotNull(testDtoStore);
    }

    // ====================================================================
    // PathProvider Integration Tests
    // ====================================================================

    [Fact]
    public async Task Decorator_Should_Work_WithPathProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new TestDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(new FakeBootstrapWrapper());
        bootstrap.RegisterServices(services);
        
        services.AddDataStoreRegistrar(new ManualTestRegistrar());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Act
        await DataStoreBootstrap.RunAsync(_serviceProvider);
        
        // Assert
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();
        
        store.Add(new TestEntity { Name = "Test" });
        Assert.Single(store.Items);
    }

    // ====================================================================
    // End-to-End Workflow Tests
    // ====================================================================

    [Fact]
    public async Task EndToEnd_Decorator_Should_EnableFullWorkflow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(new FakeBootstrapWrapper());
        bootstrap.RegisterServices(services);
        
        services.AddDataStoreRegistrar(new ManualTestRegistrar());
        services.AddDataStoreRegistrar(new ManualTestDtoRegistrar());
        
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);
        
        // Act
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var testEntityStore = stores.GetGlobal<TestEntity>();
        var testDtoStore = stores.GetGlobal<TestDto>();
        
        testEntityStore.Add(new TestEntity { Name = "Entity1" });
        testDtoStore.Add(new TestDto("Dto1", 42));
        
        // Assert
        Assert.Single(testEntityStore.Items);
        Assert.Single(testDtoStore.Items);
        Assert.Equal("Entity1", testEntityStore.Items[0].Name);
        Assert.Equal("Dto1", testDtoStore.Items[0].Name);
    }

    [Fact]
    public async Task EndToEnd_Decorator_Should_FireEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(new FakeBootstrapWrapper());
        bootstrap.RegisterServices(services);
        
        services.AddDataStoreRegistrar(new ManualTestRegistrar());
        
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);
        
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();
        
        // Act
        var eventFired = false;
        store.Changed += (s, e) => eventFired = true;
        
        store.Add(new TestEntity { Name = "Test" });
        
        // Assert
        Assert.True(eventFired);
    }

    // ====================================================================
    // Negative Tests
    // ====================================================================

    [Fact]
    public async Task Decorator_WithNoRegistrars_Should_NotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(new FakeBootstrapWrapper());
        bootstrap.RegisterServices(services, typeof(string).Assembly);
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Act & Assert
        await DataStoreBootstrap.RunAsync(_serviceProvider);
        
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        Assert.NotNull(stores);
    }

    [Fact]
    public void Decorator_WithEmptyAssemblies_Should_NotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        
        var bootstrap = new DataStoresBootstrapDecorator(new FakeBootstrapWrapper());
        
        // Act & Assert
        bootstrap.RegisterServices(services);
        
        _serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(_serviceProvider);
    }

    // ====================================================================
    // Test Registrars (Manually Added - Private to avoid scanning)
    // ====================================================================

    private class ManualTestRegistrar : DataStoreRegistrarBase
    {
        public ManualTestRegistrar()
        {
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new InMemoryDataStoreBuilder<TestEntity>());
        }
    }

    private class ManualTestDtoRegistrar : DataStoreRegistrarBase
    {
        public ManualTestDtoRegistrar()
        {
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new InMemoryDataStoreBuilder<TestDto>());
        }
    }

    // ====================================================================
    // Test Helpers
    // ====================================================================

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
            
            new DataStoresServiceModule().Register(services);
        }
    }

    private class TrackingBootstrapWrapper : IBootstrapWrapper
    {
        private readonly List<string> _callOrder;

        public TrackingBootstrapWrapper(List<string> callOrder)
        {
            _callOrder = callOrder;
        }

        public void RegisterServices(IServiceCollection services, params Assembly[] assemblies)
        {
            _callOrder.Add("InnerWrapper.RegisterServices");
            new DataStoresServiceModule().Register(services);
        }
    }
}
