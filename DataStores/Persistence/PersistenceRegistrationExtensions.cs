using DataStores.Abstractions;
using DataStores.Runtime;
using System.Text.Json;

namespace DataStores.Persistence;

/// <summary>
/// Erweiterungsmethoden für die einfache Registrierung von persistenten Stores.
/// </summary>
public static class PersistenceRegistrationExtensions
{
    /// <summary>
    /// Registriert einen globalen DataStore mit JSON-Datei-Persistierung.
    /// </summary>
    /// <typeparam name="T">Der Typ der Elemente im Store.</typeparam>
    /// <param name="registry">Die GlobalStoreRegistry-Instanz.</param>
    /// <param name="jsonFilePath">Der vollständige Pfad zur JSON-Datei.</param>
    /// <param name="jsonOptions">Optionale JSON-Serialisierungsoptionen.</param>
    /// <param name="autoLoad">Wenn true, werden Daten beim Bootstrap automatisch geladen.</param>
    /// <param name="autoSave">Wenn true, werden Änderungen automatisch gespeichert.</param>
    /// <param name="comparer">Optionaler Equality-Comparer für Elemente.</param>
    /// <param name="synchronizationContext">Optionaler SynchronizationContext für Events.</param>
    /// <returns>Die Registry-Instanz für Fluent-API.</returns>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn registry oder jsonFilePath null ist.</exception>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Wird ausgelöst, wenn bereits ein Store für Typ T registriert wurde.</exception>
    /// <remarks>
    /// <para>
    /// Diese Methode erstellt einen InMemoryDataStore, dekoriert ihn mit Persistierung
    /// und registriert ihn als globalen Store.
    /// </para>
    /// <example>
    /// <code>
    /// registry.RegisterGlobalWithJsonFile&lt;Customer&gt;(
    ///     "C:\\Data\\customers.json",
    ///     autoLoad: true,
    ///     autoSave: true);
    /// </code>
    /// </example>
    /// </remarks>
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
            throw new ArgumentNullException(nameof(registry));
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new ArgumentNullException(nameof(jsonFilePath));

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
    /// Registriert einen globalen DataStore mit LiteDB-Persistierung.
    /// </summary>
    /// <typeparam name="T">Der Typ der Elemente im Store.</typeparam>
    /// <param name="registry">Die GlobalStoreRegistry-Instanz.</param>
    /// <param name="databasePath">Der vollständige Pfad zur LiteDB-Datenbankdatei.</param>
    /// <param name="collectionName">
    /// Der Name der Collection in der Datenbank. 
    /// Wenn null, wird der Typname verwendet.
    /// </param>
    /// <param name="autoLoad">Wenn true, werden Daten beim Bootstrap automatisch geladen.</param>
    /// <param name="autoSave">Wenn true, werden Änderungen automatisch gespeichert.</param>
    /// <param name="comparer">Optionaler Equality-Comparer für Elemente.</param>
    /// <param name="synchronizationContext">Optionaler SynchronizationContext für Events.</param>
    /// <returns>Die Registry-Instanz für Fluent-API.</returns>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn registry oder databasePath null ist.</exception>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Wird ausgelöst, wenn bereits ein Store für Typ T registriert wurde.</exception>
    /// <remarks>
    /// <para>
    /// Diese Methode erstellt einen InMemoryDataStore, dekoriert ihn mit LiteDB-Persistierung
    /// und registriert ihn als globalen Store.
    /// </para>
    /// <para>
    /// <b>Hinweis:</b> Die aktuelle Implementierung verwendet ein Mock-System.
    /// Für Produktionsumgebungen sollte das LiteDB NuGet-Paket installiert werden.
    /// </para>
    /// <example>
    /// <code>
    /// registry.RegisterGlobalWithLiteDb&lt;Customer&gt;(
    ///     "C:\\Data\\myapp.db",
    ///     collectionName: "customers",
    ///     autoLoad: true,
    ///     autoSave: true);
    /// </code>
    /// </example>
    /// </remarks>
    public static IGlobalStoreRegistry RegisterGlobalWithLiteDb<T>(
        this IGlobalStoreRegistry registry,
        string databasePath,
        string? collectionName = null,
        bool autoLoad = true,
        bool autoSave = true,
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null) where T : class
    {
        if (registry == null)
            throw new ArgumentNullException(nameof(registry));
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentNullException(nameof(databasePath));

        var strategy = new LiteDbPersistenceStrategy<T>(databasePath, collectionName);
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
    /// Registriert einen globalen DataStore mit benutzerdefinierter Persistierungs-Strategie.
    /// </summary>
    /// <typeparam name="T">Der Typ der Elemente im Store.</typeparam>
    /// <param name="registry">Die GlobalStoreRegistry-Instanz.</param>
    /// <param name="strategy">Die zu verwendende Persistierungs-Strategie.</param>
    /// <param name="autoLoad">Wenn true, werden Daten beim Bootstrap automatisch geladen.</param>
    /// <param name="autoSave">Wenn true, werden Änderungen automatisch gespeichert.</param>
    /// <param name="comparer">Optionaler Equality-Comparer für Elemente.</param>
    /// <param name="synchronizationContext">Optionaler SynchronizationContext für Events.</param>
    /// <returns>Die Registry-Instanz für Fluent-API.</returns>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn registry oder strategy null ist.</exception>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">Wird ausgelöst, wenn bereits ein Store für Typ T registriert wurde.</exception>
    /// <remarks>
    /// <para>
    /// Diese Methode erlaubt die Verwendung einer benutzerdefinierten Persistierungs-Strategie.
    /// Nützlich für Cloud-Storage, Datenbanken oder andere Persistierungs-Mechanismen.
    /// </para>
    /// <example>
    /// <code>
    /// var customStrategy = new MyCustomPersistenceStrategy&lt;Customer&gt;();
    /// registry.RegisterGlobalWithPersistence(
    ///     customStrategy,
    ///     autoLoad: true,
    ///     autoSave: true);
    /// </code>
    /// </example>
    /// </remarks>
    public static IGlobalStoreRegistry RegisterGlobalWithPersistence<T>(
        this IGlobalStoreRegistry registry,
        IPersistenceStrategy<T> strategy,
        bool autoLoad = true,
        bool autoSave = true,
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null) where T : class
    {
        if (registry == null)
            throw new ArgumentNullException(nameof(registry));
        if (strategy == null)
            throw new ArgumentNullException(nameof(strategy));

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
