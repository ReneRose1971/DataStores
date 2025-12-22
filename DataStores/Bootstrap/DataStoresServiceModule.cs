using Common.Bootstrap;
using DataStores.Abstractions;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DataStores.Bootstrap;

/// <summary>
/// Service-Modul für DataStores: Registriert alle Kern-Services für das DataStores-Framework.
/// </summary>
/// <remarks>
/// <para>
/// Dieses Modul registriert alle grundlegenden Services, die für das DataStores-Framework
/// benötigt werden, einschließlich:
/// </para>
/// <list type="bullet">
/// <item><see cref="IGlobalStoreRegistry"/> - Zentrale Registry für globale DataStores</item>
/// <item><see cref="ILocalDataStoreFactory"/> - Factory für lokale DataStores</item>
/// <item><see cref="IDataStores"/> - Haupt-Facade für den Zugriff auf DataStores</item>
/// </list>
/// <para>
/// <b>Verwendung:</b>
/// </para>
/// <code>
/// // In Program.cs oder Startup.cs
/// var builder = Host.CreateApplicationBuilder(args);
/// 
/// // Automatische Registrierung via ServiceModule-Pattern
/// builder.Services.AddModulesFromAssemblies(
///     typeof(Program).Assembly,
///     typeof(DataStoresServiceModule).Assembly);
/// 
/// var app = builder.Build();
/// await app.RunAsync();
/// </code>
/// <para>
/// <b>Alternative:</b> Verwenden Sie <see cref="ServiceCollectionExtensions.AddDataStoresCore"/>
/// für eine noch einfachere Registrierung ohne explizite Modul-Instanziierung.
/// </para>
/// </remarks>
public sealed class DataStoresServiceModule : IServiceModule
{
    /// <summary>
    /// Registriert alle DataStores-Kern-Services im Dependency Injection Container.
    /// </summary>
    /// <param name="services">Die Service-Collection, in die registriert werden soll.</param>
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IGlobalStoreRegistry, GlobalStoreRegistry>();
        services.AddSingleton<ILocalDataStoreFactory, LocalDataStoreFactory>();
        services.AddSingleton<IDataStores, DataStoresFacade>();
    }
}
