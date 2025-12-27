using DataStores.Abstractions;
using DataStores.Persistence;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Registration;

/// <summary>
/// Builder for registering data stores with JSON file persistence.
/// </summary>
/// <typeparam name="T">The type of items in the store. Must be a reference type.</typeparam>
/// <remarks>
/// <para>
/// JSON stores persist data to a single JSON file with human-readable, indented formatting.
/// </para>
/// <para>
/// <b>Features:</b>
/// </para>
/// <list type="bullet">
/// <item><description>UTF-8 encoding</description></item>
/// <item><description>Indented JSON format (easy to read and edit)</description></item>
/// <item><description>Automatic directory creation</description></item>
/// <item><description>Optional auto-load on startup</description></item>
/// <item><description>Optional auto-save on changes</description></item>
/// <item><description>PropertyChanged tracking for INotifyPropertyChanged entities</description></item>
/// </list>
/// <para>
/// <b>Persistence Strategy:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Load: Deserializes entire file</description></item>
/// <item><description>Save: Serializes entire collection atomically</description></item>
/// <item><description>Update single: Saves entire collection (no partial updates)</description></item>
/// </list>
/// <para>
/// <b>Best for:</b> Small to medium datasets (&lt; 10,000 items), configuration files, user preferences.
/// </para>
/// <para>
/// <b>NOT recommended for:</b> Large datasets, high-frequency writes, concurrent access from multiple processes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple JSON store with defaults (auto-load, auto-save)
/// AddStore(new JsonDataStoreBuilder&lt;Customer&gt;(
///     filePath: "C:\\Data\\customers.json"));
/// 
/// // Load-only store (manual save required)
/// AddStore(new JsonDataStoreBuilder&lt;Settings&gt;(
///     filePath: "C:\\Data\\settings.json",
///     autoLoad: true,
///     autoSave: false));
/// 
/// // With custom comparer and UI-thread events
/// AddStore(new JsonDataStoreBuilder&lt;Product&gt;(
///     filePath: "C:\\Data\\products.json",
///     comparer: new ProductIdComparer(),
///     synchronizationContext: SynchronizationContext.Current));
/// </code>
/// </example>
public sealed class JsonDataStoreBuilder<T> : DataStoreBuilder<T> where T : class
{
    private readonly string _filePath;
    private readonly bool _autoLoad;
    private readonly bool _autoSave;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDataStoreBuilder{T}"/> class.
    /// </summary>
    /// <param name="filePath">
    /// The full path to the JSON file.
    /// The file will be created automatically if it does not exist.
    /// The directory will be created automatically if it does not exist.
    /// </param>
    /// <param name="autoLoad">
    /// If true, data is loaded automatically during bootstrap via <see cref="Bootstrap.DataStoreBootstrap"/>.
    /// If false, the store starts empty and must be populated manually.
    /// Default is true.
    /// </param>
    /// <param name="autoSave">
    /// If true, changes are saved automatically on every Add, Remove, Clear, and PropertyChanged event.
    /// Saving is asynchronous and does not block operations.
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
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
    /// <remarks>
    /// <para>
    /// The file path should be an absolute path to avoid ambiguity.
    /// Relative paths are resolved relative to the application's working directory.
    /// </para>
    /// <para>
    /// <b>Auto-Load Behavior:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>If file does not exist: Store starts empty</description></item>
    /// <item><description>If file is empty: Store starts empty</description></item>
    /// <item><description>If file is invalid JSON: Store starts empty (error is caught)</description></item>
    /// </list>
    /// </remarks>
    public JsonDataStoreBuilder(
        string filePath,
        bool autoLoad = true,
        bool autoSave = true,
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        _filePath = filePath;
        _autoLoad = autoLoad;
        _autoSave = autoSave;
        Comparer = comparer;
        SynchronizationContext = synchronizationContext;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Creates a <see cref="PersistentStoreDecorator{T}"/> wrapping an <see cref="InMemoryDataStore{T}"/>
    /// with a <see cref="JsonFilePersistenceStrategy{T}"/>.
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

        var strategy = new JsonFilePersistenceStrategy<T>(_filePath);
        var innerStore = new InMemoryDataStore<T>(effectiveComparer, SynchronizationContext);
        var decorator = new PersistentStoreDecorator<T>(innerStore, strategy, _autoLoad, _autoSave);
        registry.RegisterGlobal(decorator);
    }
}
