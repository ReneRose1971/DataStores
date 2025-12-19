# DataStores.Abstractions

Die Basis-Bibliothek mit allen Interfaces, Contracts und Exceptions für das DataStores-Framework.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Coverage](https://img.shields.io/badge/Coverage-95%25-brightgreen)](../DataStores.Tests/COMPLETE_REPORT.md)

---

## ?? Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [Komponenten](#komponenten)
- [API-Referenz](#api-referenz)
- [Verwendung](#verwendung)
- [Best Practices](#best-practices)

---

## ?? Übersicht

`DataStores.Abstractions` definiert die Kern-Contracts für das gesamte DataStores-Framework. Dieses Projekt enthält:

- ? **Interfaces** für Stores, Registry und Facades
- ? **Event-System** für Change-Notifications
- ? **Custom Exceptions** mit typsicheren Informationen
- ? **Registrar-Contracts** für Library-Integration

**Keine Implementierungen** - nur reine Abstractions!

---

## ?? Komponenten

### Core Interfaces

| Interface | Beschreibung |
|-----------|-------------|
| `IDataStore<T>` | Basis-Interface für Datenspeicher |
| `IDataStores` | Facade für Consumer-Zugriff |
| `IGlobalStoreRegistry` | Registry für globale Stores |
| `IDataStoreRegistrar` | Interface für Store-Registration |

### Event-System

| Klasse | Beschreibung |
|--------|-------------|
| `DataStoreChangedEventArgs<T>` | Event-Args für Store-Änderungen |
| `DataStoreChangeType` | Enum für Change-Typen |

### Exceptions

| Exception | Beschreibung |
|-----------|-------------|
| `GlobalStoreNotRegisteredException` | Store nicht registriert |
| `GlobalStoreAlreadyRegisteredException` | Store bereits registriert |

---

## ?? API-Referenz

**Vollständige API-Dokumentation:** [Docs/API.md](Docs/API.md)

### IDataStore<T>

Das zentrale Interface für alle Datenspeicher:

```csharp
public interface IDataStore<T> where T : class
{
    /// <summary>
    /// Liefert einen Snapshot aller Items im Store.
    /// Thread-safe und isoliert von späteren Änderungen.
    /// </summary>
    IReadOnlyList<T> Items { get; }
    
    /// <summary>
    /// Event wird gefeuert bei jeder Änderung im Store.
    /// </summary>
    event EventHandler<DataStoreChangedEventArgs<T>>? Changed;
    
    /// <summary>
    /// Fügt ein Item zum Store hinzu.
    /// </summary>
    void Add(T item);
    
    /// <summary>
    /// Fügt mehrere Items als Bulk-Operation hinzu.
    /// </summary>
    void AddRange(IEnumerable<T> items);
    
    /// <summary>
    /// Entfernt ein Item aus dem Store.
    /// </summary>
    /// <returns>true wenn Item gefunden und entfernt, sonst false</returns>
    bool Remove(T item);
    
    /// <summary>
    /// Entfernt alle Items aus dem Store.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Prüft ob ein Item im Store existiert.
    /// </summary>
    bool Contains(T item);
}
```

### IDataStores (Facade)

Die Haupt-API für Consumer:

```csharp
public interface IDataStores
{
    /// <summary>
    /// Liefert den globalen Store für Typ T.
    /// </summary>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Wenn kein Store für T registriert ist
    /// </exception>
    IDataStore<T> GetGlobal<T>() where T : class;
    
    /// <summary>
    /// Erstellt einen neuen lokalen Store.
    /// Unabhängig von globalen Stores.
    /// </summary>
    IDataStore<T> CreateLocal<T>(
        IEqualityComparer<T>? comparer = null,
        SynchronizationContext? context = null) where T : class;
    
    /// <summary>
    /// Erstellt einen lokalen Snapshot vom globalen Store.
    /// Optional mit zusätzlichem Filter.
    /// </summary>
    IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
        Func<T, bool>? predicate = null) where T : class;
}
```

### IGlobalStoreRegistry

Registry-Interface für Store-Management:

```csharp
public interface IGlobalStoreRegistry
{
    /// <summary>
    /// Registriert einen globalen Store für Typ T.
    /// </summary>
    /// <exception cref="GlobalStoreAlreadyRegisteredException">
    /// Wenn bereits ein Store für T existiert
    /// </exception>
    void RegisterGlobal<T>(IDataStore<T> store) where T : class;
    
    /// <summary>
    /// Löst einen registrierten globalen Store auf.
    /// </summary>
    /// <exception cref="GlobalStoreNotRegisteredException">
    /// Wenn kein Store für T registriert ist
    /// </exception>
    IDataStore<T> ResolveGlobal<T>() where T : class;
    
    /// <summary>
    /// Versucht einen globalen Store aufzulösen.
    /// </summary>
    /// <returns>true wenn Store gefunden, sonst false</returns>
    bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class;
}
```

---

## ?? Verwendung

### Consumer-Perspektive

Als Consumer verwenden Sie primär `IDataStores`:

```csharp
public class MyService
{
    private readonly IDataStores _stores;
    
    public MyService(IDataStores stores)
    {
        _stores = stores;
    }
    
    public void DoWork()
    {
        // Global Store verwenden
        var customerStore = _stores.GetGlobal<Customer>();
        customerStore.Add(new Customer { Id = 1, Name = "John" });
        
        // Lokalen Store erstellen
        var tempStore = _stores.CreateLocal<Customer>();
        tempStore.Add(new Customer { Id = 2, Name = "Jane" });
        
        // Snapshot erstellen
        var activeCustomers = _stores.CreateLocalSnapshotFromGlobal<Customer>(
            c => c.IsActive);
    }
}
```

### Library-Perspektive

Als Library-Entwickler implementieren Sie `IDataStoreRegistrar`:

```csharp
public class CustomerLibraryRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // Eigene Stores registrieren
        var customerStore = new InMemoryDataStore<Customer>();
        var orderStore = new InMemoryDataStore<Order>();
        
        registry.RegisterGlobal(customerStore);
        registry.RegisterGlobal(orderStore);
    }
}
```

---

## ?? Best Practices

### ? DO: IDataStores verwenden

```csharp
// ? RICHTIG - Facade verwenden
public class MyService
{
    private readonly IDataStores _stores;
    
    public MyService(IDataStores stores)
    {
        _stores = stores;
    }
}
```

### ? DON'T: Registry direkt injecten

```csharp
// ? FALSCH - Registry ist für Registrierung, nicht für Consumer
public class MyService
{
    private readonly IGlobalStoreRegistry _registry; // NICHT TUN!
}
```

### ? DO: Events für Reaktionen nutzen

```csharp
// ? RICHTIG - Event-basiert
var store = stores.GetGlobal<Customer>();
store.Changed += (sender, args) =>
{
    if (args.ChangeType == DataStoreChangeType.Add)
    {
        Console.WriteLine($"Neuer Customer: {args.AffectedItems[0].Name}");
    }
};
```

### ? DO: Snapshots für Isolation

```csharp
// ? RICHTIG - Snapshot für unabhängige Bearbeitung
var snapshot = stores.CreateLocalSnapshotFromGlobal<Customer>();
snapshot.Add(new Customer()); // Beeinflusst Global nicht
```

### ? DON'T: Exceptions für Logik verwenden

```csharp
// ? FALSCH - Exception für Flow-Control
try
{
    var store = stores.GetGlobal<Customer>();
}
catch (GlobalStoreNotRegisteredException)
{
    // Exception für normale Logik ist Anti-Pattern
}

// ? RICHTIG - TryResolveGlobal verwenden
var registry = ...; // Nur in Registrars!
if (registry.TryResolveGlobal<Customer>(out var store))
{
    // Store existiert
}
```

---

## ?? Weiterführende Dokumentation

- **[API-Referenz](Docs/API.md)** - Vollständige API-Dokumentation
- **[Event-System](Docs/EventSystem.md)** - Details zum Change-Notification-System
- **[Exceptions](Docs/Exceptions.md)** - Exception-Handling-Guide
- **[Registrar-Pattern](Docs/RegistrarPattern.md)** - Library-Integration

---

## ?? Verwandte Projekte

- **[DataStores.Runtime](../DataStores.Runtime/README.md)** - Implementierung der Interfaces
- **[DataStores.Bootstrap](../DataStores.Bootstrap/README.md)** - DI-Integration
- **[Solution README](../README.md)** - Gesamtübersicht

---

**[? Zurück zur Solution](../README.md)**
