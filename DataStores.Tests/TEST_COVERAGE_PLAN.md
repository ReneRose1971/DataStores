# DataStores Test Coverage Plan

## ?? Aktuelle Test-Abdeckung (Status: Phase 1 abgeschlossen)

### ? Abgeschlossene Tests (Phase 1 - ~60% Coverage)

#### **Fakes & Test-Infrastruktur**
- ? `FakeDataStore<T>` - Vollständig testbare In-Memory-Store-Implementierung
- ? `FakeGlobalStoreRegistry` - Testbare Registry mit History-Tracking
- ? `DataStoreBuilder<T>` - Fluent Builder für Test-DataStores

#### **InMemoryDataStore Tests**
- ? `InMemoryDataStore_SyncContextTests` - SynchronizationContext-Marshaling (5 Tests)
- ? `InMemoryDataStore_ThreadSafetyTests` - Thread-Safety (6 Tests)
- ? `InMemoryDataStore_EdgeCaseTests` - Edge Cases (13 Tests)
- ? `InMemoryDataStoreTests` - Basis-Funktionalität (bereits vorhanden, 15 Tests)

#### **Abstractions Tests**
- ? `DataStoreChangedEventArgs_Tests` - EventArgs-Validierung (9 Tests)
- ? `Exceptions_Tests` - Custom Exceptions (7 Tests)

### ?? Teilweise getestet (~30% Coverage)

#### **GlobalStoreRegistry**
- ? `GlobalStoreRegistryTests` (bereits vorhanden, 10 Tests)
- ? Fehlt: Concurrency-Tests, Dispose-Szenarien

#### **PersistentStoreDecorator**
- ? `PersistentStoreDecoratorTests` (bereits vorhanden, 13 Tests)
- ? Fehlt: Race Conditions, Async-Edge-Cases

#### **ParentChildRelationship**
- ? `ParentChildRelationshipTests` (bereits vorhanden, 15 Tests)
- ? Fehlt: Complex Filtering, Dispose-Tests

### ? Fehlende Tests (~40% Coverage)

#### **DataStoresFacade**
- ? Snapshot-Mode Tests
- ? Error Handling
- ? Event Propagation

#### **DataStoreBootstrap**
- ? Error Recovery
- ? Multiple Registrars
- ? DI Integration

---

## ?? Phase 2: Edge Cases & Error Handling (95% Coverage)

### **Noch zu erstellen:**

1. **InMemoryDataStore_ComparerTests.cs**
   - Custom Comparer-Szenarien
   - Equals/GetHashCode-Interaktion
   - Comparer-Änderungen zur Laufzeit

2. **GlobalStoreRegistry_ConcurrencyTests.cs**
   - Concurrent Registration/Resolution
   - Dispose während aktiver Nutzung
   - Thread-Safety

3. **PersistentStoreDecorator_RaceConditionTests.cs**
   - Load + Save Race Conditions
   - SemaphoreSlim Deadlock-Prevention
   - Concurrent Dispose

4. **ParentChildRelationship_EdgeCaseTests.cs**
   - Null Parent/Filter
   - Complex Cascade Updates
   - Filter Exceptions

5. **DataStoresFacade_ErrorHandlingTests.cs**
   - Snapshot-Mode Validierung
   - Missing Store-Fehler
   - Event-Propagation

6. **DataStoreBootstrap_ErrorRecoveryTests.cs**
   - Fehlerhafte Registrars
   - Rollback-Szenarien
   - Service Provider Fehler

---

## ?? Phase 3: Integration & Stress Tests (100% Coverage)

1. **DataStoresFacade_IntegrationTests.cs**
   - End-to-End Szenarien
   - Multi-Store Koordination

2. **End2End_ScenarioTests.cs**
   - Realistische Anwendungsfälle
   - Performance unter Last
   - Memory-Leak-Tests

---

## ?? Test-Metriken

| Kategorie | Tests | Coverage |
|-----------|-------|----------|
| **InMemoryDataStore** | 39 | ~85% |
| **GlobalStoreRegistry** | 10 | ~60% |
| **PersistentStore** | 13 | ~70% |
| **ParentChild** | 15 | ~65% |
| **DataStoresFacade** | 8 | ~50% |
| **Bootstrap** | 9 | ~55% |
| **Abstractions** | 16 | ~90% |
| **GESAMT** | **110** | **~70%** |

---

## ?? Verwendete Test-Patterns

### **Aus DataToolKit.Tests gelernt:**

1. **Fake-Struktur**
   - History-Tracking für alle Operationen
   - Controllable Behavior (ThrowOn*, SimulatedDelay)
   - Reset-Methode für Test-Isolation

2. **Builder-Pattern**
   - Fluent API für Test-Daten
   - Vorkonfigurierte Szenarien
   - Lesbare Test-Setup

3. **Fixture-Pattern**
   - IDisposable für Cleanup
   - Shared Setup-Logik
   - Resource-Management

---

## ?? Best Practices

### **Aus DataToolKit übernommen:**

? **Testname = User Story**
```csharp
// ? Schlecht
Test1()

// ? Gut  
Add_Should_MarshalToSyncContext_WhenCalledFromDifferentThread()
```

? **AAA-Pattern strikt einhalten**
```csharp
// Arrange - Setup
// Act - Aktion
// Assert - Validierung
```

? **One Assert Per Concept**
```csharp
// Pro logischem Konzept ein Test
// Nicht alles in einen Mega-Test packen
```

? **Test-Isolation**
```csharp
// Jeder Test startet mit sauberem Zustand
// IDisposable für Cleanup
// Reset-Methoden für Fakes
```

---

## ?? Nächste Schritte

1. ? Phase 1 abgeschlossen - Kritische Tests erstellt
2. ?? Phase 2 starten - Edge Cases & Error Handling
3. ? Phase 3 vorbereiten - Integration Tests
4. ?? Coverage-Report generieren

---

## ?? Anmerkungen

- **RecordingSynchronizationContext** aus TestHelper ist `sealed` ? Keine Ableitung möglich
- **DataStoreChangedEventArgs** hat mehrere Constructor-Overloads ? Cast bei `null` notwendig
- **IGlobalStoreRegistry** hat `ResolveGlobal` und `TryResolveGlobal` ? Beide in Fakes implementieren
- **Exception.EntityType** heißt tatsächlich `StoreType` ? Tests angepasst

---

**Status:** ? Build erfolgreich | 110 Tests | ~70% Coverage | Phase 1 abgeschlossen
