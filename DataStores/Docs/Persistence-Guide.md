# DataStores - Persistierung Guide

Umfassender Leitfaden zur Implementierung von Datenpersistierung mit DataStores.

## ?? Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [IPersistenceStrategy](#ipersistencestrategy)
- [PersistentStoreDecorator](#persistentstoredecorator)
- [Implementierungsbeispiele](#implementierungsbeispiele)
- [Best Practices](#best-practices)
- [Fehlerbehandlung](#fehlerbehandlung)

---

## Übersicht

Die DataStores-Bibliothek bietet ein flexibles Persistierungs-System, das es ermöglicht, In-Memory-Daten automatisch zu speichern und zu laden.

### Hauptkomponenten

1. **IPersistenceStrategy\<T\>** - Definiert, WIE Daten gespeichert werden
2. **PersistentStoreDecorator\<T\>** - Umhüllt einen Store mit Persistierung
3. **IAsyncInitializable** - Ermöglicht asynchrones Laden beim Bootstrap

### Funktionen

- ? **Auto-Load**: Daten werden beim Bootstrap automatisch geladen
- ? **Auto-Save**: Änderungen werden automatisch gespeichert
- ? **Async/Await**: Vollständig asynchrone API
- ? **Flexibel**: Jedes Speichermedium (JSON, XML, Datenbank, Cloud, etc.)
- ? **Thread-sicher**: Race-Condition-Schutz mit Semaphoren

---

## IPersistenceStrategy

### Interface-Definition

```csharp
/// <summary>
/// Definiert eine Strategie zum Persistieren und Laden von Daten.
/// </summary>
public interface IPersistenceStrategy<T> where T : class
{
    /// <summary>
    /// Lädt alle Elemente aus dem Persistenz-Store.
    /// </summary>
    Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Speichert alle Elemente im Persistenz-Store.
    /// </summary>
    Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default);
}
```

### Implementierungs-Pattern

```csharp
/// <summary>
/// Basis-Template für eine Persistierungsstrategie.
/// </summary>
public class MyPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    /// <summary>
    /// Lädt Daten - Implementierung spezifisch für Ihr Speichermedium.
    /// </summary>
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Prüfen, ob Daten existieren
            if (!DataExists())
                return Array.Empty<T>();
            
            // 2. Daten laden
            var data = await LoadDataAsync(cancellationToken);
            
            // 3. Deserialisieren/Konvertieren
            var items = ConvertToItems(data);
            
            return items;
        }
        catch (Exception ex)
        {
            // Logging und Fehlerbehandlung
            LogError("Load", ex);
            throw;
        }
    }
    
    /// <summary>
    /// Speichert Daten - Implementierung spezifisch für Ihr Speichermedium.
    /// </summary>
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Daten serialisieren/konvertieren
            var data = ConvertFromItems(items);
            
            // 2. Speichern
            await SaveDataAsync(data, cancellationToken);
        }
        catch (Exception ex)
        {
            // Logging und Fehlerbehandlung
            LogError("Save", ex);
            throw;
        }
    }
    
    private bool DataExists() => throw new NotImplementedException();
    private Task<object> LoadDataAsync(CancellationToken ct) => throw new NotImplementedException();
    private List<T> ConvertToItems(object data) => throw new NotImplementedException();
    private object ConvertFromItems(IReadOnlyList<T> items) => throw new NotImplementedException();
    private Task SaveDataAsync(object data, CancellationToken ct) => throw new NotImplementedException();
    private void LogError(string operation, Exception ex) { }
}
```

---

## PersistentStoreDecorator

### Verwendung

```csharp
/// <summary>
/// Registrar, der einen persistenten Store erstellt.
/// </summary>
public class MyRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // Strategie erstellen
        var strategy = new JsonPersistenceStrategy<Product>("products.json");
        
        // Persistenten Store registrieren
        var decorator = registry.RegisterPersistent(
            strategy,
            autoLoad: true,           // Beim Bootstrap laden
            autoSaveOnChange: true);  // Bei jeder Änderung speichern
            
        // decorator ist jetzt registriert und wird beim Bootstrap initialisiert
    }
}
```

### Konfigurationsoptionen

#### 1. Auto-Load: true (Empfohlen)

```csharp
// Daten werden beim Bootstrap automatisch geladen
registry.RegisterPersistent(strategy, autoLoad: true, autoSaveOnChange: true);

// Bootstrap lädt Daten:
await DataStoreBootstrap.RunAsync(serviceProvider);
```

#### 2. Auto-Load: false (Manuell)

```csharp
var decorator = registry.RegisterPersistent(strategy, autoLoad: false, autoSaveOnChange: true);

// Manuell laden:
await decorator.InitializeAsync();
```

#### 3. Auto-Save: true (Empfohlen für die meisten Fälle)

```csharp
// Jede Änderung wird automatisch gespeichert
registry.RegisterPersistent(strategy, autoLoad: true, autoSaveOnChange: true);

var store = stores.GetGlobal<Product>();
store.Add(new Product { ... }); // Wird automatisch gespeichert
```

#### 4. Auto-Save: false (Manuell)

```csharp
var decorator = registry.RegisterPersistent(strategy, autoLoad: true, autoSaveOnChange: false);

// Manuell speichern:
await strategy.SaveAllAsync(store.Items);
```

---

## Implementierungsbeispiele

### 1. JSON-Persistierung (System.Text.Json)

```csharp
using System.Text.Json;

/// <summary>
/// JSON-basierte Persistierung mit System.Text.Json.
/// </summary>
public class JsonPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    
    public JsonPersistenceStrategy(string filePath)
    {
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            // Weitere Optionen nach Bedarf
        };
    }
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<T>();
            
        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<T>>(json, _options);
        return items ?? new List<T>();
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        // Verzeichnis erstellen, falls nicht vorhanden
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
            
        var json = JsonSerializer.Serialize(items, _options);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }
}
```

### 2. XML-Persistierung

```csharp
using System.Xml.Serialization;

/// <summary>
/// XML-basierte Persistierung.
/// </summary>
public class XmlPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly string _filePath;
    private readonly XmlSerializer _serializer;
    
    public XmlPersistenceStrategy(string filePath)
    {
        _filePath = filePath;
        _serializer = new XmlSerializer(typeof(List<T>));
    }
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<T>();
            
        await using var stream = File.OpenRead(_filePath);
        var items = _serializer.Deserialize(stream) as List<T>;
        return items ?? new List<T>();
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
            
        await using var stream = File.Create(_filePath);
        _serializer.Serialize(stream, items.ToList());
    }
}
```

### 3. SQLite-Persistierung

```csharp
using Microsoft.Data.Sqlite;
using Dapper;

/// <summary>
/// SQLite-basierte Persistierung mit Dapper.
/// </summary>
public class SqlitePersistenceStrategy<T> : IPersistenceStrategy<T> 
    where T : class, IEntity
{
    private readonly string _connectionString;
    private readonly string _tableName;
    
    public SqlitePersistenceStrategy(string databasePath, string tableName)
    {
        _connectionString = $"Data Source={databasePath}";
        _tableName = tableName;
        EnsureTableExists();
    }
    
    private void EnsureTableExists()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute($@"
            CREATE TABLE IF NOT EXISTS {_tableName} (
                Data TEXT NOT NULL
            )");
    }
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        var json = await connection.QueryAsync<string>(
            $"SELECT Data FROM {_tableName}");
            
        return json
            .Select(j => JsonSerializer.Deserialize<T>(j))
            .Where(item => item != null)
            .Cast<T>()
            .ToList();
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync($"DELETE FROM {_tableName}");
        
        foreach (var item in items)
        {
            var json = JsonSerializer.Serialize(item);
            await connection.ExecuteAsync(
                $"INSERT INTO {_tableName} (Data) VALUES (@Json)",
                new { Json = json });
        }
    }
}
```

### 4. Entity Framework Core

```csharp
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core-Persistierung.
/// </summary>
public class EfCorePersistenceStrategy<T> : IPersistenceStrategy<T> 
    where T : class
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    
    public EfCorePersistenceStrategy(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<T>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        // Strategie 1: Alte löschen, neue hinzufügen
        var existing = await context.Set<T>().ToListAsync(cancellationToken);
        context.Set<T>().RemoveRange(existing);
        await context.Set<T>().AddRangeAsync(items, cancellationToken);
        
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

### 5. Azure Blob Storage

```csharp
using Azure.Storage.Blobs;

/// <summary>
/// Azure Blob Storage-Persistierung.
/// </summary>
public class AzureBlobPersistenceStrategy<T> : IPersistenceStrategy<T> 
    where T : class
{
    private readonly BlobContainerClient _containerClient;
    private readonly string _blobName;
    
    public AzureBlobPersistenceStrategy(string connectionString, string containerName, string blobName)
    {
        _containerClient = new BlobContainerClient(connectionString, containerName);
        _blobName = blobName;
        _containerClient.CreateIfNotExists();
    }
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(_blobName);
        
        if (!await blobClient.ExistsAsync(cancellationToken))
            return Array.Empty<T>();
            
        var response = await blobClient.DownloadContentAsync(cancellationToken);
        var json = response.Value.Content.ToString();
        var items = JsonSerializer.Deserialize<List<T>>(json);
        return items ?? new List<T>();
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(_blobName);
        var json = JsonSerializer.Serialize(items);
        var content = BinaryData.FromString(json);
        
        await blobClient.UploadAsync(content, overwrite: true, cancellationToken);
    }
}
```

---

## Best Practices

### 1. Fehlerbehandlung

```csharp
/// <summary>
/// Robuste Fehlerbehandlung in Persistierungsstrategie.
/// </summary>
public class RobustPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly ILogger<RobustPersistenceStrategy<T>> _logger;
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoadDataInternalAsync(cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogInformation("Datei nicht gefunden, leere Liste zurückgeben: {Message}", ex.Message);
            return Array.Empty<T>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON-Deserialisierungsfehler, leere Liste zurückgeben");
            return Array.Empty<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kritischer Fehler beim Laden");
            throw; // Kritische Fehler weiterleiten
        }
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveDataInternalAsync(items, cancellationToken);
            _logger.LogInformation("{Count} Elemente erfolgreich gespeichert", items.Count);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O-Fehler beim Speichern");
            // Retry-Logik hier
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unerwarteter Fehler beim Speichern");
            throw;
        }
    }
    
    private Task<IReadOnlyList<T>> LoadDataInternalAsync(CancellationToken ct) => throw new NotImplementedException();
    private Task SaveDataInternalAsync(IReadOnlyList<T> items, CancellationToken ct) => throw new NotImplementedException();
}
```

### 2. Backup-Strategie

```csharp
/// <summary>
/// Persistierungsstrategie mit automatischem Backup.
/// </summary>
public class BackupPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly string _filePath;
    private readonly string _backupPath;
    
    public BackupPersistenceStrategy(string filePath)
    {
        _filePath = filePath;
        _backupPath = $"{filePath}.backup";
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        // 1. Aktuelles Backup erstellen
        if (File.Exists(_filePath))
        {
            File.Copy(_filePath, _backupPath, overwrite: true);
        }
        
        try
        {
            // 2. Neue Daten speichern
            var json = JsonSerializer.Serialize(items);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }
        catch
        {
            // 3. Bei Fehler: Backup wiederherstellen
            if (File.Exists(_backupPath))
            {
                File.Copy(_backupPath, _filePath, overwrite: true);
            }
            throw;
        }
    }
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_filePath))
                return Array.Empty<T>();
                
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            // Bei Fehler: Backup versuchen
            if (File.Exists(_backupPath))
            {
                var json = await File.ReadAllTextAsync(_backupPath, cancellationToken);
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            throw;
        }
    }
}
```

### 3. Versionierung

```csharp
/// <summary>
/// Datei-Format mit Versionsinformationen.
/// </summary>
public class VersionedData<T>
{
    public int Version { get; set; } = 1;
    public DateTime SavedAt { get; set; }
    public List<T> Items { get; set; } = new();
}

/// <summary>
/// Strategie mit Versionierungs-Support.
/// </summary>
public class VersionedPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private const int CurrentVersion = 1;
    
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync("data.json", cancellationToken);
        var versionedData = JsonSerializer.Deserialize<VersionedData<T>>(json);
        
        if (versionedData == null)
            return Array.Empty<T>();
            
        // Migrations-Logik basierend auf Version
        if (versionedData.Version < CurrentVersion)
        {
            versionedData = MigrateData(versionedData);
        }
        
        return versionedData.Items;
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        var versionedData = new VersionedData<T>
        {
            Version = CurrentVersion,
            SavedAt = DateTime.UtcNow,
            Items = items.ToList()
        };
        
        var json = JsonSerializer.Serialize(versionedData);
        await File.WriteAllTextAsync("data.json", json, cancellationToken);
    }
    
    private VersionedData<T> MigrateData(VersionedData<T> oldData)
    {
        // Migrations-Logik hier
        return oldData;
    }
}
```

---

## Fehlerbehandlung

### Häufige Fehlerszenarien

#### 1. Datei/Datenbank nicht erreichbar
```csharp
catch (IOException ex)
{
    _logger.LogError(ex, "Speichermedium nicht erreichbar");
    // Retry mit exponential backoff
    // Oder: Fallback auf temporären Speicher
}
```

#### 2. Deserialisierungsfehler
```csharp
catch (JsonException ex)
{
    _logger.LogError(ex, "Ungültiges Datenformat");
    // Backup verwenden oder leere Liste zurückgeben
    return Array.Empty<T>();
}
```

#### 3. Berechtigungsfehler
```csharp
catch (UnauthorizedAccessException ex)
{
    _logger.LogError(ex, "Keine Berechtigung zum Zugriff");
    // Benutzer informieren
    throw new PersistenceException("Keine Schreibberechtigung", ex);
}
```

---

**Version**: 1.0.0  
**Weitere Informationen**: Siehe [API-Referenz](API-Reference.md) und [Usage-Examples](Usage-Examples.md)
