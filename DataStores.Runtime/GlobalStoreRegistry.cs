using System.Collections.Concurrent;
using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// Thread-safe implementation of <see cref="IGlobalStoreRegistry"/>.
/// </summary>
public class GlobalStoreRegistry : IGlobalStoreRegistry
{
    private readonly ConcurrentDictionary<Type, object> _stores = new();

    /// <inheritdoc/>
    public void RegisterGlobal<T>(IDataStore<T> store) where T : class
    {
        if (store == null)
            throw new ArgumentNullException(nameof(store));

        var storeType = typeof(T);
        if (!_stores.TryAdd(storeType, store))
        {
            throw new GlobalStoreAlreadyRegisteredException(storeType);
        }
    }

    /// <inheritdoc/>
    public IDataStore<T> ResolveGlobal<T>() where T : class
    {
        var storeType = typeof(T);
        if (_stores.TryGetValue(storeType, out var store))
        {
            return (IDataStore<T>)store;
        }

        throw new GlobalStoreNotRegisteredException(storeType);
    }

    /// <inheritdoc/>
    public bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class
    {
        var storeType = typeof(T);
        if (_stores.TryGetValue(storeType, out var storeObj))
        {
            store = (IDataStore<T>)storeObj;
            return true;
        }

        store = null!;
        return false;
    }
}
