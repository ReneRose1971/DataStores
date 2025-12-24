namespace DataStores.Persistence;

/// <summary>
/// Immutable Diff zwischen DataStore und Datenbank-Zustand.
/// Enthält NUR Insert und Delete - Updates werden durch PropertyChangedBinder behandelt.
/// </summary>
/// <typeparam name="T">Der Entitätstyp, muss von EntityBase erben.</typeparam>
/// <param name="ToInsert">Neue Entities (Id = 0) die eingefügt werden müssen.</param>
/// <param name="ToDelete">Entities aus DB die im DataStore gelöscht wurden.</param>
/// <remarks>
/// <para>
/// Diese Klasse repräsentiert die Differenz zwischen dem In-Memory DataStore-Zustand
/// und dem persistierten Datenbank-Zustand.
/// </para>
/// <para>
/// <b>Wichtig:</b> Updates sind NICHT im Diff enthalten, da diese durch den
/// PropertyChangedBinder getrackt und direkt via UpdateSingleAsync() behandelt werden.
/// </para>
/// </remarks>
public sealed record DataStoreDiff<T>(
    IReadOnlyList<T> ToInsert,
    IReadOnlyList<T> ToDelete) 
    where T : Abstractions.EntityBase
{
    /// <summary>
    /// Gibt an, ob Änderungen vorhanden sind.
    /// </summary>
    public bool HasChanges => ToInsert.Count > 0 || ToDelete.Count > 0;

    /// <summary>
    /// Gibt eine lesbare Zusammenfassung des Diffs zurück.
    /// </summary>
    public override string ToString() =>
        $"DataStoreDiff: {ToInsert.Count} to insert, {ToDelete.Count} to delete";
}
