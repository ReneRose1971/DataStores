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
- `DataStoreBuilder<T> WithSyncContext(SynchronizationContext ctx)` - Setzt SynchronizationContext
- `DataStoreBuilder<T> WithComparer(IEqualityComparer<T> comparer)` - Setzt Custom-Comparer
- `DataStoreBuilder<T> WithChangedHandler(EventHandler<DataStoreChangedEventArgs<T>> handler)` - Registriert Event-Handler
- `IDataStore<T> Build()` - Erstellt den konfigurierten Store

**Verwendung:**
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

### 1. Fakes für Unit-Tests

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

### 2. Builder für Testdaten-Setup

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

### 3. Persistence-Fakes für Decorator-Tests

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

### 4. Race-Condition-Tests

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

- ✅ `FakePersistenceStrategy<T>` ist thread-sicher (Lock)
- ✅ `FakeDataStore<T>` ist NICHT thread-sicher (für Tests ausreichend)
- ✅ `FakeGlobalStoreRegistry` ist NICHT thread-sicher (für Tests ausreichend)

### Performance

Diese Fakes sind für Tests optimiert, nicht für Performance:
- In-Memory-Speicher (keine Persistierung)
- Einfache Implementierungen
- Keine Optimierungen wie Caching

---

**Version:** 1.0.0  
**Lizenz:** MIT  
**Hauptprojekt:** [DataStores](../DataStores/README.md)
