using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// Facade-Implementierung für den Zugriff auf globale und die Erstellung lokaler Datenspeicher.
/// </summary>
/// <remarks>
/// Diese Klasse dient als zentrale Anlaufstelle für alle DataStore-Operationen.
/// Sie koordiniert die Kommunikation zwischen der <see cref="IGlobalStoreRegistry"/> 
/// und der <see cref="ILocalDataStoreFactory"/>, um eine einheitliche API bereitzustellen.
/// </remarks>
public class DataStoresFacade : IDataStores
{
    private readonly IGlobalStoreRegistry _registry;
    private readonly ILocalDataStoreFactory _localFactory;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="DataStoresFacade"/> Klasse.
    /// </summary>
    /// <param name="registry">
    /// Die globale Store-Registry für den Zugriff auf application-wide Singleton-Stores.
    /// </param>
    /// <param name="localFactory">
    /// Die Factory zum Erstellen isolierter lokaler Stores.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Wird ausgelöst, wenn <paramref name="registry"/> oder <paramref name="localFactory"/> null ist.
    /// </exception>
    /// <remarks>
    /// Diese Instanz wird typischerweise über Dependency Injection bereitgestellt
    /// und sollte als Singleton registriert werden.
    /// </remarks>
    public DataStoresFacade(IGlobalStoreRegistry registry, ILocalDataStoreFactory localFactory)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _localFactory = localFactory ?? throw new ArgumentNullException(nameof(localFactory));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegiert den Aufruf an die <see cref="IGlobalStoreRegistry.ResolveGlobal{T}"/> Methode.
    /// Globale Stores sind application-wide Singletons und werden von allen Teilen der Anwendung geteilt.
    /// </remarks>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Wird ausgelöst, wenn kein globaler Store für Typ <typeparamref name="T"/> registriert wurde.
    /// </exception>
    public IDataStore<T> GetGlobal<T>() where T : class
    {
        return _registry.ResolveGlobal<T>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Erstellt einen neuen, isolierten lokalen Store, der unabhängig von globalen Stores ist.
    /// Lokale Stores sind nützlich für temporäre Daten, Dialoge, Formulare oder andere
    /// Szenarien, in denen Isolation erforderlich ist.
    /// </remarks>
    public IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class
    {
        return _localFactory.CreateLocal(comparer);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Erstellt einen neuen lokalen Store und füllt ihn mit einer gefilterten Kopie der Daten
    /// aus dem globalen Store. Dies ist nützlich für:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Arbeiten mit einer Teilmenge globaler Daten</description></item>
    /// <item><description>Isolierte Bearbeitung ohne Auswirkung auf globale Daten</description></item>
    /// <item><description>Temporäre Filterung für UI-Szenarien</description></item>
    /// </list>
    /// <para>
    /// Änderungen am lokalen Store beeinflussen NICHT den globalen Store.
    /// </para>
    /// </remarks>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Wird ausgelöst, wenn kein globaler Store für Typ <typeparamref name="T"/> registriert wurde.
    /// </exception>
    public IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
        Func<T, bool>? predicate = null,
        IEqualityComparer<T>? comparer = null) where T : class
    {
        var globalStore = _registry.ResolveGlobal<T>();
        var localStore = _localFactory.CreateLocal(comparer);

        var items = predicate == null
            ? globalStore.Items
            : globalStore.Items.Where(predicate).ToList();

        localStore.AddRange(items);

        return localStore;
    }
}
