using Common.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DataStores.Bootstrap;

/// <summary>
/// Bootstrap-Decorator für DataStores, der das Common.Bootstrap Framework erweitert.
/// Ermöglicht automatische Registrierung von DataStores-spezifischen Services zusätzlich
/// zu den Standard-IServiceModule-Implementierungen.
/// </summary>
/// <remarks>
/// <para>
/// Dieser Decorator folgt dem Decorator-Pattern aus Common.Bootstrap und erweitert
/// den Bootstrap-Prozess um DataStores-spezifische Assembly-Scans.
/// </para>
/// <para>
/// <b>Verwendung:</b>
/// </para>
/// <code>
/// var bootstrap = new DataStoresBootstrapDecorator(new DefaultBootstrapWrapper());
/// bootstrap.RegisterServices(builder.Services, typeof(Program).Assembly);
/// </code>
/// </remarks>
public class DataStoresBootstrapDecorator : IBootstrapWrapper
{
    private readonly IBootstrapWrapper _innerWrapper;

    /// <summary>
    /// Initialisiert eine neue Instanz des DataStoresBootstrapDecorator.
    /// </summary>
    /// <param name="innerWrapper">
    /// Der innere Bootstrap-Wrapper (z.B. DefaultBootstrapWrapper aus Common.Bootstrap).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Wird ausgelöst, wenn <paramref name="innerWrapper"/> null ist.
    /// </exception>
    public DataStoresBootstrapDecorator(IBootstrapWrapper innerWrapper)
    {
        _innerWrapper = innerWrapper ?? throw new ArgumentNullException(nameof(innerWrapper));
    }

    /// <summary>
    /// Registriert Services aus den angegebenen Assemblies.
    /// Führt zuerst die Basis-Registrierungen durch (IServiceModule, EqualityComparer),
    /// dann DataStores-spezifische Registrierungen.
    /// </summary>
    /// <param name="services">Die Service-Collection.</param>
    /// <param name="assemblies">Die zu scannenden Assemblies.</param>
    /// <remarks>
    /// <para>
    /// <b>Ausführungsreihenfolge:</b>
    /// </para>
    /// <list type="number">
    /// <item><description>Basis-Registrierungen via innerWrapper (IServiceModule, EqualityComparer)</description></item>
    /// <item><description>DataStores-spezifische Scans (zukünftig: IDataStoreRegistrar, etc.)</description></item>
    /// </list>
    /// </remarks>
    public void RegisterServices(IServiceCollection services, params Assembly[] assemblies)
    {
        // Basis-Registrierungen (IServiceModule, EqualityComparer)
        _innerWrapper.RegisterServices(services, assemblies);

        // DataStores-spezifische Scans
        services.AddDataStoreRegistrarsFromAssemblies(assemblies);
    }
}
