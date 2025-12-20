using DataStores.Abstractions;
using DataStores.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Bootstrap;

/// <summary>
/// Provides bootstrap functionality for initializing data stores.
/// </summary>
public static class DataStoreBootstrap
{
    /// <summary>
    /// Runs the data store bootstrap process, registering all stores and initializing persistent stores.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task RunAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var registry = serviceProvider.GetRequiredService<IGlobalStoreRegistry>();
        var registrars = serviceProvider.GetServices<IDataStoreRegistrar>();

        foreach (var registrar in registrars)
        {
            registrar.Register(registry, serviceProvider);
        }

        var initializables = serviceProvider.GetServices<IAsyncInitializable>();
        foreach (var initializable in initializables)
        {
            await initializable.InitializeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Runs the data store bootstrap process synchronously (for testing or simple scenarios).
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void Run(IServiceProvider serviceProvider)
    {
        RunAsync(serviceProvider).GetAwaiter().GetResult();
    }
}
