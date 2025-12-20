using DataStores.Abstractions;

namespace DataStores.Runtime;

/// <summary>
/// Bietet eine In-Memory-Implementierung von <see cref="IDataStore{T}"/>.
/// Diese Klasse ist thread-sicher und unterstützt optionale SynchronizationContext-basierte Event-Marshalling.
/// </summary>
/// <typeparam name="T">Der Typ der Elemente im Store. Muss ein Referenztyp sein.</typeparam>
/// <remarks>
/// Die Klasse verwendet intern eine <see cref="List{T}"/> für die Speicherung und
/// ein Lock-Objekt für Thread-Sicherheit. Alle öffentlichen Operationen sind thread-sicher.
/// Events können optional auf einem bestimmten SynchronizationContext ausgeführt werden,
/// was nützlich ist für UI-Anwendungen (WPF, WinForms, MAUI).
/// </remarks>
public class InMemoryDataStore<T> : IDataStore<T> where T : class
{
    private readonly List<T> _items;
    private readonly IEqualityComparer<T> _comparer;
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly object _lock = new();

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Property gibt immer eine neue Kopie der Liste zurück, um Thread-Sicherheit zu gewährleisten.
    /// Änderungen an der zurückgegebenen Liste beeinflussen nicht den internen Zustand des Stores.
    /// </remarks>
    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_lock)
            {
                return _items.ToList();
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Events werden außerhalb von Locks ausgelöst, um Deadlocks zu vermeiden.
    /// Wenn ein SynchronizationContext gesetzt ist, werden Events auf diesem Context gemarshallt.
    /// </remarks>
    public event EventHandler<DataStoreChangedEventArgs<T>>? Changed;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="InMemoryDataStore{T}"/> Klasse.
    /// </summary>
    /// <param name="comparer">
    /// Optionaler Gleichheitsvergleicher für Elemente. 
    /// Wenn null, wird <see cref="EqualityComparer{T}.Default"/> verwendet.
    /// </param>
    /// <param name="synchronizationContext">
    /// Optionaler Synchronisationskontext für Event-Aufruf. 
    /// Wenn null, werden Events synchron auf dem aufrufenden Thread ausgelöst.
    /// Nützlich für UI-Anwendungen, um Events auf dem UI-Thread auszuführen.
    /// </param>
    /// <example>
    /// <code>
    /// // Standard-Store
    /// var store = new InMemoryDataStore&lt;Product&gt;();
    /// 
    /// // Mit Custom Comparer
    /// var comparer = new ProductIdComparer();
    /// var store = new InMemoryDataStore&lt;Product&gt;(comparer);
    /// 
    /// // Mit UI-Thread-Context (WPF)
    /// var syncContext = SynchronizationContext.Current;
    /// var store = new InMemoryDataStore&lt;Product&gt;(null, syncContext);
    /// </code>
    /// </example>
    public InMemoryDataStore(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? synchronizationContext = null)
    {
        _items = new List<T>();
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _synchronizationContext = synchronizationContext;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Methode ist thread-sicher. Das Element wird zur Liste hinzugefügt
    /// und ein <see cref="Changed"/> Event wird mit <see cref="DataStoreChangeType.Add"/> ausgelöst.
    /// </remarks>
    public void Add(T item)
    {
        lock (_lock)
        {
            _items.Add(item);
        }
        OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.Add, item));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Methode ist thread-sicher und effizienter als mehrfache Aufrufe von <see cref="Add"/>,
    /// da nur ein Lock und ein Event ausgelöst werden.
    /// Wenn die Sammlung leer ist, wird keine Operation durchgeführt.
    /// </remarks>
    public void AddRange(IEnumerable<T> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            return;

        lock (_lock)
        {
            _items.AddRange(itemList);
        }
        OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.BulkAdd, itemList));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Methode ist thread-sicher. Der Vergleich verwendet den konfigurierten <see cref="_comparer"/>.
    /// Wenn das Element nicht gefunden wird, wird kein Event ausgelöst.
    /// </remarks>
    public bool Remove(T item)
    {
        bool removed;
        lock (_lock)
        {
            var index = _items.FindIndex(x => _comparer.Equals(x, item));
            if (index >= 0)
            {
                _items.RemoveAt(index);
                removed = true;
            }
            else
            {
                removed = false;
            }
        }

        if (removed)
        {
            OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.Remove, item));
        }

        return removed;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Methode ist thread-sicher. Alle Elemente werden aus der Liste entfernt
    /// und ein <see cref="Changed"/> Event wird mit <see cref="DataStoreChangeType.Clear"/> ausgelöst.
    /// </remarks>
    public void Clear()
    {
        lock (_lock)
        {
            _items.Clear();
        }
        OnChanged(new DataStoreChangedEventArgs<T>(DataStoreChangeType.Clear));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Diese Methode ist thread-sicher. Der Vergleich verwendet den konfigurierten <see cref="_comparer"/>.
    /// </remarks>
    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _items.Any(x => _comparer.Equals(x, item));
        }
    }

    /// <summary>
    /// Löst das <see cref="Changed"/> Event aus.
    /// </summary>
    /// <param name="args">Die Event-Argumente mit Details zur Änderung.</param>
    /// <remarks>
    /// Diese Methode wird außerhalb von Locks aufgerufen, um Deadlocks zu vermeiden.
    /// Wenn ein SynchronizationContext gesetzt ist, wird das Event auf diesem Context ausgeführt.
    /// </remarks>
    private void OnChanged(DataStoreChangedEventArgs<T> args)
    {
        var handler = Changed;
        if (handler == null)
            return;

        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ => handler.Invoke(this, args), null);
        }
        else
        {
            handler.Invoke(this, args);
        }
    }
}
