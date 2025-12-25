using DataStores.Abstractions;

namespace TestHelper.DataStores.Fakes;

/// <summary>
/// Fake implementation of IGlobalStoreRegistry for testing.
/// Tracks all operations and provides controllable behavior.
/// </summary>
public class FakeGlobalStoreRegistry : IGlobalStoreRegistry
{
    private readonly Dictionary<Type, object> _stores = new();
    
    // Test Properties
    public int RegisterCallCount { get; private set; }
    public int ResolveGlobalCallCount { get; private set; }
    public int TryResolveGlobalCallCount { get; private set; }
    public int GetInitializableGlobalStoresCallCount { get; private set; }
    public bool ThrowOnRegister { get; set; }
    public bool ThrowOnResolveGlobal { get; set; }
    public List<(string Action, Type Type, object? Store)> History { get; } = new();

    public void RegisterGlobal<T>(IDataStore<T> store) where T : class
    {
        RegisterCallCount++;
        History.Add(("Register", typeof(T), store));
        
        if (ThrowOnRegister)
            throw new GlobalStoreAlreadyRegisteredException(typeof(T));

        if (_stores.ContainsKey(typeof(T)))
            throw new GlobalStoreAlreadyRegisteredException(typeof(T));

        _stores[typeof(T)] = store;
    }

    public IDataStore<T> ResolveGlobal<T>() where T : class
    {
        ResolveGlobalCallCount++;
        History.Add(("ResolveGlobal", typeof(T), null));
        
        if (ThrowOnResolveGlobal)
            throw new GlobalStoreNotRegisteredException(typeof(T));

        if (!_stores.TryGetValue(typeof(T), out var store))
            throw new GlobalStoreNotRegisteredException(typeof(T));

        return (IDataStore<T>)store;
    }

    public bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class
    {
        TryResolveGlobalCallCount++;
        History.Add(("TryResolveGlobal", typeof(T), null));
        
        if (_stores.TryGetValue(typeof(T), out var storeObj))
        {
            store = (IDataStore<T>)storeObj;
            return true;
        }

        store = null!;
        return false;
    }

    public IEnumerable<global::DataStores.Persistence.IAsyncInitializable> GetInitializableGlobalStores()
    {
        GetInitializableGlobalStoresCallCount++;
        History.Add(("GetInitializableGlobalStores", typeof(void), null));
        
        return _stores.Values.OfType<global::DataStores.Persistence.IAsyncInitializable>();
    }

    public void Dispose()
    {
        History.Add(("Dispose", typeof(void), null));
        _stores.Clear();
    }

    public void Reset()
    {
        _stores.Clear();
        History.Clear();
        RegisterCallCount = 0;
        ResolveGlobalCallCount = 0;
        TryResolveGlobalCallCount = 0;
        GetInitializableGlobalStoresCallCount = 0;
        ThrowOnRegister = false;
        ThrowOnResolveGlobal = false;
    }
}
