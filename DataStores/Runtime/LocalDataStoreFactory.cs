using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// Default implementation of <see cref="ILocalDataStoreFactory"/>.
/// </summary>
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
