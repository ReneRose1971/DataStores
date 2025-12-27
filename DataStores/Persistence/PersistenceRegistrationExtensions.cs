using DataStores.Abstractions;
using DataStores.Runtime;
using System.Text.Json;

namespace DataStores.Persistence;

/// <summary>
/// REGISTRATION EXTENSIONS for convenient persistent store setup.
/// Use ONLY within <see cref="IDataStoreRegistrar.Register"/> implementations during startup.
/// </summary>
/// <remarks>
/// <para>
/// These extensions simplify persistent store registration by encapsulating the decorator pattern.
/// </para>
/// <para>
/// Do NOT use in application feature code. Use <see cref="IDataStores"/> facade to access stores after registration.
/// </para>
/// </remarks>
public static class PersistenceRegistrationExtensions
{
    /// <summary>
    /// Registers a global DataStore with JSON file persistence.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="registry">The GlobalStoreRegistry instance.</param>
    /// <param name="jsonFilePath">The full path to the JSON file.</param>
    /// <param name="jsonOptions">Optional JSON serialization options.</param>
    /// <param name="autoLoad">If true, data is loaded automatically during bootstrap.</param>
    /// <param name="autoSave">If true, changes are saved automatically.</param>
    /// <param name="comparer">Optional equality comparer for items.</param>
    /// <param name="synchronizationContext">Optional SynchronizationContext for events.</param>
    /// <returns>The registry instance for fluent API.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registry or jsonFilePath is null.</exception>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Thrown when a store for type T is already registered.</exception>
    /// <remarks>
    /// Use ONLY within <see cref="IDataStoreRegistrar.Register"/> implementations.
    /// </remarks>
    /// <example>
    /// <code>
    /// public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    /// {
    ///     registry.RegisterGlobalWithJsonFile&lt;Customer&gt;(
    ///         "C:\\Data\\customers.json",
    ///         autoLoad: true,
    ///         autoSave: true);
    /// }
    /// </code>
    /// </example>
    public static IGlobalStoreRegistry RegisterGlobalWithJsonFile<T>(
        this IGlobalStoreRegistry registry,
        string jsonFilePath,
        JsonSerializerOptions? jsonOptions = null,
        bool autoLoad = true,
        bool autoSave = true,
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null) where T : class
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }
        
        if (string.IsNullOrWhiteSpace(jsonFilePath))
        {
            throw new ArgumentNullException(nameof(jsonFilePath));
        }

        var strategy = new JsonFilePersistenceStrategy<T>(jsonFilePath, jsonOptions);
        var innerStore = new InMemoryDataStore<T>(comparer, synchronizationContext);
        var persistentStore = new PersistentStoreDecorator<T>(
            innerStore,
            strategy,
            autoLoad,
            autoSave);

        registry.RegisterGlobal(persistentStore);
        return registry;
    }

    /// <summary>
    /// Registers a global DataStore with LiteDB persistence.
    /// </summary>
    /// <typeparam name="T">The type of items in the store. Must inherit from <see cref="EntityBase"/>.</typeparam>
    /// <param name="registry">The GlobalStoreRegistry instance.</param>
    /// <param name="databasePath">The full path to the LiteDB database file.</param>
    /// <param name="collectionName">The collection name in the database. If null, uses the type name.</param>
    /// <param name="diffService">The diff service for computing changes between store and database.</param>
    /// <param name="autoLoad">If true, data is loaded automatically during bootstrap.</param>
    /// <param name="autoSave">If true, changes are saved automatically.</param>
    /// <param name="comparer">Optional equality comparer for items.</param>
    /// <param name="synchronizationContext">Optional SynchronizationContext for events.</param>
    /// <returns>The registry instance for fluent API.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registry, databasePath, or diffService is null.</exception>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Thrown when a store for type T is already registered.</exception>
    /// <remarks>
    /// <para>
    /// <b>DEPRECATED:</b> Use <see cref="Registration.LiteDbDataStoreBuilder{T}"/> instead for automatic DI-based diff service resolution.
    /// </para>
    /// <para>
    /// Use ONLY within <see cref="IDataStoreRegistrar.Register"/> implementations.
    /// This method requires manual IDataStoreDiffService injection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    /// {
    ///     var diffService = serviceProvider.GetRequiredService&lt;IDataStoreDiffService&gt;();
    ///     registry.RegisterGlobalWithLiteDb&lt;Customer&gt;(
    ///         "C:\\Data\\myapp.db",
    ///         collectionName: "customers",
    ///         diffService: diffService,
    ///         autoLoad: true,
    ///         autoSave: true);
    /// }
    /// </code>
    /// </example>
    [Obsolete("Use LiteDbDataStoreBuilder instead for automatic DI-based diff service resolution.")]
    public static IGlobalStoreRegistry RegisterGlobalWithLiteDb<T>(
        this IGlobalStoreRegistry registry,
        string databasePath,
        string? collectionName,
        IDataStoreDiffService diffService,
        bool autoLoad = true,
        bool autoSave = true,
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null) where T : EntityBase
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }
        
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentNullException(nameof(databasePath));
        }

        if (diffService == null)
        {
            throw new ArgumentNullException(nameof(diffService));
        }

        var strategy = new LiteDbPersistenceStrategy<T>(databasePath, collectionName, diffService);
        var innerStore = new InMemoryDataStore<T>(comparer, synchronizationContext);
        var persistentStore = new PersistentStoreDecorator<T>(
            innerStore,
            strategy,
            autoLoad,
            autoSave);

        registry.RegisterGlobal(persistentStore);
        return registry;
    }

    /// <summary>
    /// Registers a global DataStore with custom persistence strategy.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="registry">The GlobalStoreRegistry instance.</param>
    /// <param name="strategy">The persistence strategy to use.</param>
    /// <param name="autoLoad">If true, data is loaded automatically during bootstrap.</param>
    /// <param name="autoSave">If true, changes are saved automatically.</param>
    /// <param name="comparer">Optional equality comparer for items.</param>
    /// <param name="synchronizationContext">Optional SynchronizationContext for events.</param>
    /// <returns>The registry instance for fluent API.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registry or strategy is null.</exception>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Thrown when a store for type T is already registered.</exception>
    /// <remarks>
    /// Use ONLY within <see cref="IDataStoreRegistrar.Register"/> implementations.
    /// Allows custom persistence strategies for cloud storage, databases, or other mechanisms.
    /// </remarks>
    /// <example>
    /// <code>
    /// public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    /// {
    ///     var customStrategy = new MyCustomPersistenceStrategy&lt;Customer&gt;();
    ///     registry.RegisterGlobalWithPersistence(
    ///         customStrategy,
    ///         autoLoad: true,
    ///         autoSave: true);
    /// }
    /// </code>
    /// </example>
    public static IGlobalStoreRegistry RegisterGlobalWithPersistence<T>(
        this IGlobalStoreRegistry registry,
        IPersistenceStrategy<T> strategy,
        bool autoLoad = true,
        bool autoSave = true,
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null) where T : class
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }
        
        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        var innerStore = new InMemoryDataStore<T>(comparer, synchronizationContext);
        var persistentStore = new PersistentStoreDecorator<T>(
            innerStore,
            strategy,
            autoLoad,
            autoSave);

        registry.RegisterGlobal(persistentStore);
        return registry;
    }
}
