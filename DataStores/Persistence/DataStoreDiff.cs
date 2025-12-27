namespace DataStores.Persistence;

/// <summary>
/// Immutable Diff zwischen zwei Datensammlungen (z.B. DataStore und Datenbank-Zustand).
/// Enthält Items zum Einfügen und Löschen.
/// </summary>
/// <typeparam name="T">Der Item-Typ (class constraint).</typeparam>
/// <param name="ToInsert">Items die eingefügt werden müssen (in Quelle vorhanden, in Ziel fehlend).</param>
/// <param name="ToDelete">Items die gelöscht werden müssen (in Ziel vorhanden, in Quelle fehlend).</param>
/// <remarks>
/// <para>
/// Diese Klasse repräsentiert die Differenz zwischen zwei Datensammlungen.
/// </para>
/// <para>
/// <b>Hinweis für EntityBase:</b> Updates sind NICHT im Diff enthalten, da diese durch den
/// PropertyChangedBinder getrackt und direkt via UpdateSingleAsync() behandelt werden.
/// </para>
/// <para>
/// <b>Verwendung:</b>
/// </para>
/// <list type="bullet">
/// <item><description>DataStore → Database Sync</description></item>
/// <item><description>API → Local Store Sync</description></item>
/// <item><description>Beliebige Collection-Vergleiche</description></item>
/// </list>
/// </remarks>
public sealed record DataStoreDiff<T>(
    IReadOnlyList<T> ToInsert,
    IReadOnlyList<T> ToDelete) 
    where T : class
{
    /// <summary>
    /// Gibt an, ob Änderungen vorhanden sind.
    /// </summary>
    public bool HasChanges => ToInsert.Count > 0 || ToDelete.Count > 0;

    /// <summary>
    /// Gibt eine lesbare Zusammenfassung des Diffs zurück.
    /// </summary>
    public override string ToString() =>
        $"DataStoreDiff<{typeof(T).Name}>: {ToInsert.Count} to insert, {ToDelete.Count} to delete";
}
