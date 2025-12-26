# Migration Guide: DataStoreRegistrarBase → ConfigureStores Pattern

## Änderung

`DataStoreRegistrarBase` hat jetzt eine abstrakte `ConfigureStores()` Methode, die implementiert werden MUSS.

### Warum?

1. ✅ **Assembly-Scanning**: Parameterloser Konstruktor erforderlich
2. ✅ **PathProvider Integration**: Automatische Bereitstellung von `IDataStorePathProvider`
3. ✅ **Klare Reihenfolge**: Framework kontrolliert die Ausführungsreihenfolge

---

## Migration-Muster

### VORHER (alter Code):
```csharp
private class MyRegistrar : DataStoreRegistrarBase
{
    public MyRegistrar(string dbPath)  // ❌ Parameter verhindert Assembly-Scan
    {
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath));
    }
}
```

### NACHHER (neuer Code):
```csharp
private class MyRegistrar : DataStoreRegistrarBase
{
    public MyRegistrar() { }  // ✅ Parameterlos

    protected override void ConfigureStores(IDataStorePathProvider pathProvider)
    {
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: pathProvider.FormatLiteDbFileName("myapp")));
    }
}
```

---

## Konkrete Beispiele aus Tests

### 1. InMemory Store (ohne Pfade)

**VORHER:**
```csharp
private class InMemoryOnlyRegistrar : DataStoreRegistrarBase
{
    public InMemoryOnlyRegistrar()
    {
        AddStore(new InMemoryDataStoreBuilder<TestEntity>());
    }
}
```

**NACHHER:**
```csharp
private class InMemoryOnlyRegistrar : DataStoreRegistrarBase
{
    public InMemoryOnlyRegistrar() { }

    protected override void ConfigureStores(IDataStorePathProvider pathProvider)
    {
        AddStore(new InMemoryDataStoreBuilder<TestEntity>());
    }
}
```

### 2. JSON Store (mit Pfad)

**VORHER:**
```csharp
private class JsonOnlyRegistrar : DataStoreRegistrarBase
{
    public JsonOnlyRegistrar(string jsonPath)
    {
        AddStore(new JsonDataStoreBuilder<TestDto>(
            filePath: jsonPath));
    }
}
```

**NACHHER:**
```csharp
private class JsonOnlyRegistrar : DataStoreRegistrarBase
{
    private readonly string _jsonPath;  // Fallback für Tests

    public JsonOnlyRegistrar(string jsonPath)
    {
        _jsonPath = jsonPath;
    }

    protected override void ConfigureStores(IDataStorePathProvider pathProvider)
    {
        AddStore(new JsonDataStoreBuilder<TestDto>(
            filePath: _jsonPath));  // Nutze gespeicherten Pfad
    }
}
```

**ODER (wenn PathProvider verfügbar):**
```csharp
private class JsonOnlyRegistrar : DataStoreRegistrarBase
{
    public JsonOnlyRegistrar() { }

    protected override void ConfigureStores(IDataStorePathProvider pathProvider)
    {
        AddStore(new JsonDataStoreBuilder<TestDto>(
            filePath: pathProvider.FormatJsonFileName("test")));
    }
}
```

### 3. LiteDB Store (mit Pfad)

**VORHER:**
```csharp
private class LiteDbOnlyRegistrar : DataStoreRegistrarBase
{
    public LiteDbOnlyRegistrar(string dbPath)
    {
        AddStore(new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: dbPath));
    }
}
```

**NACHHER:**
```csharp
private class LiteDbOnlyRegistrar : DataStoreRegistrarBase
{
    private readonly string _dbPath;

    public LiteDbOnlyRegistrar(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override void ConfigureStores(IDataStorePathProvider pathProvider)
    {
        AddStore(new LiteDbDataStoreBuilder<TestEntity>(
            databasePath: _dbPath));
    }
}
```

### 4. Multi-Store Registrar

**VORHER:**
```csharp
private class MultiTypeRegistrar : DataStoreRegistrarBase
{
    public MultiTypeRegistrar(string dbPath, string jsonPath)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
        AddStore(new JsonDataStoreBuilder<Customer>(jsonPath));
        AddStore(new LiteDbDataStoreBuilder<TestEntity>(dbPath));
    }
}
```

**NACHHER:**
```csharp
private class MultiTypeRegistrar : DataStoreRegistrarBase
{
    private readonly string _dbPath;
    private readonly string _jsonPath;

    public MultiTypeRegistrar(string dbPath, string jsonPath)
    {
        _dbPath = dbPath;
        _jsonPath = jsonPath;
    }

    protected override void ConfigureStores(IDataStorePathProvider pathProvider)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
        AddStore(new JsonDataStoreBuilder<Customer>(_jsonPath));
        AddStore(new LiteDbDataStoreBuilder<TestEntity>(_dbPath));
    }
}
```

---

## Betroffene Test-Dateien (29)

Alle Klassen die von `DataStoreRegistrarBase` erben müssen angepasst werden:

### Registration Tests:
- `DataStores.Tests\Registration\DataStoreRegistrarBaseTests.cs` ✅ (erledigt)
- `DataStores.Tests\Registration\InMemoryDataStoreBuilderTests.cs`
- `DataStores.Tests\Registration\JsonDataStoreBuilderTests.cs`
- `DataStores.Tests\Registration\LiteDbDataStoreBuilderTests.cs`

### Integration Tests:
- `DataStores.Tests\Integration\BuilderPattern_EndToEnd_IntegrationTests.cs`
- `DataStores.Tests\Integration\BuilderPattern_Advanced_IntegrationTests.cs`
- `DataStores.Tests\Integration\BuilderPattern_Negative_IntegrationTests.cs`

---

## Automatische Migration (PowerShell-Skript)

Verwenden Sie dieses Skript NICHT - es dient nur zur Dokumentation des Musters:

```powershell
# DIESES SKRIPT NICHT AUSFÜHREN - Nur zur Dokumentation
$testFiles = Get-ChildItem -Path "DataStores.Tests" -Recurse -Filter "*.cs"

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Pattern 1: Einfacher Konstruktor ohne Parameter
    $pattern1 = '(?s)(private class \w+Registrar : DataStoreRegistrarBase\s*\{\s*public \w+Registrar\(\)\s*\{)(.*?)(\s*\})'
    $replacement1 = '$1 }

    protected override void ConfigureStores(IDataStorePathProvider pathProvider)
    {$2$3'
    
    $newContent = $content -replace $pattern1, $replacement1
    
    Set-Content -Path $file.FullName -Value $newContent
}
```

---

## Manuelle Migration erforderlich

**Empfehlung:** Migrieren Sie die Tests manuell mit dem obigen Muster.

**Schritte:**
1. Öffnen Sie jede Test-Datei
2. Finden Sie alle `class XxxRegistrar : DataStoreRegistrarBase`
3. Verschieben Sie `AddStore()` Aufrufe aus dem Konstruktor in `ConfigureStores()`
4. Speichern Sie Konstruktor-Parameter als private Fields wenn nötig

---

## Compiler-Fehler beheben

Der Compiler zeigt alle betroffenen Stellen:

```
CS0534: "XxxRegistrar" implementiert den geerbten abstrakten Member 
"DataStoreRegistrarBase.ConfigureStores(IDataStorePathProvider)" nicht.
```

Gehen Sie jede Datei durch und fügen Sie die Methode hinzu.

---

**Status:** Breaking Change - Manuelle Migration erforderlich
**Anzahl betroffener Test-Klassen:** ~29
**Zeitaufwand:** ~15-30 Minuten
