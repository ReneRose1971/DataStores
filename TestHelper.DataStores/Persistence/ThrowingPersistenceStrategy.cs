using DataStores.Persistence;

namespace TestHelper.DataStores.Persistence;

/// <summary>
/// Persistence strategy that throws exceptions to simulate error conditions.
/// Useful for testing error handling and recovery logic.
/// </summary>
public class ThrowingPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly bool _throwOnLoad;
    private readonly bool _throwOnSave;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowingPersistenceStrategy{T}"/> class.
    /// </summary>
    /// <param name="throwOnLoad">If true, LoadAllAsync will throw an exception.</param>
    /// <param name="throwOnSave">If true, SaveAllAsync will throw an exception.</param>
    public ThrowingPersistenceStrategy(bool throwOnLoad, bool throwOnSave)
    {
        _throwOnLoad = throwOnLoad;
        _throwOnSave = throwOnSave;
    }

    public Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (_throwOnLoad)
        {
            throw new InvalidOperationException("Load failed");
        }

        return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
    }

    public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        return _throwOnSave ? throw new InvalidOperationException("Save failed") : Task.CompletedTask;
    }

    public Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default)
    {
        // Throwing strategy: No-Op (could be extended with throwOnUpdate if needed)
        return Task.CompletedTask;
    }

    public void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider)
    {
        // No-Op
    }
}
