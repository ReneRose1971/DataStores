using System.Collections.Specialized;

namespace DataStores.Relations;

/// <summary>
/// Provides a 1:1 view over a 1:n parent-child relationship.
/// Automatically updates when the underlying children collection changes.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TChild">The child entity type.</typeparam>
public class OneToOneRelationView<TParent, TChild>
    where TParent : class
    where TChild : class
{
    private readonly ParentChildRelationshipView<TParent, TChild> _relationView;
    private readonly MultipleChildrenPolicy _policy;

    /// <summary>
    /// Gets the parent entity.
    /// </summary>
    public TParent Parent => _relationView.Parent;

    /// <summary>
    /// Gets a value indicating whether a child exists for this parent.
    /// </summary>
    public bool HasChild => ChildOrNull != null;

    /// <summary>
    /// Gets the child entity, or null if no child exists.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple children exist and policy is ThrowIfMultiple.
    /// </exception>
    public TChild? ChildOrNull
    {
        get
        {
            var children = _relationView.Childs;
            
            if (children.Count == 0)
                return null;
            
            if (children.Count == 1)
                return children[0];

            return _policy switch
            {
                MultipleChildrenPolicy.ThrowIfMultiple => throw new InvalidOperationException(
                    $"Expected at most one child for parent, but found {children.Count}."),
                MultipleChildrenPolicy.TakeFirst => children[0],
                _ => throw new InvalidOperationException($"Unknown policy: {_policy}")
            };
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneToOneRelationView{TParent, TChild}"/> class.
    /// </summary>
    /// <param name="relationView">The underlying 1:n relationship view.</param>
    /// <param name="policy">The policy for handling multiple children.</param>
    /// <exception cref="ArgumentNullException">Thrown when relationView is null.</exception>
    public OneToOneRelationView(
        ParentChildRelationshipView<TParent, TChild> relationView,
        MultipleChildrenPolicy policy = MultipleChildrenPolicy.ThrowIfMultiple)
    {
        _relationView = relationView ?? throw new ArgumentNullException(nameof(relationView));
        _policy = policy;
    }

    /// <summary>
    /// Tries to get the child entity.
    /// </summary>
    /// <param name="child">The child entity if found; otherwise null.</param>
    /// <returns>True if a child was found; otherwise false.</returns>
    public bool TryGetChild(out TChild? child)
    {
        try
        {
            child = ChildOrNull;
            return child != null;
        }
        catch (InvalidOperationException)
        {
            child = null;
            return false;
        }
    }
}

/// <summary>
/// Defines policies for handling multiple children in a 1:1 relationship.
/// </summary>
public enum MultipleChildrenPolicy
{
    /// <summary>
    /// Throws an exception if more than one child exists.
    /// </summary>
    ThrowIfMultiple,

    /// <summary>
    /// Takes the first child if multiple exist.
    /// </summary>
    TakeFirst
}
