using DataStores.Abstractions;
using DataStores.Persistence;

namespace DataStores.Runtime;

/// <summary>
/// Service implementation for computing differences between two data collections.
/// Provides automatic equality comparer resolution via <see cref="IEqualityComparerService"/>.
/// </summary>
/// <remarks>
/// <para>
/// This service is registered as Singleton in the DI container via <see cref="Bootstrap.DataStoresServiceModule"/>.
/// Application code receives instances via dependency injection through <see cref="IDataStoreDiffService"/>.
/// </para>
/// <para>
/// <b>Thread-Safety:</b> This class is thread-safe. ComputeDiff() can be called concurrently.
/// </para>
/// </remarks>
public sealed class DataStoreDiffService : IDataStoreDiffService
{
    private readonly IEqualityComparerService _comparerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreDiffService"/> class.
    /// </summary>
    /// <param name="comparerService">The comparer service for automatic comparer resolution.</param>
    /// <exception cref="ArgumentNullException">Thrown when comparerService is null.</exception>
    public DataStoreDiffService(IEqualityComparerService comparerService)
    {
        _comparerService = comparerService ?? throw new ArgumentNullException(nameof(comparerService));
    }

    /// <inheritdoc/>
    public DataStoreDiff<T> ComputeDiff<T>(
        IReadOnlyList<T> sourceItems,
        IReadOnlyList<T> targetItems,
        IEqualityComparer<T>? customComparer = null) where T : class
    {
        if (sourceItems == null)
        {
            throw new ArgumentNullException(nameof(sourceItems));
        }

        if (targetItems == null)
        {
            throw new ArgumentNullException(nameof(targetItems));
        }

        // Resolve comparer: custom > service > default
        var comparer = customComparer ?? _comparerService.GetComparer<T>();

        // Use HashSet for O(1) lookups
        var targetSet = new HashSet<T>(targetItems, comparer);
        var sourceSet = new HashSet<T>(sourceItems, comparer);

        // Items in source but not in target → INSERT
        var toInsert = sourceItems
            .Where(item => !targetSet.Contains(item))
            .ToList();

        // Items in target but not in source → DELETE
        var toDelete = targetItems
            .Where(item => !sourceSet.Contains(item))
            .ToList();

        return new DataStoreDiff<T>(
            toInsert.AsReadOnly(),
            toDelete.AsReadOnly());
    }
}
