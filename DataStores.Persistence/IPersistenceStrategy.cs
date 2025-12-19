namespace DataStores.Persistence;

/// <summary>
/// Defines a strategy for persisting and loading data.
/// </summary>
/// <typeparam name="T">The type of items to persist.</typeparam>
public interface IPersistenceStrategy<T> where T : class
{
    /// <summary>
    /// Loads all items from the persistence store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of loaded items.</returns>
    Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all items to the persistence store.
    /// </summary>
    /// <param name="items">The items to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default);
}
