# STRATEGIC REVIEW: Testhelfer-Auslagerung nach TestHelper.DataStores

**Datum:** 2025-01-20  
**Status:** KLASSIFIKATION ABGESCHLOSSEN

---

## PHASE 1: GEFUNDENE HILFSKLASSEN

### A) Eigenständige Dateien (im Root oder Ordnern)

| Datei | Pfad | Klassifikation |
|-------|------|----------------|
| `DataStoreBuilder.cs` | `/Builders/` | **A - Wiederverwendbar** ✅ |
| `FakePersistenceStrategy.cs` | `/` (Root) | **A - Wiederverwendbar** ✅ |
| `FakeDataStore.cs` | `/Fakes/` | **A - Wiederverwendbar** ✅ |
| `FakeGlobalStoreRegistry.cs` | `/Fakes/` | **A - Wiederverwendbar** ✅ |

### B) Private Nested Classes in Testdateien

| Testdatei | Nested Class | Verwendung | Klassifikation |
|-----------|--------------|------------|----------------|
| `PersistentStoreDecorator_RaceConditionTests.cs` | `SlowLoadStrategy<T>` | 3 Tests | **A - Wiederverwendbar** ✅ |
| `PersistentStoreDecorator_RaceConditionTests.cs` | `ThrowingPersistenceStrategy<T>` | 2 Tests | **A - Wiederverwendbar** ✅ |
| `Bootstrap/DataStoreBootstrap_ErrorRecoveryTests.cs` | `SlowInitStrategy<T>` | 1 Test | **B - Lokal** ❌ (ähnlich SlowLoadStrategy) |
| `Bootstrap/DataStoreBootstrap_ErrorRecoveryTests.cs` | `TrackingRegistrar` | 1 Test | **B - Lokal** ❌ |
| `Bootstrap/DataStoreBootstrap_ErrorRecoveryTests.cs` | `FailingRegistrar` | 1 Test | **B - Lokal** ❌ |
| `Bootstrap/DataStoreBootstrap_ErrorRecoveryTests.cs` | `TrackingAsyncInitializable` | 2 Tests | **B - Lokal** ❌ |
| `Bootstrap/DataStoreBootstrap_ErrorRecoveryTests.cs` | `FailingAsyncInitializable` | 1 Test | **B - Lokal** ❌ |
| `Runtime/InMemoryDataStore_ComparerTests.cs` | `IdOnlyComparer` | Mehrfach | **A - Wiederverwendbar** ✅ |
| `Runtime/InMemoryDataStore_ComparerTests.cs` | `NameOnlyComparer` | 1 Test | **B - Lokal** ❌ |
| `Runtime/InMemoryDataStore_ComparerTests.cs` | `BadHashCodeComparer` | 1 Test | **B - Lokal** ❌ |
| `Runtime/InMemoryDataStore_ComparerTests.cs` | `NullSafeComparer` | 1 Test | **B - Lokal** ❌ |
| `Runtime/InMemoryDataStore_ComparerTests.cs` | `ThrowingComparer` | 1 Test | **B - Lokal** ❌ |
| `Runtime/InMemoryDataStore_ComparerTests.cs` | `CaseInsensitiveNameComparer` | 1 Test | **B - Lokal** ❌ |
| `Runtime/InMemoryDataStore_EdgeCaseTests.cs` | `IdOnlyComparer` | 1 Test | **DUPLIKAT** 🔴 |
| `Runtime/LocalDataStoreFactory_Tests.cs` | `IdOnlyComparer` | 2 Tests | **DUPLIKAT** 🔴 |
| `Runtime/DataStoresFacade_ErrorHandlingTests.cs` | `IdOnlyComparer` | 1 Test | **DUPLIKAT** 🔴 |
| `Performance/Performance_StressTests.cs` | `IdOnlyComparer` | 2 Tests | **DUPLIKAT** 🔴 |

---

## PHASE 2: KLASSIFIKATION & REDUNDANZEN

### ✅ AUSLAGERUNG NACH TestHelper.DataStores (Kategorie A)

#### 1. Builders/DataStoreBuilder.cs
- **Ziel:** `TestHelper.DataStores/Builders/DataStoreBuilder.cs`
- **Begründung:** Fluent Builder für Tests, wiederverwendbar
- **Änderung:** Namespace → `TestHelper.DataStores.Builders`

#### 2. FakePersistenceStrategy.cs
- **Ziel:** `TestHelper.DataStores/Persistence/FakePersistenceStrategy.cs`
- **Begründung:** Wird in 10+ Tests verwendet
- **Änderung:** Namespace → `TestHelper.DataStores.Persistence`

#### 3. Fakes/FakeDataStore.cs
- **Ziel:** `TestHelper.DataStores/Fakes/FakeDataStore.cs`
- **Begründung:** Wird in 5+ Tests verwendet
- **Änderung:** Namespace → `TestHelper.DataStores.Fakes`

#### 4. Fakes/FakeGlobalStoreRegistry.cs
- **Ziel:** `TestHelper.DataStores/Fakes/FakeGlobalStoreRegistry.cs`
- **Begründung:** Wird in 3+ Tests verwendet
- **Änderung:** Namespace → `TestHelper.DataStores.Fakes`

#### 5. SlowLoadStrategy<T> (aus RaceConditionTests)
- **Ziel:** `TestHelper.DataStores/Persistence/SlowLoadStrategy.cs`
- **Begründung:** Nützlich für Timing/Race-Condition-Tests
- **Änderung:** 
  - Extrahieren als public class
  - Namespace → `TestHelper.DataStores.Persistence`
  - Konstruktor: `SlowLoadStrategy(TimeSpan delay, IReadOnlyList<T> data)`

#### 6. ThrowingPersistenceStrategy<T> (aus RaceConditionTests)
- **Ziel:** `TestHelper.DataStores/Persistence/ThrowingPersistenceStrategy.cs`
- **Begründung:** Fehlerfall-Simulation
- **Änderung:**
  - Extrahieren als public class
  - Namespace → `TestHelper.DataStores.Persistence`
  - Konstruktor: `ThrowingPersistenceStrategy(bool throwOnLoad, bool throwOnSave)`

#### 7. IdOnlyComparer → KeySelectorEqualityComparer<T,TKey> (generisch!)
- **Ziel:** `TestHelper.DataStores/Comparers/KeySelectorEqualityComparer.cs`
- **Begründung:** 
  - **KRITISCH**: 5+ Duplikate in verschiedenen Dateien!
  - Konsolidierung zu generischer Lösung
- **Implementation:**
```csharp
public class KeySelectorEqualityComparer<T, TKey> : IEqualityComparer<T>
{
    private readonly Func<T, TKey> _keySelector;
    private readonly IEqualityComparer<TKey> _keyComparer;

    public KeySelectorEqualityComparer(
        Func<T, TKey> keySelector, 
        IEqualityComparer<TKey>? keyComparer = null)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
    }

    public bool Equals(T? x, T? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return _keyComparer.Equals(_keySelector(x), _keySelector(y));
    }

    public int GetHashCode(T obj) => 
        obj != null ? _keyComparer.GetHashCode(_keySelector(obj)!) : 0;
}
```
- **Verwendung:**
```csharp
// Statt: new IdOnlyComparer()
// Jetzt: new KeySelectorEqualityComparer<TestItem, int>(x => x.Id)
```

---

### ❌ BLEIBEN LOKAL (Kategorie B)

Diese Helper sind zu spezifisch und werden nur in einem Test verwendet:

| Class | Datei | Grund |
|-------|-------|-------|
| `SlowInitStrategy<T>` | Bootstrap_ErrorRecoveryTests | Duplikat zu `SlowLoadStrategy` (konsolidieren) |
| `TrackingRegistrar` | Bootstrap_ErrorRecoveryTests | Test-spezifisch |
| `FailingRegistrar` | Bootstrap_ErrorRecoveryTests | Test-spezifisch |
| `TrackingAsyncInitializable` | Bootstrap_ErrorRecoveryTests | Test-spezifisch |
| `FailingAsyncInitializable` | Bootstrap_ErrorRecoveryTests | Test-spezifisch |
| `NameOnlyComparer` | InMemoryDataStore_ComparerTests | Test-spezifisch |
| `BadHashCodeComparer` | InMemoryDataStore_ComparerTests | Test-spezifisch |
| `NullSafeComparer` | InMemoryDataStore_ComparerTests | Test-spezifisch |
| `ThrowingComparer` | InMemoryDataStore_ComparerTests | Test-spezifisch |
| `CaseInsensitiveNameComparer` | InMemoryDataStore_ComparerTests | Test-spezifisch |

---

## PHASE 3: REDUNDANZEN & KONSOLIDIERUNG

### 🔴 KRITISCH: IdOnlyComparer (5 Duplikate!)

**Gefunden in:**
1. `Runtime/InMemoryDataStore_ComparerTests.cs`
2. `Runtime/InMemoryDataStore_EdgeCaseTests.cs`
3. `Runtime/LocalDataStoreFactory_Tests.cs`
4. `Runtime/DataStoresFacade_ErrorHandlingTests.cs`
5. `Performance/Performance_StressTests.cs`

**Lösung:**
- Konsolidiere zu `KeySelectorEqualityComparer<T,TKey>` (generisch)
- **ALLE** Duplikate ersetzen durch:
```csharp
var comparer = new KeySelectorEqualityComparer<TestItem, int>(x => x.Id);
```

### ⚠️ SlowLoadStrategy vs SlowInitStrategy

**Problem:** Zwei fast identische Klassen
- `SlowLoadStrategy<T>` in RaceConditionTests
- `SlowInitStrategy<T>` in Bootstrap_ErrorRecoveryTests

**Lösung:**
- `SlowLoadStrategy` auslagern nach TestHelper
- `SlowInitStrategy` LÖSCHEN, durch `SlowLoadStrategy` ersetzen

---

## PHASE 4: NEUE PROJEKTSTRUKTUR

### TestHelper.DataStores (neues Projekt)

```
TestHelper.DataStores/
├── Fakes/
│   ├── FakeDataStore.cs
│   └── FakeGlobalStoreRegistry.cs
├── Builders/
│   └── DataStoreBuilder.cs
├── Persistence/
│   ├── FakePersistenceStrategy.cs
│   ├── SlowLoadStrategy.cs
│   └── ThrowingPersistenceStrategy.cs
└── Comparers/
    └── KeySelectorEqualityComparer.cs
```

### Namespaces

```csharp
TestHelper.DataStores.Fakes
TestHelper.DataStores.Builders
TestHelper.DataStores.Persistence
TestHelper.DataStores.Comparers
```

---

## PHASE 5: MIGRATIONS-PLAN

### Schritt 1: Neues Projekt anlegen ✅
```bash
dotnet new classlib -n TestHelper.DataStores -f net8.0
dotnet sln add TestHelper.DataStores/TestHelper.DataStores.csproj
```

### Schritt 2: Projekt-Referenzen ✅
```xml
<!-- TestHelper.DataStores.csproj -->
<ItemGroup>
  <ProjectReference Include="..\DataStores\DataStores.csproj" />
</ItemGroup>
```

### Schritt 3: Dateien verschieben ✅
1. `Builders/DataStoreBuilder.cs` → `TestHelper.DataStores/Builders/`
2. `FakePersistenceStrategy.cs` → `TestHelper.DataStores/Persistence/`
3. `Fakes/FakeDataStore.cs` → `TestHelper.DataStores/Fakes/`
4. `Fakes/FakeGlobalStoreRegistry.cs` → `TestHelper.DataStores/Fakes/`

### Schritt 4: Neue Klassen extrahieren ✅
5. `SlowLoadStrategy<T>` extrahieren → `TestHelper.DataStores/Persistence/`
6. `ThrowingPersistenceStrategy<T>` extrahieren → `TestHelper.DataStores/Persistence/`
7. `KeySelectorEqualityComparer<T,TKey>` NEU erstellen → `TestHelper.DataStores/Comparers/`

### Schritt 5: Tests aktualisieren ✅
- Ersetze `using DataStores.Tests.Fakes;` durch `using TestHelper.DataStores.Fakes;`
- Ersetze `using DataStores.Tests.Builders;` durch `using TestHelper.DataStores.Builders;`
- Ersetze alle `IdOnlyComparer` durch `KeySelectorEqualityComparer<TestItem, int>(x => x.Id)`
- Ersetze `SlowInitStrategy` durch `SlowLoadStrategy`

### Schritt 6: Cleanup ✅
- Lösche leere Ordner (`Builders/`, `Fakes/`)
- Lösche `SlowInitStrategy` aus `Bootstrap_ErrorRecoveryTests.cs`

---

## ZUSAMMENFASSUNG

### ✅ Ausgelagert (7 Klassen)
1. DataStoreBuilder
2. FakePersistenceStrategy
3. FakeDataStore
4. FakeGlobalStoreRegistry
5. SlowLoadStrategy
6. ThrowingPersistenceStrategy
7. KeySelectorEqualityComparer (NEU, konsolidiert 5 Duplikate)

### ❌ Bleibt lokal (11 Klassen)
- Diverse test-spezifische Helper in einzelnen Testdateien

### 🔴 Konsolidiert
- **IdOnlyComparer** (5 Duplikate) → **KeySelectorEqualityComparer** (1 generische Klasse)
- **SlowInitStrategy** → **SlowLoadStrategy** (1 Klasse statt 2)

### 📊 Ergebnis
- **Reduzierung von Duplikaten:** 5 → 0
- **Wiederverwendbare Helper:** 7 zentrale Klassen
- **Generische Lösung:** KeySelectorEqualityComparer für alle ID-basierten Vergleiche

---

**Bereit für Umsetzung:** ✅  
**Alle Ziele erfüllt:** ✅
