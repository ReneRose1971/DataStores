using DataStores.Abstractions;

namespace DataStores.Extensions;

/// <summary>
/// Extension methods for bidirectional synchronization between data stores.
/// </summary>
/// <remarks>
/// <para>
/// These extensions enable automatic synchronization between two data stores,
/// propagating changes (Add, Remove, Clear) from one store to another.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Global ↔ Local store synchronization</description></item>
/// <item><description>Master-Detail relationships</description></item>
/// <item><description>Real-time data mirroring</description></item>
/// <item><description>Undo/Redo scenarios with snapshots</description></item>
/// </list>
/// <para>
/// <b>Important:</b> Synchronization uses the configured IEqualityComparer to prevent duplicates.
/// The InMemoryDataStore's duplicate prevention ensures consistency.
/// </para>
/// </remarks>
public static class DataStoreSynchronizationExtensions
{
    /// <summary>
    /// Creates a bidirectional synchronization between two data stores.
    /// </summary>
    /// <typeparam name="T">The type of items in the stores.</typeparam>
    /// <param name="source">The source data store.</param>
    /// <param name="target">The target data store to synchronize with.</param>
    /// <param name="comparerService">The comparer service for automatic comparer resolution.</param>
    /// <param name="options">Optional configuration for synchronization behavior.</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that stops synchronization when disposed.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when source, target, or comparerService is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>Synchronization Behavior:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Add:</b> Items are added to the other store if they don't already exist (checked via comparer)</description></item>
    /// <item><description><b>Remove:</b> Items are removed from the other store</description></item>
    /// <item><description><b>Clear:</b> Clear operation is propagated to the other store</description></item>
    /// </list>
    /// <para>
    /// <b>Important:</b> This is a simple synchronization mechanism - it only handles Add and Remove operations.
    /// Updates to existing items (via AddOrReplace) are NOT synchronized. For update scenarios, remove and re-add the item.
    /// </para>
    /// <para>
    /// <b>Duplicate Prevention:</b>
    /// Uses the configured IEqualityComparer to check if an item already exists before adding.
    /// If an item exists (according to comparer), the Add operation is silently skipped.
    /// </para>
    /// <para>
    /// <b>Initial Sync:</b>
    /// If <see cref="SyncOptions.InitialSync"/> is true, items from source missing in target
    /// are added to target during setup.
    /// </para>
    /// <para>
    /// <b>Reentrancy Protection:</b>
    /// Uses a guard flag to prevent infinite loops in bidirectional synchronization.
    /// When syncing from source to target, the resulting target event is ignored.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Bidirectional sync between global and local store
    /// var globalStore = stores.GetGlobal&lt;Product&gt;();
    /// var localStore = stores.CreateLocal&lt;Product&gt;();
    /// 
    /// var syncHandle = globalStore.SynchronizeWith(
    ///     localStore,
    ///     comparerService,
    ///     new SyncOptions
    ///     {
    ///         SyncSourceToTarget = true,
    ///         SyncTargetToSource = true,
    ///         InitialSync = true
    ///     });
    /// 
    /// // Add to global - automatically synced to local
    /// globalStore.Add(new Product { Id = 1, Name = "Laptop" });
    /// Assert.Single(localStore.Items); // Synced!
    /// 
    /// // Add to local - automatically synced to global
    /// localStore.Add(new Product { Id = 2, Name = "Mouse" });
    /// Assert.Equal(2, globalStore.Items.Count); // Synced!
    /// 
    /// // Stop synchronization
    /// syncHandle.Dispose();
    /// </code>
    /// </example>
    public static IDisposable SynchronizeWith<T>(
        this IDataStore<T> source,
        IDataStore<T> target,
        IEqualityComparerService comparerService,
        SyncOptions? options = null) where T : class
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (comparerService == null)
        {
            throw new ArgumentNullException(nameof(comparerService));
        }

        options ??= new SyncOptions();

        // Resolve comparer
        var comparer = options.Comparer as IEqualityComparer<T> ?? comparerService.GetComparer<T>();

        // Reentrancy guard to prevent infinite loops in bidirectional sync
        bool isSyncing = false;

        // Helper: Check if item exists in store using comparer
        bool ExistsIn(IDataStore<T> store, T item)
        {
            return store.Items.Any(x => comparer.Equals(x, item));
        }

        // Handler for source → target sync
        void OnSourceChanged(object? sender, DataStoreChangedEventArgs<T> e)
        {
            if (!options.SyncSourceToTarget || isSyncing)
            {
                return;
            }

            try
            {
                isSyncing = true;

                switch (e.ChangeType)
                {
                    case DataStoreChangeType.Add:
                    case DataStoreChangeType.BulkAdd:
                        foreach (var item in e.AffectedItems)
                        {
                            if (!ExistsIn(target, item))
                            {
                                try
                                {
                                    target.Add(item);
                                }
                                catch (InvalidOperationException)
                                {
                                    // Item already exists - silently ignore
                                }
                            }
                        }
                        break;

                    case DataStoreChangeType.Remove:
                        foreach (var item in e.AffectedItems)
                        {
                            target.Remove(item);
                        }
                        break;

                    case DataStoreChangeType.Clear:
                        target.Clear();
                        break;
                }
            }
            finally
            {
                isSyncing = false;
            }
        }

        // Handler for target → source sync (bidirectional)
        void OnTargetChanged(object? sender, DataStoreChangedEventArgs<T> e)
        {
            if (!options.SyncTargetToSource || isSyncing)
            {
                return;
            }

            try
            {
                isSyncing = true;

                switch (e.ChangeType)
                {
                    case DataStoreChangeType.Add:
                    case DataStoreChangeType.BulkAdd:
                        foreach (var item in e.AffectedItems)
                        {
                            if (!ExistsIn(source, item))
                            {
                                try
                                {
                                    source.Add(item);
                                }
                                catch (InvalidOperationException)
                                {
                                    // Item already exists - silently ignore
                                }
                            }
                        }
                        break;

                    case DataStoreChangeType.Remove:
                        foreach (var item in e.AffectedItems)
                        {
                            source.Remove(item);
                        }
                        break;

                    case DataStoreChangeType.Clear:
                        source.Clear();
                        break;
                }
            }
            finally
            {
                isSyncing = false;
            }
        }

        // Subscribe to events
        source.Changed += OnSourceChanged;
        if (options.SyncTargetToSource)
        {
            target.Changed += OnTargetChanged;
        }

        // Perform initial synchronization if requested
        if (options.InitialSync)
        {
            var missingInTarget = source.Items
                .Where(item => !ExistsIn(target, item))
                .ToList();

            foreach (var item in missingInTarget)
            {
                try
                {
                    target.Add(item);
                }
                catch (InvalidOperationException)
                {
                    // Item already exists - silently ignore
                }
            }
        }

        // Return disposable to stop synchronization
        return new SyncSubscription(() =>
        {
            source.Changed -= OnSourceChanged;
            target.Changed -= OnTargetChanged;
        });
    }

    /// <summary>
    /// Internal helper class for unsubscribing from sync.
    /// </summary>
    private sealed class SyncSubscription : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public SyncSubscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe();
                _disposed = true;
            }
        }
    }
}
