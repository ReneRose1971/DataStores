# DataStores - Flexible In-Memory Datenspeicherverwaltung

Eine moderne .NET 8 Bibliothek für die Verwaltung von typsicheren In-Memory-Datensammlungen mit umfassender Unterstützung für Persistierung, Event-Handling und hierarchische Beziehungen.

## Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [Schnellstart](#schnellstart)
- [Bootstrap-Konzept](#bootstrap-konzept)
- [Kernkonzepte](#kernkonzepte)
- [Lokale InMemoryDataStore-Kopien](#lokale-inmemorydatastore-kopien)
- [Installation](#installation)
- [Dokumentation](#dokumentation)
- [Beispiele](#beispiele)

## Übersicht

DataStores ist eine leistungsstarke Bibliothek, die eine flexible und typsichere Verwaltung von In-Memory-Datensammlungen ermöglicht. Sie vereinfacht die Arbeit mit Daten in modernen .NET-Anwendungen.

### Hauptmerkmale

- **Typsichere Datenspeicher**: Generische `IDataStore<T>` Schnittstelle für jede Klasse
- **Globale & Lokale Stores**: Zentrale Singleton-Stores und isolierte lokale Instanzen
- **Thread-Sicherheit**: Alle Operationen sind thread-sicher implementiert
- **Persistierung**: Optionale asynchrone Speicherung mit Auto-Load/Auto-Save
- **Event-System**: Änderungsbenachrichtigungen mit detaillierten EventArgs
- **Relationen**: Eltern-Kind-Beziehungen zwischen verschiedenen Entitätstypen
- **DI-Integration**: Nahtlose Integration mit Microsoft.Extensions.DependencyInjection
- **UI-Thread-Support**: SynchronizationContext für WPF/WinForms/MAUI
- **Bulk-Operationen**: AddRange für performante Massen-Operationen
- **Flexible Filter**: Snapshots mit Prädikaten und Custom Comparers

## Schnellstart

### 1. Services registrieren

```csharp
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// DataStores ServiceModule registrieren
var module = new DataStoresServiceModule();
module.Register(services);

// Eigenen Registrar hinzufügen
services.AddDataStoreRegistrar(new MyAppStoreRegistrar("C:\\Data\\myapp.db"));

var serviceProvider = services.BuildServiceProvider();
```

### 2. Registrar implementieren

```csharp
using DataStores.Registration;

public class MyAppStoreRegistrar : DataStoreRegistrarBase
{
    public MyAppStoreRegistrar(string dbPath)
    {
        // InMemory store (keine Persistierung)
        AddStore(new InMemoryDataStoreBuilder<Product>());
        
        // JSON store mit Auto-Load und Auto-Save
        AddStore(new JsonDataStoreBuilder<Customer>(
            filePath: "C:\\Data\\customers.json"));
        
        // LiteDB store (Collection-Name wird automatisch aus dem Typnamen generiert)
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath));
    }
}
```

### 3. Bootstrap ausführen

```csharp
// Einmal beim Anwendungsstart ausführen, nach dem Bauen des Service Providers
await DataStoreBootstrap.RunAsync(serviceProvider);
```

### 4. Stores verwenden über IDataStores

```csharp
using DataStores.Abstractions;

public class ProductService
{
    private readonly IDataStores _stores;
    
    public ProductService(IDataStores stores)
    {
        _stores = stores;
    }
    
    public IReadOnlyList<Product> GetAllProducts()
    {
        // Zugriff auf Stores erfolgt ausschließlich über IDataStores
        var store = _stores.GetGlobal<Product>();
        return store.Items;
    }
    
    public void AddProduct(Product product)
    {
        var store = _stores.GetGlobal<Product>();
        store.Add(product);
    }
}
```

## Bootstrap-Konzept

### Trennung von Konfiguration und Laufzeit

Die DataStores-Bibliothek folgt einem strikten Separationsprinzip zwischen Bootstrap-Phase und Laufzeit-Phase.

#### Bootstrap-Phase (Anwendungsstart)

In der Bootstrap-Phase wird festgelegt, welche Art von Datenquelle für einen bestimmten Typ `T` existiert. Dies geschieht durch Implementierung eines `IDataStoreRegistrar`:

```csharp
public class MyAppStoreRegistrar : DataStoreRegistrarBase
{
    public MyAppStoreRegistrar()
    {
        // Hier wird festgelegt: Product wird InMemory gespeichert
        AddStore(new InMemoryDataStoreBuilder<Product>());
        
        // Hier wird festgelegt: Customer wird in JSON persistiert
        AddStore(new JsonDataStoreBuilder<Customer>(
            filePath: "C:\\Data\\customers.json"));
    }
}
```

**Nach dem einmaligen Bootstrap-Aufruf (`DataStoreBootstrap.RunAsync()`) ist die Konfiguration abgeschlossen.**

#### Laufzeit-Phase (Application Code)

Zur Laufzeit arbeitet der Anwendungscode ausschließlich mit den `IDataStore<T>`-Interfaces über die `IDataStores`-Facade:

```csharp
public class OrderViewModel
{
    private readonly IDataStores _stores;
    
    public OrderViewModel(IDataStores stores)
    {
        _stores = stores;
    }
    
    public void LoadOrders()
    {
        // Der Anwendungscode arbeitet nur mit dem IDataStore<Order> Interface
        // Die konkrete Implementierung (JSON, LiteDB, InMemory) ist transparent
        var orderStore = _stores.GetGlobal<Order>();
        var orders = orderStore.Items;
        
        // Die Persistierung passiert automatisch im Hintergrund (falls konfiguriert)
    }
}
```

**Vorteile dieser Trennung:**
- **Testbarkeit**: In Tests kann ein InMemory-Store verwendet werden, in Produktion LiteDB
- **Flexibilität**: Die Persistierungsstrategie kann ohne Änderung des Anwendungscodes gewechselt werden
- **Single Responsibility**: Bootstrap-Code konfiguriert, Anwendungscode nutzt nur Interfaces
- **Dependency Injection**: Stores werden über DI bereitgestellt

### Zugriff ausschließlich über IDataStores

Der Zugriff auf alle Stores erfolgt zwingend über die `IDataStores`-Facade. Dies gewährleistet:
- Konsistenten Zugriff auf globale und lokale Stores
- Korrekte Initialisierung und Lifecycle-Management
- Einheitliche API für alle Anwendungsteile

```csharp
public class ProductService
{
    private readonly IDataStores _stores;
    
    public ProductService(IDataStores stores)
    {
        _stores = stores; // IDataStores wird per DI injiziert
    }
    
    public void DoSomething()
    {
        // Globale Stores
        var productStore = _stores.GetGlobal<Product>();
        
        // Lokale Stores
        var localStore = _stores.CreateLocal<Product>();
        
        // Snapshots
        var snapshot = _stores.CreateLocalSnapshotFromGlobal<Product>(
            p => p.IsActive);
    }
}
```

## Kernkonzepte

### IDataStore<T>

Die zentrale Schnittstelle für alle Datenspeicher:

```csharp
public interface IDataStore<T> where T : class
{
    IReadOnlyList<T> Items { get; }
    event EventHandler<DataStoreChangedEventArgs<T>> Changed;
    void Add(T item);
    void AddRange(IEnumerable<T> items);
    bool Remove(T item);
    void Clear();
    bool Contains(T item);
}
```

### Globale vs. Lokale Stores

**Globale Stores** sind application-wide Singletons, die über die gesamte Anwendung geteilt werden:
```csharp
var globalProducts = stores.GetGlobal<Product>();
```

**Lokale Stores** sind isolierte Instanzen für spezifische Anwendungsfälle:
```csharp
var localStore = stores.CreateLocal<Product>();
```

### Event-System

Alle Stores bieten Änderungsbenachrichtigungen über das `Changed`-Event:

```csharp
var store = stores.GetGlobal<Product>();

store.Changed += (sender, e) =>
{
    switch (e.ChangeType)
    {
        case DataStoreChangeType.Add:
            Console.WriteLine($"Produkt hinzugefügt: {e.AffectedItems[0].Name}");
            break;
        case DataStoreChangeType.Remove:
            Console.WriteLine("Produkt entfernt");
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

## Lokale InMemoryDataStore-Kopien

### Snapshots von globalen Stores

Sie können lokale, isolierte Kopien von globalen Stores mit allen vorhandenen Daten erstellen. Dies ist besonders nützlich für:
- **Dialog-Szenarien**: Bearbeitung mit Abbrechen/Speichern
- **Temporäre Filter**: Anzeige einer gefilterten Teilmenge
- **Isolierte Tests**: Unabhängige Test-Daten

### Vollständige Kopie erstellen

```csharp
// Erstelle eine lokale Kopie mit ALLEN Daten aus dem globalen Store
var localStore = stores.CreateLocalSnapshotFromGlobal<Product>();

// localStore enthält jetzt alle Produkte aus dem globalen Store
// Änderungen am localStore beeinflussen nicht den globalen Store
localStore.Add(new Product { Name = "Neues Produkt" });

// Der globale Store bleibt unverändert
var globalStore = stores.GetGlobal<Product>();
```

### Gefilterte Kopie erstellen

```csharp
// Erstelle eine lokale Kopie mit nur den aktiven Produkten
var activeProductsStore = stores.CreateLocalSnapshotFromGlobal<Product>(
    p => p.IsActive);

// activeProductsStore enthält nur Produkte mit IsActive == true
// Änderungen sind isoliert
```

### Verwendungsbeispiel: Edit-Dialog mit Abbrechen

```csharp
public class EditProductDialogViewModel
{
    private readonly IDataStores _stores;
    private IDataStore<Product> _editStore;
    
    public EditProductDialogViewModel(IDataStores stores)
    {
        _stores = stores;
    }
    
    public void Initialize()
    {
        // Lokale Kopie für die Bearbeitung
        _editStore = _stores.CreateLocalSnapshotFromGlobal<Product>();
    }
    
    public void Save()
    {
        // Änderungen in den globalen Store übernehmen
        var globalStore = _stores.GetGlobal<Product>();
        globalStore.Clear();
        globalStore.AddRange(_editStore.Items);
    }
    
    public void Cancel()
    {
        // Einfach den lokalen Store verwerfen
        _editStore = null;
        // Globaler Store bleibt unverändert
    }
}
```

### Wichtig: Keine automatische Synchronisation

Lokale Snapshots sind eigenständige Kopien:
- Änderungen am lokalen Store beeinflussen nicht den globalen Store
- Änderungen am globalen Store werden nicht automatisch im lokalen Store reflektiert
- Für bidirektionale Synchronisation müssen Sie manuell die Daten kopieren

```csharp
// Lokalen Store erstellen
var localStore = stores.CreateLocalSnapshotFromGlobal<Product>();

// Änderung am globalen Store
var globalStore = stores.GetGlobal<Product>();
globalStore.Add(new Product { Name = "Global Produkt" });

// localStore bleibt unverändert (keine automatische Synchronisation)
// Um den lokalen Store zu aktualisieren, einen neuen Snapshot erstellen:
localStore = stores.CreateLocalSnapshotFromGlobal<Product>();
```

## Installation

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

## Dokumentation

### Detaillierte Guides

- **[API Referenz](Docs/API-Reference.md)** - Vollständige API-Dokumentation
- **[Formale Spezifikationen](Docs/Formal-Specifications.md)** - Invarianten und Verhaltensgarantien
- **[Verwendungsbeispiele](Docs/Usage-Examples.md)** - Praktische Beispiele
- **[Persistierung Guide](Docs/Persistence-Guide.md)** - Daten persistent speichern
- **[Beziehungen Guide](Docs/Relations-Guide.md)** - Eltern-Kind-Beziehungen
- **[LiteDB Integration](Docs/LiteDB-Integration.md)** - LiteDB-Persistierung
- **[Registrar Best Practices](Docs/Registrar-Best-Practices.md)** - Registrar-Patterns

## Beispiele

### Beispiel 1: Einfacher Product Store

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

// Registrar mit Builder Pattern
public class ProductStoreRegistrar : DataStoreRegistrarBase
{
    public ProductStoreRegistrar()
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
    }
}

// Verwendung über IDataStores
var stores = serviceProvider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();

productStore.Add(new Product 
{ 
    Id = 1, 
    Name = "Laptop", 
    Price = 999.99m,
    IsActive = true 
});
```

### Beispiel 2: Persistenter Store mit JSON

```csharp
public class JsonProductRegistrar : DataStoreRegistrarBase
{
    public JsonProductRegistrar(string jsonFilePath)
    {
        AddStore(new JsonDataStoreBuilder<Product>(
            filePath: jsonFilePath,
            autoLoad: true,
            autoSave: true));
    }
}

// Startup
services.AddDataStoreRegistrar(
    new JsonProductRegistrar("C:\\Data\\products.json"));
```

### Beispiel 3: LiteDB-Persistierung

```csharp
public class Order : EntityBase
{
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

public class LiteDbOrderRegistrar : DataStoreRegistrarBase
{
    public LiteDbOrderRegistrar(string dbPath)
    {
        // Collection-Name wird automatisch "Order" (aus typeof(Order).Name)
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath,
            autoLoad: true,
            autoSave: true));
    }
}
```

### Beispiel 4: Multi-Store Registrar

```csharp
public class MultiStoreRegistrar : DataStoreRegistrarBase
{
    public MultiStoreRegistrar(string dbPath)
    {
        // InMemory: Temporäre Daten, keine Persistierung
        AddStore(new InMemoryDataStoreBuilder<Product>());
        
        // JSON: Konfiguration und Einstellungen
        AddStore(new JsonDataStoreBuilder<Settings>(
            filePath: "C:\\Data\\settings.json",
            autoLoad: true,
            autoSave: false));
        
        // LiteDB: Business-Entities mit Persistierung
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath));
        
        AddStore(new LiteDbDataStoreBuilder<Customer>(
            databasePath: dbPath));
        
        // Mit Custom Comparer
        AddStore(new InMemoryDataStoreBuilder<Category>(
            comparer: new CategoryIdComparer()));
        
        // Mit UI-Thread Event-Marshalling (WPF)
        AddStore(new JsonDataStoreBuilder<UserPreferences>(
            filePath: "C:\\Data\\preferences.json",
            synchronizationContext: SynchronizationContext.Current));
    }
}
```

### Beispiel 5: Eltern-Kind-Beziehung

```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class CategoryProductService
{
    private readonly IParentChildRelationService _relationService;
    
    public CategoryProductService(IParentChildRelationService relationService)
    {
        _relationService = relationService;
    }
    
    public IDataStore<Product> GetProductsForCategory(Category category)
    {
        var relationship = _relationService.CreateRelationship<Category, Product>(
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
            
        relationship.UseGlobalDataSource();
        relationship.Refresh();
        
        return relationship.Childs;
    }
}
```

## Projekt-Struktur

```
DataStores/
├── Abstractions/          # Interfaces und Basisklassen
├── Runtime/               # Laufzeit-Implementierungen
├── Persistence/           # Persistierungs-Funktionalität
├── Relations/             # Eltern-Kind-Beziehungen
├── Bootstrap/             # Initialisierung und DI
├── Registration/          # Builder-Pattern für Store-Registrierung
└── Docs/                  # Ausführliche Dokumentation
```

## Anforderungen

- .NET 8.0 oder höher
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1+
- LiteDB 5.0.21+ (optional, für LiteDB-Persistierung)
- System.Text.Json (enthalten in .NET 8)

## Beitragen

Contributions sind willkommen! Siehe [CONTRIBUTING.md](../CONTRIBUTING.md) für Details.

## Lizenz

MIT License - siehe [LICENSE](../LICENSE) für Details.

---

**Version**: 1.0.0  
**Letzte Aktualisierung**: Januar 2025
