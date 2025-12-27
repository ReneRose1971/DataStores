namespace DataStores.Abstractions;

/// <summary>
/// Service zur zentralen Bereitstellung von IEqualityComparer-Instanzen mit automatischem Fallback.
/// </summary>
/// <remarks>
/// <para>
/// <b>Auflösungsstrategie:</b>
/// </para>
/// <list type="number">
/// <item><description>Falls T : EntityBase → EntityIdComparer (ID-basiert, nur Id > 0)</description></item>
/// <item><description>Sonst: Registrierter IEqualityComparer&lt;T&gt; aus DI (via Common.Bootstrap Extension)</description></item>
/// <item><description>Fallback: EqualityComparer&lt;T&gt;.Default</description></item>
/// </list>
/// <para>
/// <b>Verwendung in DataStores:</b>
/// </para>
/// <list type="bullet">
/// <item><description>DataStoreDiffBuilder - Automatische Comparer-Auswahl für Diff-Berechnungen</description></item>
/// <item><description>InMemoryDataStore - Fallback wenn kein expliziter Comparer übergeben wird</description></item>
/// <item><description>Synchronisations-Extensions - Konsistente Item-Vergleiche</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Verwendung via DI
/// public class MyService
/// {
///     private readonly IEqualityComparerService _comparerService;
///     
///     public MyService(IEqualityComparerService comparerService)
///     {
///         _comparerService = comparerService;
///     }
///     
///     public void ProcessItems&lt;T&gt;(IReadOnlyList&lt;T&gt; items) where T : class
///     {
///         var comparer = _comparerService.GetComparer&lt;T&gt;();
///         // Verwende comparer für Vergleiche...
///     }
/// }
/// </code>
/// </example>
public interface IEqualityComparerService
{
    /// <summary>
    /// Ruft einen IEqualityComparer für Typ T ab (mit automatischer Fallback-Strategie).
    /// </summary>
    /// <typeparam name="T">Der Typ für den ein Comparer benötigt wird.</typeparam>
    /// <returns>
    /// Ein IEqualityComparer&lt;T&gt; in folgender Priorität:
    /// <list type="number">
    /// <item><description>EntityIdComparer (falls T : EntityBase)</description></item>
    /// <item><description>Registrierter Custom-Comparer (via DI)</description></item>
    /// <item><description>EqualityComparer&lt;T&gt;.Default</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para>
    /// Diese Methode ist thread-safe und kann von mehreren Threads gleichzeitig aufgerufen werden.
    /// Comparer werden nicht gecacht - bei häufiger Verwendung sollten Sie das Ergebnis selbst cachen.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Beispiel 1: EntityBase-Typ (automatisch ID-basiert)
    /// var productComparer = comparerService.GetComparer&lt;Product&gt;();
    /// // Liefert EntityIdComparer&lt;Product&gt;
    /// 
    /// // Beispiel 2: Registrierter Custom-Comparer
    /// // (Annahme: ProductNameComparer wurde in DI registriert)
    /// var nameComparer = comparerService.GetComparer&lt;Product&gt;();
    /// // Liefert ProductNameComparer (falls registriert)
    /// 
    /// // Beispiel 3: Standard-Typ ohne Registrierung
    /// var stringComparer = comparerService.GetComparer&lt;string&gt;();
    /// // Liefert EqualityComparer&lt;string&gt;.Default
    /// </code>
    /// </example>
    IEqualityComparer<T> GetComparer<T>() where T : class;
}
