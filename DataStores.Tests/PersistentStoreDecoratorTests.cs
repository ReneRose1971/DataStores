using DataStores.Persistence;
using DataStores.Runtime;

namespace DataStores.Tests;

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
}
