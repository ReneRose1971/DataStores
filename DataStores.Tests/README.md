# DataStores.Tests - Test-Projekt

Umfassende Testsuite f�r die DataStores-Bibliothek mit �ber 100 Tests zur Sicherstellung der Qualit�t und Zuverl�ssigkeit.

## ?? �bersicht

Dieses Projekt enth�lt alle Tests f�r die DataStores-Bibliothek, organisiert in verschiedene Kategorien zur umfassenden Abdeckung aller Funktionen und Edge-Cases.

## ?? Test-Kategorien

### 1. **Unit Tests** (Grundlegende Funktionalit�t)
- `InMemoryDataStoreTests.cs` - Tests f�r den In-Memory-Store
- `DataStoresFacadeTests.cs` - Tests f�r die Facade
- `GlobalStoreRegistryTests.cs` - Tests f�r die Registry
- `ServiceCollectionExtensionsTests.cs` - DI-Integration-Tests
- `DataStoreBootstrapTests.cs` - Bootstrap-Prozess-Tests
- `PersistentStoreDecoratorTests.cs` - Persistierung-Tests
- `ParentChildRelationshipTests.cs` - Beziehungs-Tests

### 2. **Runtime Tests** (Laufzeit-Verhalten)
- `Runtime/InMemoryDataStore_ThreadSafetyTests.cs` - Thread-Sicherheits-Tests
- `Runtime/InMemoryDataStore_SyncContextTests.cs` - SynchronizationContext-Tests
- `Runtime/InMemoryDataStore_ComparerTests.cs` - Custom Comparer-Tests
- `Runtime/InMemoryDataStore_EdgeCaseTests.cs` - Edge-Case-Tests
- `Runtime/DataStoresFacade_ErrorHandlingTests.cs` - Fehlerbehandlung
- `Runtime/GlobalStoreRegistry_ConcurrencyTests.cs` - Nebenl�ufigkeits-Tests
- `Runtime/LocalDataStoreFactory_Tests.cs` - Factory-Tests

### 3. **Persistence Tests** (Persistierung)
- `Persistence/PersistentStoreDecorator_RaceConditionTests.cs` - Race-Condition-Tests

### 4. **Relations Tests** (Beziehungen)
- `Relations/ParentChildRelationship_EdgeCaseTests.cs` - Edge-Case-Tests

### 5. **Bootstrap Tests** (Initialisierung)
- `Bootstrap/DataStoreBootstrap_ErrorRecoveryTests.cs` - Fehler-Recovery-Tests

### 6. **Abstractions Tests** (Basis-Typen)
- `Abstractions/DataStoreChangedEventArgs_Tests.cs` - EventArgs-Tests
- `Abstractions/Exceptions_Tests.cs` - Exception-Tests

### 7. **Integration Tests** (End-to-End)
- `Integration/End2End_ScenarioTests.cs` - Vollst�ndige Szenarien

### 8. **Performance Tests** (Leistung)
- `Performance/Performance_StressTests.cs` - Stress- und Performance-Tests

## ?? Test-Statistiken

| Kategorie | Anzahl Tests | Abdeckung |
|-----------|--------------|-----------|
| Unit Tests | ~40 | Core-Funktionalit�t |
| Runtime Tests | ~30 | Thread-Sicherheit & Edge-Cases |
| Persistence Tests | ~15 | Auto-Load/Save & Race-Conditions |
| Relations Tests | ~10 | Eltern-Kind-Beziehungen |
| Integration Tests | ~10 | End-to-End-Szenarien |
| Performance Tests | ~5 | Stress & Concurrency |
| **Gesamt** | **~110** | **Umfassend** |

## ??? Test-Infrastruktur

### Test-Helpers

#### Fakes
- `Fakes/FakeDataStore.cs` - Test-Double f�r IDataStore
- `Fakes/FakeGlobalStoreRegistry.cs` - Test-Double f�r Registry

#### Builders
- `Builders/DataStoreBuilder.cs` - Fluent Builder f�r Test-Stores

#### Test-Utilities
- `FakePersistenceStrategy.cs` - Test-Double f�r Persistierung

## ?? Tests ausf�hren

### Alle Tests

```bash
dotnet test
```

### Spezifische Kategorie

```bash
# Nur Runtime-Tests
dotnet test --filter "FullyQualifiedName~Runtime"

# Nur Performance-Tests
dotnet test --filter "FullyQualifiedName~Performance"

# Nur Integration-Tests
dotnet test --filter "FullyQualifiedName~Integration"
```

### Mit Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Einzelner Test

```bash
dotnet test --filter "FullyQualifiedName~InMemoryDataStore_Add_Should_AddItem"
```

## ?? Test-Beispiele

### Beispiel 1: Grundlegender Unit Test

```csharp
/// <summary>
/// Test f�r Add-Funktionalit�t.
/// </summary>
[Fact]
public void Add_Should_AddItemToStore()
{
    // Arrange
    var store = new InMemoryDataStore<TestItem>();
    var item = new TestItem { Id = 1, Name = "Test" };
    
    // Act
    store.Add(item);
    
    // Assert
    Assert.Single(store.Items);
    Assert.Contains(item, store.Items);
}
```

### Beispiel 2: Event-Test

```csharp
/// <summary>
/// Test f�r Changed-Event.
/// </summary>
[Fact]
public void Add_Should_RaiseChangedEvent()
{
    // Arrange
    var store = new InMemoryDataStore<TestItem>();
    var item = new TestItem { Id = 1 };
    
    DataStoreChangedEventArgs<TestItem>? receivedArgs = null;
    store.Changed += (s, e) => receivedArgs = e;
    
    // Act
    store.Add(item);
    
    // Assert
    Assert.NotNull(receivedArgs);
    Assert.Equal(DataStoreChangeType.Add, receivedArgs.ChangeType);
    Assert.Single(receivedArgs.AffectedItems);
}
```

### Beispiel 3: Thread-Sicherheits-Test

```csharp
/// <summary>
/// Test f�r Thread-Sicherheit.
/// </summary>
[Fact]
public void ConcurrentAdds_Should_BeThreadSafe()
{
    // Arrange
    var store = new InMemoryDataStore<TestItem>();
    const int threadCount = 10;
    const int itemsPerThread = 100;
    
    // Act
    Parallel.For(0, threadCount, i =>
    {
        for (int j = 0; j < itemsPerThread; j++)
        {
            store.Add(new TestItem { Id = i * itemsPerThread + j });
        }
    });
    
    // Assert
    Assert.Equal(threadCount * itemsPerThread, store.Items.Count);
}
```

### Beispiel 4: Persistierung-Test

```csharp
/// <summary>
/// Test f�r Auto-Save.
/// </summary>
[Fact]
public async Task AutoSave_Should_SaveOnChange()
{
    // Arrange
    var strategy = new FakePersistenceStrategy<TestItem>();
    var innerStore = new InMemoryDataStore<TestItem>();
    var decorator = new PersistentStoreDecorator<TestItem>(
        innerStore, strategy, autoLoad: false, autoSaveOnChange: true);
    
    // Act
    decorator.Add(new TestItem { Id = 1 });
    await Task.Delay(100); // Auto-Save ist async
    
    // Assert
    Assert.True(strategy.SaveCallCount > 0);
}
```

### Beispiel 5: Integration Test

```csharp
/// <summary>
/// End-to-End-Szenario-Test.
/// </summary>
[Fact]
public async Task End2End_ProductManagement_Scenario()
{
    // Arrange - Services einrichten
    var services = new ServiceCollection();
    services.AddDataStoresCore();
    services.AddDataStoreRegistrar<TestRegistrar>();
    
    var provider = services.BuildServiceProvider();
    await DataStoreBootstrap.RunAsync(provider);
    
    var stores = provider.GetRequiredService<IDataStores>();
    
    // Act - Produkte verwalten
    var productStore = stores.GetGlobal<Product>();
    productStore.Add(new Product { Id = 1, Name = "Laptop" });
    productStore.Add(new Product { Id = 2, Name = "Maus" });
    
    var localStore = stores.CreateLocal<Product>();
    localStore.Add(new Product { Id = 3, Name = "Temp" });
    
    // Assert
    Assert.Equal(2, productStore.Items.Count);
    Assert.Single(localStore.Items);
    Assert.NotSame(productStore, localStore);
}
```

## ?? Code Coverage

### Aktuelle Abdeckung (Ziel: >90%)

| Namespace | Coverage |
|-----------|----------|
| Abstractions | 95%+ |
| Runtime | 95%+ |
| Persistence | 90%+ |
| Relations | 90%+ |
| Bootstrap | 95%+ |

### Coverage anzeigen

```bash
dotnet test /p:CollectCoverage=true
dotnet reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report
```

## ?? Wichtige Test-Szenarien

### Thread-Sicherheit
- ? Parallele Adds
- ? Parallele Removes
- ? Read w�hrend Write
- ? Clear w�hrend Read
- ? Multiple Threads auf Items

### Edge Cases
- ? Null-Handling
- ? Leere Collections
- ? Doppelte Eintr�ge
- ? Gro�e Datenmengen (10,000+ Items)
- ? Custom Comparers

### Persistierung
- ? Auto-Load beim Bootstrap
- ? Auto-Save bei �nderungen
- ? Race Conditions (multiple Saves)
- ? Fehler beim Laden/Speichern
- ? Cancellation Tokens

### Beziehungen
- ? Parent-Child-Filterung
- ? Refresh-Mechanismus
- ? Global vs. Snapshot DataSource
- ? Verschachtelte Beziehungen

### DI & Bootstrap
- ? Service-Registrierung
- ? Multiple Registrare
- ? Bootstrap mit Persistierung
- ? Error Recovery

## ?? Test-Dependencies

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.5.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="coverlet.collector" Version="6.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.1" />
```

## ?? Best Practices

### Test-Naming Convention

```csharp
// Pattern: [MethodName]_Should_[ExpectedBehavior]_When_[Condition]

[Fact]
public void Add_Should_AddItem_When_ItemIsValid() { }

[Fact]
public void Remove_Should_ReturnFalse_When_ItemNotFound() { }

[Fact]
public void Changed_Should_Fire_When_ItemAdded() { }
```

### AAA-Pattern (Arrange-Act-Assert)

```csharp
[Fact]
public void ExampleTest()
{
    // Arrange - Setup
    var store = new InMemoryDataStore<TestItem>();
    var item = new TestItem { Id = 1 };
    
    // Act - Ausf�hrung
    store.Add(item);
    
    // Assert - �berpr�fung
    Assert.Single(store.Items);
}
```

### Test-Isolation

```csharp
// ? GOOD - Jeder Test hat eigene Instanz
[Fact]
public void Test1()
{
    var store = new InMemoryDataStore<TestItem>();
    // Test logic
}

[Fact]
public void Test2()
{
    var store = new InMemoryDataStore<TestItem>(); // Neue Instanz!
    // Test logic
}

// ? BAD - Shared State
private static InMemoryDataStore<TestItem> _sharedStore = new();

[Fact]
public void Test1() { _sharedStore.Add(...); } // Beeinflusst Test2!

[Fact]
public void Test2() { Assert.Empty(_sharedStore.Items); } // Scheitert!
```

## ?? Debugging Tests

### Visual Studio

1. Breakpoint im Test setzen
2. Test mit **Debug Selected Tests** ausf�hren
3. Debugger stoppt am Breakpoint

### VS Code

1. `.vscode/launch.json` konfigurieren
2. Breakpoint setzen
3. F5 dr�cken

### Command Line

```bash
# Mit Debugger-Attach
dotnet test --filter "FullyQualifiedName~MyTest" --logger "console;verbosity=detailed"
```

## ?? Continuous Integration

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
```

## ?? Beitragen

### Neue Tests hinzuf�gen

1. **Kategorie w�hlen**: Unit, Runtime, Integration, etc.
2. **Test schreiben**: AAA-Pattern verwenden
3. **Naming Convention**: Beschreibende Namen
4. **Dokumentieren**: XML-Kommentare hinzuf�gen
5. **Ausf�hren**: Sicherstellen, dass Test gr�n ist

### Test-Coverage erh�hen

1. Fehlende Szenarien identifizieren
2. Edge-Cases testen
3. Error-Handling testen
4. Thread-Safety testen

## ?? Weitere Dokumentation

- [DataStores README](../DataStores/README.md)
- [API Referenz](../DataStores/Docs/API-Reference.md)
- [Usage Examples](../DataStores/Docs/Usage-Examples.md)

---

**Version**: 1.0.0  
**Test Framework**: xUnit 2.5.3  
**Target Framework**: .NET 8.0  
**Letzte Aktualisierung**: Januar 2025
