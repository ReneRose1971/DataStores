using DataStores.Abstractions;
using System.Runtime.CompilerServices;

namespace DataStores.Runtime;

/// <summary>
/// ID-basierter Equality-Comparer für EntityBase-Typen.
/// Vergleicht Entities ausschließlich nach ihrer Id-Property (nur wenn Id > 0).
/// </summary>
/// <typeparam name="T">Der Entity-Typ. Muss von EntityBase erben oder zur Laufzeit castbar sein.</typeparam>
/// <remarks>
/// <para>
/// <b>Vergleichslogik:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Persistierte Entities (Id > 0):</b> Vergleich nur nach ID - alle anderen Properties werden ignoriert</description></item>
/// <item><description><b>Neue Entities (Id = 0):</b> Referenzvergleich (nur bei ReferenceEquals true)</description></item>
/// <item><description><b>Null-Handling:</b> null == null → true, null == nicht-null → false</description></item>
/// <item><description><b>Non-EntityBase:</b> Immer false (außer bei Referenzgleichheit)</description></item>
/// </list>
/// <para>
/// <b>Hash-Code-Logik:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>Persistierte Entities (Id > 0):</b> ID-basierter Hash (Id.GetHashCode())</description></item>
/// <item><description><b>Neue Entities (Id = 0):</b> Referenz-basierter Hash (RuntimeHelpers.GetHashCode)</description></item>
/// <item><description><b>Non-EntityBase:</b> Referenz-basierter Hash</description></item>
/// </list>
/// <para>
/// <b>Wichtig:</b> Dieser Comparer ist speziell für DataStores optimiert und garantiert,
/// dass zwei Entities mit gleicher ID (> 0) als gleich betrachtet werden, unabhängig von
/// anderen Property-Werten.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Beispiel 1: Explizite Verwendung in InMemoryDataStore
/// var comparer = new EntityIdComparer&lt;Product&gt;();
/// var store = new InMemoryDataStore&lt;Product&gt;(comparer);
/// 
/// var product1 = new Product { Id = 1, Name = "Original" };
/// var product2 = new Product { Id = 1, Name = "Modified" };
/// 
/// store.Add(product1);
/// Assert.True(store.Contains(product2)); // true - gleiche ID
/// 
/// // Beispiel 2: Neue Entities (Id = 0)
/// var newProduct1 = new Product { Id = 0, Name = "New" };
/// var newProduct2 = new Product { Id = 0, Name = "New" };
/// 
/// Assert.False(comparer.Equals(newProduct1, newProduct2)); // false - verschiedene Referenzen
/// Assert.True(comparer.Equals(newProduct1, newProduct1)); // true - gleiche Referenz
/// </code>
/// </example>
public sealed class EntityIdComparer<T> : IEqualityComparer<T> where T : class
{
    /// <summary>
    /// Bestimmt, ob die angegebenen Objekte gleich sind.
    /// </summary>
    /// <param name="x">Das erste zu vergleichende Objekt.</param>
    /// <param name="y">Das zweite zu vergleichende Objekt.</param>
    /// <returns>
    /// <c>true</c> wenn die Objekte gleich sind; andernfalls <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Gleichheitskriterien:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>Referenzgleichheit → true</description></item>
    /// <item><description>Ein oder beide null → false (außer beide null)</description></item>
    /// <item><description>Kein EntityBase → false</description></item>
    /// <item><description>Id = 0 (neue Entities) → false (nur Referenzgleichheit)</description></item>
    /// <item><description>Id > 0 (persistierte Entities) → Vergleich nach ID</description></item>
    /// </list>
    /// </remarks>
    public bool Equals(T? x, T? y)
    {
        // Referenzgleichheit (optimiert für null und same instance)
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x == null || y == null)
        {
            return false;
        }

        // Nur EntityBase-Typen vergleichen
        if (x is not EntityBase entityX || y is not EntityBase entityY)
        {
            return false;
        }

        // Neue Entities (Id = 0): Nur Referenzgleichheit
        // (bereits durch ReferenceEquals oben abgefangen, aber explizit für Klarheit)
        if (entityX.Id == 0 || entityY.Id == 0)
        {
            return false;
        }

        // Persistierte Entities: ID-Vergleich
        return entityX.Id == entityY.Id;
    }

    /// <summary>
    /// Gibt einen Hash-Code für das angegebene Objekt zurück.
    /// </summary>
    /// <param name="obj">Das Objekt, für das ein Hash-Code zurückgegeben werden soll.</param>
    /// <returns>Ein Hash-Code für das Objekt.</returns>
    /// <remarks>
    /// <para>
    /// <b>Hash-Code-Berechnung:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>EntityBase mit Id > 0:</b> Hash basiert auf ID (stabil über Lifetime)</description></item>
    /// <item><description><b>EntityBase mit Id = 0:</b> Referenz-Hash (RuntimeHelpers.GetHashCode)</description></item>
    /// <item><description><b>Nicht-EntityBase:</b> Referenz-Hash (sollte nicht vorkommen bei korrekter Verwendung)</description></item>
    /// </list>
    /// <para>
    /// <b>Wichtig:</b> Konsistent mit Equals() - zwei Entities mit gleicher ID (> 0)
    /// haben denselben Hash-Code.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Wird nicht ausgelöst - null wird behandelt.</exception>
    public int GetHashCode(T obj)
    {
        if (obj is EntityBase entity)
        {
            // Persistierte Entities: ID-Hash
            if (entity.Id > 0)
            {
                return entity.Id.GetHashCode();
            }
            
            // Neue Entities: Referenz-Hash
            return RuntimeHelpers.GetHashCode(obj);
        }

        // Fallback für Non-EntityBase (sollte nicht vorkommen bei korrekter Verwendung)
        return RuntimeHelpers.GetHashCode(obj);
    }
}
