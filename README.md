# DataStores Solution

Eine umfassende, thread-sichere Datenspeicher-Bibliothek für .NET 8 mit Unterstützung für globale und lokale Stores, Persistenz und Parent-Child-Beziehungen.

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-212%20passed-success)](DataStores.Tests/COMPLETE_REPORT.md)
[![Coverage](https://img.shields.io/badge/Coverage-~98%25-brightgreen)](DataStores.Tests/COMPLETE_REPORT.md)

---

## ?? Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [Architektur](#architektur)
- [Projekte](#projekte)
- [Schnellstart](#schnellstart)
- [Features](#features)
- [Dokumentation](#dokumentation)
- [Tests](#tests)
- [Lizenz](#lizenz)

---

## ?? Übersicht

DataStores ist eine moderne, flexible Datenspeicher-Bibliothek, die entwickelt wurde, um die Verwaltung von In-Memory-Daten in .NET-Anwendungen zu vereinfachen. Sie bietet:

- ? **Thread-sichere** In-Memory-Stores
- ? **Globale & lokale** Store-Verwaltung
- ? **Persistenz-Unterstützung** via Decorator-Pattern
- ? **Parent-Child-Beziehungen** mit flexiblen Filtern
- ? **Dependency Injection** Integration
- ? **Event-System** für Change-Notifications
- ? **Snapshot-Isolation** für unabhängige Datensichten

---

## ??? Architektur

Die Solution folgt einer klaren, schichtenbasierten Architektur:

```
???????????????????????????????????????????????????????????
?                    Consumer Application                  ?
???????????????????????????????????????????????????????????
                            ?
???????????????????????????????????????????????????????????
?               DataStores.Bootstrap (DI)                  ?
???????????????????????????????????????????????????????????
                            ?
?????????????????????????????????????????????????????????
?  Relations   ?   Persistence    ?      Runtime        ?
?  (optional)  ?   (decorator)    ?   (core stores)     ?
?????????????????????????????????????????????????????????
                            ?
???????????????????????????????????????????????????????????
?            DataStores.Abstractions (contracts)           ?
???????????????????????????????????????????????????????????
```

### Design-Prinzipien

1. **Keine Vererbungshierarchien** - Decorator statt Ableitung
2. **Explicit > Implicit** - Keine versteckten Seiteneffekte
3. **Testability First** - 98% Test-Coverage
4. **Thread-Safe by Default** - Sichere Concurrent-Nutzung
5. **Separation of Concerns** - Klare Verantwortlichkeiten

---

## ?? Projekte

### Core Projects

| Projekt | Beschreibung | Dokumentation |
|---------|-------------|---------------|
| **[DataStores.Abstractions](DataStores.Abstractions/README.md)** | Basis-Interfaces und Contracts | [API-Referenz](DataStores.Abstractions/Docs/API.md) |
| **[DataStores.Runtime](DataStores.Runtime/README.md)** | In-Memory-Implementierung & Registry | [API-Referenz](DataStores.Runtime/Docs/API.md) |
| **[DataStores.Persistence](DataStores.Persistence/README.md)** | Persistenz-Decorator & Strategien | [API-Referenz](DataStores.Persistence/Docs/API.md) |
| **[DataStores.Relations](DataStores.Relations/README.md)** | Parent-Child-Beziehungen | [API-Referenz](DataStores.Relations/Docs/API.md) |
| **[DataStores.Bootstrap](DataStores.Bootstrap/README.md)** | DI-Integration & Startup | [API-Referenz](DataStores.Bootstrap/Docs/API.md) |

### Test Project

| Projekt | Beschreibung | Dokumentation |
|---------|-------------|---------------|
| **[DataStores.Tests](DataStores.Tests/README.md)** | 212 Tests, ~98% Coverage | [Test-Report](DataStores.Tests/COMPLETE_REPORT.md) |

---

## ?? Schnellstart

### Installation

```bash
# Noch nicht auf NuGet verfügbar - Build aus Source
dotnet build DataStores.sln
```

### Basis-Verwendung

```csharp
using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

// 1. Setup DI
var services = new ServiceCollection();
services.AddDataStoresCore();
services.AddDataStoreRegistrar<MyDataRegistrar>();

var provider = services.BuildServiceProvider();

// 2. Bootstrap
DataStoreBootstrap.Run(provider);

// 3. Verwenden
var stores = provider.GetRequiredService<IDataStores>();
var customerStore = stores.GetGlobal<Customer>();

customerStore.Add(new Customer { Id = 1, Name = "John Doe" });
```

### Mit Persistenz

```csharp
// Registrar mit Persistenz
public class MyDataRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var strategy = new JsonFilePersistenceStrategy<Customer>("customers.json");
        var innerStore = new InMemoryDataStore<Customer>();
        var persistentStore = new PersistentStoreDecorator<Customer>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: true);
        
        registry.RegisterGlobal(persistentStore);
    }
}
```

### Mit Relations

```csharp
// Parent-Child-Beziehung
var customer = customerStore.Items.First();
var orderRelation = new ParentChildRelationship<Customer, Order>(
    stores,
    customer,
    (parent, child) => child.CustomerId == parent.Id);

orderRelation.UseGlobalDataSource();
orderRelation.Refresh();

// Jetzt enthält orderRelation.Childs alle Orders des Customers
```

---

## ? Features

### 1. Globale & Lokale Stores

**Globale Stores** werden zentral registriert und sind application-wide verfügbar:

```csharp
var globalStore = stores.GetGlobal<Customer>();
```

**Lokale Stores** sind unabhängige, isolierte Instanzen:

```csharp
var localStore = stores.CreateLocal<Customer>();
```

**Snapshots** sind Kopien zu einem bestimmten Zeitpunkt:

```csharp
var snapshot = stores.CreateLocalSnapshotFromGlobal<Customer>(c => c.IsActive);
```

### 2. Thread-Safety

Alle Stores sind thread-safe und können concurrent verwendet werden:

```csharp
Parallel.For(0, 1000, i =>
{
    store.Add(new Customer { Id = i, Name = $"Customer{i}" });
});
```

### 3. Event-System

Stores feuern Events bei Änderungen:

```csharp
store.Changed += (sender, args) =>
{
    Console.WriteLine($"Change: {args.ChangeType}, Items: {args.AffectedItems.Count}");
};
```

### 4. SynchronizationContext-Support

Events können auf einen bestimmten Thread marshalled werden (z.B. UI-Thread):

```csharp
var store = new InMemoryDataStore<Customer>(
    synchronizationContext: SynchronizationContext.Current);
```

### 5. Custom Comparers

Eigene Gleichheitslogik für Stores:

```csharp
var comparer = new IdOnlyComparer();
var store = stores.CreateLocal<Customer>(comparer);
```

### 6. Persistenz-Strategien

Flexibles Persistenz-System via Strategy-Pattern:

```csharp
public class JsonFilePersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken ct)
    {
        // Load from JSON file
    }
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken ct)
    {
        // Save to JSON file
    }
}
```

### 7. Parent-Child-Relations

Verwalten von 1:n-Beziehungen mit automatischer Filterung:

```csharp
var relation = new ParentChildRelationship<Customer, Order>(
    stores,
    customer,
    (parent, child) => child.CustomerId == parent.Id);

relation.UseGlobalDataSource();
relation.Refresh(); // Lädt und filtert Childs
```

---

## ?? Dokumentation

### Projekt-Dokumentation

- **[Abstractions](DataStores.Abstractions/README.md)** - Interfaces & Exceptions
- **[Runtime](DataStores.Runtime/README.md)** - InMemoryDataStore & Registry
- **[Persistence](DataStores.Persistence/README.md)** - Persistenz-System
- **[Relations](DataStores.Relations/README.md)** - Beziehungs-Management
- **[Bootstrap](DataStores.Bootstrap/README.md)** - DI & Initialization

### API-Referenzen

Jedes Projekt hat eine vollständige API-Dokumentation im `Docs/`-Ordner:

- [Abstractions API](DataStores.Abstractions/Docs/API.md)
- [Runtime API](DataStores.Runtime/Docs/API.md)
- [Persistence API](DataStores.Persistence/Docs/API.md)
- [Relations API](DataStores.Relations/Docs/API.md)
- [Bootstrap API](DataStores.Bootstrap/Docs/API.md)

### Guides & Tutorials

- [Getting Started Guide](Docs/GettingStarted.md)
- [Advanced Usage](Docs/AdvancedUsage.md)
- [Best Practices](Docs/BestPractices.md)
- [Migration Guide](Docs/MigrationGuide.md)

---

## ?? Tests

Die Solution hat eine umfassende Test-Suite:

- **212 Tests** (100% bestanden)
- **~98% Code Coverage**
- **Performance-Tests** für Large Datasets
- **Stress-Tests** für Concurrent-Szenarien
- **Integration-Tests** für End-to-End-Workflows

Siehe [Test-Report](DataStores.Tests/COMPLETE_REPORT.md) für Details.

### Tests ausführen

```bash
dotnet test DataStores.Tests/DataStores.Tests.csproj
```

### Coverage-Report generieren

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## ?? Beispiele

### Beispiel 1: Einfacher Store

```csharp
// Setup
var services = new ServiceCollection();
services.AddDataStoresCore();

var provider = services.BuildServiceProvider();
var stores = provider.GetRequiredService<IDataStores>();

// Lokalen Store erstellen
var todoStore = stores.CreateLocal<TodoItem>();

// Items hinzufügen
todoStore.Add(new TodoItem { Id = 1, Title = "Learn DataStores" });
todoStore.Add(new TodoItem { Id = 2, Title = "Build App" });

// Abfragen
var allTodos = todoStore.Items;
var contains = todoStore.Contains(new TodoItem { Id = 1 });
```

### Beispiel 2: Mit Persistenz

```csharp
// Registrar definieren
public class TodoRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider sp)
    {
        var strategy = new JsonFilePersistenceStrategy<TodoItem>("todos.json");
        var innerStore = new InMemoryDataStore<TodoItem>();
        var decorator = new PersistentStoreDecorator<TodoItem>(
            innerStore, strategy, autoLoad: true, autoSaveOnChange: true);
        
        registry.RegisterGlobal(decorator);
    }
}

// Bootstrap
services.AddDataStoreRegistrar<TodoRegistrar>();
await DataStoreBootstrap.RunAsync(provider);

// Verwenden - Daten werden automatisch geladen & gespeichert
var todoStore = stores.GetGlobal<TodoItem>();
todoStore.Add(new TodoItem { Id = 3, Title = "Persist Data" }); // Auto-Save!
```

### Beispiel 3: Parent-Child mit WPF

```csharp
// ViewModel
public class CustomerViewModel
{
    private readonly IDataStores _stores;
    private ParentChildRelationship<Customer, Order> _orders;
    
    public ObservableCollection<Order> Orders { get; }
    
    public CustomerViewModel(IDataStores stores, Customer customer)
    {
        _stores = stores;
        Orders = new ObservableCollection<Order>();
        
        // Setup Relation mit UI-Thread-Context
        _orders = new ParentChildRelationship<Customer, Order>(
            _stores,
            customer,
            (p, c) => c.CustomerId == p.Id);
        
        _orders.UseGlobalDataSource();
        _orders.Childs.Changed += OnOrdersChanged;
        _orders.Refresh();
    }
    
    private void OnOrdersChanged(object? sender, DataStoreChangedEventArgs<Order> e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Update UI-bound collection
            Orders.Clear();
            foreach (var order in _orders.Childs.Items)
            {
                Orders.Add(order);
            }
        });
    }
}
```

---

## ??? Build & Development

### Voraussetzungen

- .NET 8 SDK
- Visual Studio 2022 oder VS Code

### Build

```bash
dotnet build DataStores.sln
```

### Tests ausführen

```bash
dotnet test
```

### NuGet-Pakete erstellen

```bash
dotnet pack -c Release
```

---

## ?? Contributing

Contributions sind willkommen! Bitte beachten Sie:

1. **Tests schreiben** - Neue Features benötigen Tests
2. **Dokumentation** - XML-Kommentare auf Deutsch
3. **Code-Style** - Befolgen Sie die bestehenden Konventionen
4. **Pull Requests** - Beschreiben Sie Ihre Änderungen

---

## ?? Versionshistorie

### Version 1.0.0 (2025-12-19)

- ? Initial Release
- ? InMemoryDataStore mit Thread-Safety
- ? Global & Local Store Management
- ? Persistenz-System
- ? Parent-Child-Relations
- ? DI-Integration
- ? 212 Tests, ~98% Coverage

---

## ?? Lizenz

[MIT License](LICENSE) - Siehe LICENSE-Datei für Details.

---

## ?? Danksagungen

Diese Bibliothek wurde entwickelt mit Best Practices aus:
- DataToolKit.Tests (Test-Patterns)
- Clean Architecture-Prinzipien
- Domain-Driven Design

---

## ?? Support & Kontakt

- **Issues**: [GitHub Issues](https://github.com/your-repo/DataStores/issues)
- **Dokumentation**: [Wiki](https://github.com/your-repo/DataStores/wiki)
- **Diskussionen**: [GitHub Discussions](https://github.com/your-repo/DataStores/discussions)

---

**Made with ?? for .NET Developers**
