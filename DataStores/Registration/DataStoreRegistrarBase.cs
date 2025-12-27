using DataStores.Abstractions;
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Registration;

/// <summary>
/// Abstract base class for implementing <see cref="IDataStoreRegistrar"/> with builder pattern.
/// Simplifies store registration by eliminating boilerplate code and providing a type-safe API.
/// </summary>
/// <remarks>
/// <para>
/// <b>How to use:</b>
/// </para>
/// <list type="number">
/// <item><description>Inherit from this class with a parameterless constructor</description></item>
/// <item><description>Override <see cref="ConfigureStores"/> to add your stores using <see cref="AddStore{T}"/></description></item>
/// <item><description>Use <see cref="IDataStorePathProvider"/> parameter for file/database paths</description></item>
/// <item><description>Register via <see cref="ServiceCollectionExtensions.AddDataStoreRegistrarsFromAssembly"/></description></item>
/// <item><description>Call <see cref="DataStoreBootstrap.RunAsync"/> during startup</description></item>
/// </list>
/// <para>
/// <b>Available Builders:</b>
/// <see cref="InMemoryDataStoreBuilder{T}"/>, <see cref="JsonDataStoreBuilder{T}"/>, <see cref="LiteDbDataStoreBuilder{T}"/>
/// </para>
/// </remarks>
/// <example>
/// <para><b>Modern pattern with IDataStorePathProvider (RECOMMENDED):</b></para>
/// <code>
/// public class MyAppStoreRegistrar : DataStoreRegistrarBase
/// {
///     // Parameterless constructor for assembly scanning
///     public MyAppStoreRegistrar()
///     {
///     }
///     
///     protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
///     {
///         // InMemory store (no persistence) - automatic comparer resolution
///         AddStore(new InMemoryDataStoreBuilder&lt;Product&gt;());
///         
///         // JSON store with auto-load and auto-save
///         AddStore(new JsonDataStoreBuilder&lt;Customer&gt;(
///             filePath: pathProvider.FormatJsonFileName("customers")));
///         
///         // LiteDB store with auto-generated collection name
///         AddStore(new LiteDbDataStoreBuilder&lt;Order&gt;(
///             databasePath: pathProvider.FormatLiteDbFileName("myapp")));
///     }
/// }
/// 
/// // Usage in Program.cs
/// services.AddSingleton&lt;IDataStorePathProvider&gt;(
///     new DataStorePathProvider("MyApp"));
/// services.AddDataStoreRegistrarsFromAssembly();
/// var provider = services.BuildServiceProvider();
/// await DataStoreBootstrap.RunAsync(provider);
/// </code>
/// </example>
public abstract class DataStoreRegistrarBase : IDataStoreRegistrar
{
    private readonly List<Action<IGlobalStoreRegistry, IServiceProvider>> _registrations = new();

    /// <summary>
    /// Registers all configured data stores with the global registry.
    /// Called automatically by <see cref="DataStoreBootstrap.RunAsync"/>. Do NOT call manually.
    /// </summary>
    /// <param name="registry">The global store registry.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <remarks>
    /// <b>Execution Order:</b>
    /// 1. Resolves <see cref="IDataStorePathProvider"/> from DI
    /// 2. Calls <see cref="ConfigureStores"/> (your implementation)
    /// 3. Executes all builders added via <see cref="AddStore{T}"/> (with IServiceProvider for comparer resolution)
    /// 4. Bootstrap initializes persistent stores (auto-load if enabled)
    /// </remarks>
    public void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider)
    {
        // 1. Get path provider from DI
        var pathProvider = serviceProvider.GetRequiredService<IDataStorePathProvider>();

        // 2. Let derived class configure stores
        ConfigureStores(serviceProvider, pathProvider);

        // 3. Execute all builder registrations (with ServiceProvider for comparer resolution)
        foreach (var registration in _registrations)
        {
            registration(registry, serviceProvider);
        }
    }

    /// <summary>
    /// Configure your data stores by calling <see cref="AddStore{T}"/> with appropriate builders.
    /// Use the provided <paramref name="pathProvider"/> to get file and database paths.
    /// Access additional services via <paramref name="serviceProvider"/> if needed.
    /// </summary>
    /// <param name="serviceProvider">
    /// Service provider for resolving additional services (e.g., IEqualityComparer, SynchronizationContext).
    /// Use GetService() or GetRequiredService() to resolve dependencies.
    /// </param>
    /// <param name="pathProvider">
    /// Path provider for generating standardized file paths.
    /// Use <see cref="IDataStorePathProvider.FormatJsonFileName"/> for JSON files
    /// and <see cref="IDataStorePathProvider.FormatLiteDbFileName"/> for databases.
    /// </param>
    /// <example>
    /// <code>
    /// protected override void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider)
    /// {
    ///     // Simple store with automatic comparer resolution
    ///     AddStore(new InMemoryDataStoreBuilder&lt;Product&gt;());
    ///     
    ///     // Store with path provider
    ///     AddStore(new LiteDbDataStoreBuilder&lt;Order&gt;(
    ///         databasePath: pathProvider.FormatLiteDbFileName("myapp")));
    ///     
    ///     // Advanced: Resolve custom comparer from DI (optional - automatic resolution is preferred)
    ///     var comparer = serviceProvider.GetService&lt;IEqualityComparer&lt;Product&gt;&gt;();
    ///     if (comparer != null)
    ///     {
    ///         AddStore(new InMemoryDataStoreBuilder&lt;Product&gt;(comparer: comparer));
    ///     }
    /// }
    /// </code>
    /// </example>
    protected abstract void ConfigureStores(IServiceProvider serviceProvider, IDataStorePathProvider pathProvider);

    /// <summary>
    /// Adds a data store builder to the registration list.
    /// Builders are executed in the order they are added.
    /// </summary>
    /// <typeparam name="T">The type of items in the store. Must be a reference type.</typeparam>
    /// <param name="builder">
    /// The builder that creates and registers the store.
    /// Available builders: <see cref="InMemoryDataStoreBuilder{T}"/>, 
    /// <see cref="JsonDataStoreBuilder{T}"/>, <see cref="LiteDbDataStoreBuilder{T}"/>
    /// </param>
    /// <remarks>
    /// <para>
    /// Each builder creates the appropriate store type (InMemory, JSON, LiteDB),
    /// applies decorators for persistence if needed, and registers the store with the global registry.
    /// </para>
    /// <para>
    /// Builders automatically resolve IEqualityComparer via IEqualityComparerService when no explicit
    /// comparer is provided, enabling automatic EntityIdComparer for EntityBase types and
    /// custom comparer resolution from DI.
    /// </para>
    /// </remarks>
    protected void AddStore<T>(DataStoreBuilder<T> builder) where T : class
    {
        _registrations.Add((registry, serviceProvider) => 
            builder.Register(registry, serviceProvider));
    }
}
