using DataStores.Abstractions;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.Fixtures;
using TestHelper.DataStores.Models;
using TestHelper.DataStores.TestSetup;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Konsolidierte LiteDB-Integration-Tests mit Shared Fixture.
/// Verwendet IClassFixture für gemeinsame Fixture-Instanz über alle Tests.
/// </summary>
public class LiteDbDataStore_Fixture_IntegrationTests : IClassFixture<LiteDbDataStore_Fixture_IntegrationTests.LiteDbTestFixture>
{
    private readonly LiteDbTestFixture _fixture;
    private readonly IDataStoreDiffService _diffService = TestDiffServiceFactory.Create();

    public LiteDbDataStore_Fixture_IntegrationTests(LiteDbTestFixture fixture)
    {
        _fixture = fixture;
    }

    // ====================================================================
    // CRUD Operations Tests
    // ====================================================================

    [Fact]
    public void Bootstrap_Should_CreateEmptyStore()
    {
        // Jeder Test holt sich eine frische Store-Instanz
        var store = _fixture.CreateFreshStore();
        
        Assert.Empty(store.Items);
    }

    [Fact]
    public void Add_Should_AddSingleEntity()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entity = CreateTestEntity("Test Entity", 25);

        // Act
        store.Add(entity);

        // Assert
        Assert.Single(store.Items);
    }

    [Fact]
    public void Add_Should_AssignLiteDbId()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entity = CreateTestEntity("Test", 30);

        // Act
        store.Add(entity);

        // Assert
        Assert.True(entity.Id > 0, "LiteDB should assign Id > 0");
    }

    [Fact]
    public void AddRange_Should_AddMultipleEntities()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entities = new[]
        {
            CreateTestEntity("Entity1", 20),
            CreateTestEntity("Entity2", 30)
        };

        // Act
        store.AddRange(entities);

        // Assert
        Assert.Equal(2, store.Items.Count);
    }

    [Fact]
    public void AddRange_Should_AssignIdsToAllEntities()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entities = new[]
        {
            CreateTestEntity("Entity1", 20),
            CreateTestEntity("Entity2", 30),
            CreateTestEntity("Entity3", 40)
        };

        // Act
        store.AddRange(entities);

        // Assert
        Assert.All(store.Items, e => Assert.True(e.Id > 0));
    }

    [Fact]
    public void Remove_Should_DecreaseItemCount()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entity1 = CreateTestEntity("Entity1", 20);
        var entity2 = CreateTestEntity("Entity2", 30);
        store.AddRange(new[] { entity1, entity2 });

        // Act
        store.Remove(entity1);

        // Assert
        Assert.Single(store.Items);
    }

    [Fact]
    public void Clear_Should_RemoveAllItems()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        store.AddRange(new[]
        {
            CreateTestEntity("E1", 20),
            CreateTestEntity("E2", 30)
        });

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.Items);
    }

    // ====================================================================
    // LINQ Operations Tests
    // ====================================================================

    [Fact]
    public void Items_Should_SupportLinqFiltering()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        store.AddRange(new[]
        {
            CreateTestEntity("Young", 20, status: TestEntityStatus.Pending),
            CreateTestEntity("Middle", 30, status: TestEntityStatus.Processing),
            CreateTestEntity("Old", 40, status: TestEntityStatus.Pending)
        });

        // Act
        var pendingItems = store.Items.Where(e => e.Status == TestEntityStatus.Pending).ToList();

        // Assert
        Assert.Equal(2, pendingItems.Count);
    }

    [Fact]
    public void Items_Should_SupportLinqGrouping()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        store.AddRange(new[]
        {
            CreateTestEntity("E1", 20, status: TestEntityStatus.Pending),
            CreateTestEntity("E2", 30, status: TestEntityStatus.Processing),
            CreateTestEntity("E3", 40, status: TestEntityStatus.Pending)
        });

        // Act
        var grouped = store.Items
            .GroupBy(e => e.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        Assert.Equal(2, grouped[TestEntityStatus.Pending]);
    }

    [Fact]
    public void Items_Should_SupportLinqAggregation()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var e1 = CreateTestEntity("E1", 20);
        e1.Amount = 100m;
        var e2 = CreateTestEntity("E2", 30);
        e2.Amount = 200m;
        var e3 = CreateTestEntity("E3", 40);
        e3.Amount = 300m;
        
        store.AddRange(new[] { e1, e2, e3 });

        // Act
        var totalAmount = store.Items.Sum(e => e.Amount);

        // Assert
        Assert.Equal(600m, totalAmount);
    }

    // ====================================================================
    // Event Tests
    // ====================================================================

    [Fact]
    public void Changed_Event_Should_FireOnAdd()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var eventFired = false;
        store.Changed += (sender, args) => eventFired = true;
        var entity = CreateTestEntity("Test", 25);

        // Act
        store.Add(entity);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Changed_Event_Should_ReportCorrectChangeType_OnAdd()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        DataStoreChangeType? capturedChangeType = null;
        store.Changed += (sender, args) => capturedChangeType = args.ChangeType;
        var entity = CreateTestEntity("Test", 25);

        // Act
        store.Add(entity);

        // Assert
        Assert.Equal(DataStoreChangeType.Add, capturedChangeType);
    }

    [Fact]
    public void Changed_Event_Should_FireOnRemove()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entity = CreateTestEntity("Test", 25);
        store.Add(entity);
        
        var eventFired = false;
        store.Changed += (sender, args) => eventFired = true;

        // Act
        store.Remove(entity);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Changed_Event_Should_FireOnClear()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        store.Add(CreateTestEntity("Test", 25));
        
        var eventFired = false;
        store.Changed += (sender, args) => eventFired = true;

        // Act
        store.Clear();

        // Assert
        Assert.True(eventFired);
    }

    // ====================================================================
    // Persistence Tests (mit shared DB-Pfad)
    // ====================================================================

    [Fact]
    public async Task Persistence_Should_CreatePhysicalDbFile()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entity = CreateTestEntity("Test", 25);
        store.Add(entity);

        // Act
        await Task.Delay(200); // Wait for auto-save

        // Assert
        Assert.True(File.Exists(_fixture.DbPath));
    }

    [Fact]
    public async Task Persistence_Should_CreateNonEmptyDbFile()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entity = CreateTestEntity("Test", 25);
        store.Add(entity);

        // Act
        await Task.Delay(200);

        // Assert
        Assert.True(new FileInfo(_fixture.DbPath).Length > 0);
    }

    [Fact]
    public async Task Persistence_Should_SaveAddedEntities()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        store.AddRange(new[]
        {
            CreateTestEntity("Entity1", 20),
            CreateTestEntity("Entity2", 30)
        });

        // Act
        await Task.Delay(200);
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "testentities", _diffService);
        var savedEntities = await strategy.LoadAllAsync();

        // Assert
        Assert.True(savedEntities.Count >= 2, $"Expected at least 2 entities, found {savedEntities.Count}");
    }

    [Fact]
    public async Task Persistence_Should_AssignIdsToPersistedEntities()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        store.AddRange(new[]
        {
            CreateTestEntity("E1", 20),
            CreateTestEntity("E2", 30)
        });

        // Act
        await Task.Delay(200);
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "testentities", _diffService);
        var savedEntities = await strategy.LoadAllAsync();

        // Assert
        Assert.All(savedEntities, e => Assert.True(e.Id > 0));
    }

    // ====================================================================
    // ID Handling Tests
    // ====================================================================

    [Fact]
    public void NewEntity_Should_HaveIdZero()
    {
        // Arrange & Act
        var entity = CreateTestEntity("New", 25);

        // Assert
        Assert.Equal(0, entity.Id);
    }

    [Fact]
    public void AddedEntity_Should_ReceiveIdFromLiteDb()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entity = CreateTestEntity("Test", 25);
        Assert.Equal(0, entity.Id); // Precondition

        // Act
        store.Add(entity);

        // Assert
        Assert.True(entity.Id > 0);
    }

    [Fact]
    public void AddRange_Should_AssignUniqueIds()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        var entities = new[]
        {
            CreateTestEntity("E1", 20),
            CreateTestEntity("E2", 30),
            CreateTestEntity("E3", 40)
        };

        // Act
        store.AddRange(entities);
        var ids = store.Items.Select(e => e.Id).ToList();

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // ====================================================================
    // Helper Methods
    // ====================================================================

    private static TestEntity CreateTestEntity(string name, int age, TestEntityStatus status = TestEntityStatus.Pending)
    {
        return new TestEntity
        {
            Id = 0,
            Name = name,
            Age = age,
            Status = status,
            UpdatedUtc = DateTime.UtcNow
        };
    }

    // ====================================================================
    // Fixture Class
    // ====================================================================

    /// <summary>
    /// Shared Fixture für LiteDB-Integration-Tests.
    /// Erstellt EINEN DB-Pfad für alle Tests (parallele Tests schreiben in dieselbe DB).
    /// </summary>
    public class LiteDbTestFixture : IDisposable
    {
        public string DbPath { get; }
        private readonly IDataStoreDiffService _diffService = TestDiffServiceFactory.Create();

        public LiteDbTestFixture()
        {
            DbPath = Path.Combine(Path.GetTempPath(), $"LiteDbFixtureTest_{Guid.NewGuid()}.db");
        }

        /// <summary>
        /// Erstellt einen neuen In-Memory Store mit LiteDB-Persistence (shared DB).
        /// Jeder Test bekommt eine frische Store-Instanz, aber alle teilen sich die DB.
        /// </summary>
        public IDataStore<TestEntity> CreateFreshStore()
        {
            var strategy = new LiteDbPersistenceStrategy<TestEntity>(DbPath, "testentities", _diffService);
            var innerStore = new InMemoryDataStore<TestEntity>();
            var persistentStore = new PersistentStoreDecorator<TestEntity>(
                innerStore,
                strategy,
                autoLoad: false, // Wichtig: autoLoad=false, sonst laden alle Tests die Daten der anderen
                autoSaveOnChange: true);

            return persistentStore;
        }

        public void Dispose()
        {
            if (File.Exists(DbPath))
            {
                try
                {
                    File.Delete(DbPath);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }
}
