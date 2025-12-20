# DataStore Registrar Best Practices

## Das Problem: Externe Konfigurationsabhängigkeiten

### ❌ Schlechtes Design (Anti-Pattern)

```csharp
// Konfigurationsklasse muss separat registriert werden
public class LiteDbConfiguration
{
    public string DatabasePath { get; set; } = "";
}

public class MyRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // ❌ PROBLEM: Benötigt externe Konfiguration aus DI!
        var config = serviceProvider.GetRequiredService<LiteDbConfiguration>();
        
        registry.RegisterGlobalWithLiteDb<Order>(config.DatabasePath, "orders");
    }
}

// Verwendung erfordert ZWEI separate Registrierungen:
services.AddSingleton(new LiteDbConfiguration { DatabasePath = dbPath }); // 1. Config
services.AddDataStoreRegistrar<MyRegistrar>();                             // 2. Registrar
```

**Warum ist das schlecht?**
- ✗ Registrar ist NICHT selbstständig
- ✗ Erfordert Wissen über interne Abhängigkeiten
- ✗ Fehleranfällig (Config könnte vergessen werden)
- ✗ Schwieriger zu testen
- ✗ Verletzt das Prinzip der Kapselung

---

## ✅ Gutes Design: Selbstständige Registrars

### Lösung 1: Konfiguration im Konstruktor

```csharp
public class MyRegistrar : IDataStoreRegistrar
{
    private readonly string _databasePath;
    private readonly string _jsonPath;

    // ✅ Konfiguration wird im Konstruktor übergeben
    public MyRegistrar(string databasePath, string jsonPath)
    {
        _databasePath = databasePath;
        _jsonPath = jsonPath;
    }

    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // ✅ Keine externe Abhängigkeit!
        registry
            .RegisterGlobalWithLiteDb<Order>(_databasePath, "orders")
            .RegisterGlobalWithJsonFile<Customer>(_jsonPath);
    }
}

// Verwendung: NUR EINE Registrierung nötig
services.AddDataStoreRegistrar(new MyRegistrar(dbPath, jsonPath));
```

**Warum ist das besser?**
- ✓ Registrar ist vollständig selbstständig
- ✓ Klare und explizite Abhängigkeiten
- ✓ Einfacher zu testen
- ✓ Keine versteckten Abhängigkeiten
- ✓ Folgt Dependency Injection Best Practices

---

### Lösung 2: IConfiguration direkt nutzen (für ASP.NET Core)

Wenn Sie `IConfiguration` verwenden möchten (z.B. aus `appsettings.json`):

```csharp
public class MyRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // ✅ IConfiguration aus DI ist OK, weil es ein Standard-Service ist
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        
        var dbPath = config["DataStores:DatabasePath"]!;
        var jsonPath = config["DataStores:JsonPath"]!;
        
        registry
            .RegisterGlobalWithLiteDb<Order>(dbPath, "orders")
            .RegisterGlobalWithJsonFile<Customer>(jsonPath);
    }
}

// appsettings.json
{
  "DataStores": {
    "DatabasePath": "C:\\Data\\app.db",
    "JsonPath": "C:\\Data\\customers.json"
  }
}

// Verwendung
services.AddDataStoreRegistrar<MyRegistrar>();
```

**Wann ist das akzeptabel?**
- ✓ `IConfiguration` ist ein Framework-Standard-Service
- ✓ Immer verfügbar in ASP.NET Core / Generic Host
- ✓ Konfiguration kommt aus `appsettings.json`

---

## Vergleich der Ansätze

| Aspekt | ❌ Externe Config | ✅ Konstruktor | ✅ IConfiguration |
|--------|-------------------|----------------|-------------------|
| **Selbstständig** | Nein | Ja | Ja |
| **Explizite Abhängigkeiten** | Nein | Ja | Teilweise |
| **Testbarkeit** | Schwierig | Einfach | Mittel |
| **Für ASP.NET Core** | Nein | Ja | Ideal |
| **Für Konsolen-Apps** | Nein | Ideal | Möglich |
| **Für Tests** | Umständlich | Perfekt | Möglich |

---

## Vollständige Beispiele

### Beispiel 1: Einfache Konsolen-Anwendung

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        
        services.AddDataStoresCore();
        
        // ✅ Registrar mit allen Informationen erstellen
        var dbPath = "C:\\Data\\myapp.db";
        var jsonPath = "C:\\Data\\customers.json";
        services.AddDataStoreRegistrar(new AppDataStoreRegistrar(dbPath, jsonPath));
        
        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);
        
        // DataStores verwenden
        var stores = provider.GetRequiredService<IDataStores>();
        var orders = stores.GetGlobal<Order>();
        orders.Add(new Order { Id = 1, Total = 99.99m });
    }
}

public class AppDataStoreRegistrar : IDataStoreRegistrar
{
    private readonly string _dbPath;
    private readonly string _jsonPath;

    public AppDataStoreRegistrar(string dbPath, string jsonPath)
    {
        _dbPath = dbPath;
        _jsonPath = jsonPath;
    }

    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        registry
            .RegisterGlobalWithLiteDb<Order>(_dbPath, "orders")
            .RegisterGlobalWithJsonFile<Customer>(_jsonPath);
    }
}
```

---

### Beispiel 2: ASP.NET Core mit appsettings.json

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataStoresCore();
builder.Services.AddDataStoreRegistrar<AppDataStoreRegistrar>();

var app = builder.Build();

// Bootstrap DataStores
await DataStoreBootstrap.RunAsync(app.Services);

app.Run();

// AppDataStoreRegistrar.cs
public class AppDataStoreRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        
        var dataPath = config["DataStores:DataPath"]!;
        var dbPath = Path.Combine(dataPath, "app.db");
        var jsonPath = Path.Combine(dataPath, "customers.json");
        
        registry
            .RegisterGlobalWithLiteDb<Order>(dbPath, "orders")
            .RegisterGlobalWithLiteDb<Invoice>(dbPath, "invoices")
            .RegisterGlobalWithJsonFile<Customer>(jsonPath);
    }
}

// appsettings.json
{
  "DataStores": {
    "DataPath": "C:\\ProgramData\\MyApp\\Data"
  }
}
```

---

### Beispiel 3: Unit Tests

```csharp
[Fact]
public async Task DataStore_Should_PersistData()
{
    // Arrange
    var tempPath = Path.GetTempPath();
    var dbPath = Path.Combine(tempPath, $"test_{Guid.NewGuid()}.db");
    var jsonPath = Path.Combine(tempPath, $"test_{Guid.NewGuid()}.json");
    
    var services = new ServiceCollection();
    services.AddDataStoresCore();
    
    // ✅ Sehr einfach in Tests: Direkte Parametrisierung
    services.AddDataStoreRegistrar(new TestDataStoreRegistrar(dbPath, jsonPath));
    
    var provider = services.BuildServiceProvider();
    await DataStoreBootstrap.RunAsync(provider);
    
    // Act
    var stores = provider.GetRequiredService<IDataStores>();
    var orders = stores.GetGlobal<Order>();
    orders.Add(new Order { Id = 1, Total = 99.99m });
    
    await Task.Delay(200); // Wait for auto-save
    
    // Assert
    Assert.True(File.Exists(dbPath));
}

public class TestDataStoreRegistrar : IDataStoreRegistrar
{
    private readonly string _dbPath;
    private readonly string _jsonPath;

    public TestDataStoreRegistrar(string dbPath, string jsonPath)
    {
        _dbPath = dbPath;
        _jsonPath = jsonPath;
    }

    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        registry
            .RegisterGlobalWithLiteDb<Order>(_dbPath, "orders")
            .RegisterGlobalWithJsonFile<Customer>(_jsonPath);
    }
}
```

---

## Zusammenfassung

### ✅ DO:
- Konfiguration über Konstruktor-Parameter übergeben
- `IConfiguration` direkt in der `Register()`-Methode verwenden (ASP.NET Core)
- Registrar vollständig selbstständig machen
- Extension-Methode `AddDataStoreRegistrar(instance)` verwenden

### ❌ DON'T:
- Benutzerdefinierte Konfigurationsklassen in DI registrieren
- Versteckte Abhängigkeiten zu eigenen Services
- Registrar von externen Services abhängig machen (außer Framework-Standards)
- Konfiguration irgendwo anders als im Registrar selbst verwalten

---

## Die neue Extension-Methode

```csharp
// In DataStores/Bootstrap/ServiceCollectionExtensions.cs
public static IServiceCollection AddDataStoreRegistrar(
    this IServiceCollection services, 
    IDataStoreRegistrar registrar)
{
    if (registrar == null)
        throw new ArgumentNullException(nameof(registrar));

    services.AddSingleton(registrar);
    return services;
}
```

Diese Methode ermöglicht:
- ✅ Direktes Registrieren von Registrar-Instanzen
- ✅ Übergabe von Konfiguration über Konstruktor
- ✅ Fluent-API-Stil
- ✅ Keine versteckten Abhängigkeiten
