namespace DataStores.Abstractions;

/// <summary>
/// Abstrakte Basisklasse für alle Entitäten mit ID-basierter Identität.
/// </summary>
/// <remarks>
/// <para>
/// Diese Klasse stellt sicher, dass alle Entitäten:
/// <list type="bullet">
/// <item><description>Eine eindeutige Integer-ID haben</description></item>
/// <item><description>Sinnvolle String-Repräsentation implementieren</description></item>
/// <item><description>Korrekte Gleichheits-Semantik haben</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Ableitende Klassen MÜSSEN implementieren:</b>
/// <list type="bullet">
/// <item><description><see cref="ToString"/> - Lesbare String-Darstellung</description></item>
/// <item><description><see cref="Equals(object?)"/> - Gleichheits-Logik</description></item>
/// <item><description><see cref="GetHashCode"/> - Hash-Code-Berechnung</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Product : EntityBase
/// {
///     public string Name { get; set; } = "";
///     public decimal Price { get; set; }
/// 
///     public override string ToString() => $"Product #{Id}: {Name} ({Price:C})";
/// 
///     public override bool Equals(object? obj)
///     {
///         if (obj is not Product other) return false;
///         if (Id > 0 &amp;&amp; other.Id > 0) return Id == other.Id;
///         return ReferenceEquals(this, other);
///     }
/// 
///     public override int GetHashCode() => Id > 0 ? Id : HashCode.Combine(Name, Price);
/// }
/// </code>
/// </example>
public abstract class EntityBase : IEntity
{
    /// <inheritdoc/>
    public int Id { get; set; }

    /// <summary>
    /// Gibt eine lesbare String-Darstellung der Entität zurück.
    /// </summary>
    /// <returns>String-Darstellung der Entität.</returns>
    /// <remarks>
    /// Ableitende Klassen MÜSSEN diese Methode überschreiben und eine sinnvolle
    /// Repräsentation zurückgeben, z.B. "Product #42: Laptop (1299.99€)".
    /// </remarks>
    public abstract override string ToString();

    /// <summary>
    /// Bestimmt, ob das angegebene Objekt gleich der aktuellen Entität ist.
    /// </summary>
    /// <param name="obj">Das zu vergleichende Objekt.</param>
    /// <returns><c>true</c> wenn gleich; andernfalls <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// Ableitende Klassen MÜSSEN diese Methode überschreiben.
    /// </para>
    /// <para>
    /// <b>Empfohlene Implementierung:</b>
    /// <list type="bullet">
    /// <item><description>Für persistierte Entitäten (Id &gt; 0): Vergleich nach ID</description></item>
    /// <item><description>Für neue Entitäten (Id = 0): Referenzvergleich oder Wertegleichheit</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract override bool Equals(object? obj);

    /// <summary>
    /// Dient als Hash-Funktion für die Entität.
    /// </summary>
    /// <returns>Hash-Code für die Entität.</returns>
    /// <remarks>
    /// <para>
    /// Ableitende Klassen MÜSSEN diese Methode überschreiben.
    /// </para>
    /// <para>
    /// <b>Empfohlene Implementierung:</b>
    /// <list type="bullet">
    /// <item><description>Für persistierte Entitäten (Id &gt; 0): Verwende Id.GetHashCode()</description></item>
    /// <item><description>Für neue Entitäten (Id = 0): Verwende HashCode.Combine(...) mit Eigenschaftswerten</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Wichtig:</b> Equals() und GetHashCode() müssen konsistent sein!
    /// </para>
    /// <para>
    /// <b>Hinweis:</b> Verwende NICHT base.GetHashCode() für neue Entitäten, sondern HashCode.Combine(...)
    /// oder RuntimeHelpers.GetHashCode(this) für Referenz-basierte Hashes.
    /// </para>
    /// </remarks>
    public abstract override int GetHashCode();
}
