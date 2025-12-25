using System;
using System.Threading.Tasks;
using DataStores.Persistence;
using DataStores.Runtime;
using TestHelper.DataStores.Models;
using Xunit;

namespace DataStores.Tests.Unit.Persistence;

[Trait("Category", "Unit")]
public class PersistentStoreDecorator_PropertyChanged_Tests
{
    [Fact]
    public async Task PersistentStoreDecorator_Should_Call_Save_On_Add()
    {
        // Arrange
        var spy = new SpyPersistenceStrategy<TestEntity>();
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, spy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        // Act
        decorator.Add(new TestEntity { Id = 0, Name = "Test" });
        await Task.Delay(100); // Wait for async save

        // Assert
        Assert.True(spy.SaveCallCount > 0, "Save should be called on Add");
        Assert.Equal(1, spy.LastSavedSnapshotCount);
    }

    [Fact]
    public async Task PersistentStoreDecorator_Should_Call_Save_On_Remove()
    {
        // Arrange
        var spy = new SpyPersistenceStrategy<TestEntity>();
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, spy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person = new TestEntity { Id = 0, Name = "Test" };
        decorator.Add(person);
        await Task.Delay(100);
        spy.Reset(); // Reset counter

        // Act
        decorator.Remove(person);
        await Task.Delay(100);

        // Assert
        Assert.True(spy.SaveCallCount > 0, "Save should be called on Remove");
        Assert.Equal(0, spy.LastSavedSnapshotCount);
    }

    [Fact]
    public async Task PersistentStoreDecorator_Should_Call_Save_On_PropertyChanged()
    {
        // Arrange
        var spy = new SpyPersistenceStrategy<TestEntity>();
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, spy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person = new TestEntity { Id = 0, Name = "Original" };
        decorator.Add(person);
        await Task.Delay(100);
        spy.Reset(); // Reset counter after initial add

        // Act
        person.Name = "Changed"; // PropertyChanged sollte UpdateSingleAsync triggern
        await Task.Delay(100); // Wait for async update

        // Assert
        Assert.True(spy.UpdateCallCount > 0, 
            "UpdateSingleAsync should be called when property changes on tracked item");
        Assert.NotNull(spy.LastUpdatedEntity);
        Assert.Equal("Changed", spy.LastUpdatedEntity!.Name);
    }

    [Fact]
    public async Task PersistentStoreDecorator_Should_Not_Call_Save_On_PropertyChanged_After_Remove()
    {
        // Arrange
        var spy = new SpyPersistenceStrategy<TestEntity>();
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, spy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person = new TestEntity { Id = 0, Name = "Test" };
        decorator.Add(person);
        await Task.Delay(100);
        
        decorator.Remove(person);
        await Task.Delay(100);
        spy.Reset(); // Reset after remove

        // Act
        person.Name = "Changed After Remove";
        await Task.Delay(100);

        // Assert
        Assert.Equal(0, spy.SaveCallCount);
    }

    [Fact]
    public async Task PersistentStoreDecorator_Should_Track_Multiple_Items_PropertyChanged()
    {
        // Arrange
        var spy = new SpyPersistenceStrategy<TestEntity>();
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, spy, autoLoad: false, autoSaveOnChange: true);

        await decorator.InitializeAsync();

        var person1 = new TestEntity { Id = 0, Name = "Person1" };
        var person2 = new TestEntity { Id = 0, Name = "Person2" };
        
        decorator.AddRange(new[] { person1, person2 });
        await Task.Delay(100);
        spy.Reset();

        // Act
        person1.Name = "Changed1";
        await Task.Delay(100);
        var updateCountAfterFirst = spy.UpdateCallCount;

        person2.Name = "Changed2";
        await Task.Delay(100);

        // Assert
        Assert.True(updateCountAfterFirst > 0, "UpdateSingleAsync should be called for person1");
        Assert.True(spy.UpdateCallCount > updateCountAfterFirst, "UpdateSingleAsync should be called for person2");
        Assert.Equal(2, spy.UpdatedEntities.Count);
    }

    [Fact]
    public async Task PersistentStoreDecorator_Should_Not_Save_When_AutoSaveOnChange_Disabled()
    {
        // Arrange
        var spy = new SpyPersistenceStrategy<TestEntity>();
        var innerStore = new InMemoryDataStore<TestEntity>();
        var decorator = new PersistentStoreDecorator<TestEntity>(
            innerStore, spy, autoLoad: false, autoSaveOnChange: false);

        await decorator.InitializeAsync();

        var person = new TestEntity { Id = 0, Name = "Test" };
        decorator.Add(person);
        await Task.Delay(100);

        // Act
        person.Name = "Changed";
        await Task.Delay(100);

        // Assert
        Assert.Equal(0, spy.SaveCallCount);
    }
}
