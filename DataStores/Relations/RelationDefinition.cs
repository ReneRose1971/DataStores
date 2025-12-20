namespace DataStores.Relations;

/// <summary>
/// Immutable definition of a parent-child relationship.
/// Specifies how to extract keys from parent and child entities.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TChild">The child entity type.</typeparam>
/// <typeparam name="TKey">The key type used to match parent and child.</typeparam>
public class RelationDefinition<TParent, TChild, TKey>
    where TParent : class
    where TChild : class
    where TKey : notnull
{
    /// <summary>
    /// Gets the function that extracts the key from a parent entity.
    /// </summary>
    public Func<TParent, TKey> GetParentKey { get; }

    /// <summary>
    /// Gets the function that extracts the key from a child entity.
    /// </summary>
    public Func<TChild, TKey> GetChildKey { get; }

    /// <summary>
    /// Gets the optional comparison function for sorting children.
    /// If null, children maintain insertion order.
    /// </summary>
    public IComparer<TChild>? ChildComparer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelationDefinition{TParent, TChild, TKey}"/> class.
    /// </summary>
    /// <param name="getParentKey">Function to extract key from parent.</param>
    /// <param name="getChildKey">Function to extract key from child.</param>
    /// <param name="childComparer">Optional comparer for sorting children.</param>
    /// <exception cref="ArgumentNullException">Thrown when getParentKey or getChildKey is null.</exception>
    public RelationDefinition(
        Func<TParent, TKey> getParentKey,
        Func<TChild, TKey> getChildKey,
        IComparer<TChild>? childComparer = null)
    {
        GetParentKey = getParentKey ?? throw new ArgumentNullException(nameof(getParentKey));
        GetChildKey = getChildKey ?? throw new ArgumentNullException(nameof(getChildKey));
        ChildComparer = childComparer;
    }

    /// <summary>
    /// Determines if a child belongs to a parent based on key matching.
    /// </summary>
    /// <param name="parent">The parent entity.</param>
    /// <param name="child">The child entity.</param>
    /// <returns>True if the child's key matches the parent's key; otherwise false.</returns>
    public bool IsMatch(TParent parent, TChild child)
    {
        var parentKey = GetParentKey(parent);
        var childKey = GetChildKey(child);
        return EqualityComparer<TKey>.Default.Equals(parentKey, childKey);
    }
}
