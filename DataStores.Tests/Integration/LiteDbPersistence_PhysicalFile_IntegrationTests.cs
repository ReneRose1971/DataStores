using DataStores.Persistence;
using TestHelper.DataStores.Fixtures;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Explizite Integration-Tests zur Verifikation der physischen Dateisystem-Operationen
/// der LiteDbPersistenceStrategy.
/// </summary>
public class LiteDbPersistence_PhysicalFile_IntegrationTests : IClassFixture<LiteDbPersistenceTempFixture>
{
    private readonly string _testRoot;

    public LiteDbPersistence_PhysicalFile_IntegrationTests(LiteDbPersistenceTempFixture fixture)
    {
        _testRoot = fixture.TestRoot;
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreatePhysicalDbFile()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(SaveAllAsync_Should_CreatePhysicalDbFile)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var items = new List<TestEntity>
        {
            new() { Id = 0, Name = "Item1" },
            new() { Id = 0, Name = "Item2" }
        };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert
        Assert.True(File.Exists(dbPath));
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateNonEmptyFile()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(SaveAllAsync_Should_CreateNonEmptyFile)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var items = new List<TestEntity>
        {
            new() { Id = 0, Name = "Item1" }
        };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert
        Assert.True(new FileInfo(dbPath).Length > 0);
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(_testRoot, nameof(SaveAllAsync_Should_CreateDirectoryIfNotExists), "deep", "folder");
        var dbPath = Path.Combine(nestedPath, "test.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var items = new List<TestEntity> { new() { Id = 0, Name = "Test" } };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert
        Assert.True(Directory.Exists(nestedPath));
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateFileInNestedDirectory()
    {
        // Arrange
        var nestedPath = Path.Combine(_testRoot, nameof(SaveAllAsync_Should_CreateFileInNestedDirectory), "deep", "folder");
        var dbPath = Path.Combine(nestedPath, "test.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var items = new List<TestEntity> { new() { Id = 0, Name = "Test" } };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert
        Assert.True(File.Exists(dbPath));
    }

    [Fact]
    public async Task LoadAllAsync_Should_ReadFromPhysicalDbFile()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LoadAllAsync_Should_ReadFromPhysicalDbFile)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        var originalItems = new List<TestEntity>
        {
            new() { Id = 0, Name = "LoadTest1" },
            new() { Id = 0, Name = "LoadTest2" }
        };

        await strategy.SaveAllAsync(originalItems);
        var loadStrategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        // Act
        var loadedItems = await loadStrategy.LoadAllAsync();

        // Assert
        Assert.Equal(2, loadedItems.Count);
    }

    [Fact]
    public async Task LoadAllAsync_Should_AssignIdsToLoadedItems()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LoadAllAsync_Should_AssignIdsToLoadedItems)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var originalItems = new List<TestEntity>
        {
            new() { Id = 0, Name = "Item1" },
            new() { Id = 0, Name = "Item2" }
        };
        await strategy.SaveAllAsync(originalItems);

        var loadStrategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        // Act
        var loadedItems = await loadStrategy.LoadAllAsync();

        // Assert
        Assert.All(loadedItems, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task LoadAllAsync_Should_ReturnEmpty_WhenFileDoesNotExist()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LoadAllAsync_Should_ReturnEmpty_WhenFileDoesNotExist)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        // Act
        var items = await strategy.LoadAllAsync();

        // Assert
        Assert.Empty(items);
    }

    [Fact]
    public async Task SaveThenLoad_Should_PersistAllData()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(SaveThenLoad_Should_PersistAllData)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var originalItems = new List<TestEntity>
        {
            new() { Id = 0, Name = "Alpha" },
            new() { Id = 0, Name = "Beta" },
            new() { Id = 0, Name = "Gamma" }
        };

        // Act
        await strategy.SaveAllAsync(originalItems);
        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(3, loadedItems.Count);
    }

    [Fact]
    public async Task SaveAllAsync_Should_OverwriteExistingData()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(SaveAllAsync_Should_OverwriteExistingData)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        var firstItems = new List<TestEntity> { new() { Id = 0, Name = "First" } };
        var secondItems = new List<TestEntity>
        {
            new() { Id = 0, Name = "Second" },
            new() { Id = 0, Name = "Third" }
        };

        // Act
        await strategy.SaveAllAsync(firstItems);
        await strategy.SaveAllAsync(secondItems);

        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(2, loadedItems.Count);
    }

    [Fact]
    public async Task SaveAllAsync_Should_NotContainOverwrittenData()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(SaveAllAsync_Should_NotContainOverwrittenData)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        var firstItems = new List<TestEntity> { new() { Id = 0, Name = "First" } };
        var secondItems = new List<TestEntity> { new() { Id = 0, Name = "Second" } };

        // Act
        await strategy.SaveAllAsync(firstItems);
        await strategy.SaveAllAsync(secondItems);

        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.DoesNotContain(loadedItems, i => i.Name == "First");
    }

    [Fact]
    public async Task SaveAllAsync_EmptyList_Should_CreateEmptyCollection()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(SaveAllAsync_EmptyList_Should_CreateEmptyCollection)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var emptyList = new List<TestEntity>();

        // Act
        await strategy.SaveAllAsync(emptyList);
        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Empty(loadedItems);
    }

    [Fact]
    public async Task MultipleCollections_SameDatabase_Should_BeIndependent()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(MultipleCollections_SameDatabase_Should_BeIndependent)}.db");

        var strategy1 = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "collection1");
        var strategy2 = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "collection2");

        var items1 = new List<TestEntity> { new() { Id = 0, Name = "Collection1" } };
        var items2 = new List<TestEntity> { new() { Id = 0, Name = "Collection2" } };

        // Act
        await strategy1.SaveAllAsync(items1);
        await strategy2.SaveAllAsync(items2);

        var loaded1 = await strategy1.LoadAllAsync();
        var loaded2 = await strategy2.LoadAllAsync();

        // Assert
        Assert.Equal("Collection1", loaded1[0].Name);
        Assert.Equal("Collection2", loaded2[0].Name);
    }

    [Fact]
    public async Task SaveAllAsync_LargeDataset_Should_Persist()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(SaveAllAsync_LargeDataset_Should_Persist)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        var largeDataset = Enumerable.Range(1, 10000)
            .Select(i => new TestEntity { Id = 0, Name = $"Item{i}" })
            .ToList();

        // Act
        await strategy.SaveAllAsync(largeDataset);
        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(10000, loadedItems.Count);
    }

    [Fact]
    public async Task DefaultCollectionName_Should_UseTypeName()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(DefaultCollectionName_Should_UseTypeName)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath);
        var items = new List<TestEntity> { new() { Id = 0, Name = "Test" } };

        // Act
        await strategy.SaveAllAsync(items);

        var explicitStrategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, nameof(TestEntity));
        var explicitLoaded = await explicitStrategy.LoadAllAsync();

        // Assert
        Assert.Single(explicitLoaded);
    }

    [Fact]
    public async Task ConcurrentAccess_Should_NotThrow()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(ConcurrentAccess_Should_NotThrow)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        // Act
        var tasks = Enumerable.Range(1, 10)
            .Select(async i =>
            {
                var items = new List<TestEntity> { new() { Id = 0, Name = $"Concurrent{i}" } };
                await strategy.SaveAllAsync(items);
            })
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.True(File.Exists(dbPath));
    }
}

/// <summary>
/// Fixture für LiteDB Persistence Tests.
/// </summary>
public sealed class LiteDbPersistenceTempFixture : TempDirectoryFixture
{
    public LiteDbPersistenceTempFixture() : base("LiteDbPersistence")
    {
    }
}
