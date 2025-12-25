using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// INTERNAL INFRASTRUCTURE. Do NOT use directly in application code.
/// Factory for creating local in-memory data stores.
/// </summary>
/// <remarks>
/// Application code MUST use <see cref="IDataStores.CreateLocal{T}"/> instead.
/// This interface is used internally by <see cref="DataStoresFacade"/>.
/// </remarks>
public interface ILocalDataStoreFactory
{
    /// <summary>
    /// Creates a new local in-memory data store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="comparer">Optional equality comparer.</param>
    /// <param name="context">Optional synchronization context.</param>
    /// <returns>A new in-memory data store.</returns>
    /// <remarks>
    /// INTERNAL INFRASTRUCTURE. Application code MUST use <see cref="IDataStores.CreateLocal{T}"/> instead.
    /// </remarks>
    InMemoryDataStore<T> CreateLocal<T>(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? context = null) where T : class;
}
