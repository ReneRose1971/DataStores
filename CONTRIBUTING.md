# Beitragen zu DataStores

Vielen Dank für Ihr Interesse, zu DataStores beizutragen! Wir freuen uns über jeden Beitrag zur Verbesserung dieser Bibliothek.

## Inhaltsverzeichnis

- [Code of Conduct](#code-of-conduct)
- [Wie kann ich beitragen?](#wie-kann-ich-beitragen)
- [Entwicklungsumgebung einrichten](#entwicklungsumgebung-einrichten)
- [Prozess für Pull Requests](#prozess-für-pull-requests)
- [Coding Guidelines](#coding-guidelines)
- [Commit-Nachrichten](#commit-nachrichten)
- [Tests](#tests)
- [Dokumentation](#dokumentation)

## Code of Conduct

Dieses Projekt folgt dem [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). Durch Ihre Teilnahme verpflichten Sie sich, diesen einzuhalten.

## Wie kann ich beitragen?

### Bugs melden

Bugs werden als [GitHub Issues](https://github.com/ReneRose1971/DataStores/issues) getrackt. Erstellen Sie ein Issue und geben Sie folgende Informationen an:

- **Aussagekräftiger Titel** - Verwenden Sie einen klaren und beschreibenden Titel
- **Schritte zur Reproduktion** - Detaillierte Schritte, um das Problem nachzustellen
- **Erwartetes Verhalten** - Was sollte passieren
- **Tatsächliches Verhalten** - Was passiert stattdessen
- **Umgebung** - .NET Version, Betriebssystem, etc.
- **Zusätzlicher Kontext** - Screenshots, Logs, etc.

**Vorlage für Bug-Reports:**
```markdown
## Beschreibung
[Klare und präzise Beschreibung des Bugs]

## Schritte zur Reproduktion
1. Erstelle einen Store mit '...'
2. Führe Operation '...' aus
3. Beobachte Fehler

## Erwartetes Verhalten
[Was sollte passieren]

## Tatsächliches Verhalten
[Was passiert tatsächlich]

## Umgebung
- DataStores Version: [z.B. 1.0.0]
- .NET Version: [z.B. .NET 8.0]
- OS: [z.B. Windows 11, Ubuntu 22.04]

## Zusätzlicher Kontext
[Logs, Screenshots, etc.]
```

### Features vorschlagen

Feature-Vorschläge werden ebenfalls als [GitHub Issues](https://github.com/ReneRose1971/DataStores/issues) getrackt.

**Vorlage für Feature-Requests:**
```markdown
## Problem
[Welches Problem löst dieses Feature?]

## Vorgeschlagene Lösung
[Wie könnte das Feature implementiert werden?]

## Alternativen
[Welche alternativen Lösungen wurden erwogen?]

## Zusätzlicher Kontext
[Weitere Informationen, Beispiele, etc.]
```

### Code beitragen

1. Forken Sie das Repository
2. Erstellen Sie einen Feature-Branch (`git checkout -b feature/AmazingFeature`)
3. Committen Sie Ihre Änderungen (`git commit -m 'Add some AmazingFeature'`)
4. Pushen Sie zum Branch (`git push origin feature/AmazingFeature`)
5. Öffnen Sie einen Pull Request

## Entwicklungsumgebung einrichten

### Voraussetzungen

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) oder höher
- IDE Ihrer Wahl:
  - [Visual Studio 2022](https://visualstudio.microsoft.com/) (17.8+)
  - [Visual Studio Code](https://code.visualstudio.com/) mit C# Extension
  - [JetBrains Rider](https://www.jetbrains.com/rider/)

### Setup

```bash
# Repository klonen
git clone https://github.com/ReneRose1971/DataStores.git
cd DataStores

# Dependencies installieren
dotnet restore

# Projekt bauen
dotnet build

# Tests ausführen
dotnet test

# Coverage Report erstellen
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Projektstruktur verstehen

```
DataStores/
├── DataStores/              # Hauptbibliothek
│   ├── Abstractions/        # Interfaces und Basisklassen
│   ├── Runtime/             # Implementierungen
│   ├── Persistence/         # Persistierung
│   ├── Relations/           # Beziehungen
│   ├── Bootstrap/           # Initialisierung
│   └── Docs/                # Dokumentation
│
└── DataStores.Tests/        # Tests
    ├── Runtime/
    ├── Persistence/
    ├── Relations/
    ├── Integration/
    └── Performance/
```

## Prozess für Pull Requests

### Vor dem PR

- [ ] Code folgt den [Coding Guidelines](#coding-guidelines)
- [ ] Alle Tests laufen durch (`dotnet test`)
- [ ] Neue Tests für neue Funktionalität hinzugefügt
- [ ] Dokumentation aktualisiert (XML-Kommentare, README, etc.)
- [ ] CHANGELOG.md aktualisiert
- [ ] Code Coverage nicht verringert

### PR Template

```markdown
## Beschreibung
[Was ändert dieser PR?]

## Motivation und Kontext
[Warum ist diese Änderung notwendig?]

## Typ der Änderung
- [ ] Bug Fix (nicht-breaking change)
- [ ] Neues Feature (nicht-breaking change)
- [ ] Breaking Change (fix oder feature, das existierende Funktionalität bricht)
- [ ] Dokumentation

## Checkliste
- [ ] Code folgt Coding Guidelines
- [ ] Selbst-Review durchgeführt
- [ ] Kommentare hinzugefügt, besonders in komplexen Bereichen
- [ ] Dokumentation aktualisiert
- [ ] Keine neuen Warnings
- [ ] Tests hinzugefügt
- [ ] Alle Tests laufen durch
- [ ] CHANGELOG.md aktualisiert

## Tests
[Wie wurde getestet?]

## Screenshots (falls relevant)
[Screenshots hinzufügen]
```

### Review-Prozess

1. Mindestens ein Maintainer muss den PR reviewen
2. Alle Kommentare müssen adressiert werden
3. CI-Pipeline muss grün sein
4. Nach Approval: Squash & Merge

## Coding Guidelines

### C# Konventionen

Wir folgen den [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

**Wichtigste Regeln:**

```csharp
// OK: PascalCase für öffentliche Member
public class DataStoreRegistry { }
public void RegisterGlobal<T>() { }

// OK: camelCase für private Fields mit Underscore
private readonly IDataStore<T> _innerStore;

// OK: Interfaces beginnen mit 'I'
public interface IDataStore<T> { }

// OK: Async-Methoden enden mit 'Async'
public async Task InitializeAsync() { }

// OK: Vollständige XML-Kommentare auf Deutsch
/// <summary>
/// Registriert einen globalen Datenspeicher für Typ T.
/// </summary>
/// <param name="store">Der zu registrierende Datenspeicher.</param>
public void RegisterGlobal<T>(IDataStore<T> store) { }
```

### Code-Stil

```csharp
// OK: Verwende var, wenn Typ offensichtlich
var store = new InMemoryDataStore<Product>();

// OK: Expliziter Typ, wenn nicht offensichtlich
IDataStore<Product> store = GetStore();

// OK: Expression-bodied Members für einfache Getter
public IReadOnlyList<T> Items => _items.ToList();

// OK: Null-Checks
if (store == null)
    throw new ArgumentNullException(nameof(store));

// oder
var items = store?.Items ?? Array.Empty<T>();

// OK: LINQ für Sammlungen
var activeProducts = products.Where(p => p.IsActive).ToList();
```

### Architektur-Prinzipien

1. **SOLID-Prinzipien** befolgen
2. **Dependency Injection** verwenden
3. **Interfaces über Implementierungen** bevorzugen
4. **Thread-Sicherheit** garantieren wo nötig
5. **Immutability** wo möglich
6. **Explizite Invarianten** dokumentieren

## Commit-Nachrichten

Wir folgen [Conventional Commits](https://www.conventionalcommits.org/):

```
<typ>[optionaler scope]: <beschreibung>

[optionaler body]

[optionale footer]
```

### Typen

- `feat`: Neues Feature
- `fix`: Bug Fix
- `docs`: Nur Dokumentation
- `style`: Code-Formatierung
- `refactor`: Code-Refactoring
- `perf`: Performance-Verbesserung
- `test`: Tests hinzufügen/ändern
- `chore`: Build-Prozess, Tools, etc.

### Beispiele

```bash
# Feature
feat(persistence): Add JSON persistence strategy
feat(store): Add bulk remove operation

# Bug Fix
fix(registry): Fix race condition in RegisterGlobal
fix(events): Ensure events fire on correct thread

# Dokumentation
docs(api): Add formal specifications document
docs(readme): Update installation instructions

# Breaking Change
feat(store)!: Change Items property to return IReadOnlyList

BREAKING CHANGE: Items now returns IReadOnlyList instead of IEnumerable
```

## Tests

### Test-Struktur

```csharp
/// <summary>
/// Tests für [Klasse/Feature].
/// </summary>
public class MyFeatureTests
{
    [Fact]
    public void MethodName_Should_ExpectedBehavior_When_Condition()
    {
        // Arrange
        var store = new InMemoryDataStore<Product>();
        var item = new Product { Id = 1 };
        
        // Act
        store.Add(item);
        
        // Assert
        Assert.Single(store.Items);
    }
}
```

### Test-Kategorien

- **Unit Tests**: Isolierte Tests einzelner Komponenten
- **Integration Tests**: Tests von Komponenten-Interaktionen
- **Thread-Safety Tests**: Nebenläufigkeits-Tests
- **Performance Tests**: Stress-Tests und Benchmarks

### Test Coverage

- **Minimum**: 80% Code Coverage
- **Ziel**: 90%+ Code Coverage
- Alle öffentlichen APIs müssen getestet sein
- Kritische Pfade müssen 100% abgedeckt sein

### Tests ausführen

```bash
# Alle Tests
dotnet test

# Spezifische Kategorie
dotnet test --filter "FullyQualifiedName~Runtime"

# Mit Coverage
dotnet test /p:CollectCoverage=true

# Einzelner Test
dotnet test --filter "FullyQualifiedName~MySpecificTest"
```

## Dokumentation

### XML-Kommentare

**Alle öffentlichen APIs MÜSSEN vollständige XML-Kommentare auf Deutsch haben:**

```csharp
/// <summary>
/// Fügt ein Element zum Store hinzu.
/// </summary>
/// <param name="item">Das hinzuzufügende Element.</param>
/// <exception cref="ArgumentNullException">
/// Wird ausgelöst, wenn <paramref name="item"/> null ist.
/// </exception>
/// <remarks>
/// Diese Methode ist thread-sicher. Das Element wird zur Liste hinzugefügt
/// und ein <see cref="Changed"/> Event wird ausgelöst.
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryDataStore&lt;Product&gt;();
/// store.Add(new Product { Id = 1, Name = "Laptop" });
/// </code>
/// </example>
public void Add(T item)
{
    // Implementierung
}
```

### Markdown-Dokumentation

- **README.md**: Übersicht und Schnellstart aktualisieren
- **CHANGELOG.md**: Alle Änderungen dokumentieren
- **Docs/**: Detaillierte Guides bei Bedarf erweitern

### Beispiele

Neue Features sollten mit praktischen Beispielen dokumentiert werden:

```markdown
## Neues Feature: XYZ

### Verwendung

```csharp
var example = new Example();
example.DoSomething();
```

### Wann verwenden?

- Szenario 1
- Szenario 2
```

## Fragen?

Wenn Sie Fragen haben:

- Öffnen Sie ein [GitHub Issue](https://github.com/ReneRose1971/DataStores/issues)
- Kontaktieren Sie die Maintainer
- Schauen Sie in die [Diskussionen](https://github.com/ReneRose1971/DataStores/discussions)

## Lizenz

Durch Ihren Beitrag stimmen Sie zu, dass Ihre Beiträge unter der [MIT License](LICENSE) lizenziert werden.

---

**Vielen Dank für Ihren Beitrag!**
