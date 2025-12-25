# DataStores Solution

Eine leistungsstarke .NET 8 Bibliothek für die Verwaltung von In-Memory-Datenspeichern mit Unterstützung für Persistierung, globale und lokale Stores sowie Eltern-Kind-Beziehungen.

## Übersicht

Diese Solution stellt die **DataStores**-Bibliothek bereit - eine flexible und erweiterbare Lösung zum Verwalten von typsicheren Datensammlungen im Speicher.

### Hauptfunktionen

- Thread-sichere In-Memory-Datenspeicher
- Globale und lokale Datenspeicher-Konzepte
- Optionale Persistierung mit asynchronen Strategien
- Eltern-Kind-Beziehungen zwischen Datensammlungen
- Dependency Injection Integration
- Event-basierte Änderungsbenachrichtigungen

[Zur vollständigen DataStores Dokumentation](DataStores/README.md)

## Schnellstart

### Installation

```bash
# Repository klonen
git clone https://github.com/ReneRose1971/DataStores.git

# In das Verzeichnis wechseln
cd DataStores

# Solution erstellen
dotnet build
```

### Grundlegende Verwendung

```csharp
using DataStores.Abstractions;
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

// 1. Dependency Injection einrichten
var services = new ServiceCollection();
var module = new DataStoresServiceModule();
module.Register(services);

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
- **Microsoft.Extensions.DependencyInjection** - Dependency Injection
- **LiteDB** - Optionale LiteDB-Persistierung
- **System.Text.Json** - Optionale JSON-Persistierung

## Weitere Dokumentation

- [DataStores Projekt README](DataStores/README.md)
  - [API Referenz](DataStores/Docs/API-Reference.md)
  - [Formale Spezifikationen & Invarianten](DataStores/Docs/Formal-Specifications.md)
  - [Verwendungsbeispiele](DataStores/Docs/Usage-Examples.md)
  - [Persistierung Guide](DataStores/Docs/Persistence-Guide.md)
  - [Beziehungen Guide](DataStores/Docs/Relations-Guide.md)
  - [LiteDB Integration](DataStores/Docs/LiteDB-Integration.md)
  - [Registrar Best Practices](DataStores/Docs/Registrar-Best-Practices.md)

## Tests

Die Solution verfügt über eine umfassende Testsuite mit über 370 Tests, die alle Aspekte der Bibliothek abdecken:

```bash
# Alle Tests ausführen
dotnet test

# Nur Integration-Tests
dotnet test --filter "Category=Integration"

# Mit Code Coverage
dotnet test /p:CollectCoverage=true
```

## Beitragen

Beiträge sind willkommen! Bitte stellen Sie sicher, dass:
1. Alle Tests erfolgreich durchlaufen (`dotnet test`)
2. Neue Funktionen mit Tests abgedeckt sind
3. Code den bestehenden Stil folgt
4. Deutsche Dokumentation hinzugefügt wird

Weitere Informationen finden Sie in [CONTRIBUTING.md](CONTRIBUTING.md).

## Lizenz

MIT License - siehe [LICENSE](LICENSE) Datei für Details.

## Autor

René Rose

---

**Letzte Aktualisierung**: Januar 2025
