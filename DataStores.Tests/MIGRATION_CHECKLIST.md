# PHASE 3: TEST-AKTUALISIERUNG - Migrations-Checkliste

## Status: TestHelper.DataStores Projekt erstellt ✅

### Bereits erledigt ✅
- [x] Neues Projekt `TestHelper.DataStores` erstellt
- [x] Projekt-Referenz auf `DataStores` hinzugefügt
- [x] Ordnerstruktur erstellt (Fakes, Builders, Persistence, Comparers)
- [x] 7 Klassen nach TestHelper.DataStores migriert:
  - [x] DataStoreBuilder
  - [x] FakeDataStore
  - [x] FakeGlobalStoreRegistry
  - [x] FakePersistenceStrategy
  - [x] SlowLoadStrategy (extrahiert)
  - [x] ThrowingPersistenceStrategy (extrahiert)
  - [x] KeySelectorEqualityComparer (NEU, generisch)
- [x] Referenz von DataStores.Tests → TestHelper.DataStores hinzugefügt
- [x] Build erfolgreich

---

## TODO: Alte Dateien löschen

### Zu löschende Dateien in DataStores.Tests:
- [ ] `Builders/DataStoreBuilder.cs`
- [ ] `FakePersistenceStrategy.cs`
- [ ] `Fakes/FakeDataStore.cs`
- [ ] `Fakes/FakeGlobalStoreRegistry.cs`

### Zu löschende Ordner (wenn leer):
- [ ] `Builders/`
- [ ] `Fakes/`

---

## TODO: Using-Statements aktualisieren

### Zu ersetzende Namespaces:

| Alt | Neu |
|-----|-----|
| `using DataStores.Tests.Builders;` | `using TestHelper.DataStores.Builders;` |
| `using DataStores.Tests.Fakes;` | `using TestHelper.DataStores.Fakes;` |
| `using DataStores.Tests;` (für FakePersistenceStrategy) | `using TestHelper.DataStores.Persistence;` |

### Betroffene Dateien (Suche nach "using DataStores.Tests"):
- [ ] Alle Tests die FakeDataStore verwenden
- [ ] Alle Tests die FakeGlobalStoreRegistry verwenden
- [ ] Alle Tests die FakePersistenceStrategy verwenden
- [ ] Alle Tests die DataStoreBuilder verwenden

---

## TODO: IdOnlyComparer ersetzen

### 5 Duplikate zu ersetzen mit KeySelectorEqualityComparer:

**Pattern:**
```csharp
// ALT:
private class IdOnlyComparer : IEqualityComparer<TestItem>
{
    public bool Equals(TestItem? x, TestItem? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return x.Id == y.Id;
    }
    public int GetHashCode(TestItem obj) => obj.Id.GetHashCode();
}

// Verwendung:
var comparer = new IdOnlyComparer();

// NEU:
using TestHelper.DataStores.Comparers;

// Verwendung:
var comparer = new KeySelectorEqualityComparer<TestItem, int>(x => x.Id);
```

#### Dateien mit IdOnlyComparer:
1. [ ] `Runtime/InMemoryDataStore_ComparerTests.cs`
   - Private class löschen
   - Using hinzufügen: `using TestHelper.DataStores.Comparers;`
   - Alle `new IdOnlyComparer()` ersetzen durch `new KeySelectorEqualityComparer<TestItem, int>(x => x.Id)`

2. [ ] `Runtime/InMemoryDataStore_EdgeCaseTests.cs`
   - Private class löschen
   - Using hinzufügen
   - Ersetzung

3. [ ] `Runtime/LocalDataStoreFactory_Tests.cs`
   - Private class löschen
   - Using hinzufügen
   - Ersetzung

4. [ ] `Runtime/DataStoresFacade_ErrorHandlingTests.cs`
   - Private class löschen
   - Using hinzufügen
   - Ersetzung

5. [ ] `Performance/Performance_StressTests.cs`
   - Private class löschen
   - Using hinzufügen
   - Ersetzung

---

## TODO: Private Helper-Klassen aktualisieren

### SlowLoadStrategy / SlowInitStrategy Konsolidierung

1. [ ] `Persistence/PersistentStoreDecorator_RaceConditionTests.cs`
   - Private class `SlowLoadStrategy<T>` löschen
   - Using hinzufügen: `using TestHelper.DataStores.Persistence;`
   - Alle Verwendungen funktionieren weiterhin (gleiche Signatur)

2. [ ] `Persistence/PersistentStoreDecorator_RaceConditionTests.cs`
   - Private class `ThrowingPersistenceStrategy<T>` löschen
   - Using hinzufügen: `using TestHelper.DataStores.Persistence;`
   - Alle Verwendungen funktionieren weiterhin (gleiche Signatur)

3. [ ] `Bootstrap/DataStoreBootstrap_ErrorRecoveryTests.cs`
   - Private class `SlowInitStrategy<T>` löschen
   - Ersetze alle `new SlowInitStrategy<...>(delay)` durch `new SlowLoadStrategy<...>(delay, Array.Empty<...>())`
   - Using hinzufügen: `using TestHelper.DataStores.Persistence;`

---

## TODO: Build & Test Verifikation

### Nach allen Änderungen:

1. [ ] Build der gesamten Solution
   ```bash
   dotnet build
   ```

2. [ ] Alle Tests ausführen
   ```bash
   dotnet test
   ```

3. [ ] Spezifisch: Tests mit geänderten Comparern
   ```bash
   dotnet test --filter "FullyQualifiedName~Comparer"
   ```

4. [ ] Spezifisch: Tests mit PersistenceStrategy
   ```bash
   dotnet test --filter "FullyQualifiedName~Persistence"
   ```

---

## Erwartetes Ergebnis

### Vorher:
- FakePersistenceStrategy in `/` (Root)
- Fakes/FakeDataStore.cs
- Fakes/FakeGlobalStoreRegistry.cs
- Builders/DataStoreBuilder.cs
- 5x IdOnlyComparer (Duplikate)
- 2x SlowLoadStrategy (Duplikate)
- 2x ThrowingPersistenceStrategy (in Tests als private class)

### Nachher:
- TestHelper.DataStores/Persistence/FakePersistenceStrategy.cs
- TestHelper.DataStores/Fakes/FakeDataStore.cs
- TestHelper.DataStores/Fakes/FakeGlobalStoreRegistry.cs
- TestHelper.DataStores/Builders/DataStoreBuilder.cs
- TestHelper.DataStores/Comparers/KeySelectorEqualityComparer.cs (NEU)
- TestHelper.DataStores/Persistence/SlowLoadStrategy.cs
- TestHelper.DataStores/Persistence/ThrowingPersistenceStrategy.cs
- Keine Duplikate mehr
- Alle Tests grün

---

## Nächste Schritte

Die manuelle Arbeit beginnt jetzt mit dem Aktualisieren der Using-Statements und dem Ersetzen der Duplikate.

**Empfohlene Reihenfolge:**
1. Alte Dateien löschen (Builders, Fakes, FakePersistenceStrategy)
2. Using-Statements global suchen & ersetzen
3. IdOnlyComparer-Duplikate ersetzen (Datei für Datei)
4. SlowLoadStrategy/ThrowingPersistenceStrategy private classes entfernen
5. Build + Tests

**Zeitaufwand geschätzt:** 30-45 Minuten manuelle Arbeit
