using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Registration;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Models;

namespace DataStores.Tests.Integration;

/// <summary>
/// End-to-End Integration Tests für den kompletten Bootstrap-Flow mit Builder Pattern.
/// Testet die gesamte Initialisierungssequenz von DI-Setup bis Store-Nutzung.
/// </summary>
[Trait("Category", "Integration")]
public class BuilderPattern_EndToEnd_IntegrationTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private readonly string _testDbPath;
    private readonly string _testJsonPath;

    public BuilderPattern_EndToEnd_IntegrationTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"BuilderE2E_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);
        _testDbPath = Path.Combine(tempPath, "test.db");
        _testJsonPath = Path.Combine(tempPath, "test.json");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        
        await Task.Delay(100); // Ensure file handles are released
        
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
        if (File.Exists(_testJsonPath))
            File.Delete(_testJsonPath);
    }

    // ====================================================================
    // Complete Startup Flow Tests (6 Steps)
    // ====================================================================

    [Fact]
    public async Task CompleteStartupFlow_WithInMemoryBuilder_Should_Work()
    {
        // Step 1: DI Container Setup
        var services = new ServiceCollection();

        // Step 2: Register ServiceModule
        var module = new DataStoresServiceModule();
        module.Register(services);

        // Step 3: Register Builder-based Registrar
        services.AddDataStoreRegistrar(new InMemoryOnlyRegistrar());

        // Step 4: Build Service Provider
        _serviceProvider = services.BuildServiceProvider();

        // Step 5: Bootstrap Execution
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        // Step 6: Use via Facade
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();

        // Assert: Store is usable
        store.Add(new TestEntity { Name = "Test" });
        Assert.Single(store.Items);
    }

    [Fact]
    public async Task CompleteStartupFlow_WithJsonBuilder_Should_Work()
    {
        // Step 1-2: Setup
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);

        // Step 3: Register JSON Builder
        services.AddDataStoreRegistrar(new JsonOnlyRegistrar(_testJsonPath));

        // Step 4-5: Build and Bootstrap
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        // Step 6: Use via Facade
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Assert: Store is usable and persists
        store.Add(new TestDto("Test", 25));
        await Task.Delay(200); // Wait for auto-save

        Assert.True(File.Exists(_testJsonPath));
    }

    [Fact]
    public async Task CompleteStartupFlow_WithLiteDbBuilder_Should_Work()
    {
        // Step 1-2: Setup
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);

        // Step 3: Register LiteDB Builder
        services.AddDataStoreRegistrar(new LiteDbOnlyRegistrar(_testDbPath));

        // Step 4-5: Build and Bootstrap
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        // Step 6: Use via Facade
        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();

        // Assert: Store is usable and persists
        store.Add(new TestEntity { Name = "Test" });
        await Task.Delay(200); // Wait for auto-save

        Assert.True(File.Exists(_testDbPath));
    }

    // ====================================================================
    // Multi-Store Registrar Tests
    // ====================================================================

    [Fact]
    public async Task MultiStoreRegistrar_Should_RegisterAllStoreTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);

        // Registrar with InMemory, JSON, and LiteDB stores
        services.AddDataStoreRegistrar(new MultiTypeRegistrar(_testDbPath, _testJsonPath));

        // Act
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();

        // Assert: All stores are registered and accessible
        var productStore = stores.GetGlobal<Product>();
        var customerStore = stores.GetGlobal<Customer>();
        var orderStore = stores.GetGlobal<TestEntity>();

        Assert.NotNull(productStore);
        Assert.NotNull(customerStore);
        Assert.NotNull(orderStore);
    }

    [Fact]
    public async Task MultiStoreRegistrar_Should_AllowDataOperationsOnAllStores()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new MultiTypeRegistrar(_testDbPath, _testJsonPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();

        // Act: Add data to all stores
        var productStore = stores.GetGlobal<Product>();
        productStore.Add(new Product { Id = 1, Name = "Laptop" });

        var customerStore = stores.GetGlobal<Customer>();
        customerStore.Add(new Customer { Id = 1, Name = "Max" });

        var orderStore = stores.GetGlobal<TestEntity>();
        orderStore.Add(new TestEntity { Name = "Order1" });

        await Task.Delay(200); // Wait for auto-save

        // Assert: All stores contain data
        Assert.Single(productStore.Items);
        Assert.Single(customerStore.Items);
        Assert.Single(orderStore.Items);
    }

    [Fact]
    public async Task MultiStoreRegistrar_Should_PersistOnlyPersistentStores()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new MultiTypeRegistrar(_testDbPath, _testJsonPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();

        // Act: Add data
        stores.GetGlobal<Product>().Add(new Product { Id = 1, Name = "Laptop" }); // InMemory
        stores.GetGlobal<Customer>().Add(new Customer { Id = 1, Name = "Max" }); // JSON
        stores.GetGlobal<TestEntity>().Add(new TestEntity { Name = "Order1" }); // LiteDB

        await Task.Delay(300); // Wait for auto-save

        // Assert: Only persistent stores have files
        Assert.True(File.Exists(_testJsonPath)); // JSON persisted
        Assert.True(File.Exists(_testDbPath));   // LiteDB persisted
    }

    // ====================================================================
    // Advanced Parameter Tests
    // ====================================================================

    [Fact]
    public async Task Builder_WithCustomComparer_Should_UseComparer()
    {
        // Arrange
        var comparer = new ProductIdComparer();
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new ComparerRegistrar(comparer));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<Product>();

        // Act: Add two products with same ID
        var product1 = new Product { Id = 1, Name = "Product1" };
        var product2 = new Product { Id = 1, Name = "Product2" };

        store.Add(product1);
        var containsProduct2 = store.Contains(product2); // Should use comparer

        // Assert: Comparer recognizes as same (both have Id=1)
        Assert.True(containsProduct2);
    }

    [Fact]
    public async Task Builder_WithSyncContext_Should_MarshalEvents()
    {
        // Arrange
        var syncContext = new TestSynchronizationContext();
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new SyncContextRegistrar(syncContext));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<Product>();

        // Act: Subscribe to event and add item
        var eventFired = false;
        store.Changed += (s, e) => eventFired = true;
        store.Add(new Product { Id = 1, Name = "Test" });

        // Wait for event marshalling
        await Task.Delay(100);

        // Assert: Event was marshalled to sync context
        Assert.True(eventFired);
        Assert.True(syncContext.PostWasCalled);
    }

    [Fact]
    public async Task Builder_WithAllOptions_Should_CreateCorrectStore()
    {
        // Arrange
        var comparer = new ProductIdComparer();
        var syncContext = new TestSynchronizationContext();
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new AdvancedRegistrar(_testJsonPath, comparer, syncContext));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<Product>();

        // Act: Test comparer
        var product1 = new Product { Id = 1, Name = "Product1" };
        var product2 = new Product { Id = 1, Name = "Product2" };
        store.Add(product1);
        var contains = store.Contains(product2);

        // Test sync context
        var eventFired = false;
        store.Changed += (s, e) => eventFired = true;
        store.Add(new Product { Id = 2, Name = "Test" });
        await Task.Delay(100);

        // Assert: Both features work
        Assert.True(contains); // Comparer works
        Assert.True(eventFired); // Events work
        Assert.True(syncContext.PostWasCalled); // SyncContext works
    }

    // ====================================================================
    // Auto-Load / Auto-Save Tests
    // ====================================================================

    [Fact]
    public async Task JsonBuilder_WithAutoLoadTrue_Should_LoadExistingData()
    {
        // Arrange: Pre-create JSON file
        var testData = new List<TestDto>
        {
            new TestDto("Existing1", 30),
            new TestDto("Existing2", 40)
        };
        await File.WriteAllTextAsync(_testJsonPath, 
            System.Text.Json.JsonSerializer.Serialize(testData));

        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new JsonOnlyRegistrar(_testJsonPath));

        // Act: Bootstrap (should load existing data)
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Assert: Pre-existing data was loaded
        Assert.Equal(2, store.Items.Count);
        Assert.Contains(store.Items, d => d.Name == "Existing1");
    }

    [Fact]
    public async Task JsonBuilder_WithAutoSaveTrue_Should_PersistChanges()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new JsonOnlyRegistrar(_testJsonPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Act: Add data
        store.Add(new TestDto("AutoSaveTest", 25));
        await Task.Delay(200); // Wait for auto-save

        // Assert: Data was persisted
        var jsonContent = await File.ReadAllTextAsync(_testJsonPath);
        Assert.Contains("AutoSaveTest", jsonContent);
    }

    // ====================================================================
    // Error Handling Tests
    // ====================================================================

    [Fact]
    public async Task StartupFlow_WithoutBootstrap_Should_ThrowOnStoreAccess()
    {
        // Arrange: Skip bootstrap step
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new InMemoryOnlyRegistrar());

        _serviceProvider = services.BuildServiceProvider();
        // NOTE: Bootstrap is intentionally skipped!

        var stores = _serviceProvider.GetRequiredService<IDataStores>();

        // Act & Assert: Accessing store without bootstrap throws
        Assert.Throws<GlobalStoreNotRegisteredException>(() => 
            stores.GetGlobal<TestEntity>());
    }

    [Fact]
    public async Task MultipleBootstrap_Should_BeIdempotent()
    {
        // Arrange
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        services.AddDataStoreRegistrar(new InMemoryOnlyRegistrar());

        _serviceProvider = services.BuildServiceProvider();

        // Act: Call bootstrap twice
        await DataStoreBootstrap.RunAsync(_serviceProvider);
        await DataStoreBootstrap.RunAsync(_serviceProvider); // Second call

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();

        // Assert: Store still works correctly
        store.Add(new TestEntity { Name = "Test" });
        Assert.Single(store.Items);
    }

    // ====================================================================
    // Test Registrars
    // ====================================================================

    private class InMemoryOnlyRegistrar : DataStoreRegistrarBase
    {
        public InMemoryOnlyRegistrar()
        {
            AddStore(new InMemoryDataStoreBuilder<TestEntity>());
        }
    }

    private class JsonOnlyRegistrar : DataStoreRegistrarBase
    {
        public JsonOnlyRegistrar(string jsonPath)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(
                filePath: jsonPath,
                autoLoad: true,
                autoSave: true));
        }
    }

    private class LiteDbOnlyRegistrar : DataStoreRegistrarBase
    {
        public LiteDbOnlyRegistrar(string dbPath)
        {
            AddStore(new LiteDbDataStoreBuilder<TestEntity>(
                databasePath: dbPath,
                autoLoad: true,
                autoSave: true));
        }
    }

    private class MultiTypeRegistrar : DataStoreRegistrarBase
    {
        public MultiTypeRegistrar(string dbPath, string jsonPath)
        {
            AddStore(new InMemoryDataStoreBuilder<Product>());
            AddStore(new JsonDataStoreBuilder<Customer>(jsonPath));
            AddStore(new LiteDbDataStoreBuilder<TestEntity>(dbPath));
        }
    }

    private class ComparerRegistrar : DataStoreRegistrarBase
    {
        public ComparerRegistrar(IEqualityComparer<Product> comparer)
        {
            AddStore(new InMemoryDataStoreBuilder<Product>(comparer: comparer));
        }
    }

    private class SyncContextRegistrar : DataStoreRegistrarBase
    {
        public SyncContextRegistrar(SynchronizationContext syncContext)
        {
            AddStore(new InMemoryDataStoreBuilder<Product>(
                synchronizationContext: syncContext));
        }
    }

    private class AdvancedRegistrar : DataStoreRegistrarBase
    {
        public AdvancedRegistrar(
            string jsonPath,
            IEqualityComparer<Product> comparer,
            SynchronizationContext syncContext)
        {
            AddStore(new JsonDataStoreBuilder<Product>(
                filePath: jsonPath,
                comparer: comparer,
                synchronizationContext: syncContext));
        }
    }

    // ====================================================================
    // Test Helpers
    // ====================================================================

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class ProductIdComparer : IEqualityComparer<Product>
    {
        public bool Equals(Product? x, Product? y)
        {
            if (x == null || y == null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(Product obj) => obj.Id;
    }

    private class TestSynchronizationContext : SynchronizationContext
    {
        public bool PostWasCalled { get; private set; }

        public override void Post(SendOrPostCallback d, object? state)
        {
            PostWasCalled = true;
            d(state);
        }
    }
}
