using LiteDB;
using DataStores.Abstractions;

namespace DataStores.Persistence;

/// <summary>
/// Persistierungs-Strategie für LiteDB mit Delta-Synchronisierung.
/// Speichert und lädt Daten aus einer LiteDB-Datenbank.
/// </summary>
/// <typeparam name="T">Der Typ der zu persistierenden Elemente. Muss von <see cref="EntityBase"/> erben.</typeparam>
/// <remarks>
/// <para>
/// LiteDB ist eine einfache, schnelle und leichtgewichtige NoSQL-Datenbank für .NET.
/// Diese Strategie speichert Objekte als Dokumente in Collections.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
/// <item><description>Serverless - keine Installation erforderlich</description></item>
/// <item><description>Eine einzelne Datenbankdatei</description></item>
/// <item><description>ACID-Transaktionen</description></item>
/// <item><description>Thread-sicher</description></item>
/// <item><description>Unterstützt LINQ-Queries</description></item>
/// <item><description>Automatische ID-Vergabe für neue Entitäten</description></item>
/// <item><description>Delta-basierte Synchronisierung (INSERT/DELETE)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Persistierungs-Strategie:</b>
/// </para>
/// <list type="bullet">
/// <item><description>SaveAllAsync: Berechnet Delta und führt INSERT/DELETE aus</description></item>
/// <item><description>UpdateSingleAsync: Effiziente Einzel-Entity-Updates via LiteCollection.Update</description></item>
/// </list>
/// </remarks>
public class LiteDbPersistenceStrategy<T> : IPersistenceStrategy<T>
    where T : EntityBase
{
    private readonly string _databasePath;
    private readonly string _collectionName;
    private readonly IDataStoreDiffService _diffService;
    private readonly object _lock = new();

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="LiteDbPersistenceStrategy{T}"/> Klasse.
    /// </summary>
    /// <param name="databasePath">Der vollständige Pfad zur LiteDB-Datenbankdatei.</param>
    /// <param name="collectionName">
    /// Der Name der Collection in der Datenbank. 
    /// Wenn null, wird der Typname verwendet.
    /// </param>
    /// <param name="diffService">Service zur Berechnung von Diffs zwischen DataStore und Datenbank.</param>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="databasePath"/> null oder leer ist.</exception>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="diffService"/> null ist.</exception>
    public LiteDbPersistenceStrategy(
        string databasePath, 
        string? collectionName,
        IDataStoreDiffService diffService)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentNullException(nameof(databasePath));
        }

        _databasePath = databasePath;
        _collectionName = collectionName ?? typeof(T).Name;
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
    }

    /// <inheritdoc/>
    public void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider)
    {
        // LiteDB braucht den Provider nicht - collection.Update() ist ausreichend
        // No-Op: Methode existiert nur wegen Interface-Konformität
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<T>(_collectionName);
                var items = collection.FindAll().ToList();
                return Task.FromResult<IReadOnlyList<T>>(items);
            }
            catch (LiteException ex)
            {
                throw new InvalidOperationException($"Fehler beim Laden aus LiteDB: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Dateizugriffsfehler bei LiteDB: {ex.Message}", ex);
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Berechnet das Delta zwischen dem aktuellen Store-Zustand und der Datenbank
    /// und führt die notwendigen INSERT/DELETE-Operationen transaktional aus.
    /// </para>
    /// <para>
    /// <b>Operationen:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>INSERT: Neue Entities (Id = 0) - LiteDB schreibt IDs zurück in die Objekte</description></item>
    /// <item><description>DELETE: Entities die aus dem Store entfernt wurden</description></item>
    /// </list>
    /// <para>
    /// <b>Transaktion:</b> Alle Operationen werden in einer LiteDB-Transaktion ausgeführt.
    /// Bei Fehlern erfolgt automatisch ein Rollback.
    /// </para>
    /// </remarks>
    public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        lock (_lock)
        {
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var db = new LiteDatabase(_databasePath);
            var collection = db.GetCollection<T>(_collectionName);
            
            var databaseItems = collection.FindAll().ToList();

            var diff = _diffService.ComputeDiff(items, databaseItems);

            if (!diff.HasChanges)
            {
                return Task.CompletedTask;
            }

            if (!db.BeginTrans())
            {
                throw new InvalidOperationException("Transaktion konnte nicht gestartet werden.");
            }

            try
            {
                if (diff.ToInsert.Count > 0)
                {
                    foreach (var item in diff.ToInsert)
                    {
                        collection.Insert(item);
                    }
                }

                if (diff.ToDelete.Count > 0)
                {
                    var idsToDelete = diff.ToDelete.Select(e => e.Id).ToList();
                    collection.DeleteMany(x => idsToDelete.Contains(x.Id));
                }

                db.Commit();
            }
            catch
            {
                db.Rollback();
                throw;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Aktualisiert eine einzelne Entity (wird vom PersistentStoreDecorator bei PropertyChanged aufgerufen).
    /// </summary>
    /// <param name="entity">Die zu aktualisierende Entity (muss Id > 0 haben).</param>
    /// <param name="cancellationToken">Token zur Abbruchsteuerung.</param>
    /// <returns>Ein Task, der die asynchrone Operation repräsentiert.</returns>
    /// <remarks>
    /// <para>
    /// LiteDB-Strategie: Effiziente Einzel-Entity-Update via LiteCollection.Update.
    /// </para>
    /// <para>
    /// <b>Wichtig:</b> Entities mit Id ≤ 0 werden ignoriert (noch nicht persistiert).
    /// </para>
    /// </remarks>
    public Task UpdateSingleAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (entity.Id <= 0)
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            using var db = new LiteDatabase(_databasePath);
            var collection = db.GetCollection<T>(_collectionName);
            collection.Update(entity);
        }

        return Task.CompletedTask;
    }
}
