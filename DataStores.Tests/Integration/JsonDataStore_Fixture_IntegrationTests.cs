using DataStores.Abstractions;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TestHelper.DataStores.Fixtures;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Integration;

[Trait("Category", "Integration")]
/// <summary>
/// Konsolidierte JSON-Integration-Tests mit Shared Fixture.
/// Verwendet JsonIntegrationFixture für vollständig initialisierte DataStore-Kontexte.
/// </summary>
/// <remarks>
/// Diese Testklasse vereint alle JSON-basierten Integration-Tests und nutzt
/// eine gemeinsame Fixture für DI-Setup, Bootstrap und Persistence.
/// Tests sind nach One-Assert-Rule organisiert.
/// </remarks>
public class JsonDataStore_Fixture_IntegrationTests : IAsyncLifetime
{
    private readonly JsonIntegrationFixture _fixture;
    private IDataStore<TestDto> _store = null!;
    private string _jsonFilePath = "";

    public JsonDataStore_Fixture_IntegrationTests()
    {
        _fixture = new JsonIntegrationFixture();
    }

    public async Task InitializeAsync()
    {
        var testDataPath = Path.Combine(Path.GetTempPath(), $"JsonFixtureTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDataPath);
        _jsonFilePath = Path.Combine(testDataPath, "testdata.json");
        
        var registrar = new JsonTestDtoRegistrar(_jsonFilePath);
        
        await _fixture.InitializeAsync(registrar);
        _store = _fixture.DataStores.GetGlobal<TestDto>();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    // ====================================================================
    // CRUD Operations Tests
    // ====================================================================

    [Fact]
    public void Bootstrap_Should_CreateEmptyStore()
    {
        Assert.Empty(_store.Items);
    }

    [Fact]
    public void Add_Should_AddSingleDto()
    {
        // Arrange
        var dto = CreateTestDto("Max Mustermann", 35);

        // Act
        _store.Add(dto);

        // Assert
        Assert.Single(_store.Items);
    }

    [Fact]
    public void AddRange_Should_AddMultipleDtos()
    {
        // Arrange
        var dtos = new[]
        {
            CreateTestDto("Max", 35),
            CreateTestDto("Anna", 28)
        };

        // Act
        _store.AddRange(dtos);

        // Assert
        Assert.Equal(2, _store.Items.Count);
    }

    [Fact]
    public void Remove_Should_DecreaseItemCount()
    {
        // Arrange
        var dto1 = CreateTestDto("Max", 35);
        var dto2 = CreateTestDto("Anna", 28);
        _store.AddRange(new[] { dto1, dto2 });

        // Act
        _store.Remove(dto1);

        // Assert
        Assert.Single(_store.Items);
    }

    [Fact]
    public void Clear_Should_RemoveAllItems()
    {
        // Arrange
        _store.AddRange(new[]
        {
            CreateTestDto("Max", 35),
            CreateTestDto("Anna", 28)
        });

        // Act
        _store.Clear();

        // Assert
        Assert.Empty(_store.Items);
    }

    // ====================================================================
    // LINQ Operations Tests
    // ====================================================================

    [Fact]
    public void Items_Should_SupportLinqFiltering()
    {
        // Arrange
        _store.AddRange(new[]
        {
            CreateTestDto("Max", 35, isActive: true),
            CreateTestDto("Anna", 28, isActive: true),
            CreateTestDto("Peter", 42, isActive: false)
        });

        // Act
        var activeItems = _store.Items.Where(d => d.IsActive).ToList();

        // Assert
        Assert.Equal(2, activeItems.Count);
    }

    [Fact]
    public void Items_Should_SupportLinqGrouping()
    {
        // Arrange
        _store.AddRange(new[]
        {
            CreateTestDto("Young1", 20, isActive: true),
            CreateTestDto("Young2", 25, isActive: true),
            CreateTestDto("Old", 60, isActive: false)
        });

        // Act
        var grouped = _store.Items
            .GroupBy(d => d.IsActive)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        Assert.Equal(2, grouped[true]);
    }

    [Fact]
    public void Items_Should_SupportLinqAggregation()
    {
        // Arrange
        _store.AddRange(new[]
        {
            CreateTestDto("Person1", 20, score: 100m),
            CreateTestDto("Person2", 30, score: 200m),
            CreateTestDto("Person3", 40, score: 300m)
        });

        // Act
        var totalScore = _store.Items.Sum(d => d.Score);

        // Assert
        Assert.Equal(600m, totalScore);
    }

    // ====================================================================
    // Event Tests
    // ====================================================================

    [Fact]
    public void Changed_Event_Should_FireOnAdd()
    {
        // Arrange
        var eventFired = false;
        _store.Changed += (sender, args) => eventFired = true;
        var dto = CreateTestDto("Max", 35);

        // Act
        _store.Add(dto);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Changed_Event_Should_ReportCorrectChangeType_OnAdd()
    {
        // Arrange
        DataStoreChangeType? capturedChangeType = null;
        _store.Changed += (sender, args) => capturedChangeType = args.ChangeType;
        var dto = CreateTestDto("Max", 35);

        // Act
        _store.Add(dto);

        // Assert
        Assert.Equal(DataStoreChangeType.Add, capturedChangeType);
    }

    [Fact]
    public void Changed_Event_Should_FireOnRemove()
    {
        // Arrange
        var dto = CreateTestDto("Max", 35);
        _store.Add(dto);
        
        DataStoreChangeType? capturedChangeType = null;
        _store.Changed += (sender, args) => capturedChangeType = args.ChangeType;

        // Act
        _store.Remove(dto);

        // Assert
        Assert.Equal(DataStoreChangeType.Remove, capturedChangeType);
    }

    [Fact]
    public void Changed_Event_Should_FireOnClear()
    {
        // Arrange
        _store.Add(CreateTestDto("Max", 35));
        
        var eventFired = false;
        _store.Changed += (sender, args) => eventFired = true;

        // Act
        _store.Clear();

        // Assert
        Assert.True(eventFired);
    }

    // ====================================================================
    // Persistence Tests
    // ====================================================================

    [Fact]
    public async Task Persistence_Should_CreateJsonFile()
    {
        // Arrange
        var dto = CreateTestDto("Max", 35);
        _store.Add(dto);

        // Act
        await Task.Delay(200); // Wait for auto-save

        // Assert
        Assert.True(File.Exists(_jsonFilePath));
    }

    [Fact]
    public async Task Persistence_Should_CreateNonEmptyFile()
    {
        // Arrange
        var dto = CreateTestDto("Max", 35);
        _store.Add(dto);

        // Act
        await Task.Delay(200);

        // Assert
        Assert.True(new FileInfo(_jsonFilePath).Length > 0);
    }

    [Fact]
    public async Task Persistence_Should_SaveAddedDtos()
    {
        // Arrange
        _store.AddRange(new[]
        {
            CreateTestDto("Max", 35),
            CreateTestDto("Anna", 28),
            CreateTestDto("Lisa", 31)
        });

        // Act
        await Task.Delay(200);
        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var deserializedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);

        // Assert
        Assert.Equal(3, deserializedDtos?.Count);
    }

    [Fact]
    public async Task Persistence_Should_ContainCorrectData()
    {
        // Arrange
        var dto = CreateTestDto("Max Mustermann", 35);
        dto.Notes = "max@example.com";
        _store.Add(dto);

        // Act
        await Task.Delay(200);
        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var deserializedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);

        // Assert
        Assert.Contains(deserializedDtos!, d => d.Notes == "max@example.com");
    }

    [Fact]
    public async Task Persistence_Should_NotContainRemovedDtos()
    {
        // Arrange
        var dto1 = CreateTestDto("Max", 35);
        dto1.Notes = "max@example.com";
        var dto2 = CreateTestDto("Peter", 42);
        dto2.Notes = "peter@example.com";
        
        _store.AddRange(new[] { dto1, dto2 });
        await Task.Delay(200);

        // Act
        _store.Remove(dto2);
        await Task.Delay(200);

        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var deserializedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);

        // Assert
        Assert.DoesNotContain(deserializedDtos!, d => d.Notes == "peter@example.com");
    }

    [Fact]
    public async Task Persistence_Should_PreserveGuids()
    {
        // Arrange
        var dto = CreateTestDto("Test", 25);
        var originalId = dto.Id;
        _store.Add(dto);

        // Act
        await Task.Delay(200);
        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var deserializedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);

        // Assert
        Assert.Equal(originalId, deserializedDtos![0].Id);
    }

    [Fact]
    public async Task Persistence_Should_PreserveAllProperties()
    {
        // Arrange
        var dto = new TestDto("Max Mustermann", 35, isActive: true, score: 95.5m)
        {
            Notes = "Test Notes"
        };
        _store.Add(dto);

        // Act
        await Task.Delay(200);
        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var deserializedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);
        var loaded = deserializedDtos![0];

        // Assert
        Assert.Equal(dto.Name, loaded.Name);
        Assert.Equal(dto.Age, loaded.Age);
        Assert.Equal(dto.IsActive, loaded.IsActive);
        Assert.Equal(dto.Score, loaded.Score);
        Assert.Equal(dto.Notes, loaded.Notes);
    }

    // ====================================================================
    // PropertyChanged Tests
    // ====================================================================

    [Fact]
    public async Task PropertyChanged_Should_TriggerPersistence()
    {
        // Arrange
        var dto = CreateTestDto("Original", 25);
        _store.Add(dto);
        await Task.Delay(200);

        // Verify initial state
        var initialContent = await File.ReadAllTextAsync(_jsonFilePath);
        var initialDtos = JsonSerializer.Deserialize<List<TestDto>>(initialContent);
        Assert.Equal("Original", initialDtos![0].Name);

        // Act
        dto.Name = "Updated";
        await Task.Delay(200);

        // Assert
        var updatedContent = await File.ReadAllTextAsync(_jsonFilePath);
        var updatedDtos = JsonSerializer.Deserialize<List<TestDto>>(updatedContent);
        Assert.Equal("Updated", updatedDtos![0].Name);
    }

    [Fact]
    public async Task PropertyChanged_Should_PersistMultipleChanges()
    {
        // Arrange
        var dto = CreateTestDto("Initial", 20);
        _store.Add(dto);
        await Task.Delay(200);

        // Act
        dto.Name = "First Update";
        await Task.Delay(200);
        
        dto.Age = 30;
        await Task.Delay(200);

        // Assert
        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var savedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);
        
        Assert.Equal("First Update", savedDtos![0].Name);
        Assert.Equal(30, savedDtos[0].Age);
    }

    [Fact]
    public async Task PropertyChanged_Should_NotTrack_AfterRemove()
    {
        // Arrange
        var dto = CreateTestDto("Test", 25);
        _store.Add(dto);
        await Task.Delay(200);

        // Act
        _store.Remove(dto);
        await Task.Delay(200);
        
        dto.Name = "Changed After Remove";
        await Task.Delay(200);

        // Assert
        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var savedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);
        
        Assert.Empty(savedDtos!);
    }

    [Fact]
    public async Task Clear_Should_PersistEmptyCollection()
    {
        // Arrange
        _store.AddRange(new[]
        {
            CreateTestDto("Dto1", 20),
            CreateTestDto("Dto2", 30)
        });
        await Task.Delay(200);

        // Act
        _store.Clear();
        await Task.Delay(200);

        // Assert
        var jsonContent = await File.ReadAllTextAsync(_jsonFilePath);
        var savedDtos = JsonSerializer.Deserialize<List<TestDto>>(jsonContent);
        
        Assert.Empty(savedDtos!);
    }

    // ====================================================================
    // Helper Methods
    // ====================================================================

    private static TestDto CreateTestDto(string name, int age, bool isActive = true, decimal score = 0m)
    {
        return new TestDto(name, age, isActive, score);
    }

    // ====================================================================
    // Helper Classes
    // ====================================================================

    /// <summary>
    /// Registrar für TestDto mit JSON-Persistierung.
    /// </summary>
    private class JsonTestDtoRegistrar : IDataStoreRegistrar
    {
        private readonly string _jsonFilePath;

        public JsonTestDtoRegistrar(string jsonFilePath)
        {
            _jsonFilePath = jsonFilePath;
        }

        public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
        {
            var strategy = new JsonFilePersistenceStrategy<TestDto>(_jsonFilePath);
            var innerStore = new InMemoryDataStore<TestDto>();
            var persistentStore = new PersistentStoreDecorator<TestDto>(
                innerStore,
                strategy,
                autoLoad: true,
                autoSaveOnChange: true);

            registry.RegisterGlobal(persistentStore);
        }
    }
}
