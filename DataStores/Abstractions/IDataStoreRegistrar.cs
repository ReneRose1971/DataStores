namespace DataStores.Abstractions;

/// <summary>
/// REGISTRATION-ONLY API used during application startup.
/// Libraries MUST implement this interface to register their global data stores.
/// </summary>
/// <remarks>
/// <para>
/// Registration MUST occur during startup before first access to stores.
/// Do NOT resolve or access stores within the Register method; ONLY register them.
/// </para>
/// <para>
/// After registration, use <see cref="IDataStores"/> facade to access stores.
/// </para>
/// <para>
/// <b>Recommended Implementation:</b>
/// </para>
/// <para>
/// Instead of implementing this interface directly, inherit from <see cref="Registration.DataStoreRegistrarBase"/>
/// which provides a builder-based API that eliminates boilerplate code.
/// </para>
/// </remarks>
/// <example>
/// <para><b>RECOMMENDED: Using DataStoreRegistrarBase with builders</b></para>
/// <code>
/// public class MyAppStoreRegistrar : DataStoreRegistrarBase
/// {
///     public MyAppStoreRegistrar(string dbPath)
///     {
///         // InMemory store
///         AddStore(new InMemoryDataStoreBuilder&lt;Product&gt;());
///         
///         // JSON store
///         AddStore(new JsonDataStoreBuilder&lt;Customer&gt;(
///             filePath: "C:\\Data\\customers.json"));
///         
///         // LiteDB store (collection name auto-generated)
///         AddStore(new LiteDbDataStoreBuilder&lt;Order&gt;(
///             databasePath: dbPath));
///     }
/// }
/// </code>
/// <para><b>ALTERNATIVE: Direct implementation (more boilerplate)</b></para>
/// <code>
/// public class ProductStoreRegistrar : IDataStoreRegistrar
/// {
///     public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
///     {
///         // ✅ Correct: Register stores
///         registry.RegisterGlobal(new InMemoryDataStore&lt;Product&gt;());
///         
///         // ❌ Wrong: Do NOT access stores here
///         // var store = registry.ResolveGlobal&lt;Product&gt;();
///     }
/// }
/// </code>
/// </example>
public interface IDataStoreRegistrar
{
    /// <summary>
    /// Registers global data stores with the registry.
    /// </summary>
    /// <param name="registry">The global store registry.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <remarks>
    /// This method is called once during startup by <see cref="Bootstrap.DataStoreBootstrap"/>.
    /// Do NOT perform initialization or data loading here; use the bootstrap process instead.
    /// </remarks>
    void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider);
}
