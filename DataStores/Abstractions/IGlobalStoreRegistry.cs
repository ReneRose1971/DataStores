namespace DataStores.Abstractions;

/// <summary>
/// INTERNAL INFRASTRUCTURE. Do NOT use directly in application code.
/// Manages the registration and resolution of global data stores.
/// </summary>
/// <remarks>
/// <para>
/// Application code MUST NOT depend on this type. Use <see cref="IDataStores"/> instead.
/// </para>
/// <para>
/// This interface is intended for:
/// </para>
/// <list type="bullet">
/// <item><description>Infrastructure components (Bootstrap, Facade)</description></item>
/// <item><description>Registration during startup via <see cref="IDataStoreRegistrar"/></description></item>
/// </list>
/// <para>
/// Direct usage bypasses lifecycle and initialization rules, leading to inconsistent application state.
/// </para>
/// </remarks>
public interface IGlobalStoreRegistry
{
    /// <summary>
    /// Registers a global data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="store">The data store to register.</param>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Thrown when a store for type <typeparamref name="T"/> is already registered.</exception>
    /// <remarks>
    /// This method is called by <see cref="IDataStoreRegistrar"/> implementations during startup.
    /// Do NOT call from application code.
    /// </remarks>
    void RegisterGlobal<T>(IDataStore<T> store) where T : class;

    /// <summary>
    /// Resolves the global data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <returns>The registered global data store.</returns>
    /// <exception cref="GlobalStoreNotRegisteredException">Thrown when no store is registered for type <typeparamref name="T"/>.</exception>
    /// <remarks>
    /// INTERNAL INFRASTRUCTURE. Application code MUST use <see cref="IDataStores.GetGlobal{T}"/> instead.
    /// </remarks>
    IDataStore<T> ResolveGlobal<T>() where T : class;

    /// <summary>
    /// Tries to resolve the global data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="store">The resolved store, if found.</param>
    /// <returns><c>true</c> if a store was found; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// INTERNAL INFRASTRUCTURE. Application code MUST use <see cref="IDataStores.GetGlobal{T}"/> instead.
    /// </remarks>
    bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class;

    /// <summary>
    /// Gets all registered global stores that implement IAsyncInitializable.
    /// </summary>
    /// <returns>A collection of stores that require asynchronous initialization.</returns>
    /// <remarks>
    /// INTERNAL INFRASTRUCTURE. This method is called by <see cref="Bootstrap.DataStoreBootstrap"/> to initialize persistent stores.
    /// Do NOT call from application code.
    /// </remarks>
    IEnumerable<Persistence.IAsyncInitializable> GetInitializableGlobalStores();
}
