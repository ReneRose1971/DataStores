using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataStores.Persistence;
using DataStores.Runtime;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Integration.Persistence;

[Trait("Category", "Integration")]
public class JsonPersistence_PropertyChanged_IntegrationTests : IDisposable
{
    private readonly string _testRoot;

    public JsonPersistence_PropertyChanged_IntegrationTests()
    {
        _testRoot = Path.Combine(
            Path.GetTempPath(),
            "DataStores.Tests.Integration",
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
    public async Task JsonPersistence_Should_Create_File_On_Add_When_AutoSaveOnChange_Enabled()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "add_test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var innerStore = new InMemoryDataStore<TestDto>();
        var decorator = new PersistentStoreDecorator<TestDto>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        // Act
        decorator.Add(new TestDto("John Doe", 30));
        await Task.Delay(200); // Wait for async save

        // Assert
        Assert.True(File.Exists(filePath), "JSON file should be created");
        Assert.True(new FileInfo(filePath).Length > 0, "JSON file should not be empty");

        // Verify content by loading
        var loadedItems = await strategy.LoadAllAsync();
        Assert.Single(loadedItems);
        Assert.Equal("John Doe", loadedItems[0].Name);
    }

    [Fact]
    public async Task JsonPersistence_Should_Reflect_Remove_On_Save()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "remove_test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var innerStore = new InMemoryDataStore<TestDto>();
        var decorator = new PersistentStoreDecorator<TestDto>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person1 = new TestDto("Person1", 25);
        var person2 = new TestDto("Person2", 35);
        
        decorator.AddRange(new[] { person1, person2 });
        await Task.Delay(200);

        // Verify initial state
        var initialItems = await strategy.LoadAllAsync();
        Assert.Equal(2, initialItems.Count);

        // Act
        decorator.Remove(person1);
        await Task.Delay(200);

        // Assert
        Assert.True(File.Exists(filePath), "JSON file should still exist");
        
        var finalItems = await strategy.LoadAllAsync();
        Assert.Single(finalItems);
        Assert.Equal("Person2", finalItems[0].Name);
    }

    [Fact]
    public async Task JsonPersistence_Should_Save_On_PropertyChanged()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "propertychanged_test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var innerStore = new InMemoryDataStore<TestDto>();
        var decorator = new PersistentStoreDecorator<TestDto>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person = new TestDto("Original Name", 40);
        decorator.Add(person);
        await Task.Delay(200);

        // Verify initial state
        var initialItems = await strategy.LoadAllAsync();
        Assert.Equal("Original Name", initialItems[0].Name);

        // Act
        person.Name = "Updated Name";
        await Task.Delay(200); // Wait for async save

        // Assert
        Assert.True(File.Exists(filePath), "JSON file should still exist");
        
        var updatedItems = await strategy.LoadAllAsync();
        Assert.Single(updatedItems);
        Assert.Equal("Updated Name", updatedItems[0].Name);
        Assert.Equal(40, updatedItems[0].Age); // Age unchanged
    }

    [Fact]
    public async Task JsonPersistence_Should_Not_Track_PropertyChanged_After_Remove()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "untrack_after_remove_test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var innerStore = new InMemoryDataStore<TestDto>();
        var decorator = new PersistentStoreDecorator<TestDto>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person = new TestDto("Test", 20);
        decorator.Add(person);
        await Task.Delay(200);

        decorator.Remove(person);
        await Task.Delay(200);

        // Verify removed
        var afterRemove = await strategy.LoadAllAsync();
        Assert.Empty(afterRemove);

        // Act
        person.Name = "Changed After Remove";
        await Task.Delay(200);

        // Assert - Should still be empty
        var finalItems = await strategy.LoadAllAsync();
        Assert.Empty(finalItems);
    }

    [Fact]
    public async Task JsonPersistence_Should_Track_Multiple_Items_PropertyChanged()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "multiple_items_test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var innerStore = new InMemoryDataStore<TestDto>();
        var decorator = new PersistentStoreDecorator<TestDto>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person1 = new TestDto("Person1", 20);
        var person2 = new TestDto("Person2", 25);
        
        decorator.AddRange(new[] { person1, person2 });
        await Task.Delay(200);

        // Act
        person1.Name = "Updated Person1";
        await Task.Delay(200);
        
        person2.Age = 30;
        await Task.Delay(200);

        // Assert
        var items = await strategy.LoadAllAsync();
        Assert.Equal(2, items.Count);
        
        var updated1 = items.First(p => p.Age == 20);
        var updated2 = items.First(p => p.Age == 30);
        
        Assert.Equal("Updated Person1", updated1.Name);
        Assert.Equal("Person2", updated2.Name);
    }

    [Fact]
    public async Task JsonPersistence_Should_Handle_Clear_And_PropertyChanged()
    {
        // Arrange
        var filePath = Path.Combine(_testRoot, "clear_test.json");
        var strategy = new JsonFilePersistenceStrategy<TestDto>(filePath);
        var innerStore = new InMemoryDataStore<TestDto>();
        var decorator = new PersistentStoreDecorator<TestDto>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person1 = new TestDto("Person1", 20);
        var person2 = new TestDto("Person2", 25);
        
        decorator.AddRange(new[] { person1, person2 });
        await Task.Delay(200);

        // Act
        decorator.Clear();
        await Task.Delay(200);

        // Assert
        var items = await strategy.LoadAllAsync();
        Assert.Empty(items);

        // Verify no tracking after clear
        person1.Name = "Changed After Clear";
        await Task.Delay(200);
        
        var finalItems = await strategy.LoadAllAsync();
        Assert.Empty(finalItems);
    }
}
