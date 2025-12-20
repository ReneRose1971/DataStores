using System.Collections.ObjectModel;
using DataStores.Abstractions;
using DataStores.Persistence;

namespace DataStores.Relations;

/// <summary>
/// Service zur Verwaltung dynamischer 1:n und 1:1 Beziehungen.
/// Überwacht automatisch Änderungen in Child-Stores und PropertyChanged-Events in Child-Entities.
/// </summary>
/// <typeparam name="TParent">Der Parent-Entity-Typ.</typeparam>
/// <typeparam name="TChild">Der Child-Entity-Typ. Muss INotifyPropertyChanged implementieren für dynamisches Tracking.</typeparam>
/// <typeparam name="TKey">Der Schlüssel-Typ für das Matching.</typeparam>
/// <remarks>
/// <para>
/// Verwendet den <see cref="PropertyChangedBinder{T}"/> für idempotentes PropertyChanged-Tracking.
/// Doppelbindungen werden automatisch verhindert.
/// </para>
/// </remarks>
public class RelationViewService<TParent, TChild, TKey> : IRelationViewService<TParent, TChild, TKey>
    where TParent : class
    where TChild : class
    where TKey : notnull
{
    private readonly IDataStore<TParent> _parentStore;
    private readonly IDataStore<TChild> _childStore;
    private readonly RelationDefinition<TParent, TChild, TKey> _definition;
    private readonly PropertyChangedBinder<TChild> _propertyChangedBinder;
    
    private readonly Dictionary<TKey, ObservableCollection<TChild>> _childrenByParentKey = new();
    private readonly Dictionary<TChild, TKey> _trackedChildKeys = new();
    private readonly Dictionary<TParent, OneToManyRelationView<TParent, TChild>> _viewCache = new();
    
    private bool _disposed;

    /// <summary>
    /// Initialisiert eine neue Instanz der <see cref="RelationViewService{TParent, TChild, TKey}"/> Klasse.
    /// </summary>
    /// <param name="parentStore">Der DataStore mit den Parent-Entities.</param>
    /// <param name="childStore">Der DataStore mit den Child-Entities.</param>
    /// <param name="definition">Die Relation-Definition für die Schlüssel-Extraktion.</param>
    /// <exception cref="ArgumentNullException">Wird ausgelöst, wenn ein Parameter null ist.</exception>
    public RelationViewService(
        IDataStore<TParent> parentStore,
        IDataStore<TChild> childStore,
        RelationDefinition<TParent, TChild, TKey> definition)
    {
        _parentStore = parentStore ?? throw new ArgumentNullException(nameof(parentStore));
        _childStore = childStore ?? throw new ArgumentNullException(nameof(childStore));
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));

        // PropertyChangedBinder für automatisches, idempotentes Tracking
        _propertyChangedBinder = new PropertyChangedBinder<TChild>(
            enabled: true,
            onEntityChanged: OnChildPropertyChanged);

        SubscribeToChildStore();
        InitializeExistingChildren();
    }

    /// <inheritdoc/>
    public OneToManyRelationView<TParent, TChild> GetOneToManyRelation(TParent parent)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        if (_viewCache.TryGetValue(parent, out var existingView))
            return existingView;

        var parentKey = _definition.GetParentKey(parent);
        var childCollection = GetOrCreateChildCollection(parentKey);
        var readOnlyCollection = new ReadOnlyObservableCollection<TChild>(childCollection);
        
        var view = new OneToManyRelationView<TParent, TChild>(parent, readOnlyCollection);
        _viewCache[parent] = view;
        
        return view;
    }

    /// <inheritdoc/>
    public OneToOneRelationView<TParent, TChild> GetOneToOneRelation(
        TParent parent,
        MultipleChildrenPolicy policy = MultipleChildrenPolicy.ThrowIfMultiple)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        // Holt die zugrundeliegende 1:n View (gecacht)
        var oneToManyView = GetOneToManyRelation(parent);
        
        // Erstellt eine neue 1:1 View (NICHT gecacht, da unterschiedliche Policies möglich)
        return new OneToOneRelationView<TParent, TChild>(oneToManyView, policy);
    }

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<TChild> GetChildren(TParent parent)
    {
        return GetOneToManyRelation(parent).Children;
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
        
        // PropertyChangedBinder: Idempotentes Attach (verhindert Doppelbindungen)
        _propertyChangedBinder.Attach(child);
    }

    private void RemoveChildFromIndex(TChild child)
    {
        // PropertyChangedBinder: Explizites Detach
        _propertyChangedBinder.Detach(child);

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
        // PropertyChangedBinder: Alle Bindings auf einmal entfernen
        _propertyChangedBinder.DetachAll();

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

    private void OnChildPropertyChanged(TChild child)
    {
        if (!_trackedChildKeys.TryGetValue(child, out var oldKey))
            return;

        var newKey = _definition.GetChildKey(child);

        if (EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
            return;

        // Key hat sich geändert - Child zwischen Collections verschieben
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
    /// Gibt den Service frei und meldet alle Event-Subscriptions ab.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _childStore.Changed -= OnChildStoreChanged;

        // PropertyChangedBinder übernimmt das komplette Cleanup
        _propertyChangedBinder.Dispose();

        _trackedChildKeys.Clear();
        _childrenByParentKey.Clear();
        _viewCache.Clear();

        _disposed = true;
    }
}
