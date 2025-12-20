using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using DataStores.Abstractions;

namespace DataStores.Relations;

/// <summary>
/// Service that manages dynamic parent-child relationships.
/// Automatically tracks changes in child stores and property changes in child entities.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TChild">The child entity type. Must implement INotifyPropertyChanged for dynamic tracking.</typeparam>
/// <typeparam name="TKey">The key type used for matching.</typeparam>
public class ParentChildRelationService<TParent, TChild, TKey> : IDisposable
    where TParent : class
    where TChild : class
    where TKey : notnull
{
    private readonly IDataStore<TParent> _parentStore;
    private readonly IDataStore<TChild> _childStore;
    private readonly RelationDefinition<TParent, TChild, TKey> _definition;
    
    private readonly Dictionary<TKey, ObservableCollection<TChild>> _childrenByParentKey = new();
    private readonly Dictionary<TChild, TKey> _trackedChildKeys = new();
    private readonly Dictionary<TParent, ParentChildRelationshipView<TParent, TChild>> _viewCache = new();
    
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParentChildRelationService{TParent, TChild, TKey}"/> class.
    /// </summary>
    /// <param name="parentStore">The data store containing parent entities.</param>
    /// <param name="childStore">The data store containing child entities.</param>
    /// <param name="definition">The relation definition specifying key extraction.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ParentChildRelationService(
        IDataStore<TParent> parentStore,
        IDataStore<TChild> childStore,
        RelationDefinition<TParent, TChild, TKey> definition)
    {
        _parentStore = parentStore ?? throw new ArgumentNullException(nameof(parentStore));
        _childStore = childStore ?? throw new ArgumentNullException(nameof(childStore));
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));

        SubscribeToChildStore();
        InitializeExistingChildren();
    }

    /// <summary>
    /// Gets or creates a relationship view for the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>A view of the parent-child relationship.</returns>
    public ParentChildRelationshipView<TParent, TChild> GetRelation(TParent parent)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        if (_viewCache.TryGetValue(parent, out var existingView))
            return existingView;

        var parentKey = _definition.GetParentKey(parent);
        var childCollection = GetOrCreateChildCollection(parentKey);
        var readOnlyCollection = new ReadOnlyObservableCollection<TChild>(childCollection);
        
        var view = new ParentChildRelationshipView<TParent, TChild>(parent, readOnlyCollection);
        _viewCache[parent] = view;
        
        return view;
    }

    /// <summary>
    /// Gets the read-only collection of children for the specified parent.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <returns>A read-only observable collection of children.</returns>
    public ReadOnlyObservableCollection<TChild> GetChildren(TParent parent)
    {
        return GetRelation(parent).Childs;
    }

    private void SubscribeToChildStore()
    {
        _childStore.Changed += OnChildStoreChanged;
    }

    private void InitializeExistingChildren()
    {
        foreach (var child in _childStore.Items)
        {
            AddChildToIndex(child);
        }
    }

    private void OnChildStoreChanged(object? sender, DataStoreChangedEventArgs<TChild> e)
    {
        switch (e.ChangeType)
        {
            case DataStoreChangeType.Add:
                foreach (var child in e.AffectedItems)
                    AddChildToIndex(child);
                break;

            case DataStoreChangeType.Remove:
                foreach (var child in e.AffectedItems)
                    RemoveChildFromIndex(child);
                break;

            case DataStoreChangeType.Clear:
                ClearAllChildren();
                break;

            case DataStoreChangeType.BulkAdd:
                foreach (var child in e.AffectedItems)
                    AddChildToIndex(child);
                break;
        }
    }

    private void AddChildToIndex(TChild child)
    {
        var childKey = _definition.GetChildKey(child);
        var collection = GetOrCreateChildCollection(childKey);
        
        if (!collection.Contains(child))
        {
            if (_definition.ChildComparer != null)
            {
                InsertSorted(collection, child, _definition.ChildComparer);
            }
            else
            {
                collection.Add(child);
            }
        }

        _trackedChildKeys[child] = childKey;
        SubscribeToChildPropertyChanged(child);
    }

    private void RemoveChildFromIndex(TChild child)
    {
        UnsubscribeFromChildPropertyChanged(child);

        if (_trackedChildKeys.TryGetValue(child, out var oldKey))
        {
            if (_childrenByParentKey.TryGetValue(oldKey, out var collection))
            {
                collection.Remove(child);
            }
            _trackedChildKeys.Remove(child);
        }
    }

    private void ClearAllChildren()
    {
        foreach (var child in _trackedChildKeys.Keys.ToList())
        {
            UnsubscribeFromChildPropertyChanged(child);
        }

        _trackedChildKeys.Clear();

        foreach (var collection in _childrenByParentKey.Values)
        {
            collection.Clear();
        }
    }

    private ObservableCollection<TChild> GetOrCreateChildCollection(TKey parentKey)
    {
        if (!_childrenByParentKey.TryGetValue(parentKey, out var collection))
        {
            collection = new ObservableCollection<TChild>();
            _childrenByParentKey[parentKey] = collection;
        }
        return collection;
    }

    private void SubscribeToChildPropertyChanged(TChild child)
    {
        if (child is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += OnChildPropertyChanged;
        }
    }

    private void UnsubscribeFromChildPropertyChanged(TChild child)
    {
        if (child is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= OnChildPropertyChanged;
        }
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TChild child)
            return;

        if (!_trackedChildKeys.TryGetValue(child, out var oldKey))
            return;

        var newKey = _definition.GetChildKey(child);

        if (EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
            return;

        if (_childrenByParentKey.TryGetValue(oldKey, out var oldCollection))
        {
            oldCollection.Remove(child);
        }

        var newCollection = GetOrCreateChildCollection(newKey);
        if (!newCollection.Contains(child))
        {
            if (_definition.ChildComparer != null)
            {
                InsertSorted(newCollection, child, _definition.ChildComparer);
            }
            else
            {
                newCollection.Add(child);
            }
        }

        _trackedChildKeys[child] = newKey;
    }

    private static void InsertSorted(ObservableCollection<TChild> collection, TChild item, IComparer<TChild> comparer)
    {
        int index = 0;
        while (index < collection.Count && comparer.Compare(collection[index], item) < 0)
        {
            index++;
        }
        collection.Insert(index, item);
    }

    /// <summary>
    /// Disposes the service and unsubscribes from all events.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _childStore.Changed -= OnChildStoreChanged;

        foreach (var child in _trackedChildKeys.Keys.ToList())
        {
            UnsubscribeFromChildPropertyChanged(child);
        }

        _trackedChildKeys.Clear();
        _childrenByParentKey.Clear();
        _viewCache.Clear();

        _disposed = true;
    }
}
