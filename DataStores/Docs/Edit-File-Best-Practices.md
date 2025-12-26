# Best Practices für edit_file Tool - Vermeidung von Warnungen

## Problem: Fehlende Klammern-Warnungen

Beim Verwenden des `edit_file` Tools können Warnungen entstehen, wenn der Tool den Kontext nicht korrekt identifizieren kann.

---

## ✅ **Best Practices (DIESE BEFOLGEN)**

### 1. **Vollständige Code-Blöcke bereitstellen**

❌ **FALSCH** (führt zu Warnungen):
```csharp
public class MyClass
{
    // ...existing code...
    public void NewMethod()
    {
        Console.WriteLine("New");
    }
}
```

✅ **RICHTIG** (keine Warnungen):
```csharp
public class MyClass
{
    public void ExistingMethod()
    {
        Console.WriteLine("Existing");
    }
    
    public void NewMethod()
    {
        Console.WriteLine("New");
    }
}
```

---

### 2. **Klare Anker-Punkte verwenden**

❌ **FALSCH**:
```csharp
// ...existing code...
var newVariable = 42;
// ...existing code...
```

✅ **RICHTIG**:
```csharp
public void MyMethod()
{
    var existingVariable = 10;
    var newVariable = 42;
    
    Console.WriteLine(existingVariable + newVariable);
}
```

---

### 3. **Keine Platzhalter in XML-Dokumentation**

❌ **FALSCH**:
```csharp
/// <summary>
/// Does something.
/// </summary>
/// <param name="param">The parameter.</param>
/// <remarks>
/// ...existing remarks...
/// New remark here.
/// </remarks>
```

✅ **RICHTIG**:
```csharp
/// <summary>
/// Does something.
/// </summary>
/// <param name="param">The parameter.</param>
/// <remarks>
/// <para>
/// This method performs an important operation.
/// </para>
/// <para>
/// New remark: Additional behavior added.
/// </para>
/// </remarks>
```

---

### 4. **Methoden-Kontext vollständig angeben**

❌ **FALSCH**:
```csharp
public void ConfigureStores(IDataStorePathProvider pathProvider)
{
    // ...existing code...
    AddStore(new JsonDataStoreBuilder<Customer>(...));
}
```

✅ **RICHTIG**:
```csharp
protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
{
    AddStore(new InMemoryDataStoreBuilder<Product>());
    
    AddStore(new JsonDataStoreBuilder<Customer>(
        filePath: pathProvider.FormatJsonFileName("customers")));
    
    AddStore(new LiteDbDataStoreBuilder<Order>(
        databasePath: pathProvider.FormatLiteDbFileName("myapp")));
}
```

---

### 5. **Beispiele in XML-Dokumentation aktuell halten**

❌ **FALSCH** (veraltete Signatur):
```csharp
/// <example>
/// <code>
/// protected override void ConfigureStores(IDataStorePathProvider pathProvider)
/// {
///     AddStore(new JsonDataStoreBuilder&lt;Customer&gt;(...));
/// }
/// </code>
/// </example>
protected abstract void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider);
```

✅ **RICHTIG** (aktuelle Signatur):
```csharp
/// <example>
/// <code>
/// protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
/// {
///     AddStore(new JsonDataStoreBuilder&lt;Customer&gt;(
///         filePath: pathProvider.FormatJsonFileName("customers")));
/// }
/// </code>
/// </example>
protected abstract void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider);
```

---

## 🔍 **Warum entstehen Warnungen?**

Das `edit_file` Tool versucht, den bereitgestellten Code-Kontext zu finden und zu ersetzen. Wenn:

1. **Platzhalter wie `// ...existing code...`** verwendet werden
2. **Unvollständige Code-Blöcke** bereitgestellt werden
3. **Keine eindeutigen Anker-Punkte** vorhanden sind

...kann das Tool den exakten Einfügepunkt nicht finden und erzeugt Warnungen.

---

## 📋 **Checklist vor edit_file Verwendung**

Vor jedem `edit_file` Aufruf prüfen:

- [ ] Keine `// ...existing code...` Kommentare
- [ ] Vollständige Methoden-Signaturen
- [ ] Klare Anker-Punkte (z.B. vorherige Methode/Property)
- [ ] Korrekte Einrückung
- [ ] Geschlossene Klammern `{}`
- [ ] XML-Dokumentation vollständig

---

## 🛠️ **Alternative: get_file dann edit_file**

Bei komplexen Edits:

1. **get_file** - Datei vollständig lesen
2. **Änderungen mental planen**
3. **edit_file** - Vollständigen neuen Block bereitstellen

Beispiel:

```powershell
# Schritt 1: Datei lesen
get_file("DataStores/Registration/DataStoreRegistrarBase.cs")

# Schritt 2: Verstehen, was geändert werden muss

# Schritt 3: Vollständigen Block bereitstellen
edit_file(
    filePath: "...",
    code: """
    public abstract class DataStoreRegistrarBase : IDataStoreRegistrar
    {
        // KOMPLETTER neuer Inhalt ohne Platzhalter
    }
    """
)
```

---

## ✅ **Zusammenfassung**

| Aspekt | ❌ Vermeiden | ✅ Verwenden |
|--------|-------------|-------------|
| **Kommentare** | `// ...existing code...` | Vollständiger Code |
| **Kontext** | Unvollständige Blöcke | Komplette Methoden |
| **Anker** | Vage Positionen | Klare Methodennamen |
| **XML-Docs** | Platzhalter | Vollständige Beispiele |
| **Strategie** | Raten | get_file → edit_file |

---

**Ergebnis:** Keine Warnungen, saubere Edits, erfolgreiche Builds! ✅

**Version:** 1.0.0  
**Erstellt:** Januar 2025
