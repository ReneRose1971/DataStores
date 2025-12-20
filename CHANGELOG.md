# Changelog

Alle bemerkenswerten Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
und dieses Projekt folgt [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]

### Geplant
- NuGet Package Veröffentlichung
- Beispiel-Anwendungen
- Benchmarks

## [1.0.0] - 2025-01-20

### Hinzugefügt
- Initiales Release der DataStores-Bibliothek
- `InMemoryDataStore<T>` - Thread-sichere In-Memory-Implementierung
- `GlobalStoreRegistry` - Verwaltung globaler Singleton-Stores
- `DataStoresFacade` - Zentrale Facade für Store-Zugriff
- `PersistentStoreDecorator<T>` - Optionale Persistierung mit Auto-Load/Save
- `ParentChildRelationship<TParent, TChild>` - Eltern-Kind-Beziehungen
- `IPersistenceStrategy<T>` - Interface für benutzerdefinierte Persistierung
- Bootstrap-System mit `DataStoreBootstrap` und `IDataStoreRegistrar`
- Dependency Injection Integration via `ServiceCollectionExtensions`
- Umfassende Event-basierte Änderungsbenachrichtigungen
- Support für Custom `IEqualityComparer<T>`
- `SynchronizationContext` Support für UI-Threads (WPF, WinForms, MAUI)
- Vollständige deutsche XML-Dokumentation
- Über 100 Unit- und Integrationstests
- Thread-Safety-Tests und Performance-Tests
- Umfassende Dokumentation:
  - API-Referenz
  - Formale Spezifikationen & Invarianten
  - Verwendungsbeispiele
  - Persistierung Guide
  - Beziehungen Guide

### Features
- ? Thread-sichere Operationen auf allen Stores
- ? Globale (Singleton) und lokale (isolierte) Stores
- ? Snapshots mit optionalen Filtern
- ? Asynchrone Persistierung mit Race-Condition-Schutz
- ? Bulk-Operationen für Performance
- ? Explizite Invarianten und Verhaltensgarantien

### Technologie
- Target Framework: .NET 8.0
- Test Framework: xUnit 2.5.3
- Dependencies: Microsoft.Extensions.DependencyInjection.Abstractions 10.0.1

---

## Änderungstypen

- `Hinzugefügt` für neue Features
- `Geändert` für Änderungen an bestehender Funktionalität
- `Veraltet` für Features, die bald entfernt werden
- `Entfernt` für entfernte Features
- `Behoben` für Bugfixes
- `Sicherheit` für Sicherheits-relevante Änderungen

[Unreleased]: https://github.com/[username]/DataStores/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/[username]/DataStores/releases/tag/v1.0.0
