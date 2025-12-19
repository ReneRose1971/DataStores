# DataStores.Tests Fake-Framework

## ?? Übersicht

Die Fake-Implementierungen folgen dem Muster aus **DataToolKit.Tests** und bieten:
- ? Vollständige Kontrolle über Verhalten
- ? History-Tracking für alle Operationen
- ? Controllable Failures (ThrowOn*)
- ? Reset-Funktionalität für Test-Isolation

---

## ??? Verfügbare Fakes

### **1. FakeDataStore<T>**

Testbare In-Memory-Store-Implementierung mit vollständigem Tracking.

#### Verwendung:

```csharp
[Fact]
public void Example_FakeDataStore_Usage()
{
    // Arrange
    var fake = new FakeDataStore<TestItem>();
    
    // Act
    fake.Add(new TestItem { Id = 1, Name = "Test" });
    fake.ThrowOnRemove = true; // Simuliere Fehler
    
    // Assert
    Assert.Equal(1, fake.AddCallCount);
    Assert.Single(fake.Items);
    Assert.Throws<InvalidOperationException>(() => fake.Remove(fake.Items[0]));
}
```

#### Properties:

| Property | Typ | Beschreibung |
|----------|-----|--------------|
| `AddCallCount` | `int` | Anzahl Add-Aufrufe |
| `RemoveCallCount` | `int` | Anzahl Remove-Aufrufe |
| `ClearCallCount` | `int` | Anzahl Clear-Aufrufe |
| `AddRangeCallCount` | `int` | Anzahl AddRange-Aufrufe |
| `ThrowOnAdd` | `bool` | Wirft bei Add |
| `ThrowOnRemove` | `bool` | Wirft bei Remove |
| `ChangedEvents` | `IReadOnlyList<EventArgs>` | Event-History |

---

### **2. FakeGlobalStoreRegistry**

Testbare Registry mit vollständigem History-Tracking.

#### Verwendung:

```csharp
[Fact]
public void Example_FakeRegistry_Usage()
{
    // Arrange
    var registry = new FakeGlobalStoreRegistry();
    var store = new FakeDataStore<TestItem>();
    
    // Act
    registry.RegisterGlobal(store);
    var resolved = registry.ResolveGlobal<TestItem>();
    
    // Assert
    Assert.Equal(1, registry.RegisterCallCount);
    Assert.Equal(1, registry.ResolveGlobalCallCount);
    Assert.Same(store, resolved);
    
    // History-Tracking prüfen
    Assert.Equal(2, registry.History.Count);
    Assert.Equal("Register", registry.History[0].Action);
    Assert.Equal("ResolveGlobal", registry.History[1].Action);
}
```

#### Properties:

| Property | Typ | Beschreibung |
|----------|-----|--------------|
| `RegisterCallCount` | `int` | Anzahl Register-Aufrufe |
| `ResolveGlobalCallCount` | `int` | Anzahl ResolveGlobal-Aufrufe |
| `TryResolveGlobalCallCount` | `int` | Anzahl TryResolveGlobal-Aufrufe |
| `ThrowOnRegister` | `bool` | Wirft bei Register |
| `ThrowOnResolveGlobal` | `bool` | Wirft bei ResolveGlobal |
| `History` | `List<(string, Type, object?)>` | Vollständige Operation-History |

---

## ?? Builders

### **DataStoreBuilder<T>**

Fluent Builder für Test-DataStores mit vorkonfigurierten Szenarien.

#### Verwendung:

```csharp
[Fact]
public void Example_Builder_Usage()
{
    // Arrange
    var syncContext = new RecordingSynchronizationContext();
    
    var store = new DataStoreBuilder<TestItem>()
        .WithItems(
            new TestItem { Id = 1, Name = "A" },
            new TestItem { Id = 2, Name = "B" }
        )
        .WithSyncContext(syncContext)
        .WithChangedHandler((s, e) => Console.WriteLine($"Changed: {e.ChangeType}"))
        .Build();
    
    // Act
    store.Add(new TestItem { Id = 3, Name = "C" });
    
    // Assert
    Assert.Equal(3, store.Items.Count);
}
```

#### Fluent API:

| Methode | Parameter | Beschreibung |
|---------|-----------|--------------|
| `WithItems()` | `params T[]` | Initiale Items |
| `WithSyncContext()` | `SynchronizationContext` | Event-Marshaling-Context |
| `WithComparer()` | `IEqualityComparer<T>` | Custom Comparer |
| `WithChangedHandler()` | `EventHandler<EventArgs>` | Event-Handler |
| `Build()` | - | Erstellt Store |

---

## ?? Test-Patterns

### **Pattern 1: Controllable Failure**

```csharp
[Fact]
public void Test_Error_Handling()
{
    // Arrange
    var fake = new FakeDataStore<TestItem>();
    fake.ThrowOnAdd = true;
    
    // Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(() => 
        fake.Add(new TestItem { Id = 1 }));
    
    Assert.Contains("Simulated add failure", ex.Message);
}
```

### **Pattern 2: History-Tracking**

```csharp
[Fact]
public void Test_Operation_History()
{
    // Arrange
    var registry = new FakeGlobalStoreRegistry();
    var store = new FakeDataStore<TestItem>();
    
    // Act
    registry.RegisterGlobal(store);
    registry.ResolveGlobal<TestItem>();
    registry.TryResolveGlobal<TestItem>(out _);
    
    // Assert - Prüfe Reihenfolge
    Assert.Equal(3, registry.History.Count);
    Assert.Equal("Register", registry.History[0].Action);
    Assert.Equal("ResolveGlobal", registry.History[1].Action);
    Assert.Equal("TryResolveGlobal", registry.History[2].Action);
}
```

### **Pattern 3: Reset für Test-Isolation**

```csharp
public class MyTestClass : IDisposable
{
    private readonly FakeDataStore<TestItem> _fake;

    public MyTestClass()
    {
        _fake = new FakeDataStore<TestItem>();
    }

    [Fact]
    public void Test1()
    {
        _fake.Add(new TestItem { Id = 1 });
        Assert.Single(_fake.Items);
    }

    [Fact]
    public void Test2()
    {
        // Test-Isolation: Jeder Test startet sauber
        Assert.Empty(_fake.Items);
    }

    public void Dispose()
    {
        _fake.Reset();
    }
}
```

### **Pattern 4: Event-Tracking**

```csharp
[Fact]
public void Test_Event_Tracking()
{
    // Arrange
    var fake = new FakeDataStore<TestItem>();
    
    // Act
    fake.Add(new TestItem { Id = 1 });
    fake.AddRange(new[] { new TestItem { Id = 2 }, new TestItem { Id = 3 } });
    fake.Remove(fake.Items[0]);
    fake.Clear();
    
    // Assert - Prüfe alle Events
    Assert.Equal(4, fake.ChangedEvents.Count);
    Assert.Equal(DataStoreChangeType.Add, fake.ChangedEvents[0].ChangeType);
    Assert.Equal(DataStoreChangeType.BulkAdd, fake.ChangedEvents[1].ChangeType);
    Assert.Equal(DataStoreChangeType.Remove, fake.ChangedEvents[2].ChangeType);
    Assert.Equal(DataStoreChangeType.Clear, fake.ChangedEvents[3].ChangeType);
}
```

---

## ?? Best Practices

### ? **DO: Test-Isolation**
```csharp
// Jeder Test verwendet eigene Fake-Instanzen
[Fact]
public void Test1() 
{ 
    var fake = new FakeDataStore<TestItem>(); 
    // ... 
}

[Fact]
public void Test2() 
{ 
    var fake = new FakeDataStore<TestItem>(); // Neue Instanz!
    // ... 
}
```

### ? **DO: Reset bei Shared Fixtures**
```csharp
public class Tests : IDisposable
{
    private readonly FakeDataStore<TestItem> _shared = new();
    
    public void Dispose() => _shared.Reset();
}
```

### ? **DO: Descriptive Test Names**
```csharp
// ? Schlecht
Test1()

// ? Gut
FakeDataStore_Should_ThrowException_WhenThrowOnAddIsTrue()
```

### ? **DON'T: State zwischen Tests teilen**
```csharp
// ? SCHLECHT - Shared State!
private static readonly FakeDataStore<TestItem> _shared = new();

[Fact]
public void Test1() { _shared.Add(...); } // Beeinflusst Test2!

[Fact]
public void Test2() { Assert.Empty(_shared.Items); } // Schlägt fehl!
```

---

## ?? Vergleich zu DataToolKit.Tests

| Feature | DataToolKit | DataStores | Status |
|---------|-------------|------------|--------|
| **Fakes mit History** | ? | ? | Implementiert |
| **Controllable Failures** | ? | ? | Implementiert |
| **Builder-Pattern** | ? | ? | Implementiert |
| **Fixtures** | ? | ? | Geplant (Phase 2) |
| **Repository-Fakes** | ? | ? | N/A (keine Repositories) |
| **Scenario-Builder** | ? | ? | Geplant (Phase 3) |

---

## ?? Erweiterte Nutzung

### **Kombinierte Szenarien**

```csharp
[Fact]
public void Complex_Scenario_With_Registry_And_Store()
{
    // Arrange
    var registry = new FakeGlobalStoreRegistry();
    var store = new DataStoreBuilder<TestItem>()
        .WithItems(new TestItem { Id = 1, Name = "Seeded" })
        .Build();
    
    registry.RegisterGlobal(store);
    
    // Act
    var resolved = registry.ResolveGlobal<TestItem>();
    resolved.Add(new TestItem { Id = 2, Name = "Added" });
    
    // Assert
    Assert.Same(store, resolved);
    Assert.Equal(2, resolved.Items.Count);
    Assert.Equal(1, registry.RegisterCallCount);
    Assert.Equal(1, registry.ResolveGlobalCallCount);
}
```

---

**Weitere Beispiele siehe:** `DataToolKit.Tests.md` (Referenz-Implementierung)
