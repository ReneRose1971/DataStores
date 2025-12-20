namespace DataStores.Abstractions;

/// <summary>
/// Definiert eine Entität mit einer eindeutigen Integer-ID.
/// </summary>
/// <remarks>
/// Diese Schnittstelle wird von allen Entitäten implementiert, die in LiteDB persistiert werden.
/// Die ID wird vom LiteDB-Framework automatisch gesetzt und sollte für neue Entitäten 0 sein.
/// </remarks>
public interface IEntity
{
    /// <summary>
    /// Ruft die eindeutige ID der Entität ab oder legt diese fest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Werte:</b>
    /// <list type="bullet">
    /// <item><description>0 = Neue Entität, noch nicht in der Datenbank</description></item>
    /// <item><description>&gt; 0 = Bestehende Entität mit zugewiesener ID</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Die ID wird von LiteDB automatisch beim Einfügen vergeben.
    /// </para>
    /// </remarks>
    int Id { get; set; }
}
