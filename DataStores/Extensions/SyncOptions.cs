namespace DataStores.Extensions;

/// <summary>
/// Configuration options for data store synchronization.
/// </summary>
public sealed class SyncOptions
{
    /// <summary>
    /// Gets or sets whether changes from source should be synchronized to target.
    /// Default is true.
    /// </summary>
    public bool SyncSourceToTarget { get; set; } = true;

    /// <summary>
    /// Gets or sets whether changes from target should be synchronized to source.
    /// Default is true (bidirectional sync).
    /// </summary>
    public bool SyncTargetToSource { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to perform an initial synchronization on setup.
    /// Default is true. Items from source missing in target are added to target.
    /// </summary>
    public bool InitialSync { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional custom comparer for duplicate detection.
    /// If null, uses automatic resolution via IEqualityComparerService.
    /// </summary>
    /// <remarks>
    /// This property is weakly typed to allow configuration without generics.
    /// The actual comparer type is validated during SynchronizeWith() call.
    /// </remarks>
    public object? Comparer { get; set; }
}
