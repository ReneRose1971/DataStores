using System.Collections.ObjectModel;

namespace DataStores.Relations;

/// <summary>
/// Read-only view of a parent-child relationship.
/// This is a pure container with no logic, tracking, or subscriptions.
/// All dynamics are managed by <see cref="ParentChildRelationService{TParent, TChild, TKey}"/>.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TChild">The child entity type.</typeparam>
public class ParentChildRelationshipView<TParent, TChild>
    where TParent : class
    where TChild : class
{
    /// <summary>
    /// Gets the parent entity for this relationship.
    /// </summary>
    public TParent Parent { get; }

    /// <summary>
    /// Gets the read-only collection of child entities.
    /// This collection is updated automatically by the service.
    /// </summary>
    public ReadOnlyObservableCollection<TChild> Childs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParentChildRelationshipView{TParent, TChild}"/> class.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="childs">The read-only observable collection of children.</param>
    /// <exception cref="ArgumentNullException">Thrown when parent or childs is null.</exception>
    internal ParentChildRelationshipView(TParent parent, ReadOnlyObservableCollection<TChild> childs)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Childs = childs ?? throw new ArgumentNullException(nameof(childs));
    }
}
