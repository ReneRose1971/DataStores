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
/// </remarks>
/// <example>
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
