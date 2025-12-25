# DataStores - Vollständige API-Referenz

Diese Seite enthält eine vollständige Referenz aller öffentlichen Klassen, Interfaces, Methoden und Properties der DataStores-Bibliothek.

## Inhaltsverzeichnis

- [Abstractions](#abstractions)
  - [IDataStore\<T\>](#idatastoret)
  - [IDataStores](#idatastores)
  - [IGlobalStoreRegistry](#iglobalstoreregistry)
  - [IDataStoreRegistrar](#idatastoreregistrar)
  - [DataStoreChangedEventArgs\<T\>](#datastorechangedeventargst)
  - [Exceptions](#exceptions)
- [Runtime](#runtime)
  - [InMemoryDataStore\<T\>](#inmemorydatastoret)
  - [DataStoresFacade](#datastoresfacade)
  - [GlobalStoreRegistry](#globalstoreregistry)
- [Persistence](#persistence)
  - [IPersistenceStrategy\<T\>](#ipersistencestrategyt)
  - [PersistentStoreDecorator\<T\>](#persistentstoredecoratort)
  - [JsonFilePersistenceStrategy\<T\>](#jsonfilepersistencestrategyt)
  - [LiteDbPersistenceStrategy\<T\>](#litedbpersistencestrategyt)
- [Relations](#relations)
  - [ParentChildRelationship\<TParent, TChild\>](#parentchildrelationshiptparent-tchild)
- [Bootstrap](#bootstrap)
  - [DataStoreBootstrap](#datastorebootstrap)
  - [ServiceCollectionExtensions](#servicecollectionextensions)

---

## Abstractions

Namespace: `DataStores.Abstractions`

### IDataStore\<T\>

Repräsentiert einen Datenspeicher, der eine Sammlung von Elementen vom Typ T enthält.

**Type Parameter:**
- `T` - Der Typ der Elemente im Store. Muss ein Referenztyp (class) sein.

#### Properties

##### Items
```csharp
IReadOnlyList<T> Items { get; }
```
- **Beschreibung:** Ruft die schreibgeschützte Sammlung aller Elemente im Store ab.
- **Rückgabewert:** Eine schreibgeschützte Liste aller Elemente.
- **Thread-Sicherheit:** Thread-sicher. Gibt immer eine Snapshot-Kopie zurück.

#### Events

##### Changed
```csharp
event EventHandler<DataStoreChangedEventArgs<T>> Changed;
```
- **Beschreibung:** Tritt ein, wenn sich der Datenspeicher ändert (Add, Remove, Clear, etc.).
- **Event Args:** `DataStoreChangedEventArgs<T>` mit Details zur Änderung.
- **Thread-Sicherheit:** Thread-sicher.

#### Methoden

##### Add
```csharp
void Add(T item);
```
- **Beschreibung:** Fügt ein einzelnes Element zum Store hinzu.
- **Parameter:** `item` - Das hinzuzufügende Element.
- **Exceptions:** `ArgumentNullException` - Wenn `item` null ist.
- **Events:** Löst `Changed` mit `ChangeType.Add` aus.

##### AddRange
```csharp
void AddRange(IEnumerable<T> items);
```
- **Beschreibung:** Fügt mehrere Elemente in einer Bulk-Operation zum Store hinzu.
- **Parameter:** `items` - Die hinzuzufügenden Elemente.
- **Exceptions:** `ArgumentNullException` - Wenn `items` null ist.
- **Events:** Löst `Changed` mit `ChangeType.BulkAdd` aus.

##### Remove
```csharp
bool Remove(T item);
```
- **Beschreibung:** Entfernt ein Element aus dem Store.
- **Parameter:** `item` - Das zu entfernende Element.
- **Rückgabewert:** `true` wenn entfernt, andernfalls `false`.
- **Events:** Löst `Changed` mit `ChangeType.Remove` aus (nur bei Erfolg).

##### Clear
```csharp
void Clear();
```
- **Beschreibung:** Entfernt alle Elemente aus dem Store.
- **Events:** Löst `Changed` mit `ChangeType.Clear` aus.

##### Contains
```csharp
bool Contains(T item);
```
- **Beschreibung:** Bestimmt, ob der Store ein bestimmtes Element enthält.
- **Parameter:** `item` - Das zu suchende Element.
- **Rückgabewert:** `true` wenn gefunden, andernfalls `false`.

---

### IDataStores

Stellt eine Facade für den Zugriff auf globale und die Erstellung lokaler Datenspeicher bereit.

#### Methoden

##### GetGlobal\<T\>
```csharp
IDataStore<T> GetGlobal<T>() where T : class;
```
- **Beschreibung:** Ruft den global registrierten Datenspeicher für Typ T ab.
- **Rückgabewert:** Der globale Datenspeicher für Typ T.
- **Exceptions:** `GlobalStoreNotRegisteredException` - Wenn kein Store registriert ist.

##### CreateLocal\<T\>
```csharp
IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class;
```
- **Beschreibung:** Erstellt einen neuen lokalen In-Memory-Datenspeicher.
- **Parameter:** `comparer` (optional) - Gleichheitsvergleicher.
- **Rückgabewert:** Ein neuer lokaler In-Memory-Datenspeicher.

##### CreateLocalSnapshotFromGlobal\<T\>
```csharp
IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
    Func<T, bool>? predicate = null,
    IEqualityComparer<T>? comparer = null) where T : class;
```
- **Beschreibung:** Erstellt einen lokalen Snapshot aus dem globalen Store.
- **Parameter:**
  - `predicate` (optional) - Filterprä dikat.
  - `comparer` (optional) - Gleichheitsvergleicher.
- **Rückgabewert:** Ein neuer lokaler Store mit gefilterten Daten.

---

### IGlobalStoreRegistry

Verwaltet die Registrierung und Auflösung globaler Datenspeicher.

#### Methoden

##### RegisterGlobal\<T\>
```csharp
void RegisterGlobal<T>(IDataStore<T> store) where T : class;
```
- **Beschreibung:** Registriert einen globalen Datenspeicher für Typ T.
- **Parameter:** `store` - Der zu registrierende Datenspeicher.
- **Exceptions:**
  - `ArgumentNullException` - Wenn `store` null ist.
  - `GlobalStoreAlreadyRegisteredException` - Wenn bereits registriert.

##### ResolveGlobal\<T\>
```csharp
IDataStore<T> ResolveGlobal<T>() where T : class;
```
- **Beschreibung:** Löst den globalen Datenspeicher für Typ T auf.
- **Rückgabewert:** Der registrierte globale Datenspeicher.
- **Exceptions:** `GlobalStoreNotRegisteredException` - Wenn nicht registriert.

##### TryResolveGlobal\<T\>
```csharp
bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class;
```
- **Beschreibung:** Versucht, den globalen Datenspeicher aufzulösen.
- **Parameter:** `store` (out) - Der aufgelöste Store.
- **Rückgabewert:** `true` wenn gefunden, andernfalls `false`.

---

### IDataStoreRegistrar

Definiert einen Registrar für die Registrierung globaler Datenspeicher beim Bootstrap.

#### Methoden

##### Register
```csharp
void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider);
```
- **Beschreibung:** Registriert globale Datenspeicher mit der Registry.
- **Parameter:**
  - `registry` - Die globale Store-Registry.
  - `serviceProvider` - Der Service-Provider.
- **Verwendung:** Wird beim Bootstrap-Prozess aufgerufen.

---

### DataStoreChangedEventArgs\<T\>

Stellt Daten für das `IDataStore<T>.Changed` Event bereit.

#### Properties

##### ChangeType
```csharp
DataStoreChangeType ChangeType { get; }
```
- **Beschreibung:** Ruft den Typ der Änderung ab.
- **Werte:**
  - `Add` - Einzelnes Element hinzugefügt
  - `BulkAdd` - Mehrere Elemente hinzugefügt
  - `Remove` - Element entfernt
  - `Clear` - Alle Elemente gelöscht
  - `Reset` - Sammlung zurückgesetzt

##### AffectedItems
```csharp
IReadOnlyList<T> AffectedItems { get; }
```
- **Beschreibung:** Die betroffenen Elemente (leer bei `Clear`).

---

### Exceptions

#### GlobalStoreNotRegisteredException

Exception beim Zugriff auf nicht registrierten globalen Store.

**Properties:**
- `Type StoreType { get; }` - Der nicht registrierte Typ.

#### GlobalStoreAlreadyRegisteredException

Exception bei doppelter Registrierung.

**Properties:**
- `Type StoreType { get; }` - Der bereits registrierte Typ.

---

## Runtime

Namespace: `DataStores.Runtime`

### InMemoryDataStore\<T\>

Thread-sichere In-Memory-Implementierung von `IDataStore<T>`.

#### Konstruktoren

```csharp
public InMemoryDataStore(
    IEqualityComparer<T>? comparer = null,
    SynchronizationContext? synchronizationContext = null);
```
- **Parameter:**
  - `comparer` (optional) - Gleichheitsvergleicher.
  - `synchronizationContext` (optional) - Für UI-Thread-Marshaling.

#### Properties

Siehe [IDataStore\<T\>](#idatastoret) - Alle Properties und Methoden von `IDataStore<T>` sind vollständig implementiert.

#### Thread-Sicherheit

- Verwendet `lock`-basierte Synchronisation
- `Items` gibt immer neue Snapshot-Liste zurück
- Events werden außerhalb von Locks ausgelöst

---

### DataStoresFacade

Facade-Implementierung für `IDataStores`.

#### Konstruktoren

```csharp
public DataStoresFacade(IGlobalStoreRegistry registry, ILocalDataStoreFactory localFactory);
```

---

### GlobalStoreRegistry

Thread-sichere Implementierung von `IGlobalStoreRegistry`.

- Verwendet `ConcurrentDictionary` für thread-sichere Operationen.

---

## Persistence

Namespace: `DataStores.Persistence`

### IPersistenceStrategy\<T\>

Definiert eine Strategie zum Persistieren und Laden von Daten.

#### Methoden

##### LoadAllAsync
```csharp
Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default);
```
- **Beschreibung:** Lädt alle Elemente aus dem Persistenz-Store.
- **Rückgabewert:** Liste aller geladenen Elemente.

##### SaveAllAsync
```csharp
Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default);
```
- **Beschreibung:** Speichert alle Elemente im Persistenz-Store.
- **Parameter:** `items` - Die zu speichernden Elemente.

##### UpdateSingleAsync
```csharp
Task UpdateSingleAsync(T item, CancellationToken cancellationToken = default);
```
- **Beschreibung:** Aktualisiert ein einzelnes Element (für PropertyChanged-Tracking).
- **Parameter:** `item` - Das zu aktualisierende Element.

---

### PersistentStoreDecorator\<T\>

Dekoriert einen `IDataStore<T>` mit Persistenz-Funktionalität.

#### Konstruktoren

```csharp
public PersistentStoreDecorator(
    InMemoryDataStore<T> innerStore,
    IPersistenceStrategy<T> strategy,
    bool autoLoad = true,
    bool autoSaveOnChange = true);
```
- **Parameter:**
  - `innerStore` - Der innere In-Memory-Store.
  - `strategy` - Die Persistierungsstrategie.
  - `autoLoad` - Daten beim Bootstrap laden.
  - `autoSaveOnChange` - Automatisch speichern bei Änderungen.

#### Auto-Save Verhalten

Bei `autoSaveOnChange = true`:
- Speichert automatisch bei Änderungen (Add, Remove, Clear)
- PropertyChanged-Tracking für `INotifyPropertyChanged`-Entities
- Fire-and-Forget Pattern (blockiert keine Operationen)
- Fehler beim Speichern werden gefangen

---

### JsonFilePersistenceStrategy\<T\>

JSON-basierte Persistierungsstrategie.

#### Konstruktoren

```csharp
public JsonFilePersistenceStrategy(string filePath);
```
- **Parameter:** `filePath` - Pfad zur JSON-Datei.

#### Features

- UTF-8 Encoding
- Indentierte JSON-Formatierung
- Automatische Directory-Erstellung
- PropertyChanged-Tracking Unterstützung

---

### LiteDbPersistenceStrategy\<T\>

LiteDB-basierte Persistierungsstrategie für `EntityBase`-Typen.

#### Konstruktoren

```csharp
public LiteDbPersistenceStrategy(string databasePath, string? collectionName = null);
```
- **Parameter:**
  - `databasePath` - Pfad zur LiteDB-Datei.
  - `collectionName` (optional) - Name der Collection (default: Typ-Name).

#### Features

- Automatische ID-Zuweisung für `Id = 0` Entities
- PropertyChanged-Tracking Unterstützung
- Thread-sicher
- Automatische Collection-Erstellung

#### Besonderheiten

- Nur Entities mit `Id = 0` werden bei `SaveAllAsync` gespeichert
- IDs werden sofort nach `SaveAllAsync` zurückgeschrieben
- Verwendet `DeleteAll` + `InsertBulk` für vollständige Ersetzung

---

## Relations

Namespace: `DataStores.Relations`

### ParentChildRelationship\<TParent, TChild\>

Verwaltet eine Eltern-Kind-Beziehung zwischen Datenspeichern.

#### Konstruktoren

```csharp
public ParentChildRelationship(
    IDataStores stores,
    TParent parent,
    Func<TParent, TChild, bool> filter);
```

#### Properties

- `TParent Parent { get; init; }` - Die Eltern-Entität.
- `IDataStore<TChild> DataSource { get; set; }` - Die Datenquelle.
- `InMemoryDataStore<TChild> Childs { get; }` - Die Kind-Sammlung.
- `Func<TParent, TChild, bool> Filter { get; init; }` - Die Filter-Funktion.

#### Methoden

##### UseGlobalDataSource
```csharp
public void UseGlobalDataSource();
```
- Setzt die Datenquelle auf den globalen Store.

##### UseSnapshotFromGlobal
```csharp
public void UseSnapshotFromGlobal(Func<TChild, bool>? predicate = null);
```
- Erstellt einen lokalen Snapshot als Datenquelle.

##### Refresh
```csharp
public void Refresh();
```
- Aktualisiert die Kind-Sammlung durch Filter-Anwendung.

---

## Bootstrap

Namespace: `DataStores.Bootstrap`

### DataStoreBootstrap

Stellt Bootstrap-Funktionalität für die Initialisierung bereit.

#### Methoden

##### RunAsync
```csharp
public static async Task RunAsync(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken = default);
```
- **Beschreibung:** Führt den Bootstrap-Prozess aus.
- **Ablauf:**
  1. Ruft alle `IDataStoreRegistrar` auf
  2. Initialisiert alle `IAsyncInitializable` Services

##### Run
```csharp
public static void Run(IServiceProvider serviceProvider);
```
- Synchrone Variante (für Testing).

---

### ServiceCollectionExtensions

Erweiterungsmethoden für DI-Registrierung.

#### Methoden

##### AddDataStoresCore
```csharp
public static IServiceCollection AddDataStoresCore(this IServiceCollection services);
```
- Registriert Kern-DataStores-Services als Singletons.

##### AddDataStoreRegistrar\<TRegistrar\>
```csharp
public static IServiceCollection AddDataStoreRegistrar<TRegistrar>(
    this IServiceCollection services)
    where TRegistrar : class, IDataStoreRegistrar;
```
- Registriert einen Datenspeicher-Registrar.

##### AddDataStoreRegistrar (Instanz)
```csharp
public static IServiceCollection AddDataStoreRegistrar(
    this IServiceCollection services,
    IDataStoreRegistrar registrar);
```
- Registriert eine Registrar-Instanz (für Konstruktor-Konfiguration).

---

## Best Practices

### 1. Dependency Injection

**Empfohlen:**
```csharp
public class MyService
{
    private readonly IDataStores _stores;
    
    public MyService(IDataStores stores)
    {
        _stores = stores;
    }
}
```

### 2. Registrar-Pattern

**Empfohlen:**
```csharp
public class MyRegistrar : IDataStoreRegistrar
{
    private readonly string _dbPath;
    
    public MyRegistrar(string dbPath)
    {
        _dbPath = dbPath;
    }
    
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        registry.RegisterGlobalWithLiteDb<Order>(_dbPath, "orders");
    }
}

// Verwendung
services.AddDataStoreRegistrar(new MyRegistrar(dbPath));
```

### 3. Event-Handling

**Empfohlen:**
```csharp
store.Changed += OnStoreChanged;
// ...
store.Changed -= OnStoreChanged;
```

### 4. Performance

- **AddRange** bevorzugen für Bulk-Operationen
- **Items** cachen bei häufigen Abfragen
- **Contains** statt **Items.Any()** verwenden

---

**Version:** 1.0.0  
**Letzte Aktualisierung:** Januar 2025
