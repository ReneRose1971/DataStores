# 📋 CHAT-ZUSAMMENFASSUNG: PropertyChangedBinder für ParentChildRelationship

**Datum:** 20. Dezember 2025  
**Thema:** Integration des PropertyChangedBinder aus DataToolKit in DataStores  
**Projektziel:** Dynamisches Tracking von Parent-Child-Beziehungen

---

## 🎯 AUSGANGSFRAGE

**Ist die `ParentChildRelationship<TParent, TChild>` Klasse dynamisch?**

**Kern-Fragen:**
1. ✅ Erscheinen Kind-Elemente, die in die globale Datenquelle eingefügt werden, automatisch in der `Childs`-Auflistung?
2. ✅ Führen Changed-Ereignisse, die ein Kind-Element ungültig machen, zum Entfernen aus `Childs`?
3. ✅ Führen Changed-Ereignisse, die ein Element zum Kind-Element machen, zum Einfügen in `Childs`?
4. ✅ Was geschieht, wenn ein `TChild`-Element direkt in die `Childs`-Auflistung eingefügt wird?

---

## 🔍 ANALYSE DER AKTUELLEN IMPLEMENTIERUNG

### **Status Quo in DataStores: Statisch**

Die aktuelle `ParentChildRelationship` ist **statisch bei Initialisierung**:

```csharp
public void Refresh()
{
    if (_dataSource == null) 
        throw new InvalidOperationException("...");

    Childs.Clear();
    var filteredItems = _dataSource.Items.Where(child => Filter(Parent, child));
    Childs.AddRange(filteredItems);
}
```

**Probleme:**
- ❌ Keine automatische Reaktion auf DataSource-Changes
- ❌ Keine PropertyChanged-Überwachung
- ❌ Manuelle `Refresh()` Aufrufe erforderlich
- ❌ Kein dynamisches Verhalten bei Property-Änderungen

---

## 🎯 VERGLEICH: DataToolKit vs. DataStores

### **DataToolKit ParentChildRelationship (Vollständig dynamisch)**

```csharp
public sealed class ParentChildRelationship<TParent, TChild> : IDisposable
{
    private readonly PropertyChangedBinder<TChild> _propertyChangedBinder;

    public ParentChildRelationship(IDataStoreProvider dataStoreProvider)
    {
        // PropertyChangedBinder für automatisches Tracking
        _propertyChangedBinder = new PropertyChangedBinder<TChild>(
            enabled: true,
            onEntityChanged: OnChildPropertyChanged);
    }

    private void OnChildPropertyChanged(TChild item)
    {
        var shouldInclude = ShouldIncludeChild(item);
        var isIncluded = _childStore.Items.Contains(item);

        if (shouldInclude && !isIncluded)
        {
            _childStore.Add(item);  // ← Automatisch hinzufügen
        }
        else if (!shouldInclude && isIncluded)
        {
            _childStore.Remove(item);  // ← Automatisch entfernen
        }
    }
}
```

**Funktionen:**
1. ✅ **CollectionChanged-Tracking:** Neue Items → sofort in Childs
2. ✅ **PropertyChanged-Tracking:** Property-Änderung → automatisch Add/Remove
3. ✅ **Idempotente Bindings:** Verhindert Doppelbindungen automatisch
4. ✅ **Automatisches Dispose:** Räumt alle Event-Handler sauber auf

---

## ✅ LÖSUNG: PropertyChangedBinder Integration

### **Empfehlung**

**JA**, der `PropertyChangedBinder` aus **DataToolKit** sollte auch in **DataStores** verwendet werden!

**Begründung:**
1. ✅ **Bewährtes Pattern** - Bereits erfolgreich in DataToolKit im Einsatz
2. ✅ **Gleicher Anwendungsfall** - Relations brauchen Property-Tracking
3. ✅ **Wiederverwendbar** - Kann 1:1 übernommen werden
4. ✅ **Idempotent** - Verhindert automatisch Doppelbindungen
5. ✅ **Ressourcen-Management** - Sauberes Dispose-Pattern

---

## 📦 IMPLEMENTIERUNGS-PLAN

### **Schritt 1: PropertyChangedBinder.cs kopieren**

**Datei:** `DataStores/Relations/PropertyChangedBinder.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataStores.Relations;

/// <summary>
/// Überwacht INotifyPropertyChanged-Ereignisse und führt bei Änderungen eine Aktion aus.
/// Verhindert automatisch Doppelbindungen durch idempotentes Attach.
/// </summary>
/// <typeparam name="T">Der Typ des zu überwachenden Objekts.</typeparam>
public sealed class PropertyChangedBinder<T> : IDisposable where T : class
{
    private readonly bool _enabled;
    private readonly Action<T> _onEntityChanged;
    private readonly HashSet<T> _bound;
    private bool _disposed;

    public PropertyChangedBinder(bool enabled, Action<T> onEntityChanged)
    {
        _enabled = enabled;
        _onEntityChanged = onEntityChanged ?? throw new ArgumentNullException(nameof(onEntityChanged));
        _bound = new HashSet<T>(ReferenceEqualityComparer<T>.Default);
    }

    /// <summary>
    /// Idempotentes Binden: Event-Handler wird vor dem Anfügen sicherheitshalber entfernt
    /// und danach genau einmal angefügt. Dadurch sind Doppelbindungen ausgeschlossen.
    /// </summary>
    public void Attach(T entity)
    {
        if (!_enabled || entity is not INotifyPropertyChanged npc) return;

        npc.PropertyChanged -= OnEntityPropertyChanged;
        npc.PropertyChanged += OnEntityPropertyChanged;

        _bound.Add(entity);
    }

    public void AttachRange(IEnumerable<T> entities)
    {
        if (!_enabled) return;
        foreach (var e in entities) Attach(e);
    }

    public void Detach(T entity)
    {
        if (!_enabled || entity is not INotifyPropertyChanged npc) return;

        npc.PropertyChanged -= OnEntityPropertyChanged;
        _bound.Remove(entity);
    }

    public void DetachAll()
    {
        if (!_enabled) return;

        foreach (var e in _bound.ToList())
        {
            if (e is INotifyPropertyChanged npc)
                npc.PropertyChanged -= OnEntityPropertyChanged;
        }
        _bound.Clear();
    }

    private void OnEntityPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is T entity)
            _onEntityChanged(entity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DetachAll();
    }
}
```

---

### **Schritt 2: ReferenceEqualityComparer.cs kopieren**

**Datei:** `DataStores/Relations/ReferenceEqualityComparer.cs`

```csharp
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DataStores.Relations;

/// <summary>
/// Referenzbasierter Gleichheitsvergleicher für Referenztypen.
/// </summary>
public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
{
    public static readonly ReferenceEqualityComparer<T> Default = new();

    public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

    public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
}
```

---

### **Schritt 3: ParentChildRelationship.cs erweitern**

**Datei:** `DataStores/Relations/ParentChildRelationship.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Relations;

public class ParentChildRelationship<TParent, TChild> : IDisposable
    where TParent : class
    where TChild : class
{
    private readonly IDataStores _stores;
    private readonly PropertyChangedBinder<TChild> _propertyBinder;
    private IDataStore<TChild>? _dataSource;
    private bool _isSyncing;

    public TParent Parent { get; init; }
    public InMemoryDataStore<TChild> Childs { get; }
    public Func<TParent, TChild, bool> Filter { get; init; }

    public ParentChildRelationship(
        IDataStores stores,
        TParent parent,
        Func<TParent, TChild, bool> filter,
        bool trackPropertyChanges = true)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        Childs = new InMemoryDataStore<TChild>();

        _propertyBinder = new PropertyChangedBinder<TChild>(
            enabled: trackPropertyChanges,
            onEntityChanged: OnChildPropertyChanged);
    }

    public IDataStore<TChild> DataSource
    {
        get => _dataSource ?? throw new InvalidOperationException("DataSource not set");
        set
        {
            if (_dataSource == value) return;

            UnsubscribeFromDataSource();
            _dataSource = value;

            if (_dataSource != null)
            {
                SubscribeToDataSource();
                Refresh();
            }
            else
            {
                Childs.Clear();
            }
        }
    }

    private void SubscribeToDataSource()
    {
        if (_dataSource == null) return;

        _dataSource.Changed += OnDataSourceChanged;
        _propertyBinder.AttachRange(_dataSource.Items);
    }

    private void UnsubscribeFromDataSource()
    {
        if (_dataSource == null) return;

        _dataSource.Changed -= OnDataSourceChanged;
        _propertyBinder.DetachAll();
    }

    private void OnDataSourceChanged(object? sender, DataStoreChangedEventArgs<TChild> e)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            switch (e.ChangeType)
            {
                case DataStoreChangeType.Add:
                    foreach (var item in e.AffectedItems)
                    {
                        _propertyBinder.Attach(item);
                        
                        if (Filter(Parent, item) && !Childs.Contains(item))
                        {
                            Childs.Add(item);
                        }
                    }
                    break;

                case DataStoreChangeType.Remove:
                    foreach (var item in e.AffectedItems)
                    {
                        _propertyBinder.Detach(item);
                        Childs.Remove(item);
                    }
                    break;

                case DataStoreChangeType.Clear:
                    _propertyBinder.DetachAll();
                    Childs.Clear();
                    break;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void OnChildPropertyChanged(TChild item)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            var shouldInclude = Filter(Parent, item);
            var isIncluded = Childs.Contains(item);

            if (shouldInclude && !isIncluded)
            {
                Childs.Add(item);
            }
            else if (!shouldInclude && isIncluded)
            {
                Childs.Remove(item);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public void Refresh()
    {
        if (_isSyncing) return;
        _isSyncing = true;

        try
        {
            if (_dataSource == null) 
                throw new InvalidOperationException("DataSource not set");

            Childs.Clear();
            _propertyBinder.AttachRange(_dataSource.Items);

            var filteredItems = _dataSource.Items.Where(child => Filter(Parent, child));
            Childs.AddRange(filteredItems);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public void Dispose()
    {
        UnsubscribeFromDataSource();
        _propertyBinder.Dispose();
        Childs.Clear();
    }
}
```

---

## 🎬 VERWENDUNGSBEISPIEL

```csharp
// ViewModel mit INotifyPropertyChanged
public class Order : INotifyPropertyChanged
{
    private int _customerId;
    
    public int Id { get; set; }
    
    public int CustomerId
    {
        get => _customerId;
        set
        {
            _customerId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomerId)));
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
}

// Verwendung
var customer = new Customer { Id = 5 };
var relationship = new ParentChildRelationship<Customer, Order>(
    stores,
    parent: customer,
    filter: (cust, order) => order.CustomerId == cust.Id,
    trackPropertyChanges: true);

relationship.UseGlobalDataSource();
relationship.Refresh();

// ✅ Szenario 1: Neues Kind zur DataSource hinzufügen
var order1 = new Order { Id = 1, CustomerId = 5 };
globalOrderStore.Add(order1);  // → Automatisch in relationship.Childs

// ✅ Szenario 2: Property ändern → automatisch entfernt
order1.CustomerId = 999;  // → Automatisch aus relationship.Childs entfernt!

// ✅ Szenario 3: Property zurückändern → automatisch hinzugefügt
order1.CustomerId = 5;  // → Automatisch wieder in relationship.Childs eingefügt!
```

---

## 📊 VERGLEICH: Vorher vs. Nachher

| Feature | Ohne Binder | Mit Binder |
|---------|-------------|------------|
| **Property-Tracking** | ❌ Manuell | ✅ Automatisch |
| **Doppelbindungen** | ⚠️ Möglich | ✅ Verhindert (idempotent) |
| **Ressourcen-Management** | ⚠️ Komplex | ✅ Dispose() räumt auf |
| **Code-Duplikation** | ⚠️ Überall Event-Handler | ✅ Zentrale Lösung |
| **Dynamisches Verhalten** | ❌ Nur bei Refresh() | ✅ Echtzeit-Synchronisation |
| **DataSource-Sync** | ❌ Manuell | ✅ Automatisch |

---

## 🎯 ANTWORTEN AUF DIE URSPRÜNGLICHEN FRAGEN

### **1. Sind die Beziehungen dynamisch?**

✅ **MIT PropertyChangedBinder: JA**
- Neue Kinder erscheinen automatisch in `Childs` wenn sie zur DataSource hinzugefügt werden
- Changed-Events werden überwacht und führen zu automatischem Add/Remove

❌ **OHNE PropertyChangedBinder: NEIN**
- Nur bei manuellem `Refresh()` Aufruf

### **2. Erscheinen neue Kinder automatisch?**

✅ **JA** (mit PropertyChangedBinder):
```csharp
globalOrderStore.Add(new Order { CustomerId = 5 });
// → Erscheint sofort in relationship.Childs
```

### **3. Führen Changed-Events zu Add/Remove?**

✅ **JA** (mit PropertyChangedBinder):
```csharp
order.CustomerId = 999;  // → Automatisch aus Childs entfernt
order.CustomerId = 5;    // → Automatisch wieder eingefügt
```

### **4. Was passiert bei direktem Einfügen in Childs?**

⚠️ **NICHT EMPFOHLEN:**
```csharp
relationship.Childs.Add(newOrder);  // ← Nur in Childs, nicht in DataSource!
```

**Empfohlener Weg:**
```csharp
globalOrderStore.Add(newOrder);  // ← Wird automatisch zu Childs hinzugefügt
```

**Begründung:**
- `Childs` ist eine **gefilterte View** der `DataSource`
- Direktes Einfügen in `Childs` würde Inkonsistenzen verursachen
- Die **Single Source of Truth** ist die globale `DataSource`

---

## ✅ ZUSAMMENFASSUNG

### **Empfehlung:**

1. ✅ **PropertyChangedBinder aus DataToolKit übernehmen**
2. ✅ **ReferenceEqualityComparer kopieren**
3. ✅ **In ParentChildRelationship integrieren**
4. ✅ **Nur über DataSource manipulieren, nicht direkt über Childs**

### **Vorteile:**
- ✅ Vollständig dynamisches Verhalten
- ✅ Automatische Echtzeit-Synchronisation
- ✅ Sauberes Ressourcen-Management
- ✅ Verhindert Doppelbindungen
- ✅ Bewährtes Pattern aus DataToolKit

### **Nächste Schritte:**
1. ✅ Dateien in DataStores-Projekt kopieren (`PropertyChangedBinder.cs`, `ReferenceEqualityComparer.cs`)
2. ✅ `ParentChildRelationship.cs` erweitern (siehe Schritt 3)
3. ✅ Tests für Property-Tracking-Szenarien erstellen
4. ✅ Dokumentation aktualisieren

---

## 📁 DATEIEN-CHECKLISTE

### **Neu zu erstellen:**

- [ ] `DataStores/Relations/PropertyChangedBinder.cs`
- [ ] `DataStores/Relations/ReferenceEqualityComparer.cs`

### **Zu aktualisieren:**

- [ ] `DataStores/Relations/ParentChildRelationship.cs`

### **Tests zu erstellen:**

- [ ] `DataStores.Tests/Relations/PropertyChangedBinder_Tests.cs`
- [ ] `DataStores.Tests/Relations/ParentChildRelationship_PropertyTracking_Tests.cs`

---

**Ende der Zusammenfassung**

🚀 **Viel Erfolg bei der Implementierung!**
