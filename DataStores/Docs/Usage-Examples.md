# DataStores - Verwendungsbeispiele

Praktische Beispiele für häufige Szenarien und Anwendungsfälle mit der DataStores-Bibliothek.

## ?? Inhaltsverzeichnis

- [Grundlegende Beispiele](#grundlegende-beispiele)
- [Persistierung](#persistierung)
- [Eltern-Kind-Beziehungen](#eltern-kind-beziehungen)
- [UI-Integration](#ui-integration)
- [Fortgeschrittene Szenarien](#fortgeschrittene-szenarien)

---

## Grundlegende Beispiele

### Beispiel 1: Einfacher Produkt-Store

```csharp
using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Produkt-Modell für das Beispiel.
/// </summary>
public class Product
{
    /// <summary>Eindeutige ID des Produkts.</summary>
    public int Id { get; set; }
    
    /// <summary>Name des Produkts.</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Preis des Produkts.</summary>
    public decimal Price { get; set; }
    
    /// <summary>Kategorie-ID.</summary>
    public int CategoryId { get; set; }
    
    /// <summary>Gibt an, ob das Produkt aktiv ist.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Registrar für den Product-Store.
/// </summary>
public class ProductStoreRegistrar : IDataStoreRegistrar
{
    /// <summary>
    /// Registriert den globalen Product-Store.
    /// </summary>
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        registry.RegisterGlobal(new InMemoryDataStore<Product>());
    }
}

/// <summary>
/// Hauptprogramm.
/// </summary>
public class Program
{
    public static async Task Main()
    {
        // 1. Services einrichten
        var services = new ServiceCollection();
        services.AddDataStoresCore();
        services.AddDataStoreRegistrar<ProductStoreRegistrar>();
        
        var provider = services.BuildServiceProvider();
        
        // 2. Bootstrap ausführen
        await DataStoreBootstrap.RunAsync(provider);
        
        // 3. Store verwenden
        var stores = provider.GetRequiredService<IDataStores>();
        var productStore = stores.GetGlobal<Product>();
        
        // Produkt hinzufügen
        productStore.Add(new Product 
        { 
            Id = 1, 
            Name = "Laptop", 
            Price = 999.99m,
            CategoryId = 1,
            IsActive = true 
        });
        
        // Mehrere Produkte hinzufügen
        productStore.AddRange(new[]
        {
            new Product { Id = 2, Name = "Maus", Price = 29.99m, CategoryId = 2, IsActive = true },
            new Product { Id = 3, Name = "Tastatur", Price = 79.99m, CategoryId = 2, IsActive = false },
            new Product { Id = 4, Name = "Monitor", Price = 299.99m, CategoryId = 1, IsActive = true }
        });
        
        // Alle Produkte anzeigen
        Console.WriteLine($"Anzahl Produkte: {productStore.Items.Count}");
        foreach (var product in productStore.Items)
        {
            Console.WriteLine($"- {product.Name}: {product.Price:C}");
        }
        
        // Produkt entfernen
        var toRemove = productStore.Items.FirstOrDefault(p => p.Id == 3);
        if (toRemove != null)
        {
            productStore.Remove(toRemove);
            Console.WriteLine($"Produkt '{toRemove.Name}' entfernt");
        }
    }
}
```

### Beispiel 2: Events verwenden

```csharp
/// <summary>
/// Service, der auf Store-Änderungen reagiert.
/// </summary>
public class ProductNotificationService
{
    private readonly IDataStore<Product> _store;
    
    /// <summary>
    /// Initialisiert den Service und abonniert Events.
    /// </summary>
    public ProductNotificationService(IDataStores stores)
    {
        _store = stores.GetGlobal<Product>();
        _store.Changed += OnProductStoreChanged;
    }
    
    /// <summary>
    /// Handler für Store-Änderungen mit detaillierter Behandlung.
    /// </summary>
    private void OnProductStoreChanged(object? sender, DataStoreChangedEventArgs<Product> e)
    {
        switch (e.ChangeType)
        {
            case DataStoreChangeType.Add:
                var added = e.AffectedItems[0];
                Console.WriteLine($"? Neues Produkt: {added.Name} - {added.Price:C}");
                SendNotification($"Produkt '{added.Name}' wurde hinzugefügt");
                break;
                
            case DataStoreChangeType.BulkAdd:
                Console.WriteLine($"? {e.AffectedItems.Count} Produkte hinzugefügt");
                break;
                
            case DataStoreChangeType.Remove:
                var removed = e.AffectedItems[0];
                Console.WriteLine($"? Produkt entfernt: {removed.Name}");
                break;
                
            case DataStoreChangeType.Clear:
                Console.WriteLine("??? Alle Produkte gelöscht");
                break;
        }
    }
    
    /// <summary>
    /// Sendet eine Benachrichtigung (Beispiel).
    /// </summary>
    private void SendNotification(string message)
    {
        // Implementierung für Benachrichtigungssystem
        Console.WriteLine($"?? Benachrichtigung: {message}");
    }
    
    /// <summary>
    /// Ressourcen aufräumen.
    /// </summary>
    public void Dispose()
    {
        _store.Changed -= OnProductStoreChanged;
    }
}
```

### Beispiel 3: Lokale Stores und Snapshots

```csharp
/// <summary>
/// Service, der mit lokalen Stores arbeitet.
/// </summary>
public class ProductFilterService
{
    private readonly IDataStores _stores;
    
    public ProductFilterService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Erstellt einen gefilterten lokalen Store nur mit aktiven Produkten.
    /// </summary>
    public IDataStore<Product> GetActiveProducts()
    {
        return _stores.CreateLocalSnapshotFromGlobal<Product>(
            p => p.IsActive);
    }
    
    /// <summary>
    /// Erstellt einen gefilterten Store nach Kategorie.
    /// </summary>
    public IDataStore<Product> GetProductsByCategory(int categoryId)
    {
        return _stores.CreateLocalSnapshotFromGlobal<Product>(
            p => p.CategoryId == categoryId);
    }
    
    /// <summary>
    /// Erstellt einen leeren lokalen Store für temporäre Daten.
    /// </summary>
    public IDataStore<Product> CreateTemporaryStore()
    {
        var localStore = _stores.CreateLocal<Product>();
        
        // Temporäre Produkte hinzufügen
        localStore.Add(new Product 
        { 
            Id = -1, 
            Name = "Temp Product", 
            Price = 0 
        });
        
        return localStore;
    }
    
    /// <summary>
    /// Beispiel: Arbeiten mit lokalen und globalen Stores.
    /// </summary>
    public void DemoLocalVsGlobal()
    {
        // Globaler Store
        var globalStore = _stores.GetGlobal<Product>();
        Console.WriteLine($"Global: {globalStore.Items.Count} Produkte");
        
        // Lokaler Snapshot
        var activeStore = GetActiveProducts();
        Console.WriteLine($"Aktiv: {activeStore.Items.Count} Produkte");
        
        // Änderungen am lokalen Store beeinflussen NICHT den globalen Store
        activeStore.Clear();
        Console.WriteLine($"Nach Clear - Lokal: {activeStore.Items.Count}, Global: {globalStore.Items.Count}");
    }
}
```

### Beispiel 4: Custom Comparer

```csharp
/// <summary>
/// Produkt-Comparer, der nur die ID vergleicht.
/// </summary>
public class ProductIdComparer : IEqualityComparer<Product>
{
    /// <summary>
    /// Vergleicht zwei Produkte basierend auf ihrer ID.
    /// </summary>
    public bool Equals(Product? x, Product? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Id == y.Id;
    }
    
    /// <summary>
    /// Berechnet den Hash-Code basierend auf der ID.
    /// </summary>
    public int GetHashCode(Product obj)
    {
        return obj.Id.GetHashCode();
    }
}

/// <summary>
/// Registrar mit Custom Comparer.
/// </summary>
public class ProductStoreWithComparerRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var comparer = new ProductIdComparer();
        registry.RegisterGlobal(new InMemoryDataStore<Product>(comparer));
    }
}

/// <summary>
/// Verwendung des Comparers.
/// </summary>
public void DemoComparer()
{
    var stores = /* ... */;
    var store = stores.GetGlobal<Product>();
    
    var product1 = new Product { Id = 1, Name = "Original" };
    var product2 = new Product { Id = 1, Name = "Duplikat" }; // Gleiche ID!
    
    store.Add(product1);
    
    // Contains verwendet den Comparer - true, weil ID gleich ist
    bool contains = store.Contains(product2);
    Console.WriteLine($"Enthält Duplikat: {contains}"); // true
}
```

---

## Persistierung

### Beispiel 5: JSON-Persistierung

```csharp
using System.Text.Json;
using DataStores.Persistence;

/// <summary>
/// JSON-basierte Persistierungsstrategie.
/// </summary>
public class JsonPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options;
    
    /// <summary>
    /// Initialisiert die Strategie mit Dateipfad.
    /// </summary>
    public JsonPersistenceStrategy(string filePath)
    {
        _filePath = filePath;
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }
    
    /// <summary>
    /// Lädt Daten aus JSON-Datei.
    /// </summary>
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"?? Datei nicht gefunden: {_filePath}");
                return Array.Empty<T>();
            }
            
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var items = JsonSerializer.Deserialize<List<T>>(json, _options);
            
            Console.WriteLine($"? {items?.Count ?? 0} Elemente aus {_filePath} geladen");
            return items ?? new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Fehler beim Laden: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Speichert Daten in JSON-Datei.
    /// </summary>
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(items, _options);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
            
            Console.WriteLine($"?? {items.Count} Elemente in {_filePath} gespeichert");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Fehler beim Speichern: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Registrar mit persistentem Store.
/// </summary>
public class PersistentProductRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var strategy = new JsonPersistenceStrategy<Product>("Data/products.json");
        
        // Store mit Auto-Load und Auto-Save registrieren
        var persistentStore = registry.RegisterPersistent(
            strategy,
            autoLoad: true,
            autoSaveOnChange: true);
            
        Console.WriteLine("?? Persistenter Product-Store registriert");
    }
}
```

### Beispiel 6: XML-Persistierung

```csharp
using System.Xml.Serialization;

/// <summary>
/// XML-basierte Persistierungsstrategie.
/// </summary>
public class XmlPersistenceStrategy<T> : IPersistenceStrategy<T> where T : class
{
    private readonly string _filePath;
    private readonly XmlSerializer _serializer;
    
    /// <summary>
    /// Initialisiert die Strategie.
    /// </summary>
    public XmlPersistenceStrategy(string filePath)
    {
        _filePath = filePath;
        _serializer = new XmlSerializer(typeof(List<T>));
    }
    
    /// <summary>
    /// Lädt Daten aus XML-Datei.
    /// </summary>
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<T>();
            
        await using var stream = File.OpenRead(_filePath);
        var items = _serializer.Deserialize(stream) as List<T>;
        return items ?? new List<T>();
    }
    
    /// <summary>
    /// Speichert Daten in XML-Datei.
    /// </summary>
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(_filePath);
        _serializer.Serialize(stream, items.ToList());
    }
}
```

### Beispiel 7: Datenbank-Persistierung (Entity Framework)

```csharp
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework-basierte Persistierungsstrategie.
/// </summary>
public class EfCorePersistenceStrategy<T> : IPersistenceStrategy<T> 
    where T : class, IEntity
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    
    /// <summary>
    /// Initialisiert die Strategie mit DbContext-Factory.
    /// </summary>
    public EfCorePersistenceStrategy(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    /// <summary>
    /// Lädt alle Entitäten aus der Datenbank.
    /// </summary>
    public async Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<T>().ToListAsync(cancellationToken);
    }
    
    /// <summary>
    /// Speichert alle Entitäten in die Datenbank.
    /// </summary>
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        // Alte Daten löschen
        var existing = await context.Set<T>().ToListAsync(cancellationToken);
        context.Set<T>().RemoveRange(existing);
        
        // Neue Daten hinzufügen
        await context.Set<T>().AddRangeAsync(items, cancellationToken);
        
        await context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Interface für Entitäten mit ID.
/// </summary>
public interface IEntity
{
    int Id { get; set; }
}
```

---

## Eltern-Kind-Beziehungen

### Beispiel 8: Kategorie-Produkt-Beziehung

```csharp
/// <summary>
/// Kategorie-Modell.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Service für Kategorie-Produkt-Beziehungen.
/// </summary>
public class CategoryProductService
{
    private readonly IDataStores _stores;
    
    public CategoryProductService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Erstellt eine Beziehung zwischen Kategorie und ihren Produkten.
    /// </summary>
    public ParentChildRelationship<Category, Product> CreateRelationship(Category category)
    {
        var relationship = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
            
        // Globalen Store als Datenquelle verwenden
        relationship.UseGlobalDataSource();
        
        // Kinderprodukte laden
        relationship.Refresh();
        
        // Auf Änderungen reagieren
        relationship.Childs.Changed += (s, e) =>
        {
            Console.WriteLine($"Produkte in Kategorie '{category.Name}' geändert: {e.ChangeType}");
        };
        
        return relationship;
    }
    
    /// <summary>
    /// Demo: Arbeiten mit der Beziehung.
    /// </summary>
    public void DemoRelationship()
    {
        var category = new Category { Id = 1, Name = "Electronics" };
        var rel = CreateRelationship(category);
        
        Console.WriteLine($"Kategorie: {rel.Parent.Name}");
        Console.WriteLine($"Anzahl Produkte: {rel.Childs.Items.Count}");
        
        foreach (var product in rel.Childs.Items)
        {
            Console.WriteLine($"  - {product.Name}");
        }
        
        // Neues Produkt zum globalen Store hinzufügen
        var globalStore = _stores.GetGlobal<Product>();
        globalStore.Add(new Product 
        { 
            Id = 999, 
            Name = "Neues Produkt", 
            CategoryId = category.Id 
        });
        
        // Beziehung aktualisieren
        rel.Refresh();
        Console.WriteLine($"Nach Refresh: {rel.Childs.Items.Count} Produkte");
    }
}
```

### Beispiel 9: Verschachtelte Beziehungen

```csharp
/// <summary>
/// Abteilungs-Modell.
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Service mit verschachtelten Beziehungen: Abteilung ? Kategorie ? Produkt.
/// </summary>
public class NestedRelationshipService
{
    private readonly IDataStores _stores;
    
    public NestedRelationshipService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Erstellt eine Hierarchie: Abteilung ? Kategorien ? Produkte.
    /// </summary>
    public void DemoNestedRelationships()
    {
        var department = new Department { Id = 1, Name = "Technik" };
        
        // Ebene 1: Abteilung ? Kategorien
        var deptCategoryRel = new ParentChildRelationship<Department, Category>(
            _stores,
            parent: department,
            filter: (dept, cat) => cat.DepartmentId == dept.Id);
        deptCategoryRel.UseGlobalDataSource();
        deptCategoryRel.Refresh();
        
        Console.WriteLine($"Abteilung: {department.Name}");
        Console.WriteLine($"Kategorien: {deptCategoryRel.Childs.Items.Count}");
        
        // Ebene 2: Für jede Kategorie ? Produkte
        foreach (var category in deptCategoryRel.Childs.Items)
        {
            var catProductRel = new ParentChildRelationship<Category, Product>(
                _stores,
                parent: category,
                filter: (cat, prod) => prod.CategoryId == cat.Id);
            catProductRel.UseGlobalDataSource();
            catProductRel.Refresh();
            
            Console.WriteLine($"  Kategorie: {category.Name}");
            Console.WriteLine($"    Produkte: {catProductRel.Childs.Items.Count}");
            
            foreach (var product in catProductRel.Childs.Items)
            {
                Console.WriteLine($"      - {product.Name}");
            }
        }
    }
}
```

---

## UI-Integration

### Beispiel 10: WPF ViewModel

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

/// <summary>
/// WPF ViewModel für Produkt-Verwaltung.
/// </summary>
public class ProductViewModel : INotifyPropertyChanged
{
    private readonly IDataStores _stores;
    private readonly IDataStore<Product> _productStore;
    
    /// <summary>
    /// Observable Collection für UI-Binding.
    /// </summary>
    public ObservableCollection<Product> Products { get; }
    
    /// <summary>
    /// Command zum Hinzufügen eines Produkts.
    /// </summary>
    public ICommand AddProductCommand { get; }
    
    /// <summary>
    /// Command zum Entfernen eines Produkts.
    /// </summary>
    public ICommand RemoveProductCommand { get; }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Initialisiert das ViewModel.
    /// </summary>
    public ProductViewModel(IDataStores stores)
    {
        _stores = stores;
        _productStore = _stores.GetGlobal<Product>();
        
        Products = new ObservableCollection<Product>(_productStore.Items);
        
        // Events abonnieren - auf UI-Thread marshallt
        var syncContext = SynchronizationContext.Current;
        var storeWithSync = new InMemoryDataStore<Product>(
            comparer: null,
            synchronizationContext: syncContext);
            
        _productStore.Changed += OnProductStoreChanged;
        
        AddProductCommand = new RelayCommand(AddProduct);
        RemoveProductCommand = new RelayCommand<Product>(RemoveProduct);
    }
    
    /// <summary>
    /// Handler für Store-Änderungen - aktualisiert UI.
    /// </summary>
    private void OnProductStoreChanged(object? sender, DataStoreChangedEventArgs<Product> e)
    {
        // Läuft auf UI-Thread dank SynchronizationContext
        switch (e.ChangeType)
        {
            case DataStoreChangeType.Add:
                foreach (var item in e.AffectedItems)
                    Products.Add(item);
                break;
                
            case DataStoreChangeType.Remove:
                foreach (var item in e.AffectedItems)
                    Products.Remove(item);
                break;
                
            case DataStoreChangeType.Clear:
                Products.Clear();
                break;
                
            case DataStoreChangeType.BulkAdd:
                foreach (var item in e.AffectedItems)
                    Products.Add(item);
                break;
        }
    }
    
    /// <summary>
    /// Fügt ein neues Produkt hinzu.
    /// </summary>
    private void AddProduct()
    {
        var newProduct = new Product
        {
            Id = Products.Count + 1,
            Name = $"Produkt {Products.Count + 1}",
            Price = 99.99m,
            IsActive = true
        };
        
        _productStore.Add(newProduct);
    }
    
    /// <summary>
    /// Entfernt ein Produkt.
    /// </summary>
    private void RemoveProduct(Product? product)
    {
        if (product != null)
        {
            _productStore.Remove(product);
        }
    }
}

/// <summary>
/// Einfache RelayCommand-Implementierung.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    
    public RelayCommand(Action execute) => _execute = execute;
    
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged;
}

/// <summary>
/// Generische RelayCommand-Implementierung.
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    
    public RelayCommand(Action<T?> execute) => _execute = execute;
    
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute((T?)parameter);
    public event EventHandler? CanExecuteChanged;
}
```

---

## Fortgeschrittene Szenarien

### Beispiel 11: Mehrere Registrare

```csharp
/// <summary>
/// Registrar für Produkt-bezogene Stores.
/// </summary>
public class ProductModuleRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // Product Store mit Persistierung
        var productStrategy = new JsonPersistenceStrategy<Product>("Data/products.json");
        registry.RegisterPersistent(productStrategy);
        
        // Category Store
        registry.RegisterGlobal(new InMemoryDataStore<Category>());
    }
}

/// <summary>
/// Registrar für Benutzer-bezogene Stores.
/// </summary>
public class UserModuleRegistrar : IDataStoreRegistrar
{
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // User Store mit Datenbank-Persistierung
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var userStrategy = new EfCorePersistenceStrategy<User>(factory);
        registry.RegisterPersistent(userStrategy);
    }
}

/// <summary>
/// Registrierung mehrerer Module.
/// </summary>
public void ConfigureServices(IServiceCollection services)
{
    services.AddDataStoresCore();
    services.AddDataStoreRegistrar<ProductModuleRegistrar>();
    services.AddDataStoreRegistrar<UserModuleRegistrar>();
}
```

### Beispiel 12: Conditional Registration

```csharp
/// <summary>
/// Registrar mit bedingter Registrierung basierend auf Konfiguration.
/// </summary>
public class ConditionalRegistrar : IDataStoreRegistrar
{
    private readonly IConfiguration _configuration;
    
    public ConditionalRegistrar(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        var usePersistence = _configuration.GetValue<bool>("DataStores:UsePersistence");
        
        if (usePersistence)
        {
            // Mit Persistierung
            var strategy = new JsonPersistenceStrategy<Product>("Data/products.json");
            registry.RegisterPersistent(strategy);
        }
        else
        {
            // Nur In-Memory
            registry.RegisterGlobal(new InMemoryDataStore<Product>());
        }
    }
}
```

---

**Version**: 1.0.0  
**Weitere Beispiele**: Siehe [API-Referenz](API-Reference.md) und [Persistence-Guide](Persistence-Guide.md)
