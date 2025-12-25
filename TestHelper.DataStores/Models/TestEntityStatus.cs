namespace TestHelper.DataStores.Models;

/// <summary>
/// Status-Enum für TestEntity (Order/Invoice-Szenarien).
/// </summary>
/// <remarks>
/// Verwenden Sie dieses Enum für Testszenarien, die Status-Felder benötigen,
/// z.B. Order-Status, Payment-Status, etc.
/// </remarks>
public enum TestEntityStatus
{
    /// <summary>
    /// Ausstehend / Wartend.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// In Bearbeitung.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Versendet.
    /// </summary>
    Shipped = 2,

    /// <summary>
    /// Abgeschlossen.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Storniert.
    /// </summary>
    Cancelled = 4
}
