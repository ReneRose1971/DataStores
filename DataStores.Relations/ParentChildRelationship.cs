using DataStores.Abstractions;
using DataStores.Runtime;

namespace DataStores.Relations;

/// <summary>
/// Manages a parent-child relationship between data stores.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TChild">The child entity type.</typeparam>
public class ParentChildRelationship<TParent, TChild>
    where TParent : class
    where TChild : class
{
    private readonly IDataStores _stores;
    private IDataStore<TChild>? _dataSource;

    /// <summary>
    /// Gets or sets the parent entity.
    /// </summary>
    public TParent Parent { get; init; }

    /// <summary>
    /// Gets or sets the data source for child items.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when setting to null.</exception>
    public IDataStore<TChild> DataSource
    {
        get => _dataSource ?? throw new InvalidOperationException("DataSource has not been set. Call UseGlobalDataSource() or UseSnapshotFromGlobal() first.");
        set => _dataSource = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the local collection of child items for this parent.
    /// </summary>
    public InMemoryDataStore<TChild> Childs { get; }

    /// <summary>
    /// Gets or sets the filter function to determine which children belong to this parent.
    /// </summary>
    public Func<TParent, TChild, bool> Filter { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParentChildRelationship{TParent, TChild}"/> class.
    /// </summary>
    /// <param name="stores">The data stores facade.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="filter">The filter function.</param>
    public ParentChildRelationship(
        IDataStores stores,
        TParent parent,
        Func<TParent, TChild, bool> filter)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        Childs = new InMemoryDataStore<TChild>();
    }

    /// <summary>
    /// Sets the data source to the global data store for <typeparamref name="TChild"/>.
    /// </summary>
    public void UseGlobalDataSource()
    {
        DataSource = _stores.GetGlobal<TChild>();
    }

    /// <summary>
    /// Creates a local snapshot from the global data store and sets it as the data source.
    /// </summary>
    /// <param name="predicate">Optional additional filter predicate.</param>
    public void UseSnapshotFromGlobal(Func<TChild, bool>? predicate = null)
    {
        DataSource = _stores.CreateLocalSnapshotFromGlobal(predicate);
    }

    /// <summary>
    /// Refreshes the child collection by applying the filter to the data source.
    /// </summary>
    public void Refresh()
    {
        if (_dataSource == null)
            throw new InvalidOperationException("DataSource has not been set. Call UseGlobalDataSource() or UseSnapshotFromGlobal() first.");

        Childs.Clear();
        var filteredItems = _dataSource.Items.Where(child => Filter(Parent, child));
        Childs.AddRange(filteredItems);
    }
}
