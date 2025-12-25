using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// PRIMARY API FACADE implementation for accessing global and creating local data stores.
/// Application code MUST use IDataStores, NEVER instantiate this class directly.
/// </summary>
/// <remarks>
/// <para>
/// This class coordinates communication between <see cref="IGlobalStoreRegistry"/> 
/// and <see cref="ILocalDataStoreFactory"/> to provide a unified API.
/// </para>
/// <para>
/// Registered as Singleton via <see cref="Bootstrap.DataStoresServiceModule"/>.
/// Application code receives this via dependency injection through <see cref="IDataStores"/>.
/// </para>
/// </remarks>
public class DataStoresFacade : IDataStores
{
    private readonly IGlobalStoreRegistry _registry;
    private readonly ILocalDataStoreFactory _localFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoresFacade"/> class.
    /// </summary>
    /// <param name="registry">The global store registry for accessing application-wide singleton stores.</param>
    /// <param name="localFactory">The factory for creating isolated local stores.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="registry"/> or <paramref name="localFactory"/> is null.</exception>
    /// <remarks>
    /// This constructor is called by dependency injection. Do NOT instantiate directly.
    /// </remarks>
    public DataStoresFacade(IGlobalStoreRegistry registry, ILocalDataStoreFactory localFactory)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _localFactory = localFactory ?? throw new ArgumentNullException(nameof(localFactory));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates the call to the <see cref="IGlobalStoreRegistry.ResolveGlobal{T}"/> method.
    /// Global stores are application-wide singletons and are shared by all parts of the application.
    /// </remarks>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Thrown when no global store is registered for the type <typeparamref name="T"/>.
    /// </exception>
    public IDataStore<T> GetGlobal<T>() where T : class
    {
        return _registry.ResolveGlobal<T>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creates a new, isolated local store that is independent of global stores.
    /// Local stores are useful for temporary data, dialogs, forms, or other
    /// scenarios where isolation is required.
    /// </remarks>
    public IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class
    {
        return _localFactory.CreateLocal(comparer);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Creates a new local store and populates it with a filtered copy of the data
    /// from the global store. This is useful for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Working with a subset of global data</description></item>
    /// <item><description>Isolated editing without affecting global data</description></item>
    /// <item><description>Temporary filtering for UI scenarios</description></item>
    /// </list>
    /// <para>
    /// Changes to the local store do NOT affect the global store.
    /// </para>
    /// </remarks>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Thrown when no global store is registered for the type <typeparamref name="T"/>.
    /// </exception>
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
