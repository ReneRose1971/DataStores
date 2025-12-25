namespace DataStores.Persistence;

/// <summary>
/// Factory für DataStoreDiff-Berechnung.
/// Vergleicht DataStore-Items mit Datenbank-Items und ermittelt INSERT/DELETE-Operationen.
/// </summary>
/// <remarks>
/// <para>
/// <b>Diff-Logik:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>INSERT:</b> Alle Items aus DataStore mit Id = 0 (neue Items)</description></item>
/// <item><description><b>DELETE:</b> Alle Items aus DB, deren IDs nicht mehr im DataStore sind</description></item>
/// <item><description><b>UPDATE:</b> Wird NICHT im Diff erfasst (PropertyChangedBinder behandelt dies)</description></item>
/// </list>
/// <para>
/// <b>Wichtig:</b> Die DELETE-Erkennung basiert auf ID-Vergleich, nicht auf Referenzgleichheit.
/// Ein Item wird als gelöscht erkannt, wenn seine ID in der DB existiert,
/// aber kein Item mit dieser ID im DataStore vorhanden ist.
/// </para>
/// </remarks>
public static class DataStoreDiffBuilder
{
    /// <summary>
    /// Berechnet Diff zwischen DataStore-Items und DB-Items.
    /// </summary>
    /// <typeparam name="T">Der Entitätstyp, muss von EntityBase erben.</typeparam>
    /// <param name="dataStoreItems">Aktuelle Items im DataStore (In-Memory).</param>
    /// <param name="databaseItems">Aktuelle Items in der Datenbank (alle haben Id > 0).</param>
    /// <returns>Diff mit ToInsert und ToDelete.</returns>
    /// <exception cref="ArgumentNullException">
    /// Wird ausgelöst, wenn <paramref name="dataStoreItems"/> oder <paramref name="databaseItems"/> null ist.
    /// </exception>
    /// <example>
    /// <code>
    /// var dataStoreItems = dataStore.Items;
    /// var databaseItems = await strategy.LoadAllAsync();
    /// var diff = DataStoreDiffBuilder.ComputeDiff(dataStoreItems, databaseItems);
    /// 
    /// // Verarbeite Diff
    /// if (diff.HasChanges)
    /// {
    ///     await strategy.SaveDeltaAsync(diff);
    /// }
    /// </code>
    /// </example>
    public static DataStoreDiff<T> ComputeDiff<T>(
        IReadOnlyList<T> dataStoreItems,
        IReadOnlyList<T> databaseItems) 
        where T : Abstractions.EntityBase
    {
        if (dataStoreItems == null)
            throw new ArgumentNullException(nameof(dataStoreItems));
        if (databaseItems == null)
            throw new ArgumentNullException(nameof(databaseItems));

        // Index database items by ID for fast lookup
        var dbById = databaseItems
            .Where(e => e.Id > 0)
            .ToDictionary(e => e.Id);

        // INSERT: Nur neue Items (Id = 0)
        var toInsert = dataStoreItems
            .Where(item => item.Id == 0)
            .ToList();

        // DELETE: Items aus DB, die nicht mehr im DataStore sind
        // Erstelle Set aller IDs im DataStore (nur Id > 0)
        var dataStoreIds = dataStoreItems
            .Where(item => item.Id > 0)
            .Select(item => item.Id)
            .ToHashSet();

        // Finde DB-Items, deren IDs nicht mehr im DataStore sind
        var toDelete = databaseItems
            .Where(dbItem => !dataStoreIds.Contains(dbItem.Id))
            .ToList();

        return new DataStoreDiff<T>(
            toInsert.AsReadOnly(),
            toDelete.AsReadOnly());
    }
}
