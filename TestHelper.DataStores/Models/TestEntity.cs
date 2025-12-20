using DataStores.Abstractions;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
/// </remarks>
public sealed class TestEntity : EntityBase, INotifyPropertyChanged
{
    private string _name = "";
    private int _version;
    private DateTime _updatedUtc;
    private bool _isDeleted;
    private double _ratio;
    private string? _tag;
    private int _age;

    /// <summary>
    /// Name der Test-Entity.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>
    /// Versionsnummer.
    /// </summary>
    public int Version
    {
        get => _version;
        set => SetField(ref _version, value);
    }

    /// <summary>
    /// Zeitpunkt der letzten Aktualisierung (UTC).
    /// </summary>
    public DateTime UpdatedUtc
    {
        get => _updatedUtc;
        set => SetField(ref _updatedUtc, value);
    }

    /// <summary>
    /// Lösch-Flag.
    /// </summary>
    public bool IsDeleted
    {
        get => _isDeleted;
        set => SetField(ref _isDeleted, value);
    }

    /// <summary>
    /// Verhältnis-Wert.
    /// </summary>
    public double Ratio
    {
        get => _ratio;
        set => SetField(ref _ratio, value);
    }

    /// <summary>
    /// Optional Tag.
    /// </summary>
    public string? Tag
    {
        get => _tag;
        set => SetField(ref _tag, value);
    }

    /// <summary>
    /// Alter (für Person-Szenarien).
    /// </summary>
    public int Age
    {
        get => _age;
        set => SetField(ref _age, value);
    }

    /// <summary>
    /// Parameterloser Konstruktor (für Serialisierung und LiteDB).
    /// </summary>
    public TestEntity()
    {
        Id = 0; // Neue Entity
        _updatedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Komfort-Konstruktor mit Pflichtfeldern.
    /// </summary>
    public TestEntity(string name, int version = 1, double ratio = 1.0)
        : this()
    {
        _name = name;
        _version = version;
        _ratio = ratio;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    public override string ToString() =>
        $"TestEntity[{Id}]: {Name}, Version={Version}, Age={Age}, Ratio={Ratio:F2}, Deleted={IsDeleted}";

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
