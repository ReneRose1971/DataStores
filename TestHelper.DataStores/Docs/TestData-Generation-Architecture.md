# Testdaten-Erzeugung in TestHelper.DataStores

**Version:** 1.0.0  
**Datum:** Januar 2025  
**Zielgruppe:** Entwickler, die Tests für DataStores-basierte Anwendungen schreiben

---

## 📌 Motivation

### Warum Testdaten-Factories?

In Unit- und Integrationstests müssen häufig größere Mengen an Testdaten erzeugt werden:

- **Performance-Tests**: 1000+ Entities für Stress-Tests
- **Paging-Szenarien**: Mehrere Seiten mit realistischen Daten
- **Filter-/Query-Tests**: Diverse Datensätze für LINQ-Operationen
- **Konkurrenz-Tests**: Viele parallele Operationen auf unterschiedlichen Daten

**Probleme manueller Testdaten:**
- ❌ Boilerplate-Code in jedem Test
- ❌ Inkonsistente Testdaten über Tests hinweg
- ❌ Schwer wartbar bei Entitätsänderungen
- ❌ Zeitaufwändig für große Datenmengen

**Vorteile automatisierter Erzeugung:**
- ✅ Deterministisch (gleiche Seeds = gleiche Daten)
- ✅ Weniger Code in Tests
- ✅ Konsistente Daten
- ✅ Schnell skalierbar (10 oder 10.000 Entities)

---

## 🎯 Abgrenzung zu Produktivcode

### Warum nur im TestHelper?

**Produktivcode (DataStores-Projekt):**
- ✅ Speichert und verwaltet beliebige Entities
- ✅ Persistiert Daten
- ✅ Stellt Thread-Sicherheit sicher
- ❌ **DARF NICHT** Testdaten generieren
- ❌ **DARF NICHT** von Zufallsdaten-Bibliotheken abhängen
- ❌ **DARF NICHT** Test-Utilities kennen

**TestHelper.DataStores:**
- ✅ Stellt Fakes, Mocks, Builder bereit
- ✅ Generiert Testdaten deterministisch
- ✅ Nutzt ObjectFiller für komplexe Entities
- ✅ Bietet Fixtures für Integrationstests
- ❌ **WIRD NIEMALS** in Produktivcode referenziert

**Klare Trennung:**
```
DataStores (Produktiv)
    ↓ Referenz
TestHelper.DataStores (Test-Utilities)
    ↓ Referenz
DataStores.Tests (Tests)
```

---

## 🧱 Architekturübersicht

### Komponenten

```
┌─────────────────────────────────────────────────────────────┐
│                  Test (Arrange-Phase)                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              ITestDataFactory<T>                            │
│  - CreateSingle() : T                                       │
│  - CreateMany(count) : IEnumerable<T>                       │
└──────────────────────┬──────────────────────────────────────┘
                       │
         ┌─────────────┴─────────────┐
         │                           │
         ▼                           ▼
┌──────────────────┐      ┌──────────────────────┐
│ ObjectFiller-    │      │ Custom Factories     │
│ TestDataFactory  │      │ (manuell/speziell)   │
└──────────────────┘      └──────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│              DataStoreBuilder<T>                            │
│  - WithItems(...)                                           │
│  - WithGeneratedItems(factory, count)                       │
│  - Build() → IDataStore<T>                                  │
└─────────────────────────────────────────────────────────────┘
```

### Verantwortlichkeiten

#### `ITestDataFactory<T>`
**Rolle:** Abstraktion für Testdaten-Erzeugung

- Kennt keine DataStores
- Kennt keine Persistenz
- Erzeugt reine POCOs
- Deterministisch (Seed-basiert)

#### `ObjectFillerTestDataFactory<T>`
**Rolle:** Konkrete Implementierung mit ObjectFiller.NET

- Kapselt `Filler<T>` von Tynamix.ObjectFiller
- Unterstützt Seed-Handling
- Erlaubt optionales Setup (Type/Property-Konfiguration)
- Kein statischer Zustand
- Thread-safe (jede Factory-Instanz isoliert)

#### `DataStoreBuilder<T>`
**Rolle:** Fluent Builder für Test-Stores (erweitert)

**Bestehende API (unverändert):**
- `WithItems(params T[] items)` - Manuelle Items
- `WithComparer(...)` - Equality-Comparer
- `WithSyncContext(...)` - UI-Thread-Support
- `WithChangedHandler(...)` - Event-Handler
- `Build()` - Store erstellen

**Neue API (optional):**
- `WithGeneratedItems(ITestDataFactory<T> factory, int count)` - Generierte Items

---

## 🔌 Rolle von ObjectFiller

### Was ist ObjectFiller?

**Tynamix.ObjectFiller** ist eine .NET-Bibliothek zur automatischen Befüllung von Objekten mit Zufallsdaten.

**Features:**
- Automatische Typenerkennung (int, string, DateTime, etc.)
- Konfigurierbare Bereiche (z.B. Alter zwischen 18-65)
- Ignore-Properties
- Seed-basierte Reproduzierbarkeit
- Komplexe Objekt-Graphen

**Beispiel (direkt):**
```csharp
var filler = new Filler<Person>();
filler.Setup()
    .OnProperty(x => x.Age).Use(() => Random.Shared.Next(18, 65))
    .OnProperty(x => x.Id).IgnoreIt();

var person = filler.Create(); // Zufällige Person
```

### Warum nur im TestHelper?

**Argumente:**
1. **Produktivcode-Klarheit**: DataStores hat keine Testdaten-Verantwortung
2. **Dependency-Isolation**: ObjectFiller ist eine Test-Utility, keine Runtime-Dependency
3. **Austauschbarkeit**: Die Abstraktion `ITestDataFactory<T>` erlaubt später andere Implementierungen
4. **Klare Grenzen**: Tests wissen, dass sie mit generierten Daten arbeiten

**Zukünftige Erweiterbarkeit:**
Die Architektur erlaubt später ein Zusatzprojekt:
```
TestHelper.DataStores.ObjectFiller.csproj
    - ObjectFillerTestDataFactory<T>
    - ObjectFiller-spezifische Extensions
```

Aktuell ist ObjectFiller direkt in TestHelper.DataStores integriert, da:
- Es bereits als Dependency vorhanden ist
- Die Integration minimal ist
- Kein Over-Engineering für den aktuellen Use-Case

---

## ✅ Wann ObjectFiller geeignet ist

### Ideal für:

1. **Performance-Tests**
   - 1000+ Entities generieren
   - Stress-Tests für DataStore-Operationen
   - Bulk-Insert-Szenarien

2. **Paging- und Query-Tests**
   - Diverse Datensätze für LINQ
   - Filter-/Sortier-Szenarien
   - Snapshot-Tests mit vielen Items

3. **Property-Coverage-Tests**
   - Alle Properties befüllt
   - Edge-Cases (null, leere Strings, etc.)
   - Verschiedene Datentypen

4. **Konkurrenz-Tests**
   - Viele unterschiedliche Entities
   - Parallele Operationen
   - Race-Condition-Szenarien

### Beispiel (geeignet):
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

---

## ❌ Wann ObjectFiller NICHT geeignet ist

### Ungeeignet für:

1. **Fachliche Konsistenz**
   - Geschäftsregeln müssen manuell validiert werden
   - Beispiel: `OrderDate` muss vor `ShipDate` liegen
   - ObjectFiller erzeugt zufällige Werte ohne fachliche Logik

2. **Relationen und Foreign Keys**
   - Parent-Child-Beziehungen
   - Referenzielle Integrität
   - Beispiel: `Order.CustomerId` muss in `Customer.Id` existieren

3. **Komplexe Invarianten**
   - Berechnete Properties
   - Status-Abhängigkeiten
   - Beispiel: `TotalPrice` = Summe aller `LineItem.Price`

4. **Domain-spezifische Werte**
   - Gültige Email-Adressen
   - Reale Ländercodes
   - Valide IBANs oder Telefonnummern

### Beispiel (NICHT geeignet):
```csharp
// ❌ SCHLECHT: Fachliche Logik fehlt
var factory = new ObjectFillerTestDataFactory<Order>(seed: 42);
var order = factory.CreateSingle();
// Problem: order.ShipDate könnte VOR order.OrderDate liegen!

// ✅ BESSER: Manuelle Erstellung mit fachlicher Logik
var order = new Order
{
    OrderDate = DateTime.UtcNow,
    ShipDate = DateTime.UtcNow.AddDays(3), // Logisch konsistent
    CustomerId = existingCustomer.Id       // FK-Integrität
};
```

### Lösung: Kombinieren

Für komplexe Szenarien: ObjectFiller für Basis-Properties, manuelle Nachbearbeitung für Logik.

```csharp
var factory = new ObjectFillerTestDataFactory<Order>(seed: 42);
var orders = factory.CreateMany(100).ToList();

// Nachbearbeitung für fachliche Konsistenz
foreach (var order in orders)
{
    order.ShipDate = order.OrderDate.AddDays(Random.Shared.Next(1, 7));
    order.CustomerId = validCustomerIds[Random.Shared.Next(validCustomerIds.Count)];
}
```

---

## 📚 Verwendungsbeispiele

### Beispiel 1: Unit-Test mit InMemoryDataStore

```csharp
[Fact]
public void AddRange_Should_AddAllGeneratedItems()
{
    // Arrange
    var factory = new ObjectFillerTestDataFactory<TestEntity>(seed: 123);
    var store = new DataStoreBuilder<TestEntity>()
        .WithGeneratedItems(factory, count: 50)
        .Build();

    // Act
    var count = store.Items.Count;

    // Assert
    Assert.Equal(50, count);
}
```

### Beispiel 2: Integrationstest mit LiteDB-Fixture

```csharp
public class OrderPersistenceTests : IAsyncLifetime
{
    private LiteDbIntegrationFixture<Order> _fixture = null!;
    private IDataStore<Order> _orderStore = null!;

    public async Task InitializeAsync()
    {
        _fixture = new LiteDbIntegrationFixture<Order>(
            collectionName: "orders",
            new OrderRegistrar());
        
        await _fixture.InitializeAsync();
        _orderStore = _fixture.DataStores.GetGlobal<Order>();
    }

    [Fact]
    public async Task BulkOrders_Should_BePersisted()
    {
        // Arrange
        var factory = new ObjectFillerTestDataFactory<Order>(seed: 999);
        var orders = factory.CreateMany(200).ToList();
        
        // Act
        _orderStore.AddRange(orders);
        await Task.Delay(300); // Auto-Save

        // Assert
        Assert.Equal(200, _orderStore.Items.Count);
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();
}
```

### Beispiel 3: Kombiniert mit manuellen Items

```csharp
[Fact]
public void Store_Should_ContainBothManualAndGeneratedItems()
{
    // Arrange
    var factory = new ObjectFillerTestDataFactory<Product>(seed: 42);
    var manualProduct = new Product { Name = "Special Item" };
    
    var store = new DataStoreBuilder<Product>()
        .WithItems(manualProduct)
        .WithGeneratedItems(factory, count: 10)
        .Build();

    // Act
    var hasManual = store.Items.Contains(manualProduct);

    // Assert
    Assert.True(hasManual);
}
```

---

## 🔧 Konfiguration und Setup

### Deterministisches Verhalten (Seed)

```csharp
// Gleicher Seed = Gleiche Daten
var factory1 = new ObjectFillerTestDataFactory<Person>(seed: 42);
var factory2 = new ObjectFillerTestDataFactory<Person>(seed: 42);

var person1 = factory1.CreateSingle();
var person2 = factory2.CreateSingle();

// person1 und person2 haben identische Werte!
```

### Custom Setup (Property-Konfiguration)

```csharp
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
// Alle Employees: Age 18-65, Salary 30k-120k, Id = 0
```

---

## 📖 API-Referenz

### ITestDataFactory&lt;T&gt;

```csharp
public interface ITestDataFactory<T> where T : class
{
    T CreateSingle();
    IEnumerable<T> CreateMany(int count);
}
```

### ObjectFillerTestDataFactory&lt;T&gt;

```csharp
public sealed class ObjectFillerTestDataFactory<T> : ITestDataFactory<T> 
    where T : class, new()
{
    public ObjectFillerTestDataFactory(int? seed = null);
    public ObjectFillerTestDataFactory(int? seed, Action<Filler<T>> setupAction);
    
    public T CreateSingle();
    public IEnumerable<T> CreateMany(int count);
}
```

### DataStoreBuilder&lt;T&gt; (erweitert)

```csharp
public class DataStoreBuilder<T> where T : class
{
    // Bestehende API (unverändert)
    public DataStoreBuilder<T> WithItems(params T[] items);
    public DataStoreBuilder<T> WithComparer(IEqualityComparer<T> comparer);
    public DataStoreBuilder<T> WithSyncContext(SynchronizationContext ctx);
    public DataStoreBuilder<T> WithChangedHandler(EventHandler<...> handler);
    
    // Neue API (optional)
    public DataStoreBuilder<T> WithGeneratedItems(
        ITestDataFactory<T> factory, 
        int count);
    
    public IDataStore<T> Build();
}
```

---

## ⚠️ Wichtige Hinweise

### Thread-Sicherheit

- Jede `ObjectFillerTestDataFactory<T>`-Instanz ist isoliert
- Kein statischer Zustand
- Parallel nutzbar in verschiedenen Tests
- Seed-Handling ist thread-safe

### Performance

- Generierung ist schnell (< 1ms pro Entity für einfache Typen)
- Für komplexe Objekt-Graphen: ~5-10ms pro Entity
- Bulk-Generierung (1000+) in < 1 Sekunde

### Best Practices

1. **Seed verwenden**: Für deterministische Tests immer Seed angeben
2. **Factory pro Test**: Nicht zwischen Tests teilen (Isolation)
3. **Setup minimal halten**: Nur notwendige Property-Konfiguration
4. **Nachbearbeitung für Logik**: Fachliche Regeln manuell sicherstellen

---

**Letzte Aktualisierung:** Januar 2025  
**Version:** 1.0.0  
**Autor:** DataStores Team
