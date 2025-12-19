# DataStores - GitHub Setup & Push

## ?? Schnell-Setup (GitHub CLI)

```bash
# 1. Git initialisieren
git init

# 2. Alle Dateien stagen
git add .

# 3. Initial Commit
git commit -m "?? Initial commit: DataStores - Thread-safe In-Memory Data Store Framework for .NET 8

Implementiert eine umfassende, production-ready Datenspeicher-Bibliothek mit folgenden Features:

? Features:
- Thread-sichere In-Memory-Stores mit Lock-basierter Synchronisation
- Globale & lokale Store-Verwaltung via Facade-Pattern
- Persistenz-Support durch Decorator-Pattern
- Parent-Child-Beziehungen mit flexiblen Filtern
- DI-Integration (Microsoft.Extensions.DependencyInjection)
- Event-System für Change-Notifications
- SynchronizationContext-Support für UI-Thread-Marshaling
- Snapshot-Isolation für unabhängige Datensichten

?? Projektstruktur:
- DataStores.Abstractions: Core Interfaces & Contracts
- DataStores.Runtime: InMemoryDataStore, Registry & Facade
- DataStores.Persistence: Persistenz-Decorator & Strategien
- DataStores.Relations: Parent-Child-Relationship-Management
- DataStores.Bootstrap: DI-Integration & Initialization
- DataStores.Tests: 212 Tests mit ~98% Coverage

?? Testing:
- 212 Unit-, Integration- & Performance-Tests
- ~98% Code-Coverage
- Thread-Safety validiert
- Memory-Leak-frei
- Stress-Tests für 10.000+ Items

?? Dokumentation:
- Vollständige XML-Kommentare (Deutsch)
- Solution & Projekt READMEs
- API-Referenzen
- Code-Beispiele & Best Practices
- Test-Reports

??? Architektur:
- Clean Architecture-Prinzipien
- SOLID-Patterns
- Decorator statt Vererbung
- Explicit > Implicit
- Testability First

?? Target: .NET 8.0
?? License: MIT"

# 4. GitHub Repository erstellen (öffentlich)
gh repo create DataStores --public --source=. --description="Thread-safe In-Memory Data Store Framework for .NET 8 with persistence, relations, and DI integration"

# 5. Push to GitHub
git push -u origin main
```

---

## ?? Alternative: Via GitHub Web UI

Falls GitHub CLI nicht verfügbar:

### Schritt 1: Repository auf GitHub erstellen

1. Gehe zu https://github.com/new
2. **Repository Name:** `DataStores`
3. **Description:** `Thread-safe In-Memory Data Store Framework for .NET 8 with persistence, relations, and DI integration`
4. **Visibility:** Public ?
5. **Initialize:** NICHT anklicken (kein README, .gitignore, License - haben wir schon!)
6. Klicke **Create repository**

### Schritt 2: Lokales Repo verbinden & pushen

```bash
# Im Projekt-Verzeichnis
cd C:\Users\schro\source\repos\DataStores

# Git initialisieren
git init

# Alle Dateien stagen
git add .

# Initial Commit (siehe Nachricht oben)
git commit -m "?? Initial commit: DataStores - Thread-safe In-Memory Data Store Framework for .NET 8"

# Branch umbenennen zu main (falls nötig)
git branch -M main

# Remote hinzufügen (ERSETZE 'IHR-USERNAME' mit Ihrem GitHub-Username!)
git remote add origin https://github.com/IHR-USERNAME/DataStores.git

# Push
git push -u origin main
```

---

## ?? Empfohlene GitHub-Konfiguration

Nach dem Push:

### 1. Repository-Einstellungen

**About-Sektion:**
- Description: "Thread-safe In-Memory Data Store Framework for .NET 8"
- Website: (optional)
- Topics: `dotnet`, `csharp`, `datastore`, `in-memory`, `thread-safe`, `persistence`, `dependency-injection`, `net8`

**Features:**
- ? Issues
- ? Wiki (optional für erweiterte Docs)
- ? Discussions (optional für Community)

### 2. Branch Protection (main)

**Settings ? Branches ? Add branch protection rule:**
- Branch name pattern: `main`
- ? Require pull request reviews before merging
- ? Require status checks to pass before merging
- ? Require conversation resolution before merging

### 3. GitHub Actions (CI/CD)

Erstelle `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

### 4. Issue Templates

Erstelle `.github/ISSUE_TEMPLATE/bug_report.md` & `feature_request.md`

### 5. Pull Request Template

Erstelle `.github/pull_request_template.md`

---

## ? Checkliste

- [ ] .gitignore erstellt
- [ ] LICENSE erstellt
- [ ] Git initialisiert (`git init`)
- [ ] Alle Dateien gestaged (`git add .`)
- [ ] Initial Commit (`git commit`)
- [ ] GitHub Repository erstellt
- [ ] Remote hinzugefügt (`git remote add origin`)
- [ ] Gepusht (`git push -u origin main`)
- [ ] Repository-Einstellungen konfiguriert
- [ ] Topics hinzugefügt
- [ ] GitHub Actions eingerichtet (optional)

---

## ?? Next Steps nach dem Push

1. **README badges updaten** mit echten Links
2. **GitHub Actions einrichten** für automatische Tests
3. **NuGet-Deployment** vorbereiten (optional)
4. **Contributing Guidelines** erstellen
5. **Code of Conduct** hinzufügen

---

**Viel Erfolg mit Ihrem Repository! ??**
