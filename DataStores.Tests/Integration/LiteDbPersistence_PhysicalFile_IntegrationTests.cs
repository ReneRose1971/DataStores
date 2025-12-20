using DataStores.Persistence;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Explizite Integration-Tests zur Verifikation der physischen Dateisystem-Operationen
/// der LiteDbPersistenceStrategy.
/// </summary>
public class LiteDbPersistence_PhysicalFile_IntegrationTests : IDisposable
{
    private readonly string _testRoot;

    public LiteDbPersistence_PhysicalFile_IntegrationTests()
    {
        _testRoot = Path.Combine(
            Path.GetTempPath(),
            "DataStores.Tests",
            "LiteDbPersistence",
            Guid.NewGuid().ToString("N"));
        
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            try
            {
                Directory.Delete(_testRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreatePhysicalDbFile()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "test.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var items = new List<TestEntity>
        {
            new() { Id = 0, Name = "Item1" },
            new() { Id = 0, Name = "Item2" }
        };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert - Physical DB file must exist
        Assert.True(File.Exists(dbPath), "LiteDB file was not created on disk");
        
        var fileInfo = new FileInfo(dbPath);
        Assert.True(fileInfo.Length > 0, "LiteDB file is empty");
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(_testRoot, "nested", "deep", "folder");
        var dbPath = Path.Combine(nestedPath, "test.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var items = new List<TestEntity> { new() { Id = 0, Name = "Test" } };

        // Ensure directory doesn't exist
        if (Directory.Exists(nestedPath))
        {
            Directory.Delete(nestedPath, true);
        }

        // Act
        await strategy.SaveAllAsync(items);

        // Assert - Directory and DB file must be created
        Assert.True(Directory.Exists(nestedPath), "Nested directory was not created");
        Assert.True(File.Exists(dbPath), "DB file was not created in nested directory");
    }

    [Fact]
    public async Task LoadAllAsync_Should_ReadFromPhysicalDbFile()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "load.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        var originalItems = new List<TestEntity>
        {
            new() { Id = 0, Name = "LoadTest1" },
            new() { Id = 0, Name = "LoadTest2" }
        };

        // First save to create DB
        await strategy.SaveAllAsync(originalItems);

        // Create new strategy instance to test loading
        var loadStrategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        // Act
        var loadedItems = await loadStrategy.LoadAllAsync();

        // Assert - LiteDB has assigned IDs > 0
        Assert.Equal(2, loadedItems.Count);
        Assert.All(loadedItems, item => Assert.True(item.Id > 0));
        Assert.Contains(loadedItems, i => i.Name == "LoadTest1");
        Assert.Contains(loadedItems, i => i.Name == "LoadTest2");
    }

    [Fact]
    public async Task LoadAllAsync_Should_ReturnEmpty_WhenFileDoesNotExist()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "nonexistent.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        // Act
        var items = await strategy.LoadAllAsync();

        // Assert
        Assert.Empty(items);
    }

    [Fact]
    public async Task SaveThenLoad_Should_RoundTrip()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "roundtrip.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var originalItems = new List<TestEntity>
        {
            new() { Id = 0, Name = "Alpha" },
            new() { Id = 0, Name = "Beta" },
            new() { Id = 0, Name = "Gamma" }
        };

        // Act - Save
        await strategy.SaveAllAsync(originalItems);

        // Assert - Physical file exists
        Assert.True(File.Exists(dbPath));
        Assert.True(new FileInfo(dbPath).Length > 0);

        // Act - Load
        var loadedItems = await strategy.LoadAllAsync();

        // Assert - Data matches, IDs assigned
        Assert.Equal(3, loadedItems.Count);
        Assert.All(loadedItems, item => Assert.True(item.Id > 0));
        Assert.Contains(loadedItems, i => i.Name == "Alpha");
        Assert.Contains(loadedItems, i => i.Name == "Beta");
        Assert.Contains(loadedItems, i => i.Name == "Gamma");
    }

    [Fact]
    public async Task SaveAllAsync_Should_OverwriteExistingData()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "overwrite.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        var firstItems = new List<TestEntity> { new() { Id = 0, Name = "First" } };
        var secondItems = new List<TestEntity>
        {
            new() { Id = 0, Name = "Second" },
            new() { Id = 0, Name = "Third" }
        };

        // Act - Save first set
        await strategy.SaveAllAsync(firstItems);

        // Act - Save second set (should replace)
        await strategy.SaveAllAsync(secondItems);

        // Assert - Only second set exists
        var loadedItems = await strategy.LoadAllAsync();
        Assert.Equal(2, loadedItems.Count);
        Assert.DoesNotContain(loadedItems, i => i.Name == "First");
        Assert.Contains(loadedItems, i => i.Name == "Second");
        Assert.Contains(loadedItems, i => i.Name == "Third");
    }

    [Fact]
    public async Task SaveAllAsync_EmptyList_Should_CreateEmptyCollection()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "empty.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");
        var emptyList = new List<TestEntity>();

        // Act
        await strategy.SaveAllAsync(emptyList);

        // Assert - DB file created but collection empty
        Assert.True(File.Exists(dbPath));
        var loadedItems = await strategy.LoadAllAsync();
        Assert.Empty(loadedItems);
    }

    [Fact]
    public async Task MultipleCollections_SameDatabase_Should_BeIndependent()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "multi-collection.db");

        var strategy1 = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "collection1");
        var strategy2 = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "collection2");

        var items1 = new List<TestEntity> { new() { Id = 0, Name = "Collection1" } };
        var items2 = new List<TestEntity> { new() { Id = 0, Name = "Collection2" } };

        // Act
        await strategy1.SaveAllAsync(items1);
        await strategy2.SaveAllAsync(items2);

        // Assert - Both collections exist independently in same DB
        Assert.True(File.Exists(dbPath));

        var loaded1 = await strategy1.LoadAllAsync();
        var loaded2 = await strategy2.LoadAllAsync();

        Assert.Single(loaded1);
        Assert.Equal("Collection1", loaded1[0].Name);

        Assert.Single(loaded2);
        Assert.Equal("Collection2", loaded2[0].Name);
    }

    [Fact]
    public async Task SaveAllAsync_LargeDataset_Should_Persist()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "large.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        var largeDataset = Enumerable.Range(1, 10000)
            .Select(i => new TestEntity { Id = 0, Name = $"Item{i}" })
            .ToList();

        // Act
        await strategy.SaveAllAsync(largeDataset);

        // Assert - File exists and has substantial size
        Assert.True(File.Exists(dbPath));
        var fileInfo = new FileInfo(dbPath);
        Assert.True(fileInfo.Length > 10000, "DB file should contain substantial data");

        // Verify data integrity
        var loadedItems = await strategy.LoadAllAsync();
        Assert.Equal(10000, loadedItems.Count);
    }

    [Fact]
    public async Task DefaultCollectionName_Should_UseTypeName()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "default-collection.db");
        
        // Strategy without explicit collection name
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath);
        var items = new List<TestEntity> { new() { Id = 0, Name = "Test" } };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert - Should use type name as collection name
        var loadedItems = await strategy.LoadAllAsync();
        Assert.Single(loadedItems);

        // Verify it used the type name by trying to load from explicit collection
        var explicitStrategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, nameof(TestEntity));
        var explicitLoaded = await explicitStrategy.LoadAllAsync();
        Assert.Single(explicitLoaded);
        Assert.Equal("Test", explicitLoaded[0].Name);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, "concurrent.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "items");

        // Act - Concurrent writes
        var tasks = Enumerable.Range(1, 10)
            .Select(async i =>
            {
                var items = new List<TestEntity> { new() { Id = 0, Name = $"Concurrent{i}" } };
                await strategy.SaveAllAsync(items);
            })
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Last write wins, no corruption
        Assert.True(File.Exists(dbPath));
        var loadedItems = await strategy.LoadAllAsync();
        Assert.NotEmpty(loadedItems);
    }
}
