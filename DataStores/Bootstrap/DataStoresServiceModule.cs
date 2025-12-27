using Common.Bootstrap;
using DataStores.Abstractions;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Bootstrap;

/// <summary>
/// INFRASTRUCTURE SERVICE MODULE for DataStores framework.
/// Registers core services required by the DataStores framework.
/// </summary>
/// <remarks>
/// <para>
/// This module is INTERNAL INFRASTRUCTURE and registers:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IGlobalStoreRegistry"/> - Registry for global stores (infrastructure only)</description></item>
/// <item><description><see cref="ILocalDataStoreFactory"/> - Factory for local stores (infrastructure only)</description></item>
/// <item><description><see cref="IDataStores"/> - PRIMARY API facade for application code</description></item>
/// <item><description><see cref="IEqualityComparerService"/> - Service for automatic comparer resolution</description></item>
/// </list>
/// <para>
/// Application code MUST use ONLY <see cref="IDataStores"/> after registration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Automatic registration via ServiceModule pattern
/// var builder = Host.CreateApplicationBuilder(args);
/// builder.Services.AddModulesFromAssemblies(
///     typeof(Program).Assembly,
///     typeof(DataStoresServiceModule).Assembly);
/// 
/// var app = builder.Build();
/// await DataStoreBootstrap.RunAsync(app.Services);
/// </code>
/// </example>
public sealed class DataStoresServiceModule : IServiceModule
{
    /// <summary>
    /// Registers all DataStores core services in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <remarks>
    /// This method is called automatically by the Common.Bootstrap framework.
    /// Do NOT call manually from application code.
    /// </remarks>
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IGlobalStoreRegistry, GlobalStoreRegistry>();
        services.AddSingleton<ILocalDataStoreFactory, LocalDataStoreFactory>();
        services.AddSingleton<IDataStores, DataStoresFacade>();
        services.AddSingleton<IEqualityComparerService, EqualityComparerService>();
    }
}
