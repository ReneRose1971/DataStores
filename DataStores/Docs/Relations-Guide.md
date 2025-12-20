# DataStores - Beziehungen Guide

Umfassender Leitfaden zur Verwaltung von Eltern-Kind-Beziehungen mit ParentChildRelationship.

## ?? Inhaltsverzeichnis

- [Übersicht](#übersicht)
- [ParentChildRelationship](#parentchildrelationship)
- [Verwendungsbeispiele](#verwendungsbeispiele)
- [Best Practices](#best-practices)
- [Fortgeschrittene Szenarien](#fortgeschrittene-szenarien)

---

## Übersicht

Die `ParentChildRelationship<TParent, TChild>` Klasse ermöglicht es, hierarchische Beziehungen zwischen verschiedenen Entitätstypen zu verwalten.

### Hauptmerkmale

- ? **Typsicher**: Generische Typen für Eltern und Kinder
- ? **Flexibel**: Benutzerdefinierte Filter-Funktionen
- ? **Reactive**: Automatische Updates via `Refresh()`
- ? **Multiple Datenquellen**: Global Store oder Snapshots
- ? **Event-basiert**: `Childs` Collection unterstützt Events

### Typische Anwendungsfälle

- Kategorie ? Produkte
- Abteilung ? Mitarbeiter
- Bestellung ? Bestellpositionen
- Kunde ? Bestellungen
- Projekt ? Aufgaben

---

## ParentChildRelationship

### Klassen-Definition

```csharp
/// <summary>
/// Verwaltet eine Eltern-Kind-Beziehung zwischen Datenspeichern.
/// </summary>
public class ParentChildRelationship<TParent, TChild>
    where TParent : class
    where TChild : class
{
    /// <summary>Die Eltern-Entität.</summary>
    public TParent Parent { get; init; }
    
    /// <summary>Die Datenquelle für Kind-Elemente.</summary>
    public IDataStore<TChild> DataSource { get; set; }
    
    /// <summary>Die lokale Sammlung gefilterter Kind-Elemente.</summary>
    public InMemoryDataStore<TChild> Childs { get; }
    
    /// <summary>Die Filter-Funktion.</summary>
    public Func<TParent, TChild, bool> Filter { get; init; }
}
```

### Konstruktor

```csharp
var relationship = new ParentChildRelationship<Category, Product>(
    stores: dataStores,                          // IDataStores Facade
    parent: myCategoryInstance,                  // Die Eltern-Entität
    filter: (cat, prod) => prod.CategoryId == cat.Id  // Filter-Logik
);
```

---

## Verwendungsbeispiele

### Beispiel 1: Einfache Kategorie-Produkt-Beziehung

```csharp
/// <summary>
/// Kategorie-Modell.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Produkt-Modell.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
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
    /// Erstellt eine Beziehung für eine Kategorie.
    /// </summary>
    public ParentChildRelationship<Category, Product> GetProductsForCategory(Category category)
    {
        // 1. Beziehung erstellen
        var relationship = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        
        // 2. Globalen Store als Datenquelle verwenden
        relationship.UseGlobalDataSource();
        
        // 3. Kinderprodukte laden
        relationship.Refresh();
        
        return relationship;
    }
    
    /// <summary>
    /// Demo-Verwendung.
    /// </summary>
    public void Demo()
    {
        var category = new Category { Id = 1, Name = "Electronics" };
        var rel = GetProductsForCategory(category);
        
        Console.WriteLine($"Kategorie: {rel.Parent.Name}");
        Console.WriteLine($"Produkte: {rel.Childs.Items.Count}");
        
        foreach (var product in rel.Childs.Items)
        {
            Console.WriteLine($"  - {product.Name}: {product.Price:C}");
        }
    }
}
```

### Beispiel 2: Mit Events

```csharp
/// <summary>
/// Service mit Event-Handling für Kinderänderungen.
/// </summary>
public class CategoryProductViewModel
{
    private readonly IDataStores _stores;
    private ParentChildRelationship<Category, Product>? _relationship;
    
    public CategoryProductViewModel(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Lädt eine Kategorie und abonniert Änderungen.
    /// </summary>
    public void LoadCategory(Category category)
    {
        // Alte Beziehung aufräumen
        if (_relationship != null)
        {
            _relationship.Childs.Changed -= OnChildProductsChanged;
        }
        
        // Neue Beziehung erstellen
        _relationship = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        
        _relationship.UseGlobalDataSource();
        _relationship.Refresh();
        
        // Events abonnieren
        _relationship.Childs.Changed += OnChildProductsChanged;
    }
    
    /// <summary>
    /// Handler für Änderungen an Kindprodukten.
    /// </summary>
    private void OnChildProductsChanged(object? sender, DataStoreChangedEventArgs<Product> e)
    {
        Console.WriteLine($"Kinderprodukte geändert: {e.ChangeType}");
        
        switch (e.ChangeType)
        {
            case DataStoreChangeType.Add:
                var added = e.AffectedItems[0];
                Console.WriteLine($"Produkt hinzugefügt: {added.Name}");
                break;
                
            case DataStoreChangeType.Remove:
                Console.WriteLine("Produkt entfernt");
                break;
        }
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // UI-Aktualisierungs-Logik
    }
}
```

### Beispiel 3: Snapshot statt Global Store

```csharp
/// <summary>
/// Verwendet einen lokalen Snapshot statt des globalen Stores.
/// </summary>
public class SnapshotExample
{
    public void DemoSnapshot(IDataStores stores, Category category)
    {
        var relationship = new ParentChildRelationship<Category, Product>(
            stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        
        // Snapshot erstellen - nur aktive Produkte als Datenquelle
        relationship.UseSnapshotFromGlobal(prod => prod.IsActive);
        
        // Refresh filtert dann zusätzlich nach CategoryId
        relationship.Refresh();
        
        // Resultat: Nur aktive Produkte der Kategorie
        Console.WriteLine($"Aktive Produkte: {relationship.Childs.Items.Count}");
    }
}
```

### Beispiel 4: Manuelles Hinzufügen von Kindern

```csharp
/// <summary>
/// Manuelle Verwaltung der Kinder-Collection.
/// </summary>
public class ManualChildManagement
{
    public void Demo(IDataStores stores, Category category)
    {
        var relationship = new ParentChildRelationship<Category, Product>(
            stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        
        relationship.UseGlobalDataSource();
        relationship.Refresh();
        
        // Neues Produkt zum globalen Store hinzufügen
        var globalStore = stores.GetGlobal<Product>();
        var newProduct = new Product 
        { 
            Id = 999, 
            Name = "Neues Produkt", 
            CategoryId = category.Id 
        };
        globalStore.Add(newProduct);
        
        // Refresh, um Änderungen zu übernehmen
        relationship.Refresh();
        
        // Oder: Manuell zu Childs hinzufügen (ohne Refresh)
        relationship.Childs.Add(newProduct);
    }
}
```

---

## Best Practices

### 1. Ressourcen-Management

```csharp
/// <summary>
/// Service mit ordnungsgemäßem Ressourcen-Management.
/// </summary>
public class ProductCategoryService : IDisposable
{
    private readonly IDataStores _stores;
    private readonly List<ParentChildRelationship<Category, Product>> _relationships = new();
    
    public ProductCategoryService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Erstellt und trackt eine Beziehung.
    /// </summary>
    public ParentChildRelationship<Category, Product> CreateRelationship(Category category)
    {
        var rel = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        
        rel.UseGlobalDataSource();
        rel.Refresh();
        rel.Childs.Changed += OnChildsChanged;
        
        _relationships.Add(rel);
        return rel;
    }
    
    private void OnChildsChanged(object? sender, DataStoreChangedEventArgs<Product> e)
    {
        // Event-Handling
    }
    
    /// <summary>
    /// Ressourcen aufräumen.
    /// </summary>
    public void Dispose()
    {
        foreach (var rel in _relationships)
        {
            rel.Childs.Changed -= OnChildsChanged;
        }
        _relationships.Clear();
    }
}
```

### 2. Lazy Loading

```csharp
/// <summary>
/// Lazy Loading-Pattern für Beziehungen.
/// </summary>
public class LazyRelationshipService
{
    private readonly IDataStores _stores;
    private readonly Dictionary<int, ParentChildRelationship<Category, Product>> _cache = new();
    
    public LazyRelationshipService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Ruft eine Beziehung ab oder erstellt sie lazy.
    /// </summary>
    public ParentChildRelationship<Category, Product> GetOrCreateRelationship(Category category)
    {
        if (_cache.TryGetValue(category.Id, out var existing))
        {
            return existing;
        }
        
        var rel = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        
        rel.UseGlobalDataSource();
        rel.Refresh();
        
        _cache[category.Id] = rel;
        return rel;
    }
    
    /// <summary>
    /// Aktualisiert alle gecachten Beziehungen.
    /// </summary>
    public void RefreshAll()
    {
        foreach (var rel in _cache.Values)
        {
            rel.Refresh();
        }
    }
}
```

### 3. Conditional Filtering

```csharp
/// <summary>
/// Beziehung mit bedingter Filterung.
/// </summary>
public class ConditionalFilterService
{
    /// <summary>
    /// Erstellt eine Beziehung mit optionaler Zusatzfilterung.
    /// </summary>
    public ParentChildRelationship<Category, Product> CreateFilteredRelationship(
        IDataStores stores,
        Category category,
        bool onlyActive,
        decimal? minPrice = null)
    {
        var relationship = new ParentChildRelationship<Category, Product>(
            stores,
            parent: category,
            filter: (cat, prod) =>
            {
                // Basis-Filter: CategoryId
                if (prod.CategoryId != cat.Id)
                    return false;
                
                // Zusatz-Filter: IsActive
                if (onlyActive && !prod.IsActive)
                    return false;
                
                // Zusatz-Filter: MinPrice
                if (minPrice.HasValue && prod.Price < minPrice.Value)
                    return false;
                
                return true;
            });
        
        relationship.UseGlobalDataSource();
        relationship.Refresh();
        
        return relationship;
    }
}
```

---

## Fortgeschrittene Szenarien

### Szenario 1: Verschachtelte Beziehungen (3 Ebenen)

```csharp
/// <summary>
/// Department ? Category ? Product Hierarchie.
/// </summary>
public class NestedRelationshipService
{
    private readonly IDataStores _stores;
    
    public NestedRelationshipService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Erstellt eine 3-Ebenen-Hierarchie.
    /// </summary>
    public void BuildHierarchy(Department department)
    {
        // Ebene 1: Department ? Categories
        var deptCategoryRel = new ParentChildRelationship<Department, Category>(
            _stores,
            parent: department,
            filter: (dept, cat) => cat.DepartmentId == dept.Id);
        deptCategoryRel.UseGlobalDataSource();
        deptCategoryRel.Refresh();
        
        Console.WriteLine($"Abteilung: {department.Name}");
        Console.WriteLine($"  Kategorien: {deptCategoryRel.Childs.Items.Count}");
        
        // Ebene 2: Für jede Category ? Products
        foreach (var category in deptCategoryRel.Childs.Items)
        {
            var catProductRel = new ParentChildRelationship<Category, Product>(
                _stores,
                parent: category,
                filter: (cat, prod) => prod.CategoryId == cat.Id);
            catProductRel.UseGlobalDataSource();
            catProductRel.Refresh();
            
            Console.WriteLine($"    Kategorie: {category.Name}");
            Console.WriteLine($"      Produkte: {catProductRel.Childs.Items.Count}");
            
            foreach (var product in catProductRel.Childs.Items)
            {
                Console.WriteLine($"        - {product.Name}");
            }
        }
    }
}

/// <summary>
/// Abteilungs-Modell.
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

### Szenario 2: Bidirektionale Beziehungen

```csharp
/// <summary>
/// Manager für bidirektionale Beziehungen.
/// </summary>
public class BidirectionalRelationshipManager
{
    private readonly IDataStores _stores;
    
    public BidirectionalRelationshipManager(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Erstellt Beziehungen in beiden Richtungen.
    /// </summary>
    public (
        ParentChildRelationship<Category, Product> CategoryToProducts,
        ParentChildRelationship<Product, Category> ProductToCategory
    ) CreateBidirectional(Category category, Product product)
    {
        // Vorwärts: Category ? Products
        var forward = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        forward.UseGlobalDataSource();
        forward.Refresh();
        
        // Rückwärts: Product ? Category (1:1 Beziehung)
        var backward = new ParentChildRelationship<Product, Category>(
            _stores,
            parent: product,
            filter: (prod, cat) => cat.Id == prod.CategoryId);
        backward.UseGlobalDataSource();
        backward.Refresh();
        
        return (forward, backward);
    }
}
```

### Szenario 3: Aggregierte Beziehungen

```csharp
/// <summary>
/// Service mit Aggregations-Funktionen.
/// </summary>
public class AggregatedRelationshipService
{
    private readonly IDataStores _stores;
    
    public AggregatedRelationshipService(IDataStores stores)
    {
        _stores = stores;
    }
    
    /// <summary>
    /// Berechnet Statistiken für eine Kategorie.
    /// </summary>
    public CategoryStatistics GetCategoryStatistics(Category category)
    {
        var rel = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        rel.UseGlobalDataSource();
        rel.Refresh();
        
        var products = rel.Childs.Items;
        
        return new CategoryStatistics
        {
            CategoryName = category.Name,
            TotalProducts = products.Count,
            ActiveProducts = products.Count(p => p.IsActive),
            TotalValue = products.Sum(p => p.Price),
            AveragePrice = products.Any() ? products.Average(p => p.Price) : 0
        };
    }
}

/// <summary>
/// Statistik-DTO.
/// </summary>
public class CategoryStatistics
{
    public string CategoryName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AveragePrice { get; set; }
}
```

### Szenario 4: Reactive Updates

```csharp
/// <summary>
/// Service mit reaktiven Auto-Updates.
/// </summary>
public class ReactiveRelationshipService
{
    private readonly IDataStores _stores;
    private readonly IDataStore<Product> _globalProductStore;
    private ParentChildRelationship<Category, Product>? _currentRelationship;
    
    public ReactiveRelationshipService(IDataStores stores)
    {
        _stores = stores;
        _globalProductStore = _stores.GetGlobal<Product>();
        
        // Auf globale Änderungen reagieren
        _globalProductStore.Changed += OnGlobalProductStoreChanged;
    }
    
    /// <summary>
    /// Lädt eine Kategorie.
    /// </summary>
    public void LoadCategory(Category category)
    {
        _currentRelationship = new ParentChildRelationship<Category, Product>(
            _stores,
            parent: category,
            filter: (cat, prod) => prod.CategoryId == cat.Id);
        _currentRelationship.UseGlobalDataSource();
        _currentRelationship.Refresh();
    }
    
    /// <summary>
    /// Automatisch refresh bei globalen Änderungen.
    /// </summary>
    private void OnGlobalProductStoreChanged(object? sender, DataStoreChangedEventArgs<Product> e)
    {
        if (_currentRelationship == null)
            return;
        
        // Prüfen, ob Änderung relevant ist
        bool isRelevant = e.AffectedItems.Any(p => 
            _currentRelationship.Filter(_currentRelationship.Parent, p));
        
        if (isRelevant)
        {
            Console.WriteLine("Relevante Änderung erkannt - Refresh...");
            _currentRelationship.Refresh();
        }
    }
}
```

---

## Häufige Fehler vermeiden

### ? FEHLER: DataSource nicht gesetzt

```csharp
var rel = new ParentChildRelationship<Category, Product>(...);
rel.Refresh(); // ? InvalidOperationException!
```

### ? KORREKT: DataSource immer setzen

```csharp
var rel = new ParentChildRelationship<Category, Product>(...);
rel.UseGlobalDataSource(); // ? DataSource gesetzt
rel.Refresh(); // ? Funktioniert
```

### ? FEHLER: Events nicht aufräumen

```csharp
void LoadCategory(Category cat)
{
    var rel = new ParentChildRelationship<Category, Product>(...);
    rel.Childs.Changed += Handler; // ? Memory Leak!
}
```

### ? KORREKT: Events aufräumen

```csharp
ParentChildRelationship<Category, Product>? _rel;

void LoadCategory(Category cat)
{
    if (_rel != null)
        _rel.Childs.Changed -= Handler; // ? Altes Event entfernen
    
    _rel = new ParentChildRelationship<Category, Product>(...);
    _rel.Childs.Changed += Handler; // ? Neues Event hinzufügen
}
```

---

**Version**: 1.0.0  
**Weitere Informationen**: Siehe [API-Referenz](API-Reference.md) und [Usage-Examples](Usage-Examples.md)
