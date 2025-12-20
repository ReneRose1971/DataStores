using DataStores.Persistence;
using System.Text.Json;
using TestHelper.DataStores.Fixtures;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Explizite Integration-Tests zur Verifikation der physischen Dateisystem-Operationen
/// der JsonFilePersistenceStrategy.
/// </summary>
public class JsonPersistence_PhysicalFile_IntegrationTests : IClassFixture<JsonPersistenceTempFixture>
{
    private readonly string _testRoot;

    public JsonPersistence_PhysicalFile_IntegrationTests(JsonPersistenceTempFixture fixture)
    {
        _testRoot = fixture.TestRoot;
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

        // Assert
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateNonEmptyFile()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "test2.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto> { new("Item1", 10) };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert
        Assert.True(new FileInfo(filePath).Length > 0);
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateValidJsonContent()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "valid.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto> { new("TestItem", 42) };

        // Act
        await strategy.SaveAllAsync(items);

        var json = await File.ReadAllTextAsync(filePath);
        var deserialized = JsonSerializer.Deserialize<List<TestDto>>(json);

        // Assert
        Assert.NotNull(deserialized);
    }

    [Fact]
    public async Task SaveAllAsync_Should_PreserveData()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "preserve.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto> { new("TestItem", 42) };

        // Act
        await strategy.SaveAllAsync(items);

        var json = await File.ReadAllTextAsync(filePath);
        var deserialized = JsonSerializer.Deserialize<List<TestDto>>(json);

        // Assert
        Assert.Equal("TestItem", deserialized![0].Name);
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(_testRoot, "nested", "deep", "folder");
        var filePath = Path.Combine(nestedPath, "test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto> { new("Test", 1) };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert
        Assert.True(Directory.Exists(nestedPath));
    }

    [Fact]
    public async Task SaveAllAsync_Should_CreateFileInNestedDirectory()
    {
        // Arrange
        var nestedPath = Path.Combine(_testRoot, "nested2", "deep", "folder");
        var filePath = Path.Combine(nestedPath, "test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var items = new List<TestDto> { new("Test", 1) };

        // Act
        await strategy.SaveAllAsync(items);

        // Assert
        Assert.True(File.Exists(filePath));
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

        var json = JsonSerializer.Serialize(originalItems);
        await File.WriteAllTextAsync(filePath, json);

        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);

        // Act
        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(2, loadedItems.Count);
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

        // Act
        await strategy.SaveAllAsync(originalItems);
        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(3, loadedItems.Count);
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

        // Act
        await strategy.SaveAllAsync(firstItems);
        await strategy.SaveAllAsync(secondItems);

        var loadedItems = await strategy.LoadAllAsync();

        // Assert
        Assert.Equal(2, loadedItems.Count);
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

        var json = await File.ReadAllTextAsync(filePath);

        // Assert
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

        // Assert
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

        // Assert
        Assert.True(File.Exists(file1));
        Assert.True(File.Exists(file2));
    }
}

/// <summary>
/// Fixture für JSON Persistence Tests.
/// </summary>
public sealed class JsonPersistenceTempFixture : TempDirectoryFixture
{
    public JsonPersistenceTempFixture() : base("JsonPersistence")
    {
    }
}
