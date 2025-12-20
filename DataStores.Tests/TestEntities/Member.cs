using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataStores.Tests.TestEntities;

/// <summary>
/// Test-Entity für Child in ParentChildRelationship-Tests.
/// Implementiert INotifyPropertyChanged für dynamische Tests.
/// </summary>
public class Member : INotifyPropertyChanged
{
    private Guid _id;
    private Guid _groupId;
    private string _name = string.Empty;

    public Guid Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    public Guid GroupId
    {
        get => _groupId;
        set
        {
            if (_groupId != value)
            {
                _groupId = value;
                OnPropertyChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
