using DataStores.Abstractions;
using DataStores.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

namespace TestHelper.DataStores.Fixtures;

/// <summary>
/// Shared Fixture für JSON-basierte Integration-Tests.
/// Erstellt einen vollständig initialisierten DataStore-Kontext mit JSON-Persistierung.
/// </summary>
/// <remarks>
/// Verwendung mit xUnit IClassFixture oder IAsyncLifetime für async Setup.
/// Stellt einen konfigurierten ServiceProvider und IDataStores-Facade bereit.
/// </remarks>
public sealed class JsonIntegrationFixture : IAsyncDisposable
{
    private bool _isInitialized;

    /// <summary>
    /// Root-Verzeichnis für JSON-Dateien.
    /// </summary>
    public string DataPath { get; private set; } = "";

    /// <summary>
    /// Konfigurierter Service Provider mit allen DataStore-Services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// DataStores-Facade für Zugriff auf globale und lokale Stores.
    /// </summary>
    public IDataStores DataStores { get; private set; } = null!;

    /// <summary>
    /// Initialisiert die JSON-Fixture mit einem spezifischen Registrar.
    /// </summary>
    /// <typeparam name="TRegistrar">Typ des zu verwendenden DataStore-Registrars.</typeparam>
    /// <param name="registrar">Instanz des Registrars mit Konfiguration.</param>
    public async Task InitializeAsync<TRegistrar>(TRegistrar registrar) 
        where TRegistrar : IDataStoreRegistrar
    {
        if (_isInitialized)
            throw new InvalidOperationException("Fixture bereits initialisiert.");

        DataPath = Path.Combine(Path.GetTempPath(), $"DataStoresTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(DataPath);

        var services = new ServiceCollection();
        var module = new DataStoresServiceModule();
        module.Register(services);
        services.AddDataStoreRegistrar(registrar);

        ServiceProvider = services.BuildServiceProvider();
        await DataStoreBootstrap.RunAsync(ServiceProvider);

        DataStores = ServiceProvider.GetRequiredService<IDataStores>();
        _isInitialized = true;
    }

    /// <summary>
    /// Bereinigt Ressourcen und löscht das Test-Verzeichnis.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(DataPath))
        {
            try
            {
                Directory.Delete(DataPath, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
