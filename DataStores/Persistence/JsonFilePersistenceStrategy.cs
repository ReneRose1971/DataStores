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

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="JsonFilePersistenceStrategy{T}"/> Klasse.
    /// </summary>
    /// <param name="filePath">Der vollständige Pfad zur JSON-Datei.</param>
    /// <param name="jsonOptions">Optionale JSON-Serialisierungsoptionen. Wenn null, werden Standardoptionen verwendet.</param>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn <paramref name="filePath"/> null oder leer ist.</exception>
    public JsonFilePersistenceStrategy(string filePath, JsonSerializerOptions? jsonOptions = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        _filePath = filePath;
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
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
            throw new ArgumentNullException(nameof(items));

        // Verzeichnis erstellen, falls nicht vorhanden
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }
}
