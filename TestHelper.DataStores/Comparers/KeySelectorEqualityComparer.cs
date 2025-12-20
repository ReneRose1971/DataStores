namespace TestHelper.DataStores.Comparers;

/// <summary>
/// Generic equality comparer that compares objects based on a key selector function.
/// Consolidates multiple specialized comparers (IdOnlyComparer, etc.) into one reusable implementation.
/// </summary>
/// <typeparam name="T">The type of objects to compare.</typeparam>
/// <typeparam name="TKey">The type of the key used for comparison.</typeparam>
public class KeySelectorEqualityComparer<T, TKey> : IEqualityComparer<T>
{
    private readonly Func<T, TKey> _keySelector;
    private readonly IEqualityComparer<TKey> _keyComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeySelectorEqualityComparer{T,TKey}"/> class.
    /// </summary>
    /// <param name="keySelector">Function to extract the key from an object.</param>
    /// <param name="keyComparer">Optional comparer for keys. If null, uses default comparer.</param>
    /// <exception cref="ArgumentNullException">Thrown when keySelector is null.</exception>
    public KeySelectorEqualityComparer(
        Func<T, TKey> keySelector, 
        IEqualityComparer<TKey>? keyComparer = null)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
    }

    /// <inheritdoc/>
    public bool Equals(T? x, T? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        
        var keyX = _keySelector(x);
        var keyY = _keySelector(y);
        
        return _keyComparer.Equals(keyX, keyY);
    }

    /// <inheritdoc/>
    public int GetHashCode(T obj)
    {
        if (obj == null) return 0;
        
        var key = _keySelector(obj);
        return key != null ? _keyComparer.GetHashCode(key) : 0;
    }
}
