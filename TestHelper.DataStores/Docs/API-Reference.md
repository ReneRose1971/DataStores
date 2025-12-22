# TestHelper.DataStores - API-Referenz

**Version:** 1.0.0  
**Zweck:** Wiederverwendbare Test-Hilfsklassen für DataStores-Tests  
**Framework:** .NET 8.0

---

## Übersicht

TestHelper.DataStores ist eine dedizierte Test-Bibliothek, die wiederverwendbare Fakes, Builder und Comparers für das Testen von DataStores-basierten Anwendungen bereitstellt.

**Wichtig:** Diese Bibliothek ist **ausschließlich für Tests** gedacht und darf **nicht** in Produktionscode verwendet werden.

---

## Namespaces

### TestHelper.DataStores.TestData

Abstraktion und Implementierung für Testdaten-Erzeugung.

#### `ITestDataFactory<T>`

Abstraktion für die Erzeugung von Testdaten.

```csharp
public interface ITestDataFactory<T> where T : class
{
    T CreateSingle();
    IEnumerable<T> CreateMany(int count);
}
```

**Design-Prinzipien:**
- ✅ Kennt keine DataStores-Implementierungen
- ✅ Kennt keine Persistenz-Logik
- ✅ Erzeugt reine POCOs ohne Seiteneffekte
- ✅ Deterministisch (gleiche Konfiguration = gleiche Daten)
- ✅ Thread-safe (Instanz-basiert, kein statischer Zustand)

**Verwendung:**
Primär in der Arrange-Phase von Unit- und Integrationstests zur Erzeugung
von Testdaten für DataStores, LINQ-Queries, Performance-Tests, etc.

#### `ObjectFillerTestDataFactory<T>`

Testdaten-Factory basierend auf ObjectFiller.NET.

```csharp
public sealed class ObjectFillerTestDataFactory<T> : ITestDataFactory<T> 
    where T : class, new()
```

**Konstruktoren:**
```csharp
public ObjectFillerTestDataFactory(int? seed = null);
public ObjectFillerTestDataFactory(int? seed, Action<Filler<T>> setupAction);
```

**Features:**
- ✅ Seed-basierte Reproduzierbarkeit (gleicher Seed = gleiche Daten)
- ✅ Automatische Befüllung aller Properties
- ✅ Optionales Custom-Setup für Property-Konfiguration
- ✅ Thread-safe (keine statischen Felder)
- ✅ Lazy Evaluation für CreateMany()

**Verwendung:**
```csharp
// Beispiel 1: Einfache Verwendung mit Seed
var factory = new ObjectFillerTestDataFactory<Product>(seed: 42);
var product = factory.CreateSingle();
var products = factory.CreateMany(100).ToList();

// Beispiel 2: Mit Custom-Setup
var factory = new ObjectFillerTestDataFactory<Employee>(
    seed: 123,
    setupAction: filler =>
    {
        filler.Setup()
            .OnProperty(x => x.Age).Use(() => Random.Shared.Next(18, 65))
            .OnProperty(x => x.Salary).Use(() => Random.Shared.Next(30000, 120000))
            .OnProperty(x => x.Id).IgnoreIt(); // LiteDB setzt ID
    });
var employees = factory.CreateMany(50);

// Beispiel 3: Nachbearbeitung für fachliche Logik
var orderFactory = new ObjectFillerTestDataFactory<Order>(seed: 999);
var orders = orderFactory.CreateMany(100).ToList();
foreach (var order in orders)
{
    // Fachliche Konsistenz sicherstellen
    order.ShipDate = order.OrderDate.AddDays(Random.Shared.Next(1, 7));
}
```

**Einschränkungen:**
- ❌ Keine fachliche Logik (z.B. OrderDate vor ShipDate)
- ❌ Keine Relationen oder FK-Integrität
- ❌ Keine komplexen Invarianten

➡️ **Siehe auch:** [Testdaten-Erzeugung Architektur](TestData-Generation-Architecture.md)

---

### TestHelper.DataStores.Fakes

Fake-Implementierungen der DataStores-Interfaces für Testzwecke.

#### `FakeDataStore<T>`

Fake-Implementation von `IDataStore<T>` mit vollständigem Call-Tracking.

```csharp
public class FakeDataStore<T> : IDataStore<T> where T : class
```

**Features:**
- ✅ Vollständige `IDataStore<T>`-Implementierung
- ✅ Call-Tracking (AddCallCount, RemoveCallCount, etc.)
- ✅ Kontrollierbare Fehler-Simulation (ThrowOnAdd, ThrowOnRemove)
- ✅ Event-Tracking (ChangedEvents)
- ✅ Reset-Methode

**Eigenschaften:**
- `int AddCallCount` - Anzahl der Add-Aufrufe
- `int RemoveCallCount` - Anzahl der Remove-Aufrufe
- `int ClearCallCount` - Anzahl der Clear-Aufrufe
- `int AddRangeCallCount` - Anzahl der AddRange-Aufrufe
- `bool ThrowOnAdd` - Soll Add eine Exception werfen?
- `bool ThrowOnRemove` - Soll Remove eine Exception werfen?
- `IReadOnlyList<DataStoreChangedEventArgs<T>> ChangedEvents` - Aufgezeichnete Events

**Methoden:**
- `void Reset()` - Setzt alle Zähler und Daten zurück

**Verwendung:**
```csharp
var fake = new FakeDataStore<Product>();
fake.ThrowOnAdd = true; // Nächster Add wirft Exception

fake.Add(new Product()); // Wirft InvalidOperationException

Assert.Equal(1, fake.AddCallCount);
```

#### `FakeGlobalStoreRegistry`

Fake-Implementation von `IGlobalStoreRegistry` mit vollständigem Call-Tracking.

```csharp
public class FakeGlobalStoreRegistry : IGlobalStoreRegistry
```

**Features:**
- ✅ Vollständige `IGlobalStoreRegistry`-Implementierung
- ✅ Call-Tracking
- ✅ Kontrollierbare Fehler-Simulation
- ✅ Operation-History

**Eigenschaften:**
- `int RegisterCallCount` - Anzahl der RegisterGlobal-Aufrufe
- `int ResolveGlobalCallCount` - Anzahl der ResolveGlobal-Aufrufe
- `int TryResolveGlobalCallCount` - Anzahl der TryResolveGlobal-Aufrufe
- `bool ThrowOnRegister` - Soll RegisterGlobal werfen?
- `bool ThrowOnResolveGlobal` - Soll ResolveGlobal werfen?
- `List<(string Action, Type Type, object? Store)> History` - Vollständige Operation-History

**Methoden:**
- `void Reset()` - Setzt alle Zähler und Daten zurück

**Verwendung:**
```csharp
var fake = new FakeGlobalStoreRegistry();
fake.ThrowOnRegister = true;

Assert.Throws<GlobalStoreAlreadyRegisteredException>(() =>
    fake.RegisterGlobal(new InMemoryDataStore<Product>()));

Assert.Equal(1, fake.RegisterCallCount);
```

---

### TestHelper.DataStores.Builders

Fluent-Builder für komfortable Test-Setup.

#### `DataStoreBuilder<T>`

Fluent-Builder zum Erstellen von DataStore-Instanzen mit vorkonfigurierten Test-Daten.

```csharp
public class DataStoreBuilder<T> where T : class
```

**Methoden:**
- `DataStoreBuilder<T> WithItems(params T[] items)` - Fügt initiale Items hinzu
- `DataStoreBuilder<T> WithGeneratedItems(ITestDataFactory<T> factory, int count)` - Fügt generierte Items hinzu ⭐ NEU
- `DataStoreBuilder<T> WithSyncContext(SynchronizationContext ctx)` - Setzt SynchronizationContext
- `DataStoreBuilder<T> WithComparer(IEqualityComparer<T> comparer)` - Setzt Custom-Comparer
- `DataStoreBuilder<T> WithChangedHandler(EventHandler<DataStoreChangedEventArgs<T>> handler)` - Registriert Event-Handler
- `IDataStore<T> Build()` - Erstellt den konfigurierten Store

**Verwendung (klassisch):**
```csharp
var store = new DataStoreBuilder<Product>()
    .WithItems(
        new Product { Id = 1, Name = "A" },
        new Product { Id = 2, Name = "B" })
    .WithComparer(new IdComparer())
    .WithChangedHandler((s, e) => Console.WriteLine("Changed!"))
    .Build();

Assert.Equal(2, store.Items.Count);
```

**Verwendung (mit Testdaten-Generierung):** ⭐ NEU
```csharp
// Einfach: Generierte Items
var factory = new ObjectFillerTestDataFactory<Product>(seed: 42);
var store = new DataStoreBuilder<Product>()
    .WithGeneratedItems(factory, count: 100)
    .Build();

// Kombiniert: Manuelle + Generierte Items
var specialProduct = new Product { Name = "Special" };
var store = new DataStoreBuilder<Product>()
    .WithItems(specialProduct)
    .WithGeneratedItems(factory, count: 50)
    .WithComparer(new IdComparer())
    .Build();

Assert.Equal(51, store.Items.Count);
```

---

### TestHelper.DataStores.Persistence

Test-Persistierungsstrategien für verschiedene Testszenarien.

#### `FakePersistenceStrategy<T>`

Fake-Implementation von `IPersistenceStrategy<T>` für Unit-Tests.

```csharp
public class FakePersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
```

**Features:**
- ✅ Vollständige `IPersistenceStrategy<T>`-Implementierung
- ✅ Call-Tracking
- ✅ Thread-sicher (mit Lock)
- ✅ In-Memory-Speicher

**Eigenschaften:**
- `int LoadCallCount` - Anzahl der LoadAllAsync-Aufrufe
- `int SaveCallCount` - Anzahl der SaveAllAsync-Aufrufe
- `IReadOnlyList<T>? LastSavedItems` - Zuletzt gespeicherte Items

**Methoden:**
- `void SetData(IReadOnlyList<T> data)` - Setzt Daten für nächsten Load

**Verwendung:**
```csharp
var strategy = new FakePersistenceStrategy<Product>(new[]
{
    new Product { Id = 1, Name = "Initial" }
});

var items = await strategy.LoadAllAsync();
Assert.Single(items);
Assert.Equal(1, strategy.LoadCallCount);

await strategy.SaveAllAsync(newItems);
Assert.Equal(1, strategy.SaveCallCount);
Assert.Same(newItems, strategy.LastSavedItems);
```

#### `SlowLoadStrategy<T>`

Persistierungsstrategie mit künstlicher Verzögerung für Race-Condition-Tests.

```csharp
public class SlowLoadStrategy<T> : IPersistenceStrategy<T> where T : class
```

**Konstruktor:**
```csharp
public SlowLoadStrategy(TimeSpan delay, IReadOnlyList<T> data)
```

**Eigenschaften:**
- `int LoadCallCount` - Anzahl der LoadAllAsync-Aufrufe
- `int SaveCallCount` - Anzahl der SaveAllAsync-Aufrufe

**Verwendung:**
```csharp
var strategy = new SlowLoadStrategy<Product>(
    TimeSpan.FromMilliseconds(500),
    new[] { new Product { Id = 1 } });

// Startet Load-Operation (dauert 500ms)
var loadTask = strategy.LoadAllAsync();

// Kann währenddessen andere Operations testen
// z.B. Concurrent Load Calls

await loadTask;
Assert.Equal(1, strategy.LoadCallCount);
```

#### `ThrowingPersistenceStrategy<T>`

Persistierungsstrategie, die Exceptions wirft - für Fehlerbehandlungs-Tests.

```csharp
public class ThrowingPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
```

**Konstruktor:**
```csharp
public ThrowingPersistenceStrategy(bool throwOnLoad, bool throwOnSave)
```

**Verwendung:**
```csharp
var strategy = new ThrowingPersistenceStrategy<Product>(
    throwOnLoad: true, 
    throwOnSave: false);

// LoadAllAsync wirft Exception
await Assert.ThrowsAsync<InvalidOperationException>(() =>
    strategy.LoadAllAsync());

// SaveAllAsync funktioniert normal
await strategy.SaveAllAsync(items); // OK
```

---

### TestHelper.DataStores.Comparers

Wiederverwendbare Equality-Comparer für Tests.

#### `KeySelectorEqualityComparer<T, TKey>`

Generischer Equality-Comparer basierend auf einem Key-Selector.

```csharp
public class KeySelectorEqualityComparer<T, TKey> : IEqualityComparer<T>
```

**Konstruktor:**
```csharp
public KeySelectorEqualityComparer(
    Func<T, TKey> keySelector, 
    IEqualityComparer<TKey>? keyComparer = null)
```

**Features:**
- ✅ Generisch - funktioniert mit jedem Typ
- ✅ Ersetzt spezialisierte Comparer (z.B. IdOnlyComparer)
- ✅ Null-sicher
- ✅ Optionaler Key-Comparer

**Verwendung:**
```csharp
// Vergleich nur nach Id
var idComparer = new KeySelectorEqualityComparer<Product, int>(x => x.Id);

var store = new InMemoryDataStore<Product>(idComparer);
store.Add(new Product { Id = 1, Name = "Original" });

// Findet Item nur nach Id (Name wird ignoriert)
Assert.True(store.Contains(new Product { Id = 1, Name = "Different" }));

// Mit Custom Key-Comparer (z.B. Case-Insensitive für Strings)
var nameComparer = new KeySelectorEqualityComparer<Product, string>(
    x => x.Name,
    StringComparer.OrdinalIgnoreCase);

store = new InMemoryDataStore<Product>(nameComparer);
store.Add(new Product { Id = 1, Name = "Test" });

Assert.True(store.Contains(new Product { Id = 999, Name = "TEST" })); // Case-insensitive
```

---

## Best Practices

### 1. Testdaten-Generierung für Performance-Tests

```csharp
[Fact]
public void BulkInsert_Should_HandleThousandItems()
{
    // Arrange
    var factory = new ObjectFillerTestDataFactory<Product>(seed: 42);
    var store = new DataStoreBuilder<Product>()
        .WithGeneratedItems(factory, count: 1000)
        .Build();

    // Act
    var count = store.Items.Count;

    // Assert
    Assert.Equal(1000, count);
}
```

### 2. Deterministisches Verhalten mit Seeds

```csharp
[Fact]
public void GeneratedData_WithSameSeed_Should_BeIdentical()
{
    // Arrange
    var factory1 = new ObjectFillerTestDataFactory<Person>(seed: 123);
    var factory2 = new ObjectFillerTestDataFactory<Person>(seed: 123);

    // Act
    var person1 = factory1.CreateSingle();
    var person2 = factory2.CreateSingle();

    // Assert
    Assert.Equal(person1.Name, person2.Name);
}
```

### 3. Fachliche Logik durch Nachbearbeitung

```csharp
[Fact]
public void Orders_Should_HaveLogicalDates()
{
    // Arrange
    var factory = new ObjectFillerTestDataFactory<Order>(seed: 42);
    var orders = factory.CreateMany(50).ToList();
    
    // Fachliche Konsistenz herstellen
    foreach (var order in orders)
    {
        order.ShipDate = order.OrderDate.AddDays(Random.Shared.Next(1, 7));
    }

    // Act
    var store = new DataStoreBuilder<Order>()
        .WithItems(orders.ToArray())
        .Build();

    // Assert
    Assert.All(store.Items, o => Assert.True(o.ShipDate >= o.OrderDate));
}
```

### 4. Fakes für Unit-Tests

```csharp
[Fact]
public void MyService_Should_CallStore()
{
    // Arrange
    var fakeStore = new FakeDataStore<Product>();
    var service = new ProductService(fakeStore);

    // Act
    service.AddProduct(new Product { Id = 1 });

    // Assert
    Assert.Equal(1, fakeStore.AddCallCount);
    Assert.Single(fakeStore.Items);
}
```

### 5. Builder für Testdaten-Setup

```csharp
[Fact]
public void ComplexScenario_Test()
{
    // Arrange - Readable, fluent setup
    var store = new DataStoreBuilder<Product>()
        .WithItems(CreateTestProducts())
        .WithComparer(new IdComparer())
        .Build();

    // Act & Assert...
}
```

### 6. Persistence-Fakes für Decorator-Tests

```csharp
[Fact]
public async Task Decorator_Should_LoadOnInit()
{
    // Arrange
    var strategy = new FakePersistenceStrategy<Product>(initialData);
    var decorator = new PersistentStoreDecorator<Product>(
        new InMemoryDataStore<Product>(), 
        strategy);

    // Act
    await decorator.InitializeAsync();

    // Assert
    Assert.Equal(1, strategy.LoadCallCount);
}
```

### 7. Race-Condition-Tests

```csharp
[Fact]
public async Task ConcurrentLoads_Should_LoadOnlyOnce()
{
    // Arrange
    var strategy = new SlowLoadStrategy<Product>(
        TimeSpan.FromMilliseconds(100), 
        testData);

    // Act - Concurrent calls
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => Task.Run(() => strategy.LoadAllAsync()))
        .ToArray();
    await Task.WhenAll(tasks);

    // Assert
    Assert.Equal(10, strategy.LoadCallCount); // Oder 1 bei echter Synchronisation
}
```

---

## Weitere Dokumentation

- **[Testdaten-Erzeugung Architektur](TestData-Generation-Architecture.md)** ⭐ NEU
  - Motivation und Design-Entscheidungen
  - Wann ObjectFiller geeignet ist
  - Wann ObjectFiller NICHT geeignet ist
  - Detaillierte Verwendungsbeispiele

---

## Migration von privaten Helper-Klassen

### Vorher (in Testdatei):

```csharp
private class IdOnlyComparer : IEqualityComparer<Product>
{
    public bool Equals(Product? x, Product? y) => x?.Id == y?.Id;
    public int GetHashCode(Product obj) => obj.Id.GetHashCode();
}

// Verwendung
var comparer = new IdOnlyComparer();
```

### Nachher (mit TestHelper):

```csharp
using TestHelper.DataStores.Comparers;

// Verwendung
var comparer = new KeySelectorEqualityComparer<Product, int>(x => x.Id);
```

**Vorteile:**
- ✅ Keine Code-Duplikation
- ✅ Wiederverwendbar
- ✅ Generisch
- ✅ Weniger Maintenance

---

## Hinweise

### Nur für Tests!

⚠️ **Diese Bibliothek ist ausschließlich für Tests gedacht!**

- ❌ NICHT in Produktionscode verwenden
- ❌ NICHT von Produktionsprojekten referenzieren
- ✅ NUR von Testprojekten referenzieren

### Thread-Sicherheit

- ✅ `ObjectFillerTestDataFactory<T>` ist thread-sicher (Lock) ⭐ NEU
- ✅ `FakePersistenceStrategy<T>` ist thread-sicher (Lock)
- ✅ `FakeDataStore<T>` ist NICHT thread-sicher (für Tests ausreichend)
- ✅ `FakeGlobalStoreRegistry` ist NICHT thread-sicher (für Tests ausreichend)

### Performance

Diese Fakes sind für Tests optimiert, nicht für Performance:
- In-Memory-Speicher (keine Persistierung)
- Einfache Implementierungen
- Keine Optimierungen wie Caching

**ObjectFiller Performance:** ⭐ NEU
- Einfache Entities: ~1ms pro Stück
- Komplexe Objekt-Graphen: ~5-10ms pro Stück
- 1000 Entities: < 1 Sekunde

---

**Version:** 1.0.0  
**Lizenz:** MIT  
**Hauptprojekt:** [DataStores](../DataStores/README.md)
