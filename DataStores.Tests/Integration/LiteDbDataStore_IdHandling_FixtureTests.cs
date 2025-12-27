using DataStores.Abstractions;
using DataStores.Persistence;
using DataStores.Runtime;
using TestHelper.DataStores.Models;
using TestHelper.DataStores.TestSetup;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Integration-Tests für LiteDB ID-Handling und ID-Writeback.
/// Verwendet Custom-Fixture ohne vollständiges Bootstrap für fokussierte ID-Tests.
/// </summary>
public class LiteDbDataStore_IdHandling_IntegrationTests : IClassFixture<LiteDbDataStore_IdHandling_IntegrationTests.IdHandlingFixture>
{
    private readonly IdHandlingFixture _fixture;
    private readonly IDataStoreDiffService _diffService = TestDiffServiceFactory.Create();

    public LiteDbDataStore_IdHandling_IntegrationTests(IdHandlingFixture fixture)
    {
        _fixture = fixture;
    }

    // ====================================================================
    // ID Assignment Tests
    // ====================================================================

    [Fact]
    public async Task NewEntities_Should_GetIdFromLiteDb_AfterPersistence()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        
        var entity1 = new TestEntity { Id = 0, Name = "Laptop", Amount = 1299.99m };
        var entity2 = new TestEntity { Id = 0, Name = "Mouse", Amount = 29.99m };

        // Act
        store.Add(entity1);
        store.AddRange(new[] { entity2 });
        
        await Task.Delay(200); // Wait for auto-save

        // Assert - IDs wurden von LiteDB vergeben
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "entities", _diffService);
        var savedEntities = await strategy.LoadAllAsync();

        Assert.Equal(2, savedEntities.Count);
        Assert.All(savedEntities, e => Assert.True(e.Id > 0, $"Entity {e.Name} should have Id > 0"));
    }

    [Fact]
    public async Task SavedEntities_Should_HaveUniqueIds()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();
        
        store.AddRange(new[]
        {
            new TestEntity { Id = 0, Name = "E1", Amount = 100m },
            new TestEntity { Id = 0, Name = "E2", Amount = 200m },
            new TestEntity { Id = 0, Name = "E3", Amount = 300m }
        });

        // Act
        await Task.Delay(200);
        
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "entities", _diffService);
        var savedEntities = await strategy.LoadAllAsync();
        var ids = savedEntities.Select(e => e.Id).ToList();

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task EntitiesWithNonZeroId_NotInDatabase_Should_AlsoBeInserted()
    {
        // Arrange
        var store = _fixture.CreateFreshStore();

        var newEntity = new TestEntity { Id = 0, Name = "New Entity", Amount = 100m };
        var fakeExisting = new TestEntity { Id = 999, Name = "Fake Existing", Amount = 200m };

        // Act
        store.Add(newEntity);
        store.Add(fakeExisting);

        Assert.Equal(2, store.Items.Count);

        await Task.Delay(200);

        // Assert - BEIDE werden gespeichert, weil beide nicht in DB sind
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "entities", _diffService);
        var savedEntities = await strategy.LoadAllAsync();

        Assert.Equal(2, savedEntities.Count);
        Assert.Contains(savedEntities, e => e.Name == "New Entity");
        Assert.Contains(savedEntities, e => e.Name == "Fake Existing");
    }

    [Fact]
    public void EntityBase_Should_ImplementEquals_ById()
    {
        // Arrange
        var entity1 = new TestEntity { Id = 1, Name = "Original" };
        var entity2 = new TestEntity { Id = 1, Name = "Different" };
        var entity3 = new TestEntity { Id = 2, Name = "Original" };
        var newEntity = new TestEntity { Id = 0, Name = "New" };

        // Act & Assert - Equals basiert auf ID für Id > 0
        Assert.True(entity1.Equals(entity2)); // Gleiche ID
        Assert.False(entity1.Equals(entity3)); // Verschiedene ID
        Assert.False(entity1.Equals(newEntity)); // Eine ist neu (Id=0)
    }

    [Fact]
    public void EntityBase_Should_ImplementGetHashCode_Correctly()
    {
        // Arrange
        var entity1 = new TestEntity { Id = 1, Name = "A" };
        var entity2 = new TestEntity { Id = 1, Name = "B" };
        var entity3 = new TestEntity { Id = 2, Name = "A" };

        // Act & Assert
        Assert.Equal(entity1.GetHashCode(), entity2.GetHashCode());
        Assert.NotEqual(entity1.GetHashCode(), entity3.GetHashCode());
    }

    [Fact]
    public void EntityBase_ToString_Should_ContainIdAndName()
    {
        // Arrange
        var entity = new TestEntity { Id = 42, Name = "TestEntity", Amount = 100m };

        // Act
        var str = entity.ToString();

        // Assert
        Assert.Contains("42", str);
        Assert.Contains("TestEntity", str);
    }

    [Fact]
    public async Task AfterLoadFromLiteDb_AllEntities_Should_HavePositiveIds()
    {
        // Arrange - Explizites Save
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "entities", _diffService);

        var entities = new[]
        {
            new TestEntity { Id = 0, Name = "A", Amount = 10m },
            new TestEntity { Id = 0, Name = "B", Amount = 20m },
            new TestEntity { Id = 0, Name = "C", Amount = 30m }
        };

        // Act - Save schreibt IDs zurück
        await strategy.SaveAllAsync(entities);
        
        // Assert - IDs wurden zurückgeschrieben
        Assert.All(entities, e => Assert.True(e.Id > 0));
        
        // Verify in DB
        var savedEntities = await strategy.LoadAllAsync();
        Assert.Equal(3, savedEntities.Count);
        Assert.All(savedEntities, e => Assert.True(e.Id > 0));
    }

    [Fact]
    public async Task SaveWithMixedIds_Should_SaveAllNewEntities()
    {
        // Arrange
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "entities", _diffService);

        var entities = new []
        {
            new TestEntity { Id = 0, Name = "New 1", Amount = 10m },
            new TestEntity { Id = 99, Name = "Fake Existing", Amount = 20m },
            new TestEntity { Id = 0, Name = "New 2", Amount = 30m }
        };

        // Act
        await strategy.SaveAllAsync(entities);

        // Assert - ALLE 3 werden gespeichert (keine in DB)
        var loaded = await strategy.LoadAllAsync();
        Assert.Equal(3, loaded.Count);
        Assert.All(loaded, e => Assert.True(e.Id > 0));
        Assert.Contains(loaded, e => e.Name == "New 1");
        Assert.Contains(loaded, e => e.Name == "New 2");
        Assert.Contains(loaded, e => e.Name == "Fake Existing");
    }

    [Fact]
    public async Task IdWriteback_Should_HappenImmediately_AfterSave()
    {
        // Arrange
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "entities", _diffService);
        var entity = new TestEntity { Id = 0, Name = "Test", Amount = 100m };
        
        Assert.Equal(0, entity.Id); // Precondition

        // Act
        await strategy.SaveAllAsync(new[] { entity });

        // Assert - ID wurde sofort zurückgeschrieben
        Assert.True(entity.Id > 0, "ID should be written back immediately after save");
    }

    [Fact]
    public async Task ReloadedEntities_Should_PreserveIds()
    {
        // Arrange
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(_fixture.DbPath, "entities", _diffService);
        var originalEntity = new TestEntity { Id = 0, Name = "Original", Amount = 100m };
        
        await strategy.SaveAllAsync(new[] { originalEntity });
        var assignedId = originalEntity.Id;

        // Act - Reload
        var reloadedEntities = await strategy.LoadAllAsync();
        var reloadedEntity = reloadedEntities.Single();

        // Assert
        Assert.Equal(assignedId, reloadedEntity.Id);
        Assert.Equal("Original", reloadedEntity.Name);
    }

    // ====================================================================
    // Fixture Class
    // ====================================================================

    /// <summary>
    /// Lightweight Fixture für ID-Handling-Tests.
    /// Kein vollständiges Bootstrap - nur Store + Persistence.
    /// </summary>
    public class IdHandlingFixture : IDisposable
    {
        public string DbPath { get; }
        private readonly IDataStoreDiffService _diffService = TestDiffServiceFactory.Create();

        public IdHandlingFixture()
        {
            DbPath = Path.Combine(Path.GetTempPath(), $"LiteDbIdHandling_{Guid.NewGuid()}.db");
        }

        public IDataStore<TestEntity> CreateFreshStore()
        {
            var strategy = new LiteDbPersistenceStrategy<TestEntity>(DbPath, "entities", _diffService);
            var innerStore = new InMemoryDataStore<TestEntity>();
            var persistentStore = new PersistentStoreDecorator<TestEntity>(
                innerStore,
                strategy,
                autoLoad: false,
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
