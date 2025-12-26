using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DataStores.PathProviders;

namespace TestHelper.DataStores.TestSetup;

/// <summary>
/// Helper methods for setting up DataStore tests with all required services.
/// </summary>
public static class DataStoreTestSetup
{
    /// <summary>
    /// Creates a ServiceCollection with DataStores core services and a test PathProvider.
    /// </summary>
    /// <param name="useTestPathProvider">
    /// If true, uses TestDataStorePathProvider with isolated temp directory.
    /// If false, uses NullDataStorePathProvider (for InMemory-only tests).
    /// </param>
    /// <returns>Configured ServiceCollection ready for registrar registration.</returns>
    public static IServiceCollection CreateTestServices(bool useTestPathProvider = true)
    {
        var services = new ServiceCollection();
        
        // Register DataStores core services
        new DataStoresServiceModule().Register(services);
        
        // Register appropriate PathProvider for tests
        if (useTestPathProvider)
        {
            services.AddSingleton<IDataStorePathProvider>(new TestDataStorePathProvider());
        }
        else
        {
            services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        }
        
        return services;
    }

    /// <summary>
    /// Extension method to add PathProvider to an existing ServiceCollection.
    /// Use this when you already have a ServiceCollection created.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="useTestPathProvider">
    /// If true, uses TestDataStorePathProvider with isolated temp directory.
    /// If false, uses NullDataStorePathProvider (for InMemory-only tests).
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTestPathProvider(this IServiceCollection services, bool useTestPathProvider = true)
    {
        if (useTestPathProvider)
        {
            services.AddSingleton<IDataStorePathProvider>(new TestDataStorePathProvider());
        }
        else
        {
            services.AddSingleton<IDataStorePathProvider>(new NullDataStorePathProvider());
        }
        
        return services;
    }
}
