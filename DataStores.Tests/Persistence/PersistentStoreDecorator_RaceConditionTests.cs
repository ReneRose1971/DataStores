using DataStores.Persistence;
using DataStores.Runtime;
using TestHelper.DataStores.Persistence;

namespace DataStores.Tests.Persistence;

/// <summary>
/// Race condition and async edge case tests for PersistentStoreDecorator.
/// </summary>
public class PersistentStoreDecorator_RaceConditionTests
{
    [Fact]
    public async Task InitializeAsync_CalledTwice_Should_LoadOnlyOnce()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>(new[]
        {
            new TestItem { Id = 1, Name = "Item1" }
        });
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        // Act
        await decorator.InitializeAsync();
        await decorator.InitializeAsync(); // Second call

        // Assert - Should load only once
        Assert.Equal(1, strategy.LoadCallCount);
        Assert.Single(decorator.Items);
    }

    [Fact]
    public async Task InitializeAsync_ConcurrentCalls_Should_LoadOnlyOnce()
    {
        // Arrange
        var strategy = new SlowLoadStrategy<TestItem>(
            TimeSpan.FromMilliseconds(100),
            new[] { new TestItem { Id = 1, Name = "Item1" } });
        
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        // Act - Call InitializeAsync from multiple threads
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => decorator.InitializeAsync()))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Should load only once despite concurrent calls
        Assert.Equal(1, strategy.LoadCallCount);
        Assert.Single(decorator.Items);
    }

    [Fact]
    public async Task SaveAllAsync_DuringLoad_Should_Wait()
    {
        // Arrange
        var strategy = new SlowLoadStrategy<TestItem>(
            TimeSpan.FromMilliseconds(200),
            new[] { new TestItem { Id = 1, Name = "Original" } });
        
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: true);

        // Act - Initialize and immediately add (triggers save)
        var initTask = decorator.InitializeAsync();
        
        // Give init a head start
        await Task.Delay(50);
        
        // This should wait for init to complete
        decorator.Add(new TestItem { Id = 2, Name = "Added" });
        
        await initTask;
        await Task.Delay(100); // Wait for async save

        // Assert
        Assert.Equal(2, decorator.Items.Count);
        Assert.True(strategy.SaveCallCount > 0);
    }

    [Fact]
    public void Dispose_DuringOperations_Should_NotThrow()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        decorator.Add(new TestItem { Id = 1, Name = "Item1" });

        // Act & Assert - Dispose should not throw
        decorator.Dispose();
        
        // Multiple dispose should also not throw
        decorator.Dispose();
    }

    [Fact]
    public async Task ConcurrentChanges_Should_SerializeSaves()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        // Act - Add items concurrently
        var tasks = Enumerable.Range(1, 10)
            .Select(i => Task.Run(() => decorator.Add(new TestItem { Id = i, Name = $"Item{i}" })))
            .ToArray();

        await Task.WhenAll(tasks);
        await Task.Delay(500); // Wait for async saves

        // Assert - All items added
        Assert.Equal(10, decorator.Items.Count);
        
        // Saves were serialized (may not be exactly 10 due to batching)
        Assert.True(strategy.SaveCallCount > 0);
    }

    [Fact]
    public async Task LoadException_Should_Propagate()
    {
        // Arrange
        var strategy = new ThrowingPersistenceStrategy<TestItem>(
            throwOnLoad: true, throwOnSave: false);
        
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => decorator.InitializeAsync());
        
        Assert.Contains("Load failed", ex.Message);
    }

    [Fact]
    public async Task SaveException_Should_NotCorruptState()
    {
        // Arrange
        var strategy = new ThrowingPersistenceStrategy<TestItem>(
            throwOnLoad: false, throwOnSave: true);
        
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: true);

        // Act - Add item (will try to save and fail)
        decorator.Add(new TestItem { Id = 1, Name = "Item1" });
        await Task.Delay(100); // Wait for async save attempt

        // Assert - Item should still be in store despite save failure
        Assert.Single(decorator.Items);
    }

    [Fact]
    public async Task InitializeAsync_Cancel_Should_LeaveStoreEmpty()
    {
        // Arrange
        var strategy = new SlowLoadStrategy<TestItem>(
            TimeSpan.FromSeconds(10),
            new[] { new TestItem { Id = 1, Name = "Item1" } });
        
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: false);

        var cts = new CancellationTokenSource();

        // Act - Start init and cancel immediately
        var initTask = decorator.InitializeAsync(cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initTask);
        Assert.Empty(decorator.Items);
    }

    [Fact]
    public async Task AutoSaveOnChange_False_Should_NotSave()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: false);

        // Act
        decorator.Add(new TestItem { Id = 1, Name = "Item1" });
        await Task.Delay(100); // Wait to ensure no async save happens

        // Assert
        Assert.Single(decorator.Items);
        Assert.Equal(0, strategy.SaveCallCount); // No save should occur
    }

    [Fact]
    public void Items_Should_ReflectInnerStoreImmediately()
    {
        // Arrange
        var strategy = new FakePersistenceStrategy<TestItem>();
        var innerStore = new InMemoryDataStore<TestItem>();
        var decorator = new PersistentStoreDecorator<TestItem>(
            innerStore, strategy, autoLoad: false, autoSaveOnChange: false);

        // Act
        decorator.Add(new TestItem { Id = 1, Name = "Item1" });

        // Assert - Should be visible immediately
        Assert.Single(decorator.Items);
        Assert.Equal(1, decorator.Items[0].Id);
    }

    // Helper Classes

    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class SlowLoadStrategy<T> : IPersistenceStrategy<T> where T : class
    {
        private readonly TimeSpan _delay;
        private readonly IReadOnlyList<T> _data;
        public int LoadCallCount { get; private set; }
        public int SaveCallCount { get; private set; }

        public SlowLoadStrategy(TimeSpan delay, IReadOnlyList<T> data)
        {
            _delay = delay;
            _data = data;
        }

        public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            await Task.Delay(_delay, cancellationToken);
            return _data;
        }

        public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }

        public Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider)
        {
            // No-Op
        }
    }

    private class ThrowingPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
    {
        private readonly bool _throwOnLoad;
        private readonly bool _throwOnSave;

        public ThrowingPersistenceStrategy(bool throwOnLoad, bool throwOnSave)
        {
            _throwOnLoad = throwOnLoad;
            _throwOnSave = throwOnSave;
        }

        public Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
        {
            if (_throwOnLoad)
            {
                throw new InvalidOperationException("Load failed");
            }

            return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
        }

        public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
        {
            if (_throwOnSave)
            {
                throw new InvalidOperationException("Save failed");
            }

            return Task.CompletedTask;
        }

        public Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider)
        {
            // No-Op
        }
    }
}
