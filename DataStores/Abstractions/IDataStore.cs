namespace DataStores.Abstractions;

/// <summary>
/// Represents a data store that holds a collection of items of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of items in the store.</typeparam>
public interface IDataStore<T> where T : class
{
    /// <summary>
    /// Gets the read-only collection of items in the store.
    /// </summary>
    IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Occurs when the data store changes.
    /// </summary>
    event EventHandler<DataStoreChangedEventArgs<T>> Changed;

    /// <summary>
    /// Adds an item to the store.
    /// </summary>
    /// <param name="item">The item to add.</param>
    void Add(T item);

    /// <summary>
    /// Adds multiple items to the store in a single operation.
    /// </summary>
    /// <param name="items">The items to add.</param>
    void AddRange(IEnumerable<T> items);

    /// <summary>
    /// Removes an item from the store.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns><c>true</c> if the item was removed; otherwise, <c>false</c>.</returns>
    bool Remove(T item);

    /// <summary>
    /// Removes all items from the store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Determines whether the store contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns><c>true</c> if the item is found; otherwise, <c>false</c>.</returns>
    bool Contains(T item);
}
