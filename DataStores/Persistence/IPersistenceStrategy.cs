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

    /// <summary>
    /// Updates a single item in the persistence store.
    /// </summary>
    /// <param name="item">The item to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// <b>JSON Strategy:</b> Uses ItemsProvider to get current list, then delegates to SaveAllAsync (atomic write).
    /// </para>
    /// <para>
    /// <b>LiteDB Strategy:</b> Uses LiteCollection.Update for efficient single-item updates.
    /// </para>
    /// </remarks>
    Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Setzt die Callback-Funktion für den Zugriff auf die aktuelle Items-Liste des Stores.
    /// </summary>
    /// <param name="itemsProvider">Funktion, die die aktuelle Items-Liste zurückgibt.</param>
    /// <remarks>
    /// <para>
    /// Wird vom PersistentStoreDecorator im Konstruktor aufgerufen, um der Strategie
    /// Zugriff auf die aktuelle Items-Liste zu geben.
    /// </para>
    /// <para>
    /// <b>JSON-Strategie:</b> Benötigt den Provider für UpdateSingleAsync (atomic write).
    /// </para>
    /// <para>
    /// <b>LiteDB-Strategie:</b> Ignoriert den Provider (nutzt collection.Update).
    /// </para>
    /// </remarks>
    void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider);
}
