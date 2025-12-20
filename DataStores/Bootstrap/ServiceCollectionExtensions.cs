using DataStores.Abstractions;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Bootstrap;

/// <summary>
/// Provides extension methods for registering DataStores services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core DataStores services with the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataStoresCore(this IServiceCollection services)
    {
        services.AddSingleton<IGlobalStoreRegistry, GlobalStoreRegistry>();
        services.AddSingleton<ILocalDataStoreFactory, LocalDataStoreFactory>();
        services.AddSingleton<IDataStores, DataStoresFacade>();

        return services;
    }

    /// <summary>
    /// Registers a data store registrar with the service collection.
    /// </summary>
    /// <typeparam name="TRegistrar">The registrar type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataStoreRegistrar<TRegistrar>(this IServiceCollection services)
        where TRegistrar : class, IDataStoreRegistrar
    {
        services.AddSingleton<IDataStoreRegistrar, TRegistrar>();
        return services;
    }
}
