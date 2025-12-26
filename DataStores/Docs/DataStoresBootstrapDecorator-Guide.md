# DataStoresBootstrapDecorator - Common.Bootstrap Integration

## Übersicht

Der `DataStoresBootstrapDecorator` ist ein **Bootstrap-Decorator**, der das **Common.Bootstrap Framework** erweitert und DataStores-spezifische Service-Registrierungen ermöglicht.

Folgt dem **Decorator-Pattern** aus der Common.Bootstrap-Dokumentation.

---

## Zweck

- ✅ **Erweitert** den Standard-Bootstrap-Prozess um DataStores-Features
- ✅ **Kompatibel** mit Common.Bootstrap's `IBootstrapWrapper`
- ✅ **Decorator-Pattern** - kann beliebig verschachtelt werden
- ✅ **Vorbereitet** für zukünftige DataStores-spezifische Scans

---

## Verwendung

### Basic Usage (Leer - Nur Delegation)

```csharp
using Common.Bootstrap;
using DataStores.Bootstrap;

var builder = Host.CreateApplicationBuilder(args);

// DataStoresBootstrapDecorator erweitert DefaultBootstrapWrapper
var bootstrap = new DataStoresBootstrapDecorator(new DefaultBootstrapWrapper());

bootstrap.RegisterServices(
    builder.Services,
    typeof(Program).Assembly);

var app = builder.Build();
await app.RunAsync();
```

**Aktuelles Verhalten:**
- Delegiert alle Calls an den inneren Wrapper (`DefaultBootstrapWrapper`)
- Scannt `IServiceModule` und `IEqualityComparer<T>` (via DefaultBootstrapWrapper)
- Bereit für zukünftige Erweiterungen

---

## Architektur

### Decorator-Pattern

```
┌─────────────────────────────────────────┐
│  DataStoresBootstrapDecorator           │
│  ┌───────────────────────────────────┐  │
│  │  DefaultBootstrapWrapper          │  │
│  │  (IServiceModule + Comparer)      │  │
│  └───────────────────────────────────┘  │
│  + DataStores-Scans (TODO)              │
└─────────────────────────────────────────┘
```

### Ausführungsreihenfolge

1. **Inner Wrapper** (`DefaultBootstrapWrapper`)
   - Scannt `IServiceModule` aus allen Assemblies
   - Scannt `IEqualityComparer<T>` aus allen Assemblies
   - Registriert als Singletons

2. **DataStores-Decorator** (`DataStoresBootstrapDecorator`)
   - Aktuell: Leer (nur Delegation)
   - Zukünftig: Scan nach `IDataStoreRegistrar`, etc.

---

## Zukünftige Erweiterungen

### Mögliche DataStores-Scans (TODO)

```csharp
public void RegisterServices(IServiceCollection services, params Assembly[] assemblies)
{
    // 1. Basis-Registrierungen
    _innerWrapper.RegisterServices(services, assemblies);

    // 2. DataStores-spezifische Scans (ZUKÜNFTIG)
    services.AddDataStoreRegistrarsFromAssemblies(assemblies);
    services.AddIDataStorePathProvidersFromAssemblies(assemblies);
    // etc.
}
```

---

## Vergleich: Mit und Ohne Decorator

### ❌ Ohne Decorator (Manuell)

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Manuell: DefaultBootstrapWrapper
var bootstrap = new DefaultBootstrapWrapper();
bootstrap.RegisterServices(builder.Services, typeof(Program).Assembly);

// Manuell: DataStores-Services
new DataStoresServiceModule().Register(builder.Services);
services.AddDataStoreRegistrarsFromAssembly();

var app = builder.Build();
await app.RunAsync();
```

**Probleme:**
- ❌ Mehrere manuelle Schritte
- ❌ Leicht zu vergessen
- ❌ Nicht erweiterbar

---

### ✅ Mit Decorator (Automatisch)

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Alles in einem: IServiceModule + DataStores
var bootstrap = new DataStoresBootstrapDecorator(new DefaultBootstrapWrapper());
bootstrap.RegisterServices(builder.Services, typeof(Program).Assembly);

var app = builder.Build();
await app.RunAsync();
```

**Vorteile:**
- ✅ Ein einziger Aufruf
- ✅ Automatisch erweiterbar
- ✅ Folgt Decorator-Pattern

---

## Integration mit Common.Bootstrap

### Common.Bootstrap scannt automatisch:

1. **IServiceModule** - Modulare Service-Registrierung
   ```csharp
   public class DataStoresServiceModule : IServiceModule
   {
       public void Register(IServiceCollection services)
       {
           services.AddSingleton<IDataStores, DataStoresFacade>();
           // ...
       }
   }
   ```

2. **IEqualityComparer\<T\>** - Comparer mit Fallback
   ```csharp
   public class ProductIdComparer : IEqualityComparer<Product>
   {
       // ...
   }
   ```

### DataStoresBootstrapDecorator erweitert (zukünftig):

3. **IDataStoreRegistrar** - Store-Registrierung (TODO)
4. **IDataStorePathProvider** - Path-Provider (TODO)

---

## Tests

### Test-Abdeckung

✅ **5 Unit-Tests** in `DataStoresBootstrapDecoratorTests.cs`:

1. `Constructor_WithNullInnerWrapper_Should_Throw`
2. `Constructor_WithValidInnerWrapper_Should_Succeed`
3. `RegisterServices_Should_CallInnerWrapper`
4. `RegisterServices_Should_PassMultipleAssemblies`
5. `RegisterServices_Should_NotThrow_WithEmptyAssemblies`

---

## Best Practices

### ✅ DO

- Verwenden Sie `DataStoresBootstrapDecorator` mit `DefaultBootstrapWrapper`
- Kombinieren Sie mit anderen Decorators bei Bedarf
- Nutzen Sie Assembly-Scanning für automatische Registrierung

### ❌ DON'T

- Nicht direkt instanziieren ohne inneren Wrapper
- Nicht manuell `DataStoresServiceModule.Register()` nach Decorator aufrufen

---

## Beispiele

### Beispiel 1: Einfache Konsolen-App

```csharp
using Common.Bootstrap;
using DataStores.Bootstrap;

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = new DataStoresBootstrapDecorator(new DefaultBootstrapWrapper());
bootstrap.RegisterServices(builder.Services, typeof(Program).Assembly);

var app = builder.Build();
await app.RunAsync();
```

---

### Beispiel 2: ASP.NET Core

```csharp
var builder = WebApplication.CreateBuilder(args);

// DataStores + Common.Bootstrap
var bootstrap = new DataStoresBootstrapDecorator(new DefaultBootstrapWrapper());
bootstrap.RegisterServices(
    builder.Services,
    typeof(Program).Assembly,
    typeof(ProductStoreRegistrar).Assembly);

var app = builder.Build();

// DataStores Bootstrap
await DataStoreBootstrap.RunAsync(app.Services);

app.Run();
```

---

### Beispiel 3: Multi-Layer Decorator

```csharp
// Custom Decorator
public class MyAppBootstrapDecorator : IBootstrapWrapper
{
    private readonly IBootstrapWrapper _innerWrapper;

    public MyAppBootstrapDecorator(IBootstrapWrapper innerWrapper)
    {
        _innerWrapper = innerWrapper;
    }

    public void RegisterServices(IServiceCollection services, params Assembly[] assemblies)
    {
        _innerWrapper.RegisterServices(services, assemblies);
        
        // MyApp-spezifische Scans
        services.AddMyValidatorsFromAssemblies(assemblies);
    }
}

// Verwendung: 3 Layer
var bootstrap = new MyAppBootstrapDecorator(
    new DataStoresBootstrapDecorator(
        new DefaultBootstrapWrapper()));

bootstrap.RegisterServices(builder.Services, typeof(Program).Assembly);
```

---

## Zusammenfassung

| Feature | Status |
|---------|--------|
| **Decorator-Pattern** | ✅ Implementiert |
| **Common.Bootstrap Integration** | ✅ Kompatibel |
| **IServiceModule Scan** | ✅ Via DefaultBootstrapWrapper |
| **EqualityComparer Scan** | ✅ Via DefaultBootstrapWrapper |
| **IDataStoreRegistrar Scan** | ⏳ TODO (vorbereitet) |
| **Unit-Tests** | ✅ 5 Tests |

---

**Version:** 1.0.0  
**Status:** ✅ Bereit für Erweiterungen  
**Last Updated:** Januar 2025
