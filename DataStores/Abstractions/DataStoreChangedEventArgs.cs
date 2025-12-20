namespace DataStores.Abstractions;

/// <summary>
/// Specifies the type of change that occurred in a data store.
/// </summary>
public enum DataStoreChangeType
{
    /// <summary>A single item was added.</summary>
    Add,
    /// <summary>Multiple items were added in a bulk operation.</summary>
    BulkAdd,
    /// <summary>A single item was removed.</summary>
    Remove,
    /// <summary>All items were cleared.</summary>
    Clear,
    /// <summary>The entire collection was reset.</summary>
    Reset
}

/// <summary>
/// Provides data for the <see cref="IDataStore{T}.Changed"/> event.
/// </summary>
/// <typeparam name="T">The type of items in the data store.</typeparam>
public class DataStoreChangedEventArgs<T> : EventArgs
{
    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public DataStoreChangeType ChangeType { get; }

    /// <summary>
    /// Gets the items affected by the change, if applicable.
    /// </summary>
    public IReadOnlyList<T> AffectedItems { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreChangedEventArgs{T}"/> class.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <param name="affectedItems">The items affected by the change.</param>
    public DataStoreChangedEventArgs(DataStoreChangeType changeType, IReadOnlyList<T> affectedItems)
    {
        ChangeType = changeType;
        AffectedItems = affectedItems ?? Array.Empty<T>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreChangedEventArgs{T}"/> class for a single item.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <param name="item">The single item affected.</param>
    public DataStoreChangedEventArgs(DataStoreChangeType changeType, T item)
        : this(changeType, new[] { item })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreChangedEventArgs{T}"/> class with no affected items.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    public DataStoreChangedEventArgs(DataStoreChangeType changeType)
        : this(changeType, Array.Empty<T>())
    {
    }
}
