using DataStores.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Bootstrap;

/// <summary>
/// REGISTRATION EXTENSIONS for IDataStoreRegistrar instances.
/// Use ONLY during startup configuration to register store registrars.
/// </summary>
/// <remarks>
/// These extensions are intended for external callers (application startup code) to register
/// their own IDataStoreRegistrar implementations with the DI container.
/// Do NOT use for general service registration or feature code.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a data store registrar with the service collection.
    /// </summary>
    /// <typeparam name="TRegistrar">The registrar type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Call during startup configuration before building the service provider.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDataStoreRegistrar&lt;ProductStoreRegistrar&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddDataStoreRegistrar<TRegistrar>(this IServiceCollection services)
        where TRegistrar : class, IDataStoreRegistrar
    {
        services.AddSingleton<IDataStoreRegistrar, TRegistrar>();
        return services;
    }

    /// <summary>
    /// Registers a data store registrar instance with the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registrar">The registrar instance to register.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method allows registration of registrar instances that receive configuration via constructor.
    /// Useful when registrars need configuration parameters like file paths or connection strings.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDataStoreRegistrar(new ProductStoreRegistrar(dbPath));
    /// </code>
    /// </example>
    public static IServiceCollection AddDataStoreRegistrar(this IServiceCollection services, IDataStoreRegistrar registrar)
    {
        if (registrar == null)
            throw new ArgumentNullException(nameof(registrar));

        services.AddSingleton(registrar);
        return services;
    }
}
