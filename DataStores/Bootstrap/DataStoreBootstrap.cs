using DataStores.Abstractions;
using DataStores.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Bootstrap;

/// <summary>
/// STARTUP INITIALIZATION for data stores. MUST be executed once during application startup.
/// Do NOT call from feature code, viewmodels, or services.
/// </summary>
/// <remarks>
/// <para>
/// This class orchestrates the complete initialization sequence:
/// </para>
/// <list type="number">
/// <item><description>Execute all <see cref="IDataStoreRegistrar"/> implementations to register stores</description></item>
/// <item><description>Initialize persistent stores by loading data asynchronously</description></item>
/// <item><description>Initialize additional async-initializable services</description></item>
/// </list>
/// <para>
/// After bootstrap completes, access stores via <see cref="IDataStores"/> facade ONLY.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Program.cs or Startup.cs
/// var serviceProvider = services.BuildServiceProvider();
/// await DataStoreBootstrap.RunAsync(serviceProvider);
/// 
/// // Now use stores via facade
/// var stores = serviceProvider.GetRequiredService&lt;IDataStores&gt;();
/// var productStore = stores.GetGlobal&lt;Product&gt;();
/// </code>
/// </example>
public static class DataStoreBootstrap
{
    /// <summary>
    /// Runs the data store bootstrap process, registering all stores and initializing persistent stores.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <remarks>
    /// This method MUST be called once during application startup, after building the service provider.
    /// Do NOT call from feature code or services.
    /// </remarks>
    public static async Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var registry = serviceProvider.GetRequiredService<IGlobalStoreRegistry>();
        var registrars = serviceProvider.GetServices<IDataStoreRegistrar>();

        foreach (var registrar in registrars)
        {
            registrar.Register(registry, serviceProvider);
        }

        // Initialize stores from registry
        var initializableStores = registry.GetInitializableGlobalStores();
        foreach (var initializable in initializableStores)
        {
            await initializable.InitializeAsync(cancellationToken);
        }

        // Initialize additional IAsyncInitializable services (if any)
        var initializableServices = serviceProvider.GetServices<IAsyncInitializable>();
        foreach (var initializable in initializableServices)
        {
            await initializable.InitializeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Runs the data store bootstrap process synchronously (for testing or simple scenarios).
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <remarks>
    /// Prefer <see cref="RunAsync"/> for production scenarios to avoid blocking the thread.
    /// This synchronous variant is provided for testing or console applications.
    /// </remarks>
    public static void Run(IServiceProvider serviceProvider)
    {
        RunAsync(serviceProvider).GetAwaiter().GetResult();
    }
}
