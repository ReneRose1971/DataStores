using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// PRIMARY API FACADE implementation for accessing global and creating local data stores.
/// Application code MUST use IDataStores, NEVER instantiate this class directly.
/// </summary>
/// <remarks>
/// <para>
/// This class coordinates communication between <see cref="IGlobalStoreRegistry"/>, 
/// <see cref="ILocalDataStoreFactory"/>, and <see cref="IEqualityComparerService"/>
/// to provide a unified API with automatic comparer resolution.
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
    private readonly IEqualityComparerService _comparerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoresFacade"/> class.
    /// </summary>
    /// <param name="registry">The global store registry for accessing application-wide singleton stores.</param>
    /// <param name="localFactory">The factory for creating isolated local stores.</param>
    /// <param name="comparerService">The comparer service for automatic comparer resolution.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <remarks>
    /// This constructor is called by dependency injection. Do NOT instantiate directly.
    /// </remarks>
    public DataStoresFacade(
        IGlobalStoreRegistry registry, 
        ILocalDataStoreFactory localFactory,
        IEqualityComparerService comparerService)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _localFactory = localFactory ?? throw new ArgumentNullException(nameof(localFactory));
        _comparerService = comparerService ?? throw new ArgumentNullException(nameof(comparerService));
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
    /// <para>
    /// Creates a new, isolated local store that is independent of global stores.
    /// Local stores are useful for temporary data, dialogs, forms, or other
    /// scenarios where isolation is required.
    /// </para>
    /// <para>
    /// <b>Automatic Comparer Resolution:</b>
    /// If comparer is null, automatically resolves via <see cref="IEqualityComparerService"/>:
    /// </para>
    /// <list type="bullet">
    /// <item><description>EntityBase types → EntityIdComparer</description></item>
    /// <item><description>Types with registered comparer → From DI</description></item>
    /// <item><description>Other types → EqualityComparer&lt;T&gt;.Default</description></item>
    /// </list>
    /// </remarks>
    public IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class
    {
        // Automatic comparer resolution when null
        var effectiveComparer = comparer ?? _comparerService.GetComparer<T>();
        return _localFactory.CreateLocal(effectiveComparer);
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
    /// <para>
    /// <b>Automatic Comparer Resolution:</b>
    /// If comparer is null, automatically resolves via <see cref="IEqualityComparerService"/>.
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
        
        // Automatic comparer resolution when null
        var effectiveComparer = comparer ?? _comparerService.GetComparer<T>();
        var localStore = _localFactory.CreateLocal(effectiveComparer);

        var items = predicate == null
            ? globalStore.Items
            : globalStore.Items.Where(predicate).ToList();

        localStore.AddRange(items);

        return localStore;
    }
}
