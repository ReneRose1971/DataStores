using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// In-memory implementation of <see cref="IDataStore{T}"/>.
/// Thread-safe with optional SynchronizationContext-based event marshalling.
/// </summary>
/// <typeparam name="T">The type of items in the store. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// Application code MUST obtain instances via <see cref="IDataStores"/> facade:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IDataStores.GetGlobal{T}"/> - For global stores</description></item>
/// <item><description><see cref="IDataStores.CreateLocal{T}"/> - For local stores</description></item>
/// </list>
/// <para>
/// Direct instantiation is allowed ONLY for:
/// </para>
/// <list type="bullet">
/// <item><description>Infrastructure components (registrars, decorators)</description></item>
/// <item><description>Test scenarios</description></item>
/// </list>
/// <para>
/// This class uses a <see cref="List{T}"/> internally with lock-based synchronization for thread safety.
/// Events can optionally be marshalled to a specific SynchronizationContext (useful for UI applications).
/// </para>
/// <para>
/// <b>Duplicate Prevention:</b>
/// Add() enforces uniqueness using the configured IEqualityComparer. Attempting to add a duplicate
/// item will throw an InvalidOperationException. Use AddOrReplace() for update semantics.
/// </para>
/// </remarks>
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
    /// <param name="comparer">Optional equality comparer for items. If null, uses <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <param name="synchronizationContext">Optional synchronization context for events. If null, events are raised synchronously on calling thread.</param>
    /// <remarks>
    /// For application code, use <see cref="IDataStores"/> methods instead of direct instantiation.
    /// </remarks>
    public InMemoryDataStore(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null)
    {
        _items = new List<T>();
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _synchronizationContext = synchronizationContext;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown when attempting to add a duplicate item.</exception>
    /// <remarks>
    /// <para>
    /// <b>Duplicate Prevention:</b>
    /// This method enforces uniqueness using the configured IEqualityComparer.
    /// If an item with the same identity already exists, an exception is thrown.
    /// </para>
    /// <para>
    /// For update semantics, use <see cref="AddOrReplace"/> instead.
    /// </para>
    /// </remarks>
    public void Add(T item)
    {
        lock (_lock)
        {
            // Duplicate prevention
            if (_items.Any(x => _comparer.Equals(x, item)))
            {
                throw new InvalidOperationException(
                    $"Duplicate item rejected. An item with the same identity already exists in the store. " +
                    $"Identity determined by: {_comparer.GetType().Name}. " +
                    $"Use AddOrReplace() for update semantics.");
            }

            _items.Add(item);
        }
        OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.Add, item));
    }

    /// <summary>
    /// Adds a new item or replaces an existing item with the same identity.
    /// </summary>
    /// <param name="item">The item to add or replace.</param>
    /// <remarks>
    /// <para>
    /// <b>Update Semantics:</b>
    /// If an item with the same identity (according to the comparer) already exists,
    /// it is replaced. Otherwise, the item is added.
    /// </para>
    /// <para>
    /// Raises <see cref="Changed"/> event with <see cref="DataStoreChangeType.Update"/>
    /// for replacements and <see cref="DataStoreChangeType.Add"/> for new items.
    /// </para>
    /// </remarks>
    public void AddOrReplace(T item)
    {
        bool wasReplaced;
        lock (_lock)
        {
            var index = _items.FindIndex(x => _comparer.Equals(x, item));
            if (index >= 0)
            {
                _items[index] = item;
                wasReplaced = true;
            }
            else
            {
                _items.Add(item);
                wasReplaced = false;
            }
        }

        OnChanged(new DataStoreChangedEventArgs<T>(
            wasReplaced ? DataStoreChangeType.Update : DataStoreChangeType.Add,
            item));
    }

    /// <inheritdoc/>
    public void AddRange(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            return;
        }

        lock (_lock)
        {
            // Check for duplicates with existing items
            var duplicatesWithExisting = itemList
                .Where(newItem => _items.Any(existing => _comparer.Equals(existing, newItem)))
                .ToList();

            if (duplicatesWithExisting.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate items rejected. {duplicatesWithExisting.Count} item(s) with existing identities detected in batch. " +
                    $"Identity determined by: {_comparer.GetType().Name}");
            }

            // Check for duplicates within the batch itself
            var seenItems = new HashSet<T>(_comparer);
            foreach (var item in itemList)
            {
                if (!seenItems.Add(item))
                {
                    throw new InvalidOperationException(
                        $"Duplicate items rejected. Batch contains duplicate items according to comparer. " +
                        $"Identity determined by: {_comparer.GetType().Name}");
                }
            }

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
        {
            return;
        }

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
