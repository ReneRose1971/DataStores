# Data Store Registration Builders

Complete guide to using the builder pattern for data store registration.

## Overview

The builder pattern provides a clean, type-safe, and declarative API for registering data stores. Instead of manually creating stores, decorators, and strategies, you use specialized builder classes.

## Benefits

### Type Safety
Builders use generic type constraints and named parameters to prevent configuration errors at compile-time.

```csharp
// ✅ Compile-time error: LiteDbDataStoreBuilder requires EntityBase
// AddStore(new LiteDbDataStoreBuilder<string>("test.db")); // Won't compile!

// ✅ Correct: Product inherits from EntityBase
AddStore(new LiteDbDataStoreBuilder<Product>("test.db"));
```

### Self-Documenting
Named parameters make the intent clear without requiring comments.

```csharp
// What do these booleans mean?
RegisterLiteDb<Order>(dbPath, "orders", true, true, null, null);

// Clear and explicit
AddStore(new LiteDbDataStoreBuilder<Order>(
    databasePath: dbPath,
    autoLoad: true,
    autoSave: true));
```

### No Boilerplate
Builders encapsulate the decorator pattern, eliminating repetitive code.

```csharp
// ❌ OLD: Manual decorator creation (6 lines)
var strategy = new LiteDbPersistenceStrategy<Order>(dbPath, "orders");
var innerStore = new InMemoryDataStore<Order>();
var decorator = new PersistentStoreDecorator<Order>(
    innerStore, strategy, true, true);
registry.RegisterGlobal(decorator);

// ✅ NEW: Builder pattern (1 line)
AddStore(new LiteDbDataStoreBuilder<Order>(databasePath: dbPath));
```

### Consistency
Collection names are auto-generated from type names, eliminating manual naming errors.

```csharp
// ❌ OLD: Manual collection names (error-prone)
RegisterLiteDb<Order>(dbPath, "orders");    // Lowercase
RegisterLiteDb<Customer>(dbPath, "Customer"); // Uppercase - Inconsistent!

// ✅ NEW: Automatic consistency
AddStore(new LiteDbDataStoreBuilder<Order>(dbPath));     // → "Order"
AddStore(new LiteDbDataStoreBuilder<Customer>(dbPath));  // → "Customer"
```

## Available Builders

### InMemoryDataStoreBuilder\<T\>

Transient in-memory stores without persistence.

**Use When:**
- Data does not need to persist (temporary UI state, cache)
- Data is loaded from external sources
- Performance is critical

**Parameters:**
- `comparer` (optional): Equality comparer for Contains/Remove
- `synchronizationContext` (optional): UI-thread event marshalling

**Example:**
```csharp
// Simple in-memory store
AddStore(new InMemoryDataStoreBuilder<Product>());

// With custom comparer
AddStore(new InMemoryDataStoreBuilder<Category>(
    comparer: new CategoryIdComparer()));

// With UI-thread events (WPF)
AddStore(new InMemoryDataStoreBuilder<OrderViewModel>(
    synchronizationContext: SynchronizationContext.Current));
```

---

### JsonDataStoreBuilder\<T\>

Persistent stores using JSON file storage.

**Use When:**
- Small to medium datasets (< 10,000 items)
- Human-readable format preferred
- Configuration files, user preferences

**NOT Recommended For:**
- Large datasets
- High-frequency writes
- Concurrent access from multiple processes

**Parameters:**
- `filePath`: Full path to JSON file (created automatically)
- `autoLoad` (default: true): Load during bootstrap
- `autoSave` (default: true): Save on changes
- `comparer` (optional): Equality comparer
- `synchronizationContext` (optional): UI-thread event marshalling

**Features:**
- UTF-8 encoding
- Indented JSON format (easy to read/edit)
- Automatic directory creation
- PropertyChanged tracking for INotifyPropertyChanged

**Example:**
```csharp
// Simple JSON store
AddStore(new JsonDataStoreBuilder<Customer>(
    filePath: "C:\\Data\\customers.json"));

// Read-only configuration
AddStore(new JsonDataStoreBuilder<Settings>(
    filePath: "C:\\Data\\settings.json",
    autoLoad: true,
    autoSave: false));

// With all options
AddStore(new JsonDataStoreBuilder<Product>(
    filePath: "C:\\Data\\products.json",
    autoLoad: true,
    autoSave: true,
    comparer: new ProductIdComparer(),
    synchronizationContext: SynchronizationContext.Current));
```

---

### LiteDbDataStoreBuilder\<T\>

Persistent stores using LiteDB NoSQL database.

**Type Constraint:** T must inherit from `EntityBase`

**Use When:**
- Medium to large datasets
- Offline-first applications
- Desktop applications
- ACID transactions needed

**NOT Recommended For:**
- Web applications with concurrent access
- Cloud-first scenarios

**Parameters:**
- `databasePath`: Full path to LiteDB file (created automatically)
- `autoLoad` (default: true): Load during bootstrap
- `autoSave` (default: true): Save on changes
- `comparer` (optional): Equality comparer
- `synchronizationContext` (optional): UI-thread event marshalling

**Features:**
- Serverless NoSQL database
- ACID transactions
- Automatic ID assignment
- Delta-based synchronization (INSERT/DELETE)
- Thread-safe
- Collection name auto-generated from typeof(T).Name

**Example:**
```csharp
// Simple LiteDB store
// Collection name is automatically "Order"
AddStore(new LiteDbDataStoreBuilder<Order>(
    databasePath: "C:\\Data\\myapp.db"));

// Multiple stores in same database
AddStore(new LiteDbDataStoreBuilder<Order>("myapp.db"));      // → "Order" collection
AddStore(new LiteDbDataStoreBuilder<Customer>("myapp.db"));   // → "Customer" collection
AddStore(new LiteDbDataStoreBuilder<Product>("myapp.db"));    // → "Product" collection

// With all options
AddStore(new LiteDbDataStoreBuilder<Invoice>(
    databasePath: "C:\\Data\\myapp.db",
    autoLoad: true,
    autoSave: true,
    comparer: new InvoiceNumberComparer(),
    synchronizationContext: SynchronizationContext.Current));
```

## Creating a Registrar

### Basic Pattern

Inherit from `DataStoreRegistrarBase` and call `AddStore()` in the constructor:

```csharp
public class MyAppStoreRegistrar : DataStoreRegistrarBase
{
    public MyAppStoreRegistrar(string dbPath)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
        AddStore(new JsonDataStoreBuilder<Customer>("customers.json"));
        AddStore(new LiteDbDataStoreBuilder<Order>(dbPath));
    }
}
```

### Registration

Register your registrar during application startup:

```csharp
var services = new ServiceCollection();

// Register DataStores framework
var module = new DataStoresServiceModule();
module.Register(services);

// Register your custom registrar
services.AddDataStoreRegistrar(new MyAppStoreRegistrar("C:\\Data\\myapp.db"));

var provider = services.BuildServiceProvider();
```

### Bootstrap

Execute bootstrap before accessing stores:

```csharp
await DataStoreBootstrap.RunAsync(provider);
```

### Access Stores

Use `IDataStores` facade exclusively:

```csharp
var stores = provider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();
```

## Complete Example

```csharp
// Entities
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class Order : EntityBase
{
    public string OrderNumber { get; set; }
    public decimal Total { get; set; }
    
    public override string ToString() => $"Order #{Id}: {OrderNumber}";
    public override bool Equals(object? obj) => obj is Order o && Id > 0 && Id == o.Id;
    public override int GetHashCode() => Id;
}

// Registrar
public class MyAppStoreRegistrar : DataStoreRegistrarBase
{
    public MyAppStoreRegistrar(string dbPath)
    {
        // In-memory for temporary data
        AddStore(new InMemoryDataStoreBuilder<Product>());
        
        // JSON for configuration
        AddStore(new JsonDataStoreBuilder<Settings>(
            filePath: "C:\\Data\\settings.json",
            autoSave: false)); // Manual save
        
        // LiteDB for business entities
        AddStore(new LiteDbDataStoreBuilder<Order>(
            databasePath: dbPath));
    }
}

// Startup
var services = new ServiceCollection();
new DataStoresServiceModule().Register(services);
services.AddDataStoreRegistrar(new MyAppStoreRegistrar("C:\\Data\\myapp.db"));

var provider = services.BuildServiceProvider();
await DataStoreBootstrap.RunAsync(provider);

// Usage
var stores = provider.GetRequiredService<IDataStores>();
var productStore = stores.GetGlobal<Product>();
productStore.Add(new Product { Id = 1, Name = "Laptop", Price = 999.99m });
```

## Advanced Scenarios

### Custom Comparer

```csharp
public class ProductIdComparer : IEqualityComparer<Product>
{
    public bool Equals(Product? x, Product? y)
    {
        if (x == null || y == null) return false;
        return x.Id == y.Id;
    }
    
    public int GetHashCode(Product obj) => obj.Id;
}

// Usage
AddStore(new InMemoryDataStoreBuilder<Product>(
    comparer: new ProductIdComparer()));
```

### UI-Thread Event Marshalling (WPF)

```csharp
public class WpfStoreRegistrar : DataStoreRegistrarBase
{
    public WpfStoreRegistrar(SynchronizationContext uiContext)
    {
        // All events are marshalled to UI thread
        AddStore(new JsonDataStoreBuilder<Customer>(
            filePath: "customers.json",
            synchronizationContext: uiContext));
    }
}

// In App.xaml.cs
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var services = new ServiceCollection();
        new DataStoresServiceModule().Register(services);
        
        // Capture UI SynchronizationContext
        services.AddDataStoreRegistrar(
            new WpfStoreRegistrar(SynchronizationContext.Current!));
        
        var provider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(provider);
        
        // Now safe to bind stores to UI
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
```

### Multiple Environments

```csharp
public class EnvironmentStoreRegistrar : DataStoreRegistrarBase
{
    public EnvironmentStoreRegistrar(string environment, string basePath)
    {
        if (environment == "Production")
        {
            // Production: LiteDB with auto-save
            AddStore(new LiteDbDataStoreBuilder<Order>(
                databasePath: Path.Combine(basePath, "production.db"),
                autoSave: true));
        }
        else
        {
            // Development: JSON for easy debugging
            AddStore(new JsonDataStoreBuilder<Order>(
                filePath: Path.Combine(basePath, "dev-orders.json"),
                autoSave: true));
        }
    }
}
```

## Migration from Old Pattern

### Before (Manual Registration)

```csharp
public class OldProductRegistrar : IDataStoreRegistrar
{
    private readonly string _dbPath;
    
    public OldProductRegistrar(string dbPath)
    {
        _dbPath = dbPath;
    }
    
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // Manual InMemory
        registry.RegisterGlobal(new InMemoryDataStore<Product>());
        
        // Manual JSON (8 lines)
        var jsonStrategy = new JsonFilePersistenceStrategy<Customer>("customers.json");
        var jsonInner = new InMemoryDataStore<Customer>();
        var jsonDecorator = new PersistentStoreDecorator<Customer>(
            jsonInner, jsonStrategy, true, true);
        registry.RegisterGlobal(jsonDecorator);
        
        // Manual LiteDB (9 lines)
        var liteStrategy = new LiteDbPersistenceStrategy<Order>(_dbPath, "orders");
        var liteInner = new InMemoryDataStore<Order>();
        var liteDecorator = new PersistentStoreDecorator<Order>(
            liteInner, liteStrategy, true, true);
        registry.RegisterGlobal(liteDecorator);
    }
}
```

### After (Builder Pattern)

```csharp
public class NewProductRegistrar : DataStoreRegistrarBase
{
    public NewProductRegistrar(string dbPath)
    {
        AddStore(new InMemoryDataStoreBuilder<Product>());
        AddStore(new JsonDataStoreBuilder<Customer>("customers.json"));
        AddStore(new LiteDbDataStoreBuilder<Order>(dbPath));
    }
}
```

**Result:** 3 lines instead of 20+ lines. Same functionality, better clarity.

---

**Version:** 1.0.0  
**Last Updated:** January 2025
