using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// Factory for creating local in-memory data stores.
/// </summary>
public interface ILocalDataStoreFactory
{
    /// <summary>
    /// Creates a new local in-memory data store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="comparer">Optional equality comparer.</param>
    /// <param name="context">Optional synchronization context.</param>
    /// <returns>A new in-memory data store.</returns>
    InMemoryDataStore<T> CreateLocal<T>(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? context = null) where T : class;
}

/// <summary>
/// Default implementation of <see cref="ILocalDataStoreFactory"/>.
/// </summary>
public class LocalDataStoreFactory : ILocalDataStoreFactory
{
    /// <inheritdoc/>
    public InMemoryDataStore<T> CreateLocal<T>(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? context = null) where T : class
    {
        return new InMemoryDataStore<T>(comparer, context);
    }
}
