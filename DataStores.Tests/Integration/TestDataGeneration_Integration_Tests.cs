using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Builders;
using TestHelper.DataStores.Models;
using TestHelper.DataStores.TestData;
using Xunit;

namespace DataStores.Tests.Integration;

/// <summary>
/// Integrationstests für Testdaten-Generierung mit realen Persistence-Szenarien.
/// </summary>
[Trait("Category", "Integration")]
public class TestDataGeneration_Integration_Tests : IAsyncLifetime
{
    private string _testDbPath = null!;
    private IServiceProvider _serviceProvider = null!;
    private IDataStores _dataStores = null!;

    public async Task InitializeAsync()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"TestDataGen_{Guid.NewGuid()}.db");

        var services = new ServiceCollection();
        
        // Manuelle Registrierung statt AddModulesFromAssemblies
        var module = new DataStoresServiceModule();
        module.Register(services);
        
        services.AddDataStoreRegistrar(new TestDataRegistrar(_testDbPath));

        _serviceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(_serviceProvider);

        _dataStores = _serviceProvider.GetRequiredService<IDataStores>();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
        return Task.CompletedTask;
    }

    [Fact]
    public void GeneratedItems_Should_BeAddedToGlobalStore()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var store = _dataStores.GetGlobal<TestEntity>();
        var items = factory.CreateMany(20).ToList();

        // Act
        store.AddRange(items);

        // Assert
        Assert.Equal(20, store.Items.Count);
    }

    [Fact]
    public async Task GeneratedItems_Should_BePersisted()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 99);
        var store = _dataStores.GetGlobal<TestEntity>();
        var items = factory.CreateMany(10).ToList();
        store.AddRange(items);

        // Act
        await Task.Delay(300); // Auto-Save

        // Assert
        Assert.True(new FileInfo(_testDbPath).Length > 0);
    }

    [Fact]
    public void LocalStore_Should_SupportGeneratedItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 123);

        // Act
        var localStore = new DataStoreBuilder<TestEntity>()
            .WithGeneratedItems(factory, count: 15)
            .Build();

        // Assert
        Assert.Equal(15, localStore.Items.Count);
    }

    [Fact]
    public void Snapshot_Should_WorkWithGeneratedItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 42);
        var globalStore = _dataStores.GetGlobal<TestEntity>();
        var items = factory.CreateMany(30).ToList();
        globalStore.AddRange(items);

        // Act
        var snapshot = _dataStores.CreateLocalSnapshotFromGlobal<TestEntity>();

        // Assert
        Assert.Equal(30, snapshot.Items.Count);
    }

    [Fact]
    public void Snapshot_WithFilter_Should_WorkWithGeneratedItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(
            seed: 42,
            setupAction: filler =>
            {
                filler.Setup().OnProperty(x => x.Age).Use(() => Random.Shared.Next(18, 80));
            });
        
        var globalStore = _dataStores.GetGlobal<TestEntity>();
        var items = factory.CreateMany(50).ToList();
        globalStore.AddRange(items);

        // Act - CreateLocalSnapshotFromGlobal mit Filter
        var snapshot = _dataStores.CreateLocalSnapshotFromGlobal<TestEntity>(x => x.Age >= 50);

        // Assert
        Assert.True(snapshot.Items.Count < 50);
    }

    [Fact]
    public void BulkOperations_Should_HandleGeneratedItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 999);
        var store = _dataStores.GetGlobal<TestEntity>();
        var items = factory.CreateMany(100).ToList();

        // Act
        store.AddRange(items);
        var first50 = items.Take(50).ToList();
        foreach (var item in first50)
        {
            store.Remove(item);
        }

        // Assert
        Assert.Equal(50, store.Items.Count);
    }

    [Fact]
    public void QueryOperations_Should_WorkWithGeneratedItems()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<TestEntity>(
            seed: 42,
            setupAction: filler =>
            {
                filler.Setup().OnProperty(x => x.Age).Use(() => Random.Shared.Next(18, 80));
            });
        
        var store = _dataStores.GetGlobal<TestEntity>();
        var items = factory.CreateMany(200).ToList();
        store.AddRange(items);

        // Act
        var filteredItems = store.Items.Where(x => x.Age > 30).ToList();

        // Assert
        Assert.True(filteredItems.Count > 0);
    }

    // Helper-Klasse für Registrar
    private class TestDataRegistrar : IDataStoreRegistrar
    {
        private readonly string _dbPath;

        public TestDataRegistrar(string dbPath)
        {
            _dbPath = dbPath;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            // Manuelle Registrierung mit LiteDbPersistenceStrategy
            var strategy = new LiteDbPersistenceStrategy<TestEntity>(_dbPath, "testentities");
            var innerStore = new InMemoryDataStore<TestEntity>();
            var persistentStore = new PersistentStoreDecorator<TestEntity>(
                innerStore,
                strategy,
                autoLoad: true,
                autoSaveOnChange: true);

            registry.RegisterGlobal(persistentStore);
        }
    }
}
