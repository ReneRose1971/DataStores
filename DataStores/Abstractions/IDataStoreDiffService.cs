using DataStores.Persistence;

namespace DataStores.Abstractions;

/// <summary>
/// Service for computing differences between two data collections.
/// Automatically resolves appropriate equality comparers for type-safe diff operations.
/// </summary>
/// <remarks>
/// <para>
/// This service provides type-safe diff computation with automatic comparer resolution.
/// It is the recommended approach for comparing data stores, database results, or any collections.
/// </para>
/// <para>
/// <b>Comparer Resolution Strategy:</b>
/// </para>
/// <list type="number">
/// <item><description>If custom comparer provided → Use it</description></item>
/// <item><description>Else if T : EntityBase → EntityIdComparer (ID-based)</description></item>
/// <item><description>Else if registered in DI → Custom comparer from container</description></item>
/// <item><description>Else → EqualityComparer&lt;T&gt;.Default</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class SyncService
/// {
///     private readonly IDataStoreDiffService _diffService;
///     
///     public SyncService(IDataStoreDiffService diffService)
///     {
///         _diffService = diffService;
///     }
///     
///     public async Task SyncToDatabase()
///     {
///         var storeItems = _productStore.Items;
///         var dbItems = await _database.LoadAllAsync();
///         
///         // Automatic comparer resolution (EntityIdComparer for Product : EntityBase)
///         var diff = _diffService.ComputeDiff(storeItems, dbItems);
///         
///         if (diff.HasChanges)
///         {
///             await ApplyChanges(diff);
///         }
///     }
/// }
/// </code>
/// </example>
public interface IDataStoreDiffService
{
    /// <summary>
    /// Computes the difference between source and target collections.
    /// </summary>
    /// <typeparam name="T">The type of items in the collections.</typeparam>
    /// <param name="sourceItems">The source collection (e.g., current data store items).</param>
    /// <param name="targetItems">The target collection (e.g., database items).</param>
    /// <param name="customComparer">
    /// Optional custom comparer. If null, comparer is resolved automatically via <see cref="IEqualityComparerService"/>.
    /// </param>
    /// <returns>
    /// A <see cref="DataStoreDiff{T}"/> containing items to insert and delete.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when sourceItems or targetItems is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>Diff Logic:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>ToInsert:</b> Items in source but not in target</description></item>
    /// <item><description><b>ToDelete:</b> Items in target but not in source</description></item>
    /// <item><description><b>Update:</b> Not detected (use property tracking or timestamp comparison)</description></item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> O(n + m) where n = source count, m = target count.
    /// Uses HashSet for efficient lookups.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example 1: EntityBase type (automatic ID-based comparison)
    /// var diff = diffService.ComputeDiff(storeOrders, dbOrders);
    /// 
    /// // Example 2: With custom comparer
    /// var diff = diffService.ComputeDiff(
    ///     storeProducts, 
    ///     dbProducts,
    ///     customComparer: new ProductSkuComparer());
    /// 
    /// // Example 3: DTO type (uses registered comparer or default)
    /// var diff = diffService.ComputeDiff(customerDtos, apiCustomers);
    /// </code>
    /// </example>
    DataStoreDiff<T> ComputeDiff<T>(
        IReadOnlyList<T> sourceItems,
        IReadOnlyList<T> targetItems,
        IEqualityComparer<T>? customComparer = null) where T : class;
}
