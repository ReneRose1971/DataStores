# API-Referenz: DataStores.Abstractions

Vollständige Referenz aller öffentlichen Typen in `DataStores.Abstractions`.

---

## Interfaces

### IDataStore<T>

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Basis-Interface für alle Datenspeicher.

```csharp
public interface IDataStore<T> where T : class
```

#### Properties

| Property | Typ | Beschreibung |
|----------|-----|--------------|
| `Items` | `IReadOnlyList<T>` | Thread-sicherer Snapshot aller Items |

#### Events

| Event | Typ | Beschreibung |
|-------|-----|--------------|
| `Changed` | `EventHandler<DataStoreChangedEventArgs<T>>` | Wird bei Änderungen gefeuert |

#### Methoden

##### Add(T item)

Fügt ein Item zum Store hinzu.

```csharp
void Add(T item)
```

**Parameter:**
- `item` - Das hinzuzufügende Item

**Exceptions:**
- Keine (Implementation-spezifisch)

**Beispiel:**
```csharp
store.Add(new Customer { Id = 1, Name = "John" });
```

##### AddRange(IEnumerable<T> items)

Fügt mehrere Items als Bulk-Operation hinzu.

```csharp
void AddRange(IEnumerable<T> items)
```

**Parameter:**
- `items` - Die hinzuzufügenden Items

**Beispiel:**
```csharp
store.AddRange(new[] { customer1, customer2, customer3 });
```

##### Remove(T item)

Entfernt ein Item aus dem Store.

```csharp
bool Remove(T item)
```

**Returns:** `true` wenn Item gefunden und entfernt, sonst `false`

##### Clear()

Entfernt alle Items aus dem Store.

```csharp
void Clear()
```

##### Contains(T item)

Prüft ob ein Item im Store existiert.

```csharp
bool Contains(T item)
```

**Returns:** `true` wenn Item existiert, sonst `false`

---

### IDataStores

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Facade-Interface für Consumer-Zugriff auf Stores.

```csharp
public interface IDataStores
```

#### Methoden

##### GetGlobal<T>()

Liefert den registrierten globalen Store für Typ T.

```csharp
IDataStore<T> GetGlobal<T>() where T : class
```

**Returns:** Der globale Store für T

**Exceptions:**
- `GlobalStoreNotRegisteredException` - Kein Store für T registriert

**Beispiel:**
```csharp
var customerStore = stores.GetGlobal<Customer>();
```

##### CreateLocal<T>(IEqualityComparer<T>?, SynchronizationContext?)

Erstellt einen neuen lokalen Store.

```csharp
IDataStore<T> CreateLocal<T>(
    IEqualityComparer<T>? comparer = null,
    SynchronizationContext? context = null) 
    where T : class
```

**Parameter:**
- `comparer` - Optional: Custom Equality-Comparer
- `context` - Optional: SynchronizationContext für Events

**Returns:** Neuer lokaler Store

**Beispiel:**
```csharp
var localStore = stores.CreateLocal<Customer>();
```

##### CreateLocalSnapshotFromGlobal<T>(Func<T, bool>?)

Erstellt einen Snapshot vom globalen Store.

```csharp
IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
    Func<T, bool>? predicate = null) 
    where T : class
```

**Parameter:**
- `predicate` - Optional: Filter-Funktion

**Returns:** Lokaler Store mit Snapshot-Daten

**Exceptions:**
- `GlobalStoreNotRegisteredException` - Kein globaler Store für T

**Beispiel:**
```csharp
var activeCustomers = stores.CreateLocalSnapshotFromGlobal<Customer>(
    c => c.IsActive);
```

---

### IGlobalStoreRegistry

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Registry-Interface für Store-Verwaltung.

```csharp
public interface IGlobalStoreRegistry : IDisposable
```

#### Methoden

##### RegisterGlobal<T>(IDataStore<T>)

Registriert einen globalen Store.

```csharp
void RegisterGlobal<T>(IDataStore<T> store) where T : class
```

**Parameters:**
- `store` - Der zu registrierende Store

**Exceptions:**
- `GlobalStoreAlreadyRegisteredException` - Store bereits registriert

##### ResolveGlobal<T>()

Löst einen registrierten Store auf.

```csharp
IDataStore<T> ResolveGlobal<T>() where T : class
```

**Returns:** Der registrierte Store

**Exceptions:**
- `GlobalStoreNotRegisteredException` - Kein Store registriert

##### TryResolveGlobal<T>(out IDataStore<T>)

Versucht einen Store aufzulösen.

```csharp
bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class
```

**Returns:** `true` wenn Store gefunden

---

### IDataStoreRegistrar

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Interface für Library-Store-Registrierung.

```csharp
public interface IDataStoreRegistrar
```

#### Methoden

##### Register(IGlobalStoreRegistry, IServiceProvider)

Registriert Stores einer Library.

```csharp
void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
```

**Beispiel:**
```csharp
public class MyRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider sp)
    {
        registry.RegisterGlobal(new InMemoryDataStore<Customer>());
    }
}
```

---

## Klassen

### DataStoreChangedEventArgs<T>

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Event-Args für Store-Änderungen.

```csharp
public class DataStoreChangedEventArgs<T> : EventArgs
```

#### Properties

| Property | Typ | Beschreibung |
|----------|-----|--------------|
| `ChangeType` | `DataStoreChangeType` | Art der Änderung |
| `AffectedItems` | `IReadOnlyList<T>` | Betroffene Items |

#### Konstruktoren

```csharp
// Mit Item-Liste
public DataStoreChangedEventArgs(DataStoreChangeType changeType, IReadOnlyList<T> affectedItems)

// Mit einzelnem Item
public DataStoreChangedEventArgs(DataStoreChangeType changeType, T item)

// Ohne Items (z.B. Clear)
public DataStoreChangedEventArgs(DataStoreChangeType changeType)
```

---

## Enums

### DataStoreChangeType

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Typ der Store-Änderung.

```csharp
public enum DataStoreChangeType
{
    Add,       // Einzelnes Item hinzugefügt
    BulkAdd,   // Mehrere Items hinzugefügt
    Remove,    // Item entfernt
    Clear,     // Alle Items entfernt
    Reset      // Store zurückgesetzt
}
```

---

## Exceptions

### GlobalStoreNotRegisteredException

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Exception wenn Store nicht registriert ist.

```csharp
public class GlobalStoreNotRegisteredException : InvalidOperationException
```

#### Properties

| Property | Typ | Beschreibung |
|----------|-----|--------------|
| `StoreType` | `Type` | Der nicht registrierte Typ |

#### Konstruktoren

```csharp
public GlobalStoreNotRegisteredException(Type storeType)
```

### GlobalStoreAlreadyRegisteredException

**Namespace:** `DataStores.Abstractions`  
**Assembly:** DataStores.Abstractions.dll

Exception wenn Store bereits registriert ist.

```csharp
public class GlobalStoreAlreadyRegisteredException : InvalidOperationException
```

#### Properties

| Property | Typ | Beschreibung |
|----------|-----|--------------|
| `StoreType` | `Type` | Der bereits registrierte Typ |

---

**[? Zurück zum Projekt](../README.md)**
