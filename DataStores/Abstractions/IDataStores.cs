namespace DataStores.Abstractions;

/// <summary>
/// PRIMARY API for accessing global and creating local data stores.
/// Application code MUST access stores ONLY via IDataStores.
/// </summary>
/// <remarks>
/// <para>
/// This facade provides the ONLY supported entry point for application code to interact with data stores.
/// Do NOT directly use infrastructure types like <see cref="IGlobalStoreRegistry"/> or <see cref="Runtime.GlobalStoreRegistry"/>.
/// </para>
/// <para>
/// Supported operations:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="GetGlobal{T}"/> - Access application-wide singleton stores</description></item>
/// <item><description><see cref="CreateLocal{T}"/> - Create isolated local stores</description></item>
/// <item><description><see cref="CreateLocalSnapshotFromGlobal{T}"/> - Create filtered snapshots from global stores</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class ProductService
/// {
///     private readonly IDataStores _stores;
///     
///     public ProductService(IDataStores stores)
///     {
///         _stores = stores;
///     }
///     
///     public void AddProduct(Product product)
///     {
///         var store = _stores.GetGlobal&lt;Product&gt;();
///         store.Add(product);
///     }
/// }
/// </code>
/// </example>
public interface IDataStores
{
    /// <summary>
    /// Gets the globally registered data store for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <returns>The global data store.</returns>
    /// <exception cref="GlobalStoreNotRegisteredException">Thrown when no global store is registered for type <typeparamref name="T"/>.</exception>
    /// <remarks>
    /// Global stores are application-wide singletons. All parts of the application share the same instance.
    /// </remarks>
    IDataStore<T> GetGlobal<T>() where T : class;

    /// <summary>
    /// Creates a new local in-memory data store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="comparer">Optional equality comparer for items.</param>
    /// <returns>A new local in-memory data store.</returns>
    /// <remarks>
    /// Local stores are isolated instances, independent from global stores.
    /// Useful for temporary data, dialogs, or scenarios requiring isolation.
    /// </remarks>
    IDataStore<T> CreateLocal<T>(IEqualityComparer<T>? comparer = null) where T : class;

    /// <summary>
    /// Creates a new local in-memory data store and populates it with a snapshot from the global store.
    /// </summary>
    /// <typeparam name="T">The type of items in the store.</typeparam>
    /// <param name="predicate">Optional filter predicate for the snapshot.</param>
    /// <param name="comparer">Optional equality comparer for items.</param>
    /// <returns>A new local in-memory data store containing the filtered snapshot.</returns>
    /// <exception cref="GlobalStoreNotRegisteredException">Thrown when no global store is registered for type <typeparamref name="T"/>.</exception>
    /// <remarks>
    /// Changes to the local snapshot do NOT affect the global store.
    /// </remarks>
    IDataStore<T> CreateLocalSnapshotFromGlobal<T>(
        Func<T, bool>? predicate = null,
        IEqualityComparer<T>? comparer = null) where T : class;
}
