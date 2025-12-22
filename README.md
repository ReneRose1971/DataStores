# DataStores Solution

Eine leistungsstarke .NET 8 Bibliothek für die Verwaltung von In-Memory-Datenspeichern mit Unterstützung für Persistierung, globale und lokale Stores sowie Eltern-Kind-Beziehungen.

## Übersicht

Diese Solution besteht aus zwei Hauptprojekten:

### 1. **DataStores** - Kernbibliothek
Eine flexible und erweiterbare Bibliothek zum Verwalten von typsicheren Datensammlungen im Speicher. Die Bibliothek bietet:
- Thread-sichere In-Memory-Datenspeicher
- Globale und lokale Datenspeicher-Konzepte
- Optionale Persistierung mit asynchronen Strategien
- Eltern-Kind-Beziehungen zwischen Datensammlungen
- Dependency Injection Integration
- Event-basierte Änderungsbenachrichtigungen

[Zur DataStores Dokumentation](DataStores/README.md)

### 2. **DataStores.Tests** - Unit Tests
Umfassende Testsuite mit über 100 Tests zur Sicherstellung der Qualität und Zuverlässigkeit:
- Unit Tests für alle Kernkomponenten
- Nebenläufigkeits- und Thread-Sicherheitstests
- Performance- und Stress-Tests
- Integrationstests für End-to-End-Szenarien
- Edge-Case- und Fehlerbehandlungstests

[Zur DataStores.Tests Projekt README](DataStores.Tests/README.md)

## Schnellstart

### Installation

```bash
# Klonen Sie das Repository
git clone https://github.com/ReneRose1971/DataStores.git

# Wechseln Sie in das Verzeichnis
cd DataStores

# Erstellen Sie die Solution
dotnet build
```

### Grundlegende Verwendung

```csharp
using DataStores.Abstractions;
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using Common.Bootstrap;

// 1. Dependency Injection einrichten mit ServiceModule-Pattern
var services = new ServiceCollection();

// Automatische Registrierung via AddModulesFromAssemblies
services.AddModulesFromAssemblies(
    typeof(DataStoresServiceModule).Assembly);

// Oder manuell:
// var module = new DataStoresServiceModule();
// module.Register(services);

// 2. Registrar hinzufügen
services.AddDataStoreRegistrar<MyDataStoreRegistrar>();

var provider = services.BuildServiceProvider();

// 3. Bootstrap ausführen
await DataStoreBootstrap.RunAsync(provider);

// 4. DataStores verwenden
var stores = provider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();

// 5. Daten hinzufügen
productStore.Add(new Product { Id = 1, Name = "Laptop" });

// 6. Auf Änderungen reagieren
productStore.Changed += (sender, args) => 
{
    Console.WriteLine($"Store geändert: {args.ChangeType}");
};
```

## Architektur

```
┌─────────────────────────────────────────────────────────┐
│                    IDataStores (Facade)                  │
│  - GetGlobal<T>()                                        │
│  - CreateLocal<T>()                                      │
│  - CreateLocalSnapshotFromGlobal<T>()                    │
└───────────────┬─────────────────┬───────────────────────┘
                │                 │
    ┌───────────▼──────────┐   ┌──▼──────────────────────┐
    │ GlobalStoreRegistry  │   │ LocalDataStoreFactory   │
    │ (Thread-safe)        │   │                         │
    └──────────┬───────────┘   └─────────────────────────┘
               │
    ┌──────────▼──────────────────────────────────────────┐
    │          InMemoryDataStore<T>                       │
    │  - Thread-sicher                                    │
    │  - Event-basiert                                    │
    │  - Anpassbare Comparer                              │
    └──────────┬──────────────────────────────────────────┘
               │
    ┌──────────▼──────────────────────────────────────────┐
    │     PersistentStoreDecorator<T> (Optional)          │
    │  - Auto-Load                                        │
    │  - Auto-Save                                        │
    │  - Async Persistence                                │
    └─────────────────────────────────────────────────────┘
```

## Projektstruktur

```
DataStores/
├── DataStores/                    # Hauptbibliothek
│   ├── Abstractions/              # Interfaces und Basisklassen
│   ├── Runtime/                   # Laufzeit-Implementierungen
│   ├── Persistence/               # Persistierung-Funktionalität
│   ├── Relations/                 # Eltern-Kind-Beziehungen
│   ├── Bootstrap/                 # Initialisierung und DI
│   ├── Docs/                      # Ausführliche Dokumentation
│   └── README.md                  # Projekt-Dokumentation
│
├── DataStores.Tests/              # Test-Projekt
│   ├── Runtime/                   # Runtime-Tests
│   ├── Persistence/               # Persistierung-Tests
│   ├── Relations/                 # Beziehungs-Tests
│   ├── Integration/               # End-to-End-Tests
│   ├── Performance/               # Performance-Tests
│   └── README.md                  # Test-Dokumentation
│
└── README.md                      # Diese Datei
```

## Hauptfunktionen

### Globale vs. Lokale Stores
- **Globale Stores**: Singleton-Instanzen, die über die gesamte Anwendung geteilt werden
- **Lokale Stores**: Isolierte Instanzen für spezifische Anwendungsfälle (z.B. Dialog-/Formularkontext)
- **Snapshots**: Erstellen Sie lokale Kopien von globalen Stores mit optionalen Filtern

### Persistierung
- Asynchrone Lade- und Speichervorgänge
- Auto-Load beim Bootstrap
- Auto-Save bei Änderungen
- Benutzerdefinierte Persistierungsstrategien
- Fehlertoleranz und Race-Condition-Handling

### Eltern-Kind-Beziehungen
- Definieren Sie hierarchische Beziehungen zwischen Entitäten
- Automatische Filterung von Kind-Elementen
- Lazy Loading und Snapshot-Unterstützung
- Refresh-Mechanismus für aktuelle Daten

### Thread-Sicherheit
- Alle Operationen sind thread-sicher
- Lock-basierte Synchronisation
- Optionale SynchronizationContext-Unterstützung für UI-Threads
- Getestet mit Stress-Tests und Nebenläufigkeits-Szenarien

## Technologien

- **.NET 8** - Target Framework
- **C# 12** - Programmiersprache
- **xUnit** - Test-Framework
- **Microsoft.Extensions.DependencyInjection** - Dependency Injection

## Weitere Dokumentation

- [DataStores Projekt README](DataStores/README.md)
  - [API Referenz](DataStores/Docs/API-Reference.md)
  - [Formale Spezifikationen & Invarianten](DataStores/Docs/Formal-Specifications.md)
  - [Verwendungsbeispiele](DataStores/Docs/Usage-Examples.md)
  - [Persistierung Guide](DataStores/Docs/Persistence-Guide.md)
  - [Beziehungen Guide](DataStores/Docs/Relations-Guide.md)

- [DataStores.Tests Projekt README](DataStores.Tests/README.md)

## Beitragen

Beiträge sind willkommen! Bitte stellen Sie sicher, dass:
1. Alle Tests erfolgreich durchlaufen (`dotnet test`)
2. Neue Funktionen mit Tests abgedeckt sind
3. Code den bestehenden Stil folgt
4. Deutsche Dokumentation hinzugefügt wird

## Lizenz

[Ihre Lizenz hier einfügen]

## Autor

[Ihre Informationen hier einfügen]

---

**Letzte Aktualisierung**: Januar 2025
