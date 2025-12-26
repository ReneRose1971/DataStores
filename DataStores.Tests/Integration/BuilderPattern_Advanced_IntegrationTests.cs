using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Registration;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Models;
using TestHelper.DataStores.PathProviders;
using TestHelper.DataStores.TestSetup;

namespace DataStores.Tests.Integration;

/// <summary>
/// Erweiterte Tests für Builder Pattern mit Fokus auf Parameter-Kombinationen
/// und Edge-Cases.
/// </summary>
[Trait("Category", "Integration")]
public class BuilderPattern_Advanced_IntegrationTests : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private readonly string _testDbPath;
    private readonly string _testJsonPath;

    public BuilderPattern_Advanced_IntegrationTests()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"BuilderAdv_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);
        _testDbPath = Path.Combine(tempPath, "advanced.db");
        _testJsonPath = Path.Combine(tempPath, "advanced.json");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        await Task.Delay(100);
        
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
        if (File.Exists(_testJsonPath))
            File.Delete(_testJsonPath);
    }

    // ====================================================================
    // Auto-Load FALSE Tests
    // ====================================================================

    [Fact]
    public async Task JsonBuilder_WithAutoLoadFalse_Should_StartEmpty()
    {
        // Arrange: Pre-create JSON file with data
        var testData = new List<TestDto>
        {
            new TestDto("ShouldNotLoad", 30)
        };
        await File.WriteAllTextAsync(_testJsonPath, 
            System.Text.Json.JsonSerializer.Serialize(testData));

        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new JsonNoAutoLoadRegistrar(_testJsonPath));

        // Act
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Assert: Store is empty despite existing file
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task LiteDbBuilder_WithAutoLoadFalse_Should_StartEmpty()
    {
        // Arrange: Pre-create database with data
        using (var db = new LiteDB.LiteDatabase(_testDbPath))
        {
            var collection = db.GetCollection<TestEntity>("TestEntity");
            collection.Insert(new TestEntity { Id = 1, Name = "ShouldNotLoad" });
        }

        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new LiteDbNoAutoLoadRegistrar(_testDbPath));

        // Act
        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();

        // Assert: Store is empty despite existing database
        Assert.Empty(store.Items);
    }

    // ====================================================================
    // Auto-Save FALSE Tests
    // ====================================================================

    [Fact]
    public async Task JsonBuilder_WithAutoSaveFalse_Should_NotPersist()
    {
        // Arrange
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new JsonNoAutoSaveRegistrar(_testJsonPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Act: Add data and wait
        store.Add(new TestDto("ShouldNotPersist", 25));
        await Task.Delay(300); // Normal auto-save delay

        // Assert: File not created (no auto-save)
        Assert.False(File.Exists(_testJsonPath));
    }

    [Fact]
    public async Task LiteDbBuilder_WithAutoSaveFalse_Should_NotPersist()
    {
        // Arrange
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new LiteDbNoAutoSaveRegistrar(_testDbPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();

        // Act: Add data and wait
        store.Add(new TestEntity { Name = "ShouldNotPersist" });
        await Task.Delay(300);

        // Assert: Database file not created (no auto-save)
        Assert.False(File.Exists(_testDbPath));
    }

    // ====================================================================
    // Collection Name Auto-Generation Tests
    // ====================================================================

    [Fact]
    public async Task LiteDbBuilder_Should_UseTypeNameAsCollectionName()
    {
        // Arrange
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new LiteDbOnlyRegistrar(_testDbPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestEntity>();

        // Act: Add and persist data
        store.Add(new TestEntity { Name = "Test" });
        await Task.Delay(200);

        // Assert: Collection name matches type name
        using (var db = new LiteDB.LiteDatabase(_testDbPath))
        {
            var collectionNames = db.GetCollectionNames().ToList();
            Assert.Contains("TestEntity", collectionNames);
        }
    }

    [Fact]
    public async Task LiteDbBuilder_WithMultipleTypes_Should_CreateSeparateCollections()
    {
        // Arrange
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new MultiLiteDbRegistrar(_testDbPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();

        // Act: Add data to both stores
        stores.GetGlobal<TestEntity>().Add(new TestEntity { Name = "Entity1" });
        stores.GetGlobal<OrderEntity>().Add(new OrderEntity { Name = "Order1" });
        await Task.Delay(300);

        // Assert: Two separate collections exist
        using (var db = new LiteDB.LiteDatabase(_testDbPath))
        {
            var collectionNames = db.GetCollectionNames().ToList();
            Assert.Contains("TestEntity", collectionNames);
            Assert.Contains("OrderEntity", collectionNames);
            Assert.Equal(2, collectionNames.Count);
        }
    }

    // ====================================================================
    // Comparer Integration Tests
    // ====================================================================

    [Fact]
    public async Task InMemoryBuilder_WithComparer_Should_UseForContains()
    {
        // Arrange
        var comparer = new NameOnlyComparer();
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new ComparerRegistrar(comparer));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<SimpleEntity>();

        // Act: Add entity, check with different instance but same name
        var entity1 = new SimpleEntity { Id = 1, Name = "Test" };
        var entity2 = new SimpleEntity { Id = 999, Name = "Test" }; // Different ID, same Name

        store.Add(entity1);
        var contains = store.Contains(entity2);

        // Assert: Comparer matches with Name only
        Assert.True(contains);
    }

    [Fact]
    public async Task InMemoryBuilder_WithComparer_Should_UseForRemove()
    {
        // Arrange
        var comparer = new NameOnlyComparer();
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new ComparerRegistrar(comparer));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<SimpleEntity>();

        // Act: Add entity, remove with different instance but same name
        var entity1 = new SimpleEntity { Id = 1, Name = "Test" };
        var entity2 = new SimpleEntity { Id = 999, Name = "Test" };

        store.Add(entity1);
        var removed = store.Remove(entity2); // Should remove entity1

        // Assert: Remove used comparer
        Assert.True(removed);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task JsonBuilder_WithComparer_Should_UseForStoreOperations()
    {
        // Arrange
        var comparer = new TestDtoNameComparer();
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new JsonComparerRegistrar(_testJsonPath, comparer));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Act
        var dto1 = new TestDto("Alice", 30);
        var dto2 = new TestDto("Alice", 999); // Same name, different age

        store.Add(dto1);
        var contains = store.Contains(dto2);

        // Assert: Comparer matches by Name
        Assert.True(contains);
    }

    // ====================================================================
    // SynchronizationContext Integration Tests
    // ====================================================================

    [Fact]
    public async Task InMemoryBuilder_WithSyncContext_Should_MarshalChangedEvent()
    {
        // Arrange
        var syncContext = new TestSynchronizationContext();
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new SyncContextRegistrar(syncContext));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<SimpleEntity>();

        // Act
        var eventReceived = false;
        store.Changed += (s, e) => eventReceived = true;
        
        store.Add(new SimpleEntity { Id = 1, Name = "Test" });
        await Task.Delay(50);

        // Assert
        Assert.True(eventReceived);
        Assert.True(syncContext.PostWasCalled);
    }

    [Fact]
    public async Task JsonBuilder_WithSyncContext_Should_MarshalAllEvents()
    {
        // Arrange
        var syncContext = new TestSynchronizationContext();
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(new JsonSyncContextRegistrar(_testJsonPath, syncContext));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Act: Test multiple event types
        var addEventCount = 0;
        var removeEventCount = 0;
        
        store.Changed += (s, e) =>
        {
            if (e.ChangeType == DataStoreChangeType.Add) addEventCount++;
            if (e.ChangeType == DataStoreChangeType.Remove) removeEventCount++;
        };

        var dto = new TestDto("Test", 25);
        store.Add(dto);
        await Task.Delay(50);
        
        store.Remove(dto);
        await Task.Delay(50);

        // Assert: Both events were marshalled
        Assert.Equal(1, addEventCount);
        Assert.Equal(1, removeEventCount);
        Assert.True(syncContext.PostWasCalled);
    }

    // ====================================================================
    // Combined Parameters Tests
    // ====================================================================

    [Fact]
    public async Task Builder_WithComparerAndSyncContext_Should_UseBoth()
    {
        // Arrange
        var comparer = new NameOnlyComparer();
        var syncContext = new TestSynchronizationContext();
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(
            new CombinedParametersRegistrar(comparer, syncContext));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<SimpleEntity>();

        // Act: Test comparer
        var entity1 = new SimpleEntity { Id = 1, Name = "Test" };
        var entity2 = new SimpleEntity { Id = 999, Name = "Test" };
        store.Add(entity1);
        var comparerWorks = store.Contains(entity2);

        // Test sync context
        var eventFired = false;
        store.Changed += (s, e) => eventFired = true;
        store.Add(new SimpleEntity { Id = 2, Name = "Another" });
        await Task.Delay(50);

        // Assert: Both features work
        Assert.True(comparerWorks);
        Assert.True(eventFired);
        Assert.True(syncContext.PostWasCalled);
    }

    [Fact]
    public async Task JsonBuilder_WithAllParameters_Should_Work()
    {
        // Arrange
        var comparer = new TestDtoNameComparer();
        var syncContext = new TestSynchronizationContext();
        var services = DataStoreTestSetup.CreateTestServices();
        services.AddDataStoreRegistrar(
            new JsonAllParametersRegistrar(_testJsonPath, comparer, syncContext));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        var stores = _serviceProvider.GetRequiredService<IDataStores>();
        var store = stores.GetGlobal<TestDto>();

        // Act & Assert: Test all features
        // 1. Comparer
        store.Add(new TestDto("Alice", 30));
        Assert.True(store.Contains(new TestDto("Alice", 999)));

        // 2. Events + SyncContext
        var eventFired = false;
        store.Changed += (s, e) => eventFired = true;
        store.Add(new TestDto("Bob", 25));
        await Task.Delay(50);
        Assert.True(eventFired);
        Assert.True(syncContext.PostWasCalled);

        // 3. Persistence (auto-save)
        await Task.Delay(200);
        Assert.True(File.Exists(_testJsonPath));
    }

    // ====================================================================
    // Test Registrars
    // ====================================================================

    private class JsonNoAutoLoadRegistrar : DataStoreRegistrarBase
    {
        private readonly string _jsonPath;

        public JsonNoAutoLoadRegistrar(string jsonPath)
        {
            _jsonPath = jsonPath;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(
                filePath: _jsonPath,
                autoLoad: false,
                autoSave: true));
        }
    }

    private class LiteDbNoAutoLoadRegistrar : DataStoreRegistrarBase
    {
        private readonly string _dbPath;

        public LiteDbNoAutoLoadRegistrar(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new LiteDbDataStoreBuilder<TestEntity>(
                databasePath: _dbPath,
                autoLoad: false,
                autoSave: true));
        }
    }

    private class JsonNoAutoSaveRegistrar : DataStoreRegistrarBase
    {
        private readonly string _jsonPath;

        public JsonNoAutoSaveRegistrar(string jsonPath)
        {
            _jsonPath = jsonPath;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(
                filePath: _jsonPath,
                autoLoad: false,
                autoSave: false));
        }
    }

    private class LiteDbNoAutoSaveRegistrar : DataStoreRegistrarBase
    {
        private readonly string _dbPath;

        public LiteDbNoAutoSaveRegistrar(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new LiteDbDataStoreBuilder<TestEntity>(
                databasePath: _dbPath,
                autoLoad: false,
                autoSave: false));
        }
    }

    private class LiteDbOnlyRegistrar : DataStoreRegistrarBase
    {
        private readonly string _dbPath;

        public LiteDbOnlyRegistrar(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new LiteDbDataStoreBuilder<TestEntity>(databasePath: _dbPath));
        }
    }

    private class MultiLiteDbRegistrar : DataStoreRegistrarBase
    {
        private readonly string _dbPath;

        public MultiLiteDbRegistrar(string dbPath)
        {
            _dbPath = dbPath;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new LiteDbDataStoreBuilder<TestEntity>(databasePath: _dbPath));
            AddStore(new LiteDbDataStoreBuilder<OrderEntity>(databasePath: _dbPath));
        }
    }

    private class ComparerRegistrar : DataStoreRegistrarBase
    {
        private readonly IEqualityComparer<SimpleEntity> _comparer;

        public ComparerRegistrar(IEqualityComparer<SimpleEntity> comparer)
        {
            _comparer = comparer;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new InMemoryDataStoreBuilder<SimpleEntity>(comparer: _comparer));
        }
    }

    private class JsonComparerRegistrar : DataStoreRegistrarBase
    {
        private readonly string _jsonPath;
        private readonly IEqualityComparer<TestDto> _comparer;

        public JsonComparerRegistrar(string jsonPath, IEqualityComparer<TestDto> comparer)
        {
            _jsonPath = jsonPath;
            _comparer = comparer;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(
                filePath: _jsonPath,
                comparer: _comparer));
        }
    }

    private class SyncContextRegistrar : DataStoreRegistrarBase
    {
        private readonly SynchronizationContext _syncContext;

        public SyncContextRegistrar(SynchronizationContext syncContext)
        {
            _syncContext = syncContext;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new InMemoryDataStoreBuilder<SimpleEntity>(
                synchronizationContext: _syncContext));
        }
    }

    private class JsonSyncContextRegistrar : DataStoreRegistrarBase
    {
        private readonly string _jsonPath;
        private readonly SynchronizationContext _syncContext;

        public JsonSyncContextRegistrar(string jsonPath, SynchronizationContext syncContext)
        {
            _jsonPath = jsonPath;
            _syncContext = syncContext;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(
                filePath: _jsonPath,
                synchronizationContext: _syncContext));
        }
    }

    private class CombinedParametersRegistrar : DataStoreRegistrarBase
    {
        private readonly IEqualityComparer<SimpleEntity> _comparer;
        private readonly SynchronizationContext _syncContext;

        public CombinedParametersRegistrar(
            IEqualityComparer<SimpleEntity> comparer,
            SynchronizationContext syncContext)
        {
            _comparer = comparer;
            _syncContext = syncContext;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new InMemoryDataStoreBuilder<SimpleEntity>(
                comparer: _comparer,
                synchronizationContext: _syncContext));
        }
    }

    private class JsonAllParametersRegistrar : DataStoreRegistrarBase
    {
        private readonly string _jsonPath;
        private readonly IEqualityComparer<TestDto> _comparer;
        private readonly SynchronizationContext _syncContext;

        public JsonAllParametersRegistrar(
            string jsonPath,
            IEqualityComparer<TestDto> comparer,
            SynchronizationContext syncContext)
        {
            _jsonPath = jsonPath;
            _comparer = comparer;
            _syncContext = syncContext;
        }

        protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
        {
            AddStore(new JsonDataStoreBuilder<TestDto>(
                filePath: _jsonPath,
                autoLoad: true,
                autoSave: true,
                comparer: _comparer,
                synchronizationContext: _syncContext));
        }
    }

    // ====================================================================
    // Test Helpers
    // ====================================================================

    private class SimpleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class OrderEntity : EntityBase
    {
        public string Name { get; set; } = "";
        public override string ToString() => $"Order #{Id}: {Name}";
        public override bool Equals(object? obj) => obj is OrderEntity o && Id > 0 && Id == o.Id;
        public override int GetHashCode() => Id;
    }

    private class NameOnlyComparer : IEqualityComparer<SimpleEntity>
    {
        public bool Equals(SimpleEntity? x, SimpleEntity? y)
        {
            if (x == null || y == null) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(SimpleEntity obj) => obj.Name.GetHashCode();
    }

    private class TestDtoNameComparer : IEqualityComparer<TestDto>
    {
        public bool Equals(TestDto? x, TestDto? y)
        {
            if (x == null || y == null) return false;
            return x.Name == y.Name;
        }

        public int GetHashCode(TestDto obj) => obj.Name.GetHashCode();
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
