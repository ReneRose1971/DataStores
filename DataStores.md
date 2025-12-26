# DataStores - Vollständiger Bootstrap-Prozess

## Übersicht

DataStores ist eine moderne .NET 8 Bibliothek für die Verwaltung von typsicheren In-Memory-Datensammlungen mit umfassender Unterstützung für Persistierung, Event-Handling und hierarchische Beziehungen.

Die Bibliothek integriert sich nahtlos mit **Common.Bootstrap** und bietet einen automatisierten Bootstrap-Prozess, der alle erforderlichen Services aus den beteiligten Assemblies scannt und registriert.

---

## Bootstrap-Prozess

Der DataStores Bootstrap-Prozess erfolgt in **4 Schritten** und nutzt das **Decorator-Pattern** aus Common.Bootstrap:

### Schritt 1: PathProvider registrieren

Der `IDataStorePathProvider` ist verantwortlich für die Generierung standardisierter Dateipfade für JSON- und LiteDB-Dateien.

```csharp
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 1. Initialisiere app-spezifischen PathProvider
var pathProvider = new DataStorePathProvider("MyApp");
services.AddSingleton<IDataStorePathProvider>(pathProvider);
```

**Zweck:**
- Zentrale Verwaltung von Dateipfaden
- Automatische Erstellung von standardisierten Dateinamen
- Vermeidung von Pfad-Duplikation im Code

---

### Schritt 2: DefaultBootstrapWrapper instanziieren

Der `DefaultBootstrapWrapper` aus Common.Bootstrap scannt nach `IServiceModule` und `IEqualityComparer<T>` Implementierungen.

```csharp
using Common.Bootstrap;

// 2. Instanziiere DefaultBootstrapWrapper
var defaultWrapper = new DefaultBootstrapWrapper();
```

**Was wird gescannt:**
- ✅ **IServiceModule** - Modulare Service-Registrierung
- ✅ **IEqualityComparer\<T\>** - Custom Equality-Comparer

---

### Schritt 3: DataStoresBootstrapDecorator instanziieren

Der `DataStoresBootstrapDecorator` erweitert den DefaultBootstrapWrapper um DataStores-spezifische Scans.

```csharp
// 3. Instanziiere DataStoresBootstrapDecorator
var bootstrap = new DataStoresBootstrapDecorator(defaultWrapper);
```

**Was wird zusätzlich gescannt:**
- ✅ **IDataStoreRegistrar** - Store-Registrierung

---

### Schritt 4: RegisterServices aufrufen

Der Aufruf von `RegisterServices` scannt **alle angegebenen Assemblies** und registriert die gefundenen Implementierungen.

```csharp
// 4. Registriere Services aus beiden Assemblies
bootstrap.RegisterServices(
    services,
    typeof(DefaultBootstrapWrapper).Assembly,      // Common.Bootstrap Assembly
    typeof(DataStoresBootstrapDecorator).Assembly  // DataStores Assembly
);
```

**Wichtig:**
- Übergeben Sie **beide Assemblies** (Common.Bootstrap + DataStores)
- Sie können **weitere Assemblies** hinzufügen (z.B. Ihre App-Assembly)
- Die Reihenfolge ist wichtig: DefaultBootstrapWrapper zuerst, dann DataStores-Scan

---

### Schritt 5: ServiceProvider bauen und Bootstrap ausführen

Nach der Registrierung müssen Sie den `ServiceProvider` bauen und `DataStoreBootstrap.RunAsync()` aufrufen.

```csharp
// ServiceProvider bauen
var provider = services.BuildServiceProvider();

// DataStores Bootstrap ausführen (lädt persistente Stores)
await DataStoreBootstrap.RunAsync(provider);
```

**Was passiert beim Bootstrap:**
1. Alle `IDataStoreRegistrar` werden aufgerufen
2. Stores werden in der `IGlobalStoreRegistry` registriert
3. Persistente Stores werden initialisiert (auto-load)

---

### Schritt 6: Stores verwenden

Nach dem Bootstrap können Sie über die `IDataStores` Facade auf Ihre Stores zugreifen.

```csharp
// Stores verwenden
var stores = provider.GetRequiredService<IDataStores>();
var store = stores.GetGlobal<MyEntity>();

// Daten hinzufügen
store.Add(new MyEntity { Name = "Test" });
```

---

## Vollständiges Beispiel

### Kompletter Bootstrap-Workflow

```csharp
using Common.Bootstrap;
using DataStores.Abstractions;
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

// Service Collection erstellen
var services = new ServiceCollection();

// 1. PathProvider registrieren
var pathProvider = new DataStorePathProvider("MyApp");
services.AddSingleton<IDataStorePathProvider>(pathProvider);

// 2. DefaultBootstrapWrapper instanziieren
var defaultWrapper = new DefaultBootstrapWrapper();

// 3. DataStoresBootstrapDecorator instanziieren
var bootstrap = new DataStoresBootstrapDecorator(defaultWrapper);

// 4. Services aus Assemblies registrieren
bootstrap.RegisterServices(
    services,
    typeof(DefaultBootstrapWrapper).Assembly,      // Common.Bootstrap
    typeof(DataStoresBootstrapDecorator).Assembly  // DataStores
);

// 5. ServiceProvider bauen und Bootstrap ausführen
var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);

// 6. Stores verwenden
var stores = provider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();

productStore.Add(new Product { Id = 1, Name = "Laptop", Price = 999.99m });
Console.WriteLine($"Products in store: {productStore.Items.Count}");
```

---

## Was wird automatisch registriert?

Der Bootstrap-Prozess scannt die angegebenen Assemblies und registriert automatisch:

### 1. IServiceModule (via DefaultBootstrapWrapper)

Alle Klassen, die `IServiceModule` implementieren:

```csharp
public class DataStoresServiceModule : IServiceModule
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IDataStores, DataStoresFacade>();
        services.AddSingleton<IGlobalStoreRegistry, GlobalStoreRegistry>();
        services.AddSingleton<ILocalDataStoreFactory, LocalDataStoreFactory>();
    }
}
```

**Automatisch erkannt und registriert!**

---

### 2. IEqualityComparer\<T\> (via DefaultBootstrapWrapper)

Alle Klassen, die `IEqualityComparer<T>` implementieren:

```csharp
public class ProductIdComparer : IEqualityComparer<Product>
{
    public bool Equals(Product? x, Product? y)
    {
        if (x == null || y == null) return false;
        return x.Id == y.Id;
    }

    public int GetHashCode(Product obj) => obj.Id.GetHashCode();
}
```

**Automatisch erkannt und registriert!**

**Verwendung:**
```csharp
var comparer = serviceProvider.GetEqualityComparer<Product>();
// Falls nicht gefunden: Fallback auf EqualityComparer<Product>.Default
```

---

### 3. IDataStoreRegistrar (via DataStoresBootstrapDecorator)

Alle Klassen, die `IDataStoreRegistrar` implementieren und einen **parameterlosen öffentlichen Konstruktor** haben:

```csharp
public class ProductStoreRegistrar : DataStoreRegistrarBase
{
    // Parameterloser Konstruktor erforderlich!
    public ProductStoreRegistrar()
    {
    }

    protected override void ConfigureStores(
        IServiceProvider serviceProvider, 
        IDataStorePathProvider pathProvider)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
        
        AddStore(new JsonDataStoreBuilder<Customer>(
            filePath: pathProvider.FormatJsonFileName("customers")));
        
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: pathProvider.FormatLiteDbFileName("myapp")));
    }
}
```

**Automatisch erkannt und registriert!**

**Anforderungen:**
- ✅ Implementiert `IDataStoreRegistrar` (oder erbt von `DataStoreRegistrarBase`)
- ✅ Ist eine **public** Klasse
- ✅ Ist **nicht abstract**
- ✅ Hat einen **public parameterlosen Konstruktor**

---

## Ausführungsreihenfolge

### Beim Aufruf von `RegisterServices`:

```
┌────────────────────────────────────────────────────────┐
│  1. DefaultBootstrapWrapper.RegisterServices()         │
│     ├── Scannt IServiceModule                          │
│     │   └── Registriert als Singleton                  │
│     └── Scannt IEqualityComparer<T>                    │
│         └── Registriert als Singleton                  │
└────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────┐
│  2. DataStoresBootstrapDecorator (zusätzlich)          │
│     └── Scannt IDataStoreRegistrar                     │
│         └── Registriert als Singleton                  │
└────────────────────────────────────────────────────────┘
```

### Beim Aufruf von `DataStoreBootstrap.RunAsync()`:

```
┌────────────────────────────────────────────────────────┐
│  1. Ruft alle IDataStoreRegistrar auf                  │
│     └── Registriert Stores in IGlobalStoreRegistry     │
└────────────────────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────┐
│  2. Initialisiert alle IAsyncInitializable             │
│     └── Lädt Daten aus persistenten Stores             │
│         (wenn autoLoad = true)                         │
└────────────────────────────────────────────────────────┘
```

---

## ASP.NET Core Integration

### Beispiel: Program.cs

```csharp
using Common.Bootstrap;
using DataStores.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

// 1. PathProvider
var pathProvider = new DataStorePathProvider("MyWebApp");
builder.Services.AddSingleton<IDataStorePathProvider>(pathProvider);

// 2-4. Bootstrap mit Decorator
var defaultWrapper = new DefaultBootstrapWrapper();
var bootstrap = new DataStoresBootstrapDecorator(defaultWrapper);

bootstrap.RegisterServices(
    builder.Services,
    typeof(DefaultBootstrapWrapper).Assembly,
    typeof(DataStoresBootstrapDecorator).Assembly,
    typeof(Program).Assembly  // Ihre App-Assembly
);

var app = builder.Build();

// 5. DataStores Bootstrap
await DataStoreBootstrap.RunAsync(app.Services);

app.MapControllers();
app.Run();
```

---

## Vorteile des automatischen Scannings

### ✅ Weniger Boilerplate

**Ohne Scanning (manuell):**
```csharp
services.AddSingleton<IServiceModule, DataStoresServiceModule>();
services.AddSingleton<IServiceModule, MyAppServiceModule>();
services.AddSingleton<IEqualityComparer<Product>, ProductIdComparer>();
services.AddSingleton<IDataStoreRegistrar, ProductStoreRegistrar>();
services.AddSingleton<IDataStoreRegistrar, CustomerStoreRegistrar>();
services.AddSingleton<IDataStoreRegistrar, OrderStoreRegistrar>();
// ... leicht zu vergessen!
```

**Mit Scanning (automatisch):**
```csharp
var bootstrap = new DataStoresBootstrapDecorator(new DefaultBootstrapWrapper());
bootstrap.RegisterServices(services, 
    typeof(DefaultBootstrapWrapper).Assembly,
    typeof(DataStoresBootstrapDecorator).Assembly);
// Fertig! Alle Implementierungen werden automatisch gefunden
```

---

### ✅ Wartbarkeit

- Neue `IDataStoreRegistrar` werden automatisch erkannt
- Keine Startup-Code-Änderungen bei neuen Stores
- Zentrale Konfiguration pro Assembly

---

### ✅ Convention over Configuration

- Folgt etablierten Patterns (IServiceModule, IBootstrapWrapper)
- Selbsterklärend durch Interfaces
- Weniger Fehlerquellen

---

## Häufige Fehler und Lösungen

### ❌ Fehler: "No service for type IGlobalStoreRegistry"

**Ursache:** `RegisterServices` wurde nicht aufgerufen oder falsche Assembly.

**Lösung:**
```csharp
// Stellen Sie sicher, dass DataStores Assembly gescannt wird
bootstrap.RegisterServices(services, 
    typeof(DataStoresBootstrapDecorator).Assembly);
```

---

### ❌ Fehler: "Global store for type X already registered"

**Ursache:** Doppelte Registrierung durch mehrfaches Scannen derselben Assembly.

**Lösung:**
```csharp
// Übergeben Sie jede Assembly nur EINMAL
bootstrap.RegisterServices(services, 
    typeof(DefaultBootstrapWrapper).Assembly,      // Einmal
    typeof(DataStoresBootstrapDecorator).Assembly  // Einmal
);
```

---

### ❌ Fehler: "IDataStoreRegistrar not found"

**Ursache:** Registrar hat keinen parameterlosen Konstruktor oder ist nicht public.

**Lösung:**
```csharp
// ✅ RICHTIG
public class MyRegistrar : DataStoreRegistrarBase
{
    public MyRegistrar() { }  // Parameterloser Konstruktor
    
    protected override void ConfigureStores(...) { }
}

// ❌ FALSCH
public class MyRegistrar : DataStoreRegistrarBase
{
    public MyRegistrar(string config) { }  // Hat Parameter!
}
```

**Alternative für Registrars mit Parametern:**
```csharp
// Manuelle Registrierung
services.AddDataStoreRegistrar(new MyRegistrar("config"));
```

---

## Best Practices

### ✅ DO

1. **Immer PathProvider zuerst registrieren**
   ```csharp
   services.AddSingleton<IDataStorePathProvider>(new DataStorePathProvider("MyApp"));
   ```

2. **Beide Assemblies übergeben**
   ```csharp
   bootstrap.RegisterServices(services,
       typeof(DefaultBootstrapWrapper).Assembly,
       typeof(DataStoresBootstrapDecorator).Assembly);
   ```

3. **Bootstrap vor erster Store-Nutzung ausführen**
   ```csharp
   await DataStoreBootstrap.RunAsync(provider);
   ```

4. **Nur über IDataStores Facade zugreifen**
   ```csharp
   var stores = provider.GetRequiredService<IDataStores>();
   var store = stores.GetGlobal<MyEntity>();
   ```

---

### ❌ DON'T

1. **Nicht IGlobalStoreRegistry direkt verwenden**
   ```csharp
   // ❌ FALSCH
   var registry = provider.GetRequiredService<IGlobalStoreRegistry>();
   ```

2. **Nicht Stores direkt instanziieren**
   ```csharp
   // ❌ FALSCH
   var store = new InMemoryDataStore<Product>();
   ```

3. **Nicht Bootstrap überspringen**
   ```csharp
   // ❌ FALSCH
   var stores = provider.GetRequiredService<IDataStores>();
   // Stores sind noch nicht initialisiert!
   ```

---

## Zusammenfassung

Der DataStores Bootstrap-Prozess:

1. ✅ **PathProvider** registrieren
2. ✅ **DefaultBootstrapWrapper** instanziieren
3. ✅ **DataStoresBootstrapDecorator** instanziieren
4. ✅ **RegisterServices** aufrufen mit beiden Assemblies
5. ✅ **ServiceProvider** bauen
6. ✅ **DataStoreBootstrap.RunAsync()** ausführen
7. ✅ **Stores verwenden** via `IDataStores`

**Was wird automatisch registriert:**
- ✅ **IServiceModule** (DataStoresServiceModule, etc.)
- ✅ **IEqualityComparer\<T\>** (Custom Comparer)
- ✅ **IDataStoreRegistrar** (Store-Registrierung)

**Ergebnis:**
- Vollständig initialisierte DataStores
- Alle persistenten Stores geladen
- Bereit für den Einsatz in Ihrer Anwendung

---

**Version:** 1.0.0  
**Letzte Aktualisierung:** Januar 2025  
**Framework:** .NET 8.0
