# DataStores - Flexible In-Memory Datenspeicherverwaltung

Eine moderne .NET 8 Bibliothek f�r die Verwaltung von typsicheren In-Memory-Datensammlungen mit umfassender Unterst�tzung f�r Persistierung, Event-Handling und hierarchische Beziehungen.

## ?? Inhaltsverzeichnis

- [�bersicht](#�bersicht)
- [Installation](#installation)
- [Schnellstart](#schnellstart)
- [Kernkonzepte](#kernkonzepte)
- [Dokumentation](#dokumentation)
- [Beispiele](#beispiele)

## ?? �bersicht

DataStores ist eine leistungsstarke Bibliothek, die eine flexible und typsichere Verwaltung von In-Memory-Datensammlungen erm�glicht. Sie vereinfacht die Arbeit mit Daten in modernen .NET-Anwendungen durch:

### Hauptmerkmale

- ? **Typsichere Datenspeicher**: Generische `IDataStore<T>` Schnittstelle f�r jede Klasse
- ?? **Globale & Lokale Stores**: Zentrale Singleton-Stores und isolierte lokale Instanzen
- ?? **Thread-Sicherheit**: Alle Operationen sind thread-sicher implementiert
- ?? **Persistierung**: Optionale asynchrone Speicherung mit Auto-Load/Auto-Save
- ?? **Event-System**: �nderungsbenachrichtigungen mit detaillierten EventArgs
- ??????????? **Relationen**: Eltern-Kind-Beziehungen zwischen verschiedenen Entit�tstypen
- ?? **DI-Integration**: Nahtlose Integration mit Microsoft.Extensions.DependencyInjection
- ?? **UI-Thread-Support**: SynchronizationContext f�r WPF/WinForms/MAUI
- ?? **Bulk-Operationen**: AddRange f�r performante Massen-Operationen
- ?? **Flexible Filter**: Snapshots mit Pr�dikaten und Custom Comparers

## ?? Installation

### NuGet Package (wenn ver�ffentlicht)
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

// Eigenen Registrar hinzuf�gen
services.AddDataStoreRegistrar<ProductStoreRegistrar>();

var serviceProvider = services.BuildServiceProvider();
```

### 2. Registrar implementieren

```csharp
using DataStores.Abstractions;
using DataStores.Runtime;

/// <summary>
/// Registriert globale DataStores f�r die Anwendung.
/// </summary>
public class ProductStoreRegistrar : IDataStoreRegistrar
{
    /// <summary>
    /// Registriert alle ben�tigten globalen Stores.
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

### 3. Bootstrap ausf�hren

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
    /// L�dt alle Produkte aus dem globalen Store.
    /// </summary>
    public IReadOnlyList<Product> GetAllProducts()
    {
        var store = _stores.GetGlobal<Product>();
        return store.Items;
    }
    
    /// <summary>
    /// F�gt ein neues Produkt hinzu.
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

Die zentrale Schnittstelle f�r alle Datenspeicher:

```csharp
/// <summary>
/// Repr�sentiert einen Datenspeicher f�r Elemente vom Typ T.
/// </summary>
public interface IDataStore<T> where T : class
{
    /// <summary>
    /// Ruft die schreibgesch�tzte Sammlung aller Elemente ab.
    /// </summary>
    IReadOnlyList<T> Items { get; }
    
    /// <summary>
    /// Tritt ein, wenn sich der Datenspeicher �ndert.
    /// </summary>
    event EventHandler<DataStoreChangedEventArgs<T>> Changed;
    
    /// <summary>
    /// F�gt ein Element zum Store hinzu.
    /// </summary>
    void Add(T item);
    
    /// <summary>
    /// F�gt mehrere Elemente in einer Bulk-Operation hinzu.
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
    /// Pr�ft, ob ein Element im Store enthalten ist.
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
/// Handler f�r Store-�nderungen.
/// </summary>
store.Changed += (sender, e) =>
{
    switch (e.ChangeType)
    {
        case DataStoreChangeType.Add:
            Console.WriteLine($"Produkt hinzugef�gt: {e.AffectedItems[0].Name}");
            break;
        case DataStoreChangeType.Remove:
            Console.WriteLine($"Produkt entfernt");
            break;
        case DataStoreChangeType.Clear:
            Console.WriteLine("Alle Produkte gel�scht");
            break;
        case DataStoreChangeType.BulkAdd:
            Console.WriteLine($"{e.AffectedItems.Count} Produkte hinzugef�gt");
            break;
    }
};
```

## ?? Dokumentation

### Detaillierte Guides

- **[API Referenz](Docs/API-Reference.md)** - Vollst�ndige API-Dokumentation aller Klassen und Methoden
- **[Formale Spezifikationen](Docs/Formal-Specifications.md)** - Invarianten, Verhaltensgarantien und formale Regeln
- **[Verwendungsbeispiele](Docs/Usage-Examples.md)** - Praktische Beispiele f�r h�ufige Szenarien
- **[Persistierung Guide](Docs/Persistence-Guide.md)** - Daten persistent speichern
- **[Beziehungen Guide](Docs/Relations-Guide.md)** - Eltern-Kind-Beziehungen verwalten

### API-�bersicht

#### Abstractions (Interfaces)
- `IDataStore<T>` - Hauptschnittstelle f�r Datenspeicher
- `IDataStores` - Facade f�r den Zugriff auf Stores
- `IGlobalStoreRegistry` - Verwaltung globaler Stores
- `IDataStoreRegistrar` - Registrierung von Stores beim Bootstrap
- `DataStoreChangedEventArgs<T>` - Event-Daten f�r �nderungen

#### Runtime (Implementierungen)
- `InMemoryDataStore<T>` - Thread-sichere In-Memory-Implementierung
- `DataStoresFacade` - Facade-Implementierung
- `GlobalStoreRegistry` - Thread-sichere Registry
- `LocalDataStoreFactory` - Factory f�r lokale Stores

#### Persistence (Persistierung)
- `IPersistenceStrategy<T>` - Schnittstelle f�r Persistierung
- `PersistentStoreDecorator<T>` - Decorator mit Auto-Load/Save
- `IAsyncInitializable` - Marker-Interface f�r async Initialisierung

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

// Produkt hinzuf�gen
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
    /// L�dt Daten aus JSON-Datei.
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
        
        // Auf �nderungen reagieren
        rel.Childs.Changed += (s, e) => 
        {
            Console.WriteLine($"Produkte in {rel.Parent.Name} ge�ndert");
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
            // L�uft auf UI-Thread - kann direkt UI aktualisieren
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
?   ??? GlobalStoreNotRegisteredException.cs       # Exception f�r fehlende Registrierung
?   ??? GlobalStoreAlreadyRegisteredException.cs   # Exception f�r doppelte Registrierung
?
??? Runtime/
?   ??? InMemoryDataStore.cs                       # In-Memory-Implementierung
?   ??? DataStoresFacade.cs                        # Facade-Implementierung
?   ??? GlobalStoreRegistry.cs                     # Registry-Implementierung
?   ??? ILocalDataStoreFactory.cs                  # Factory f�r lokale Stores
?
??? Persistence/
?   ??? IPersistenceStrategy.cs                    # Persistierungs-Interface
?   ??? PersistentStoreDecorator.cs                # Decorator f�r Persistierung
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

- .NET 8.0 oder h�her
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1+

## ?? Migration & Updates

Pr�fen Sie die [CHANGELOG.md](CHANGELOG.md) f�r Informationen zu Breaking Changes und neuen Features.

## ?? Beitragen

Contributions sind willkommen! Siehe [CONTRIBUTING.md](../CONTRIBUTING.md) f�r Details.

## ?? Lizenz

[Lizenz hier einf�gen]

## ?? Maintainer

[Ihre Informationen hier]

---

**Version**: 1.0.0  
**Letzte Aktualisierung**: Januar 2025  
**Repository**: [GitHub-Link]
