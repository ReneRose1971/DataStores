using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Persistence;

/// <summary>
/// Decorates an <see cref="IDataStore{T}"/> with persistence capabilities.
/// </summary>
/// <typeparam name="T">The type of items in the store.</typeparam>
public class PersistentStoreDecorator<T> : IDataStore<T>, IAsyncInitializable, IDisposable where T : class
{
    private readonly InMemoryDataStore<T> _innerStore;
    private readonly IPersistenceStrategy<T> _strategy;
    private readonly bool _autoSaveOnChange;
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentStoreDecorator{T}"/> class.
    /// </summary>
    /// <param name="innerStore">The inner in-memory store to decorate.</param>
    /// <param name="strategy">The persistence strategy to use.</param>
    /// <param name="autoLoad">If true, data will be loaded during initialization.</param>
    /// <param name="autoSaveOnChange">If true, data will be saved automatically on changes.</param>
    public PersistentStoreDecorator(
        InMemoryDataStore<T> innerStore,
        IPersistenceStrategy<T> strategy,
        bool autoLoad = true,
        bool autoSaveOnChange = true)
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _autoSaveOnChange = autoSaveOnChange;

        if (_autoSaveOnChange)
        {
            _innerStore.Changed += OnInnerStoreChanged;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<T> Items => _innerStore.Items;

    /// <inheritdoc/>
    public event EventHandler<DataStoreChangedEventArgs<T>>? Changed
    {
        add => _innerStore.Changed += value;
        remove => _innerStore.Changed -= value;
    }

    /// <inheritdoc/>
    public void Add(T item) => _innerStore.Add(item);

    /// <inheritdoc/>
    public void AddRange(IEnumerable<T> items) => _innerStore.AddRange(items);

    /// <inheritdoc/>
    public bool Remove(T item) => _innerStore.Remove(item);

    /// <inheritdoc/>
    public void Clear() => _innerStore.Clear();

    /// <inheritdoc/>
    public bool Contains(T item) => _innerStore.Contains(item);

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
                return;

            var items = await _strategy.LoadAllAsync(cancellationToken);
            _innerStore.AddRange(items);
            _isInitialized = true;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    private async void OnInnerStoreChanged(object? sender, DataStoreChangedEventArgs<T> e)
    {
        if (_disposed)
            return;

        try
        {
            await _saveSemaphore.WaitAsync();
            try
            {
                await _strategy.SaveAllAsync(_innerStore.Items);
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }
        catch (Exception)
        {
            // Consider logging here
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_autoSaveOnChange)
        {
            _innerStore.Changed -= OnInnerStoreChanged;
        }

        _saveSemaphore.Dispose();
        _initSemaphore.Dispose();
        _disposed = true;
    }
}
