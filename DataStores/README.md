# DataStores - Flexible In-Memory Datenspeicherverwaltung

Eine moderne .NET 8 Bibliothek für die Verwaltung von typsicheren In-Memory-Datensammlungen mit umfassender Unterstützung für Persistierung, Event-Handling und hierarchische Beziehungen.

## Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [Usage Contract (verbindlich)](#usage-contract-verbindlich)
- [Installation](#installation)
- [Schnellstart](#schnellstart)
- [Application Startup Flow](#application-startup-flow)
- [Kernkonzepte](#kernkonzepte)
- [Dokumentation](#dokumentation)
- [Beispiele](#beispiele)

## Übersicht

DataStores ist eine leistungsstarke Bibliothek, die eine flexible und typsichere Verwaltung von In-Memory-Datensammlungen ermöglicht. Sie vereinfacht die Arbeit mit Daten in modernen .NET-Anwendungen durch:

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

## Usage Contract (verbindlich)

### MUST: Zugriff ausschließlich über IDataStores

- **Application code MUST access stores ONLY via `IDataStores` facade**
- Use `IDataStores.GetGlobal<T>()` for global stores
- Use `IDataStores.CreateLocal<T>()` for local stores
- NEVER directly instantiate `InMemoryDataStore<T>`, `PersistentStoreDecorator<T>`, or other store types in application code

### MUST: Registration nur via IDataStoreRegistrar

- **Registration MUST occur ONLY within `IDataStoreRegistrar.Register()` implementations**
- Register stores during startup before first access
- Do NOT resolve or access stores within the Register method
- Use `ServiceCollectionExtensions.AddDataStoreRegistrar<T>()` to register registrars

### MUST: Bootstrap vor erster Nutzung

- **`DataStoreBootstrap.RunAsync()` MUST be executed once during application startup**
- Call after building the service provider
- Do NOT call from feature code, viewmodels, or services

### MUST NOT: Direkte Nutzung von Infrastruktur-Komponenten

- **Application code MUST NOT depend on infrastructure types**:
  - `IGlobalStoreRegistry` / `GlobalStoreRegistry`
  - `ILocalDataStoreFactory` / `LocalDataStoreFactory`
  - Direct instantiation of decorators or store implementations
- These types are for internal framework use only
- Bypassing the facade prevents proper lifecycle and initialization

### Anti-Patterns (NEVER do this)

❌ **WRONG: Direct store instantiation in application code**
```csharp
// NEVER do this in feature code
var store = new InMemoryDataStore<Product>();
var store = new PersistentStoreDecorator<Product>(...);
```

❌ **WRONG: Direct registry access in application code**
```csharp
// NEVER do this in feature code
var registry = serviceProvider.GetRequiredService<IGlobalStoreRegistry>();
var store = registry.ResolveGlobal<Product>();
```

❌ **WRONG: Store access in registrar**
```csharp
public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
{
    registry.RegisterGlobal(new InMemoryDataStore<Product>());
    // NEVER access stores here
    var store = registry.ResolveGlobal<Product>(); // WRONG!
}
```

✅ **CORRECT: Use IDataStores facade**
```csharp
public class ProductService
{
    private readonly IDataStores _stores;
    
    public ProductService(IDataStores stores)
    {
        _stores = stores;
    }
    
    public void AddProduct(Product product)
    {
        var store = _stores.GetGlobal<Product>();
        store.Add(product);
    }
}
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

## Schnellstart

### Option 1: Mit Common.Bootstrap (EMPFOHLEN - Automatisches Scanning)

Der empfohlene Weg nutzt `DataStoresBootstrapDecorator` für automatische Service-Registrierung.

```csharp
using Common.Bootstrap;
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 1. PathProvider registrieren
var pathProvider = new DataStorePathProvider("MyApp");
services.AddSingleton<IDataStorePathProvider>(pathProvider);

// 2. DefaultBootstrapWrapper instanziieren
var defaultWrapper = new DefaultBootstrapWrapper();

// 3. DataStoresBootstrapDecorator instanziieren
var bootstrap = new DataStoresBootstrapDecorator(defaultWrapper);

// 4. Services aus Assemblies registrieren (automatisches Scanning)
bootstrap.RegisterServices(
    services,
    typeof(DefaultBootstrapWrapper).Assembly,      // Common.Bootstrap
    typeof(DataStoresBootstrapDecorator).Assembly  // DataStores
);

// 5. ServiceProvider bauen und Bootstrap ausführen
var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);

// 6. Stores verwenden
var stores = provider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();
productStore.Add(new Product { Id = 1, Name = "Laptop" });
```

**Was wird automatisch registriert:**
- ✅ **IServiceModule** - Alle Service-Module (z.B. DataStoresServiceModule)
- ✅ **IEqualityComparer\<T\>** - Custom Equality-Comparer
- ✅ **IDataStoreRegistrar** - Store-Registrierungen (mit parameterlosem Konstruktor)

**Vorteile:**
- Automatische Erkennung aller Registrars
- Keine manuelle Registrierung erforderlich
- Convention over Configuration

---

### Option 2: Manuell (Ohne Common.Bootstrap)

Für Projekte ohne Common.Bootstrap oder mit parametrisierten Registrars.

#### 1. Services registrieren

```csharp
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Registriere DataStores ServiceModule
var module = new DataStoresServiceModule();
module.Register(services);

// Eigenen Registrar hinzufügen
services.AddDataStoreRegistrar(new MyAppStoreRegistrar("C:\\Data\\myapp.db"));

var serviceProvider = services.BuildServiceProvider();
```

#### 2. Registrar implementieren (EMPFOHLEN: Builder Pattern)

```csharp
using DataStores.Registration;

public class MyAppStoreRegistrar : DataStoreRegistrarBase
{
    public MyAppStoreRegistrar(string dbPath)
    {
        // InMemory store (no persistence)
        AddStore(new InMemoryDataStoreBuilder<Product>());
        
        // JSON store with auto-load and auto-save
        AddStore(new JsonDataStoreBuilder<Customer>(
            filePath: "C:\\Data\\customers.json"));
        
        // LiteDB store (collection name auto-generated from type name)
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath));
    }
}
```

**Alternative (mehr Boilerplate):**
```csharp
using DataStores.Abstractions;
using DataStores.Runtime;

public class ProductStoreRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // ONLY register stores here, do NOT access them
        registry.RegisterGlobal(new InMemoryDataStore<Product>());
    }
}
```

#### 3. Bootstrap ausführen

```csharp
// MUST be called once during startup, after building service provider
await DataStoreBootstrap.RunAsync(serviceProvider);
```

#### 4. Stores verwenden

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
        // Access stores via facade ONLY
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

## Application Startup Flow

The DataStores framework follows a strict 5-step initialization sequence:

### Step 1: DI Container Setup
```csharp
var services = new ServiceCollection();
```

### Step 2: Register ServiceModule
```csharp
// Registers IDataStores, IGlobalStoreRegistry, ILocalDataStoreFactory
var module = new DataStoresServiceModule();
module.Register(services);
```

### Step 3: Register Store Registrars
```csharp
// Register your IDataStoreRegistrar implementations
services.AddDataStoreRegistrar<ProductStoreRegistrar>();
services.AddDataStoreRegistrar<CustomerStoreRegistrar>();
```

### Step 4: Build Service Provider
```csharp
var serviceProvider = services.BuildServiceProvider();
```

### Step 5: Bootstrap Execution
```csharp
// MUST be called before first store access
await DataStoreBootstrap.RunAsync(serviceProvider);
```

### Step 6: Use via Facade
```csharp
// Now access stores via IDataStores ONLY
var stores = serviceProvider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();
```

**Important:** Do NOT skip step 5 (Bootstrap). Accessing stores before bootstrap will throw exceptions.

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

**Globale Stores** sind application-wide Singletons:
```csharp
var globalProducts = stores.GetGlobal<Product>();
```

**Lokale Stores** sind isolierte Instanzen:
```csharp
var localStore = stores.CreateLocal<Product>();
var filteredStore = stores.CreateLocalSnapshotFromGlobal<Product>(
    p => p.Category == "Electronics");
```

### Event-System

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

## Dokumentation

### Hauptdokumentation

- **[DataStores.md](../DataStores.md)** - **Vollständiger Bootstrap-Prozess** mit Common.Bootstrap Integration

### Detaillierte Guides

- **[API Referenz](Docs/API-Reference.md)** - Vollständige API-Dokumentation
- **[Formale Spezifikationen](Docs/Formal-Specifications.md)** - Invarianten und Verhaltensgarantien
- **[Verwendungsbeispiele](Docs/Usage-Examples.md)** - Praktische Beispiele
- **[Persistierung Guide](Docs/Persistence-Guide.md)** - Daten persistent speichern
- **[Beziehungen Guide](Docs/Relations-Guide.md)** - Eltern-Kind-Beziehungen
- **[LiteDB Integration](Docs/LiteDB-Integration.md)** - LiteDB-Persistierung
- **[Registrar Best Practices](Docs/Registrar-Best-Practices.md)** - Registrar-Patterns
- **[Assembly Scanning Guide](Docs/Assembly-Scanning-Guide.md)** - Automatische Registrar-Erkennung
- **[DataStoresBootstrapDecorator Guide](Docs/DataStoresBootstrapDecorator-Guide.md)** - Bootstrap-Decorator Details

## Beispiele

### Beispiel 1: Einfacher Product Store mit Builder Pattern

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

// Verwendung
var stores = serviceProvider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();

productStore.Add(new Product 
{ 
    Id = 1, 
    Name = "Laptop", 
    Price = 999.99m,
    IsActive = true 
});

productStore.AddRange(new[]
{
    new Product { Id = 2, Name = "Maus", Price = 29.99m },
    new Product { Id = 3, Name = "Tastatur", Price = 79.99m }
});
```

### Beispiel 2: Persistenter Store mit JSON (Builder Pattern)

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

**Alternative (alte Methode - mehr Boilerplate):**
```csharp
public class JsonProductRegistrar : IDataStoreRegistrar
{
    private readonly string _jsonFilePath;
    
    public JsonProductRegistrar(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
    }
    
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var strategy = new JsonFilePersistenceStrategy<Product>(_jsonFilePath);
        var innerStore = new InMemoryDataStore<Product>();
        var persistentStore = new PersistentStoreDecorator<Product>(
            innerStore,
            strategy,
            autoLoad: true,
            autoSaveOnChange: true);
            
        registry.RegisterGlobal(persistentStore);
    }
}
```

### Beispiel 3: LiteDB-Persistierung mit EntityBase (Builder Pattern)

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
        // Collection name is automatically "Order" (from typeof(Order).Name)
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath,
            autoLoad: true,
            autoSave: true));
    }
}

// Startup
services.AddDataStoreRegistrar(
    new LiteDbOrderRegistrar("C:\\Data\\myapp.db"));
```

**Alternative (alte Methode - mehr Boilerplate):**
```csharp
public class LiteDbOrderRegistrar : IDataStoreRegistrar
{
    private readonly string _dbPath;
    
    public LiteDbOrderRegistrar(string dbPath)
    {
        _dbPath = dbPath;
    }
    
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var strategy = new LiteDbPersistenceStrategy<Order>(_dbPath, "orders");
        var innerStore = new InMemoryDataStore<Order>();
        var persistentStore = new PersistentStoreDecorator<Order>(
            innerStore,
            strategy,
            autoLoad: true,
            autoSaveOnChange: true);
            
        registry.RegisterGlobal(persistentStore);
    }
}
```

### Beispiel 4: Multi-Store Registrar mit verschiedenen Strategien

```csharp
public class MultiStoreRegistrar : DataStoreRegistrarBase
{
    public MultiStoreRegistrar(string dbPath)
    {
        // InMemory: Temporary data, no persistence
        AddStore(new InMemoryDataStoreBuilder<Product>());
        
        // JSON: Configuration and settings
        AddStore(new JsonDataStoreBuilder<Settings>(
            filePath: "C:\\Data\\settings.json",
            autoLoad: true,
            autoSave: false)); // Read-only
        
        // LiteDB: Business entities with persistence
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath));
        
        AddStore(new LiteDbDataStoreBuilder<Customer>(
            databasePath: dbPath));
        
        // With custom comparer
        AddStore(new InMemoryDataStoreBuilder<Category>(
            comparer: new CategoryIdComparer()));
        
        // With UI-thread event marshalling (WPF)
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
├── Abstractions/
│   ├── IDataStore.cs
│   ├── IDataStores.cs
│   ├── IGlobalStoreRegistry.cs
│   ├── IDataStoreRegistrar.cs
│   └── DataStoreChangedEventArgs.cs
│
├── Runtime/
│   ├── InMemoryDataStore.cs
│   ├── DataStoresFacade.cs
│   ├── GlobalStoreRegistry.cs
│   └── LocalDataStoreFactory.cs
│
├── Persistence/
│   ├── IPersistenceStrategy.cs
│   ├── PersistentStoreDecorator.cs
│   ├── JsonFilePersistenceStrategy.cs
│   └── LiteDbPersistenceStrategy.cs
│
├── Relations/
│   ├── ParentChildRelationship.cs
│   └── IParentChildRelationService.cs
│
├── Bootstrap/
│   ├── DataStoreBootstrap.cs
│   ├── ServiceCollectionExtensions.cs
│   └── DataStoresServiceModule.cs
│
└── Docs/
    ├── API-Reference.md
    ├── Formal-Specifications.md
    ├── Usage-Examples.md
    ├── Persistence-Guide.md
    ├── Relations-Guide.md
    ├── LiteDB-Integration.md
    └── Registrar-Best-Practices.md
```

## Anforderungen

- .NET 8.0 oder höher
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1+
- **Common.Bootstrap** (empfohlen für automatisches Scanning)
- LiteDB 5.0.21+ (optional, für LiteDB-Persistierung)
- System.Text.Json (enthalten in .NET 8)

## Schnellreferenz: Bootstrap-Optionen

### Mit Common.Bootstrap (Automatisch)
```csharp
// Automatisches Scanning aller IServiceModule, IEqualityComparer<T>, IDataStoreRegistrar
var bootstrap = new DataStoresBootstrapDecorator(new DefaultBootstrapWrapper());
bootstrap.RegisterServices(services, 
    typeof(DefaultBootstrapWrapper).Assembly,
    typeof(DataStoresBootstrapDecorator).Assembly);
```

### Ohne Common.Bootstrap (Manuell)
```csharp
// Manuelle Registrierung
new DataStoresServiceModule().Register(services);
services.AddDataStoreRegistrar(new MyRegistrar());
```

**Siehe [DataStores.md](../DataStores.md) für vollständige Details zum Bootstrap-Prozess.**

## Beitragen

Contributions sind willkommen! Siehe [CONTRIBUTING.md](../CONTRIBUTING.md) für Details.

## Lizenz

MIT License - siehe [LICENSE](../LICENSE) für Details.

---

**Version**: 1.0.0  
**Letzte Aktualisierung**: Januar 2025
