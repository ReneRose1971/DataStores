using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// Provides an in-memory implementation of <see cref="IDataStore{T}"/>.
/// </summary>
/// <typeparam name="T">The type of items in the store.</typeparam>
public class InMemoryDataStore<T> : IDataStore<T> where T : class
{
    private readonly List<T> _items;
    private readonly IEqualityComparer<T> _comparer;
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly object _lock = new();

    /// <inheritdoc/>
    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_lock)
            {
                return _items.ToList();
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<DataStoreChangedEventArgs<T>>? Changed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDataStore{T}"/> class.
    /// </summary>
    /// <param name="comparer">Optional equality comparer. If null, uses default comparer.</param>
    /// <param name="synchronizationContext">Optional synchronization context for event invocation. If null, events fire synchronously.</param>
    public InMemoryDataStore(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null)
    {
        _items = new List<T>();
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _synchronizationContext = synchronizationContext;
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        lock (_lock)
        {
            _items.Add(item);
        }
        OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.Add, item));
    }

    /// <inheritdoc/>
    public void AddRange(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            return;

        lock (_lock)
        {
            _items.AddRange(itemList);
        }
        OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.BulkAdd, itemList));
    }

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        bool removed;
        lock (_lock)
        {
            var index = _items.FindIndex(x => _comparer.Equals(x, item));
            if (index >= 0)
            {
                _items.RemoveAt(index);
                removed = true;
            }
            else
            {
                removed = false;
            }
        }

        if (removed)
        {
            OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.Remove, item));
        }

        return removed;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock)
        {
            _items.Clear();
        }
        OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.Clear));
    }

    /// <inheritdoc/>
    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _items.Any(x => _comparer.Equals(x, item));
        }
    }

    private void OnChanged(DataStoreChangedEventArgs<T> args)
    {
        var handler = Changed;
        if (handler == null)
            return;

        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ => handler.Invoke(this, args), null);
        }
        else
        {
            handler.Invoke(this, args);
        }
    }
}
