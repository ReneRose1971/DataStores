using DataStores.Persistence;
using System.Text.Json;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Explizite Integration-Tests zur Verifikation der physischen Dateisystem-Operationen
/// der JsonFilePersistenceStrategy.
/// </summary>
public class JsonPersistence_PhysicalFile_IntegrationTests : IDisposable
{
    private readonly string _testRoot;

    public JsonPersistence_PhysicalFile_IntegrationTests()
    {
        _testRoot = Path.Combine(
            Path.GetTempPath(),
            "DataStores.Tests",
            "JsonPersistence",
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
    public async Task SaveAllAsync_Should_CreatePhysicalJsonFile()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto>
        {
            new("Item1", 10),
            new("Item2", 20)
        };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert - Physical file must exist
        Assert.True(File.Exists(filePath), "JSON file was not created on disk");
        
        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Length > 0, "JSON file is empty");
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateValidJsonContent()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "valid.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto>
        {
            new("TestItem", 42)
        };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert - File content must be valid JSON
        var json = await File.ReadAllTextAsync(filePath);
        Assert.NotEmpty(json);
        
        // Verify it's deserializable
        var deserialized = JsonSerializer.Deserialize<List<TestDto>>(json);
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
        Assert.Equal("TestItem", deserialized[0].Name);
        Assert.Equal(42, deserialized[0].Age);
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(_testRoot, "nested", "deep", "folder");
        var filePath = Path.Combine(nestedPath, "test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto> { new("Test", 1) };

        // Ensure directory doesn't exist
        if (Directory.Exists(nestedPath))
        {
            Directory.Delete(nestedPath, true);
        }

        // Act
        await strategy.SaveAllAsync(items);

        // Assert - Directory and file must be created
        Assert.True(Directory.Exists(nestedPath), "Nested directory was not created");
        Assert.True(File.Exists(filePath), "File was not created in nested directory");
    }

    [Fact]
    public async Task LoadAllAsync_Should_ReadFromPhysicalFile()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "load.json");
        var originalItems = new List<TestDto>
        {
            new("LoadTest1", 10),
            new("LoadTest2", 20)
        };

        // Manually create JSON file
        var json = JsonSerializer.Serialize(originalItems);
        await File.WriteAllTextAsync(filePath, json);

        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);

        // Act
        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(2, loadedItems.Count);
        Assert.Contains(loadedItems, i => i.Name == "LoadTest1" && i.Age == 10);
        Assert.Contains(loadedItems, i => i.Name == "LoadTest2" && i.Age == 20);
    }

    [Fact]
    public async Task LoadAllAsync_Should_ReturnEmpty_WhenFileDoesNotExist()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "nonexistent.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);

        // Act
        var items = await strategy.LoadAllAsync();

        // Assert
        Assert.Empty(items);
        Assert.False(File.Exists(filePath), "File should not be created by LoadAllAsync");
    }

    [Fact]
    public async Task SaveThenLoad_Should_RoundTrip()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "roundtrip.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var originalItems = new List<TestDto>
        {
            new("Alpha", 100),
            new("Beta", 200),
            new("Gamma", 300)
        };

        // Act - Save
        await strategy.SaveAllAsync(originalItems);

        // Assert - Physical file exists
        Assert.True(File.Exists(filePath));

        // Act - Load
        var loadedItems = await strategy.LoadAllAsync();

        // Assert - Data matches
        Assert.Equal(3, loadedItems.Count);
        Assert.Contains(loadedItems, i => i.Name == "Alpha" && i.Age == 100);
        Assert.Contains(loadedItems, i => i.Name == "Beta" && i.Age == 200);
        Assert.Contains(loadedItems, i => i.Name == "Gamma" && i.Age == 300);
    }

    [Fact]
    public async Task SaveAllAsync_Should_OverwriteExistingFile()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "overwrite.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);

        var firstItems = new List<TestDto> { new("First", 1) };
        var secondItems = new List<TestDto>
        {
            new("Second", 2),
            new("Third", 3)
        };

        // Act - Save first set
        await strategy.SaveAllAsync(firstItems);
        var firstFileSize = new FileInfo(filePath).Length;

        // Act - Save second set (should overwrite)
        await strategy.SaveAllAsync(secondItems);

        // Assert - File updated
        Assert.True(File.Exists(filePath));
        var loadedItems = await strategy.LoadAllAsync();
        Assert.Equal(2, loadedItems.Count);
        Assert.Contains(loadedItems, i => i.Name == "Second");
        Assert.Contains(loadedItems, i => i.Name == "Third");
    }

    [Fact]
    public async Task SaveAllAsync_EmptyList_Should_CreateEmptyJsonArray()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "empty.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var emptyList = new List<TestDto>();

        // Act
        await strategy.SaveAllAsync(emptyList);

        // Assert
        Assert.True(File.Exists(filePath));
        var json = await File.ReadAllTextAsync(filePath);
        Assert.Contains("[]", json);
    }

    [Fact]
    public async Task LoadAllAsync_CorruptedJson_Should_ReturnEmpty()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "corrupted.json");
        await File.WriteAllTextAsync(filePath, "{ INVALID JSON }");

        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);

        // Act
        var items = await strategy.LoadAllAsync();

        // Assert - Should handle error gracefully
        Assert.Empty(items);
    }

    [Fact]
    public async Task MultipleStrategies_SameDirectory_Should_CreateSeparateFiles()
    {
        // Arrange
        var file1 = Path.Combine(_testRoot, "items1.json");
        var file2 = Path.Combine(_testRoot, "items2.json");

        var strategy1 = new JsonFilePersistenceStrategy<TestDto>(file1);
        var strategy2 = new JsonFilePersistenceStrategy<TestDto>(file2);

        var items1 = new List<TestDto> { new("File1", 1) };
        var items2 = new List<TestDto> { new("File2", 2) };

        // Act
        await strategy1.SaveAllAsync(items1);
        await strategy2.SaveAllAsync(items2);

        // Assert - Both files exist independently
        Assert.True(File.Exists(file1));
        Assert.True(File.Exists(file2));

        var loaded1 = await strategy1.LoadAllAsync();
        var loaded2 = await strategy2.LoadAllAsync();

        Assert.Equal("File1", loaded1[0].Name);
        Assert.Equal("File2", loaded2[0].Name);
    }
}
