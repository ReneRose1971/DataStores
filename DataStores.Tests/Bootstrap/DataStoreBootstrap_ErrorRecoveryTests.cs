using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Runtime;
using DataStores.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DataStores.Tests.Bootstrap;

/// <summary>
/// Error recovery and edge case tests for DataStoreBootstrap.
/// </summary>
public class DataStoreBootstrap_ErrorRecoveryTests
{
    [Fact]
    public void Run_WithFailingRegistrar_Should_PropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, FailingRegistrar>();

        var provider = services.BuildServiceProvider();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DataStoreBootstrap.Run(provider));

        Assert.Contains("Registrar failed", ex.Message);
    }

    [Fact]
    public void Run_WithMultipleRegistrars_Should_ExecuteAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        
        var registrar1 = new TrackingRegistrar();
        var registrar2 = new TrackingRegistrar();
        
        services.AddSingleton<IDataStoreRegistrar>(registrar1);
        services.AddSingleton<IDataStoreRegistrar>(registrar2);

        var provider = services.BuildServiceProvider();

        // Act
        DataStoreBootstrap.Run(provider);

        // Assert - Both registrars were called
        Assert.True(registrar1.WasCalled);
        Assert.True(registrar2.WasCalled);
    }

    [Fact]
    public void Run_WithDuplicateRegistration_Should_Throw()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IDataStoreRegistrar, DuplicateRegistrar>();

        var provider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<GlobalStoreAlreadyRegisteredException>(() =>
            DataStoreBootstrap.Run(provider));
    }

    [Fact]
    public async Task RunAsync_WithCancellation_Should_Stop()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        
        var slowStrategy = new SlowInitStrategy<TestItem>(TimeSpan.FromSeconds(10));
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, slowStrategy, autoLoad: true, autoSaveOnChange: false);
        
        services.AddSingleton<IAsyncInitializable>(decorator);

        var provider = services.BuildServiceProvider();
        var cts = new CancellationTokenSource();

        // Act
        var runTask = DataStoreBootstrap.RunAsync(provider, cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    [Fact]
    public async Task RunAsync_WithAsyncInitializableException_Should_Propagate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<IAsyncInitializable, FailingAsyncInitializable>();

        var provider = services.BuildServiceProvider();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DataStoreBootstrap.RunAsync(provider));

        Assert.Contains("Async init failed", ex.Message);
    }

    [Fact]
    public void Run_WithNoRegistrars_Should_NotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();

        var provider = services.BuildServiceProvider();

        // Act & Assert - Should complete without error
        DataStoreBootstrap.Run(provider);
    }

    [Fact]
    public void Run_WithNullServiceProvider_Should_Throw()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DataStoreBootstrap.Run(null!));
    }

    [Fact]
    public async Task RunAsync_WithMultipleAsyncInitializables_Should_InitializeAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        
        var init1 = new TrackingAsyncInitializable();
        var init2 = new TrackingAsyncInitializable();
        
        services.AddSingleton<IAsyncInitializable>(init1);
        services.AddSingleton<IAsyncInitializable>(init2);

        var provider = services.BuildServiceProvider();

        // Act
        await DataStoreBootstrap.RunAsync(provider);

        // Assert
        Assert.True(init1.WasInitialized);
        Assert.True(init2.WasInitialized);
    }

    [Fact]
    public void Run_RegistrarAccessingServiceProvider_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddSingleton<TestService>();
        services.AddSingleton<IDataStoreRegistrar, ServiceProviderUsingRegistrar>();

        var provider = services.BuildServiceProvider();

        // Act
        DataStoreBootstrap.Run(provider);

        // Assert - Should complete without error
        var stores = provider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestItem>();
        Assert.NotNull(store);
    }

    [Fact]
    public async Task RunAsync_OrderOfOperations_Should_BeCorrect()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        
        var operations = new List<string>();
        var registrar = new OrderTrackingRegistrar(operations);
        var initializable = new OrderTrackingAsyncInitializable(operations);
        
        services.AddSingleton<IDataStoreRegistrar>(registrar);
        services.AddSingleton<IAsyncInitializable>(initializable);

        var provider = services.BuildServiceProvider();

        // Act
        await DataStoreBootstrap.RunAsync(provider);

        // Assert - Registrars should run before async initializables
        Assert.Equal(2, operations.Count);
        Assert.Equal("Register", operations[0]);
        Assert.Equal("Initialize", operations[1]);
    }

    // Helper Classes

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class TestService { }

    private class FailingRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            throw new InvalidOperationException("Registrar failed");
        }
    }

    private class TrackingRegistrar : IDataStoreRegistrar
    {
        private static int _counter = 0;
        private readonly int _id;
        public bool WasCalled { get; private set; }

        public TrackingRegistrar()
        {
            _id = Interlocked.Increment(ref _counter);
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            WasCalled = true;
            // Register different types to avoid conflicts
            if (_id == 1)
            {
                registry.RegisterGlobal(new InMemoryDataStore<TestItem>());
            }
            else
            {
                // For other instances, register a different type or check if already registered
                if (!registry.TryResolveGlobal<TestItem>(out _))
                {
                    registry.RegisterGlobal(new InMemoryDataStore<TestItem>());
                }
            }
        }
    }

    private class DuplicateRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            var store = new InMemoryDataStore<TestItem>();
            registry.RegisterGlobal(store);
            registry.RegisterGlobal(store); // Duplicate!
        }
    }

    private class SlowInitStrategy<T> : IPersistenceStrategy<T> where T : class
    {
        private readonly TimeSpan _delay;

        public SlowInitStrategy(TimeSpan delay)
        {
            _delay = delay;
        }

        public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return Array.Empty<T>();
        }

        public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class FailingAsyncInitializable : IAsyncInitializable
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Async init failed");
        }
    }

    private class TrackingAsyncInitializable : IAsyncInitializable
    {
        public bool WasInitialized { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            WasInitialized = true;
            return Task.CompletedTask;
        }
    }

    private class ServiceProviderUsingRegistrar : IDataStoreRegistrar
    {
        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            var testService = serviceProvider.GetRequiredService<TestService>();
            Assert.NotNull(testService);
            
            registry.RegisterGlobal(new InMemoryDataStore<TestItem>());
        }
    }

    private class OrderTrackingRegistrar : IDataStoreRegistrar
    {
        private readonly List<string> _operations;

        public OrderTrackingRegistrar(List<string> operations)
        {
            _operations = operations;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            _operations.Add("Register");
            registry.RegisterGlobal(new InMemoryDataStore<TestItem>());
        }
    }

    private class OrderTrackingAsyncInitializable : IAsyncInitializable
    {
        private readonly List<string> _operations;

        public OrderTrackingAsyncInitializable(List<string> operations)
        {
            _operations = operations;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _operations.Add("Initialize");
            return Task.CompletedTask;
        }
    }
}
