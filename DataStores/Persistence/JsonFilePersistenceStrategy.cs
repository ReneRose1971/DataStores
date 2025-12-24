using System.Text.Json;

namespace DataStores.Persistence;

/// <summary>
/// Persistierungs-Strategie für JSON-Dateien.
/// Speichert und lädt Daten als JSON in einer Datei.
/// </summary>
/// <typeparam name="T">Der Typ der zu persistierenden Elemente.</typeparam>
public class JsonFilePersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private Func<IReadOnlyList<T>>? _itemsProvider;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="JsonFilePersistenceStrategy{T}"/> Klasse.
    /// </summary>
    /// <param name="filePath">Der vollständige Pfad zur JSON-Datei.</param>
    /// <param name="jsonOptions">Optionale JSON-Serialisierungsoptionen. Wenn null, werden Standardoptionen verwendet.</param>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="filePath"/> null oder leer ist.</exception>
    public JsonFilePersistenceStrategy(string filePath, JsonSerializerOptions? jsonOptions = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        _filePath = filePath;
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Legt den Items-Anbieter fest.
    /// </summary>
    /// <param name="itemsProvider">Die Funktion, die die Elemente bereitstellt.</param>
    public void SetItemsProvider(Func<IReadOnlyList<T>>? itemsProvider)
    {
        _itemsProvider = itemsProvider;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<T>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<T>();
            }

            var items = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            return items?.ToList() ?? (IReadOnlyList<T>)Array.Empty<T>();
        }
        catch (JsonException)
        {
            // JSON-Deserialisierungsfehler - leere Liste zurückgeben
            return Array.Empty<T>();
        }
        catch (IOException)
        {
            // Datei-Zugriffsfehler - leere Liste zurückgeben
            return Array.Empty<T>();
        }
    }

    /// <inheritdoc/>
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        // Verzeichnis erstellen, falls nicht vorhanden
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// JSON-Strategie: Atomare Schreiboperation für die gesamte Datei.
    /// </para>
    /// <para>
    /// Nutzt den ItemsProvider (vom PersistentStoreDecorator gesetzt) um die aktuelle 
    /// Liste vom Store zu holen. Dadurch wird sichergestellt, dass bei PropertyChanged-Events 
    /// die vollständige aktuelle Liste gespeichert wird.
    /// </para>
    /// </remarks>
    public async Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_itemsProvider == null)
        {
            throw new InvalidOperationException(
                "ItemsProvider wurde nicht gesetzt. " +
                "UpdateSingleAsync erfordert einen ItemsProvider, der vom PersistentStoreDecorator " +
                "über SetItemsProvider() bereitgestellt wird.");
        }

        // JSON-Strategie: Atomare Schreiboperation = komplettes Neu-Schreiben der aktuellen Liste
        var currentItems = _itemsProvider();
        await SaveAllAsync(currentItems, cancellationToken);
    }
}
