# ObjectFiller-Integration - Architektur-Zusammenfassung

**Datum:** Januar 2025  
**Status:** ✅ Implementiert und getestet

---

## 🎯 Ziel

Saubere, optionale Integration von ObjectFiller.NET zur deterministischen Erzeugung von Testdaten in TestHelper.DataStores, ohne harte Kopplung an Produktivcode.

---

## 🏗️ Architektur

### Schichtenmodell

```
┌─────────────────────────────────────────────────────────┐
│                 DataStores (Produktiv)                  │
│  - InMemoryDataStore<T>                                 │
│  - PersistentStoreDecorator<T>                          │
│  - GlobalStoreRegistry                                  │
│  ❌ KEINE Abhängigkeit zu Testdaten-Bibliotheken        │
└─────────────────────────────────────────────────────────┘
                        ▲
                        │ Referenz
                        │
┌─────────────────────────────────────────────────────────┐
│          TestHelper.DataStores (Test-Utilities)         │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │ Abstraktion (ITestDataFactory<T>)                 │ │
│  │  - CreateSingle()                                 │ │
│  │  - CreateMany(count)                              │ │
│  └───────────────────────┬───────────────────────────┘ │
│                          │                             │
│            ┌─────────────┴──────────────┐              │
│            │                            │              │
│  ┌─────────▼──────────┐   ┌─────────────▼───────────┐ │
│  │ ObjectFiller-      │   │ Custom Factories        │ │
│  │ TestDataFactory    │   │ (manuell/speziell)      │ │
│  │                    │   │                         │ │
│  │ + Seed-Handling    │   │ + Fachliche Logik       │ │
│  │ + Auto-Populate    │   │ + FK-Integrität         │ │
│  │ + Thread-safe      │   │ + Invarianten           │ │
│  └────────────────────┘   └─────────────────────────┘ │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │ DataStoreBuilder<T> (erweitert)                   │ │
│  │  - WithItems(...)                (besteht)        │ │
│  │  - WithGeneratedItems(factory, count)  ⭐ NEU     │ │
│  │  - WithComparer(...)             (besteht)        │ │
│  │  - WithSyncContext(...)          (besteht)        │ │
│  │  - Build()                       (besteht)        │ │
│  └───────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                        ▲
                        │ Referenz
                        │
┌─────────────────────────────────────────────────────────┐
│              DataStores.Tests (Tests)                   │
│  - Unit Tests                                           │
│  - Integration Tests                                    │
│  - Performance Tests                                    │
└─────────────────────────────────────────────────────────┘
```

---

## 📁 Neue Dateien

### TestHelper.DataStores

#### Abstraktion
- `TestData/ITestDataFactory.cs` - Interface für Testdaten-Erzeugung

#### Implementierung
- `TestData/ObjectFillerTestDataFactory.cs` - ObjectFiller-basierte Implementierung

#### Builder (erweitert)
- `Builders/DataStoreBuilder.cs` - Erweitert um `WithGeneratedItems(...)`

#### Dokumentation
- `Docs/TestData-Generation-Architecture.md` - Architektur und Best Practices
- `Docs/API-Reference.md` - Aktualisiert mit TestData-Namespace

### DataStores.Tests

#### Unit Tests
- `TestData/ObjectFillerTestDataFactory_Tests.cs` - 14 Tests für Factory
- `Builders/DataStoreBuilder_WithGeneratedItems_Tests.cs` - 13 Tests für Builder

#### Integration Tests
- `Integration/TestDataGeneration_Integration_Tests.cs` - 7 Integration-Tests

---

## ✅ Design-Prinzipien eingehalten

### 1. Keine harte Kopplung ✅
- `ITestDataFactory<T>` abstrakt ObjectFiller
- Produktivcode (DataStores) kennt ObjectFiller nicht
- Austauschbarkeit garantiert

### 2. Klare Verantwortlichkeiten ✅
- **ITestDataFactory**: Erzeugt reine POCOs
- **ObjectFillerTestDataFactory**: Kapselt ObjectFiller
- **DataStoreBuilder**: Integriert Factories optional

### 3. Kompatibilität mit bestehenden Komponenten ✅
- `WithItems(...)` unverändert
- `WithComparer(...)` funktioniert mit generierten Items
- `WithChangedHandler(...)` feuert Events für generierte Items
- Fixtures (LiteDb, Json) kompatibel

### 4. Vollständige Dokumentation ✅
- XML-Kommentare für alle öffentlichen Typen
- Architektur-Dokument mit Motivation
- API-Referenz aktualisiert
- Beispiele für alle Szenarien

### 5. Test-Philosophie eingehalten ✅
- **One Assert Rule**: Jeder Test prüft einen Aspekt
- **Arrange/Act/Assert**: Klar getrennt
- **Keine kombinierten Assertions**
- **Didaktisch lesbar**

---

## 📊 Test-Coverage

### Unit Tests: 27 Tests

#### ObjectFillerTestDataFactory_Tests.cs (14 Tests)
- ✅ CreateSingle liefert nicht-null Entity
- ✅ CreateSingle befüllt Properties
- ✅ Gleicher Seed = identische Daten
- ✅ Unterschiedliche Seeds = unterschiedliche Daten
- ✅ CreateMany liefert korrekte Anzahl
- ✅ CreateMany mit 0 liefert leere Sequenz
- ✅ CreateMany mit negativer Zahl wirft Exception
- ✅ CreateMany liefert distinct Instanzen
- ✅ CreateMany unterstützt lazy evaluation
- ✅ Setup-Action konfiguriert Properties
- ✅ Setup-Action ignoriert Properties
- ✅ Null Setup-Action wirft Exception
- ✅ Thread-Sicherheit gewährleistet
- ✅ Performance (1000 Entities < 5s)

#### DataStoreBuilder_WithGeneratedItems_Tests.cs (13 Tests)
- ✅ WithGeneratedItems fügt Items hinzu
- ✅ Zero Count erstellt leeren Store
- ✅ Null Factory wirft Exception
- ✅ Negativer Count wirft Exception
- ✅ Kombination mit WithItems funktioniert
- ✅ Manuelle Items werden bewahrt
- ✅ Mehrfachaufrufe akkumulieren
- ✅ Comparer wird respektiert
- ✅ Changed-Events werden gefeuert
- ✅ Deterministisch mit gleichem Seed
- ✅ Performance (1000 Entities < 5s)
- ✅ Items werden in korrekter Reihenfolge hinzugefügt
- ✅ Letztes manuelles Item ist letztes

### Integration Tests: 7 Tests

#### TestDataGeneration_Integration_Tests.cs
- ✅ Generierte Items zu globalem Store hinzufügen
- ✅ Generierte Items werden persistiert
- ✅ Lokaler Store unterstützt generierte Items
- ✅ Snapshot funktioniert mit generierten Items
- ✅ Snapshot mit Filter funktioniert
- ✅ Bulk-Operationen funktionieren
- ✅ Query-Operationen funktionieren

**Gesamt: 34 Tests, alle grün ✅**

---

## 🔄 API-Beispiele

### Einfache Verwendung

```csharp
var factory = new ObjectFillerTestDataFactory<Product>(seed: 42);
var store = new DataStoreBuilder<Product>()
    .WithGeneratedItems(factory, count: 100)
    .Build();

Assert.Equal(100, store.Items.Count);
```

### Mit Custom-Setup

```csharp
var factory = new ObjectFillerTestDataFactory<Employee>(
    seed: 123,
    setupAction: filler =>
    {
        filler.Setup()
            .OnProperty(x => x.Age).Use(() => Random.Shared.Next(18, 65))
            .OnProperty(x => x.Id).IgnoreIt();
    });

var employees = factory.CreateMany(50);
```

### Kombiniert mit manuellen Items

```csharp
var specialProduct = new Product { Name = "Special" };
var factory = new ObjectFillerTestDataFactory<Product>(seed: 42);

var store = new DataStoreBuilder<Product>()
    .WithItems(specialProduct)
    .WithGeneratedItems(factory, count: 50)
    .WithComparer(new IdComparer())
    .Build();

Assert.Equal(51, store.Items.Count);
```

### Integration mit Persistence

```csharp
var factory = new ObjectFillerTestDataFactory<Order>(seed: 999);
var globalStore = dataStores.GetGlobal<Order>();
var orders = factory.CreateMany(200).ToList();

// Fachliche Logik hinzufügen
foreach (var order in orders)
{
    order.ShipDate = order.OrderDate.AddDays(Random.Shared.Next(1, 7));
}

globalStore.AddRange(orders);
await Task.Delay(300); // Auto-Save

Assert.Equal(200, globalStore.Items.Count);
```

---

## 🚀 Erweiterbarkeit

### Zukünftige Custom Factories

```csharp
public class PersonWithAddressFactory : ITestDataFactory<Person>
{
    private readonly AddressFactory _addressFactory = new();
    
    public Person CreateSingle()
    {
        return new Person
        {
            Name = $"Person_{Guid.NewGuid()}",
            Address = _addressFactory.CreateSingle() // Relationen
        };
    }
    
    public IEnumerable<Person> CreateMany(int count)
    {
        return Enumerable.Range(0, count).Select(_ => CreateSingle());
    }
}
```

### Auslagern in separates Projekt (optional)

Wenn gewünscht, kann später ein separates Projekt erstellt werden:

```
TestHelper.DataStores.ObjectFiller.csproj
  - ObjectFillerTestDataFactory<T>
  - ObjectFiller-spezifische Extensions
  - Package-Referenz: Tynamix.ObjectFiller
```

Aktuell ist die Integration direkt in TestHelper.DataStores optimal.

---

## 📝 Zusammenfassung

### Erreichte Ziele ✅

1. ✅ Saubere Abstraktion (`ITestDataFactory<T>`)
2. ✅ Optionale ObjectFiller-Integration
3. ✅ Keine Breaking Changes an bestehender API
4. ✅ Kompatibel mit Fixtures und Persistence
5. ✅ Vollständige Dokumentation
6. ✅ 34 Tests (alle grün)
7. ✅ Thread-safe und performant
8. ✅ One Assert Rule eingehalten

### Keine Kompromisse ❌

- Keine harte Kopplung an ObjectFiller
- Keine Abhängigkeit von DataStores auf Testdaten
- Keine Seiteneffekte beim Generieren
- Keine statischen Globals
- Keine Breaking Changes

### Nächste Schritte (optional)

1. Integration in bestehende Tests nutzen
2. Performance-Tests mit 10.000+ Entities
3. Weitere Custom Factories für spezielle Szenarien
4. CI/CD-Pipeline mit Tests

---

**Status:** ✅ Produktionsreif  
**Build:** ✅ Erfolgreich  
**Tests:** ✅ 34/34 grün  
**Dokumentation:** ✅ Vollständig

