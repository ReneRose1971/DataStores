using DataStores.Abstractions;
using DataStores.Runtime;

namespace TestHelper.DataStores.Builders;

/// <summary>
/// Fluent builder for creating test DataStore instances with predefined configurations.
/// </summary>
public class DataStoreBuilder<T> where T : class
{
    private List<T> _items = new();
    private SynchronizationContext? _syncContext;
    private IEqualityComparer<T>? _comparer;
    private EventHandler<DataStoreChangedEventArgs<T>>? _changedHandler;

    public DataStoreBuilder<T> WithItems(params T[] items)
    {
        _items.AddRange(items);
        return this;
    }

    public DataStoreBuilder<T> WithSyncContext(SynchronizationContext ctx)
    {
        _syncContext = ctx;
        return this;
    }

    public DataStoreBuilder<T> WithComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer;
        return this;
    }

    public DataStoreBuilder<T> WithChangedHandler(EventHandler<DataStoreChangedEventArgs<T>> handler)
    {
        _changedHandler = handler;
        return this;
    }

    public IDataStore<T> Build()
    {
        var store = new InMemoryDataStore<T>(_comparer, _syncContext);
        
        if (_changedHandler != null)
            store.Changed += _changedHandler;

        if (_items.Count > 0)
            store.AddRange(_items);

        return store;
    }
}
