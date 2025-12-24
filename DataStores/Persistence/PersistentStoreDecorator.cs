using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Persistence;

/// <summary>
/// Dekoriert einen <see cref="IDataStore{T}"/> mit Persistenz-Funktionalität.
/// </summary>
/// <typeparam name="T">Der Typ der Elemente im Store. Muss ein Referenztyp sein.</typeparam>
/// <remarks>
/// <para>
/// Diese Klasse implementiert das Decorator-Pattern und umhüllt einen <see cref="InMemoryDataStore{T}"/>
/// mit optionaler Auto-Load- und Auto-Save-Funktionalität. Die eigentliche Persistierungslogik
/// wird durch eine <see cref="IPersistenceStrategy{T}"/> Implementierung bereitgestellt.
/// </para>
/// <para>
/// <b>Auto-Load:</b> Wenn aktiviert, werden Daten beim Aufruf von <see cref="InitializeAsync"/> geladen.
/// Dies geschieht typischerweise während des Bootstrap-Prozesses.
/// </para>
/// <para>
/// <b>Auto-Save:</b> Wenn aktiviert, werden Daten automatisch bei jeder Änderung (Add, Remove, Clear, etc.)
/// gespeichert. Das Speichern erfolgt asynchron im Hintergrund und blockiert die Operationen nicht.
/// </para>
/// <para>
/// <b>PropertyChanged-Tracking:</b> Wenn Auto-Save aktiviert ist und Items <see cref="System.ComponentModel.INotifyPropertyChanged"/>
/// implementieren, werden auch PropertyChanged-Events getrackt und lösen ein Save aus.
/// </para>
/// <para>
/// <b>Thread-Sicherheit:</b> Verwendet <see cref="SemaphoreSlim"/> zur Vermeidung von Race-Conditions
/// beim Laden und Speichern.
/// </para>
/// </remarks>
public class PersistentStoreDecorator<T> : IDataStore<T>, IAsyncInitializable, IDisposable where T : class
{
    private readonly InMemoryDataStore<T> _innerStore;
    private readonly IPersistenceStrategy<T> _strategy;
    private readonly bool _autoLoad;
    private readonly bool _autoSaveOnChange;
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private readonly PropertyChangedBinder<T>? _propertyChangedBinder;
    private readonly IDisposable? _binderSubscription;
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="PersistentStoreDecorator{T}"/> Klasse.
    /// </summary>
    /// <param name="innerStore">
    /// Der innere In-Memory-Store, der dekoriert wird. Dieser Store enthält die eigentlichen Daten.
    /// </param>
    /// <param name="strategy">
    /// Die Persistierungsstrategie, die zum Laden und Speichern verwendet wird.
    /// </param>
    /// <param name="autoLoad">
    /// Wenn <c>true</c>, werden Daten während der Initialisierung (<see cref="InitializeAsync"/>) geladen.
    /// Standard ist <c>true</c>.
    /// </param>
    /// <param name="autoSaveOnChange">
    /// Wenn <c>true</c>, werden Daten automatisch bei Änderungen gespeichert.
    /// Das Speichern erfolgt asynchron und blockiert nicht.
    /// Standard ist <c>true</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Wird ausgelöst, wenn <paramref name="innerStore"/> oder <paramref name="strategy"/> null ist.
    /// </exception>
    /// <example>
    /// <code>
    /// var innerStore = new InMemoryDataStore&lt;Product&gt;();
    /// var strategy = new JsonPersistenceStrategy&lt;Product&gt;("products.json");
    /// var decorator = new PersistentStoreDecorator&lt;Product&gt;(
    ///     innerStore, 
    ///     strategy, 
    ///     autoLoad: true, 
    ///     autoSaveOnChange: true);
    /// 
    /// // Daten laden
    /// await decorator.InitializeAsync();
    /// 
    /// // Änderungen werden automatisch gespeichert
    /// decorator.Add(new Product { Id = 1, Name = "Test" });
    /// </code>
    /// </example>
    public PersistentStoreDecorator(
        InMemoryDataStore<T> innerStore,
        IPersistenceStrategy<T> strategy,
        bool autoLoad = true,
        bool autoSaveOnChange = true)
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _autoLoad = autoLoad;
        _autoSaveOnChange = autoSaveOnChange;

        // Gib der Strategie Zugriff auf die aktuelle Items-Liste
        // (JSON braucht dies für UpdateSingleAsync, LiteDB ignoriert es)
        _strategy.SetItemsProvider(() => _innerStore.Items);

        if (_autoSaveOnChange)
        {
            // Collection-Changes abonnieren
            _innerStore.Changed += OnInnerStoreChanged;
            
            // PropertyChanged-Tracking für INotifyPropertyChanged-Entities
            if (typeof(T).IsAssignableTo(typeof(System.ComponentModel.INotifyPropertyChanged)))
            {
                _propertyChangedBinder = new PropertyChangedBinder<T>(
                    enabled: true,
                    onEntityChanged: OnItemPropertyChanged);
                
                _binderSubscription = _propertyChangedBinder.AttachToDataStore(_innerStore);
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegiert an den inneren Store.
    /// </remarks>
    public IReadOnlyList<T> Items => _innerStore.Items;

    /// <inheritdoc/>
    /// <remarks>
    /// Events werden vom inneren Store weitergeleitet.
    /// </remarks>
    public event EventHandler<DataStoreChangedEventArgs<T>>? Changed
    {
        add => _innerStore.Changed += value;
        remove => _innerStore.Changed -= value;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegiert an den inneren Store. Wenn Auto-Save aktiviert ist, wird das Element automatisch gespeichert.
    /// </remarks>
    public void Add(T item) => _innerStore.Add(item);

    /// <inheritdoc>
    /// <remarks>
    /// Delegiert an den inneren Store. Wenn Auto-Save aktiviert ist, werden die Elemente automatisch gespeichert.
    /// </remarks>
    public void AddRange(IEnumerable<T> items) => _innerStore.AddRange(items);

    /// <inheritdoc/>
    /// <remarks>
    /// Delegiert an den inneren Store. Wenn Auto-Save aktiviert ist, wird die Änderung automatisch gespeichert.
    /// </remarks>
    public bool Remove(T item) => _innerStore.Remove(item);

    /// <inheritdoc/>
    /// <remarks>
    /// Delegiert an den inneren Store. Wenn Auto-Save aktiviert ist, wird die Änderung automatisch gespeichert.
    /// </remarks>
    public void Clear() => _innerStore.Clear();

    /// <inheritdoc/>
    /// <remarks>
    /// Delegiert an den inneren Store.
    /// </remarks>
    public bool Contains(T item) => _innerStore.Contains(item);

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Initialisiert den Store asynchron durch Laden der Daten aus der Persistierungsstrategie.
    /// Diese Methode ist idempotent - mehrfache Aufrufe laden die Daten nur einmal.
    /// </para>
    /// <para>
    /// Die Methode ist thread-sicher und verwendet ein <see cref="SemaphoreSlim"/> 
    /// zur Vermeidung von Race-Conditions.
    /// </para>
    /// <para>
    /// Wird typischerweise vom <see cref="Bootstrap.DataStoreBootstrap"/> automatisch aufgerufen.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">
    /// Kann verschiedene Exceptions werfen, abhängig von der verwendeten <see cref="IPersistenceStrategy{T}"/>.
    /// </exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            { 
            return;
            }

            if (_autoLoad)
            {
                var items = await _strategy.LoadAllAsync(cancellationToken);
                _innerStore.AddRange(items);
            }
            
            _isInitialized = true;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    /// <summary>
    /// Event-Handler für Änderungen am inneren Store.
    /// Speichert die Daten automatisch, wenn Auto-Save aktiviert ist.
    /// </summary>
    /// <param name="sender">Die Quelle des Events.</param>
    /// <param name="e">Die Event-Daten mit Details zur Änderung.</param>
    /// <remarks>
    /// <para>
    /// Das Speichern erfolgt asynchron im Hintergrund (Fire-and-Forget-Pattern).
    /// Fehler beim Speichern werden gefangen und ignoriert, um die Anwendung nicht zu blockieren.
    /// In Produktionsumgebungen sollten Fehler geloggt werden.
    /// </para>
    /// <para>
    /// Verwendet ein <see cref="SemaphoreSlim"/> zur Vermeidung von Race-Conditions
    /// bei mehreren gleichzeitigen Änderungen.
    /// </para>
    /// </remarks>
    private async void OnInnerStoreChanged(object? sender, DataStoreChangedEventArgs<T> e)
    {
        if (_disposed)
        {
            return;
        }

        await SaveAsync();
    }

    /// <summary>
    /// Event-Handler für PropertyChanged-Events einzelner Items.
    /// </summary>
    /// <param name="entity">Die Entität, deren Property sich geändert hat.</param>
    private async void OnItemPropertyChanged(T entity)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await _saveSemaphore.WaitAsync();
            try
            {
                await _strategy.UpdateSingleAsync(entity);
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }
        catch (Exception)
        {
            // In Produktionsumgebungen sollte hier geloggt werden
        }
    }

    /// <summary>
    /// Speichert den aktuellen Zustand des Stores asynchron.
    /// </summary>
    private async Task SaveAsync()
    {
        try
        {
            await _saveSemaphore.WaitAsync();
            try
            {
                await _strategy.SaveAllAsync(_innerStore.Items);
            }
            finally
            {
                _saveSemaphore.Release();
            }
        }
        catch (Exception)
        {
            // In Produktionsumgebungen sollte hier geloggt werden
            // Fehler werden gefangen, um die Anwendung nicht zu blockieren
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Gibt alle verwendeten Ressourcen frei, insbesondere:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Trennt Event-Handler vom inneren Store</description></item>
    /// <item><description>Beendet PropertyChanged-Tracking</description></item>
    /// <item><description>Gibt Semaphore frei</description></item>
    /// </list>
    /// <para>
    /// Diese Methode ist idempotent - mehrfache Aufrufe sind sicher.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_autoSaveOnChange)
        {
            _innerStore.Changed -= OnInnerStoreChanged;
            _binderSubscription?.Dispose();
            _propertyChangedBinder?.Dispose();
        }

        _saveSemaphore.Dispose();
        _initSemaphore.Dispose();
        _disposed = true;
    }
}
