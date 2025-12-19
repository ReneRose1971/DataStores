using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// Facade implementation for accessing global and creating local data stores.
/// </summary>
public class DataStoresFacade : IDataStores
{
    private readonly IGlobalStoreRegistry _registry;
    private readonly ILocalDataStoreFactory _localFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoresFacade"/> class.
    /// </summary>
    /// <param name="registry">The global store registry.</param>
    /// <param name="localFactory">The factory for creating local stores.</param>
    public DataStoresFacade(IGlobalStoreRegistry registry, ILocalDataStoreFactory localFactory)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _localFactory = localFactory ?? throw new ArgumentNullException(nameof(localFactory));
    }

    /// <inheritdoc/>
    public IDataStore<T> GetGlobal<T>() where T : class
    {
        return _registry.ResolveGlobal<T>();
    }

    /// <inheritdoc/>
    public IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class
    {
        return _localFactory.CreateLocal(comparer);
    }

    /// <inheritdoc/>
    public IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
        Func<T, bool>? predicate = null,
        IEqualityComparer<T>? comparer = null) where T : class
    {
        var globalStore = _registry.ResolveGlobal<T>();
        var localStore = _localFactory.CreateLocal(comparer);

        var items = predicate == null
            ? globalStore.Items
            : globalStore.Items.Where(predicate).ToList();

        localStore.AddRange(items);

        return localStore;
    }
}
