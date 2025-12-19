namespace DataStores.Abstractions;

/// <summary>
/// Provides a facade for accessing global and creating local data stores.
/// </summary>
public interface IDataStores
{
    /// <summary>
    /// Gets the globally registered data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <returns>The global data store.</returns>
    /// <exception cref="GlobalStoreNotRegisteredException">Thrown when no global store is registered for type <typeparamref name="T"/>.</exception>
    IDataStore<T> GetGlobal<T>() where T : class;

    /// <summary>
    /// Creates a new local in-memory data store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="comparer">Optional equality comparer for items.</param>
    /// <returns>A new local in-memory data store.</returns>
    IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class;

    /// <summary>
    /// Creates a new local in-memory data store and populates it with a snapshot from the global store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="predicate">Optional filter predicate for the snapshot.</param>
    /// <param name="comparer">Optional equality comparer for items.</param>
    /// <returns>A new local in-memory data store containing the filtered snapshot.</returns>
    /// <exception cref="GlobalStoreNotRegisteredException">Thrown when no global store is registered for type <typeparamref name="T"/>.</exception>
    IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
        Func<T, bool>? predicate = null,
        IEqualityComparer<T>? comparer = null) where T : class;
}
