# DataStores - Vollständige API-Referenz

Diese Seite enthält eine vollständige Referenz aller öffentlichen Klassen, Interfaces, Methoden und Properties der DataStores-Bibliothek.

## ?? Inhaltsverzeichnis

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
  - [LocalDataStoreFactory](#localdatastorefactory)
- [Persistence](#persistence)
  - [IPersistenceStrategy\<T\>](#ipersistencestrategyt)
  - [PersistentStoreDecorator\<T\>](#persistentstoredecoratort)
  - [IAsyncInitializable](#iasyncin initializable)
- [Relations](#relations)
  - [ParentChildRelationship\<TParent, TChild\>](#parentchildrelationshiptparent-tchild)
- [Bootstrap](#bootstrap)
  - [DataStoreBootstrap](#datastorebootstrap)
  - [ServiceCollectionExtensions](#servicecollectionextensions)

---

## Abstractions

Namespace: `DataStores.Abstractions`

### IDataStore\<T\>

**Beschreibung**: Repräsentiert einen Datenspeicher, der eine Sammlung von Elementen vom Typ T enthält.

**Type Parameter**:
- `T` - Der Typ der Elemente im Store. Muss eine Referenztyp (class) sein.

#### Properties

##### Items
```csharp
IReadOnlyList<T> Items { get; }
```
**Beschreibung**: Ruft die schreibgeschützte Sammlung aller Elemente im Store ab.  
**Rückgabewert**: Eine schreibgeschützte Liste aller Elemente.  
**Thread-Sicherheit**: Thread-sicher. Gibt immer eine Snapshot-Kopie zurück.

#### Events

##### Changed
```csharp
event EventHandler<DataStoreChangedEventArgs<T>> Changed;
```
**Beschreibung**: Tritt ein, wenn sich der Datenspeicher ändert (Add, Remove, Clear, etc.).  
**Event Args**: `DataStoreChangedEventArgs<T>` mit Details zur Änderung.  
**Thread-Sicherheit**: Thread-sicher. Events können auf verschiedenen Threads ausgelöst werden.

#### Methoden

##### Add
```csharp
void Add(T item);
```
**Beschreibung**: Fügt ein einzelnes Element zum Store hinzu.  
**Parameter**:
- `item` - Das hinzuzufügende Element.  

**Exceptions**:
- `ArgumentNullException` - Wenn `item` null ist.  

**Events**: Löst `Changed` mit `ChangeType.Add` aus.  
**Thread-Sicherheit**: Thread-sicher.

##### AddRange
```csharp
void AddRange(IEnumerable<T> items);
```
**Beschreibung**: Fügt mehrere Elemente in einer einzigen Bulk-Operation zum Store hinzu.  
**Parameter**:
- `items` - Die hinzuzufügenden Elemente.  

**Exceptions**:
- `ArgumentNullException` - Wenn `items` null ist.  

**Events**: Löst `Changed` mit `ChangeType.BulkAdd` aus.  
**Performance**: Effizienter als mehrfache `Add()` Aufrufe.  
**Thread-Sicherheit**: Thread-sicher.

##### Remove
```csharp
bool Remove(T item);
```
**Beschreibung**: Entfernt ein Element aus dem Store.  
**Parameter**:
- `item` - Das zu entfernende Element.  

**Rückgabewert**: `true` wenn das Element entfernt wurde, andernfalls `false`.  
**Events**: Löst `Changed` mit `ChangeType.Remove` aus (nur bei Erfolg).  
**Vergleich**: Verwendet den konfigurierten `IEqualityComparer<T>`.  
**Thread-Sicherheit**: Thread-sicher.

##### Clear
```csharp
void Clear();
```
**Beschreibung**: Entfernt alle Elemente aus dem Store.  
**Events**: Löst `Changed` mit `ChangeType.Clear` aus.  
**Thread-Sicherheit**: Thread-sicher.

##### Contains
```csharp
bool Contains(T item);
```
**Beschreibung**: Bestimmt, ob der Store ein bestimmtes Element enthält.  
**Parameter**:
- `item` - Das zu suchende Element.  

**Rückgabewert**: `true` wenn das Element gefunden wurde, andernfalls `false`.  
**Vergleich**: Verwendet den konfigurierten `IEqualityComparer<T>`.  
**Thread-Sicherheit**: Thread-sicher.

---

### IDataStores

**Beschreibung**: Stellt eine Facade für den Zugriff auf globale und die Erstellung lokaler Datenspeicher bereit.

#### Methoden

##### GetGlobal\<T\>
```csharp
IDataStore<T> GetGlobal<T>() where T : class;
```
**Beschreibung**: Ruft den global registrierten Datenspeicher für Typ T ab.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.  

**Rückgabewert**: Der globale Datenspeicher für Typ T.  
**Exceptions**:
- `GlobalStoreNotRegisteredException` - Wenn kein globaler Store für Typ T registriert ist.  

**Verwendung**: Für application-wide Singleton-Stores.

##### CreateLocal\<T\>
```csharp
IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class;
```
**Beschreibung**: Erstellt einen neuen lokalen In-Memory-Datenspeicher.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.  

**Parameter**:
- `comparer` (optional) - Gleichheitsvergleicher für Elemente. Standard ist `EqualityComparer<T>.Default`.  

**Rückgabewert**: Ein neuer lokaler In-Memory-Datenspeicher.  
**Verwendung**: Für isolierte, temporäre Stores (z.B. in Dialogen, Formularen).

##### CreateLocalSnapshotFromGlobal\<T\>
```csharp
IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
    Func<T, bool>? predicate = null,
    IEqualityComparer<T>? comparer = null) where T : class;
```
**Beschreibung**: Erstellt einen neuen lokalen In-Memory-Datenspeicher und füllt ihn mit einem Snapshot aus dem globalen Store.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.  

**Parameter**:
- `predicate` (optional) - Filterprädikat für den Snapshot. Wenn null, werden alle Elemente kopiert.
- `comparer` (optional) - Gleichheitsvergleicher für Elemente.  

**Rückgabewert**: Ein neuer lokaler In-Memory-Datenspeicher mit gefilterten Daten.  
**Exceptions**:
- `GlobalStoreNotRegisteredException` - Wenn kein globaler Store für Typ T registriert ist.  

**Verwendung**: Für gefilterte, lokale Kopien von globalen Daten.

---

### IGlobalStoreRegistry

**Beschreibung**: Verwaltet die Registrierung und Auflösung globaler Datenspeicher.

#### Methoden

##### RegisterGlobal\<T\>
```csharp
void RegisterGlobal<T>(IDataStore<T> store) where T : class;
```
**Beschreibung**: Registriert einen globalen Datenspeicher für Typ T.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.  

**Parameter**:
- `store` - Der zu registrierende Datenspeicher.  

**Exceptions**:
- `ArgumentNullException` - Wenn `store` null ist.
- `GlobalStoreAlreadyRegisteredException` - Wenn bereits ein Store für Typ T registriert ist.  

**Thread-Sicherheit**: Thread-sicher.

##### ResolveGlobal\<T\>
```csharp
IDataStore<T> ResolveGlobal<T>() where T : class;
```
**Beschreibung**: Löst den globalen Datenspeicher für Typ T auf.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.  

**Rückgabewert**: Der registrierte globale Datenspeicher.  
**Exceptions**:
- `GlobalStoreNotRegisteredException` - Wenn kein Store für Typ T registriert ist.  

**Thread-Sicherheit**: Thread-sicher.

##### TryResolveGlobal\<T\>
```csharp
bool TryResolveGlobal<T>(out IDataStore<T> store) where T : class;
```
**Beschreibung**: Versucht, den globalen Datenspeicher für Typ T aufzulösen.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.  

**Parameter**:
- `store` (out) - Der aufgelöste Store, falls gefunden; andernfalls null.  

**Rückgabewert**: `true` wenn ein Store gefunden wurde; andernfalls `false`.  
**Thread-Sicherheit**: Thread-sicher.

---

### IDataStoreRegistrar

**Beschreibung**: Definiert einen Registrar, den Bibliotheken implementieren können, um ihre globalen Datenspeicher zu registrieren.

#### Methoden

##### Register
```csharp
void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider);
```
**Beschreibung**: Registriert globale Datenspeicher mit der Registry.  
**Parameter**:
- `registry` - Die globale Store-Registry.
- `serviceProvider` - Der Service-Provider für Dependency Resolution.  

**Verwendung**: Wird beim Bootstrap-Prozess aufgerufen.

---

### DataStoreChangedEventArgs\<T\>

**Beschreibung**: Stellt Daten für das `IDataStore<T>.Changed` Event bereit.

#### Properties

##### ChangeType
```csharp
DataStoreChangeType ChangeType { get; }
```
**Beschreibung**: Ruft den Typ der Änderung ab, die aufgetreten ist.  
**Werte**:
- `Add` - Ein einzelnes Element wurde hinzugefügt
- `BulkAdd` - Mehrere Elemente wurden in einer Bulk-Operation hinzugefügt
- `Remove` - Ein Element wurde entfernt
- `Clear` - Alle Elemente wurden gelöscht
- `Reset` - Die gesamte Sammlung wurde zurückgesetzt

##### AffectedItems
```csharp
IReadOnlyList<T> AffectedItems { get; }
```
**Beschreibung**: Ruft die von der Änderung betroffenen Elemente ab, falls zutreffend.  
**Hinweis**: Bei `Clear` ist diese Liste leer.

#### Konstruktoren

##### DataStoreChangedEventArgs(DataStoreChangeType, IReadOnlyList\<T\>)
```csharp
public DataStoreChangedEventArgs(DataStoreChangeType changeType, IReadOnlyList<T> affectedItems);
```
**Beschreibung**: Initialisiert eine neue Instanz für mehrere betroffene Elemente.  
**Parameter**:
- `changeType` - Der Typ der Änderung.
- `affectedItems` - Die betroffenen Elemente.

##### DataStoreChangedEventArgs(DataStoreChangeType, T)
```csharp
public DataStoreChangedEventArgs(DataStoreChangeType changeType, T item);
```
**Beschreibung**: Initialisiert eine neue Instanz für ein einzelnes betroffenes Element.  
**Parameter**:
- `changeType` - Der Typ der Änderung.
- `item` - Das einzelne betroffene Element.

##### DataStoreChangedEventArgs(DataStoreChangeType)
```csharp
public DataStoreChangedEventArgs(DataStoreChangeType changeType);
```
**Beschreibung**: Initialisiert eine neue Instanz ohne betroffene Elemente.  
**Parameter**:
- `changeType` - Der Typ der Änderung.

---

### Exceptions

#### GlobalStoreNotRegisteredException

**Beschreibung**: Exception, die ausgelöst wird, wenn versucht wird, auf einen globalen Store zuzugreifen, der nicht registriert wurde.

**Properties**:
- `Type StoreType { get; }` - Der Typ des Stores, der nicht registriert wurde.

**Konstruktoren**:
```csharp
public GlobalStoreNotRegisteredException(Type storeType);
public GlobalStoreNotRegisteredException(Type storeType, string message);
public GlobalStoreNotRegisteredException(Type storeType, string message, Exception innerException);
```

#### GlobalStoreAlreadyRegisteredException

**Beschreibung**: Exception, die ausgelöst wird, wenn versucht wird, einen globalen Store für einen Typ zu registrieren, der bereits eine Registrierung hat.

**Properties**:
- `Type StoreType { get; }` - Der Typ des Stores, der bereits registriert wurde.

**Konstruktoren**:
```csharp
public GlobalStoreAlreadyRegisteredException(Type storeType);
public GlobalStoreAlreadyRegisteredException(Type storeType, string message);
public GlobalStoreAlreadyRegisteredException(Type storeType, string message, Exception innerException);
```

---

## Runtime

Namespace: `DataStores.Runtime`

### InMemoryDataStore\<T\>

**Beschreibung**: Stellt eine In-Memory-Implementierung von `IDataStore<T>` bereit.

**Type Parameter**:
- `T` - Der Typ der Elemente im Store.

#### Konstruktoren

##### InMemoryDataStore(IEqualityComparer\<T\>?, SynchronizationContext?)
```csharp
public InMemoryDataStore(
    IEqualityComparer<T>? comparer = null,
    SynchronizationContext? synchronizationContext = null);
```
**Beschreibung**: Initialisiert eine neue Instanz der InMemoryDataStore-Klasse.  
**Parameter**:
- `comparer` (optional) - Gleichheitsvergleicher. Wenn null, wird der Standard-Vergleicher verwendet.
- `synchronizationContext` (optional) - Synchronisationskontext für Event-Aufruf. Wenn null, werden Events synchron ausgelöst.

**SynchronizationContext-Verwendung**: Nützlich für UI-Anwendungen (WPF, WinForms, MAUI), um Events auf dem UI-Thread auszulösen.

#### Properties

Siehe [IDataStore\<T\>](#idatastoret) - Alle Properties und Methoden von `IDataStore<T>` sind vollständig implementiert.

#### Thread-Sicherheit

**Implementierungsdetails**:
- Verwendet `lock`-basierte Synchronisation für alle Operationen
- Alle öffentlichen Methoden sind thread-sicher
- `Items` Property gibt immer eine neue Snapshot-Liste zurück
- Events werden außerhalb von Locks ausgelöst, um Deadlocks zu vermeiden

---

### DataStoresFacade

**Beschreibung**: Facade-Implementierung für den Zugriff auf globale und die Erstellung lokaler Datenspeicher.

#### Konstruktoren

##### DataStoresFacade(IGlobalStoreRegistry, ILocalDataStoreFactory)
```csharp
public DataStoresFacade(IGlobalStoreRegistry registry, ILocalDataStoreFactory localFactory);
```
**Beschreibung**: Initialisiert eine neue Instanz der DataStoresFacade-Klasse.  
**Parameter**:
- `registry` - Die globale Store-Registry.
- `localFactory` - Die Factory zum Erstellen lokaler Stores.

**Exceptions**:
- `ArgumentNullException` - Wenn einer der Parameter null ist.

#### Methoden

Siehe [IDataStores](#idatastores) - Implementiert alle Methoden der `IDataStores` Schnittstelle.

---

### GlobalStoreRegistry

**Beschreibung**: Thread-sichere Implementierung von `IGlobalStoreRegistry`.

#### Konstruktoren

##### GlobalStoreRegistry()
```csharp
public GlobalStoreRegistry();
```
**Beschreibung**: Initialisiert eine neue Instanz der GlobalStoreRegistry-Klasse.  
**Implementierung**: Verwendet `ConcurrentDictionary` für thread-sichere Operationen.

#### Methoden

Siehe [IGlobalStoreRegistry](#iglobalstoreregistry) - Implementiert alle Methoden der `IGlobalStoreRegistry` Schnittstelle.

---

### LocalDataStoreFactory

**Beschreibung**: Default-Implementierung der `ILocalDataStoreFactory`.

#### Interface: ILocalDataStoreFactory

##### CreateLocal\<T\>
```csharp
InMemoryDataStore<T> CreateLocal<T>(
    IEqualityComparer<T>? comparer = null,
    SynchronizationContext? context = null) where T : class;
```
**Beschreibung**: Erstellt einen neuen lokalen In-Memory-Datenspeicher.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.

**Parameter**:
- `comparer` (optional) - Gleichheitsvergleicher.
- `context` (optional) - Synchronisationskontext.

**Rückgabewert**: Ein neuer InMemoryDataStore.

---

## Persistence

Namespace: `DataStores.Persistence`

### IPersistenceStrategy\<T\>

**Beschreibung**: Definiert eine Strategie zum Persistieren und Laden von Daten.

**Type Parameter**:
- `T` - Der Typ der zu persistierenden Elemente.

#### Methoden

##### LoadAllAsync
```csharp
Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default);
```
**Beschreibung**: Lädt alle Elemente aus dem Persistenz-Store.  
**Parameter**:
- `cancellationToken` (optional) - Cancellation Token.

**Rückgabewert**: Eine schreibgeschützte Liste aller geladenen Elemente.  
**Exceptions**: Implementation-spezifisch (z.B. `IOException`, `UnauthorizedAccessException`).

##### SaveAllAsync
```csharp
Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default);
```
**Beschreibung**: Speichert alle Elemente im Persistenz-Store.  
**Parameter**:
- `items` - Die zu speichernden Elemente.
- `cancellationToken` (optional) - Cancellation Token.

**Exceptions**: Implementation-spezifisch.

---

### PersistentStoreDecorator\<T\>

**Beschreibung**: Dekoriert einen `IDataStore<T>` mit Persistenz-Funktionalität.

**Type Parameter**:
- `T` - Der Typ der Elemente im Store.

**Implementiert**:
- `IDataStore<T>`
- `IAsyncInitializable`
- `IDisposable`

#### Konstruktoren

##### PersistentStoreDecorator(InMemoryDataStore\<T\>, IPersistenceStrategy\<T\>, bool, bool)
```csharp
public PersistentStoreDecorator(
    InMemoryDataStore<T> innerStore,
    IPersistenceStrategy<T> strategy,
    bool autoLoad = true,
    bool autoSaveOnChange = true);
```
**Beschreibung**: Initialisiert eine neue Instanz der PersistentStoreDecorator-Klasse.  
**Parameter**:
- `innerStore` - Der innere In-Memory-Store zum Dekorieren.
- `strategy` - Die zu verwendende Persistierungsstrategie.
- `autoLoad` (optional) - Wenn true, werden Daten während der Initialisierung geladen.
- `autoSaveOnChange` (optional) - Wenn true, werden Daten automatisch bei Änderungen gespeichert.

**Exceptions**:
- `ArgumentNullException` - Wenn `innerStore` oder `strategy` null ist.

#### Methoden

##### InitializeAsync
```csharp
public async Task InitializeAsync(CancellationToken cancellationToken = default);
```
**Beschreibung**: Initialisiert den Store asynchron durch Laden der Daten.  
**Parameter**:
- `cancellationToken` (optional) - Cancellation Token.

**Verhalten**:
- Lädt Daten nur einmal (idempotent)
- Thread-sicher (verwendet SemaphoreSlim)
- Ruft `strategy.LoadAllAsync()` auf

##### Dispose
```csharp
public void Dispose();
```
**Beschreibung**: Gibt verwendete Ressourcen frei.  
**Verhalten**:
- Trennt Event-Handler
- Gibt Semaphores frei
- Idempotent (mehrfache Aufrufe sind sicher)

#### Properties & Events

Siehe [IDataStore\<T\>](#idatastoret) - Alle Properties, Events und Methoden von `IDataStore<T>` werden an den inneren Store delegiert.

#### Auto-Save Verhalten

**Bei autoSaveOnChange = true**:
- Speichert automatisch bei jeder Änderung (Add, Remove, Clear, etc.)
- Verwendet SemaphoreSlim zur Vermeidung von Race Conditions
- Fire-and-Forget Pattern (blockiert keine Operationen)
- Fehler beim Speichern werden gefangen (silent fail)

---

### IAsyncInitializable

**Beschreibung**: Marker-Interface für Typen, die asynchrone Initialisierung erfordern.

#### Methoden

##### InitializeAsync
```csharp
Task InitializeAsync(CancellationToken cancellationToken = default);
```
**Beschreibung**: Initialisiert die Instanz asynchron.  
**Parameter**:
- `cancellationToken` (optional) - Cancellation Token.

**Verwendung**: Wird von `DataStoreBootstrap` aufgerufen, um alle `IAsyncInitializable` Services zu initialisieren.

---

### PersistentStoreRegistrationExtensions

**Beschreibung**: Stellt Erweiterungsmethoden für die Registrierung persistenter Stores bereit.

#### Methoden

##### RegisterPersistent\<T\> (mit Factory)
```csharp
public static PersistentStoreDecorator<T> RegisterPersistent<T>(
    this IGlobalStoreRegistry registry,
    Func<InMemoryDataStore<T>> createInnerStore,
    IPersistenceStrategy<T> strategy,
    bool autoLoad = true,
    bool autoSaveOnChange = true) where T : class;
```
**Beschreibung**: Registriert einen persistenten globalen Datenspeicher.  
**Type Parameter**:
- `T` - Der Typ der Elemente im Store.

**Parameter**:
- `registry` - Die globale Store-Registry.
- `createInnerStore` - Factory-Funktion zum Erstellen des inneren In-Memory-Stores.
- `strategy` - Die Persistierungsstrategie.
- `autoLoad` (optional) - Wenn true, werden Daten während der Initialisierung geladen.
- `autoSaveOnChange` (optional) - Wenn true, werden Daten automatisch bei Änderungen gespeichert.

**Rückgabewert**: Der erstellte persistente Store-Decorator.

##### RegisterPersistent\<T\> (Default)
```csharp
public static PersistentStoreDecorator<T> RegisterPersistent<T>(
    this IGlobalStoreRegistry registry,
    IPersistenceStrategy<T> strategy,
    bool autoLoad = true,
    bool autoSaveOnChange = true) where T : class;
```
**Beschreibung**: Registriert einen persistenten globalen Datenspeicher mit Default-innerem Store.  
**Parameter**: Siehe oben, aber ohne `createInnerStore` (verwendet `new InMemoryDataStore<T>()`).

**Verwendung**:
```csharp
registry.RegisterPersistent(
    new JsonPersistenceStrategy<Product>("products.json"),
    autoLoad: true,
    autoSaveOnChange: true);
```

---

## Relations

Namespace: `DataStores.Relations`

### ParentChildRelationship\<TParent, TChild\>

**Beschreibung**: Verwaltet eine Eltern-Kind-Beziehung zwischen Datenspeichern.

**Type Parameter**:
- `TParent` - Der Eltern-Entitätstyp.
- `TChild` - Der Kind-Entitätstyp.

#### Konstruktoren

##### ParentChildRelationship(IDataStores, TParent, Func\<TParent, TChild, bool\>)
```csharp
public ParentChildRelationship(
    IDataStores stores,
    TParent parent,
    Func<TParent, TChild, bool> filter);
```
**Beschreibung**: Initialisiert eine neue Instanz der ParentChildRelationship-Klasse.  
**Parameter**:
- `stores` - Die DataStores-Facade.
- `parent` - Die Eltern-Entität.
- `filter` - Die Filter-Funktion zur Bestimmung, welche Kinder zu diesem Elternteil gehören.

**Exceptions**:
- `ArgumentNullException` - Wenn einer der Parameter null ist.

#### Properties

##### Parent
```csharp
public TParent Parent { get; init; }
```
**Beschreibung**: Ruft die Eltern-Entität ab oder legt sie fest.  
**Init-only**: Kann nur während der Objektinitialisierung gesetzt werden.

##### DataSource
```csharp
public IDataStore<TChild> DataSource { get; set; }
```
**Beschreibung**: Ruft die Datenquelle für Kind-Elemente ab oder legt sie fest.  
**Exceptions**:
- `ArgumentNullException` - Beim Setzen auf null.
- `InvalidOperationException` - Beim Abrufen, wenn nicht gesetzt.

**Verwendung**: Sollte über `UseGlobalDataSource()` oder `UseSnapshotFromGlobal()` gesetzt werden.

##### Childs
```csharp
public InMemoryDataStore<TChild> Childs { get; }
```
**Beschreibung**: Ruft die lokale Sammlung von Kind-Elementen für diesen Elternteil ab.  
**Hinweis**: Wird durch `Refresh()` gefüllt.

##### Filter
```csharp
public Func<TParent, TChild, bool> Filter { get; init; }
```
**Beschreibung**: Ruft die Filter-Funktion ab oder legt sie fest.  
**Init-only**: Kann nur während der Objektinitialisierung gesetzt werden.

#### Methoden

##### UseGlobalDataSource
```csharp
public void UseGlobalDataSource();
```
**Beschreibung**: Setzt die Datenquelle auf den globalen Datenspeicher für TChild.  
**Exceptions**:
- `GlobalStoreNotRegisteredException` - Wenn kein globaler Store für TChild registriert ist.

##### UseSnapshotFromGlobal
```csharp
public void UseSnapshotFromGlobal(Func<TChild, bool>? predicate = null);
```
**Beschreibung**: Erstellt einen lokalen Snapshot aus dem globalen Datenspeicher und setzt ihn als Datenquelle.  
**Parameter**:
- `predicate` (optional) - Zusätzliche Filterprädikat.

**Exceptions**:
- `GlobalStoreNotRegisteredException` - Wenn kein globaler Store für TChild registriert ist.

##### Refresh
```csharp
public void Refresh();
```
**Beschreibung**: Aktualisiert die Kind-Sammlung durch Anwenden des Filters auf die Datenquelle.  
**Exceptions**:
- `InvalidOperationException` - Wenn DataSource nicht gesetzt wurde.

**Verhalten**:
1. Löscht `Childs` Sammlung
2. Filtert `DataSource.Items` mit `Filter(Parent, child)`
3. Fügt gefilterte Elemente zu `Childs` hinzu

---

## Bootstrap

Namespace: `DataStores.Bootstrap`

### DataStoreBootstrap

**Beschreibung**: Stellt Bootstrap-Funktionalität für die Initialisierung von Datenspeichern bereit.

#### Methoden

##### RunAsync
```csharp
public static async Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
```
**Beschreibung**: Führt den Datenspeicher-Bootstrap-Prozess aus, registriert alle Stores und initialisiert persistente Stores.  
**Parameter**:
- `serviceProvider` - Der Service-Provider.
- `cancellationToken` (optional) - Cancellation Token.

**Ablauf**:
1. Ruft `IGlobalStoreRegistry` aus dem Service-Provider ab
2. Ruft alle `IDataStoreRegistrar` Services ab
3. Ruft `Register()` für jeden Registrar auf
4. Ruft alle `IAsyncInitializable` Services ab
5. Ruft `InitializeAsync()` für jede instanz auf

**Verwendung**: Einmal beim Application-Start aufrufen.

##### Run
```csharp
public static void Run(IServiceProvider serviceProvider);
```
**Beschreibung**: Führt den Datenspeicher-Bootstrap-Prozess synchron aus (für Testing oder einfache Szenarien).  
**Parameter**:
- `serviceProvider` - Der Service-Provider.

**Hinweis**: Blockiert den aufrufenden Thread. Verwenden Sie `RunAsync()` für Produktionscode.

---

### ServiceCollectionExtensions

**Beschreibung**: Stellt Erweiterungsmethoden für die Registrierung von DataStores-Services mit Dependency Injection bereit.

#### Methoden

##### AddDataStoresCore
```csharp
public static IServiceCollection AddDataStoresCore(this IServiceCollection services);
```
**Beschreibung**: Registriert Kern-DataStores-Services mit der Service-Collection.  
**Parameter**:
- `services` - Die Service-Collection.

**Rückgabewert**: Die Service-Collection für Verkettung.

**Registrierte Services**:
- `IGlobalStoreRegistry` ? `GlobalStoreRegistry` (Singleton)
- `ILocalDataStoreFactory` ? `LocalDataStoreFactory` (Singleton)
- `IDataStores` ? `DataStoresFacade` (Singleton)

**Verwendung**:
```csharp
services.AddDataStoresCore();
```

##### AddDataStoreRegistrar\<TRegistrar\>
```csharp
public static IServiceCollection AddDataStoreRegistrar<TRegistrar>(this IServiceCollection services)
    where TRegistrar : class, IDataStoreRegistrar;
```
**Beschreibung**: Registriert einen Datenspeicher-Registrar mit der Service-Collection.  
**Type Parameter**:
- `TRegistrar` - Der Registrar-Typ.

**Parameter**:
- `services` - Die Service-Collection.

**Rückgabewert**: Die Service-Collection für Verkettung.

**Verwendung**:
```csharp
services.AddDataStoreRegistrar<MyProductStoreRegistrar>();
services.AddDataStoreRegistrar<MyCategoryStoreRegistrar>();
```

**Hinweis**: Mehrere Registrare können registriert werden - alle werden beim Bootstrap ausgeführt.

---

## Best Practices

### 1. Dependency Injection

**? Empfohlen**:
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

**? Vermeiden**:
```csharp
// Direkte Instanziierung vermeiden
var store = new InMemoryDataStore<Product>();
```

### 2. Event-Handling

**? Empfohlen**:
```csharp
// Event-Handler immer wieder entfernen
store.Changed += OnStoreChanged;
// ...
store.Changed -= OnStoreChanged;
```

**? Vermeiden**:
```csharp
// Memory Leak durch fehlende Event-Entfernung
store.Changed += (s, e) => { /* ... */ };
```

### 3. Persistierung

**? Empfohlen**:
```csharp
// Exception-Handling in IPersistenceStrategy
public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken ct)
{
    try
    {
        // Speicher-Logik
    }
    catch (IOException ex)
    {
        // Logging & Error-Handling
    }
}
```

### 4. Thread-Sicherheit

**? Sicher** (keine zusätzlichen Locks nötig):
```csharp
// Alle DataStore-Operationen sind thread-sicher
Parallel.For(0, 100, i => store.Add(new Product { Id = i }));
```

---

## Performance-Hinweise

### Items Property
- Gibt immer eine **neue Snapshot-Liste** zurück
- **O(n)** Zeitkomplexität
- Für häufige Abfragen: Lokal cachen

### AddRange vs. Add
- `AddRange()` ist effizienter für Bulk-Operationen
- Nur **ein Event** vs. mehrere Events
- **Ein Lock** vs. mehrere Locks

### Contains vs. Items.Any()
- `Contains()` bevorzugen - verwendet optimierten Comparer
- `Items.Any()` erstellt zusätzliche Snapshot-Liste

---

**Version**: 1.0.0  
**Letzte Aktualisierung**: Januar 2025
