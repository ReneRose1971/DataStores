using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Persistence;

/// <summary>
/// Decorator that adds persistence functionality to <see cref="IDataStore{T}"/>.
/// </summary>
/// <typeparam name="T">The type of items in the store. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// MUST NOT be instantiated directly in application code. Obtain stores via <see cref="IDataStores"/> facade.
/// </para>
/// <para>
/// Direct instantiation is allowed ONLY in:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IDataStoreRegistrar"/> implementations during registration</description></item>
/// <item><description>Test scenarios</description></item>
/// </list>
/// <para>
/// This decorator implements the Decorator pattern, wrapping an <see cref="InMemoryDataStore{T}"/>
/// with optional auto-load and auto-save functionality. Persistence logic is provided by
/// an <see cref="IPersistenceStrategy{T}"/> implementation.
/// </para>
/// <para>
/// <b>Auto-Load:</b> When enabled, data is loaded during <see cref="InitializeAsync"/>,
/// typically called by <see cref="Bootstrap.DataStoreBootstrap"/>.
/// </para>
/// <para>
/// <b>Auto-Save:</b> When enabled, data is saved automatically on every change (Add, Remove, Clear, etc.).
/// Saving occurs asynchronously in the background and does not block operations.
/// </para>
/// <para>
/// <b>PropertyChanged Tracking:</b> When auto-save is enabled and items implement <see cref="System.ComponentModel.INotifyPropertyChanged"/>,
/// property changes are tracked and trigger a save.
/// </para>
/// </remarks>
public class PersistentStoreDecorator<T> : IDataStore<T>, IAsyncInitializable, IDisposable where T : class
{
    private readonly InMemoryDataStore<T> _innerStore;
    private readonly IPersistenceStrategy<T> _strategy;
    private readonly bool _autoLoad;
    private readonly bool _autoSaveOnChange;
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private readonly PropertyChangedBinder<T>? _propertyChangedBinder;
    private readonly IDisposable? _binderSubscription;
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentStoreDecorator{T}"/> class.
    /// </summary>
    /// <param name="innerStore">The inner in-memory store to decorate.</param>
    /// <param name="strategy">The persistence strategy for loading and saving.</param>
    /// <param name="autoLoad">If <c>true</c>, data is loaded during initialization. Default is <c>true</c>.</param>
    /// <param name="autoSaveOnChange">If <c>true</c>, data is saved automatically on changes. Default is <c>true</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="innerStore"/> or <paramref name="strategy"/> is null.</exception>
    /// <remarks>
    /// This constructor is typically called within <see cref="IDataStoreRegistrar"/> implementations.
    /// Do NOT call from application feature code.
    /// </remarks>
    public PersistentStoreDecorator(
        InMemoryDataStore<T> innerStore,
        IPersistenceStrategy<T> strategy,
        bool autoLoad = true,
        bool autoSaveOnChange = true)
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _autoLoad = autoLoad;
        _autoSaveOnChange = autoSaveOnChange;

        _strategy.SetItemsProvider(() => _innerStore.Items);

        if (_autoSaveOnChange)
        {
            _innerStore.Changed += OnInnerStoreChanged;
            
            if (typeof(T).IsAssignableTo(typeof(System.ComponentModel.INotifyPropertyChanged)))
            {
                _propertyChangedBinder = new PropertyChangedBinder<T>(
                    enabled: true,
                    onEntityChanged: OnItemPropertyChanged);
                
                _binderSubscription = _propertyChangedBinder.AttachToDataStore(_innerStore);
            }
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
    /// <remarks>
    /// Initializes the store asynchronously by loading data from the persistence strategy.
    /// This method is idempotent and thread-safe.
    /// Typically called automatically by <see cref="Bootstrap.DataStoreBootstrap"/>.
    /// </remarks>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            { 
            return;
            }

            if (_autoLoad)
            {
                var items = await _strategy.LoadAllAsync(cancellationToken);
                _innerStore.AddRange(items);
            }
            
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
        {
            return;
        }

        await SaveAsync();
    }

    private async void OnItemPropertyChanged(T entity)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await _saveSemaphore.WaitAsync();
            try
            {
                await _strategy.UpdateSingleAsync(entity);
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }
        catch (Exception)
        {
        }
    }

    private async Task SaveAsync()
    {
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
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_autoSaveOnChange)
        {
            _innerStore.Changed -= OnInnerStoreChanged;
            _binderSubscription?.Dispose();
            _propertyChangedBinder?.Dispose();
        }

        _saveSemaphore.Dispose();
        _initSemaphore.Dispose();
        _disposed = true;
    }
}
