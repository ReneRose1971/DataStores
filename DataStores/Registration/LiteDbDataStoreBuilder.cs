using DataStores.Abstractions;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Registration;

/// <summary>
/// Builder for registering data stores with LiteDB persistence.
/// </summary>
/// <typeparam name="T">The type of items in the store. Must inherit from <see cref="EntityBase"/>.</typeparam>
/// <remarks>
/// <para>
/// LiteDB stores persist data to a serverless NoSQL database file with ACID transactions.
/// </para>
/// <para>
/// <b>Features:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Serverless - no installation required</description></item>
/// <item><description>Single file database</description></item>
/// <item><description>ACID transactions</description></item>
/// <item><description>Thread-safe</description></item>
/// <item><description>LINQ query support</description></item>
/// <item><description>Automatic ID assignment for new entities</description></item>
/// <item><description>Delta-based synchronization (INSERT/DELETE)</description></item>
/// </list>
/// <para>
/// <b>Collection Naming:</b>
/// </para>
/// <para>
/// The collection name is AUTOMATICALLY derived from typeof(T).Name.
/// This ensures consistency and eliminates manual naming errors.
/// </para>
/// <para>
/// Examples:
/// </para>
/// <list type="bullet">
/// <item><description>Order → "Order"</description></item>
/// <item><description>CustomerEntity → "CustomerEntity"</description></item>
/// <item><description>ProductDto → "ProductDto"</description></item>
/// </list>
/// <para>
/// <b>Persistence Strategy:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Load: Fetches all documents from collection</description></item>
/// <item><description>Save: Computes delta (INSERT new, DELETE removed)</description></item>
/// <item><description>Update single: Efficient single-entity update via LiteCollection.Update</description></item>
/// </list>
/// <para>
/// <b>Best for:</b> Medium to large datasets, offline-first applications, desktop applications.
/// </para>
/// <para>
/// <b>NOT recommended for:</b> Web applications with concurrent access, cloud-first scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple LiteDB store with defaults (auto-load, auto-save)
/// // Collection name is automatically "Order"
/// AddStore(new LiteDbDataStoreBuilder&lt;Order&gt;(
///     databasePath: "C:\\Data\\myapp.db"));
/// 
/// // Load-only store (manual save required)
/// // Collection name is automatically "Customer"
/// AddStore(new LiteDbDataStoreBuilder&lt;Customer&gt;(
///     databasePath: "C:\\Data\\myapp.db",
///     autoLoad: true,
///     autoSave: false));
/// 
/// // With custom comparer and UI-thread events
/// // Collection name is automatically "Invoice"
/// AddStore(new LiteDbDataStoreBuilder&lt;Invoice&gt;(
///     databasePath: "C:\\Data\\myapp.db",
///     comparer: new InvoiceNumberComparer(),
///     synchronizationContext: SynchronizationContext.Current));
/// </code>
/// </example>
public sealed class LiteDbDataStoreBuilder<T> : DataStoreBuilder<T> where T : EntityBase
{
    private readonly string _databasePath;
    private readonly bool _autoLoad;
    private readonly bool _autoSave;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteDbDataStoreBuilder{T}"/> class.
    /// </summary>
    /// <param name="databasePath">
    /// The full path to the LiteDB database file.
    /// The file will be created automatically if it does not exist.
    /// The directory will be created automatically if it does not exist.
    /// Multiple stores can share the same database file using different collections.
    /// </param>
    /// <param name="autoLoad">
    /// If true, data is loaded automatically during bootstrap via <see cref="Bootstrap.DataStoreBootstrap"/>.
    /// If false, the store starts empty and must be populated manually.
    /// Default is true.
    /// </param>
    /// <param name="autoSave">
    /// If true, changes are saved automatically on every Add, Remove, Clear, and PropertyChanged event.
    /// Saving is transactional and uses delta-synchronization (only INSERT/DELETE changed entities).
    /// If false, saving must be triggered manually.
    /// Default is true.
    /// </param>
    /// <param name="comparer">
    /// Optional equality comparer for items.
    /// Used for Contains and Remove operations.
    /// If null, uses EqualityComparer&lt;T&gt;.Default.
    /// </param>
    /// <param name="synchronizationContext">
    /// Optional synchronization context for event marshalling.
    /// If provided, Changed events are posted to this context (e.g., UI thread).
    /// If null, events are raised synchronously on the calling thread.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databasePath"/> is null or empty.</exception>
    /// <remarks>
    /// <para>
    /// <b>Collection Name:</b> Automatically set to typeof(T).Name internally.
    /// </para>
    /// <para>
    /// <b>Shared Database:</b> Multiple stores can use the same database file:
    /// </para>
    /// <code>
    /// // Same database, different collections
    /// AddStore(new LiteDbDataStoreBuilder&lt;Order&gt;("myapp.db"));      // → "Order" collection
    /// AddStore(new LiteDbDataStoreBuilder&lt;Customer&gt;("myapp.db"));   // → "Customer" collection
    /// AddStore(new LiteDbDataStoreBuilder&lt;Product&gt;("myapp.db"));    // → "Product" collection
    /// </code>
    /// <para>
    /// <b>ID Assignment:</b> Entities with Id = 0 are considered new and receive auto-assigned IDs.
    /// </para>
    /// <para>
    /// <b>Delta Sync:</b> SaveAllAsync computes the difference between store and database:
    /// </para>
    /// <list type="bullet">
    /// <item><description>INSERT: Entities in store but not in database</description></item>
    /// <item><description>DELETE: Entities in database but not in store</description></item>
    /// <item><description>No UPDATE: Use UpdateSingleAsync for property changes</description></item>
    /// </list>
    /// </remarks>
    public LiteDbDataStoreBuilder(
        string databasePath,
        bool autoLoad = true,
        bool autoSave = true,
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be null or empty.", nameof(databasePath));
        }

        _databasePath = databasePath;
        _autoLoad = autoLoad;
        _autoSave = autoSave;
        Comparer = comparer;
        SynchronizationContext = synchronizationContext;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Creates a <see cref="PersistentStoreDecorator{T}"/> wrapping an <see cref="InMemoryDataStore{T}"/>
    /// with a <see cref="LiteDbPersistenceStrategy{T}"/>.
    /// </para>
    /// <para>
    /// Collection name is automatically set to typeof(T).Name.
    /// </para>
    /// <para>
    /// The decorator handles auto-load and auto-save behavior transparently.
    /// If no explicit comparer was provided, automatically resolves an appropriate comparer via
    /// <see cref="IEqualityComparerService"/>.
    /// </para>
    /// </remarks>
    internal override void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // Resolve comparer automatically if not explicitly provided
        var effectiveComparer = Comparer;
        if (effectiveComparer == null)
        {
            var comparerService = serviceProvider.GetRequiredService<IEqualityComparerService>();
            effectiveComparer = comparerService.GetComparer<T>();
        }

        // Resolve IDataStoreDiffService for LiteDB strategy
        var diffService = serviceProvider.GetRequiredService<IDataStoreDiffService>();

        var collectionName = typeof(T).Name;
        var strategy = new LiteDbPersistenceStrategy<T>(_databasePath, collectionName, diffService);
        var innerStore = new InMemoryDataStore<T>(effectiveComparer, SynchronizationContext);
        var decorator = new PersistentStoreDecorator<T>(innerStore, strategy, _autoLoad, _autoSave);
        registry.RegisterGlobal(decorator);
    }
}
