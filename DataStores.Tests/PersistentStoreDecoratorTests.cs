using DataStores.Persistence;
using DataStores.Runtime;
using TestHelper.DataStores.Persistence;

namespace DataStores.Tests;

[Trait("Category", "Unit")]
public class PersistentStoreDecoratorTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task InitializeAsync_Should_LoadDataFromStrategy()
    {
        var strategy = new FakePersistenceStrategy<TestItem>(new[]
        {
            new TestItem { Id = 1, Name = "Item1" },
            new TestItem { Id = 2, Name = "Item2" }
        });
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        await decorator.InitializeAsync();

        Assert.Equal(2, decorator.Items.Count);
    }

    [Fact]
    public async Task InitializeAsync_Should_CallLoadOnce()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        await decorator.InitializeAsync();

        Assert.Equal(1, strategy.LoadCallCount);
    }

    [Fact]
    public async Task InitializeAsync_Should_NotLoadTwice()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        await decorator.InitializeAsync();
        await decorator.InitializeAsync();

        Assert.Equal(1, strategy.LoadCallCount);
    }

    [Fact]
    public async Task Add_Should_InvokeSave_WhenAutoSaveOnChange()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        decorator.Add(new TestItem { Id = 1, Name = "Test" });
        await Task.Delay(100);

        Assert.True(strategy.SaveCallCount > 0);
    }

    [Fact]
    public async Task Add_Should_SaveCorrectItems()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        decorator.Add(new TestItem { Id = 1, Name = "Test" });
        await Task.Delay(100);

        Assert.Single(strategy.LastSavedItems!);
    }

    [Fact]
    public void Items_Should_ReflectInnerItems()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: false);
        
        decorator.Add(new TestItem { Id = 1, Name = "Test" });

        Assert.Single(decorator.Items);
    }

    [Fact]
    public void Items_Should_ContainAddedItem()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: false);
        var item = new TestItem { Id = 1, Name = "Test" };
        
        decorator.Add(item);

        Assert.Equal(1, decorator.Items[0].Id);
    }

    [Fact]
    public void Changed_Should_BeForwarded()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: false);
        var fired = false;

        decorator.Changed += (s, e) => fired = true;
        decorator.Add(new TestItem { Id = 1 });

        Assert.True(fired);
    }

    [Fact]
    public void Remove_Should_RemoveFromInner()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: false);
        var item = new TestItem { Id = 1, Name = "Test" };
        decorator.Add(item);

        decorator.Remove(item);

        Assert.Empty(decorator.Items);
    }

    [Fact]
    public void Clear_Should_ClearInner()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: false);
        decorator.Add(new TestItem { Id = 1 });
        decorator.Add(new TestItem { Id = 2 });

        decorator.Clear();

        Assert.Empty(decorator.Items);
    }

    [Fact]
    public void Contains_Should_CheckInner()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(innerStore, strategy, autoLoad: false, autoSaveOnChange: false);
        var item = new TestItem { Id = 1, Name = "Test" };
        decorator.Add(item);

        var result = decorator.Contains(item);

        Assert.True(result);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenInnerStoreIsNull()
    {
        var strategy = new FakePersistenceStrategy<TestItem>();

        Assert.Throws<ArgumentNullException>(() => 
            new PersistentStoreDecorator<TestItem>(null!, strategy, false, false));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenStrategyIsNull()
    {
        var innerStore = new InMemoryDataStore<TestItem>();

        Assert.Throws<ArgumentNullException>(() => 
            new PersistentStoreDecorator<TestItem>(innerStore, null!, false, false));
    }

    [Fact]
    public async Task InitializeAsync_WithAutoLoadTrue_Should_LoadData()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>(new[]
        {
            new TestItem { Id = 1, Name = "Item1" },
            new TestItem { Id = 2, Name = "Item2" }
        });
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        // Act
        await decorator.InitializeAsync();

        // Assert
        Assert.Equal(2, decorator.Items.Count);
        Assert.Equal(1, strategy.LoadCallCount);
    }

    [Fact]
    public async Task InitializeAsync_WithAutoLoadFalse_Should_NotLoadData()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>(new[]
        {
            new TestItem { Id = 1, Name = "Item1" },
            new TestItem { Id = 2, Name = "Item2" }
        });
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: false);

        // Act
        await decorator.InitializeAsync();

        // Assert
        Assert.Empty(decorator.Items);
        Assert.Equal(0, strategy.LoadCallCount);
    }

    [Fact]
    public void Constructor_WithAutoLoadFalse_Should_NotLoadImmediately()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>(new[]
        {
            new TestItem { Id = 1, Name = "Item1" }
        });
        var innerStore = new InMemoryDataStore<TestItem>();

        // Act
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: false);

        // Assert - No data loaded until InitializeAsync is called
        Assert.Empty(decorator.Items);
        Assert.Equal(0, strategy.LoadCallCount);
    }

    [Fact]
    public void Constructor_WithAutoLoadTrue_Should_NotLoadImmediately()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>(new[]
        {
            new TestItem { Id = 1, Name = "Item1" }
        });
        var innerStore = new InMemoryDataStore<TestItem>();

        // Act
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        // Assert - No data loaded until InitializeAsync is called
        Assert.Empty(decorator.Items);
        Assert.Equal(0, strategy.LoadCallCount);
    }

    [Fact]
    public async Task InitializeAsync_WithAutoLoadFalse_CalledTwice_Should_NotLoad()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>(new[]
        {
            new TestItem { Id = 1, Name = "Item1" }
        });
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: false);

        // Act
        await decorator.InitializeAsync();
        await decorator.InitializeAsync(); // Second call

        // Assert - No data loaded at all
        Assert.Empty(decorator.Items);
        Assert.Equal(0, strategy.LoadCallCount);
    }
}
