using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataStores.Persistence;
using DataStores.Runtime;
using TestHelper.DataStores.Fixtures;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Integration.Persistence;

[Trait("Category", "Integration")]
public class LiteDbPersistence_PropertyChanged_IntegrationTests : IClassFixture<LiteDbPersistenceTempFixture>
{
    private readonly string _testRoot;

    public LiteDbPersistence_PropertyChanged_IntegrationTests(LiteDbPersistenceTempFixture fixture)
    {
        _testRoot = fixture.TestRoot;
    }

    [Fact]
    public async Task LiteDbPersistence_Should_Create_DbFile_On_Add_When_AutoSaveOnChange_Enabled()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LiteDbPersistence_Should_Create_DbFile_On_Add_When_AutoSaveOnChange_Enabled)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "persons");
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        // Act
        decorator.Add(new TestEntity { Id = 0, Name = "John Doe", Age = 30 });
        await Task.Delay(200); // Wait for async save

        // Assert
        Assert.True(File.Exists(dbPath), "LiteDB file should be created");
        Assert.True(new FileInfo(dbPath).Length > 0, "LiteDB file should not be empty");

        // Verify content by loading
        var loadedItems = await strategy.LoadAllAsync();
        Assert.Single(loadedItems);
        Assert.Equal("John Doe", loadedItems[0].Name);
    }

    [Fact]
    public async Task LiteDbPersistence_Should_Reflect_Remove_On_Save()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LiteDbPersistence_Should_Reflect_Remove_On_Save)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "persons");
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person1 = new TestEntity { Id = 0, Name = "Person1", Age = 25 };
        var person2 = new TestEntity { Id = 0, Name = "Person2", Age = 35 };
        
        decorator.AddRange(new[] { person1, person2 });
        await Task.Delay(200);

        // Verify initial state
        var initialItems = await strategy.LoadAllAsync();
        Assert.Equal(2, initialItems.Count);

        // Act
        decorator.Remove(person1);
        await Task.Delay(200);

        // Assert
        Assert.True(File.Exists(dbPath), "LiteDB file should still exist");
        
        var finalItems = await strategy.LoadAllAsync();
        Assert.Single(finalItems);
        Assert.Equal("Person2", finalItems[0].Name);
    }

    [Fact]
    public async Task LiteDbPersistence_Should_Save_On_PropertyChanged()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LiteDbPersistence_Should_Save_On_PropertyChanged)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "persons");
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person = new TestEntity { Id = 0, Name = "Original Name", Age = 40 };
        decorator.Add(person);
        await Task.Delay(200);

        // Verify initial state
        var initialItems = await strategy.LoadAllAsync();
        Assert.Equal("Original Name", initialItems[0].Name);

        // Act
        person.Name = "Updated Name";
        await Task.Delay(200); // Wait for async save

        // Assert
        Assert.True(File.Exists(dbPath), "LiteDB file should still exist");
        
        var updatedItems = await strategy.LoadAllAsync();
        Assert.Single(updatedItems);
        Assert.Equal("Updated Name", updatedItems[0].Name);
        Assert.Equal(40, updatedItems[0].Age); // Age unchanged
    }

    [Fact]
    public async Task LiteDbPersistence_Should_Track_Multiple_Items_PropertyChanged()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LiteDbPersistence_Should_Track_Multiple_Items_PropertyChanged)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "persons");
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person1 = new TestEntity { Id = 0, Name = "Person1", Age = 20 };
        var person2 = new TestEntity { Id = 0, Name = "Person2", Age = 25 };
        
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
    public async Task LiteDbPersistence_Should_Not_Track_PropertyChanged_After_Remove()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LiteDbPersistence_Should_Not_Track_PropertyChanged_After_Remove)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "persons");
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person = new TestEntity { Id = 0, Name = "Test", Age = 25 };
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
    public async Task LiteDbPersistence_Should_Handle_Clear_And_PropertyChanged()
    {
        // Arrange
        var dbPath = Path.Combine(_testRoot, $"{nameof(LiteDbPersistence_Should_Handle_Clear_And_PropertyChanged)}.db");
        var strategy = new LiteDbPersistenceStrategy<TestEntity>(dbPath, "persons");
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person1 = new TestEntity { Id = 0, Name = "Person1", Age = 20 };
        var person2 = new TestEntity { Id = 0, Name = "Person2", Age = 25 };
        
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
