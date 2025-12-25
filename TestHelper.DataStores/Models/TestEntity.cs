using DataStores.Abstractions;

namespace TestHelper.DataStores.Models;

/// <summary>
/// Standard-Test-Entity für LiteDB-Persistenz-Tests.
/// Erbt von EntityBase - verwendet int Id.
/// </summary>
/// <remarks>
/// <para>
/// Verwenden Sie diese Klasse für Tests mit LiteDB-Persistierung.
/// Die Id wird automatisch von LiteDB gesetzt und sollte für neue
/// Entities immer 0 sein.
/// </para>
/// <para>
/// <b>PropertyChanged:</b>
/// INotifyPropertyChanged wird automatisch durch Fody.PropertyChanged
/// von EntityBase vererbt. Keine manuelle Implementierung erforderlich.
/// </para>
/// </remarks>
public sealed class TestEntity : EntityBase
{
    /// <summary>
    /// Name der Test-Entity.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Versionsnummer.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Zeitpunkt der letzten Aktualisierung (UTC).
    /// </summary>
    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Lösch-Flag.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Verhältnis-Wert.
    /// </summary>
    public double Ratio { get; set; }

    /// <summary>
    /// Optional Tag.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Alter (für Person-Szenarien).
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Status (für Order/Invoice-Szenarien).
    /// </summary>
    public TestEntityStatus Status { get; set; }

    /// <summary>
    /// Kunden-ID (für Order/Invoice-Szenarien).
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Betrag/Amount (für Order/Invoice-Szenarien).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Bezahlt-Flag (für Invoice-Szenarien).
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Bestellnummer/Rechnungsnummer (für Order/Invoice-Szenarien).
    /// </summary>
    public string OrderNumber { get; set; } = "";

    /// <summary>
    /// Bestelldatum (für Order-Szenarien).
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Verweis auf Parent-Order (für Invoice-Szenarien).
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Items-Liste (für Order-Szenarien).
    /// </summary>
    public List<string> Items { get; set; } = new();

    /// <summary>
    /// Kundenname (für Order-Szenarien).
    /// </summary>
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Parameterloser Konstruktor (für Serialisierung und LiteDB).
    /// </summary>
    public TestEntity()
    {
        Id = 0; // Neue Entity
        UpdatedUtc = DateTime.UtcNow;
        OrderDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Komfort-Konstruktor mit Pflichtfeldern.
    /// </summary>
    public TestEntity(string name, int version = 1, double ratio = 1.0)
        : this()
    {
        Name = name;
        Version = version;
        Ratio = ratio;
    }

    public override string ToString() =>
        $"TestEntity[{Id}]: {Name}, Version={Version}, Age={Age}, Ratio={Ratio:F2}, Status={Status}, Deleted={IsDeleted}";

    public override bool Equals(object? obj)
    {
        if (obj is not TestEntity other)
            return false;

        // Für persistierte Entities: Vergleich nach ID
        if (Id > 0 && other.Id > 0)
            return Id == other.Id;

        // Für neue Entities: Referenzvergleich
        return ReferenceEquals(this, other);
    }

    public override int GetHashCode()
    {
        // Für persistierte Entities: Hash der ID
        if (Id > 0)
            return Id.GetHashCode();

        // Für neue Entities: Hash aus Properties
        return HashCode.Combine(Name, Version, Ratio, Age);
    }
}
