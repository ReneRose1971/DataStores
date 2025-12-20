using LiteDB;
using DataStores.Abstractions;

namespace DataStores.Persistence;

/// <summary>
/// Persistierungs-Strategie für LiteDB.
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
/// </list>
/// </para>
/// <para>
/// <b>SaveAllAsync - Delta-Synchronisierung:</b>
/// Verwendet dieselbe bewährte Delta-Logik wie DataToolKit.LiteDbRepository.
/// </para>
/// </remarks>
public class LiteDbPersistenceStrategy<T> : IPersistenceStrategy<T> 
    where T : EntityBase
{
    private readonly string _databasePath;
    private readonly string _collectionName;
    private readonly object _lock = new();

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="LiteDbPersistenceStrategy{T}"/> Klasse.
    /// </summary>
    /// <param name="databasePath">Der vollständige Pfad zur LiteDB-Datenbankdatei.</param>
    /// <param name="collectionName">
    /// Der Name der Collection in der Datenbank. 
    /// Wenn null, wird der Typname verwendet.
    /// </param>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="databasePath"/> null oder leer ist.</exception>
    public LiteDbPersistenceStrategy(string databasePath, string? collectionName = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentNullException(nameof(databasePath));

        _databasePath = databasePath;
        _collectionName = collectionName ?? typeof(T).Name;
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
                // LiteDB-spezifische Fehler durchreichen (z.B. korrupte Datenbank)
                throw new InvalidOperationException($"Fehler beim Laden aus LiteDB: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                // Dateizugriffsfehler durchreichen
                throw new InvalidOperationException($"Dateizugriffsfehler bei LiteDB: {ex.Message}", ex);
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Synchronisiert den Store-Zustand mit der LiteDB-Datenbank (Delta-Save).
    /// Verwendet dieselbe bewährte Delta-Logik wie DataToolKit.LiteDbRepository.
    /// </para>
    /// <para>
    /// <b>Operationen:</b>
    /// <list type="bullet">
    /// <item><description>UPDATE: Items mit Id > 0 die in DB existieren UND inhaltlich geändert wurden</description></item>
    /// <item><description>DELETE: Items in DB die nicht in items-Liste sind</description></item>
    /// <item><description>INSERT: Items mit Id = 0 ODER Items mit Id > 0 die nicht in DB existieren</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Transaktion:</b> Alle Operationen werden in einer LiteDB-Transaktion ausgeführt.
    /// Bei Fehlern erfolgt automatisch ein Rollback.
    /// </para>
    /// <para>
    /// <b>ID-Handling:</b> LiteDB schreibt automatisch vergebene IDs in die Objekte zurück.
    /// Für EntityBase.Id ist dies standardmäßig konfiguriert.
    /// </para>
    /// </remarks>
    public Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        lock (_lock)
        {
            // Verzeichnis erstellen, falls nicht vorhanden
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var db = new LiteDatabase(_databasePath);
            var collection = db.GetCollection<T>(_collectionName);
            
            // Aktuellen DB-Zustand laden
            var existing = collection.FindAll().ToList();
            
            // Delta berechnen (wie in DataToolKit.LiteDbRepository)
            var existingById = existing.Where(e => e.Id > 0).ToDictionary(e => e.Id);
            var incomingById = items.Where(e => e.Id > 0).ToDictionary(e => e.Id);
            
            // UPDATE: Items mit Id > 0 die in DB existieren
            var toUpdate = items.Where(i => i.Id > 0 && existingById.ContainsKey(i.Id)).ToList();
            
            // DELETE: IDs in DB die nicht mehr in incoming sind
            var toDeleteIds = existingById.Keys.Except(incomingById.Keys).ToList();
            
            // INSERT: 
            // (a) Neue Items (Id = 0)
            // (b) Items mit Id > 0 die NICHT in DB existieren (missing IDs policy)
            var toInsert = items.Where(i => i.Id == 0).ToList();
            foreach (var item in items.Where(i => i.Id > 0))
            {
                if (!existingById.ContainsKey(item.Id))
                {
                    toInsert.Add(item);
                }
            }
            
            // Nur Transaktion starten wenn Änderungen vorhanden
            if (toInsert.Count > 0 || toUpdate.Count > 0 || toDeleteIds.Count > 0)
            {
                if (!db.BeginTrans())
                    throw new InvalidOperationException("Transaktion konnte nicht gestartet werden.");
                
                try
                {
                    // UPDATE: Bestehende Entities aktualisieren
                    if (toUpdate.Count > 0)
                    {
                        foreach (var item in toUpdate)
                        {
                            collection.Update(item);
                        }
                    }
                    
                    // DELETE: Nicht mehr vorhandene Entities löschen
                    if (toDeleteIds.Count > 0)
                    {
                        collection.DeleteMany(x => toDeleteIds.Contains(x.Id));
                    }
                    
                    // INSERT: Neue Entities einfügen
                    if (toInsert.Count > 0)
                    {
                        foreach (var item in toInsert)
                        {
                            // Insert schreibt die vergebene ID automatisch in item.Id zurück
                            // Dies funktioniert weil wir oben den BsonMapper konfiguriert haben
                            collection.Insert(item);
                        }
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
    }
}
