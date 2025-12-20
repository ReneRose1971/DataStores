using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestHelper.DataStores.Models;

/// <summary>
/// Standard-Test-DTO für JSON-Persistenz-Tests.
/// Keine Abhängigkeit von EntityBase - verwendet Guid als ID.
/// </summary>
/// <remarks>
/// Verwenden Sie diese Klasse für Tests mit JSON-Persistenz oder anderen
/// nicht-LiteDB-Persistierungsstrategien.
/// </remarks>
public sealed class TestDto : INotifyPropertyChanged
{
    private Guid _id;
    private string _name = "";
    private int _age;
    private DateTime _createdUtc;
    private bool _isActive;
    private decimal _score;
    private string? _notes;

    /// <summary>
    /// Eindeutige ID (Guid-basiert).
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    /// <summary>
    /// Name des Test-Objekts.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>
    /// Alter.
    /// </summary>
    public int Age
    {
        get => _age;
        set => SetField(ref _age, value);
    }

    /// <summary>
    /// Zeitpunkt der Erstellung (UTC).
    /// </summary>
    public DateTime CreatedUtc
    {
        get => _createdUtc;
        set => SetField(ref _createdUtc, value);
    }

    /// <summary>
    /// Aktiv-Status.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    /// <summary>
    /// Score-Wert.
    /// </summary>
    public decimal Score
    {
        get => _score;
        set => SetField(ref _score, value);
    }

    /// <summary>
    /// Optionale Notizen.
    /// </summary>
    public string? Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    /// <summary>
    /// Parameterloser Konstruktor (für Serialisierung).
    /// </summary>
    public TestDto()
    {
        _id = Guid.NewGuid();
        _createdUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Komfort-Konstruktor mit allen Pflichtfeldern.
    /// </summary>
    public TestDto(string name, int age, bool isActive = true, decimal score = 0m)
        : this()
    {
        _name = name;
        _age = age;
        _isActive = isActive;
        _score = score;
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
        $"TestDto[{Id}]: {Name}, Age={Age}, Active={IsActive}, Score={Score}";

    public override bool Equals(object? obj) =>
        obj is TestDto other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
