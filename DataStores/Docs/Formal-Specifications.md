# DataStores - Formale Spezifikationen und Invarianten

Dieses Dokument beschreibt die formalen Verhaltensgarantien, Invarianten und Regeln der DataStores-Bibliothek.

## ?? Inhaltsverzeichnis

- [Globale Invarianten](#globale-invarianten)
- [GlobalStoreRegistry](#globalstoreregistry)
- [InMemoryDataStore](#inmemorydatastore)
- [PersistentStoreDecorator](#persistentstoredecorator)
- [ParentChildRelationship](#parentchildrelationship)
- [Thread-Sicherheits-Garantien](#thread-sicherheits-garantien)
- [Lebenszyklus-Regeln](#lebenszyklus-regeln)

---

## Globale Invarianten

### INV-GLOBAL-001: Type-Eindeutigkeit in globaler Registry

**Formale Beschreibung**:
```
? Type T ? RegisteredTypes: |GlobalStoreRegistry.Stores(T)| ? 1
```

**Natürlichsprachlich**:
Für jeden Typ T kann höchstens ein globaler Store in der Registry registriert sein.

**Durchsetzung**:
- `GlobalStoreRegistry.RegisterGlobal<T>()` wirft `GlobalStoreAlreadyRegisteredException`, wenn bereits ein Store für T existiert
- Technisch durchgesetzt durch `ConcurrentDictionary.TryAdd()`

**Konsequenzen**:
- Garantiert eindeutige Auflösung bei `GetGlobal<T>()`
- Verhindert versehentliche Mehrfachregistrierung
- Ermöglicht Singleton-Semantik für globale Stores

**Beispiel für Verletzung**:
```csharp
registry.RegisterGlobal(new InMemoryDataStore<Product>()); // ? OK
registry.RegisterGlobal(new InMemoryDataStore<Product>()); // ? Exception!
```

---

### INV-GLOBAL-002: Referenz-Stabilität globaler Stores

**Formale Beschreibung**:
```
? Type T, ? t?, t? ? Time: 
    GlobalStoreRegistry.ResolveGlobal<T>() at t? == 
    GlobalStoreRegistry.ResolveGlobal<T>() at t?
```

**Natürlichsprachlich**:
Wiederholte Aufrufe von `ResolveGlobal<T>()` für denselben Typ liefern immer dieselbe Store-Instanz (Referenzgleichheit).

**Durchsetzung**:
- Dictionary speichert Referenzen, keine Kopien
- Keine Mechanismen zum Ersetzen registrierter Stores

**Konsequenzen**:
- Änderungen am Store sind application-wide sichtbar
- Keine unerwarteten Store-Wechsel zur Laufzeit
- Konsistentes Verhalten über die gesamte Anwendung

**Beispiel**:
```csharp
var store1 = stores.GetGlobal<Product>();
var store2 = stores.GetGlobal<Product>();
Assert.True(ReferenceEquals(store1, store2)); // ? Immer wahr
```

---

### INV-GLOBAL-003: Registrierungs-Zeitpunkt

**Fachliche Regel**:
Alle globalen Stores MÜSSEN vor dem ersten `ResolveGlobal<T>()` Aufruf registriert sein.

**Empfohlener Zeitpunkt**:
Während des Bootstrap-Prozesses (`DataStoreBootstrap.RunAsync()`)

**Erlaubt aber nicht empfohlen**:
Lazy Registrierung zur Laufzeit (erhöht Komplexität)

**Verboten**:
Registrierung nach Beginn der normalen Anwendungsausführung

**Durchsetzung**:
- Technisch: Keine (könnte zur Laufzeit registriert werden)
- Architektonisch: Durch Konvention und Code-Reviews

**Begründung**:
- Vorhersehbares Verhalten
- Einfachere Fehlersuche
- Vermeidung von Race Conditions bei Initialisierung

---

## GlobalStoreRegistry

### INV-REGISTRY-001: Thread-sichere Operationen

**Formale Beschreibung**:
```
? Threads t?, t?, ..., t?:
    Concurrent execution of RegisterGlobal(), ResolveGlobal(), TryResolveGlobal()
    ? No race conditions, no data corruption
```

**Natürlichsprachlich**:
Alle Methoden der GlobalStoreRegistry können von beliebig vielen Threads gleichzeitig aufgerufen werden, ohne Race Conditions oder Datenkorruption zu verursachen.

**Implementierung**:
- `ConcurrentDictionary<Type, object>` als Thread-sichere Datenstruktur
- Keine zusätzlichen Locks erforderlich

**Garantien**:
1. **Atomare Registrierung**: `RegisterGlobal<T>()` ist atomar
2. **Konsistente Auflösung**: `ResolveGlobal<T>()` sieht immer konsistenten Zustand
3. **Keine Lost Updates**: Keine Registrierung geht verloren

**Performanz-Charakteristik**:
- O(1) für alle Operationen (im Durchschnitt)
- Lock-free für Lesevorgänge
- Lock-basiert für Schreibvorgänge (intern in ConcurrentDictionary)

---

### INV-REGISTRY-002: Keine Null-Stores

**Formale Beschreibung**:
```
? Type T: GlobalStoreRegistry.Stores(T) ? null
```

**Natürlichsprachlich**:
Die Registry akzeptiert keine null-Stores bei der Registrierung.

**Durchsetzung**:
```csharp
public void RegisterGlobal<T>(IDataStore<T> store) where T : class
{
    if (store == null)
        throw new ArgumentNullException(nameof(store));
    // ...
}
```

**Konsequenz**:
- `ResolveGlobal<T>()` gibt niemals null zurück
- Entweder: Gültiger Store oder `GlobalStoreNotRegisteredException`

---

### INV-REGISTRY-003: Type-Safety-Garantie

**Formale Beschreibung**:
```
? Type T: ResolveGlobal<T>() ? IDataStore<T>
```

**Natürlichsprachlich**:
Die Registry garantiert Type-Safety: Ein als `IDataStore<Product>` registrierter Store wird immer als `IDataStore<Product>` aufgelöst.

**Implementierung**:
- Verwendet `typeof(T)` als Schlüssel
- Cast wird intern durchgeführt
- Generics garantieren Typ-Sicherheit zur Compile-Zeit

**Garantie**:
Keine `InvalidCastException` bei korrekter Verwendung der API.

---

## InMemoryDataStore

### INV-STORE-001: Snapshot-Semantik für Items

**Formale Beschreibung**:
```
? Store s, ? Time t:
    items? = s.Items at t
    items? = s.Items at t
    ? items? ? items? (different references)
    ? items?.SequenceEqual(items?) (same content at time t)
```

**Natürlichsprachlich**:
Jeder Zugriff auf `Items` liefert eine neue Snapshot-Liste zurück. Änderungen an der zurückgegebenen Liste beeinflussen den Store nicht.

**Implementierung**:
```csharp
public IReadOnlyList<T> Items
{
    get
    {
        lock (_lock)
        {
            return _items.ToList(); // Neue Liste!
        }
    }
}
```

**Konsequenzen**:
- ? Thread-Sicherheit: Snapshots sind isoliert
- ? Keine unerwarteten Seiteneffekte
- ? Performance-Overhead bei großen Collections
- ? Speicher-Overhead bei häufigen Zugriffen

**Performance-Implikationen**:
- **Zeitkomplexität**: O(n) pro Zugriff
- **Speicherkomplexität**: O(n) neue Allokation pro Zugriff
- **Empfehlung**: Bei Collections >1000 Elementen und häufigen Zugriffen: Lokal cachen

**Anti-Pattern**:
```csharp
// ? SCHLECHT: Erstellt bei jedem Zugriff neue Liste
for (int i = 0; i < 1000; i++)
{
    var count = store.Items.Count; // 1000 neue Listen!
}

// ? GUT: Snapshot einmal erstellen
var items = store.Items;
for (int i = 0; i < 1000; i++)
{
    var count = items.Count; // Nur 1 neue Liste
}
```

---

### INV-STORE-002: Event-Garantien

**Formale Beschreibung**:
```
? Operation op ? {Add, AddRange, Remove, Clear}:
    op succeeds ? Changed event is fired
    op fails ? Changed event is NOT fired
```

**Natürlichsprachlich**:
Das `Changed` Event wird genau dann ausgelöst, wenn eine Operation erfolgreich war.

**Spezialfälle**:

1. **Remove bei nicht vorhandenem Element**:
```csharp
bool removed = store.Remove(notExistingItem);
// removed == false
// ? Kein Event wird ausgelöst
```

2. **AddRange mit leerer Collection**:
```csharp
store.AddRange(Array.Empty<Product>());
// ? Kein Event wird ausgelöst (frühzeitiger Return)
```

**Event-Reihenfolge**:
```
Operation execution ? Lock release ? Event firing
```

**Begründung für "außerhalb Lock"**:
- Verhindert Deadlocks
- Event-Handler können Store-Operationen durchführen
- Trade-off: Event-Handler sieht möglicherweise bereits weitere Änderungen

---

### INV-STORE-003: Comparer-Konsistenz

**Formale Beschreibung**:
```
? Store s mit Comparer c:
    s.Contains(item) uses c
    s.Remove(item) uses c
    ? Konsistentes Verhalten basierend auf c
```

**Natürlichsprachlich**:
Der bei Konstruktion übergebene `IEqualityComparer<T>` wird konsistent für alle Vergleichsoperationen verwendet.

**Betroffene Operationen**:
- `Contains(item)` - Verwendet Comparer
- `Remove(item)` - Verwendet Comparer zum Finden
- `Add(item)` - Verwendet Comparer NICHT (keine Duplikatsprüfung!)

**Wichtige Verhaltensregel**:
```csharp
var comparer = new ProductIdComparer(); // Vergleicht nur ID
var store = new InMemoryDataStore<Product>(comparer);

var p1 = new Product { Id = 1, Name = "A" };
var p2 = new Product { Id = 1, Name = "B" }; // Gleiche ID!

store.Add(p1);
store.Add(p2); // ? Beide werden hinzugefügt!

bool contains = store.Contains(p2); // true (wegen Comparer)
// Aber: store.Items.Count == 2 (Duplikate erlaubt!)
```

**Invariante**:
InMemoryDataStore führt KEINE automatische Duplikatsprüfung durch, selbst mit Custom Comparer.

---

### INV-STORE-004: Thread-Sicherheits-Garantie

**Formale Beschreibung**:
```
? Threads t?, t?, ..., t?:
    Concurrent execution of {Add, Remove, Clear, Contains, Items}
    ? Sequential consistency guaranteed
```

**Natürlichsprachlich**:
Alle Operationen sind thread-sicher und garantieren sequentielle Konsistenz.

**Implementierung**:
- Einfaches `lock`-basiertes Locking
- Ein globales Lock-Objekt pro Store-Instanz
- Kein Reader-Writer-Lock (bewusste Design-Entscheidung)

**Lock-Strategie - Design-Entscheidung**:

**Gewählt**: Einfaches `lock`
```csharp
private readonly object _lock = new();
```

**Nicht gewählt**: `ReaderWriterLockSlim`

**Begründung**:
1. **Operationen sind kurz** (typisch <1ms)
   - Kein signifikanter Vorteil von Reader-Writer-Locks
   - Overhead von ReaderWriterLockSlim wäre höher

2. **Einfachere Fehlersuche**
   - Deadlocks sind einfacher zu diagnostizieren
   - Weniger Komplexität im Lock-Management

3. **Bessere Testbarkeit**
   - Einfachere Reproduktion von Concurrency-Problemen
   - Deterministischeres Verhalten

**Performance-Charakteristik**:
- Schreiboperationen: Blockieren Lesevorgänge (akzeptierter Trade-off)
- Lesevorgänge: Blockieren Schreibvorgänge
- Bei hoher Read-Last: Performance-Nachteil vs. ReaderWriterLockSlim (~10-20%)

**Wann ReaderWriterLockSlim erwägen**:
- Collections >100.000 Elemente
- Read:Write Ratio >100:1
- Messbare Performance-Probleme

---

## PersistentStoreDecorator

### INV-PERSIST-001: Initialisierungs-Idempotenz

**Formale Beschreibung**:
```
? PersistentStoreDecorator d:
    d.InitializeAsync()
    d.InitializeAsync()
    ...
    d.InitializeAsync()
    ? Data loaded exactly once
```

**Natürlichsprachlich**:
Mehrfache Aufrufe von `InitializeAsync()` laden die Daten nur beim ersten Aufruf.

**Implementierung**:
```csharp
private bool _isInitialized;
private readonly SemaphoreSlim _initSemaphore = new(1, 1);

public async Task InitializeAsync(CancellationToken ct = default)
{
    await _initSemaphore.WaitAsync(ct);
    try
    {
        if (_isInitialized) // Schutz vor mehrfachem Laden
            return;

        var items = await _strategy.LoadAllAsync(ct);
        _innerStore.AddRange(items);
        _isInitialized = true;
    }
    finally
    {
        _initSemaphore.Release();
    }
}
```

**Thread-Sicherheit**:
Auch bei gleichzeitigen Aufrufen von mehreren Threads wird nur einmal geladen.

**Konsequenz**:
Sicher für mehrfache Bootstrap-Aufrufe (z.B. in Test-Szenarien).

---

### INV-PERSIST-002: Auto-Save-Semantik

**Formale Beschreibung**:
```
IF autoSaveOnChange == true THEN
    ? Operation op ? {Add, AddRange, Remove, Clear}:
        op executes successfully
        ? SaveAllAsync() is triggered asynchronously
```

**Natürlichsprachlich**:
Wenn Auto-Save aktiviert ist, wird bei jeder erfolgreichen Änderung automatisch ein Speichervorgang ausgelöst.

**Wichtige Verhaltensregel - Fire-and-Forget**:

Das Speichern erfolgt im Fire-and-Forget-Muster:
```csharp
private async void OnInnerStoreChanged(...)
{
    // async void = Fire-and-Forget
    // Fehler werden NICHT propagiert
}
```

**Konsequenzen**:
1. **? Nicht-blockierend**: Änderungen werden nicht verzögert
2. **? Responsive**: UI bleibt reaktionsfähig
3. **? Kein Fehler-Feedback**: Anwendung erfährt nicht von Speicherfehlern
4. **? Datenverlust möglich**: Bei Speicherfehler sind Daten nur im Speicher

**Design-Entscheidung - Fehlerbehandlung**:

**Gewählt**: Silent Failure
```csharp
catch (Exception)
{
    // Fehler werden geschluckt
}
```

**Begründung**:
- Anwendung soll bei Speicherproblemen NICHT abstürzen
- Datenverlust ist akzeptabler als Anwendungsabsturz
- In-Memory-Daten bleiben verfügbar

**Anforderung an Produktivcode**:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Fehler beim Speichern von {Type}", typeof(T).Name);
    // Optional: Retry-Logik
    // Optional: Fallback auf temporären Speicher
}
```

**Alternative Design-Option** (nicht implementiert):
```csharp
// Könnte implementiert werden:
public event EventHandler<SaveErrorEventArgs>? SaveError;

private async void OnInnerStoreChanged(...)
{
    try { ... }
    catch (Exception ex)
    {
        SaveError?.Invoke(this, new SaveErrorEventArgs(ex));
    }
}
```

---

### INV-PERSIST-003: Race-Condition-Schutz

**Formale Beschreibung**:
```
? Concurrent changes c?, c?, ..., c?:
    Save operations are serialized via SemaphoreSlim
    ? Last complete save contains all changes up to that point
```

**Natürlichsprachlich**:
Bei mehreren gleichzeitigen Änderungen werden Speichervorgänge serialisiert, sodass keine Änderungen verloren gehen.

**Szenario ohne Schutz**:
```
Thread 1: Add(item1) ? Save starts
Thread 2: Add(item2) ? Save starts
Thread 1: Save completes (writes item1)
Thread 2: Save completes (writes item1, item2) ?
```

**Mit Semaphore**:
```csharp
private readonly SemaphoreSlim _saveSemaphore = new(1, 1);

private async void OnInnerStoreChanged(...)
{
    await _saveSemaphore.WaitAsync(); // Serialisierung
    try
    {
        await _strategy.SaveAllAsync(_innerStore.Items);
    }
    finally
    {
        _saveSemaphore.Release();
    }
}
```

**Garantie**:
Speichervorgänge überschreiben sich nicht gegenseitig.

**Performance-Implikation**:
Bei sehr häufigen Änderungen (>100/sec) können sich Speichervorgänge aufstauen.

---

## ParentChildRelationship

### INV-RELATION-001: Filter-Konsistenz

**Formale Beschreibung**:
```
? ParentChildRelationship r:
    r.Refresh() applies r.Filter(r.Parent, child) to all children in r.DataSource
    ? r.Childs contains exactly those children where Filter returns true
```

**Natürlichsprachlich**:
Nach `Refresh()` enthält `Childs` genau die Elemente aus `DataSource`, für die `Filter(Parent, child)` true zurückgibt.

**Wichtige Verhaltensregel**:
```csharp
relationship.Refresh();
// Nach Refresh gilt:
Assert.True(relationship.Childs.Items.All(child => 
    relationship.Filter(relationship.Parent, child)));
```

**Keine automatische Aktualisierung**:
```csharp
globalStore.Add(newProduct); // Gehört zu Category
// relationship.Childs enthält newProduct NICHT automatisch!
relationship.Refresh(); // Manueller Refresh erforderlich
// Jetzt enthält relationship.Childs das neue Product
```

---

### INV-RELATION-002: DataSource-Requirement

**Formale Beschreibung**:
```
? ParentChildRelationship r:
    r.Refresh() requires r.DataSource ? null
    r.DataSource == null ? InvalidOperationException
```

**Natürlichsprachlich**:
`Refresh()` kann nur aufgerufen werden, wenn zuvor `DataSource` gesetzt wurde.

**Lebenszyklus**:
```csharp
// 1. Erstellung
var rel = new ParentChildRelationship<Category, Product>(...);
// rel.DataSource == null

// 2. DataSource setzen (ERFORDERLICH)
rel.UseGlobalDataSource();
// oder
rel.UseSnapshotFromGlobal();

// 3. Refresh (JETZT möglich)
rel.Refresh();
```

**Durchsetzung**:
```csharp
public IDataStore<TChild> DataSource
{
    get => _dataSource ?? throw new InvalidOperationException(
        "DataSource has not been set. Call UseGlobalDataSource() or UseSnapshotFromGlobal() first.");
}
```

---

### INV-RELATION-003: Childs-Isolation

**Formale Beschreibung**:
```
? ParentChildRelationship r:
    r.Childs is independent InMemoryDataStore
    Changes to r.Childs do NOT affect r.DataSource
    Changes to r.DataSource do NOT affect r.Childs (until Refresh)
```

**Natürlichsprachlich**:
Die `Childs`-Collection ist ein separater lokaler Store. Änderungen propagieren nicht automatisch zwischen `Childs` und `DataSource`.

**Beispiel**:
```csharp
relationship.UseGlobalDataSource();
relationship.Refresh();
// Childs enthält jetzt gefilterte Produkte

// Szenario 1: Änderung an Childs
relationship.Childs.Remove(product1);
// ? Globaler Store UNVERÄNDERT

// Szenario 2: Änderung an globalem Store
globalStore.Add(newProduct);
// ? relationship.Childs UNVERÄNDERT (bis Refresh)

relationship.Refresh();
// ? Jetzt sind beide synchronisiert
```

**Konsequenz**:
`Childs` ist ein Arbeits-Store, kein Mirror des globalen Stores.

---

## Thread-Sicherheits-Garantien

### Übersicht Thread-Sicherheit

| Klasse | Thread-sicher? | Mechanismus | Einschränkungen |
|--------|----------------|-------------|-----------------|
| `InMemoryDataStore<T>` | ? Ja | `lock` | Keine |
| `GlobalStoreRegistry` | ? Ja | `ConcurrentDictionary` | Keine |
| `DataStoresFacade` | ? Ja | Delegiert an thread-sichere Komponenten | Keine |
| `PersistentStoreDecorator<T>` | ? Ja | `SemaphoreSlim` + innerer Store | Save-Operationen serialisiert |
| `ParentChildRelationship<T,U>` | ? Teilweise | Childs ist thread-sicher, Refresh nicht | Refresh muss extern synchronisiert werden |
| `LocalDataStoreFactory` | ? Ja | Stateless | Keine |

### Thread-Sicherheits-Level

**Level 1 - Vollständig Thread-sicher**:
- Alle Operationen können von beliebig vielen Threads gleichzeitig aufgerufen werden
- Keine externe Synchronisation erforderlich
- Beispiel: `InMemoryDataStore<T>`, `GlobalStoreRegistry`

**Level 2 - Bedingt Thread-sicher**:
- Grundoperationen sind thread-sicher
- Komplexe Operationen erfordern externe Synchronisation
- Beispiel: `ParentChildRelationship` (Childs thread-sicher, aber Refresh + gleichzeitiger Zugriff auf Childs problematisch)

**Level 3 - Nicht Thread-sicher**:
- Externe Synchronisation erforderlich
- Keine Klassen in DataStores fallen in diese Kategorie

---

## Lebenszyklus-Regeln

### Lebenszyklus: Globale Stores

**Phase 1: Registrierung** (Bootstrap)
```csharp
services.AddDataStoreRegistrar<MyRegistrar>();

public class MyRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider sp)
    {
        registry.RegisterGlobal(new InMemoryDataStore<Product>());
    }
}
```

**Phase 2: Initialisierung** (Bootstrap)
```csharp
await DataStoreBootstrap.RunAsync(serviceProvider);
// Alle IAsyncInitializable werden initialisiert
// (z.B. PersistentStoreDecorator lädt Daten)
```

**Phase 3: Verwendung** (Application Lifetime)
```csharp
var stores = serviceProvider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();
// Arbeiten mit dem Store
```

**Phase 4: Beendigung** (Application Shutdown)
```csharp
// PersistentStoreDecorator sollte disposed werden
if (decorator is IDisposable disposable)
    disposable.Dispose();
```

**Invariante**:
```
Registrierung ? Initialisierung ? Verwendung ? Beendigung
     ?              ?                  ?
   Einmalig      Einmalig         Mehrfach       Optional
```

---

### Lebenszyklus: Lokale Stores

**Erstellung**:
```csharp
var localStore = stores.CreateLocal<Product>();
// Neuer, isolierter Store
```

**Verwendung**:
```csharp
localStore.Add(...);
localStore.Remove(...);
// Unabhängig von globalen Stores
```

**Beendigung**:
```csharp
// Keine explizite Cleanup erforderlich
// Garbage Collector räumt auf
```

**Invariante**:
Lokale Stores haben keine Registrierung oder Initialisierung. Sie existieren nur während ihrer Verwendung.

---

## Verhaltensregeln für Fehlerbehandlung

### Regel 1: Global Store Not Found

**Situation**:
```csharp
var store = stores.GetGlobal<UnregisteredType>();
```

**Garantiertes Verhalten**:
- `GlobalStoreNotRegisteredException` wird geworfen
- Niemals `null` zurückgegeben
- Exception enthält den Type-Namen

**Empfohlene Behandlung**:
```csharp
try
{
    var store = stores.GetGlobal<Product>();
}
catch (GlobalStoreNotRegisteredException ex)
{
    // Log und Fail-Fast (nicht recoverable)
    _logger.LogCritical(ex, "Product store not registered");
    throw; // Propagieren, da kritischer Konfigurationsfehler
}
```

---

### Regel 2: Persistence Failures

**Auto-Save Fehler**:
```csharp
// Werden IMMER geschluckt (Fire-and-Forget)
// Keine Exception propagiert an Caller
```

**Load-Fehler**:
```csharp
// InitializeAsync() propagiert Exceptions
try
{
    await decorator.InitializeAsync();
}
catch (IOException ex)
{
    // Datei nicht gefunden / nicht lesbar
}
catch (JsonException ex)
{
    // Korrupte Daten
}
```

**Empfohlene Strategie**:
```csharp
try
{
    await decorator.InitializeAsync();
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Could not load data, starting with empty store");
    // Weiter mit leerem Store
}
```

---

## Zusammenfassung: Kritische Invarianten

Die **5 kritischsten Invarianten**, die NIEMALS verletzt werden dürfen:

1. **INV-GLOBAL-001**: Ein Type = Ein globaler Store (Eindeutigkeit)
2. **INV-GLOBAL-002**: Gleiche Store-Referenz bei wiederholten Aufrufen (Stabilität)
3. **INV-STORE-001**: Items liefert immer neue Snapshot-Liste (Isolation)
4. **INV-STORE-004**: Alle Store-Operationen sind thread-sicher (Korrektheit)
5. **INV-PERSIST-001**: InitializeAsync ist idempotent (Verlässlichkeit)

**Validierung in Tests**:
Alle diese Invarianten werden durch dedizierte Tests abgedeckt.

---

**Version**: 1.0.0  
**Letzte Aktualisierung**: Januar 2025  
**Siehe auch**: [API-Referenz](API-Reference.md), [Usage-Examples](Usage-Examples.md)
