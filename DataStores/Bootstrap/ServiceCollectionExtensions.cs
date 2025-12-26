using DataStores.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
        {
            throw new ArgumentNullException(nameof(registrar));
        }

        services.AddSingleton(registrar);
        return services;
    }

    /// <summary>
    /// Automatically discovers and registers all IDataStoreRegistrar implementations from the calling assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method scans the calling assembly for all non-abstract classes that implement
    /// <see cref="IDataStoreRegistrar"/> and have a public parameterless constructor.
    /// Each discovered registrar is registered as a singleton.
    /// </para>
    /// <para>
    /// <b>Requirements:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Registrars MUST have a public parameterless constructor</description></item>
    /// <item><description>Registrars MUST be non-abstract classes</description></item>
    /// <item><description>Registrars MUST implement <see cref="IDataStoreRegistrar"/></description></item>
    /// </list>
    /// <para>
    /// If your registrars need constructor parameters, use <see cref="AddDataStoreRegistrar(IServiceCollection, IDataStoreRegistrar)"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Automatically registers all IDataStoreRegistrar implementations from calling assembly
    /// services.AddDataStoreRegistrarsFromAssembly();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddDataStoreRegistrarsFromAssembly(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var callingAssembly = Assembly.GetCallingAssembly();
        return AddDataStoreRegistrarsFromAssembly(services, callingAssembly);
    }

    /// <summary>
    /// Automatically discovers and registers all IDataStoreRegistrar implementations from the specified assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for registrars.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method scans the specified assembly for all non-abstract classes that implement
    /// <see cref="IDataStoreRegistrar"/> and have a public parameterless constructor.
    /// Each discovered registrar is registered as a singleton.
    /// </para>
    /// <para>
    /// <b>Requirements:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Registrars MUST have a public parameterless constructor</description></item>
    /// <item><description>Registrars MUST be non-abstract classes</description></item>
    /// <item><description>Registrars MUST implement <see cref="IDataStoreRegistrar"/></description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register from specific assembly
    /// services.AddDataStoreRegistrarsFromAssembly(typeof(ProductStoreRegistrar).Assembly);
    /// 
    /// // Register from current assembly
    /// services.AddDataStoreRegistrarsFromAssembly(Assembly.GetExecutingAssembly());
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when services or assembly is null.</exception>
    public static IServiceCollection AddDataStoreRegistrarsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        var registrarTypes = assembly.GetTypes()
            .Where(type =>
                typeof(IDataStoreRegistrar).IsAssignableFrom(type) &&
                type is { IsClass: true, IsAbstract: false } &&
                type.GetConstructor(Type.EmptyTypes) != null)
            .ToList();

        foreach (var registrarType in registrarTypes)
        {
            services.AddSingleton(typeof(IDataStoreRegistrar), registrarType);
        }

        return services;
    }

    /// <summary>
    /// Automatically discovers and registers all IDataStoreRegistrar implementations from multiple assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for registrars.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method scans all specified assemblies for non-abstract classes that implement
    /// <see cref="IDataStoreRegistrar"/> and have a public parameterless constructor.
    /// Each discovered registrar is registered as a singleton.
    /// </para>
    /// <para>
    /// Useful when registrars are distributed across multiple assemblies.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register from multiple assemblies
    /// services.AddDataStoreRegistrarsFromAssemblies(
    ///     typeof(ProductStoreRegistrar).Assembly,
    ///     typeof(CustomerStoreRegistrar).Assembly);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when services or assemblies is null.</exception>
    public static IServiceCollection AddDataStoreRegistrarsFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (assemblies == null)
        {
            throw new ArgumentNullException(nameof(assemblies));
        }

        foreach (var assembly in assemblies)
        {
            if (assembly != null)
            {
                AddDataStoreRegistrarsFromAssembly(services, assembly);
            }
        }

        return services;
    }
}
