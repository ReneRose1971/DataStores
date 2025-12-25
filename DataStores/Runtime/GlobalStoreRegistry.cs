using System.Collections.Concurrent;
using DataStores.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace DataStores.Runtime;

/// <summary>
/// Thread-sichere Implementierung von <see cref="IGlobalStoreRegistry"/>.
/// </summary>
/// <remarks>
/// Diese Klasse verwaltet die Registrierung und Auflösung globaler Datenspeicher
/// in einer thread-sicheren Weise mittels <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// Globale Stores sind application-wide Singletons und sollten beim Application-Start
/// über <see cref="IDataStoreRegistrar"/> Implementierungen registriert werden.
/// </remarks>
public class GlobalStoreRegistry : IGlobalStoreRegistry
{
    private readonly ConcurrentDictionary<Type, object> _stores = new();

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Diese Methode ist thread-sicher und kann von mehreren Threads gleichzeitig aufgerufen werden.
    /// Wenn bereits ein Store für den Typ <typeparamref name="T"/> registriert wurde,
    /// wird eine <see cref="GlobalStoreAlreadyRegisteredException"/> ausgelöst.
    /// </para>
    /// <para>
    /// Registrierungen sollten typischerweise während der Anwendungsinitialisierung
    /// im <see cref="IDataStoreRegistrar.Register"/> vorgenommen werden.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Wird ausgelöst, wenn <paramref name="store"/> null ist.
    /// </exception>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">
    /// Wird ausgelöst, wenn bereits ein Store für Typ <typeparamref name="T"/> registriert wurde.
    /// </exception>
    public void RegisterGlobal<T>(IDataStore<T> store) where T : class
    {
        if (store == null)
            throw new ArgumentNullException(nameof(store));

        var storeType = typeof(T);
        if (!_stores.TryAdd(storeType, store))
        {
            throw new GlobalStoreAlreadyRegisteredException(storeType);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Methode ist thread-sicher und kann von mehreren Threads gleichzeitig aufgerufen werden.
    /// Wenn kein Store für den Typ <typeparamref name="T"/> registriert wurde,
    /// wird eine <see cref="GlobalStoreNotRegisteredException"/> ausgelöst.
    /// </remarks>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Wird ausgelöst, wenn kein Store für Typ <typeparamref name="T"/> registriert wurde.
    /// </exception>
    public IDataStore<T> ResolveGlobal<T>() where T : class
    {
        var storeType = typeof(T);
        if (_stores.TryGetValue(storeType, out var store))
        {
            return (IDataStore<T>)store;
        }

        throw new GlobalStoreNotRegisteredException(storeType);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Methode ist thread-sicher und kann von mehreren Threads gleichzeitig aufgerufen werden.
    /// Im Gegensatz zu <see cref="ResolveGlobal{T}"/> löst diese Methode keine Exception aus,
    /// sondern gibt false zurück, wenn kein Store gefunden wurde.
    /// </remarks>
    public bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class
    {
        var storeType = typeof(T);
        if (_stores.TryGetValue(storeType, out var storeObj))
        {
            store = (IDataStore<T>)storeObj;
            return true;
        }

        store = null!;
        return false;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Diese Methode durchläuft alle registrierten Stores und gibt diejenigen zurück,
    /// die <see cref="Persistence.IAsyncInitializable"/> implementieren.
    /// </para>
    /// <para>
    /// Die Methode ist thread-sicher und kann von mehreren Threads gleichzeitig aufgerufen werden.
    /// Die zurückgegebene Collection ist eine Momentaufnahme zum Zeitpunkt des Aufrufs.
    /// </para>
    /// </remarks>
    public IEnumerable<Persistence.IAsyncInitializable> GetInitializableGlobalStores()
    {
        return _stores.Values.OfType<Persistence.IAsyncInitializable>();
    }
}
