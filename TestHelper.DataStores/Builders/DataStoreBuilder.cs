using DataStores.Abstractions;
using DataStores.Runtime;
using TestHelper.DataStores.TestData;

namespace TestHelper.DataStores.Builders;

/// <summary>
/// Fluent builder for creating test DataStore instances with predefined configurations.
/// </summary>
/// <typeparam name="T">Der Entity-Typ des DataStores.</typeparam>
/// <remarks>
/// <para>
/// Dieser Builder ermöglicht die komfortable Erstellung von DataStore-Instanzen
/// für Unit- und Integrationstests mit optionaler Testdaten-Generierung.
/// </para>
/// <para>
/// <b>Unterstützte Konfigurationen:</b>
/// <list type="bullet">
/// <item><description>Manuelle Items via <see cref="WithItems(T[])"/></description></item>
/// <item><description>Generierte Items via <see cref="WithGeneratedItems(ITestDataFactory{T}, int)"/></description></item>
/// <item><description>Custom Comparer via <see cref="WithComparer(IEqualityComparer{T})"/></description></item>
/// <item><description>SynchronizationContext via <see cref="WithSyncContext(SynchronizationContext)"/></description></item>
/// <item><description>Changed Event Handler via <see cref="WithChangedHandler(EventHandler{DataStoreChangedEventArgs{T}})"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Beispiel 1: Manuelle Items
/// var store = new DataStoreBuilder&lt;Product&gt;()
///     .WithItems(new Product { Name = "A" }, new Product { Name = "B" })
///     .Build();
/// 
/// // Beispiel 2: Generierte Items
/// var factory = new ObjectFillerTestDataFactory&lt;Product&gt;(seed: 42);
/// var store = new DataStoreBuilder&lt;Product&gt;()
///     .WithGeneratedItems(factory, count: 100)
///     .Build();
/// 
/// // Beispiel 3: Kombiniert
/// var store = new DataStoreBuilder&lt;Product&gt;()
///     .WithItems(specialProduct)
///     .WithGeneratedItems(factory, count: 50)
///     .WithComparer(new IdComparer())
///     .Build();
/// </code>
/// </example>
public class DataStoreBuilder<T> where T : class
{
    private List<T> _items = new();
    private SynchronizationContext? _syncContext;
    private IEqualityComparer<T>? _comparer;
    private EventHandler<DataStoreChangedEventArgs<T>>? _changedHandler;

    /// <summary>
    /// Fügt manuelle Items zum Store hinzu.
    /// </summary>
    /// <param name="items">Die hinzuzufügenden Items.</param>
    /// <returns>Der Builder für Fluent-API.</returns>
    /// <remarks>
    /// Diese Methode kann mehrfach aufgerufen werden. Alle Items werden akkumuliert.
    /// Items werden in der Reihenfolge der Aufrufe zum Store hinzugefügt.
    /// </remarks>
    public DataStoreBuilder<T> WithItems(params T[] items)
    {
        _items.AddRange(items);
        return this;
    }

    /// <summary>
    /// Fügt generierte Test-Items zum Store hinzu.
    /// </summary>
    /// <param name="factory">Die Factory zur Erzeugung der Test-Items.</param>
    /// <param name="count">Anzahl der zu generierenden Items. Muss &gt;= 0 sein.</param>
    /// <returns>Der Builder für Fluent-API.</returns>
    /// <remarks>
    /// <para>
    /// Die generierten Items werden intern wie manuell hinzugefügte Items behandelt.
    /// Diese Methode kann mit <see cref="WithItems(T[])"/> kombiniert werden.
    /// </para>
    /// <para>
    /// <b>Ausführungszeitpunkt:</b>
    /// Die Items werden sofort bei Aufruf dieser Methode generiert, nicht erst bei Build().
    /// Dies ermöglicht deterministisches Verhalten und frühe Fehlerdiagnose.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Wird ausgelöst, wenn <paramref name="factory"/> <c>null</c> ist.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Wird ausgelöst, wenn <paramref name="count"/> negativ ist.
    /// </exception>
    /// <example>
    /// <code>
    /// var factory = new ObjectFillerTestDataFactory&lt;Product&gt;(seed: 42);
    /// var store = new DataStoreBuilder&lt;Product&gt;()
    ///     .WithGeneratedItems(factory, count: 100)
    ///     .Build();
    /// 
    /// Assert.Equal(100, store.Items.Count);
    /// </code>
    /// </example>
    public DataStoreBuilder<T> WithGeneratedItems(ITestDataFactory<T> factory, int count)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be >= 0");

        var generatedItems = factory.CreateMany(count);
        _items.AddRange(generatedItems);
        
        return this;
    }

    /// <summary>
    /// Setzt den SynchronizationContext für Thread-Marshalling.
    /// </summary>
    /// <param name="ctx">Der SynchronizationContext (z.B. für UI-Thread).</param>
    /// <returns>Der Builder für Fluent-API.</returns>
    /// <remarks>
    /// Wenn gesetzt, werden Changed-Events auf diesem Context dispatched.
    /// Nützlich für UI-Tests mit WPF/WinForms.
    /// </remarks>
    public DataStoreBuilder<T> WithSyncContext(SynchronizationContext ctx)
    {
        _syncContext = ctx;
        return this;
    }

    /// <summary>
    /// Setzt einen Custom Equality-Comparer für den Store.
    /// </summary>
    /// <param name="comparer">Der Comparer für Gleichheitsvergleiche.</param>
    /// <returns>Der Builder für Fluent-API.</returns>
    /// <remarks>
    /// Beeinflusst <see cref="IDataStore{T}.Contains(T)"/> und Remove-Operationen.
    /// </remarks>
    public DataStoreBuilder<T> WithComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer;
        return this;
    }

    /// <summary>
    /// Registriert einen Event-Handler für Changed-Events.
    /// </summary>
    /// <param name="handler">Der Event-Handler.</param>
    /// <returns>Der Builder für Fluent-API.</returns>
    /// <remarks>
    /// Der Handler wird sofort nach Store-Erstellung registriert,
    /// aber BEVOR initiale Items hinzugefügt werden.
    /// Daher werden Events für initiale Items gefeuert.
    /// </remarks>
    public DataStoreBuilder<T> WithChangedHandler(EventHandler<DataStoreChangedEventArgs<T>> handler)
    {
        _changedHandler = handler;
        return this;
    }

    /// <summary>
    /// Erstellt den konfigurierten DataStore.
    /// </summary>
    /// <returns>Ein neuer <see cref="IDataStore{T}"/> mit allen konfigurierten Einstellungen.</returns>
    /// <remarks>
    /// <para>
    /// <b>Ausführungsreihenfolge:</b>
    /// <list type="number">
    /// <item><description>InMemoryDataStore erstellen (mit Comparer und SyncContext)</description></item>
    /// <item><description>Changed-Handler registrieren (falls vorhanden)</description></item>
    /// <item><description>Alle Items hinzufügen (AddRange)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Alle Items (manuelle + generierte) werden in der Reihenfolge hinzugefügt,
    /// in der die With-Methoden aufgerufen wurden.
    /// </para>
    /// </remarks>
    public IDataStore<T> Build()
    {
        var store = new InMemoryDataStore<T>(_comparer, _syncContext);
        
        if (_changedHandler != null)
            store.Changed += _changedHandler;

        if (_items.Count > 0)
            store.AddRange(_items);

        return store;
    }
}
