namespace TestHelper.DataStores.TestData;

/// <summary>
/// Abstraktion für die Erzeugung von Testdaten.
/// </summary>
/// <typeparam name="T">Der Entitätstyp, für den Testdaten erzeugt werden sollen.</typeparam>
/// <remarks>
/// <para>
/// Diese Schnittstelle definiert einen Vertrag für Testdaten-Factories, die deterministisch
/// Entities für Unit- und Integrationstests generieren.
/// </para>
/// <para>
/// <b>Design-Prinzipien:</b>
/// <list type="bullet">
/// <item><description>Kennt keine DataStores-Implementierungen</description></item>
/// <item><description>Kennt keine Persistenz-Logik</description></item>
/// <item><description>Erzeugt reine POCOs ohne Seiteneffekte</description></item>
/// <item><description>Deterministisch (gleiche Konfiguration = gleiche Daten)</description></item>
/// <item><description>Thread-safe (Instanz-basiert, kein statischer Zustand)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Verwendung:</b>
/// Primär in der Arrange-Phase von Unit- und Integrationstests zur Erzeugung
/// von Testdaten für DataStores, LINQ-Queries, Performance-Tests, etc.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Beispiel: Custom Factory
/// public class PersonFactory : ITestDataFactory&lt;Person&gt;
/// {
///     private int _idCounter = 1;
///     
///     public Person CreateSingle()
///     {
///         return new Person
///         {
///             Id = _idCounter++,
///             Name = $"Person_{_idCounter}",
///             Age = 20 + (_idCounter % 50)
///         };
///     }
///     
///     public IEnumerable&lt;Person&gt; CreateMany(int count)
///     {
///         return Enumerable.Range(0, count).Select(_ => CreateSingle());
///     }
/// }
/// 
/// // Verwendung im Test
/// var factory = new PersonFactory();
/// var person = factory.CreateSingle();
/// var persons = factory.CreateMany(100).ToList();
/// </code>
/// </example>
public interface ITestDataFactory<T> where T : class
{
    /// <summary>
    /// Erzeugt eine einzelne Test-Entity.
    /// </summary>
    /// <returns>Eine neue Test-Entity vom Typ <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// <para>
    /// Jeder Aufruf erzeugt eine neue Instanz. Bei Seed-basierten Factories
    /// hängt der Wert vom internen Zustand ab (deterministisch).
    /// </para>
    /// <para>
    /// Die erzeugte Entity ist ein reines POCO ohne Persistenz-Kontext.
    /// Properties können Default-Werte oder generierte Werte haben.
    /// </para>
    /// </remarks>
    T CreateSingle();

    /// <summary>
    /// Erzeugt mehrere Test-Entities.
    /// </summary>
    /// <param name="count">Anzahl der zu erzeugenden Entities. Muss &gt;= 0 sein.</param>
    /// <returns>Eine Sequenz von <paramref name="count"/> neuen Test-Entities.</returns>
    /// <remarks>
    /// <para>
    /// Diese Methode ist äquivalent zu:
    /// <code>
    /// Enumerable.Range(0, count).Select(_ => CreateSingle())
    /// </code>
    /// </para>
    /// <para>
    /// Implementierungen sollten lazy evaluation unterstützen, um bei großen
    /// Mengen Speicher zu sparen.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Wird ausgelöst, wenn <paramref name="count"/> negativ ist.
    /// </exception>
    IEnumerable<T> CreateMany(int count);
}
