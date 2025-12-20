using LiteDB;

namespace DataStores.Persistence;

/// <summary>
/// Persistierungs-Strategie für LiteDB.
/// Speichert und lädt Daten aus einer LiteDB-Datenbank.
/// </summary>
/// <typeparam name="T">Der Typ der zu persistierenden Elemente.</typeparam>
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
/// </list>
/// </para>
/// </remarks>
public class LiteDbPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
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
            catch (LiteException)
            {
                // Bei Datenbankfehlern leere Liste zurückgeben
                return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
            }
            catch (IOException)
            {
                // Bei Dateizugriffsfehlern leere Liste zurückgeben
                return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
            }
        }
    }

    /// <inheritdoc/>
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
            
            // Alle vorhandenen Dokumente löschen
            collection.DeleteAll();
            
            // Neue Dokumente einfügen
            if (items.Count > 0)
            {
                collection.InsertBulk(items);
            }

            return Task.CompletedTask;
        }
    }
}
