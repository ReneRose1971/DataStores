namespace DataStores.Abstractions;

/// <summary>
/// Defines a registrar that libraries can implement to register their global data stores.
/// </summary>
public interface IDataStoreRegistrar
{
    /// <summary>
    /// Registers global data stores with the registry.
    /// </summary>
    /// <param name="registry">The global store registry.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    void Register(IGlobalStoreRegistry registry, IServiceProvider serviceProvider);
}
