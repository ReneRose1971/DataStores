# Assembly Scanning - Automatic Registrar Discovery

## Overview

The `AddDataStoreRegistrarsFromAssembly` extension methods enable **automatic discovery and registration** of all `IDataStoreRegistrar` implementations from one or more assemblies.

This eliminates the need for manual registration of each registrar, making the startup code cleaner and reducing the chance of forgetting to register a store.

---

## Benefits

### ✅ Advantages

1. **Less Boilerplate** - No manual registration needed
2. **Convention-Based** - Follows "scan and register" pattern
3. **Maintainable** - Adding new stores doesn't require startup code changes
4. **Type-Safe** - Compile-time validation of registrar classes
5. **Flexible** - Supports single or multiple assemblies

### ⚠️ Limitations

- **Requires Parameterless Constructor** - Registrars MUST have `public` parameterless constructor
- **No Constructor Injection** - Cannot pass configuration via constructor
- **Runtime Discovery** - Slight startup performance cost (usually negligible)

---

## Usage

### Basic Usage (Calling Assembly)

Automatically scans the **calling assembly** for all registrars:

```csharp
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register DataStores core services
new DataStoresServiceModule().Register(services);

// Automatically discover and register all IDataStoreRegistrar from calling assembly
services.AddDataStoreRegistrarsFromAssembly();

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);
```

**Requirements:**
- All registrars MUST be in the **same assembly** as the calling code
- All registrars MUST have a **public parameterless constructor**

---

### Explicit Assembly

Scans a **specific assembly** for registrars:

```csharp
using System.Reflection;

var services = new ServiceCollection();
new DataStoresServiceModule().Register(services);

// Register from specific assembly
services.AddDataStoreRegistrarsFromAssembly(typeof(ProductStoreRegistrar).Assembly);

// Or use Assembly.GetExecutingAssembly()
services.AddDataStoreRegistrarsFromAssembly(Assembly.GetExecutingAssembly());

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);
```

---

### Multiple Assemblies

Scans **multiple assemblies** for registrars:

```csharp
var services = new ServiceCollection();
new DataStoresServiceModule().Register(services);

// Register from multiple assemblies
services.AddDataStoreRegistrarsFromAssemblies(
    typeof(ProductStoreRegistrar).Assembly,      // Core module
    typeof(CustomerStoreRegistrar).Assembly,     // Customer module
    typeof(OrderStoreRegistrar).Assembly);       // Order module

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);
```

**Use Case:** Modular applications with stores distributed across multiple assemblies.

---

## Registrar Requirements

### ✅ Valid Registrar (Will Be Discovered)

```csharp
public class ProductStoreRegistrar : DataStoreRegistrarBase
{
    // ✅ Public parameterless constructor
    public ProductStoreRegistrar()
    {
    }

    protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
    }
}
```

**Criteria:**
- ✅ Implements `IDataStoreRegistrar` (or inherits from `DataStoreRegistrarBase`)
- ✅ Is a **non-abstract class**
- ✅ Has a **public parameterless constructor**

---

### ❌ Invalid Registrars (Will NOT Be Discovered)

#### 1. Registrar with Constructor Parameters

```csharp
// ❌ Will NOT be discovered (no parameterless constructor)
public class JsonProductRegistrar : DataStoreRegistrarBase
{
    private readonly string _jsonPath;

    public JsonProductRegistrar(string jsonPath)
    {
        _jsonPath = jsonPath;
    }

    protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
    {
        AddStore(new JsonDataStoreBuilder<Product>(_jsonPath));
    }
}
```

**Solution:** Register manually with instance:
```csharp
services.AddDataStoreRegistrar(new JsonProductRegistrar("C:\\Data\\products.json"));
```

#### 2. Abstract Registrar

```csharp
// ❌ Will NOT be discovered (abstract class)
public abstract class BaseProductRegistrar : DataStoreRegistrarBase
{
    // ...
}
```

#### 3. Internal/Private Registrar

```csharp
// ❌ Will NOT be discovered (not public)
internal class InternalProductRegistrar : DataStoreRegistrarBase
{
    // ...
}
```

---

## Combining Approaches

You can **mix automatic scanning** with **manual registration**:

```csharp
var services = new ServiceCollection();
new DataStoresServiceModule().Register(services);

// 1. Automatic: Scan assembly for parameterless registrars
services.AddDataStoreRegistrarsFromAssembly();

// 2. Manual: Register registrars that need constructor parameters
services.AddDataStoreRegistrar(new JsonProductRegistrar("C:\\Data\\products.json"));
services.AddDataStoreRegistrar(new LiteDbOrderRegistrar("C:\\Data\\orders.db"));

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);
```

**Best Practice:**
- Use **assembly scanning** for stores without configuration needs
- Use **manual registration** for stores that need file paths, connection strings, etc.

---

## Complete Examples

### Example 1: Simple Console App

```csharp
using DataStores.Bootstrap;
using DataStores.Registration;
using Microsoft.Extensions.DependencyInjection;

// Registrar with no parameters
public class InMemoryStoresRegistrar : DataStoreRegistrarBase
{
    public InMemoryStoresRegistrar() { }

    protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
        AddStore(new InMemoryDataStoreBuilder<Customer>());
        AddStore(new InMemoryDataStoreBuilder<Order>());
    }
}

// Program.cs
class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        
        new DataStoresServiceModule().Register(services);
        
        // Automatically discovers InMemoryStoresRegistrar
        services.AddDataStoreRegistrarsFromAssembly();
        
        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);
        
        // Use stores
        var stores = provider.GetRequiredService<IDataStores>();
        var productStore = stores.GetGlobal<Product>();
        productStore.Add(new Product { Id = 1, Name = "Laptop" });
    }
}
```

---

### Example 2: ASP.NET Core with IDataStorePathProvider

```csharp
using DataStores.Bootstrap;
using DataStores.Registration;

// Registrar using IDataStorePathProvider from DI
public class AppStoresRegistrar : DataStoreRegistrarBase
{
    public AppStoresRegistrar() { }

    protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
    {
        // Use PathProvider from DI
        AddStore(new JsonDataStoreBuilder<Customer>(
            filePath: pathProvider.FormatJsonFileName("customers")));
        
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: pathProvider.FormatLiteDbFileName("myapp")));
    }
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register PathProvider
builder.Services.AddSingleton<IDataStorePathProvider>(
    new DataStorePathProvider("MyApp"));

// Register DataStores
new DataStoresServiceModule().Register(builder.Services);

// Auto-discover registrars
builder.Services.AddDataStoreRegistrarsFromAssembly();

var app = builder.Build();

// Bootstrap
await DataStoreBootstrap.RunAsync(app.Services);

app.Run();
```

---

### Example 3: Modular Application

```csharp
// CoreModule.dll
public class CoreStoresRegistrar : DataStoreRegistrarBase
{
    public CoreStoresRegistrar() { }
    
    protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
    }
}

// CustomerModule.dll
public class CustomerStoresRegistrar : DataStoreRegistrarBase
{
    public CustomerStoresRegistrar() { }
    
    protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
    {
        AddStore(new JsonDataStoreBuilder<Customer>(
            pathProvider.FormatJsonFileName("customers")));
    }
}

// Startup
var services = new ServiceCollection();
new DataStoresServiceModule().Register(services);

// Register from all modules
services.AddDataStoreRegistrarsFromAssemblies(
    typeof(CoreStoresRegistrar).Assembly,
    typeof(CustomerStoresRegistrar).Assembly);

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);
```

---

## Performance Considerations

### Startup Time

Assembly scanning uses **reflection** which has a small startup cost:

- **Small assemblies** (< 100 types): **< 1ms**
- **Medium assemblies** (< 1000 types): **< 10ms**
- **Large assemblies** (> 1000 types): **< 50ms**

**Verdict:** Negligible for most applications.

### Memory

- **No additional runtime overhead** - Registrars are only instantiated once
- **Same memory usage** as manual registration

---

## Migration Guide

### Before (Manual Registration)

```csharp
var services = new ServiceCollection();
new DataStoresServiceModule().Register(services);

// Manual registration - verbose, error-prone
services.AddDataStoreRegistrar<ProductStoreRegistrar>();
services.AddDataStoreRegistrar<CustomerStoreRegistrar>();
services.AddDataStoreRegistrar<OrderStoreRegistrar>();
services.AddDataStoreRegistrar<InvoiceStoreRegistrar>();
// ... easy to forget one!

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);
```

**Problems:**
- ❌ Verbose
- ❌ Easy to forget a registrar
- ❌ Requires startup code changes when adding new stores

---

### After (Assembly Scanning)

```csharp
var services = new ServiceCollection();
new DataStoresServiceModule().Register(services);

// Automatic discovery - clean, maintainable
services.AddDataStoreRegistrarsFromAssembly();

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);
```

**Benefits:**
- ✅ Clean and concise
- ✅ Impossible to forget a registrar
- ✅ No startup code changes when adding new stores

---

## API Reference

### Methods

#### `AddDataStoreRegistrarsFromAssembly()`

Scans the **calling assembly** for registrars.

```csharp
IServiceCollection AddDataStoreRegistrarsFromAssembly(this IServiceCollection services)
```

**Returns:** `IServiceCollection` for chaining

**Throws:**
- `ArgumentNullException` - When `services` is null

---

#### `AddDataStoreRegistrarsFromAssembly(Assembly)`

Scans a **specific assembly** for registrars.

```csharp
IServiceCollection AddDataStoreRegistrarsFromAssembly(
    this IServiceCollection services, 
    Assembly assembly)
```

**Parameters:**
- `assembly` - The assembly to scan

**Returns:** `IServiceCollection` for chaining

**Throws:**
- `ArgumentNullException` - When `services` or `assembly` is null

---

#### `AddDataStoreRegistrarsFromAssemblies(params Assembly[])`

Scans **multiple assemblies** for registrars.

```csharp
IServiceCollection AddDataStoreRegistrarsFromAssemblies(
    this IServiceCollection services, 
    params Assembly[] assemblies)
```

**Parameters:**
- `assemblies` - The assemblies to scan

**Returns:** `IServiceCollection` for chaining

**Throws:**
- `ArgumentNullException` - When `services` or `assemblies` is null

**Note:** Null assemblies in the array are skipped

---

## Best Practices

### ✅ DO

- Use assembly scanning for stores **without configuration**
- Combine scanning with manual registration when needed
- Keep registrars in the **same assembly** as the startup code (for default scanning)
- Use `DataStoreRegistrarBase` for cleaner syntax

### ❌ DON'T

- Don't use assembly scanning for registrars with **constructor parameters**
- Don't scan **third-party assemblies** (only your own)
- Don't rely on registration **order** (registrars should be independent)

---

## Troubleshooting

### Problem: My registrar is not discovered

**Checklist:**
1. ✅ Is the registrar class **public**?
2. ✅ Is the registrar **non-abstract**?
3. ✅ Does it implement `IDataStoreRegistrar`?
4. ✅ Does it have a **public parameterless constructor**?
5. ✅ Is it in the correct **assembly**?

**Solution:** If any answer is "no", either fix it or use manual registration.

---

### Problem: I need constructor parameters

**Solution:** Use manual registration:

```csharp
services.AddDataStoreRegistrar(new MyRegistrar(configValue));
```

Or use `IDataStorePathProvider` from DI instead of constructor parameters.

---

## Summary

| Feature | Manual Registration | Assembly Scanning |
|---------|---------------------|-------------------|
| **Verbosity** | High | Low |
| **Maintainability** | Manual updates | Automatic |
| **Constructor Parameters** | ✅ Supported | ❌ Not supported |
| **Type Safety** | ✅ Compile-time | ✅ Compile-time |
| **Performance** | Instant | < 50ms |
| **Use Case** | Configuration-heavy stores | Simple stores |

**Recommendation:** Use **assembly scanning** by default, fall back to **manual registration** when needed.

---

**Version:** 1.0.0  
**Last Updated:** Januar 2025
