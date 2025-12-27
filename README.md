# DataStores Solution

Eine leistungsstarke .NET 8 Bibliothek für die Verwaltung von In-Memory-Datenspeichern mit optionaler Persistierung und hierarchischen Beziehungen.

## Projekte in dieser Solution

### [DataStores](DataStores/README.md)
Die Hauptbibliothek für typsichere In-Memory-Datenspeicherverwaltung mit umfassender Unterstützung für:
- Thread-sichere Datenspeicher
- Optionale Persistierung (JSON, LiteDB)
- Event-basierte Änderungsbenachrichtigungen
- Dependency Injection Integration
- Eltern-Kind-Beziehungen zwischen Entitäten

**➡️ [Zur DataStores Dokumentation](DataStores/README.md)**

### TestHelper.DataStores
Test-Utilities und Builders für DataStores-basierte Anwendungen:
- Fluent Builders für Test-Stores
- Fixtures für Integrationstests
- Mock- und Fake-Implementierungen

## Schnellstart

### 1. Repository klonen

```bash
git clone https://github.com/ReneRose1971/DataStores.git
cd DataStores
```

### 2. Solution bauen

```bash
dotnet build
```

### 3. Tests ausführen

```bash
dotnet test
```

## Technologie-Stack

- **.NET 8** - Target Framework
- **C# 12** - Programmiersprache
- **Microsoft.Extensions.DependencyInjection** - Dependency Injection
- **LiteDB 5.0.21+** - Optionale LiteDB-Persistierung
- **System.Text.Json** - JSON-Persistierung
- **xUnit 2.5.3** - Test Framework

## Anforderungen

- .NET 8.0 SDK oder höher
- Visual Studio 2022 (17.8+) oder VS Code mit C# Extension

## Weiterführende Dokumentation

- **[DataStores README](DataStores/README.md)** - Hauptprojekt-Dokumentation
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Contribution Guidelines
- **[API Referenz](DataStores/Docs/API-Reference.md)** - Vollständige API-Dokumentation
- **[Verwendungsbeispiele](DataStores/Docs/Usage-Examples.md)** - Praktische Beispiele
- **[Persistierung Guide](DataStores/Docs/Persistence-Guide.md)** - Persistierungs-Strategien

## Beitragen

Contributions sind willkommen! Bitte beachten Sie:
1. Alle Tests müssen erfolgreich durchlaufen (`dotnet test`)
2. Code folgt den Coding Guidelines (siehe `.editorconfig` und `CONTRIBUTING.md`)
3. Neue Features benötigen Tests und deutsche Dokumentation

**➡️ [Zum Contribution Guide](CONTRIBUTING.md)**

## Lizenz

MIT License - siehe [LICENSE](LICENSE) Datei für Details.

---

**Repository**: https://github.com/ReneRose1971/DataStores  
**Autor**: René Rose  
**Letzte Aktualisierung**: Januar 2025
