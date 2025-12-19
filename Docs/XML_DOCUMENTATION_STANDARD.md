# XML-Dokumentations-Standard für DataStores

## Übersicht

Alle öffentlichen Typen, Methoden und Properties MÜSSEN mit deutschen XML-Kommentaren versehen werden.

---

## Klassen-Dokumentation

### Template

```csharp
/// <summary>
/// Kurzbeschreibung der Klasse in einem Satz.
/// </summary>
/// <remarks>
/// Optionale ausführlichere Beschreibung:
/// - Feature 1
/// - Feature 2
/// - Besondere Hinweise
/// </remarks>
/// <typeparam name="T">Beschreibung des generischen Parameters</typeparam>
/// <example>
/// Verwendungsbeispiel:
/// <code>
/// var instance = new MyClass&lt;Customer&gt;();
/// instance.DoSomething();
/// </code>
/// </example>
public class MyClass<T> where T : class
{
}
```

### Beispiel: InMemoryDataStore

```csharp
/// <summary>
/// Stellt einen thread-sicheren In-Memory-Datenspeicher bereit.
/// </summary>
/// <remarks>
/// <para>
/// Der <see cref="InMemoryDataStore{T}"/> verwendet Lock-basierte 
/// Synchronisation für Thread-Safety. Die <see cref="Items"/>-Property 
/// liefert immer einen Snapshot zurück.
/// </para>
/// <para>
/// Unterstützt optionales Event-Marshaling über einen 
/// <see cref="SynchronizationContext"/>, z.B. für UI-Thread-Updates.
/// </para>
/// </remarks>
/// <typeparam name="T">Der Typ der zu speichernden Entitäten. Muss ein Referenztyp sein.</typeparam>
/// <example>
/// Basis-Verwendung:
/// <code>
/// var store = new InMemoryDataStore&lt;Customer&gt;();
/// store.Add(new Customer { Id = 1, Name = "John Doe" });
/// 
/// foreach (var customer in store.Items)
/// {
///     Console.WriteLine(customer.Name);
/// }
/// </code>
/// 
/// Mit SynchronizationContext für WPF:
/// <code>
/// var store = new InMemoryDataStore&lt;Customer&gt;(
///     synchronizationContext: SynchronizationContext.Current);
/// 
/// store.Changed += (s, e) =>
/// {
///     // Läuft automatisch auf UI-Thread
///     UpdateUI();
/// };
/// </code>
/// </example>
public class InMemoryDataStore<T> : IDataStore<T> where T : class
{
}
```

---

## Konstruktor-Dokumentation

### Template

```csharp
/// <summary>
/// Initialisiert eine neue Instanz der <see cref="MyClass"/>-Klasse.
/// </summary>
/// <param name="param1">Beschreibung von Parameter 1</param>
/// <param name="param2">Beschreibung von Parameter 2</param>
/// <exception cref="ArgumentNullException">
/// Wird ausgelöst, wenn <paramref name="param1"/> <c>null</c> ist.
/// </exception>
/// <remarks>
/// Optionale zusätzliche Hinweise zur Initialisierung.
/// </remarks>
public MyClass(string param1, int param2)
{
}
```

### Beispiel

```csharp
/// <summary>
/// Initialisiert eine neue Instanz der <see cref="InMemoryDataStore{T}"/>-Klasse.
/// </summary>
/// <param name="comparer">
/// Optionaler Equality-Comparer für Item-Vergleiche. 
/// Wenn <c>null</c>, wird <see cref="EqualityComparer{T}.Default"/> verwendet.
/// </param>
/// <param name="synchronizationContext">
/// Optionaler <see cref="SynchronizationContext"/> für Event-Marshaling.
/// Wenn <c>null</c>, werden Events synchron im aufrufenden Thread gefeuert.
/// Nützlich für UI-Thread-Marshaling in WPF/WinForms.
/// </param>
/// <remarks>
/// Der Store ist nach Initialisierung leer. Items müssen explizit 
/// über <see cref="Add"/> oder <see cref="AddRange"/> hinzugefügt werden.
/// </remarks>
public InMemoryDataStore(
    IEqualityComparer<T>? comparer = null,
    SynchronizationContext? synchronizationContext = null)
{
}
```

---

## Property-Dokumentation

### Template

```csharp
/// <summary>
/// Ruft [Beschreibung] ab oder legt [Beschreibung] fest.
/// </summary>
/// <value>
/// Ein/Eine <see cref="Type"/> der/die [Beschreibung].
/// </value>
/// <remarks>
/// Zusätzliche Hinweise zum Property.
/// </remarks>
/// <exception cref="ArgumentNullException">
/// Wird beim Setzen ausgelöst, wenn der Wert <c>null</c> ist.
/// </exception>
public string MyProperty { get; set; }
```

### Beispiel

```csharp
/// <summary>
/// Ruft einen thread-sicheren Snapshot aller Items im Store ab.
/// </summary>
/// <value>
/// Eine <see cref="IReadOnlyList{T}"/> mit allen aktuellen Items.
/// Die Liste ist ein Snapshot zum Zeitpunkt des Zugriffs.
/// </value>
/// <remarks>
/// <para>
/// Diese Property erzeugt bei jedem Aufruf eine neue Liste-Instanz.
/// Änderungen am Store nach dem Zugriff werden in bereits 
/// erhaltenen Snapshots NICHT reflektiert.
/// </para>
/// <para>
/// Thread-Safe: Kann sicher während concurrent Add/Remove-Operationen 
/// aufgerufen werden.
/// </para>
/// </remarks>
/// <example>
/// Sichere Iteration während Concurrent-Updates:
/// <code>
/// var snapshot = store.Items;
/// 
/// // Andere Threads können jetzt Add/Remove aufrufen
/// Parallel.For(0, 100, i => store.Add(new Customer { Id = i }));
/// 
/// // Snapshot bleibt unverändert
/// foreach (var item in snapshot)
/// {
///     Console.WriteLine(item.Name);
/// }
/// </code>
/// </example>
public IReadOnlyList<T> Items { get; }
```

---

## Methoden-Dokumentation

### Template

```csharp
/// <summary>
/// [Verb in 3. Person Singular] [Beschreibung was die Methode tut].
/// </summary>
/// <param name="param1">Beschreibung von Parameter 1</param>
/// <param name="param2">Beschreibung von Parameter 2</param>
/// <returns>
/// Beschreibung des Rückgabewerts.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Wird ausgelöst, wenn <paramref name="param1"/> <c>null</c> ist.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Wird ausgelöst, wenn [Bedingung].
/// </exception>
/// <remarks>
/// Zusätzliche Hinweise zur Methode.
/// </remarks>
/// <example>
/// <code>
/// var result = instance.MyMethod("value", 42);
/// </code>
/// </example>
/// <seealso cref="RelatedMethod"/>
public ReturnType MyMethod(string param1, int param2)
{
}
```

### Beispiel: Synchrone Methode

```csharp
/// <summary>
/// Fügt ein Item zum Store hinzu und feuert ein <see cref="Changed"/>-Event.
/// </summary>
/// <param name="item">Das hinzuzufügende Item. Darf nicht <c>null</c> sein.</param>
/// <exception cref="ArgumentNullException">
/// Wird ausgelöst, wenn <paramref name="item"/> <c>null</c> ist.
/// </exception>
/// <remarks>
/// <para>
/// Diese Methode ist thread-safe und kann concurrent aufgerufen werden.
/// Das <see cref="Changed"/>-Event wird mit <see cref="DataStoreChangeType.Add"/> 
/// gefeuert.
/// </para>
/// <para>
/// Wenn ein <see cref="SynchronizationContext"/> konfiguriert ist, 
/// wird das Event auf diesem Context marshalled (z.B. UI-Thread).
/// </para>
/// </remarks>
/// <example>
/// Einfaches Hinzufügen:
/// <code>
/// store.Add(new Customer { Id = 1, Name = "John Doe" });
/// </code>
/// 
/// Mit Event-Handling:
/// <code>
/// store.Changed += (s, e) =>
/// {
///     if (e.ChangeType == DataStoreChangeType.Add)
///     {
///         Console.WriteLine($"Added: {e.AffectedItems[0]}");
///     }
/// };
/// 
/// store.Add(new Customer { Id = 1, Name = "John" });
/// </code>
/// </example>
/// <seealso cref="AddRange"/>
/// <seealso cref="Remove"/>
public void Add(T item)
{
}
```

### Beispiel: Asynchrone Methode

```csharp
/// <summary>
/// Lädt alle Items asynchron aus der konfigurierten <see cref="IPersistenceStrategy{T}"/>.
/// </summary>
/// <param name="cancellationToken">
/// Token zum Abbrechen des asynchronen Ladevorgangs.
/// </param>
/// <returns>
/// Ein <see cref="Task"/>, der den asynchronen Ladevorgang repräsentiert.
/// Bei Erfolg ist der Store mit geladenen Items gefüllt.
/// </returns>
/// <exception cref="InvalidOperationException">
/// Wird ausgelöst, wenn die Methode mehrfach aufgerufen wird.
/// </exception>
/// <exception cref="OperationCanceledException">
/// Wird ausgelöst, wenn <paramref name="cancellationToken"/> das Abbrechen signalisiert.
/// </exception>
/// <remarks>
/// <para>
/// Diese Methode kann nur einmal aufgerufen werden. Wiederholte Aufrufe 
/// werden ignoriert (Idempotenz).
/// </para>
/// <para>
/// Thread-Safe: Concurrent Aufrufe werden serialisiert. Nur der erste 
/// Aufruf lädt tatsächlich Daten.
/// </para>
/// </remarks>
/// <example>
/// Basis-Verwendung:
/// <code>
/// var decorator = new PersistentStoreDecorator&lt;Customer&gt;(
///     innerStore, strategy, autoLoad: true, autoSaveOnChange: true);
/// 
/// await decorator.InitializeAsync();
/// 
/// // Store ist jetzt mit persistierten Daten gefüllt
/// var customers = decorator.Items;
/// </code>
/// 
/// Mit Cancellation:
/// <code>
/// var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
/// 
/// try
/// {
///     await decorator.InitializeAsync(cts.Token);
/// }
/// catch (OperationCanceledException)
/// {
///     Console.WriteLine("Load cancelled");
/// }
/// </code>
/// </example>
/// <seealso cref="IAsyncInitializable"/>
public Task InitializeAsync(CancellationToken cancellationToken = default)
{
}
```

---

## Event-Dokumentation

### Template

```csharp
/// <summary>
/// Tritt ein, wenn [Bedingung eintritt].
/// </summary>
/// <remarks>
/// Zusätzliche Hinweise zum Event.
/// </remarks>
/// <example>
/// <code>
/// instance.MyEvent += (sender, args) =>
/// {
///     Console.WriteLine("Event fired");
/// };
/// </code>
/// </example>
public event EventHandler<MyEventArgs>? MyEvent;
```

### Beispiel

```csharp
/// <summary>
/// Tritt ein, wenn sich der Inhalt des Stores ändert.
/// </summary>
/// <remarks>
/// <para>
/// Das Event wird für folgende Operationen gefeuert:
/// - <see cref="Add"/> ? <see cref="DataStoreChangeType.Add"/>
/// - <see cref="AddRange"/> ? <see cref="DataStoreChangeType.BulkAdd"/>
/// - <see cref="Remove"/> ? <see cref="DataStoreChangeType.Remove"/>
/// - <see cref="Clear"/> ? <see cref="DataStoreChangeType.Clear"/>
/// </para>
/// <para>
/// Wenn ein <see cref="SynchronizationContext"/> konfiguriert ist, 
/// wird das Event auf diesem Context gefeuert. Dies ist nützlich für 
/// UI-Thread-Marshaling in WPF/WinForms-Anwendungen.
/// </para>
/// <para>
/// Thread-Safe: Event-Handler können sicher während concurrent 
/// Operationen hinzugefügt/entfernt werden.
/// </para>
/// </remarks>
/// <example>
/// Basis-Event-Handling:
/// <code>
/// store.Changed += (sender, args) =>
/// {
///     Console.WriteLine($"Change: {args.ChangeType}");
///     Console.WriteLine($"Items affected: {args.AffectedItems.Count}");
/// };
/// 
/// store.Add(new Customer { Id = 1, Name = "John" });
/// // Output: Change: Add
/// //         Items affected: 1
/// </code>
/// 
/// Typ-spezifisches Handling:
/// <code>
/// store.Changed += (sender, args) =>
/// {
///     switch (args.ChangeType)
///     {
///         case DataStoreChangeType.Add:
///             HandleAdd(args.AffectedItems);
///             break;
///         case DataStoreChangeType.BulkAdd:
///             HandleBulkAdd(args.AffectedItems);
///             break;
///         case DataStoreChangeType.Remove:
///             HandleRemove(args.AffectedItems);
///             break;
///         case DataStoreChangeType.Clear:
///             HandleClear();
///             break;
///     }
/// };
/// </code>
/// </example>
/// <seealso cref="DataStoreChangedEventArgs{T}"/>
/// <seealso cref="DataStoreChangeType"/>
public event EventHandler<DataStoreChangedEventArgs<T>>? Changed;
```

---

## Interface-Dokumentation

### Template

```csharp
/// <summary>
/// Definiert [Beschreibung des Interfaces].
/// </summary>
/// <typeparam name="T">Beschreibung des generischen Parameters</typeparam>
/// <remarks>
/// Zusätzliche Hinweise zum Interface.
/// </remarks>
/// <example>
/// Implementierungs-Beispiel:
/// <code>
/// public class MyImplementation : IMyInterface
/// {
///     // Implementation
/// }
/// </code>
/// </example>
public interface IMyInterface<T>
{
}
```

---

## Enum-Dokumentation

### Template

```csharp
/// <summary>
/// Gibt [Beschreibung] an.
/// </summary>
/// <remarks>
/// Zusätzliche Hinweise zum Enum.
/// </remarks>
public enum MyEnum
{
    /// <summary>
    /// Beschreibung von Wert1.
    /// </summary>
    Value1 = 0,
    
    /// <summary>
    /// Beschreibung von Wert2.
    /// </summary>
    Value2 = 1,
    
    /// <summary>
    /// Beschreibung von Wert3.
    /// </summary>
    Value3 = 2
}
```

### Beispiel

```csharp
/// <summary>
/// Gibt den Typ einer Änderung in einem <see cref="IDataStore{T}"/> an.
/// </summary>
/// <remarks>
/// Dieser Enum wird in <see cref="DataStoreChangedEventArgs{T}"/> 
/// verwendet, um die Art der Änderung zu kommunizieren.
/// </remarks>
public enum DataStoreChangeType
{
    /// <summary>
    /// Ein einzelnes Item wurde über <see cref="IDataStore{T}.Add"/> hinzugefügt.
    /// </summary>
    Add = 0,
    
    /// <summary>
    /// Mehrere Items wurden über <see cref="IDataStore{T}.AddRange"/> 
    /// als Bulk-Operation hinzugefügt.
    /// </summary>
    BulkAdd = 1,
    
    /// <summary>
    /// Ein Item wurde über <see cref="IDataStore{T}.Remove"/> entfernt.
    /// </summary>
    Remove = 2,
    
    /// <summary>
    /// Alle Items wurden über <see cref="IDataStore{T}.Clear"/> entfernt.
    /// </summary>
    Clear = 3,
    
    /// <summary>
    /// Der Store wurde komplett zurückgesetzt.
    /// </summary>
    Reset = 4
}
```

---

## Best Practices

### ? DO

1. **Deutsche Sprache** für alle Kommentare
2. **Vollständige Sätze** mit Punkt am Ende
3. **`<see cref="..."/>`** für Typ-Referenzen
4. **`<paramref name="..."/>`** für Parameter-Referenzen
5. **`<code>...</code>`** für Code-Beispiele
6. **`<para>...</para>`** für Absätze in `<remarks>`
7. **Exceptions dokumentieren** mit Bedingungen
8. **Examples bereitstellen** für komplexe APIs

### ? DON'T

1. **Keine Englisch-Deutsch-Mischung**
2. **Keine Wiederholung** von offensichtlichen Infos
3. **Keine TODO-Kommentare** in Production-Code
4. **Keine veralteten** Dokumentationen

---

**Stand:** 2025-12-19  
**Version:** 1.0.0
