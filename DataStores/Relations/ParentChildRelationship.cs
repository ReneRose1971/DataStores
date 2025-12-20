using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Relations;

/// <summary>
/// Verwaltet eine Eltern-Kind-Beziehung zwischen Datenspeichern.
/// </summary>
/// <typeparam name="TParent">Der Eltern-Entitätstyp. Muss ein Referenztyp sein.</typeparam>
/// <typeparam name="TChild">Der Kind-Entitätstyp. Muss ein Referenztyp sein.</typeparam>
/// <remarks>
/// <para>
/// Diese Klasse ermöglicht es, hierarchische Beziehungen zwischen verschiedenen Entitätstypen zu definieren
/// und zu verwalten. Ein typisches Beispiel wäre eine Kategorie (Parent) mit ihren Produkten (Children).
/// </para>
/// <para>
/// <b>Funktionsweise:</b>
/// <list type="number">
/// <item><description>Eine Datenquelle wird festgelegt (global Store oder Snapshot)</description></item>
/// <item><description>Eine Filter-Funktion bestimmt, welche Kinder zum Elternteil gehören</description></item>
/// <item><description><see cref="Refresh"/> wendet den Filter an und füllt die <see cref="Childs"/> Collection</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Verwendungsszenarien:</b>
/// <list type="bullet">
/// <item><description>Kategorie ? Produkte</description></item>
/// <item><description>Abteilung ? Mitarbeiter</description></item>
/// <item><description>Bestellung ? Bestellpositionen</description></item>
/// <item><description>Projekt ? Aufgaben</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var category = new Category { Id = 1, Name = "Electronics" };
/// 
/// var relationship = new ParentChildRelationship&lt;Category, Product&gt;(
///     stores,
///     parent: category,
///     filter: (cat, prod) => prod.CategoryId == cat.Id);
/// 
/// relationship.UseGlobalDataSource();
/// relationship.Refresh();
/// 
/// // Zugriff auf gefilterte Kinder
/// foreach (var product in relationship.Childs.Items)
/// {
///     Console.WriteLine($"{product.Name} gehört zu {category.Name}");
/// }
/// </code>
/// </example>
public class ParentChildRelationship<TParent, TChild>
    where TParent : class
    where TChild : class
{
    private readonly IDataStores _stores;
    private IDataStore<TChild>? _dataSource;

    /// <summary>
    /// Ruft die Eltern-Entität ab oder legt sie fest.
    /// </summary>
    /// <remarks>
    /// Diese Property kann nur während der Objekt-Initialisierung gesetzt werden (init-only).
    /// </remarks>
    public TParent Parent { get; init; }

    /// <summary>
    /// Ruft die Datenquelle für Kind-Elemente ab oder legt sie fest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Datenquelle bestimmt, aus welchem Store die Kind-Elemente stammen.
    /// Sie sollte über <see cref="UseGlobalDataSource"/> oder <see cref="UseSnapshotFromGlobal"/> gesetzt werden.
    /// </para>
    /// <para>
    /// Beim Abrufen wird eine <see cref="InvalidOperationException"/> ausgelöst,
    /// wenn die Datenquelle noch nicht gesetzt wurde.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Wird beim Setzen ausgelöst, wenn der Wert null ist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Wird beim Abrufen ausgelöst, wenn die Datenquelle noch nicht gesetzt wurde.
    /// </exception>
    public IDataStore<TChild> DataSource
    {
        get => _dataSource ?? throw new InvalidOperationException("DataSource has not been set. Call UseGlobalDataSource() or UseSnapshotFromGlobal() first.");
        set => _dataSource = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Ruft die lokale Sammlung von Kind-Elementen für diesen Elternteil ab.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Diese Collection wird durch <see cref="Refresh"/> gefüllt und enthält nur die Kinder,
    /// die durch die <see cref="Filter"/>-Funktion als zugehörig identifiziert wurden.
    /// </para>
    /// <para>
    /// Es handelt sich um einen separaten lokalen Store, sodass Änderungen an dieser Collection
    /// nicht automatisch die <see cref="DataSource"/> beeinflussen.
    /// </para>
    /// <para>
    /// Sie können das <see cref="InMemoryDataStore{T}.Changed"/> Event abonnieren,
    /// um auf Änderungen an den Kind-Elementen zu reagieren.
    /// </para>
    /// </remarks>
    public InMemoryDataStore<TChild> Childs { get; }

    /// <summary>
    /// Ruft die Filter-Funktion ab oder legt sie fest, die bestimmt, welche Kinder zu diesem Elternteil gehören.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Filter-Funktion nimmt das Elternteil und ein potenzielles Kind entgegen
    /// und gibt <c>true</c> zurück, wenn das Kind zum Elternteil gehört.
    /// </para>
    /// <para>
    /// Diese Property kann nur während der Objekt-Initialisierung gesetzt werden (init-only).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Einfacher Filter nach ID
    /// filter: (category, product) => product.CategoryId == category.Id
    /// 
    /// // Komplexer Filter mit mehreren Bedingungen
    /// filter: (department, employee) => 
    ///     employee.DepartmentId == department.Id &amp;&amp; 
    ///     employee.IsActive &amp;&amp;
    ///     employee.StartDate &lt;= DateTime.Now
    /// </code>
    /// </example>
    public Func<TParent, TChild, bool> Filter { get; init; }

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="ParentChildRelationship{TParent, TChild}"/> Klasse.
    /// </summary>
    /// <param name="stores">
    /// Die DataStores-Facade für den Zugriff auf globale Stores.
    /// </param>
    /// <param name="parent">
    /// Die Eltern-Entität, für die die Beziehung verwaltet wird.
    /// </param>
    /// <param name="filter">
    /// Die Filter-Funktion, die bestimmt, welche Kinder zu diesem Elternteil gehören.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Wird ausgelöst, wenn einer der Parameter null ist.
    /// </exception>
    /// <remarks>
    /// Nach der Initialisierung muss die Datenquelle über <see cref="UseGlobalDataSource"/> 
    /// oder <see cref="UseSnapshotFromGlobal"/> gesetzt und <see cref="Refresh"/> aufgerufen werden,
    /// bevor auf <see cref="Childs"/> zugegriffen werden kann.
    /// </remarks>
    public ParentChildRelationship(
        IDataStores stores,
        TParent parent,
        Func<TParent, TChild, bool> filter)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        Childs = new InMemoryDataStore<TChild>();
    }

    /// <summary>
    /// Setzt die Datenquelle auf den globalen Datenspeicher für <typeparamref name="TChild"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Diese Methode ruft den globalen Store über <see cref="IDataStores.GetGlobal{T}"/> ab
    /// und setzt ihn als <see cref="DataSource"/>.
    /// </para>
    /// <para>
    /// Verwenden Sie diese Methode, wenn Sie die vollständige globale Sammlung als Basis
    /// für die Filterung verwenden möchten.
    /// </para>
    /// </remarks>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Wird ausgelöst, wenn kein globaler Store für <typeparamref name="TChild"/> registriert wurde.
    /// </exception>
    public void UseGlobalDataSource()
    {
        DataSource = _stores.GetGlobal<TChild>();
    }

    /// <summary>
    /// Erstellt einen lokalen Snapshot aus dem globalen Datenspeicher und setzt ihn als Datenquelle.
    /// </summary>
    /// <param name="predicate">
    /// Optionale zusätzliche Filter-Funktion für den Snapshot.
    /// Wenn null, werden alle Elemente aus dem globalen Store kopiert.
    /// </param>
    /// <remarks>
    /// <para>
    /// Diese Methode ist nützlich, wenn Sie eine Vorab-Filterung durchführen möchten,
    /// bevor der Parent-Child-Filter angewendet wird.
    /// </para>
    /// <para>
    /// Der Snapshot ist eine isolierte Kopie. Änderungen am globalen Store nach dem
    /// Erstellen des Snapshots sind erst nach einem erneuten Aufruf von <see cref="Refresh"/> sichtbar.
    /// </para>
    /// </remarks>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Wird ausgelöst, wenn kein globaler Store für <typeparamref name="TChild"/> registriert wurde.
    /// </exception>
    /// <example>
    /// <code>
    /// // Snapshot nur mit aktiven Produkten erstellen
    /// relationship.UseSnapshotFromGlobal(prod => prod.IsActive);
    /// relationship.Refresh();
    /// 
    /// // Resultat: Nur aktive Produkte, die zum Elternteil gehören
    /// </code>
    /// </example>
    public void UseSnapshotFromGlobal(Func<TChild, bool>? predicate = null)
    {
        DataSource = _stores.CreateLocalSnapshotFromGlobal(predicate);
    }

    /// <summary>
    /// Aktualisiert die Kind-Sammlung durch Anwenden des Filters auf die Datenquelle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Diese Methode führt folgende Schritte aus:
    /// <list type="number">
    /// <item><description>Löscht die aktuelle <see cref="Childs"/> Collection</description></item>
    /// <item><description>Wendet die <see cref="Filter"/>-Funktion auf alle Elemente der <see cref="DataSource"/> an</description></item>
    /// <item><description>Fügt die gefilterten Elemente zur <see cref="Childs"/> Collection hinzu</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Rufen Sie diese Methode auf, wenn:
    /// <list type="bullet">
    /// <item><description>Die Beziehung initial geladen werden soll</description></item>
    /// <item><description>Die Datenquelle geändert wurde</description></item>
    /// <item><description>Das Elternteil geändert wurde</description></item>
    /// <item><description>Eine Aktualisierung der Kinder erforderlich ist</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Wird ausgelöst, wenn <see cref="DataSource"/> noch nicht gesetzt wurde.
    /// </exception>
    public void Refresh()
    {
        if (_dataSource == null)
            throw new InvalidOperationException("DataSource has not been set. Call UseGlobalDataSource() or UseSnapshotFromGlobal() first.");

        Childs.Clear();
        var filteredItems = _dataSource.Items.Where(child => Filter(Parent, child));
        Childs.AddRange(filteredItems);
    }
}
