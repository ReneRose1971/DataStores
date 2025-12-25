using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// INTERNAL INFRASTRUCTURE. Do NOT use directly in application code.
/// Default implementation of <see cref="ILocalDataStoreFactory"/>.
/// </summary>
/// <remarks>
/// Application code MUST use <see cref="IDataStores.CreateLocal{T}"/> instead.
/// </remarks>
public sealed class LocalDataStoreFactory : ILocalDataStoreFactory
{
    /// <inheritdoc/>
    public InMemoryDataStore<T> CreateLocal<T>(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? context = null) where T : class
    {
        return new InMemoryDataStore<T>(comparer, context);
    }
}
