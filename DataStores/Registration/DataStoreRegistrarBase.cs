using DataStores.Abstractions;

namespace DataStores.Registration;

/// <summary>
/// Abstract base class for implementing <see cref="IDataStoreRegistrar"/> with builder pattern.
/// Eliminates boilerplate code and provides a clean, declarative API for store registration.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b>
/// </para>
/// <para>
/// This class simplifies the implementation of <see cref="IDataStoreRegistrar"/> by providing
/// a builder-based registration API. Instead of manually creating stores, decorators, and strategies,
/// you declare your stores using type-safe builders.
/// </para>
/// <para>
/// <b>How it works:</b>
/// </para>
/// <list type="number">
/// <item><description>Inherit from this class</description></item>
/// <item><description>Call <see cref="AddStore{T}"/> with appropriate builders in your constructor</description></item>
/// <item><description>Register your registrar via <see cref="Bootstrap.ServiceCollectionExtensions.AddDataStoreRegistrar{TRegistrar}"/></description></item>
/// <item><description>Call <see cref="Bootstrap.DataStoreBootstrap.RunAsync"/> during startup</description></item>
/// </list>
/// <para>
/// <b>Available Builders:</b>
/// </para>
/// <list type="bullet">
/// <item><description><see cref="InMemoryDataStoreBuilder{T}"/> - Transient in-memory stores</description></item>
/// <item><description><see cref="JsonDataStoreBuilder{T}"/> - JSON file persistence</description></item>
/// <item><description><see cref="LiteDbDataStoreBuilder{T}"/> - LiteDB NoSQL persistence</description></item>
/// </list>
/// <para>
/// <b>Key Benefits:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Type-safe: Compile-time errors for invalid configurations</description></item>
/// <item><description>Self-documenting: Named parameters make intent clear</description></item>
/// <item><description>No boilerplate: No manual decorator/strategy creation</description></item>
/// <item><description>Consistent: Collection names auto-generated from type names</description></item>
/// <item><description>Extensible: Create custom builders for specialized scenarios</description></item>
/// </list>
/// <para>
/// <b>Registration Phase vs Access Phase:</b>
/// </para>
/// <para>
/// This class is for REGISTRATION ONLY (during startup). After bootstrap completes,
/// application code MUST access stores via <see cref="IDataStores"/> facade.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple registrar with three different store types
/// public class MyAppStoreRegistrar : DataStoreRegistrarBase
/// {
///     public MyAppStoreRegistrar(string dbPath)
///     {
///         // In-memory store (no persistence)
///         AddStore(new InMemoryDataStoreBuilder&lt;Product&gt;());
///         
///         // JSON store with auto-load and auto-save
///         AddStore(new JsonDataStoreBuilder&lt;Customer&gt;(
///             filePath: "C:\\Data\\customers.json"));
///         
///         // LiteDB store with auto-generated collection name
///         AddStore(new LiteDbDataStoreBuilder&lt;Order&gt;(
///             databasePath: dbPath));
///     }
/// }
/// 
/// // Usage in Program.cs
/// var services = new ServiceCollection();
/// services.AddDataStoreRegistrar(new MyAppStoreRegistrar("C:\\Data\\myapp.db"));
/// var provider = services.BuildServiceProvider();
/// await DataStoreBootstrap.RunAsync(provider);
/// 
/// // Access stores via facade ONLY
/// var stores = provider.GetRequiredService&lt;IDataStores&gt;();
/// var productStore = stores.GetGlobal&lt;Product&gt;();
/// </code>
/// </example>
/// <example>
/// <code>
/// // Advanced registrar with custom comparers and UI-thread support
/// public class AdvancedStoreRegistrar : DataStoreRegistrarBase
/// {
///     public AdvancedStoreRegistrar(string dbPath, SynchronizationContext uiContext)
///     {
///         // In-memory store with custom comparer
///         AddStore(new InMemoryDataStoreBuilder&lt;Category&gt;(
///             comparer: new CategoryIdComparer()));
///         
///         // JSON store with UI-thread event marshalling
///         AddStore(new JsonDataStoreBuilder&lt;Settings&gt;(
///             filePath: "C:\\Data\\settings.json",
///             synchronizationContext: uiContext));
///         
///         // LiteDB store with custom comparer and UI-thread events
///         AddStore(new LiteDbDataStoreBuilder&lt;Invoice&gt;(
///             databasePath: dbPath,
///             comparer: new InvoiceNumberComparer(),
///             synchronizationContext: uiContext));
///         
///         // Read-only store (auto-load but no auto-save)
///         AddStore(new JsonDataStoreBuilder&lt;Configuration&gt;(
///             filePath: "C:\\Data\\config.json",
///             autoLoad: true,
///             autoSave: false));
///     }
/// }
/// </code>
/// </example>
public abstract class DataStoreRegistrarBase : IDataStoreRegistrar
{
    private readonly List<Action<IGlobalStoreRegistry>> _registrations = new();

    /// <summary>
    /// Registers all configured data stores with the global registry.
    /// </summary>
    /// <param name="registry">The global store registry.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <remarks>
    /// <para>
    /// This method is called automatically by <see cref="Bootstrap.DataStoreBootstrap.RunAsync"/>
    /// during application startup. Do NOT call manually.
    /// </para>
    /// <para>
    /// All builders added via <see cref="AddStore{T}"/> are executed sequentially.
    /// Each builder creates its store and registers it with the global registry.
    /// </para>
    /// <para>
    /// <b>Execution Order:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>Constructor runs (builds registration list)</description></item>
    /// <item><description>Bootstrap calls Register method</description></item>
    /// <item><description>Each builder.Register() is executed</description></item>
    /// <item><description>Stores are now registered (but NOT initialized yet)</description></item>
    /// <item><description>Bootstrap initializes persistent stores (auto-load)</description></item>
    /// <item><description>Application can access stores via IDataStores</description></item>
    /// </list>
    /// </remarks>
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        foreach (var registration in _registrations)
        {
            registration(registry);
        }
    }

    /// <summary>
    /// Adds a data store builder to the registration list.
    /// </summary>
    /// <typeparam name="T">The type of items in the store. Must be a reference type.</typeparam>
    /// <param name="builder">The builder that creates and registers the store.</param>
    /// <remarks>
    /// <para>
    /// This method captures the builder and defers its execution until the <see cref="Register"/> method is called.
    /// This allows declarative store configuration in the constructor.
    /// </para>
    /// <para>
    /// <b>Builder Execution:</b>
    /// </para>
    /// <para>
    /// Builders are executed in the order they are added. Each builder is responsible for:
    /// </para>
    /// <list type="number">
    /// <item><description>Creating the appropriate store type (InMemory, JSON, LiteDB)</description></item>
    /// <item><description>Creating decorators if persistence is needed</description></item>
    /// <item><description>Creating strategies if persistence is needed</description></item>
    /// <item><description>Registering the final store with the global registry</description></item>
    /// </list>
    /// <para>
    /// <b>Type Safety:</b>
    /// </para>
    /// <para>
    /// The type parameter T is enforced at compile-time. You cannot register incompatible types.
    /// For example, LiteDbDataStoreBuilder requires T : EntityBase.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In your registrar constructor:
    /// 
    /// // InMemory store
    /// AddStore(new InMemoryDataStoreBuilder&lt;Product&gt;());
    /// 
    /// // JSON store
    /// AddStore(new JsonDataStoreBuilder&lt;Customer&gt;(
    ///     filePath: "customers.json"));
    /// 
    /// // LiteDB store
    /// AddStore(new LiteDbDataStoreBuilder&lt;Order&gt;(
    ///     databasePath: "myapp.db"));
    /// </code>
    /// </example>
    protected void AddStore<T>(DataStoreBuilder<T> builder) where T : class
    {
        _registrations.Add(registry => builder.Register(registry));
    }
}
