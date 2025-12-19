using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Persistence;

/// <summary>
/// Provides extension methods for registering persistent stores.
/// </summary>
public static class PersistentStoreRegistrationExtensions
{
    /// <summary>
    /// Registers a persistent global data store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="registry">The global store registry.</param>
    /// <param name="createInnerStore">Factory function to create the inner in-memory store.</param>
    /// <param name="strategy">The persistence strategy.</param>
    /// <param name="autoLoad">If true, data will be loaded during initialization.</param>
    /// <param name="autoSaveOnChange">If true, data will be saved automatically on changes.</param>
    /// <returns>The created persistent store decorator.</returns>
    public static PersistentStoreDecorator<T> RegisterPersistent<T>(
        this IGlobalStoreRegistry registry,
        Func<InMemoryDataStore<T>> createInnerStore,
        IPersistenceStrategy<T> strategy,
        bool autoLoad = true,
        bool autoSaveOnChange = true) where T : class
    {
        var innerStore = createInnerStore();
        var decorator = new PersistentStoreDecorator<T>(innerStore, strategy, autoLoad, autoSaveOnChange);
        registry.RegisterGlobal(decorator);
        return decorator;
    }

    /// <summary>
    /// Registers a persistent global data store with default inner store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="registry">The global store registry.</param>
    /// <param name="strategy">The persistence strategy.</param>
    /// <param name="autoLoad">If true, data will be loaded during initialization.</param>
    /// <param name="autoSaveOnChange">If true, data will be saved automatically on changes.</param>
    /// <returns>The created persistent store decorator.</returns>
    public static PersistentStoreDecorator<T> RegisterPersistent<T>(
        this IGlobalStoreRegistry registry,
        IPersistenceStrategy<T> strategy,
        bool autoLoad = true,
        bool autoSaveOnChange = true) where T : class
    {
        return RegisterPersistent(
            registry,
            () => new InMemoryDataStore<T>(),
            strategy,
            autoLoad,
            autoSaveOnChange);
    }
}
