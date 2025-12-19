namespace DataStores.Abstractions;

/// <summary>
/// Manages the registration and resolution of global data stores.
/// </summary>
public interface IGlobalStoreRegistry
{
    /// <summary>
    /// Registers a global data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="store">The data store to register.</param>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Thrown when a store for type <typeparamref name="T"/> is already registered.</exception>
    void RegisterGlobal<T>(IDataStore<T> store) where T : class;

    /// <summary>
    /// Resolves the global data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <returns>The registered global data store.</returns>
    /// <exception cref="GlobalStoreNotRegisteredException">Thrown when no store is registered for type <typeparamref name="T"/>.</exception>
    IDataStore<T> ResolveGlobal<T>() where T : class;

    /// <summary>
    /// Tries to resolve the global data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="store">The resolved store, if found.</param>
    /// <returns><c>true</c> if a store was found; otherwise, <c>false</c>.</returns>
    bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class;
}
