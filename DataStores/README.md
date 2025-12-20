# DataStores - Flexible In-Memory Datenspeicherverwaltung

Eine moderne .NET 8 Bibliothek für die Verwaltung von typsicheren In-Memory-Datensammlungen mit umfassender Unterstützung für Persistierung, Event-Handling und hierarchische Beziehungen.

## ?? Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [Installation](#installation)
- [Schnellstart](#schnellstart)
- [Kernkonzepte](#kernkonzepte)
- [Dokumentation](#dokumentation)
- [Beispiele](#beispiele)

## ?? Übersicht

DataStores ist eine leistungsstarke Bibliothek, die eine flexible und typsichere Verwaltung von In-Memory-Datensammlungen ermöglicht. Sie vereinfacht die Arbeit mit Daten in modernen .NET-Anwendungen durch:

### Hauptmerkmale

- ? **Typsichere Datenspeicher**: Generische `IDataStore<T>` Schnittstelle für jede Klasse
- ?? **Globale & Lokale Stores**: Zentrale Singleton-Stores und isolierte lokale Instanzen
- ?? **Thread-Sicherheit**: Alle Operationen sind thread-sicher implementiert
- ?? **Persistierung**: Optionale asynchrone Speicherung mit Auto-Load/Auto-Save
- ?? **Event-System**: Änderungsbenachrichtigungen mit detaillierten EventArgs
- ??????????? **Relationen**: Eltern-Kind-Beziehungen zwischen verschiedenen Entitätstypen
- ?? **DI-Integration**: Nahtlose Integration mit Microsoft.Extensions.DependencyInjection
- ?? **UI-Thread-Support**: SynchronizationContext für WPF/WinForms/MAUI
- ?? **Bulk-Operationen**: AddRange für performante Massen-Operationen
- ?? **Flexible Filter**: Snapshots mit Prädikaten und Custom Comparers

## ?? Installation

### NuGet Package (wenn veröffentlicht)
```bash
dotnet add package DataStores
```

### Als Projekt-Referenz
```xml
<ItemGroup>
  <ProjectReference Include="..\DataStores\DataStores.csproj" />
</ItemGroup>
```

## ?? Schnellstart

### 1. Services registrieren

```csharp
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// DataStores Core registrieren
services.AddDataStoresCore();

// Eigenen Registrar hinzufügen
services.AddDataStoreRegistrar<ProductStoreRegistrar>();

var serviceProvider = services.BuildServiceProvider();
```

### 2. Registrar implementieren

```csharp
using DataStores.Abstractions;
using DataStores.Runtime;

/// <summary>
/// Registriert globale DataStores für die Anwendung.
/// </summary>
public class ProductStoreRegistrar : IDataStoreRegistrar
{
    /// <summary>
    /// Registriert alle benötigten globalen Stores.
    /// </summary>
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // Einfacher In-Memory Store
        registry.RegisterGlobal(new InMemoryDataStore<Product>());
        
        // Store mit Custom Comparer
        var categoryComparer = new CategoryComparer();
        registry.RegisterGlobal(new InMemoryDataStore<Category>(categoryComparer));
    }
}
```

### 3. Bootstrap ausführen

```csharp
// Stores initialisieren
await DataStoreBootstrap.RunAsync(serviceProvider);
```

### 4. Stores verwenden

```csharp
using DataStores.Abstractions;

/// <summary>
/// Service der mit DataStores arbeitet.
/// </summary>
public class ProductService
{
    private readonly IDataStores _stores;
    
    /// <summary>
    /// Initialisiert eine neue Instanz der ProductService Klasse.
    /// </summary>
    public ProductService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Lädt alle Produkte aus dem globalen Store.
    /// </summary>
    public IReadOnlyList<Product> GetAllProducts()
    {
        var store = _stores.GetGlobal<Product>();
        return store.Items;
    }
    
    /// <summary>
    /// Fügt ein neues Produkt hinzu.
    /// </summary>
    public void AddProduct(Product product)
    {
        var store = _stores.GetGlobal<Product>();
        store.Add(product);
    }
    
    /// <summary>
    /// Erstellt einen gefilterten lokalen Store.
    /// </summary>
    public IDataStore<Product> GetActiveProducts()
    {
        return _stores.CreateLocalSnapshotFromGlobal<Product>(
            p => p.IsActive);
    }
}
```

## ?? Kernkonzepte

### IDataStore<T>

Die zentrale Schnittstelle für alle Datenspeicher:

```csharp
/// <summary>
/// Repräsentiert einen Datenspeicher für Elemente vom Typ T.
/// </summary>
public interface IDataStore<T> where T : class
{
    /// <summary>
    /// Ruft die schreibgeschützte Sammlung aller Elemente ab.
    /// </summary>
    IReadOnlyList<T> Items { get; }
    
    /// <summary>
    /// Tritt ein, wenn sich der Datenspeicher ändert.
    /// </summary>
    event EventHandler<DataStoreChangedEventArgs<T>> Changed;
    
    /// <summary>
    /// Fügt ein Element zum Store hinzu.
    /// </summary>
    void Add(T item);
    
    /// <summary>
    /// Fügt mehrere Elemente in einer Bulk-Operation hinzu.
    /// </summary>
    void AddRange(IEnumerable<T> items);
    
    /// <summary>
    /// Entfernt ein Element aus dem Store.
    /// </summary>
    bool Remove(T item);
    
    /// <summary>
    /// Entfernt alle Elemente aus dem Store.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Prüft, ob ein Element im Store enthalten ist.
    /// </summary>
    bool Contains(T item);
}
```

### Globale vs. Lokale Stores

**Globale Stores** sind application-wide Singletons:
```csharp
// Zugriff auf globalen Store
var globalProducts = stores.GetGlobal<Product>();
```

**Lokale Stores** sind isolierte Instanzen:
```csharp
// Neuer leerer lokaler Store
var localStore = stores.CreateLocal<Product>();

// Lokaler Store mit Snapshot aus globalem Store
var filteredStore = stores.CreateLocalSnapshotFromGlobal<Product>(
    p => p.Category == "Electronics");
```

### Event-System

```csharp
var store = stores.GetGlobal<Product>();

/// <summary>
/// Handler für Store-Änderungen.
/// </summary>
store.Changed += (sender, e) =>
{
    switch (e.ChangeType)
    {
        case DataStoreChangeType.Add:
            Console.WriteLine($"Produkt hinzugefügt: {e.AffectedItems[0].Name}");
            break;
        case DataStoreChangeType.Remove:
            Console.WriteLine($"Produkt entfernt");
            break;
        case DataStoreChangeType.Clear:
            Console.WriteLine("Alle Produkte gelöscht");
            break;
        case DataStoreChangeType.BulkAdd:
            Console.WriteLine($"{e.AffectedItems.Count} Produkte hinzugefügt");
            break;
    }
};
```

## ?? Dokumentation

### Detaillierte Guides

- **[API Referenz](Docs/API-Reference.md)** - Vollständige API-Dokumentation aller Klassen und Methoden
- **[Formale Spezifikationen](Docs/Formal-Specifications.md)** - Invarianten, Verhaltensgarantien und formale Regeln
- **[Verwendungsbeispiele](Docs/Usage-Examples.md)** - Praktische Beispiele für häufige Szenarien
- **[Persistierung Guide](Docs/Persistence-Guide.md)** - Daten persistent speichern
- **[Beziehungen Guide](Docs/Relations-Guide.md)** - Eltern-Kind-Beziehungen verwalten

### API-Übersicht

#### Abstractions (Interfaces)
- `IDataStore<T>` - Hauptschnittstelle für Datenspeicher
- `IDataStores` - Facade für den Zugriff auf Stores
- `IGlobalStoreRegistry` - Verwaltung globaler Stores
- `IDataStoreRegistrar` - Registrierung von Stores beim Bootstrap
- `DataStoreChangedEventArgs<T>` - Event-Daten für Änderungen

#### Runtime (Implementierungen)
- `InMemoryDataStore<T>` - Thread-sichere In-Memory-Implementierung
- `DataStoresFacade` - Facade-Implementierung
- `GlobalStoreRegistry` - Thread-sichere Registry
- `LocalDataStoreFactory` - Factory für lokale Stores

#### Persistence (Persistierung)
- `IPersistenceStrategy<T>` - Schnittstelle für Persistierung
- `PersistentStoreDecorator<T>` - Decorator mit Auto-Load/Save
- `IAsyncInitializable` - Marker-Interface für async Initialisierung

#### Relations (Beziehungen)
- `ParentChildRelationship<TParent, TChild>` - Eltern-Kind-Beziehungen

#### Bootstrap (Initialisierung)
- `DataStoreBootstrap` - Bootstrap-Prozess
- `ServiceCollectionExtensions` - DI-Erweiterungen

## ?? Beispiele

### Beispiel 1: Einfacher Product Store

```csharp
/// <summary>
/// Einfaches Produkt-Modell.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Verwendung des Product Stores.
/// </summary>
var stores = serviceProvider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();

// Produkt hinzufügen
productStore.Add(new Product 
{ 
    Id = 1, 
    Name = "Laptop", 
    Price = 999.99m,
    IsActive = true 
});

// Mehrere Produkte auf einmal
productStore.AddRange(new[]
{
    new Product { Id = 2, Name = "Maus", Price = 29.99m, IsActive = true },
    new Product { Id = 3, Name = "Tastatur", Price = 79.99m, IsActive = false }
});

// Produkt finden und entfernen
var toRemove = productStore.Items.FirstOrDefault(p => p.Id == 3);
if (toRemove != null)
{
    productStore.Remove(toRemove);
}
```

### Beispiel 2: Persistenter Store

```csharp
using DataStores.Persistence;

/// <summary>
/// JSON-basierte Persistierungsstrategie.
/// </summary>
public class JsonPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly string _filePath;
    
    public JsonPersistenceStrategy(string filePath)
    {
        _filePath = filePath;
    }
    
    /// <summary>
    /// Lädt Daten aus JSON-Datei.
    /// </summary>
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<T>();
            
        var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }
    
    /// <summary>
    /// Speichert Daten in JSON-Datei.
    /// </summary>
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);
    }
}

/// <summary>
/// Registrar mit persistentem Store.
/// </summary>
public class PersistentProductRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var strategy = new JsonPersistenceStrategy<Product>("products.json");
        
        // Store mit Auto-Load und Auto-Save
        var persistentStore = registry.RegisterPersistent(
            strategy,
            autoLoad: true,
            autoSaveOnChange: true);
            
        // Store wird automatisch beim Bootstrap initialisiert
    }
}
```

### Beispiel 3: Eltern-Kind-Beziehung

```csharp
/// <summary>
/// Kategorie-Modell.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// Service mit Eltern-Kind-Beziehung.
/// </summary>
public class CategoryProductService
{
    private readonly IDataStores _stores;
    
    public CategoryProductService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Erstellt eine Beziehung zwischen Kategorie und Produkten.
    /// </summary>
    public ParentChildRelationship<Category, Product> GetProductsForCategory(Category category)
    {
        var relationship = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
            
        // Globalen Store als Datenquelle verwenden
        relationship.UseGlobalDataSource();
        
        // Kinderprodukte laden
        relationship.Refresh();
        
        return relationship;
    }
    
    /// <summary>
    /// Verwendung der Beziehung.
    /// </summary>
    public void Example()
    {
        var category = new Category { Id = 1, Name = "Electronics" };
        var rel = GetProductsForCategory(category);
        
        // Kinderprodukte abrufen
        var childProducts = rel.Childs.Items;
        
        // Auf Änderungen reagieren
        rel.Childs.Changed += (s, e) => 
        {
            Console.WriteLine($"Produkte in {rel.Parent.Name} geändert");
        };
    }
}
```

### Beispiel 4: UI-Thread-Integration (WPF)

```csharp
using System.Windows;

/// <summary>
/// WPF-ViewModel mit DataStore.
/// </summary>
public class ProductViewModel
{
    private readonly IDataStores _stores;
    
    public ProductViewModel(IDataStores stores)
    {
        _stores = stores;
        InitializeStore();
    }
    
    /// <summary>
    /// Initialisiert den Store mit SynchronizationContext.
    /// </summary>
    private void InitializeStore()
    {
        // Store mit UI-Thread-Context erstellen
        var syncContext = SynchronizationContext.Current;
        var store = new InMemoryDataStore<Product>(
            comparer: null,
            synchronizationContext: syncContext);
            
        // Events werden automatisch auf UI-Thread gemarshallt
        store.Changed += (s, e) =>
        {
            // Läuft auf UI-Thread - kann direkt UI aktualisieren
            Application.Current.Dispatcher.Invoke(() =>
            {
                RefreshUI();
            });
        };
    }
    
    private void RefreshUI()
    {
        // UI-Aktualisierung
    }
}
```

## ??? Projekt-Struktur

```
DataStores/
??? Abstractions/
?   ??? IDataStore.cs                              # Haupt-Store-Interface
?   ??? IDataStores.cs                             # Facade-Interface
?   ??? IGlobalStoreRegistry.cs                    # Registry-Interface
?   ??? IDataStoreRegistrar.cs                     # Registrar-Interface
?   ??? DataStoreChangedEventArgs.cs               # Event-Argumente
?   ??? GlobalStoreNotRegisteredException.cs       # Exception für fehlende Registrierung
?   ??? GlobalStoreAlreadyRegisteredException.cs   # Exception für doppelte Registrierung
?
??? Runtime/
?   ??? InMemoryDataStore.cs                       # In-Memory-Implementierung
?   ??? DataStoresFacade.cs                        # Facade-Implementierung
?   ??? GlobalStoreRegistry.cs                     # Registry-Implementierung
?   ??? ILocalDataStoreFactory.cs                  # Factory für lokale Stores
?
??? Persistence/
?   ??? IPersistenceStrategy.cs                    # Persistierungs-Interface
?   ??? PersistentStoreDecorator.cs                # Decorator für Persistierung
?   ??? IAsyncInitializable.cs                     # Async-Init-Interface
?   ??? PersistentStoreRegistrationExtensions.cs   # Helper-Erweiterungen
?
??? Relations/
?   ??? ParentChildRelationship.cs                 # Eltern-Kind-Beziehung
?
??? Bootstrap/
?   ??? DataStoreBootstrap.cs                      # Bootstrap-Prozess
?   ??? ServiceCollectionExtensions.cs             # DI-Erweiterungen
?
??? Docs/
    ??? API-Reference.md                           # API-Referenz
    ??? Formal-Specifications.md                   # Formale Spezifikationen
    ??? Usage-Examples.md                          # Verwendungsbeispiele
    ??? Persistence-Guide.md                       # Persistierung-Guide
    ??? Relations-Guide.md                         # Beziehungen-Guide
```

## ?? Anforderungen

- .NET 8.0 oder höher
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1+

## ?? Migration & Updates

Prüfen Sie die [CHANGELOG.md](CHANGELOG.md) für Informationen zu Breaking Changes und neuen Features.

## ?? Beitragen

Contributions sind willkommen! Siehe [CONTRIBUTING.md](../CONTRIBUTING.md) für Details.

## ?? Lizenz

[Lizenz hier einfügen]

## ?? Maintainer

[Ihre Informationen hier]

---

**Version**: 1.0.0  
**Letzte Aktualisierung**: Januar 2025  
**Repository**: [GitHub-Link]
