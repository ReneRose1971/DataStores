using System.Collections.ObjectModel;

namespace DataStores.Relations;

/// <summary>
/// Read-only view of a one-to-many relationship.
/// This is a pure container with no logic, tracking, or subscriptions.
/// All dynamics are managed by <see cref="RelationViewService{TParent, TChild, TKey}"/>.
/// </summary>
/// <typeparam name="TParent">The parent entity type (the "one" side).</typeparam>
/// <typeparam name="TChild">The child entity type (the "many" side).</typeparam>
/// <remarks>
/// <para>
/// Repräsentiert eine 1:n-Beziehung zwischen Parent und Children.
/// Die Collection wird automatisch durch den Service aktualisiert.
/// </para>
/// </remarks>
public class OneToManyRelationView<TParent, TChild>
    where TParent : class
    where TChild : class
{
    /// <summary>
    /// Gets the parent entity for this relationship (the "one" side).
    /// </summary>
    public TParent Parent { get; }

    /// <summary>
    /// Gets the read-only collection of child entities (the "many" side).
    /// This collection is updated automatically by the service.
    /// </summary>
    public ReadOnlyObservableCollection<TChild> Children { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneToManyRelationView{TParent, TChild}"/> class.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="children">The read-only observable collection of children.</param>
    /// <exception cref="ArgumentNullException">Thrown when parent or children is null.</exception>
    internal OneToManyRelationView(TParent parent, ReadOnlyObservableCollection<TChild> children)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Children = children ?? throw new ArgumentNullException(nameof(children));
    }
}
