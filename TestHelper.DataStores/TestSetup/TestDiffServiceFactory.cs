using DataStores.Abstractions;
using DataStores.Bootstrap;
using DataStores.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace TestHelper.DataStores.TestSetup;

/// <summary>
/// Factory für Test-Instanzen von IDataStoreDiffService.
/// Wird in Integration-Tests verwendet, die direkt Persistenz-Strategien erstellen.
/// </summary>
public static class TestDiffServiceFactory
{
    /// <summary>
    /// Erstellt eine Instanz von IDataStoreDiffService für Tests.
    /// </summary>
    /// <returns>Eine funktionierende IDataStoreDiffService-Instanz mit EqualityComparerService.</returns>
    public static IDataStoreDiffService Create()
    {
        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        
        var serviceProvider = services.BuildServiceProvider();
        var comparerService = serviceProvider.GetRequiredService<IEqualityComparerService>();
        return new DataStoreDiffService(comparerService);
    }
}
