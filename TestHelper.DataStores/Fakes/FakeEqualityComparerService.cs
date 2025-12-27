using DataStores.Abstractions;
using DataStores.Runtime;

namespace TestHelper.DataStores.Fakes;

/// <summary>
/// Fake implementation of IEqualityComparerService for testing purposes.
/// Returns EqualityComparer&lt;T&gt;.Default for all types.
/// </summary>
public class FakeEqualityComparerService : IEqualityComparerService
{
    /// <summary>
    /// Gets the default comparer for testing.
    /// </summary>
    public IEqualityComparer<T> GetComparer<T>() where T : class
    {
        // For testing: Always return default comparer
        // Real implementation would use EntityIdComparer for EntityBase, etc.
        return EqualityComparer<T>.Default;
    }
}
