# DataStores.Runtime

Die Kern-Implementierung des DataStores-Frameworks mit In-Memory-Stores, Registry und Facade.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Coverage](https://img.shields.io/badge/Coverage-98%25-brightgreen)](../DataStores.Tests/COMPLETE_REPORT.md)

---

## ?? Übersicht

`DataStores.Runtime` enthält die produktionsreifen Implementierungen:

- ? **InMemoryDataStore<T>** - Thread-sicherer In-Memory-Store
- ? **GlobalStoreRegistry** - Zentrale Store-Registry
- ? **DataStoresFacade** - Consumer-Facade-Implementierung
- ? **LocalDataStoreFactory** - Factory für lokale Stores

---

## ?? Schnellstart

```csharp
using DataStores.Runtime;

// Einfacher Store
var store = new InMemoryDataStore<Customer>();
store.Add(new Customer { Id = 1, Name = "John Doe" });

// Mit Custom Comparer
var comparer = new IdOnlyComparer();
var store2 = new InMemoryDataStore<Customer>(comparer);

// Mit SynchronizationContext (für UI)
var store3 = new InMemoryDataStore<Customer>(
    synchronizationContext: SynchronizationContext.Current);
```

---

## ?? Komponenten

### InMemoryDataStore<T>

Thread-sicherer In-Memory-Datenspeicher mit Event-System.

**Features:**
- Thread-Safe durch Lock-basierte Synchronisation
- Snapshot-Isolation für `Items`-Property
- SynchronizationContext-Support für Events
- Custom IEqualityComparer-Support

**Verwendung:**
```csharp
var store = new InMemoryDataStore<Product>();

// Events abonnieren
store.Changed += (s, e) => Console.WriteLine($"Changed: {e.ChangeType}");

// Items hinzufügen
store.Add(new Product { Id = 1, Name = "Laptop" });
store.AddRange(new[] { product2, product3 });

// Abfragen
var allProducts = store.Items; // Snapshot
var exists = store.Contains(product1);

// Entfernen
store.Remove(product1);
store.Clear();
```

### GlobalStoreRegistry

Thread-sichere Registry für globale Stores.

**Features:**
- ConcurrentDictionary-basiert
- ResolveGlobal & TryResolveGlobal
- IDisposable für Cleanup

**Verwendung:**
```csharp
var registry = new GlobalStoreRegistry();

// Registrieren
var customerStore = new InMemoryDataStore<Customer>();
registry.RegisterGlobal(customerStore);

// Auflösen
var resolved = registry.ResolveGlobal<Customer>();

// Try-Pattern
if (registry.TryResolveGlobal<Order>(out var orderStore))
{
    // Store existiert
}
```

### DataStoresFacade

Implementierung von `IDataStores` für Consumer.

**Verwendung:**
```csharp
var registry = new GlobalStoreRegistry();
var factory = new LocalDataStoreFactory();
var facade = new DataStoresFacade(registry, factory);

// Global Store
var global = facade.GetGlobal<Customer>();

// Local Store
var local = facade.CreateLocal<Customer>();

// Snapshot
var snapshot = facade.CreateLocalSnapshotFromGlobal<Customer>(
    c => c.IsActive);
```

---

## ?? API-Referenz

**Vollständige Dokumentation:** [Docs/API.md](Docs/API.md)

**Siehe auch:**
- [InMemoryDataStore Details](Docs/InMemoryDataStore.md)
- [Thread-Safety Guide](Docs/ThreadSafety.md)
- [SynchronizationContext Usage](Docs/SyncContext.md)

---

## ?? Best Practices

### Thread-Safety

```csharp
// ? RICHTIG - Store ist thread-safe
Parallel.For(0, 1000, i =>
{
    store.Add(new Customer { Id = i });
});

// ? RICHTIG - Items ist Snapshot
var snapshot = store.Items;
foreach (var item in snapshot) // Sicher, auch bei concurrent Adds
{
    Console.WriteLine(item.Name);
}
```

### SynchronizationContext

```csharp
// ? RICHTIG - UI-Thread-Marshaling
public class ViewModel
{
    private readonly InMemoryDataStore<Customer> _store;
    
    public ViewModel()
    {
        _store = new InMemoryDataStore<Customer>(
            synchronizationContext: SynchronizationContext.Current);
        
        _store.Changed += OnStoreChanged; // Event auf UI-Thread!
    }
    
    private void OnStoreChanged(object? sender, DataStoreChangedEventArgs<Customer> e)
    {
        // Läuft automatisch auf UI-Thread
        UpdateUI();
    }
}
```

### Custom Comparers

```csharp
// ? RICHTIG - Eigene Equality-Logic
public class IdOnlyComparer : IEqualityComparer<Customer>
{
    public bool Equals(Customer? x, Customer? y) => 
        x?.Id == y?.Id;
    
    public int GetHashCode(Customer obj) => 
        obj.Id.GetHashCode();
}

var store = new InMemoryDataStore<Customer>(new IdOnlyComparer());
store.Add(new Customer { Id = 1, Name = "John" });

// Contains findet by Id
var exists = store.Contains(new Customer { Id = 1, Name = "Different" }); // true!
```

---

## ?? Verwandte Projekte

- **[DataStores.Abstractions](../DataStores.Abstractions/README.md)** - Interfaces
- **[DataStores.Persistence](../DataStores.Persistence/README.md)** - Persistenz-Layer
- **[Solution README](../README.md)** - Gesamtübersicht

---

**[? Zurück zur Solution](../README.md)**
